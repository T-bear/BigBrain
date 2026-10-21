using System.Net;
using BigBrain.Api.Media;

namespace BigBrain.Api.Tests;

public sealed class DirectAudioImportTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "bb158-test-" + Guid.NewGuid().ToString("N"));
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private MediaOptions Options => new() { Librarr = new() { DirectAudioImportEnabled = true, ApiKey = "fixture" }, Audiobookshelf = new() { LibraryId = "library", PublicUrl = "https://abs.example", AcquisitionDatabasePath = Path.Combine(directory, "media.db") } };
    private static MediaUrlImportInput Input => new("https://audio.example/file.mp3", "Chapter", "10.1234/example", "mp3");

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.2.3.4")]
    [InlineData("172.31.1.2")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("198.18.0.1")]
    [InlineData("192.0.2.1")]
    [InlineData("203.0.113.1")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("0.1.2.3")]
    [InlineData("::1")]
    [InlineData("::ffff:8.8.8.8")]
    [InlineData("2606:4700:4700::1111")]
    public void NonPublicOrUnsupportedFamiliesAreDenied(string ip) => Assert.False(DirectAudioTransport.IsPublicAddress(IPAddress.Parse(ip)));

    [Fact]
    public async Task ResolvedAddressIsPinnedAndMixedAnswersCannotConnect()
    {
        var resolutions = 0; var connections = 0;
        IPAddress[] addresses = [IPAddress.Parse("8.8.8.8")];
        var transport = new DirectAudioTransport((_, _) => { resolutions++; return Task.FromResult(addresses); },
            (endpoint, _) => { connections++; Assert.Equal("8.8.8.8", endpoint.Address.ToString()); Assert.Equal(443, endpoint.Port); return ValueTask.FromResult<Stream>(new MemoryStream()); });
        await using var stream = await transport.ConnectPublicAsync("audio.example", TestContext.Current.CancellationToken);
        Assert.Equal(1, resolutions); Assert.Equal(1, connections);
        addresses = [IPAddress.Parse("8.8.8.8"), IPAddress.Loopback];
        await Assert.ThrowsAsync<HttpRequestException>(async () => await transport.ConnectPublicAsync("audio.example", TestContext.Current.CancellationToken));
        Assert.Equal(1, connections);
    }

    [Fact]
    public void PublicIPv4AndOptionalNameAreAccepted()
    {
        Assert.True(DirectAudioTransport.IsPublicAddress(IPAddress.Parse("8.8.8.8")));
        Assert.Equal("Ljudimport", MediaUrlImportValidation.Normalize(Input with { FileName = "" }).FileName);
    }

    [Theory]
    [InlineData(302, "audio/mpeg")]
    [InlineData(200, "text/html")]
    [InlineData(206, "audio/mpeg")]
    public async Task RedirectsPartialResponsesAndWrongMimeAreRejected(int status, string mime)
    {
        using var response = Response(new byte[8]);
        response.StatusCode = (HttpStatusCode)status;
        response.Content.Headers.ContentType = new(mime);
        await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => DirectAudioTransport.ReadResponseAsync(response, "mp3", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SizeEncodingCancellationAndStagingAreBounded()
    {
        using var response = Response(new byte[20]);
        response.Content.Headers.ContentLength = DirectAudioTransport.MaximumBytes + 1;
        await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => DirectAudioTransport.ReadResponseAsync(response, "mp3", TestContext.Current.CancellationToken));
        response.Content.Headers.ContentLength = 20;
        response.Content.Headers.ContentEncoding.Add("gzip");
        await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => DirectAudioTransport.ReadResponseAsync(response, "mp3", TestContext.Current.CancellationToken));
        response.Content.Headers.ContentEncoding.Clear();
        await using var staged = await DirectAudioTransport.ReadResponseAsync(response, "mp3", TestContext.Current.CancellationToken);
        Assert.Equal(20, staged.Size);
        Assert.Equal(64, staged.Checksum.Length);
        if (OperatingSystem.IsLinux()) Assert.False(File.Exists(staged.Stream.Name));
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DirectAudioTransport.ReadResponseAsync(response, "mp3", cancelled.Token));
    }

    [Fact]
    public async Task UnknownLengthCannotBypassByteLimit()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new OversizeStream()) };
        response.Content.Headers.ContentType = new("audio/mpeg");
        Assert.Null(response.Content.Headers.ContentLength);
        await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => DirectAudioTransport.ReadResponseAsync(response, "mp3", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("mp3", "audio/mpeg", true)]
    [InlineData("m4a", "audio/mp4", true)]
    [InlineData("m4a", "audio/x-m4a", true)]
    [InlineData("m4a", "audio/mpeg", false)]
    [InlineData("mp3", "audio/mp4", false)]
    [InlineData("m4a", "video/mp4", false)]
    [InlineData("m4a", "application/octet-stream", false)]
    public async Task SelectedFormatRequiresMatchingMime(string format, string mime, bool accepted)
    {
        using var response = Response(new byte[20]);
        response.Content.Headers.ContentType = new(mime);
        if (accepted)
        {
            await using var file = await DirectAudioTransport.ReadResponseAsync(response, format, TestContext.Current.CancellationToken);
            Assert.Equal(20, file.Size); // Content admission belongs to Librarr before publication.
        }
        else await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => DirectAudioTransport.ReadResponseAsync(response, format, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("mp3", "https://audio.example/file.m4a")]
    [InlineData("m4a", "https://audio.example/file.mp3")]
    public void MismatchedExtensionCannotBePreviewed(string format, string url) =>
        Assert.Throws<AudiobookAcquisitionException>(() => MediaUrlImportValidation.Normalize(Input with { Format = format, SourceUrl = url }));

    [Theory]
    [InlineData("bb158-direct-audio-v1", false)]
    [InlineData("bb158-direct-audio-v2", true)]
    public async Task CapabilityVersionMustMatchBeforeUpload(string contract, bool supported)
    {
        using var handler = new CapabilityHandler(contract);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://librarr.example/") };
        var importer = new LibrarrDirectAudioImporter(client);
        if (supported) await importer.RequireAvailableAsync(TestContext.Current.CancellationToken);
        else await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => importer.RequireAvailableAsync(TestContext.Current.CancellationToken));
    }
    private sealed class CapabilityHandler(string contract) : HttpMessageHandler
    {
        private static readonly string[] Formats = ["mp3", "m4a"];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = System.Net.Http.Json.JsonContent.Create(new { contract, maximumBytes = DirectAudioTransport.MaximumBytes, formats = Formats })
            });
    }

    private sealed class OversizeStream : Stream
    {
        private long left = DirectAudioTransport.MaximumBytes + 1;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var size = (int)Math.Min(count, left); Array.Clear(buffer, offset, size); left -= size; return size;
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); var size = (int)Math.Min(buffer.Length, left);
            buffer.Span[..size].Clear(); left -= size; return ValueTask.FromResult(size);
        }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Theory]
    [InlineData("10.1234/example")]
    [InlineData(null)]
    public async Task BothFormatsReopenWithExactIndependentIndexIdentityWithOrWithoutDoi(string? doi)
    {
        using var store = new AudiobookAcquisitionStore(Options);
        var source = new FakeSource(); var importer = new FakeImporter();
        using var service = new MediaUrlImportService(store, source, importer, Options, TimeProvider.System);
        var first = await Confirm(store, Input with { Doi = doi }); var second = await Confirm(store, Input with { Doi = doi, FileName = "Chapter 2", SourceUrl = "https://audio.example/second.m4a", Format = "m4a" });
        var placed = await service.StartAsync(first.Id, TestContext.Current.CancellationToken);
        Assert.Equal("indexing", placed.Status); Assert.True(string.IsNullOrEmpty(placed.ItemId));
        var indexed = await service.InspectAsync(first.Id, TestContext.Current.CancellationToken);
        Assert.Equal("completed", indexed.Status); Assert.Equal("item-" + first.Id, indexed.ItemId);
        Assert.Equal("https://abs.example/item/item-" + first.Id, indexed.AudiobookshelfUrl);
        await service.StartAsync(second.Id, TestContext.Current.CancellationToken);
        await service.InspectAsync(second.Id, TestContext.Current.CancellationToken);
        using var reopened = new AudiobookAcquisitionStore(Options);
        var saved = await reopened.ListUrlImportsAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, saved.Count); Assert.All(saved, v =>
        {
            Assert.Equal(doi, v.Doi);
            Assert.Equal("completed", v.Status);
            Assert.Equal(64, v.Checksum!.Length);
            Assert.Equal(834, v.Bytes);
            Assert.Equal("library", v.LibraryId);
            Assert.Equal("123", v.Inode);
            Assert.Equal($"BigBrain Imports/{v.Id}/{v.ProposedFileName}", v.RelativePath);
            if (doi is null) Assert.DoesNotContain("DOI-kopplingen", v.Message);
            else Assert.Contains("DOI-kopplingen", v.Message);
        });
        Assert.Equal(2, saved.Select(v => v.ItemId).Distinct().Count());
        Assert.Equal(2, source.Calls);
        await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => service.StartAsync(first.Id, TestContext.Current.CancellationToken));
        Assert.Equal(2, source.Calls);
    }

    [Theory]
    [InlineData("10.1234/example")]
    [InlineData(null)]
    public async Task LostUploadResponseIsInspectedWithoutRedownloadAfterRestart(string? doi)
    {
        var source = new FakeSource(); var importer = new FakeImporter { LoseResponse = true };
        using var store = new AudiobookAcquisitionStore(Options);
        var draft = await Confirm(store, Input with { Doi = doi });
        using (var service = new MediaUrlImportService(store, source, importer, Options, TimeProvider.System))
            Assert.Equal("uncertain", (await service.StartAsync(draft.Id, TestContext.Current.CancellationToken)).Status);
        using var reopened = new AudiobookAcquisitionStore(Options);
        using var next = new MediaUrlImportService(reopened, source, importer, Options, TimeProvider.System);
        Assert.Equal("completed", (await next.InspectAsync(draft.Id, TestContext.Current.CancellationToken)).Status);
        Assert.Equal(1, source.Calls); Assert.Equal(1, importer.Uploads);
    }

    [Fact]
    public async Task ConcurrentReservationsAndInterruptedDownloadDoNotReplay()
    {
        using var store = new AudiobookAcquisitionStore(Options);
        var draft = await Confirm(store, Input);
        using var other = new AudiobookAcquisitionStore(Options);
        var attempts = await Task.WhenAll(Enumerable.Range(0, 8).Select(async i =>
        {
            try { await (i % 2 == 0 ? store : other).StartUrlImportAsync(draft.Id, Now, TestContext.Current.CancellationToken); return true; }
            catch (AudiobookAcquisitionException) { return false; }
        }));
        Assert.Equal(1, attempts.Count(v => v));
        using var reopened = new AudiobookAcquisitionStore(Options);
        var second = await Confirm(reopened, Input);
        await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => reopened.StartUrlImportAsync(second.Id, Now, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MissingCapabilityCannotFetchEitherFormat()
    {
        using var store = new AudiobookAcquisitionStore(Options);
        var source = new FakeSource(); var importer = new FakeImporter { Available = false };
        using var service = new MediaUrlImportService(store, source, importer, Options, TimeProvider.System);
        var draft = await Confirm(store, Input);
        await Assert.ThrowsAsync<HttpRequestException>(() => service.StartAsync(draft.Id, TestContext.Current.CancellationToken));
        var m4a = await Confirm(store, Input with { SourceUrl = "https://audio.example/file.m4a", Format = "m4a" });
        await Assert.ThrowsAsync<HttpRequestException>(() => service.StartAsync(m4a.Id, TestContext.Current.CancellationToken));
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public void TitleOrDoiAloneCannotCompleteImport()
    {
        var d = new MediaUrlImportDraft("id", "https://audio.example/file.mp3", "Chapter", "10.1234/example", "mp3", "Chapter -- id.mp3", Now, Now.AddMinutes(15), "importing", Now, "") { Checksum = "abc", Bytes = 10 };
        var r = new DirectAudioResult("id", "abc", "mp3", "Chapter", 10, "completed", "wrong/file.mp3", "123", "library", "item");
        Assert.Throws<AudiobookAcquisitionException>(() => MediaUrlImportService.Apply(d, r));
        Assert.Throws<AudiobookAcquisitionException>(() => MediaUrlImportService.Apply(d, r with { RelativePath = "BigBrain Imports/id/Chapter -- id.mp3", Sha256 = "different" }));
    }

    private static Task<MediaUrlImportDraft> Confirm(AudiobookAcquisitionStore store, MediaUrlImportInput input) => ConfirmCore(store, input);
    private static async Task<MediaUrlImportDraft> ConfirmCore(AudiobookAcquisitionStore store, MediaUrlImportInput input)
    {
        var d = await store.PreviewUrlImportAsync(input, Now.AddMinutes(-1), TestContext.Current.CancellationToken);
        return await store.ConfirmUrlImportAsync(d.Id, new(true, true), Now, TestContext.Current.CancellationToken);
    }
    private static HttpResponseMessage Response(byte[] bytes)
    {
        var r = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
        r.Content.Headers.ContentType = new("audio/mpeg"); return r;
    }
    private sealed class FakeSource : IDirectAudioTransport
    {
        public int Calls { get; private set; }
        public async Task<DownloadedAudio> FetchAsync(MediaUrlImportDraft draft, CancellationToken token)
        { Calls++; using var r = Response(new byte[834]); r.Content.Headers.ContentType = new(draft.Format == "m4a" ? "audio/mp4" : "audio/mpeg"); return await DirectAudioTransport.ReadResponseAsync(r, draft.Format, token); }
    }
    private sealed class FakeImporter : IDirectAudioImporter
    {
        public bool Available { get; set; } = true;
        public bool LoseResponse { get; set; }
        public int Uploads { get; private set; }
        private readonly Dictionary<string, DirectAudioResult> results = [];
        public Task RequireAvailableAsync(CancellationToken token) => Available ? Task.CompletedTask : Task.FromException(new HttpRequestException("fixture unavailable"));
        public Task<DirectAudioResult> ImportAsync(MediaUrlImportDraft d, DownloadedAudio file, CancellationToken token)
        {
            Uploads++;
            var r = new DirectAudioResult(d.Id, file.Checksum, d.Format, d.FileName, file.Size, "indexing", $"BigBrain Imports/{d.Id}/{d.ProposedFileName}", "123", "library", "");
            results.Add(d.Id, r);
            return LoseResponse ? Task.FromException<DirectAudioResult>(new HttpRequestException("lost fixture response")) : Task.FromResult(r);
        }
        public Task<DirectAudioResult> InspectAsync(string id, CancellationToken token) => Task.FromResult(results[id] with { Status = "completed", ItemId = "item-" + id });
    }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
