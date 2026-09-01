using System.Collections.Immutable;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using ClosedXML.Excel;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

public sealed record FinanceResearchDatasetCatalog(DateTimeOffset GeneratedAtUtc, string OperatingMode,
    IReadOnlyList<FinanceResearchDatasetItem> Datasets);

public sealed record FinanceResearchDatasetItem(string RevisionId, string CandidateId, string DatasetId,
    string SheetName, string SourceClaim, string InstrumentType, string Symbol, string VenueClaim,
    string SchemaClass, string ArtifactSha256, string WorkbookSha256, string DatasetFingerprint,
    long ObservationCount, string? CoverageFrom, string? CoverageTo, long DuplicateKeys,
    long ConflictingKeys, long MissingValues, long InvalidDates, long NonPositivePrices,
    long InconsistentOhlc, long InvalidVolume, long OutOfOrderRows, string TechnicalQuality,
    long MissingSessions, long SuspiciousDiscontinuities, long SplitLikeJumps, string CrossSourceComparison,
    string OwnerRightsDecision, string OwnerDecisionEvidence, string ExternalRights,
    string PriceBasisClaim, string PriceBasisEvidence, string CorporateActionEvidence,
    string HistoricalIdentityEvidence, IReadOnlyList<string> Limitations,
    IReadOnlyList<ResearchCapabilityDecision> Capabilities, bool CanonicalPromoted);

public sealed record ResearchWorkbookInspection(string CandidateId, string ArtifactSha256,
    string WorkbookSha256, int HistoricalDatasets, int AcceptedDatasets, int Eligible,
    int EligibleWithLimitations, int Ineligible, IReadOnlyList<FinanceResearchDatasetItem> Datasets);

internal sealed partial class FinanceDatasetIntakeStore
{
    internal const string OwnerWorkbookDecisionEvidence = "OWNER-GOOGLEFINANCE-WORKBOOK-2026-09-01-V1";
    private const int MaximumWorkbookEntries = 200;
    private const long MaximumWorkbookExpandedBytes = 256_000_000;
    private const long MaximumWorkbookXmlBytes = 64_000_000;
    private const int MaximumWorkbookSheets = 64;
    private const int MaximumWorkbookRowsPerSheet = 100_000;
    private const int MaximumWorkbookColumns = 32;
    private const long MaximumWorkbookCells = 2_000_000;
    private static readonly JsonSerializerOptions ResearchJson = new(JsonSerializerDefaults.Web);

    private static void InitializeResearchDatasetStorage(SqliteConnection connection)
    {
        Exec(connection, """
          CREATE TABLE IF NOT EXISTS research_dataset_revisions(
            revision_id TEXT PRIMARY KEY,candidate_id TEXT NOT NULL,dataset_id TEXT NOT NULL,sheet_name TEXT NOT NULL,
            source_claim TEXT NOT NULL,instrument_type TEXT NOT NULL,symbol TEXT NOT NULL,venue_claim TEXT NOT NULL,
            schema_class TEXT NOT NULL,artifact_sha256 TEXT NOT NULL,workbook_sha256 TEXT NOT NULL,dataset_fingerprint TEXT NOT NULL,
            observation_count INTEGER NOT NULL,coverage_from TEXT,coverage_to TEXT,duplicate_keys INTEGER NOT NULL,
            conflicting_keys INTEGER NOT NULL,missing_values INTEGER NOT NULL,invalid_dates INTEGER NOT NULL,
            non_positive_prices INTEGER NOT NULL,inconsistent_ohlc INTEGER NOT NULL,invalid_volume INTEGER NOT NULL,
            out_of_order_rows INTEGER NOT NULL,technical_quality TEXT NOT NULL,owner_rights_decision TEXT NOT NULL,
            owner_decision_evidence TEXT NOT NULL,external_rights TEXT NOT NULL,price_basis_claim TEXT NOT NULL,
            price_basis_evidence TEXT NOT NULL,corporate_action_evidence TEXT NOT NULL,historical_identity_evidence TEXT NOT NULL,
            limitations_json TEXT NOT NULL,capabilities_json TEXT NOT NULL,canonical_promoted INTEGER NOT NULL DEFAULT 0,
            created_utc TEXT NOT NULL);
          CREATE TABLE IF NOT EXISTS research_dataset_observations(
            revision_id TEXT NOT NULL,session_date TEXT NOT NULL,open TEXT,high TEXT,low TEXT,close TEXT NOT NULL,
            volume INTEGER,source_row INTEGER NOT NULL,PRIMARY KEY(revision_id,session_date));
          CREATE INDEX IF NOT EXISTS ix_research_dataset_candidate ON research_dataset_revisions(candidate_id,dataset_id);
          """);
        if (!ColumnExists(connection, "research_dataset_revisions", "missing_sessions"))
            Exec(connection, "ALTER TABLE research_dataset_revisions ADD COLUMN missing_sessions INTEGER NOT NULL DEFAULT 0");
        if (!ColumnExists(connection, "research_dataset_revisions", "suspicious_discontinuities"))
            Exec(connection, "ALTER TABLE research_dataset_revisions ADD COLUMN suspicious_discontinuities INTEGER NOT NULL DEFAULT 0");
        if (!ColumnExists(connection, "research_dataset_revisions", "split_like_jumps"))
            Exec(connection, "ALTER TABLE research_dataset_revisions ADD COLUMN split_like_jumps INTEGER NOT NULL DEFAULT 0");
        if (!ColumnExists(connection, "research_dataset_revisions", "cross_source_comparison"))
            Exec(connection, "ALTER TABLE research_dataset_revisions ADD COLUMN cross_source_comparison TEXT NOT NULL DEFAULT 'InsufficientOverlap'");
    }

