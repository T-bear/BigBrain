using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Finance.OutcomeAudit;

internal sealed record AuditReport(string Contract, string Status, string? StopCode,
    Dictionary<string, long> Counts, Dictionary<string, long> Findings)
{
    internal static AuditReport Stopped(string code) => new("outcome-ledger-audit-v1", "STOP", code, [], []);
}

internal static class LedgerAudit
{
    private static readonly SearchValues<char> HashCharacters = SearchValues.Create("0123456789abcdef");
    private const int MaximumRows = 100000;
    private const string Horizon = "next-eligible-source-session-close-v1";
    // Frozen mapping contract from main EodhdCatalog; no provider discovery or latest-state fallback.
    private static readonly Dictionary<string, string> Venues = new(StringComparer.Ordinal)
    { ["SPY"] = "ARCX", ["QQQ"] = "XNAS", ["IWM"] = "ARCX", ["AAPL"] = "XNAS", ["MSFT"] = "XNAS", ["JPM"] = "XNYS", ["XOM"] = "XNYS", ["JNJ"] = "XNYS" };

    internal static AuditReport Read(SqliteConnection connection, SqliteTransaction transaction, DateTimeOffset snapshotUtc)
    {
        using (var integrity = connection.CreateCommand())
        {
            integrity.Transaction = transaction;
            integrity.CommandText = "PRAGMA quick_check";
            using var check = integrity.ExecuteReader();
            if (!check.Read() || check.GetString(0) != "ok" || check.Read()) return AuditReport.Stopped("image.integrity");
        }
        var tables = new Dictionary<string, List<Row>>(StringComparer.Ordinal);
        // Table names/SQL are compile-time constants. No arbitrary query/filter input, views or extensions.
        var queries = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["shadow_predictions"] = "SELECT prediction_id,instrument_id,symbol,session_date,provider,source_revision_id,observation_knowledge_utc,knowledge_cutoff_utc,feature_revision_id,strategy_id,strategy_version,parameter_fingerprint,signal,horizon,created_utc,state,operating_mode FROM shadow_predictions LIMIT 100001",
            ["shadow_outcomes"] = "SELECT outcome_id,prediction_id,target_session,source_revision_id,knowledge_utc,reference_close,realized_close,realized_return,direction_result,evaluated_utc FROM shadow_outcomes LIMIT 100001",
            ["revisions"] = "SELECT revision_id,checksum,created_utc FROM revisions LIMIT 100001",
            ["observations"] = "SELECT provider,product,policy,instrument_id,symbol,provider_symbol,mic,session_date,close,acquired_utc,revision_id FROM observations LIMIT 100001",
            ["feature_revisions"] = "SELECT revision_id,source_revisions_json,created_utc,checksum,price_basis FROM feature_revisions LIMIT 100001"
        };
        foreach (var (name, query) in queries)
        {
            using var schema = connection.CreateCommand();
            schema.Transaction = transaction;
            schema.CommandText = "SELECT type,sql FROM sqlite_schema WHERE name=$name";
            schema.Parameters.AddWithValue("$name", name);
            using (var reader = schema.ExecuteReader())
                if (!reader.Read() || reader.GetString(0) != "table" || reader.GetString(1).Contains("VIRTUAL", StringComparison.OrdinalIgnoreCase))
                    return AuditReport.Stopped("schema.table");
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = query;
            using var result = command.ExecuteReader();
            var rows = new List<Row>();
            while (result.Read())
            {
                var row = new Row();
                for (var i = 0; i < result.FieldCount; i++) row[result.GetName(i)] = result.IsDBNull(i) ? "" : result.GetString(i);
                rows.Add(row);
            }
            if (rows.Count > MaximumRows) return AuditReport.Stopped("ledger.limit");
            tables.Add(name, rows);
        }
        var predictions = tables["shadow_predictions"];
        var outcomes = tables["shadow_outcomes"];
        var revisions = tables["revisions"].ToLookup(row => row["revision_id"], StringComparer.Ordinal);
        var features = tables["feature_revisions"].ToLookup(row => row["revision_id"], StringComparer.Ordinal);
        var bars = tables["observations"];
        var barIndex = bars.ToLookup(row => (row["instrument_id"], row["session_date"]));
        var outcomeIds = outcomes.ToLookup(row => row["outcome_id"], StringComparer.Ordinal);
        var outcomePredictions = outcomes.ToLookup(row => row["prediction_id"], StringComparer.Ordinal);
        var predictionIndex = predictions.ToLookup(row => row["prediction_id"], StringComparer.Ordinal);
        var counts = tables.ToDictionary(pair => pair.Key, pair => (long)pair.Value.Count, StringComparer.Ordinal);
        var findings = new Dictionary<string, long>(StringComparer.Ordinal);
        using (var foreignKeys = connection.CreateCommand())
        {
            foreignKeys.Transaction = transaction;
            foreignKeys.CommandText = "PRAGMA foreign_key_check";
            using var check = foreignKeys.ExecuteReader();
            long violations = 0;
            while (check.Read()) violations++;
            counts["sqlite-foreign-key-violations"] = violations;
        }
        void Count(string code, long count = 1) { if (count != 0) findings[code] = findings.GetValueOrDefault(code) + count; }
        Count("prediction.duplicate-id", predictionIndex.Sum(group => Math.Max(0, group.Count() - 1)));
        Count("outcome.duplicate-id", outcomes.GroupBy(row => row["outcome_id"], StringComparer.Ordinal).Sum(group => Math.Max(0, group.Count() - 1)));
        Count("outcome.duplicate-prediction", outcomes.GroupBy(row => row["prediction_id"], StringComparer.Ordinal).Sum(group => Math.Max(0, group.Count() - 1)));
        Count("observation.orphan-revision", bars.Count(bar => !revisions.Contains(bar["revision_id"])));
        Count("prediction.orphan-source", predictions.Count(p => !revisions.Contains(p["source_revision_id"])));
        Count("prediction.orphan-feature", predictions.Count(p => !features.Contains(p["feature_revision_id"])));
        Count("revision.duplicate-id", revisions.Sum(group => Math.Max(0, group.Count() - 1)));
        Count("feature.duplicate-id", features.Sum(group => Math.Max(0, group.Count() - 1)));
        counts["historical-unversioned-outcomes"] = outcomes.Count; // Schema has no admission-version column.
        counts["v2-criteria-satisfied"] = 0;
        counts["invalidated-with-outcome"] = 0;
        counts["payload-checksums-not-recomputed"] = tables["revisions"].Count;
        counts["feature-checksums-not-recomputed"] = tables["feature_revisions"].Count;
        foreach (var outcome in outcomes)
        {
            var issues = new HashSet<string>(StringComparer.Ordinal);
            void Check(bool valid, string code) { if (!valid) issues.Add(code); }
            var matches = predictionIndex[outcome["prediction_id"]].ToArray();
            if (matches.Length != 1) { Count(matches.Length == 0 ? "outcome.orphan-prediction" : "outcome.ambiguous-prediction"); continue; }
            var p = matches[0];
            Check(outcomeIds[outcome["outcome_id"]].Count() == 1 && outcomePredictions[outcome["prediction_id"]].Count() == 1, "outcome.nonunique-link");
            Check(revisions.Contains(outcome["source_revision_id"]), "outcome.orphan-revision");
            if (p["state"] == "Invalidated") { counts["invalidated-with-outcome"]++; issues.Add("prediction.invalidated"); }
            Check(p["state"] is "Pending" or "Evaluated" or "Invalidated", "prediction.state");
            Check(p["provider"] == "EODHD" && p["operating_mode"] == "RESEARCH" && p["horizon"] == Horizon, "prediction.contract");
            Check(!string.IsNullOrWhiteSpace(p["strategy_id"]) && !string.IsNullOrWhiteSpace(p["strategy_version"]) && HashShape(p["parameter_fingerprint"]), "prediction.strategy-identity");
            var sourceDateValid = Date(p["session_date"], out var sourceDate);
            var targetDateValid = Date(outcome["target_session"], out var targetDate);
            var sourceSession = sourceDateValid ? UsMarketCalendar.Session(sourceDate) : null;
            var targetSession = targetDateValid ? UsMarketCalendar.Session(targetDate) : null;
            Check(sourceSession is not null && targetSession is not null && Next(sourceDate) == targetDate, "session.not-immediate-next");
            var times = Instant(p["created_utc"], out var created) & Instant(p["knowledge_cutoff_utc"], out var cutoff)
                & Instant(p["observation_knowledge_utc"], out var observed) & Instant(outcome["knowledge_utc"], out var known)
                & Instant(outcome["evaluated_utc"], out var evaluated);
            Check(times, "time.missing-or-invalid-zone");
            if (times)
            {
                Check(created <= snapshotUtc && evaluated <= snapshotUtc, "time.after-snapshot");
                Check(observed <= cutoff && cutoff <= created, "time.prediction-order");
                Check(targetSession is not null && created < targetSession.OpenUtc, "time.not-pre-open");
                Check(known > created && known > cutoff, "time.outcome-already-known");
                Check(known <= evaluated && targetSession is not null && known >= targetSession.CloseUtc, "time.outcome-order");
                Check(sourceSession is not null && observed >= sourceSession.CloseUtc, "time.source-before-close");
            }
            var source = barIndex[(p["instrument_id"], p["session_date"])].Where(bar => bar["revision_id"] == p["source_revision_id"]).ToArray();
            var target = barIndex[(p["instrument_id"], outcome["target_session"])].Where(bar => bar["revision_id"] == outcome["source_revision_id"]).ToArray();
            Check(source.Length == 1, "source.missing-or-ambiguous-observation");
            Check(target.Length == 1, "target.missing-or-ambiguous-observation");
            void ValidateBar(Row bar, DateTimeOffset expectedKnowledge, string prefix)
            {
                Check(Venues.TryGetValue(p["symbol"], out var mic) && p["instrument_id"] == $"US:{mic}:{p["symbol"]}" &&
                    bar["symbol"] == p["symbol"] && bar["provider_symbol"] == p["symbol"] + ".US" && bar["mic"] == mic &&
                    bar["provider"] == "EODHD" && bar["product"] == "Free" && bar["policy"] == "eodhd-free-personal-v2026-08-11", prefix + ".mapping");
                Check(Instant(bar["acquired_utc"], out var acquired) && times && acquired == expectedKnowledge, prefix + ".knowledge-link");
                var revision = revisions[bar["revision_id"]].ToArray();
                Check(revision.Length == 1, prefix + ".missing-or-ambiguous-revision");
                if (revision.Length == 1)
                {
                    Check(HashShape(revision[0]["checksum"]) && bar["revision_id"] == string.Concat("eodhd-", revision[0]["checksum"].AsSpan(7, 16)), prefix + ".checksum-identity");
                    Check(Instant(revision[0]["created_utc"], out var revisionTime) && revisionTime == acquired, prefix + ".revision-time");
                }
            }
            if (source.Length == 1) ValidateBar(source[0], observed, "source");
            if (target.Length == 1) ValidateBar(target[0], known, "target");
            var candidates = barIndex[(p["instrument_id"], outcome["target_session"])].Where(bar => bar["provider"] == "EODHD").ToArray();
            var candidateTimes = candidates.Select(bar => Instant(bar["acquired_utc"], out var time) ? (DateTimeOffset?)time : null).ToArray();
            if (candidateTimes.Length == 0 || candidateTimes.Any(time => !time.HasValue)) issues.Add("target.unknown-first-revision");
            else
            {
                var first = candidateTimes.Min();
                Check(candidateTimes.Count(time => time == first) == 1, "target.ambiguous-first-revision");
                Check(times && known == first, "target.not-first-revision");
            }
            var feature = features[p["feature_revision_id"]].ToArray();
            Check(feature.Length == 1, "feature.missing-or-ambiguous");
            if (feature.Length == 1)
            {
                Check(Instant(feature[0]["created_utc"], out var featureTime) && times && featureTime <= cutoff, "feature.future-or-unknown-time");
                Check(HashShape(feature[0]["checksum"]), "feature.checksum-shape");
                Check(feature[0]["price_basis"] == "raw close/OHLC; provider volume classification; no adjusted-price mixing", "feature.price-basis");
                try
                {
                    var sources = JsonSerializer.Deserialize<string[]>(feature[0]["source_revisions_json"]);
                    Check(sources is not null && sources.Contains(p["source_revision_id"], StringComparer.Ordinal), "feature.source-membership");
                    if (sources is not null)
                        foreach (var id in sources)
                        {
                            var refs = revisions[id ?? ""].ToArray();
                            Check(refs.Length == 1 && Instant(refs[0]["created_utc"], out var time) && times && time <= cutoff, "feature.input-lineage");
                        }
                }
                catch (JsonException) { issues.Add("feature.invalid-json"); }
            }
            Check(outcome["outcome_id"] == OutcomeId(p["prediction_id"], outcome["source_revision_id"], outcome["target_session"]), "outcome.identity");
            if (source.Length == 1 && target.Length == 1 && Number(source[0]["close"], out var reference) && reference > 0 &&
                Number(target[0]["close"], out var close) && close > 0 && Number(outcome["reference_close"], out var storedReference) &&
                Number(outcome["realized_close"], out var storedClose) && Number(outcome["realized_return"], out var storedReturn))
            {
                Check(reference == storedReference && close == storedClose, "outcome.price-link");
                try
                {
                    var realized = close / reference - 1;
                    Check(realized == storedReturn, "outcome.return");
                    var direction = p["signal"] == "TargetLong" ? (realized > 0 ? "CORRECT" : "INCORRECT") :
                        p["signal"] == "TargetFlat" ? (realized <= 0 ? "CORRECT" : "INCORRECT") : "NOT_APPLICABLE";
                    Check(p["signal"] is "TargetLong" or "TargetFlat", "prediction.signal");
                    Check(direction == outcome["direction_result"], "outcome.direction");
                }
                catch (OverflowException) { issues.Add("outcome.numeric-range"); }
            }
            else issues.Add("outcome.missing-or-invalid-price");
            foreach (var issue in issues) Count(issue);
            if (issues.Count == 0) counts["v2-criteria-satisfied"]++;
        }
        // Even zero findings cannot certify payload hashes, original v1 causality admission,
        // rights, operational authorization or prospective cohort membership.
        return new("outcome-ledger-audit-v1", "HISTORICAL_AUDIT_ONLY", null, counts, findings);
    }

    internal static string OutcomeId(string prediction, string revision, string date) =>
        "outcome-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{prediction}|{revision}|{date}"))).ToLowerInvariant()[..16];
    private static bool HashShape(string value) => value.Length == 71 && value.StartsWith("sha256:", StringComparison.Ordinal) && value.AsSpan(7).IndexOfAnyExcept(HashCharacters) < 0;
    private static bool Number(string text, out decimal value) => decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    private static bool Date(string text, out DateOnly date) => DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date) && date.Year is >= 2000 and <= 2100;
    private static bool Instant(string text, out DateTimeOffset value)
    {
        value = default;
        return text.Length >= 20 && text[10] == 'T' && text[13] == ':' && text[16] == ':' && (text.EndsWith('Z') || (text.Length >= 6 && text[^3] == ':' && text[^6] is '+' or '-')) &&
            DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }
    private static DateOnly? Next(DateOnly date)
    {
        if (date.Year is < 2000 or > 2100) return null;
        for (var offset = 1; offset <= 10; offset++) if (UsMarketCalendar.IsSession(date.AddDays(offset))) return date.AddDays(offset);
        return null;
    }
    private sealed class Row : Dictionary<string, string>
    {
        internal Row() : base(StringComparer.Ordinal) { }
    }
}
