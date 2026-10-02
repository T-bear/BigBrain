using System.Net;
using System.Text;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using Microsoft.Extensions.Configuration;

namespace BigBrain.Api.Tests;

public sealed class TwelveDataMarketObservationTests
{
    private const string Fixture = """
        {"symbol":"AAPL","exchange":"NASDAQ","mic_code":"XNAS","currency":"USD",
        "datetime":"2026-10-01 14:59:00","timestamp":1790866740,"last_quote_at":1790866740,
        "open":"100.00000","high":"102.00000","low":"99.00000","close":"101.00000","volume":"10000"}
        """;
    private static readonly ProviderInstrumentMapping Mapping = new(FinanceMarketObservationTests.Instrument.Id,
        new("TwelveData"), new("quote-1min-regular-raw"), "AAPL", new("NASDAQ", "NASDAQ"), "XNAS", new(2000, 1, 1), null, new("fixture:mapping"));
    private static TwelveDataObservationOptions Options => new() { Enabled = true, ApiKey = "FixtureOnlyNotACredential" };

    [Fact]
    public async Task FixedBoundedRequestNormalizesAndPersistsThroughFinance()
    {
        var handler = new Transport((request, _) =>
        {
            Assert.Equal("https", request.RequestUri!.Scheme); Assert.Equal("api.twelvedata.com", request.RequestUri.Host);
            Assert.Equal("/quote", request.RequestUri.AbsolutePath); Assert.Contains("interval=1min&timezone=UTC&prepost=false", request.RequestUri.Query, StringComparison.Ordinal);
            Assert.DoesNotContain("apikey", request.RequestUri.Query, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("apikey", request.Headers.Authorization!.Scheme);
            return Task.FromResult(Response(Fixture));
        });
        using var adapter = new TwelveDataMarketObservations(Options, handler);
        using var db = new FinanceMarketObservationTests.Database();
        var clock = new FinanceMarketObservationTests.Clock(new(2026, 10, 1, 15, 0, 0, TimeSpan.Zero));
        var receipt = await db.Memory().AcquireMarketObservationAsync(adapter, FinanceMarketObservationTests.Instrument, Mapping,
            FinanceMarketObservationTests.Policy(mapping: Mapping), clock, CancellationToken.None);
        Assert.Equal(1, handler.Calls);
        Assert.Equal(Timeframe.OneMinute, receipt.Value.Interval);
        Assert.Null(receipt.Value.ProviderAvailableAtUtc);
        Assert.Equal(MarketObservationOrigin.DeterministicFixture, receipt.Origin);
        Assert.Equal(receipt, Assert.Single(db.Memory().MarketKnowledgeAt(clock.Now, clock).Observations));
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("duplicate")]
    [InlineData("symbol")]
    [InlineData("mic")]
    [InlineData("currency")]
    [InlineData("interval")]
    [InlineData("timestamp")]
    [InlineData("timezone")]
    [InlineData("last")]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("number")]
    [InlineData("batch")]
    [InlineData("error")]
    [InlineData("future")]
    [InlineData("ohlc")]
    [InlineData("encoding")]
    public async Task InvalidResponsesCreateNoReceiptAndAreNotRetried(string invalid)
    {
        var json = invalid switch
        {
            "malformed" => "{",
            "duplicate" => Fixture.Replace("{", "{\"symbol\":\"MSFT\",", StringComparison.Ordinal),
            "symbol" => Fixture.Replace("AAPL", "MSFT", StringComparison.Ordinal),
            "mic" => Fixture.Replace("XNAS", "XNYS", StringComparison.Ordinal),
            "currency" => Fixture.Replace("USD", "SEK", StringComparison.Ordinal),
            "interval" => Fixture.Replace("{", "{\"interval\":\"1day\",", StringComparison.Ordinal),
            "timestamp" => Fixture.Replace("1790866740", "1790866741", StringComparison.Ordinal),
            "timezone" => Fixture.Replace("14:59:00", "10:59:00", StringComparison.Ordinal),
            "last" => Fixture.Replace("\"last_quote_at\":1790866740", "\"last_quote_at\":1790866680", StringComparison.Ordinal),
            "missing" => Fixture.Replace("\"volume\":\"10000\"", "\"unused\":\"10000\"", StringComparison.Ordinal),
            "null" => Fixture.Replace("\"10000\"", "null", StringComparison.Ordinal),
            "number" => Fixture.Replace("10000", "1e4", StringComparison.Ordinal),
            "batch" => "[" + Fixture + "]",
            "error" => "{\"code\":429,\"status\":\"error\",\"message\":\"untrusted-provider-text\"}",
            "future" => Fixture.Replace("14:59:00", "15:01:00", StringComparison.Ordinal).Replace("1790866740", "1790866860", StringComparison.Ordinal),
            "ohlc" => Fixture.Replace("101.00000", "103.00000", StringComparison.Ordinal),
            _ => Fixture
        };
        var handler = new Transport((_, _) => Task.FromResult(invalid == "encoding"
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[] { 0xff, 0xfe }) { Headers = { ContentType = new("application/json") } } }
            : Response(json)));
        using var adapter = new TwelveDataMarketObservations(Options, handler);
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(new(2026, 10, 1, 15, 0, 0, TimeSpan.Zero));
        var error = await Assert.ThrowsAnyAsync<Exception>(() => db.Memory().AcquireMarketObservationAsync(adapter,
            FinanceMarketObservationTests.Instrument, Mapping, FinanceMarketObservationTests.Policy(mapping: Mapping), clock, CancellationToken.None));
        Assert.DoesNotContain("untrusted-provider-text", error.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(Options.ApiKey, error.ToString(), StringComparison.Ordinal);
        Assert.Equal(1, handler.Calls); Assert.Empty(db.Memory().MarketKnowledgeAt(clock.Now, clock).Observations);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Redirect)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task HttpFailureIsSanitizedWithoutRetry(HttpStatusCode status)
    {
        var handler = new Transport((_, _) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent("untrusted-body") }));
        using var adapter = new TwelveDataMarketObservations(Options, handler);
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => adapter.ReadAsync(Mapping, CancellationToken.None));
        Assert.DoesNotContain("untrusted-body", error.Message, StringComparison.Ordinal);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task DisabledAndMissingCredentialNeverUseTransport()
    {
        var handler = new Transport((_, _) => throw new InvalidOperationException("must not run"));
        var options = TwelveDataObservationOptions.FromConfiguration(new ConfigurationBuilder().Build());
        Assert.False(options.Enabled);
        using var disabled = new TwelveDataMarketObservations(options, handler);
        await Assert.ThrowsAsync<InvalidOperationException>(() => disabled.ReadAsync(Mapping, CancellationToken.None));
        using var missing = new TwelveDataMarketObservations(new() { Enabled = true }, handler);
        await Assert.ThrowsAsync<InvalidOperationException>(() => missing.ReadAsync(Mapping, CancellationToken.None));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task ReceiveLimitStopsUnknownLengthBeforeUnboundedAllocation()
    {
        using var stream = new EndlessStream();
        var handler = new Transport((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StreamContent(stream) { Headers = { ContentType = new("application/json") } } }));
        using var adapter = new TwelveDataMarketObservations(Options, handler);
        await Assert.ThrowsAsync<InvalidDataException>(() => adapter.ReadAsync(Mapping, CancellationToken.None));
        Assert.Equal(TwelveDataMarketObservations.MaximumResponseBytes + 1, stream.Bytes);
    }

    [Fact]
    public async Task TimeoutAndCancellationAreBoundedWithoutRetry()
    {
        var handler = new Transport(async (_, token) => { await Task.Delay(Timeout.InfiniteTimeSpan, token); throw new InvalidOperationException(); });
        using var adapter = new TwelveDataMarketObservations(new() { Enabled = true, ApiKey = Options.ApiKey, TimeoutSeconds = 1 }, handler);
        await Assert.ThrowsAsync<TimeoutException>(() => adapter.ReadAsync(Mapping, CancellationToken.None));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => adapter.ReadAsync(Mapping, cancel.Token));
        Assert.InRange(handler.Calls, 1, 2);
    }

    private static HttpResponseMessage Response(string json) => new(HttpStatusCode.OK)
    { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    private sealed class Transport(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        internal int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { Calls++; return send(request, cancellationToken); }
    }
    private sealed class EndlessStream : Stream
    {
        internal int Bytes { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) { buffer.AsSpan(offset, count).Fill((byte)' '); Bytes += count; return count; }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); buffer.Span.Fill((byte)' '); Bytes += buffer.Length; return ValueTask.FromResult(buffer.Length); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
