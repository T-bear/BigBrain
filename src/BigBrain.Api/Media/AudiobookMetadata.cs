using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BigBrain.Api.Media;

public static class AudiobookMetadataInputKinds
{
    public const string Isbn10 = "isbn10";
    public const string Isbn13 = "isbn13";
    public const string ProbableAsin = "probableAsin";
    public const string FreeText = "freeText";
}

public sealed record AudiobookMetadataQuery(string Original, string Normalized, string Kind);
public sealed record AudiobookMetadataWork(
    string WorkId,
    IReadOnlyList<string> EditionIds,
    string CanonicalTitle,
    IReadOnlyList<string> AlternateTitles,
    IReadOnlyList<string> Authors,
    string? Series,
    string? SeriesNumber,
    IReadOnlyList<string> Narrators,
    string? Isbn10,
    string? Isbn13,
    string? Asin,
    string Language,
    int? PublicationYear,
    string? CoverUrl,
    string Provider);
public sealed record AudiobookMetadataResolution(
    AudiobookMetadataQuery Query,
    string State,
    IReadOnlyList<AudiobookMetadataWork> Works,
    bool NarratorSearchSupported,
    string? Message);
public sealed record AudiobookDiscoverySeed(string Query, string? Author, string? MetadataWorkId, string MatchEvidence);
public sealed record AudiobookUniversalSearchResult(
    IReadOnlyList<AudiobookItem> Library,
    AudiobookMetadataResolution Metadata,
    IReadOnlyList<AudiobookAcquisitionCandidate> Discovery,
    AudiobookAcquisitionProviderStatus Acquisition);

public interface IAudiobookMetadataProvider
{
    Task<IReadOnlyList<AudiobookMetadataWork>> ResolveAsync(AudiobookMetadataQuery query, CancellationToken token);
    Task<(byte[] Bytes, string ContentType)?> GetCoverAsync(string coverId, CancellationToken token);
}

