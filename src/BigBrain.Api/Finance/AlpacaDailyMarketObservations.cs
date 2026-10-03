using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Finance;

// Transport credentials remain separate from the disabled-by-default observation runtime.
internal sealed class AlpacaDailyObservationOptions
{
    internal const string Section = "Finance:AlpacaDailyObservation";
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 15;
    internal static AlpacaDailyObservationOptions FromConfiguration(IConfiguration configuration) =>
        configuration.GetSection(Section).Get<AlpacaDailyObservationOptions>() ?? new();
}

internal sealed class AlpacaDailyMarketObservations : IMarketObservationSource, IDisposable
{
    internal const int MaximumResponseBytes = 65536;
    private readonly HttpClient _client;
    private readonly bool _enabled;
    private readonly string _key;
    private readonly string _secret;
    private readonly DateOnly _day;
    private readonly TimeSpan _timeout;
    public MarketDataProvider Provider { get; } = new("Alpaca");
    public ProviderDataset Dataset { get; } = new(DailyMarketEvidence.Dataset);
    public string AdapterVersion => "alpaca-daily-observation-v1";
    public string ContractVersion => DailyMarketEvidence.Contract;
    public MarketObservationOrigin Origin { get; }

    internal AlpacaDailyMarketObservations(AlpacaDailyObservationOptions options, DateOnly sourceDay)
        : this(options, sourceDay, new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            ConnectTimeout = TimeSpan.FromSeconds(10),
            MaxResponseHeadersLength = 16
        },
            MarketObservationOrigin.AcquiredProviderDaily)
    { }

    // Transport doubles cannot label fixtures as actually acquired provider evidence.
    internal AlpacaDailyMarketObservations(AlpacaDailyObservationOptions options, DateOnly sourceDay, HttpMessageHandler fixtureTransport)
        : this(options, sourceDay, fixtureTransport, MarketObservationOrigin.DeterministicFixture) { }

    private AlpacaDailyMarketObservations(AlpacaDailyObservationOptions options, DateOnly sourceDay, HttpMessageHandler transport, MarketObservationOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.TimeoutSeconds is < 1 or > 30) throw new ArgumentOutOfRangeException(nameof(options));
        _enabled = options.Enabled; _key = options.ApiKey; _secret = options.ApiSecret; _day = sourceDay; _timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        _client = new HttpClient(transport, true) { Timeout = Timeout.InfiniteTimeSpan }; Origin = origin;
    }

    public async Task<ProviderObservation> ReadAsync(ProviderInstrumentMapping mapping, CancellationToken cancellationToken)
    {
        if (!_enabled || !ValidCredential(_key) || !ValidCredential(_secret))
            throw new InvalidOperationException("Alpaca daily observation transport is disabled or not configured.");
        if (mapping.Provider != Provider || mapping.ProviderDataset != Dataset ||
            mapping.Mic is not ("XNAS" or "XNYS" or "ARCX") || mapping.ProviderReference.Length is < 1 or > 16 ||
            mapping.ProviderReference.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '.' or '-')))
            throw new InvalidDataException("Unsupported Alpaca daily observation mapping.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(_timeout); var token = deadline.Token;
        // Fixed HTTPS authority/path. No caller URL, credentials in query, redirects or retries.
        using var request = new HttpRequestMessage(HttpMethod.Get,
            new Uri("https://data.alpaca.markets/v2/stocks/" + Uri.EscapeDataString(mapping.ProviderReference) +
                "/bars?timeframe=1Day&feed=iex&adjustment=raw&asof=-&limit=2&start=" +
                Uri.EscapeDataString(DailyMarketEvidence.DayStart(_day).ToString("O", CultureInfo.InvariantCulture)) +
                "&end=" + Uri.EscapeDataString(DailyMarketEvidence.DayStart(_day.AddDays(1)).AddTicks(-1).ToString("O", CultureInfo.InvariantCulture))));
        request.Headers.Add("APCA-API-KEY-ID", _key);
        request.Headers.Add("APCA-API-SECRET-KEY", _secret);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        try
        {
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength is > MaximumResponseBytes ||
                response.Content.Headers.ContentType?.MediaType != "application/json" || response.Content.Headers.ContentEncoding.Count != 0)
                throw new InvalidDataException("Alpaca daily response status or framing rejected.");
            await using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            var buffer = new byte[MaximumResponseBytes + 1]; var count = 0;
            while (count < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(count), token).ConfigureAwait(false);
                if (read == 0) break;
                count += read;
            }
            if (count > MaximumResponseBytes || response.Content.Headers.ContentLength is { } length && count != length)
                throw new InvalidDataException("Alpaca daily response size rejected.");
            token.ThrowIfCancellationRequested();
            return Parse(buffer.AsMemory(0, count), mapping, _day);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new TimeoutException("Alpaca daily observation deadline exceeded."); }
        catch (IOException) { throw new IOException("Alpaca daily observation receive failed."); }
        catch (HttpRequestException) { throw new IOException("Alpaca daily observation transport failed."); }
        catch (JsonException) { throw new InvalidDataException("Alpaca daily observation JSON rejected."); }
        catch (FormatException) { throw new InvalidDataException("Alpaca daily observation value rejected."); }
        catch (ArgumentException) { throw new InvalidDataException("Alpaca daily observation value rejected."); }
    }

    internal static bool ValidCredential(string? value) => value is not null && value.Length is > 0 and <= 128 && value.All(char.IsAsciiLetterOrDigit);

    private static ProviderObservation Parse(ReadOnlyMemory<byte> bytes, ProviderInstrumentMapping mapping, DateOnly day)
    {
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        Unique(root);
        if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("code", out _) ||
            !root.TryGetProperty("symbol", out var symbol) || symbol.GetString() != mapping.ProviderReference ||
            !root.TryGetProperty("next_page_token", out var next) || next.ValueKind != JsonValueKind.Null ||
            !root.TryGetProperty("bars", out var bars) || bars.ValueKind != JsonValueKind.Array || bars.GetArrayLength() != 1)
            throw new InvalidDataException("Alpaca daily scope or incomplete response rejected.");
        var bar = bars[0];
        if (!bar.TryGetProperty("t", out var timestamp) || timestamp.ValueKind != JsonValueKind.String ||
            !DateTimeOffset.TryParse(timestamp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var at) ||
            at.Offset != TimeSpan.Zero || at != DailyMarketEvidence.DayStart(day))
            throw new InvalidDataException("Alpaca daily timestamp rejected.");
        return new(mapping.ProviderReference, mapping.Mic, new("USD"), Timeframe.OneDay, at, null,
            Number(bar, "o"), Number(bar, "h"), Number(bar, "l"), Number(bar, "c"), Number(bar, "v"),
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes.Span)),
            new(DailyMarketEvidence.Eligibility, day, "America/New_York", DailyMarketEvidence.SourceContract, DailyMarketEvidence.SymbolPolicy));
    }
    private static decimal Number(JsonElement root, string field)
    {
        if (!root.TryGetProperty(field, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var result))
            throw new InvalidDataException("Alpaca daily numeric value rejected.");
        return result;
    }
    private static void Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in value.EnumerateObject())
            { if (!keys.Add(p.Name)) throw new InvalidDataException("Alpaca duplicate property rejected."); Unique(p.Value); }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) Unique(item);
    }
    public void Dispose() => _client.Dispose();
}
