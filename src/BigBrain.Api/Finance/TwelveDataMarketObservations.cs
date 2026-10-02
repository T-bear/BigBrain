using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Finance;

// Explicit construction only; no API endpoint, DI registration, scheduler or default entitlement.
internal sealed class TwelveDataObservationOptions
{
    internal const string Section = "Finance:TwelveDataObservation";
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 15;
    internal static TwelveDataObservationOptions FromConfiguration(IConfiguration configuration) =>
        configuration.GetSection(Section).Get<TwelveDataObservationOptions>() ?? new();
}

internal sealed class TwelveDataMarketObservations : IMarketObservationSource, IDisposable
{
    internal const int MaximumResponseBytes = 65536;
    private readonly HttpClient _client;
    private readonly bool _enabled;
    private readonly string _key;
    private readonly TimeSpan _timeout;
    public MarketDataProvider Provider { get; } = new("TwelveData");
    public ProviderDataset Dataset { get; } = new("quote-1min-regular-raw");
    public string AdapterVersion => "twelvedata-quote-observation-v1";
    public MarketObservationOrigin Origin { get; }

    internal TwelveDataMarketObservations(TwelveDataObservationOptions options)
        : this(options, new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            ConnectTimeout = TimeSpan.FromSeconds(10),
            MaxResponseHeadersLength = 16
        },
            MarketObservationOrigin.AcquiredProviderSnapshot)
    { }

    // Transport doubles cannot label fixtures as actually acquired provider evidence.
    internal TwelveDataMarketObservations(TwelveDataObservationOptions options, HttpMessageHandler fixtureTransport)
        : this(options, fixtureTransport, MarketObservationOrigin.DeterministicFixture) { }

    private TwelveDataMarketObservations(TwelveDataObservationOptions options, HttpMessageHandler transport, MarketObservationOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.TimeoutSeconds is < 1 or > 30) throw new ArgumentOutOfRangeException(nameof(options));
        _enabled = options.Enabled; _key = options.ApiKey; _timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        _client = new HttpClient(transport, true) { Timeout = Timeout.InfiniteTimeSpan }; Origin = origin;
    }

    public async Task<ProviderObservation> ReadAsync(ProviderInstrumentMapping mapping, CancellationToken cancellationToken)
    {
        if (!_enabled || string.IsNullOrWhiteSpace(_key) || _key.Length > 128 || !_key.All(char.IsAsciiLetterOrDigit))
            throw new InvalidOperationException("Twelve Data observation transport is disabled or not configured.");
        if (mapping.Provider != Provider || mapping.ProviderDataset != Dataset ||
            mapping.Mic is not ("XNAS" or "XNYS" or "ARCX") || mapping.ProviderReference.Length is < 1 or > 16 ||
            mapping.ProviderReference.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '.' or '-')))
            throw new InvalidDataException("Unsupported Twelve Data observation mapping.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(_timeout); var token = deadline.Token;
        // Fixed HTTPS authority/path. No caller URL, credentials in query, redirects or retries.
        using var request = new HttpRequestMessage(HttpMethod.Get,
            new Uri("https://api.twelvedata.com/quote?symbol=" + Uri.EscapeDataString(mapping.ProviderReference) +
                "&mic_code=" + mapping.Mic + "&interval=1min&timezone=UTC&prepost=false&eod=false&format=JSON"));
        request.Headers.Authorization = new AuthenticationHeaderValue("apikey", _key);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        try
        {
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength is > MaximumResponseBytes ||
                response.Content.Headers.ContentType?.MediaType != "application/json" || response.Content.Headers.ContentEncoding.Count != 0)
                throw new InvalidDataException("Twelve Data response status or framing rejected.");
            await using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            var buffer = new byte[MaximumResponseBytes + 1]; var count = 0;
            while (count < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(count), token).ConfigureAwait(false);
                if (read == 0) break;
                count += read;
            }
            if (count > MaximumResponseBytes || response.Content.Headers.ContentLength is { } length && count != length)
                throw new InvalidDataException("Twelve Data response size rejected.");
            token.ThrowIfCancellationRequested();
            return Parse(buffer.AsMemory(0, count), mapping);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new TimeoutException("Twelve Data observation deadline exceeded."); }
        catch (IOException) { throw new IOException("Twelve Data observation receive failed."); }
        catch (HttpRequestException) { throw new IOException("Twelve Data observation transport failed."); }
        catch (JsonException) { throw new InvalidDataException("Twelve Data observation JSON rejected."); }
        catch (FormatException) { throw new InvalidDataException("Twelve Data observation value rejected."); }
        catch (ArgumentException) { throw new InvalidDataException("Twelve Data observation value rejected."); }
    }

    private static ProviderObservation Parse(ReadOnlyMemory<byte> bytes, ProviderInstrumentMapping mapping)
    {
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        Unique(root);
        if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("code", out _) || root.TryGetProperty("status", out var status) && status.GetString() != "ok")
            throw new InvalidDataException("Twelve Data observation error envelope rejected.");
        var symbol = Text(root, "symbol"); var mic = Text(root, "mic_code"); var currency = Text(root, "currency");
        if (symbol != mapping.ProviderReference || mic != mapping.Mic || Text(root, "exchange") != mapping.Venue.Code || currency != "USD" ||
            root.TryGetProperty("interval", out var interval) && interval.GetString() != "1min" ||
            root.TryGetProperty("is_extended_hours", out var extended) && extended.ValueKind != JsonValueKind.False)
            throw new InvalidDataException("Twelve Data observation scope rejected.");
        if (!root.TryGetProperty("timestamp", out var timestamp) || !timestamp.TryGetInt64(out var seconds) || seconds <= 0 || seconds % 60 != 0 ||
            !root.TryGetProperty("last_quote_at", out var last) || !last.TryGetInt64(out var lastSeconds) || lastSeconds != seconds ||
            !DateTimeOffset.TryParseExact(Text(root, "datetime"), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var eventTime) || eventTime.ToUnixTimeSeconds() != seconds)
            throw new InvalidDataException("Twelve Data observation timestamp rejected.");
        return new(symbol, mic, new(currency), Timeframe.OneMinute, eventTime, null,
            Number(root, "open"), Number(root, "high"), Number(root, "low"), Number(root, "close"), Number(root, "volume"),
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes.Span)));
    }

    private static string Text(JsonElement root, string field)
    {
        if (!root.TryGetProperty(field, out var value) || value.ValueKind != JsonValueKind.String ||
            value.GetString() is not { Length: > 0 and <= 128 } result)
            throw new InvalidDataException("Twelve Data observation required field rejected.");
        return result;
    }
    private static decimal Number(JsonElement root, string field)
    {
        var text = Text(root, field);
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            throw new InvalidDataException("Twelve Data observation number rejected.");
        return value;
    }
    private static void Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in value.EnumerateObject())
            { if (!keys.Add(p.Name)) throw new InvalidDataException("Twelve Data duplicate property rejected."); Unique(p.Value); }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            throw new InvalidDataException("Twelve Data single observation required.");
    }
    public void Dispose() => _client.Dispose();
}
