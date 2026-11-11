using System.Buffers;
using System.Globalization;
using System.Text.Json;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

internal sealed partial class EodhdMarketMemory
{
    // An evaluator must establish causality itself, even for legacy stored predictions.
    // This version admits only the immediately following exchange session, forecast before open.
    internal const string ShadowOutcomeAdmissionVersion = "shadow-outcome-causality-v2";
    private static readonly SearchValues<char> ShadowHashCharacters = SearchValues.Create("0123456789abcdef");

    internal static void EvaluatePending(SqliteConnection connection, DateTimeOffset now)
    {
        if (now.Offset != TimeSpan.Zero) throw new ArgumentException("Shadow evaluation clock must be UTC.", nameof(now));
        using var transaction = connection.BeginTransaction();
        var pending = ReadPredictions(connection, null, null, nameof(FinanceShadowState.Pending), null, null, 2000);
        foreach (var prediction in pending)
        {
            if (!ShadowPredictionTimesExplicit(connection, prediction) || !TryShadowTarget(prediction, now, out var target)) continue;
            var original = ReadShadowBars(connection, prediction.InstrumentId, prediction.SessionDate)
                .Where(bar => bar.Revision == prediction.SourceRevisionId).ToArray();
            if (original.Length != 1 || !ValidShadowBar(original[0], prediction, prediction.SessionDate) ||
                original[0].Known != prediction.ObservationKnowledgeUtc ||
                original[0].Known > prediction.KnowledgeCutoffUtc || !ShadowFeatureKnown(connection, prediction)) continue;

            // Never filter away an earlier known revision and choose a later, more convenient one.
            // Parse instants before ordering: SQLite TEXT order is not time-zone-safe.
            var candidates = ReadShadowBars(connection, prediction.InstrumentId, target.Date);
            if (candidates.Count == 0 || candidates.Any(bar => !bar.ValidTime)) continue;
            var earliest = candidates.Min(bar => bar.Known);
            var first = candidates.Where(bar => bar.Known == earliest).ToArray();
            if (first.Length != 1) continue; // Tied revision identities are ambiguous, not a tie-break opportunity.
            var outcome = first[0];
            if (!ValidShadowBar(outcome, prediction, target.Date) || outcome.Known <= prediction.CreatedAtUtc ||
                outcome.Known <= prediction.KnowledgeCutoffUtc || outcome.Known > now || target.CloseUtc > now) continue;

            var realizedReturn = outcome.Close / original[0].Close - 1;
            var direction = prediction.Signal == nameof(ResearchIntentKind.TargetLong)
                ? (realizedReturn > 0 ? "CORRECT" : "INCORRECT")
                : prediction.Signal == nameof(ResearchIntentKind.TargetFlat)
                    ? (realizedReturn <= 0 ? "CORRECT" : "INCORRECT") : "NOT_APPLICABLE";
            Execute(connection, transaction, """
                INSERT OR IGNORE INTO shadow_outcomes VALUES($id,$prediction,$date,$revision,$known,$reference,$close,$return,$direction,$evaluated)
                """, ("$id", FinanceShadowIdentity.Outcome(prediction.PredictionId, outcome.Revision, target.Date)),
                ("$prediction", prediction.PredictionId), ("$date", target.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("$revision", outcome.Revision), ("$known", outcome.Known.ToUniversalTime().ToString("O")),
                ("$reference", original[0].Close.ToString(CultureInfo.InvariantCulture)),
                ("$close", outcome.Close.ToString(CultureInfo.InvariantCulture)),
                ("$return", realizedReturn.ToString(CultureInfo.InvariantCulture)), ("$direction", direction),
                ("$evaluated", now.ToString("O")));
        }
        transaction.Commit();
    }

    private static bool TryShadowTarget(FinanceShadowPrediction prediction, DateTimeOffset now, out UsEquitySession target)
    {
        target = null!;
        if (prediction.Provider != Provider || prediction.OperatingMode != "RESEARCH" ||
            prediction.Horizon != FinanceShadowIdentity.Horizon ||
            UsMarketCalendar.Session(prediction.SessionDate) is not { } source ||
            prediction.ObservationKnowledgeUtc < source.CloseUtc ||
            prediction.ObservationKnowledgeUtc > prediction.KnowledgeCutoffUtc ||
            prediction.KnowledgeCutoffUtc > prediction.CreatedAtUtc || prediction.CreatedAtUtc > now) return false;
        for (var offset = 1; offset <= 10; offset++)
        {
            if (prediction.SessionDate.DayNumber > DateOnly.MaxValue.DayNumber - offset) return false;
            if (UsMarketCalendar.Session(prediction.SessionDate.AddDays(offset)) is not { } next) continue;
            target = next;
            // A late import after the target's market event cannot become prospective evidence.
            // Requiring pre-open also avoids relying on an unmodelled intraday/early-close boundary.
            return prediction.CreatedAtUtc < next.OpenUtc;
        }
        return false;
    }

    private sealed record ShadowBar(string Revision, string Product, string Policy, string Symbol,
        string ProviderSymbol, string Mic, decimal Close, DateTimeOffset Known, bool ValidTime,
        string? Checksum, DateTimeOffset RevisionCreated, bool ValidRevisionTime);

    private static List<ShadowBar> ReadShadowBars(SqliteConnection connection, string instrument, DateOnly session)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT o.revision_id,o.product,o.policy,o.symbol,o.provider_symbol,o.mic,o.close,o.acquired_utc,r.checksum,r.created_utc
            FROM observations o LEFT JOIN revisions r ON r.revision_id=o.revision_id
            WHERE o.provider=$provider AND o.instrument_id=$instrument AND o.session_date=$date
            """;
        command.Parameters.AddWithValue("$provider", Provider);
        command.Parameters.AddWithValue("$instrument", instrument);
        command.Parameters.AddWithValue("$date", session.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        using var reader = command.ExecuteReader();
        var bars = new List<ShadowBar>();
        while (reader.Read())
        {
            var validTime = TryShadowInstant(reader.GetString(7), out var known);
            var validRevision = TryShadowInstant(reader.IsDBNull(9) ? "" : reader.GetString(9), out var revisionCreated);
            decimal.TryParse(reader.GetString(6), NumberStyles.Number, CultureInfo.InvariantCulture, out var close);
            bars.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetString(5), close, known, validTime,
                reader.IsDBNull(8) ? null : reader.GetString(8), revisionCreated, validRevision));
        }
        return bars;
    }

    private static bool ValidShadowBar(ShadowBar bar, FinanceShadowPrediction prediction, DateOnly date)
    {
        var instrument = EodhdCatalog.Watchlist.SingleOrDefault(item => item.InstrumentId == prediction.InstrumentId);
        return instrument is not null && prediction.Symbol == instrument.Symbol && bar.Symbol == instrument.Symbol &&
            bar.ProviderSymbol == instrument.ProviderSymbol && bar.Mic == instrument.Mic &&
            bar.Product == Product && bar.Policy == Policy && bar.Close > 0 && bar.ValidTime && bar.ValidRevisionTime &&
            bar.Known == bar.RevisionCreated && UsMarketCalendar.Session(date) is { } session && bar.Known >= session.CloseUtc &&
            bar.Checksum is { Length: 71 } checksum && checksum.StartsWith("sha256:", StringComparison.Ordinal) &&
            checksum.AsSpan(7).IndexOfAnyExcept(ShadowHashCharacters) < 0 && bar.Revision == $"eodhd-{checksum[7..23]}";
    }

    private static bool ShadowFeatureKnown(SqliteConnection connection, FinanceShadowPrediction prediction)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT created_utc,source_revisions_json FROM feature_revisions WHERE revision_id=$revision";
        command.Parameters.AddWithValue("$revision", prediction.FeatureRevisionId);
        using var reader = command.ExecuteReader();
        if (!reader.Read() || !TryShadowInstant(reader.GetString(0), out var created) || created > prediction.KnowledgeCutoffUtc) return false;
        try
        {
            var sources = JsonSerializer.Deserialize<string[]>(reader.GetString(1));
            reader.Close();
            if (sources is null || !sources.Contains(prediction.SourceRevisionId, StringComparer.Ordinal)) return false;
            foreach (var source in sources)
            {
                using var revision = connection.CreateCommand();
                revision.CommandText = "SELECT created_utc FROM revisions WHERE revision_id=$source";
                revision.Parameters.AddWithValue("$source", source);
                if (revision.ExecuteScalar() is not string text || !TryShadowInstant(text, out var known) || known > prediction.KnowledgeCutoffUtc)
                    return false;
            }
            return true;
        }
        catch (JsonException) { return false; }
    }

    private static bool ShadowPredictionTimesExplicit(SqliteConnection connection, FinanceShadowPrediction prediction)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT observation_knowledge_utc,knowledge_cutoff_utc,created_utc FROM shadow_predictions WHERE prediction_id=$id";
        command.Parameters.AddWithValue("$id", prediction.PredictionId);
        using var reader = command.ExecuteReader();
        return reader.Read() && TryShadowInstant(reader.GetString(0), out _) &&
            TryShadowInstant(reader.GetString(1), out _) && TryShadowInstant(reader.GetString(2), out _);
    }

    private static bool TryShadowInstant(string text, out DateTimeOffset instant)
    {
        instant = default;
        var explicitZone = text.EndsWith('Z') || (text.Length >= 6 && text[^3] == ':' && text[^6] is '+' or '-');
        return explicitZone && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out instant);
    }
}