public static class AudiobookMetadataInput
{
    private static readonly Regex Separators = new(@"[-\s]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private static readonly Regex Asin = new(@"^[A-Z0-9]{10}$", RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50));

    public static AudiobookMetadataQuery Classify(string input)
    {
        var original = input.Trim();
        var compact = Separators.Replace(original, string.Empty).ToUpperInvariant();
        if (IsValidIsbn13(compact)) return new(original, compact, AudiobookMetadataInputKinds.Isbn13);
        if (IsValidIsbn10(compact)) return new(original, compact, AudiobookMetadataInputKinds.Isbn10);
        if (Asin.IsMatch(compact) && compact.Any(char.IsLetter)) return new(original, compact, AudiobookMetadataInputKinds.ProbableAsin);
        return new(original, NormalizeText(original), AudiobookMetadataInputKinds.FreeText);
    }

    public static bool IsValidIsbn10(string value)
    {
        if (value.Length != 10 || value[..9].Any(c => !char.IsDigit(c)) || !(char.IsDigit(value[9]) || value[9] == 'X')) return false;
        var sum = 0;
        for (var index = 0; index < 10; index++)
        {
            var digit = index == 9 && value[index] == 'X' ? 10 : value[index] - '0';
            sum += (10 - index) * digit;
        }
        return sum % 11 == 0;
    }

    public static bool IsValidIsbn13(string value)
    {
        if (value.Length != 13 || value.Any(c => !char.IsDigit(c))) return false;
        var sum = 0;
        for (var index = 0; index < 12; index++) sum += (value[index] - '0') * (index % 2 == 0 ? 1 : 3);
        return (10 - sum % 10) % 10 == value[12] - '0';
    }

    internal static string NormalizeText(string value) =>
        string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

public sealed class OpenLibraryAudiobookMetadataProvider(HttpClient http, MediaOptions options) : IAudiobookMetadataProvider
{
    private const int MaximumResponseBytes = 1_000_000;
    private const int MaximumCoverBytes = 2_000_000;

    public async Task<IReadOnlyList<AudiobookMetadataWork>> ResolveAsync(AudiobookMetadataQuery query, CancellationToken token)
    {
        var parameter = query.Kind is AudiobookMetadataInputKinds.Isbn10 or AudiobookMetadataInputKinds.Isbn13 ? "isbn" : "q";
        var fields = "key,edition_key,title,alternative_title,author_name,first_publish_year,isbn,language,series,cover_i";
        var uri = $"search.json?{parameter}={Uri.EscapeDataString(query.Normalized)}&fields={Uri.EscapeDataString(fields)}&limit={options.OpenLibrary.ResultLimit}";
        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        using var document = await ParseBoundedAsync(response, MaximumResponseBytes, token);
        if (!document.RootElement.TryGetProperty("docs", out var docs) || docs.ValueKind != JsonValueKind.Array) return [];
        return docs.EnumerateArray().Take(options.OpenLibrary.ResultLimit).Select(Map).Where(value => value is not null).Cast<AudiobookMetadataWork>().ToArray();
    }

    public async Task<(byte[] Bytes, string ContentType)?> GetCoverAsync(string coverId, CancellationToken token)
    {
        if (coverId.Length is < 1 or > 20 || coverId.Any(c => !char.IsDigit(c))) return null;
        using var response = await http.GetAsync($"https://covers.openlibrary.org/b/id/{coverId}-M.jpg", HttpCompletionOption.ResponseHeadersRead, token);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var bytes = await ReadBoundedAsync(response, MaximumCoverBytes, token);
        return (bytes, response.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
    }

    private static AudiobookMetadataWork? Map(JsonElement item)
    {
        var title = Text(item, "title");
        var key = Text(item, "key")?.Trim('/').Split('/').LastOrDefault();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(key) || key.Length > 80) return null;
        var isbns = Strings(item, "isbn", 20).Select(value => value.Replace("-", string.Empty, StringComparison.Ordinal).ToUpperInvariant()).ToArray();
        var isbn10 = isbns.FirstOrDefault(AudiobookMetadataInput.IsValidIsbn10);
        var isbn13 = isbns.FirstOrDefault(AudiobookMetadataInput.IsValidIsbn13);
        var cover = item.TryGetProperty("cover_i", out var coverValue) && coverValue.TryGetInt64(out var coverId) && coverId > 0
            ? $"/api/v1/modules/media/audiobooks/metadata/covers/{coverId}" : null;
        var language = Strings(item, "language", 10).Select(AudiobookLanguages.Normalize).FirstOrDefault(value => value != AudiobookLanguages.Unknown)
            ?? AudiobookLanguages.Unknown;
        return new(
            key,
            Strings(item, "edition_key", 20),
            WebUtility.HtmlDecode(title).Trim(),
            Strings(item, "alternative_title", 5).Select(value => WebUtility.HtmlDecode(value) ?? value).ToArray(),
            Strings(item, "author_name", 5).Select(value => WebUtility.HtmlDecode(value) ?? value).ToArray(),
            Strings(item, "series", 3).Select(WebUtility.HtmlDecode).FirstOrDefault(),
            null,
            [],
            isbn10,
            isbn13,
            null,
            language,
            item.TryGetProperty("first_publish_year", out var year) && year.TryGetInt32(out var parsedYear) && parsedYear is >= 1000 and <= 3000 ? parsedYear : null,
            cover,
            "openLibrary");
    }

    private static string? Text(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static string[] Strings(JsonElement item, string name, int maximum) =>
        item.TryGetProperty(name, out var values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString()?.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value) && value!.Length <= 300).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).Take(maximum).ToArray()
            : [];

    private static async Task<JsonDocument> ParseBoundedAsync(HttpResponseMessage response, int maximum, CancellationToken token)
    {
        var bytes = await ReadBoundedAsync(response, maximum, token);
        return JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 32 });
    }

    private static async Task<byte[]> ReadBoundedAsync(HttpResponseMessage response, int maximum, CancellationToken token)
    {
        if (response.Content.Headers.ContentLength is > 0 && response.Content.Headers.ContentLength > maximum)
            throw new InvalidDataException("Metadata provider response exceeded the configured limit.");
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var buffer = new MemoryStream();
        var chunk = new byte[16_384];
        while (true)
        {
            var read = await stream.ReadAsync(chunk, token);
            if (read == 0) break;
            if (buffer.Length + read > maximum) throw new InvalidDataException("Metadata provider response exceeded the configured limit.");
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }
}

public static class AudiobookDiscoveryPlanner
{
    public const int MaximumProviderSearches = 2;
    public const int MaximumUpstreamQueries = 6;

