using BigBrain.Api.Media;
using Microsoft.Extensions.Logging.Abstractions;

namespace BigBrain.Api.Tests;

public sealed class DownloadControlTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdapterUsesSingleHashAndExplicitDeleteFiles(bool deleteFiles)
    {
        HttpRequestMessage? captured = null;
        string? form = null;
        using var http = new HttpClient(new Handler(async request =>
        {
            captured = request;
            form = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        })) { BaseAddress = new Uri("http://qbittorrent.test/") };
        var client = new QBittorrentClient(http, new MediaOptions
        {
            QBittorrent = new QBittorrentOptions { BaseUrl = "http://qbittorrent.test/", ApiKey = "fake" }
        });
        await ((IQBittorrentQueueClient)client).RemoveAsync(new string('a', 40), deleteFiles, CancellationToken.None);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("/api/v2/torrents/delete", captured.RequestUri!.AbsolutePath);
        Assert.Equal($"hashes={new string('a', 40)}&deleteFiles={deleteFiles.ToString().ToLowerInvariant()}", form);
        Assert.DoesNotContain("%7C", form, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("all", form, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListUsesOpaqueIdsAndSafeFields()
    {
        var fake = new FakeQueue([Item()]);
        var response = await Service(fake).GetAsync(TestContext.Current.CancellationToken);
        var item = Assert.Single(response.Downloads);
        Assert.Matches("^[0-9a-f]{36}$", item.Id);
        Assert.DoesNotContain(fake.Items[0].Hash, System.Text.Json.JsonSerializer.Serialize(response), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/downloads", System.Text.Json.JsonSerializer.Serialize(response), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("sonarr", item.Ownership);
        Assert.Contains(item.Warnings, warning => warning.Contains("Sonarr", StringComparison.Ordinal));
    }

    [Fact]
    public async Task NormalRemovalSendsExactlyOneHashAndPreservesFiles()
    {
        var fake = new FakeQueue([Item()]);
        var service = Service(fake);
        var listed = Assert.Single((await service.GetAsync(CancellationToken.None)).Downloads);
        var preview = await service.PreviewAsync(listed.Id, new(false), CancellationToken.None);
        var result = await service.RemoveAsync(listed.Id, new(preview.ConfirmationToken, false), CancellationToken.None);
        Assert.True(result.Removed);
        Assert.True(result.DataPreserved);
        Assert.Equal([(fake.Items[0].Hash, false)], fake.Removals);
    }

    [Fact]
    public async Task DestructiveRemovalRequiresMatchingExplicitPreview()
    {
        var fake = new FakeQueue([Item()]);
        var service = Service(fake);
        var listed = Assert.Single((await service.GetAsync(CancellationToken.None)).Downloads);
        var safePreview = await service.PreviewAsync(listed.Id, new(false), CancellationToken.None);
        var error = await Assert.ThrowsAsync<DownloadControlException>(() =>
            service.RemoveAsync(listed.Id, new(safePreview.ConfirmationToken, true), CancellationToken.None));
        Assert.Equal("confirmationExpired", error.Code);
        Assert.Empty(fake.Removals);
    }

    [Fact]
    public async Task DestructiveRemovalIsBlockedForCompletedOrSharedData()
    {
        foreach (var items in new[]
        {
            new[] { Item(progress: 1) },
            new[] { Item(), Item(hash: new string('b', 40)) },
            new[] { Item(contentPath: "") }
        })
        {
            var fake = new FakeQueue(items);
            var service = Service(fake);
            var listed = (await service.GetAsync(CancellationToken.None)).Downloads[0];
            var error = await Assert.ThrowsAsync<DownloadControlException>(() =>
                service.PreviewAsync(listed.Id, new(true), CancellationToken.None));
            Assert.True(error.Code is "destructiveRemovalNotAllowed" or "sharedPathRisk");
            Assert.Empty(fake.Removals);
        }
    }

    [Fact]
    public async Task IdentityChangeConflictsBeforeMutation()
    {
        var fake = new FakeQueue([Item()]);
        var service = Service(fake);
        var listed = Assert.Single((await service.GetAsync(CancellationToken.None)).Downloads);
        var preview = await service.PreviewAsync(listed.Id, new(false), CancellationToken.None);
        fake.Items = [Item(name: "Changed")];
        var error = await Assert.ThrowsAsync<DownloadControlException>(() =>
            service.RemoveAsync(listed.Id, new(preview.ConfirmationToken, false), CancellationToken.None));
        Assert.Equal("downloadIdentityChanged", error.Code);
        Assert.Empty(fake.Removals);
    }

    [Fact]
    public async Task MissingTorrentIsIdempotentAndTokenCannotDeleteTwice()
    {
        var fake = new FakeQueue([Item()]);
        var service = Service(fake);
        var listed = Assert.Single((await service.GetAsync(CancellationToken.None)).Downloads);
        var preview = await service.PreviewAsync(listed.Id, new(false), CancellationToken.None);
        fake.Items = [];
        var first = await service.RemoveAsync(listed.Id, new(preview.ConfirmationToken, false), CancellationToken.None);
        var second = await service.RemoveAsync(listed.Id, new(preview.ConfirmationToken, false), CancellationToken.None);
        Assert.True(first.AlreadyMissing);
        Assert.Equal(first, second);
        Assert.Empty(fake.Removals);
    }

    [Fact]
    public async Task ConcurrentRemovalMakesOnlyOneDelete()
    {
        var fake = new FakeQueue([Item()]) { DeleteGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
        var service = Service(fake);
        var listed = Assert.Single((await service.GetAsync(CancellationToken.None)).Downloads);
        var preview = await service.PreviewAsync(listed.Id, new(false), CancellationToken.None);
        var first = service.RemoveAsync(listed.Id, new(preview.ConfirmationToken, false), CancellationToken.None);
        await fake.DeleteStarted.Task;
        var error = await Assert.ThrowsAsync<DownloadControlException>(() =>
            service.RemoveAsync(listed.Id, new(preview.ConfirmationToken, false), CancellationToken.None));
        fake.DeleteGate.SetResult();
        await first;
        Assert.Equal("downloadRemovalConflict", error.Code);
        Assert.Single(fake.Removals);
    }

    [Fact]
    public async Task ProviderFailuresAreSanitized()
    {
        var service = Service(new FakeQueue([]) { Failure = new TaskCanceledException("raw hash " + new string('a', 40)) });
        var error = await Assert.ThrowsAsync<DownloadControlException>(() => service.GetAsync(CancellationToken.None));
        Assert.Equal("providerTimeout", error.Code);
        Assert.DoesNotContain("raw", error.SafeMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static DownloadControlService Service(FakeQueue fake) =>
        new(fake, new DownloadControlStore(), NullLogger<DownloadControlService>.Instance);

    private static QBittorrentQueueItem Item(
        string? hash = null, string name = "Safe.Show.S01E01", double progress = .4,
        string contentPath = "/downloads/Safe.Show.S01E01") =>
        new(hash ?? new string('a', 40), name, "downloading", "sonarr", "/downloads", contentPath,
            progress, 1000, 400, 20, 2, 1);

    private sealed class FakeQueue(IReadOnlyList<QBittorrentQueueItem> items) : IQBittorrentQueueClient
    {
        public IReadOnlyList<QBittorrentQueueItem> Items { get; set; } = items;
        public List<(string Hash, bool DeleteFiles)> Removals { get; } = [];
        public Exception? Failure { get; init; }
        public TaskCompletionSource DeleteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource? DeleteGate { get; init; }
        public Task<IReadOnlyList<QBittorrentQueueItem>> GetQueueAsync(CancellationToken cancellationToken) =>
            Failure is null ? Task.FromResult(Items) : Task.FromException<IReadOnlyList<QBittorrentQueueItem>>(Failure);
        public async Task RemoveAsync(string hash, bool deleteFiles, CancellationToken cancellationToken)
        {
            Removals.Add((hash, deleteFiles));
            DeleteStarted.TrySetResult();
            if (DeleteGate is not null) await DeleteGate.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}
