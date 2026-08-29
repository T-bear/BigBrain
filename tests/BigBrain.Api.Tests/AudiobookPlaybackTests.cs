using System.Net;
using System.Text;
using System.Text.Json;
using BigBrain.Api.Media;
using Microsoft.AspNetCore.Http;

namespace BigBrain.Api.Tests;

public sealed class AudiobookPlaybackTests
{
    [Fact]
    public async Task VerificationRequiresASeparateRestrictedProgressIdentity()
    {
        var service = Service([Json("{\"isRoot\":false,\"isAdmin\":false}"), Json("{\"mediaProgress\":[{\"libraryItemId\":\"book\"}]}")]);
        var result = await service.VerifyAsync(TestContext.Current.CancellationToken);
        Assert.Equal("configuredHealthy", result.State);
        Assert.True(result.SeparateIdentity);
        Assert.True(result.HasProgress);
        Assert.DoesNotContain("key", JsonSerializer.Serialize(result), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartReturnsOnlyOpaqueBoundedTrackContract()
    {
        var service = Service([
            Json("{\"isRoot\":false}"), Json("{\"mediaProgress\":[{}]}"),
            Json("{\"id\":\"upstream-session\",\"libraryItemId\":\"book\",\"duration\":100,\"currentTime\":25,\"audioTracks\":[{\"index\":1,\"startOffset\":0,\"duration\":100,\"mimeType\":\"audio/mp4\"}]}")]);
        var result = await service.StartAsync("book", TestContext.Current.CancellationToken);
        var json = JsonSerializer.Serialize(result);
        Assert.Equal(48, result.Id.Length);
        Assert.DoesNotContain("upstream-session", json);
        Assert.DoesNotContain("playback-key", json);
        Assert.StartsWith("/api/v1/modules/media/audiobooks/playback/sessions/", result.Tracks[0].StreamUrl);
    }

    [Fact]
    public async Task SessionTrackMismatchAndInvalidProgressFailClosed()
    {
        var service = Service([
            Json("{\"isRoot\":false}"), Json("{\"mediaProgress\":[{}]}"),
            Json("{\"id\":\"upstream-session\",\"libraryItemId\":\"book\",\"duration\":100,\"audioTracks\":[{\"index\":1,\"duration\":100}]}")]);
        var session = await service.StartAsync("book", TestContext.Current.CancellationToken);
        var context = new DefaultHttpContext();
        var mismatch = await Assert.ThrowsAsync<AudiobookPlaybackException>(() => service.StreamAsync(session.Id, 2, context, TestContext.Current.CancellationToken));
        Assert.Equal("trackMismatch", mismatch.Code);
        var invalid = await Assert.ThrowsAsync<AudiobookPlaybackException>(() => service.SyncAsync(session.Id, new(1010, 100, 0), false, TestContext.Current.CancellationToken));
        Assert.Equal("invalidProgress", invalid.Code);
    }

    [Fact]
    public async Task RangeIsSingleAndBoundedAndDoesNotAcceptAUrl()
    {
        var handler = new QueueHandler([
            Json("{\"isRoot\":false}"), Json("{\"mediaProgress\":[{}]}"),
            Json("{\"id\":\"upstream-session\",\"libraryItemId\":\"book\",\"duration\":100,\"audioTracks\":[{\"index\":1,\"duration\":100}]}") ,
            new(HttpStatusCode.PartialContent) { Content = new ByteArrayContent([1, 2, 3]) }
        ]);
        var service = Service(handler);
        var session = await service.StartAsync("book", TestContext.Current.CancellationToken);
        var context = new DefaultHttpContext(); context.Response.Body = new MemoryStream(); context.Request.Headers.Range = "bytes=0-";
        await service.StreamAsync(session.Id, 1, context, TestContext.Current.CancellationToken);
        Assert.Equal(206, context.Response.StatusCode);
        Assert.Equal(8 * 1024 * 1024 - 1, handler.LastRequest!.Headers.Range!.Ranges.Single().To);
        Assert.DoesNotContain("http://", JsonSerializer.Serialize(session), StringComparison.OrdinalIgnoreCase);
    }

    private static AudiobookPlaybackService Service(IEnumerable<HttpResponseMessage> responses) => Service(new QueueHandler(responses));
    private static AudiobookPlaybackService Service(QueueHandler handler)
    {
        var options = new MediaOptions { Audiobookshelf = new() { ApiKey = "integration-key", PlaybackApiKey = "playback-key", LibraryId = "library" } };
        return new(new Factory(handler), options, TimeProvider.System);
    }
    private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK) { Content = new StringContent(value, Encoding.UTF8, "application/json") };
    private sealed class Factory(QueueHandler handler) : IHttpClientFactory { public HttpClient CreateClient(string name) => new(handler, false) { BaseAddress = new("http://audiobookshelf/") }; }
    private sealed class QueueHandler(IEnumerable<HttpResponseMessage> responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> queue = new(responses); public HttpRequestMessage? LastRequest { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { LastRequest = request; return Task.FromResult(queue.Dequeue()); }
    }
}
