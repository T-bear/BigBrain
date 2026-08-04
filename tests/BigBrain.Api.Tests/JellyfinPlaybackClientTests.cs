using System.Net;
using BigBrain.Api.Media;

namespace BigBrain.Api.Tests;

public sealed class JellyfinPlaybackClientTests
{
    [Theory]
    [InlineData(null, "playCommand=PlayNow&itemIds=episode-id")]
    [InlineData(123L, "playCommand=PlayNow&itemIds=episode-id&startPositionTicks=123")]
    public async Task PlayNowUsesJellyfin101111QueryContract(long? ticks, string expectedQuery)
    {
        HttpRequestMessage? captured = null;
        var client = Client(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        await ((IJellyfinPlaybackClient)client).PlayNowAsync("raw-session", "episode-id", ticks, TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("/Sessions/raw-session/Playing", captured.RequestUri!.AbsolutePath);
        Assert.Equal(expectedQuery, captured.RequestUri.Query.TrimStart('?'));
        Assert.Null(captured.Content);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "playbackRejected")]
    [InlineData(HttpStatusCode.Unauthorized, "playbackAuthenticationFailure")]
    [InlineData(HttpStatusCode.Forbidden, "playbackAuthenticationFailure")]
    [InlineData(HttpStatusCode.NotFound, "playbackTargetUnavailable")]
    public async Task PlayNowMapsUpstreamStatusWithoutRawBody(HttpStatusCode status, string code)
    {
        var client = Client(_ => new HttpResponseMessage(status) { Content = new StringContent("secret raw identity") });

        var error = await Assert.ThrowsAsync<JellyfinPlaybackException>(() =>
            ((IJellyfinPlaybackClient)client).PlayNowAsync("raw-session", "episode-id", null, TestContext.Current.CancellationToken));

        Assert.Equal(code, error.Code);
        Assert.DoesNotContain("secret", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static JellyfinClient Client(Func<HttpRequestMessage, HttpResponseMessage> response) =>
        new(new HttpClient(new Handler(response)) { BaseAddress = new Uri("http://jellyfin/") }, new MediaOptions
        {
            Jellyfin = new MediaApiKeyOptions("http://jellyfin") { ApiKey = "fake", UserId = "user" },
            SmartShuffle = new SmartShuffleOptions { Enabled = true }
        });

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response(request));
    }
}
