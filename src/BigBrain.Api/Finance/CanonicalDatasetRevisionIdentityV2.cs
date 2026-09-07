using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BigBrain.Api.Finance;

internal sealed record CanonicalDatasetIdentityRow(string Symbol, DateOnly Date,
    decimal Open, decimal High, decimal Low, decimal Close, decimal? AdjustedClose, decimal Volume);

internal sealed record CanonicalDatasetRevisionIdentity(string Source, string Product, string RevisionId, string Checksum);

// Only future canonical dataset promotions use this contract. Legacy IDs are never recomputed.
internal static class CanonicalDatasetRevisionIdentityV2
{
    internal const string Algorithm = "canonical-dataset-revision-v2";

    internal static string NormalizeIdentifier(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > 64 || !char.IsAsciiLetterOrDigit(trimmed[0]) ||
            trimmed.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '.' and not '_' and not '-'))
            throw new InvalidDataException("Canonical source and product require 1-64 ASCII identifier characters, starting with a letter or digit.");
        return trimmed.ToUpperInvariant();
    }

    internal static CanonicalDatasetRevisionIdentity Create(string source, string? product,
        IEnumerable<CanonicalDatasetIdentityRow> rows)
    {
        var normalizedSource = NormalizeIdentifier(source);
        var normalizedProduct = NormalizeIdentifier(product);
        var bytes = Serialize(normalizedSource, normalizedProduct, rows);
        var digest = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        return new(normalizedSource, normalizedProduct, "dataset-v2-" + digest, "sha256:" + digest);
    }

    internal static byte[] Serialize(string source, string? product, IEnumerable<CanonicalDatasetIdentityRow> rows)
    {
        var header = Algorithm + "\n" + NormalizeIdentifier(source) + "\n" + NormalizeIdentifier(product);
        var ordered = rows.OrderBy(x => x.Symbol, StringComparer.Ordinal).ThenBy(x => x.Date).ToArray();
        if (ordered.Length == 0) throw new InvalidDataException("Canonical identity requires rows.");
        // Symbols have already passed mapping; reject delimiter ambiguity even at this internal boundary.
        if (ordered.Any(x => string.IsNullOrEmpty(x.Symbol) || x.Symbol.IndexOfAny(['|', '\r', '\n']) >= 0))
            throw new InvalidDataException("Canonical symbol contains an identity delimiter.");
        var content = string.Join("\n", ordered.Select(x => string.Join('|', x.Symbol,
            x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Number(x.Open), Number(x.High),
            Number(x.Low), Number(x.Close), x.AdjustedClose is { } adjusted ? Number(adjusted) : "", Number(x.Volume))));
        // UTF-8 without BOM; LF only, no final LF; decimal default format deliberately preserves scale.
        return Encoding.UTF8.GetBytes(header + "\n" + content);
    }

    private static string Number(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}