    internal bool IsOwnerWorkbookArtifact(string artifactPath)
    {
        var extension = Path.GetExtension(artifactPath);
        if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) return true;
        if (!extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)) return false;
        using var archive = ZipFile.OpenRead(artifactPath);
        ValidateOuterArchive(archive);
        return archive.Entries.Count(x => Path.GetExtension(x.Name).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) == 1;
    }

    internal ResearchWorkbookInspection InspectOwnerWorkbookForResearch(ExternalDatasetCandidate candidate, string artifactPath)
    {
        Discover(candidate, "owner-drop-workbook");
        EnsureArtifactRecorded(candidate.CandidateId, artifactPath);
        var existing = ResearchCatalog().Datasets.Where(x => x.CandidateId == candidate.CandidateId).ToArray();
        if (existing.Length > 0)
            return Inspection(candidate.CandidateId, ArtifactHashFromFile(artifactPath), existing[0].WorkbookSha256, existing);

        var state = State(candidate.CandidateId);
        if (state == DatasetCandidateState.Discovered)
        {
            Transition(candidate.CandidateId, state, DatasetCandidateState.Downloading);
            Transition(candidate.CandidateId, DatasetCandidateState.Downloading, DatasetCandidateState.Downloaded);
        }
        if (State(candidate.CandidateId) != DatasetCandidateState.Downloaded)
            throw new InvalidOperationException("Workbook candidate is not available for deterministic inspection.");
        Transition(candidate.CandidateId, DatasetCandidateState.Downloaded, DatasetCandidateState.Inspecting);

        var workbookPath = PrepareWorkbook(candidate.CandidateId, artifactPath);
        var workbookSha = ArtifactHashFromFile(workbookPath);
        var parsed = ParseWorkbook(workbookPath, candidate.CandidateId, ArtifactHashFromFile(artifactPath), workbookSha);
        Transition(candidate.CandidateId, DatasetCandidateState.Inspecting, DatasetCandidateState.Validating);
        PersistResearchWorkbook(candidate, parsed, workbookSha);
        Transition(candidate.CandidateId, DatasetCandidateState.Validating, DatasetCandidateState.ManualReviewRequired);
        var catalog = ResearchCatalog().Datasets.Where(x => x.CandidateId == candidate.CandidateId).ToArray();
        return Inspection(candidate.CandidateId, ArtifactHashFromFile(artifactPath), workbookSha, catalog);
    }

    internal FinanceResearchDatasetCatalog ResearchCatalog()
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT revision_id,candidate_id,dataset_id,sheet_name,source_claim,instrument_type,symbol,venue_claim,schema_class,artifact_sha256,workbook_sha256,dataset_fingerprint,observation_count,coverage_from,coverage_to,duplicate_keys,conflicting_keys,missing_values,invalid_dates,non_positive_prices,inconsistent_ohlc,invalid_volume,out_of_order_rows,technical_quality,missing_sessions,suspicious_discontinuities,split_like_jumps,cross_source_comparison,owner_rights_decision,owner_decision_evidence,external_rights,price_basis_claim,price_basis_evidence,corporate_action_evidence,historical_identity_evidence,limitations_json,capabilities_json,canonical_promoted FROM research_dataset_revisions ORDER BY candidate_id,dataset_id";
        using var reader = command.ExecuteReader(); var rows = new List<FinanceResearchDatasetItem>();
        while (reader.Read()) rows.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8),
            reader.GetString(9), reader.GetString(10), reader.GetString(11), reader.GetInt64(12), reader.IsDBNull(13) ? null : reader.GetString(13),
            reader.IsDBNull(14) ? null : reader.GetString(14), reader.GetInt64(15), reader.GetInt64(16), reader.GetInt64(17),
            reader.GetInt64(18), reader.GetInt64(19), reader.GetInt64(20), reader.GetInt64(21), reader.GetInt64(22), reader.GetString(23),
            reader.GetInt64(24), reader.GetInt64(25), reader.GetInt64(26), reader.GetString(27), reader.GetString(28), reader.GetString(29),
            reader.GetString(30), reader.GetString(31), reader.GetString(32), reader.GetString(33), reader.GetString(34),
            JsonSerializer.Deserialize<string[]>(reader.GetString(35), ResearchJson) ?? [],
            JsonSerializer.Deserialize<ResearchCapabilityDecision[]>(reader.GetString(36), ResearchJson) ?? [], reader.GetInt64(37) != 0));
        return new(DateTimeOffset.UtcNow, "RESEARCH", rows);
    }

    internal BacktestResult RunBoundedResearchBacktest(string revisionId)
    {
        var item = ResearchCatalog().Datasets.SingleOrDefault(x => x.RevisionId == revisionId)
                   ?? throw new ArgumentException("Research dataset revision does not exist.", nameof(revisionId));
        var capability = item.Capabilities.Single(x => x.Purpose == ResearchDatasetPurpose.BoundedHistoricalBacktest);
        if (capability.State == ResearchEligibilityState.Ineligible)
            throw new InvalidOperationException("Dataset is ineligible for bounded historical backtesting.");
        if (item.SchemaClass != ResearchDatasetClass.DailyOhlcv.ToString())
            throw new InvalidOperationException("The existing daily backtest engine requires OHLCV research evidence.");

        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT session_date,open,close,volume FROM research_dataset_observations WHERE revision_id=$id ORDER BY session_date";
        command.Parameters.AddWithValue("$id", revisionId);
        using var reader = command.ExecuteReader(); var bars = new List<BacktestMarketBar>();
        var boundedIdentity = new InstrumentId($"owner-research:{item.VenueClaim}:{item.Symbol}").Value;
        while (reader.Read())
        {
            var date = DateOnly.Parse(reader.GetString(0), CultureInfo.InvariantCulture);
            bars.Add(new(new InstrumentId(boundedIdentity), revisionId, date,
                decimal.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
                new DateTimeOffset(date.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero),
                reader.IsDBNull(3) ? null : reader.GetInt64(3)));
        }
        if (bars.Count < 2) throw new InvalidOperationException("Research dataset has insufficient accepted observations.");
        var lineage = new ResearchDatasetLineage(item.RevisionId, item.CandidateId, item.ArtifactSha256,
            item.DatasetFingerprint, item.SourceClaim, item.SheetName, item.OwnerDecisionEvidence,
            Enum.Parse<DatasetEvidenceResult>(item.ExternalRights), ResearchDatasetPurpose.BoundedHistoricalBacktest,
            capability.State, item.Limitations.Concat(capability.ReasonCodes).Distinct(StringComparer.Ordinal).ToArray());
        var strategy = new BuyAndHoldResearchStrategy();
        var configuration = new BacktestRunConfiguration([revisionId], "research-no-features-v1", strategy.Identity,
            strategy.Parameters, DeterministicBacktestEngine.SimulationModel, BacktestCostModel.Conservative, 100_000m,
            [boundedIdentity], bars.Min(x => x.SessionDate), bars.Max(x => x.SessionDate),
            DeterministicBacktestEngine.SizingPolicy, 0, "owner-research-dataset-v1", BacktestFillModel.NextSessionOpen, lineage);
        var result = DeterministicBacktestEngine.Run(configuration, strategy, bars, []);
        EodhdMarketMemory.PersistBacktest(connection, result);
        return result;
    }

    internal ResearchCapabilityDecision EnsureExistingCandidateResearchEligibility(string candidateId)
    {
        var candidate = Catalog().Datasets.Single(x => x.CandidateId == candidateId);
        var facts = new ResearchDatasetFacts(ResearchDatasetClass.DailyOhlcv,
            Enum.Parse<DatasetOwnerRightsDecision>(candidate.OwnerRightsDecision), candidate.OwnerRightsEvidence,
            Enum.Parse<DatasetEvidenceResult>(candidate.ExternalRightsVerification),
            candidate.TechnicalQuality == "FAIL" ? DatasetEvidenceResult.Fail : candidate.TechnicalQuality == "PASS" ? DatasetEvidenceResult.Pass : DatasetEvidenceResult.Unknown,
            candidate.UnmappedInstruments == 0 ? DatasetEvidenceResult.Pass : DatasetEvidenceResult.Unknown,
            candidate.PriceBasis == DatasetPriceBasis.Unclear.ToString() ? DatasetEvidenceResult.Unknown : DatasetEvidenceResult.Pass,
            DatasetEvidenceResult.Unknown, candidate.Limitations);
        return ResearchDatasetEligibilityPolicyV1.Evaluate(facts).For(ResearchDatasetPurpose.BoundedHistoricalBacktest);
    }

    private string PrepareWorkbook(string candidateId, string artifactPath)
    {
        if (Path.GetExtension(artifactPath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ValidateWorkbookPackage(artifactPath); return artifactPath;
        }
        using var archive = ZipFile.OpenRead(artifactPath); ValidateOuterArchive(archive);
        var workbooks = archive.Entries.Where(x => Path.GetExtension(x.Name).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (workbooks.Length != 1) throw new InvalidDataException("Owner package must contain exactly one XLSX workbook.");
        var root = Path.Combine(_options.QuarantineDirectory, candidateId, "extracted"); Directory.CreateDirectory(root);
        var target = Path.Combine(root, "owner-workbook.xlsx"); var partial = target + ".partial";
        using (var input = workbooks[0].Open()) using (var output = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None))
            input.CopyTo(output);
        File.Move(partial, target, true); ValidateWorkbookPackage(target); return target;
    }

    private void ValidateOuterArchive(ZipArchive archive)
    {
        if (archive.Entries.Count > _options.MaximumArchiveFiles) throw new InvalidDataException("Archive contains too many files.");
        long expanded = 0;
        foreach (var entry in archive.Entries)
        {
            ValidateArchiveName(entry.FullName); expanded += entry.Length;
            if (expanded > _options.MaximumExtractedBytes) throw new InvalidDataException("Archive exceeds extraction limit.");
            if (Path.GetExtension(entry.Name).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Nested ZIP archives are unsupported.");
        }
    }

    private static void ValidateWorkbookPackage(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        if (archive.Entries.Count is 0 or > MaximumWorkbookEntries) throw new InvalidDataException("Workbook entry count is outside the safety bound.");
        long expanded = 0; var cells = 0L; var sheets = 0;
        foreach (var entry in archive.Entries)
        {
            ValidateArchiveName(entry.FullName); expanded += entry.Length;
            if (expanded > MaximumWorkbookExpandedBytes) throw new InvalidDataException("Workbook expansion exceeds the safety bound.");
            var name = entry.FullName.Replace('\\', '/');
            if (name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) || name.Contains("/embeddings/", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("/externalLinks/", StringComparison.OrdinalIgnoreCase) || name.Contains("vbaProject", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("macrosheet", StringComparison.OrdinalIgnoreCase) || name.EndsWith("connections.xml", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Workbook contains an unsupported executable or external feature.");
            if (name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) && entry.Length > MaximumWorkbookXmlBytes)
                throw new InvalidDataException("Workbook XML entry exceeds the safety bound.");
            if (name.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                sheets++; cells += InspectWorksheetXml(entry);
                if (sheets > MaximumWorkbookSheets || cells > MaximumWorkbookCells)
                    throw new InvalidDataException("Workbook sheet or cell count exceeds the safety bound.");
            }
            if (name.EndsWith(".rels", StringComparison.OrdinalIgnoreCase)) RejectExternalRelationships(entry);
        }
    }

    private static long InspectWorksheetXml(ZipArchiveEntry entry)
    {
        using var stream = entry.Open(); using var reader = XmlReader.Create(stream, SafeXmlSettings());
        long cells = 0; var maxRow = 0; var maxColumn = 0;
        while (reader.Read()) if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "c")
        {
            cells++; var reference = reader.GetAttribute("r") ?? "";
            var split = reference.TakeWhile(char.IsLetter).Count();
            if (split > 0 && int.TryParse(reference[split..], out var row)) maxRow = Math.Max(maxRow, row);
            var column = 0; foreach (var ch in reference[..split].ToUpperInvariant()) column = checked(column * 26 + ch - 'A' + 1);
            maxColumn = Math.Max(maxColumn, column);
            if (maxRow > MaximumWorkbookRowsPerSheet || maxColumn > MaximumWorkbookColumns)
                throw new InvalidDataException("Workbook dimensions exceed the safety bound.");
        }
        return cells;
    }

    private static void RejectExternalRelationships(ZipArchiveEntry entry)
    {
        using var stream = entry.Open(); using var reader = XmlReader.Create(stream, SafeXmlSettings());
        while (reader.Read()) if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Relationship" &&
            string.Equals(reader.GetAttribute("TargetMode"), "External", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("External workbook relationships are unsupported.");
    }

    private static XmlReaderSettings SafeXmlSettings() => new()
    {
        DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaximumWorkbookXmlBytes,
        MaxCharactersFromEntities = 0, IgnoreComments = true, IgnoreProcessingInstructions = true
    };

    private static void ValidateArchiveName(string name)
    {
        if (name.StartsWith('/') || name.StartsWith('\\') || Path.IsPathRooted(name) ||
            name.Split('/', '\\').Any(x => x == "..")) throw new InvalidDataException("Unsafe archive path.");
    }

    private static WorkbookParseResult ParseWorkbook(string path, string candidateId, string artifactSha, string workbookSha)
    {
        using var workbook = new XLWorkbook(path);
        if (!workbook.TryGetWorksheet("EXPORT_MANIFEST", out var manifestSheet))
            throw new InvalidDataException("Workbook lacks EXPORT_MANIFEST.");
        var manifestRows = ReadRows(manifestSheet, 500, 32);
        if (manifestRows.Count < 2) throw new InvalidDataException("EXPORT_MANIFEST is empty.");
        var headers = manifestRows[0].Select((x, i) => (Name: Normalize(x), Index: i)).ToDictionary(x => x.Name, x => x.Index, StringComparer.Ordinal);
        string Field(string[] row, string name) => headers.TryGetValue(Normalize(name), out var index) && index < row.Length ? row[index].Trim() : "";
        var datasets = new List<ParsedResearchDataset>();
        foreach (var claim in manifestRows.Skip(1).Where(x => x.Any(y => !string.IsNullOrWhiteSpace(y))))
        {
            var datasetId = Field(claim, "dataset_id"); var sheetName = Field(claim, "sheet");
            if (datasetId.Length is 0 or > 160 || sheetName.Length is 0 or > 80 || !workbook.TryGetWorksheet(sheetName, out var sheet))
                throw new InvalidDataException("Manifest references an invalid or missing dataset sheet.");
            var dataType = Field(claim, "data_type");
            var instrumentType = Field(claim, "instrument_type");
            var datasetClass = dataType.Equals("OHLCV", StringComparison.OrdinalIgnoreCase) ? ResearchDatasetClass.DailyOhlcv :
                dataType.Equals("CLOSE_ONLY", StringComparison.OrdinalIgnoreCase) && instrumentType.Equals("FX", StringComparison.OrdinalIgnoreCase)
                    ? ResearchDatasetClass.DailyCloseOnlyFx : dataType.Equals("CLOSE_ONLY", StringComparison.OrdinalIgnoreCase)
                        ? ResearchDatasetClass.DailyCloseOnlyMarketContext : throw new InvalidDataException("Unsupported manifest data class.");
            var parsed = ParseDatasetSheet(sheet, datasetId, Field(claim, "provider"), instrumentType,
                Field(claim, "symbol"), Field(claim, "exchange"), datasetClass, Field(claim, "price_basis"),
                Field(claim, "owner_use_decision"), Field(claim, "external_rights_verified"), candidateId, artifactSha, workbookSha);
            if (long.TryParse(Field(claim, "observations"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var expected) && expected != parsed.TotalRows)
                parsed.Limitations.Add($"ManifestObservationMismatch:{expected}:{parsed.TotalRows}");
            datasets.Add(parsed);
        }
        if (datasets.Count == 0) throw new InvalidDataException("Workbook contains no declared historical dataset.");
        return new(workbookSha, datasets);
    }

    private static ParsedResearchDataset ParseDatasetSheet(IXLWorksheet sheet, string datasetId, string source,
        string instrumentType, string symbol, string venue, ResearchDatasetClass datasetClass, string priceBasisClaim,
        string ownerDecision, string externalRights, string candidateId, string artifactSha, string workbookSha)
    {
        var rows = ReadRows(sheet, MaximumWorkbookRowsPerSheet, MaximumWorkbookColumns);
        if (rows.Count < 2) throw new InvalidDataException($"Dataset sheet {sheet.Name} is empty.");
        var expectedHeaders = datasetClass == ResearchDatasetClass.DailyOhlcv
            ? new[] { "date", "open", "high", "low", "close", "volume" } : new[] { "date", "close" };
        var actualHeaders = rows[0].Select(Normalize).ToArray();
        if (!actualHeaders.SequenceEqual(expectedHeaders, StringComparer.Ordinal))
            throw new InvalidDataException($"Dataset sheet {sheet.Name} has an unsupported schema.");
        var observations = new List<ResearchObservation>(); var seen = new Dictionary<DateOnly, ResearchObservation>();
        long duplicates = 0, conflicts = 0, missing = 0, invalidDates = 0, nonPositive = 0, inconsistent = 0, invalidVolume = 0, outOfOrder = 0;
        DateOnly? previous = null; var rawRows = new List<string>();
        for (var index = 1; index < rows.Count; index++)
        {
            var values = rows[index]; if (values.All(string.IsNullOrWhiteSpace)) continue;
            rawRows.Add(string.Join('|', values));
            if (values.Length < expectedHeaders.Length || values.Take(expectedHeaders.Length).Any(string.IsNullOrWhiteSpace)) { missing++; continue; }
            if (!TryWorkbookDate(values[0], out var date)) { invalidDates++; continue; }
            if (!decimal.TryParse(values[datasetClass == ResearchDatasetClass.DailyOhlcv ? 4 : 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var close) || close <= 0) { nonPositive++; continue; }
            decimal? open = null, high = null, low = null; long? volume = null;
            if (datasetClass == ResearchDatasetClass.DailyOhlcv)
            {
                if (!decimal.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var o) ||
                    !decimal.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var h) ||
                    !decimal.TryParse(values[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var l) ||
                    !decimal.TryParse(values[4], NumberStyles.Float, CultureInfo.InvariantCulture, out close) || o <= 0 || h <= 0 || l <= 0 || close <= 0)
                { nonPositive++; continue; }
                if (!decimal.TryParse(values[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) || v < 0 || v > long.MaxValue)
                { invalidVolume++; continue; }
                open = o; high = h; low = l; volume = decimal.ToInt64(v);
                if (h < Math.Max(o, close) || l > Math.Min(o, close) || l > h) { inconsistent++; continue; }
            }
            if (previous is { } prior && date < prior) outOfOrder++; previous = date;
            var observation = new ResearchObservation(date, open, high, low, close, volume, index + 1);
            if (seen.TryGetValue(date, out var existing)) { if (existing == observation with { SourceRow = existing.SourceRow }) duplicates++; else conflicts++; continue; }
            seen.Add(date, observation); observations.Add(observation);
        }
        long missingSessions = 0, suspicious = 0, splitLike = 0;
        if (datasetClass == ResearchDatasetClass.DailyOhlcv && observations.Count > 0)
        {
            var ordered = observations.OrderBy(x => x.Date).ToArray(); var dates = ordered.Select(x => x.Date).ToHashSet();
            for (var day = ordered[0].Date; day <= ordered[^1].Date; day = day.AddDays(1))
                if (UsMarketCalendar.IsSession(day) && !dates.Contains(day)) missingSessions++;
            for (var i = 1; i < ordered.Length; i++)
            {
                var ratio = ordered[i].Close / ordered[i - 1].Close;
                if (Math.Abs(ratio - 1m) >= 0.20m) suspicious++;
                if (ratio <= 0.55m || ratio >= 1.80m) splitLike++;
            }
        }
        var limitations = new List<string> { "ExternalRightsUnknown", "HistoricalIdentityBoundedOwnerClaimOnly" };
        if (datasetClass == ResearchDatasetClass.DailyOhlcv)
        {
            limitations.Add("PriceBasisOwnerClaimOnly"); limitations.Add("CorporateActionsUnresolved");
        }
        if (missing > 0) limitations.Add($"MissingValues:{missing}");
        if (invalidDates > 0) limitations.Add($"InvalidDates:{invalidDates}");
        if (nonPositive > 0) limitations.Add($"NonPositivePrices:{nonPositive}");
        if (inconsistent > 0) limitations.Add($"RejectedInconsistentOhlc:{inconsistent}");
        if (invalidVolume > 0) limitations.Add($"InvalidVolume:{invalidVolume}");
        if (outOfOrder > 0) limitations.Add($"OutOfOrderRows:{outOfOrder}");
        if (conflicts > 0) limitations.Add($"ConflictingDuplicates:{conflicts}");
        if (missingSessions > 0) limitations.Add($"MissingCalendarSessions:{missingSessions}");
        if (suspicious > 0) limitations.Add($"SuspiciousCloseDiscontinuities:{suspicious}");
        if (splitLike > 0) limitations.Add($"SplitLikeJumpsWithoutCorporateActionEvidence:{splitLike}");
        if (datasetClass != ResearchDatasetClass.DailyOhlcv) limitations.Add("CalendarGapAnalysisNotSupportedForDataClass");
        var invalid = missing + invalidDates + nonPositive + inconsistent + invalidVolume;
        var technical = observations.Count == 0 || conflicts > 0 || invalid * 100m / Math.Max(1, observations.Count + invalid) > 1m
            ? DatasetEvidenceResult.Fail : invalid > 0 || duplicates > 0 || outOfOrder > 0 ? DatasetEvidenceResult.Unknown : DatasetEvidenceResult.Pass;
        var owner = ownerDecision.Equals("APPROVED_BY_OWNER", StringComparison.OrdinalIgnoreCase)
            ? DatasetOwnerRightsDecision.ApprovedByOwner : DatasetOwnerRightsDecision.NotProvided;
        var external = externalRights.Equals("FAIL", StringComparison.OrdinalIgnoreCase) ? DatasetEvidenceResult.Fail :
            externalRights.Equals("PASS", StringComparison.OrdinalIgnoreCase) ? DatasetEvidenceResult.Pass : DatasetEvidenceResult.Unknown;
        var facts = new ResearchDatasetFacts(datasetClass, owner, OwnerWorkbookDecisionEvidence, external, technical,
            DatasetEvidenceResult.Unknown, DatasetEvidenceResult.Unknown, DatasetEvidenceResult.Unknown, limitations);
        var eligibility = ResearchDatasetEligibilityPolicyV1.Evaluate(facts);
        var fingerprintInput = workbookSha + "\n" + datasetId + "\n" + string.Join("\n", rawRows);
        var fingerprint = "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintInput))).ToLowerInvariant();
        var revisionId = "research-" + fingerprint[7..23];
        return new(revisionId, datasetId, sheet.Name, source, instrumentType, symbol, venue, datasetClass,
            artifactSha, workbookSha, fingerprint, observations, rows.Count - 1, duplicates, conflicts, missing,
            invalidDates, nonPositive, inconsistent, invalidVolume, outOfOrder, missingSessions, suspicious, splitLike, technical, owner, external,
            priceBasisClaim, eligibility, limitations);
    }

    private static List<string[]> ReadRows(IXLWorksheet sheet, int maximumRows, int maximumColumns)
    {
        var range = sheet.RangeUsed() ?? throw new InvalidDataException($"Worksheet {sheet.Name} is empty.");
        if (range.RowCount() > maximumRows || range.ColumnCount() > maximumColumns)
            throw new InvalidDataException($"Worksheet {sheet.Name} exceeds the bounded dimensions.");
        var result = new List<string[]>(range.RowCount());
        foreach (var row in range.Rows())
        {
            var values = new string[range.ColumnCount()];
            for (var column = 1; column <= values.Length; column++)
            {
                var cell = row.Cell(column);
                values[column - 1] = cell.CachedValue.ToString(CultureInfo.InvariantCulture);
            }
            result.Add(values);
        }
        return result;
    }

    private void PersistResearchWorkbook(ExternalDatasetCandidate candidate, WorkbookParseResult parsed, string workbookSha)
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open(); using var transaction = connection.BeginTransaction();
        var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        foreach (var dataset in parsed.Datasets)
        {
            var existing = Scalar(connection, "SELECT dataset_fingerprint FROM research_dataset_revisions WHERE revision_id=$id", ("$id", dataset.RevisionId));
            if (existing is not null && existing != dataset.Fingerprint) throw new InvalidOperationException("Immutable research dataset identity conflict.");
            var comparison = CompareResearchDataset(connection, dataset);
            Exec(connection, transaction, "INSERT OR IGNORE INTO research_dataset_revisions(revision_id,candidate_id,dataset_id,sheet_name,source_claim,instrument_type,symbol,venue_claim,schema_class,artifact_sha256,workbook_sha256,dataset_fingerprint,observation_count,coverage_from,coverage_to,duplicate_keys,conflicting_keys,missing_values,invalid_dates,non_positive_prices,inconsistent_ohlc,invalid_volume,out_of_order_rows,technical_quality,owner_rights_decision,owner_decision_evidence,external_rights,price_basis_claim,price_basis_evidence,corporate_action_evidence,historical_identity_evidence,limitations_json,capabilities_json,canonical_promoted,created_utc,missing_sessions,suspicious_discontinuities,split_like_jumps,cross_source_comparison) VALUES($revision,$candidate,$dataset,$sheet,$source,$type,$symbol,$venue,$class,$artifact,$workbook,$fingerprint,$count,$from,$to,$duplicates,$conflicts,$missing,$dates,$prices,$ohlc,$volume,$order,$technical,$owner,$ownerEvidence,$external,$basis,$basisEvidence,$actions,$identity,$limitations,$capabilities,0,$created,$missingSessions,$suspicious,$splitLike,$comparison)",
                ("$revision", dataset.RevisionId), ("$candidate", candidate.CandidateId), ("$dataset", dataset.DatasetId), ("$sheet", dataset.SheetName),
                ("$source", dataset.Source), ("$type", dataset.InstrumentType), ("$symbol", dataset.Symbol), ("$venue", dataset.Venue),
                ("$class", dataset.DatasetClass.ToString()), ("$artifact", dataset.ArtifactSha), ("$workbook", workbookSha),
                ("$fingerprint", dataset.Fingerprint), ("$count", dataset.Observations.Count),
                ("$from", dataset.Observations.Count == 0 ? DBNull.Value : dataset.Observations.Min(x => x.Date).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("$to", dataset.Observations.Count == 0 ? DBNull.Value : dataset.Observations.Max(x => x.Date).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("$duplicates", dataset.Duplicates), ("$conflicts", dataset.Conflicts), ("$missing", dataset.MissingValues),
                ("$dates", dataset.InvalidDates), ("$prices", dataset.NonPositivePrices), ("$ohlc", dataset.InconsistentOhlc),
                ("$volume", dataset.InvalidVolume), ("$order", dataset.OutOfOrderRows), ("$technical", dataset.Technical.ToString()),
                ("$owner", dataset.OwnerDecision.ToString()), ("$ownerEvidence", OwnerWorkbookDecisionEvidence),
                ("$external", dataset.ExternalRights.ToString()), ("$basis", dataset.PriceBasisClaim), ("$basisEvidence", "OWNER_DECLARATION"),
                ("$actions", "UNKNOWN"), ("$identity", "BOUNDED_EXCHANGE_QUALIFIED_OWNER_CLAIM"),
                ("$limitations", JsonSerializer.Serialize(dataset.Limitations, ResearchJson)),
                ("$capabilities", JsonSerializer.Serialize(dataset.Eligibility.Capabilities, ResearchJson)), ("$created", now),
                ("$missingSessions", dataset.MissingSessions), ("$suspicious", dataset.SuspiciousDiscontinuities),
                ("$splitLike", dataset.SplitLikeJumps), ("$comparison", comparison));
            foreach (var row in dataset.Observations)
                Exec(connection, transaction, "INSERT OR IGNORE INTO research_dataset_observations VALUES($revision,$date,$open,$high,$low,$close,$volume,$row)",
                    ("$revision", dataset.RevisionId), ("$date", row.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    ("$open", row.Open is null ? DBNull.Value : Text(row.Open.Value)), ("$high", row.High is null ? DBNull.Value : Text(row.High.Value)),
                    ("$low", row.Low is null ? DBNull.Value : Text(row.Low.Value)), ("$close", Text(row.Close)),
                    ("$volume", row.Volume is null ? DBNull.Value : row.Volume.Value), ("$row", row.SourceRow));
        }
        var accepted = parsed.Datasets.Sum(x => x.Observations.Count); var limitations = parsed.Datasets.SelectMany(x => x.Limitations).Distinct().Order().ToArray();
        var acceptedRows = parsed.Datasets.SelectMany(x => x.Observations).ToArray();
        var stored = new StoredValidation("Unknown", accepted, parsed.Datasets.Count,
            acceptedRows.Length == 0 ? null : acceptedRows.Min(x => x.Date).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            acceptedRows.Length == 0 ? null : acceptedRows.Max(x => x.Date).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            "Unclear", DatasetSurvivorshipBias.SurvivorshipUnknown.ToString(), [], limitations, workbookSha,
            parsed.Datasets.Sum(x => x.Duplicates), parsed.Datasets.Sum(x => x.Conflicts), parsed.Datasets.Sum(x => x.InconsistentOhlc),
            DatasetComparisonClass.InsufficientOverlap.ToString(), 0, parsed.Datasets.Count, "XLSX", "OpenXML", [],
            DatasetOwnerRightsDecision.ApprovedByOwner.ToString(), OwnerWorkbookDecisionEvidence, DatasetEvidenceResult.Unknown.ToString(),
            "MIXED_OWNER_CLAIMS", "LIMITED", 0, parsed.Datasets.Sum(x => x.OutOfOrderRows), 0, 0, 0,
            parsed.Datasets.Sum(x => x.MissingValues), parsed.Datasets.Sum(x => x.InvalidDates),
            parsed.Datasets.Sum(x => x.NonPositivePrices), parsed.Datasets.Sum(x => x.InconsistentOhlc), parsed.Datasets.Sum(x => x.InvalidVolume));
        Exec(connection, transaction, "UPDATE dataset_candidates SET validation_json=$validation,manifest_json=$manifest,promotion_policy=$policy,promotion_result=$result,canonical_revision_id=NULL,updated_utc=$now WHERE candidate_id=$id",
            ("$validation", JsonSerializer.Serialize(stored, Json)), ("$manifest", JsonSerializer.Serialize(new
            {
                candidate.CandidateId, WorkbookSha256 = workbookSha, OwnerDecision = DatasetOwnerRightsDecision.ApprovedByOwner,
                OwnerDecisionEvidence = OwnerWorkbookDecisionEvidence, ExternalRights = DatasetEvidenceResult.Unknown,
                ResearchPolicy = ResearchDatasetEligibilityPolicyV1.Id, Datasets = parsed.Datasets.Select(x => new { x.DatasetId, x.RevisionId, x.SheetName, x.Fingerprint })
            }, Json)), ("$policy", DatasetPromotionPolicyV1.Id), ("$result", "researchOnlyWorkbookCanonicalReviewBlocked"), ("$now", now), ("$id", candidate.CandidateId));
        transaction.Commit();
    }

    private static ResearchWorkbookInspection Inspection(string candidateId, string artifactSha, string workbookSha,
        FinanceResearchDatasetItem[] datasets)
    {
        var states = datasets.Select(x => x.Capabilities.Any(y => y.State == ResearchEligibilityState.Eligible)
            ? ResearchEligibilityState.Eligible : x.Capabilities.Any(y => y.State == ResearchEligibilityState.EligibleWithLimitations)
                ? ResearchEligibilityState.EligibleWithLimitations : ResearchEligibilityState.Ineligible).ToArray();
        return new(candidateId, artifactSha, workbookSha, datasets.Length, datasets.Count(x => x.TechnicalQuality != DatasetEvidenceResult.Fail.ToString()),
            states.Count(x => x == ResearchEligibilityState.Eligible), states.Count(x => x == ResearchEligibilityState.EligibleWithLimitations),
            states.Count(x => x == ResearchEligibilityState.Ineligible), datasets);
    }

    private static string ArtifactHashFromFile(string path) { using var stream = File.OpenRead(path); return DatasetContentIdentity.Sha256(stream); }
    private static string CompareResearchDataset(SqliteConnection connection, ParsedResearchDataset dataset)
    {
        if (dataset.DatasetClass != ResearchDatasetClass.DailyOhlcv) return DatasetComparisonClass.InsufficientOverlap.ToString();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT symbol,session_date,open,high,low,close,volume FROM observations WHERE symbol=$symbol AND provider IN ('EODHD','NASDAQ-WIKI') ORDER BY acquired_utc,revision_id";
        command.Parameters.AddWithValue("$symbol", dataset.Symbol); using var reader = command.ExecuteReader();
        var canonical = new List<DatasetComparableBar>();
        while (reader.Read()) canonical.Add(new(reader.GetString(0), DateOnly.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
            decimal.Parse(reader.GetString(2), CultureInfo.InvariantCulture), decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
            decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture), decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
            reader.GetInt64(6), DatasetPriceBasis.Raw));
        var owner = dataset.Observations.Select(x => new DatasetComparableBar(dataset.Symbol, x.Date, x.Open!.Value, x.High!.Value,
            x.Low!.Value, x.Close, x.Volume ?? 0, DatasetPriceBasis.Unclear));
        return DatasetCrossSourceComparerV1.Compare(owner, canonical).Classification.ToString();
    }
    private static bool TryWorkbookDate(string value, out DateOnly date)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial))
        {
            try { date = DateOnly.FromDateTime(DateTime.FromOADate(Math.Floor(serial))); return true; } catch (ArgumentException) { }
        }
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) return true;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var timestamp))
        { date = DateOnly.FromDateTime(timestamp); return true; }
        return false;
    }
    private sealed record WorkbookParseResult(string WorkbookSha, List<ParsedResearchDataset> Datasets);
    private sealed record ResearchObservation(DateOnly Date, decimal? Open, decimal? High, decimal? Low, decimal Close, long? Volume, int SourceRow);
    private sealed record ParsedResearchDataset(string RevisionId, string DatasetId, string SheetName, string Source,
        string InstrumentType, string Symbol, string Venue, ResearchDatasetClass DatasetClass, string ArtifactSha,
        string WorkbookSha, string Fingerprint, List<ResearchObservation> Observations, long TotalRows, long Duplicates,
        long Conflicts, long MissingValues, long InvalidDates, long NonPositivePrices, long InconsistentOhlc,
        long InvalidVolume, long OutOfOrderRows, long MissingSessions, long SuspiciousDiscontinuities, long SplitLikeJumps,
        DatasetEvidenceResult Technical, DatasetOwnerRightsDecision OwnerDecision,
        DatasetEvidenceResult ExternalRights, string PriceBasisClaim, ResearchDatasetEligibility Eligibility, List<string> Limitations);
}

public interface IFinanceResearchDatasetReader { FinanceResearchDatasetCatalog GetCatalog(); }
internal sealed class FinanceResearchDatasetReader(FinanceDatasetIntakeStore store) : IFinanceResearchDatasetReader
{ public FinanceResearchDatasetCatalog GetCatalog() => store.ResearchCatalog(); }
