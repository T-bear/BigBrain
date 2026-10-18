using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BigBrain.Api.Media;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BigBrain.Api.Tests;

public sealed class MediaUrlImportTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "bb-url-import-" + Guid.NewGuid().ToString("N"));
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 12, 0, 0, TimeSpan.Zero);
    private static MediaUrlImportInput Input => new("https://audio.example/chapter.mp3", "Kapitel 1 – Introduktion", "https://doi.org/10.1234/EXAMPLE", "MP3");
    private MediaOptions Options => new() { Audiobookshelf = new() { AcquisitionDatabasePath = Path.Combine(directory, "media.db") } };

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("http://audio.example/chapter.mp3")]
    [InlineData("https://127.0.0.1/chapter.mp3")]
    [InlineData("https://2130706433/chapter.mp3")]
    [InlineData("https://[::1]/chapter.mp3")]
    [InlineData("https://169.254.169.254/chapter.mp3")]
    [InlineData("https://localhost/chapter.mp3")]
    [InlineData("https://server.local/chapter.mp3")]
    [InlineData("https://server.internal/chapter.mp3")]
    [InlineData("https://audio.example:8443/chapter.mp3")]
    [InlineData("https://user:password@audio.example/chapter.mp3")]
    [InlineData("https://audio.example/chapter.mp3?token=example")]
    [InlineData("https://audio.example/chapter.mp3#fragment")]
    [InlineData("https://www.youtube.com/watch?v=example")]
    [InlineData("https://audio.example/video.mp4")]
    public void UnsafeOrUnsupportedSourcesAreRejectedWithoutNetwork(string url) =>
        Assert.Throws<AudiobookAcquisitionException>(() => MediaUrlImportValidation.Normalize(Input with { SourceUrl = url }));

    [Theory]
    [InlineData("../chapter")]
    [InlineData("folder/chapter")]
    [InlineData("folder\\chapter")]
    [InlineData("CON")]
    [InlineData("LPT1.mp3")]
    [InlineData("trailing.")]
    [InlineData("$(touch file)")]
    [InlineData("hidden\u202Ename")]
    public void UnsafeFileNamesAreRejected(string name) =>
        Assert.Throws<AudiobookAcquisitionException>(() => MediaUrlImportValidation.Normalize(Input with { FileName = name }));

    [Theory]
    [InlineData("")]
    [InlineData("10.12/example")]
    [InlineData("10.1234/example\ntrailer")]
    [InlineData("https://evil.example/10.1234/example")]
    [InlineData("10.1234/")]
    public void DoiRequiresBoundedSyntax(string doi) =>
        Assert.Throws<AudiobookAcquisitionException>(() => MediaUrlImportValidation.Normalize(Input with { Doi = doi }));

    [Fact]
    public async Task MultipleNamesShareNormalizedDoiButHaveDistinctDurableIdentitiesAndNoFiles()
    {
        MediaUrlImportDraft first;
        using (var store = new AudiobookAcquisitionStore(Options))
        {
            first = await store.PreviewUrlImportAsync(Input, Now, TestContext.Current.CancellationToken);
            var second = await store.PreviewUrlImportAsync(Input with { FileName = "Annat namn" }, Now, TestContext.Current.CancellationToken);
            Assert.Equal("10.1234/example", first.Doi);
            Assert.Equal(first.Doi, second.Doi);
            Assert.NotEqual(first.Id, second.Id);
            Assert.NotEqual(first.ProposedFileName, second.ProposedFileName);
            Assert.DoesNotContain(first.Doi, first.ProposedFileName);
            Assert.False(first.CanStart);
            Assert.Empty((await store.ListAsync(0, 25, TestContext.Current.CancellationToken)).Items);
        }
        using var reopened = new AudiobookAcquisitionStore(Options);
        Assert.Equal(first, (await reopened.ListUrlImportsAsync(TestContext.Current.CancellationToken)).Single(x => x.Id == first.Id));
        Assert.Equal(2, (await reopened.ListUrlImportsAsync(TestContext.Current.CancellationToken)).Count);
        Assert.DoesNotContain(Directory.EnumerateFiles(directory), path => path.EndsWith(".mp3", StringComparison.Ordinal) || path.EndsWith(".m4a", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConfirmationIsExplicitIdempotentAndRemainsConfirmedAcrossReopen()
    {
        MediaUrlImportDraft blocked;
        using (var store = new AudiobookAcquisitionStore(Options))
        {
            var draft = await store.PreviewUrlImportAsync(Input, Now, TestContext.Current.CancellationToken);
            await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => store.ConfirmUrlImportAsync(draft.Id, new(true, false), Now, TestContext.Current.CancellationToken));
            await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => store.ConfirmUrlImportAsync(draft.Id, new(false, true), Now, TestContext.Current.CancellationToken));
            var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => store.ConfirmUrlImportAsync(draft.Id, new(true, true), Now.AddMinutes(1), TestContext.Current.CancellationToken)));
            blocked = results[0];
            Assert.All(results, value => Assert.Equal(blocked, value));
            Assert.Equal("confirmed", blocked.Status);
            Assert.True(blocked.CanStart);
            Assert.Equal("", blocked.BlockerCode);
            Assert.Empty((await store.ListAsync(0, 25, TestContext.Current.CancellationToken)).Items);
        }
        using var reopened = new AudiobookAcquisitionStore(Options);
        Assert.Equal(blocked, Assert.Single(await reopened.ListUrlImportsAsync(TestContext.Current.CancellationToken)));
        Assert.Equal(blocked, await reopened.ConfirmUrlImportAsync(blocked.Id, new(true, true), Now.AddDays(1), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExpiredPreviewAndCancelledWriteCannotAuthorizeAnything()
    {
        using var store = new AudiobookAcquisitionStore(Options);
        var draft = await store.PreviewUrlImportAsync(Input, Now, TestContext.Current.CancellationToken);
        foreach (var time in new[] { Now.AddTicks(-1), Now.AddMinutes(15) })
            Assert.Equal("previewExpired", (await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => store.ConfirmUrlImportAsync(draft.Id, new(true, true), time, TestContext.Current.CancellationToken))).Code);
        using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.PreviewUrlImportAsync(Input, Now, cancelled.Token));
        Assert.Equal(draft, Assert.Single(await store.ListUrlImportsAsync(TestContext.Current.CancellationToken)));
    }

    [Fact]
    public async Task DraftCountIsBoundedAndExistingRecordsAreNotDeleted()
    {
        using var store = new AudiobookAcquisitionStore(Options);
        for (var i = 0; i < 200; i++) await store.PreviewUrlImportAsync(Input, Now, TestContext.Current.CancellationToken);
        Assert.Equal("draftLimit", (await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => store.PreviewUrlImportAsync(Input, Now, TestContext.Current.CancellationToken))).Code);
        Assert.Equal(200, (await store.ListUrlImportsAsync(TestContext.Current.CancellationToken)).Count);
    }

    [Fact]
    public async Task VersionedEndpointsReturnProblemsAndStartFailsClosedWhenDisabled()
    {
        using var host = await new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services => { services.AddRouting(); services.AddSingleton(Options); services.AddSingleton<AudiobookAcquisitionStore>(); services.AddSingleton(TimeProvider.System); services.AddSingleton<IDirectAudioTransport, DirectAudioTransport>(); services.AddSingleton<IDirectAudioImporter>(new LibrarrDirectAudioImporter(new HttpClient())); services.AddSingleton<MediaUrlImportService>(); })
            .Configure(app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapMediaUrlImportEndpoints())))
            .StartAsync(TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();
        var rejected = await client.PostAsJsonAsync("/api/v1/modules/media/url-imports/preview", Input with { SourceUrl = "file:///tmp/a.mp3" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType!.MediaType);
        using var problem = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("invalidSource", problem.RootElement.GetProperty("code").GetString());
        var response = await client.PostAsJsonAsync("/api/v1/modules/media/url-imports/preview", Input, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var draft = (await response.Content.ReadFromJsonAsync<MediaUrlImportDraft>(TestContext.Current.CancellationToken))!;
        Assert.False(draft.CanStart);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync($"/api/v1/modules/media/url-imports/{draft.Id}/start", new { }, TestContext.Current.CancellationToken)).StatusCode);
        var confirmation = await client.PostAsJsonAsync($"/api/v1/modules/media/url-imports/{draft.Id}/confirm", new MediaUrlImportConfirmation(true, true), TestContext.Current.CancellationToken);
        confirmation.EnsureSuccessStatusCode();
        Assert.Equal("confirmed", (await confirmation.Content.ReadFromJsonAsync<MediaUrlImportDraft>(TestContext.Current.CancellationToken))!.Status);
    }

    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