    public static IReadOnlyList<AudiobookDiscoverySeed> Plan(AudiobookMetadataQuery input, IReadOnlyList<AudiobookMetadataWork> works, string? authorHint)
    {
        var seeds = new List<AudiobookDiscoverySeed>();
        var inputLooksLikeAuthor = works.Count > 0 && works.Any(work => work.Authors.Any(author => Same(author, input.Original)));
        // A literal owner query is an immutable discovery identity. Author
        // metadata may add a representative work, but must never consume both
        // bounded slots and replace the text the owner actually entered.
        if (inputLooksLikeAuthor)
            Add(seeds, new(input.Original, null, null, "literal"));
        foreach (var work in works.Take(inputLooksLikeAuthor ? MaximumProviderSearches : 1))
        {
            var author = (work.Authors.Count > 0 ? work.Authors[0] : null) ?? NullIfWhiteSpace(authorHint);
            var evidence = input.Kind is AudiobookMetadataInputKinds.Isbn10 or AudiobookMetadataInputKinds.Isbn13
                ? "identifier" : inputLooksLikeAuthor ? "authorWork" : author is null ? "canonicalTitle" : "canonicalTitleAuthor";
            Add(seeds, new(work.CanonicalTitle, author, work.WorkId, evidence));
            if (seeds.Count >= MaximumProviderSearches) break;
            if (!string.IsNullOrWhiteSpace(work.Series) && !Same(work.Series, work.CanonicalTitle))
                Add(seeds, new(work.Series!, null, work.WorkId, "series"));
            if (seeds.Count >= MaximumProviderSearches) break;
            var alternate = work.AlternateTitles.FirstOrDefault(value => !Same(value, work.CanonicalTitle));
            if (!string.IsNullOrWhiteSpace(alternate)) Add(seeds, new(alternate!, null, work.WorkId, "alternateTitle"));
            if (seeds.Count >= MaximumProviderSearches) break;
        }
        if (seeds.Count == 0) Add(seeds, new(input.Original, NullIfWhiteSpace(authorHint), null, "literal"));
        else if (input.Kind == AudiobookMetadataInputKinds.FreeText && !seeds.Any(seed => Same(seed.Query, input.Original)) && seeds.Count < MaximumProviderSearches)
            Add(seeds, new(input.Original, NullIfWhiteSpace(authorHint), null, "literal"));
        return seeds.Take(MaximumProviderSearches).ToArray();
    }

    private static void Add(List<AudiobookDiscoverySeed> seeds, AudiobookDiscoverySeed candidate)
    {
        var normalizedQuery = AudiobookMetadataInput.NormalizeText(candidate.Query);
        var normalizedAuthor = NullIfWhiteSpace(candidate.Author);
        if (normalizedQuery.Length is < 2 or > 120 || normalizedAuthor?.Length > 120) return;
        if (seeds.Any(seed => Same(seed.Query, normalizedQuery) && Same(seed.Author, normalizedAuthor))) return;
        seeds.Add(candidate with { Query = normalizedQuery, Author = normalizedAuthor });
    }
    private static bool Same(string? left, string? right) =>
        string.Equals(AudiobookMetadataInput.NormalizeText(left ?? string.Empty), AudiobookMetadataInput.NormalizeText(right ?? string.Empty), StringComparison.OrdinalIgnoreCase);
    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : AudiobookMetadataInput.NormalizeText(value);
}

public sealed class AudiobookUniversalSearchService(
    IAudiobookMetadataProvider metadata,
    IAudiobookshelfClient library,
    AudiobookAcquisitionService acquisition)
{
    public async Task<AudiobookUniversalSearchResult> SearchAsync(string input, string? authorHint, string? language, CancellationToken token)
    {
        var query = AudiobookMetadataInput.Classify(input);
        IReadOnlyList<AudiobookMetadataWork> works;
        var metadataState = "resolved";
        string? metadataMessage = null;
        try { works = await metadata.ResolveAsync(query, token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException)
        {
            works = [];
            metadataState = "unavailable";
            metadataMessage = "Bokmetadata kunde inte hämtas; sökningen fortsatte med originaltexten.";
        }
        if (works.Count == 0 && metadataState == "resolved") metadataState = "notFound";
        var resolution = new AudiobookMetadataResolution(query, metadataState, works, false, metadataMessage);
        var seeds = AudiobookDiscoveryPlanner.Plan(query, works, authorHint);
        var normalizedLanguage = string.IsNullOrWhiteSpace(language) || language.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? AudiobookLanguages.Unknown : AudiobookLanguages.Normalize(language);
        var libraryQuery = works.Count > 0 ? works[0].CanonicalTitle : query.Original;
        var local = await library.GetLibraryAsync(0, 25, libraryQuery, null, token);
        AudiobookAcquisitionProviderStatus status;
        IReadOnlyList<AudiobookAcquisitionCandidate> discovery;
        try
        {
            status = await acquisition.StatusAsync(token);
            discovery = status.CanSearch
                ? await acquisition.SearchVariantsAsync(seeds, normalizedLanguage, token)
                : [];
        }
        catch (AudiobookAcquisitionException exception)
        {
            status = new(AudiobookIntegrationStates.ConfiguredUnavailable, "unknown", false, false, false, exception.SafeMessage);
            discovery = [];
        }
        return new(local.Items, resolution, discovery, status);
    }
}
