using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

public sealed record FinanceDataProtectionOptions
{
    public const string Section = "Finance:DataProtection";
    public string BackupDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "bigbrain-finance", "backups");
    public string RestoreStagingDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "bigbrain-finance", "restore-staging");
    public long MinimumFreeBytesAfterOperation { get; set; } = 1_000_000_000;
    public int RejectedQuarantineRetentionDays { get; set; } = 30;
}

public enum FinanceBackupEligibility { Eligible, Restricted, Excluded }
public sealed record FinanceSourceProtection(string Provider, string Product, string RightsClass,
    string RetentionClass, string DeletionRequirement, DateTimeOffset? DeletionDeadlineUtc,
    FinanceBackupEligibility BackupEligibility, bool RestoreEligible, string Reason);
public sealed record FinanceBackupRevision(string RevisionId, string Provider, string Product, string Policy,
    string Checksum, long ObservationCount, string? CoverageFrom, string? CoverageTo);
public sealed record FinanceBackupArtifact(string Path, long Bytes, string Sha256);
public sealed record FinanceBackupManifest(string BackupId, DateTimeOffset CreatedAtUtc, string SchemaVersion,
    string BigBrainVersion, string Status, IReadOnlyList<FinanceSourceProtection> Sources,
    IReadOnlyList<FinanceBackupRevision> Revisions, IReadOnlyList<string> FeatureRevisionIds,
    IReadOnlyList<string> BacktestRunIds, IReadOnlyList<string> RobustnessEvaluationIds,
    IReadOnlyList<FinanceBackupArtifact> Artifacts, string ContentFingerprint);
public sealed record FinanceBackupInventory(DateTimeOffset GeneratedAtUtc, string OperatingMode,
    IReadOnlyList<FinanceBackupManifest> Backups, IReadOnlyList<FinanceSourceProtection> SourcePolicies);
public sealed record FinanceRestoreDrillResult(string BackupId, bool Verified, bool RestoredIdentityMatches,
    int RevisionCount, long ObservationCount, string ContentFingerprint, string StagingState);
public sealed record FinanceCorruptionDrillResult(string BackupId, bool ChecksumMismatchDetected, bool RestoreRejected, string StagingState);

internal static class FinanceBackupPolicyV1
{
    internal const string Version = "finance-provider-backup-v1";

    internal static FinanceSourceProtection Classify(string provider, string product, string policy,
        string? licenseClass, string? provenanceResult, string? candidateState, DateTimeOffset? entitlementEndsAtUtc)
    {
        if (provider == "NASDAQ-WIKI" && licenseClass == "PublicDomain" && provenanceResult == "Pass" && candidateState == "Promoted")
            return new(provider, product, "PublicDomain", "Indefinite", "None", null,
                FinanceBackupEligibility.Eligible, true, "Verified public-domain promoted evidence.");
        if (provider == EodhdMarketMemory.Provider && product == EodhdMarketMemory.Product && policy == EodhdMarketMemory.Policy)
            return new(provider, product, "OwnerAcceptedPersonalResearch", "SubscriptionOnly", "DeleteAtSubscriptionEnd",
                entitlementEndsAtUtc?.AddMonths(1), FinanceBackupEligibility.Restricted, true,
                "Provider-tagged copies remain in the EODHD deletion inventory and may not enter an indefinite backup.");
        if (candidateState == "Promoted" && provenanceResult == "Pass" && licenseClass is "Cc0" or "CcBy" or "CompatibleOther")
            return new(provider, product, licenseClass, licenseClass == "CcBy" ? "AttributionRequired" : "Indefinite", "None", null,
                FinanceBackupEligibility.Eligible, true, "Compatible promoted evidence with preserved attribution and lineage.");
        return new(provider, product, licenseClass ?? "Unknown", "Unknown", "FailClosed", null,
            FinanceBackupEligibility.Excluded, false, "Rights, provenance or canonical promotion is unresolved.");
    }
}

internal sealed class FinanceDataProtectionStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly EodhdFinanceOptions _market;
    private readonly FinanceDataProtectionOptions _options;

    public FinanceDataProtectionStore(EodhdFinanceOptions market, FinanceDataProtectionOptions options)
    {
        _market = market; _options = options;
        Directory.CreateDirectory(options.BackupDirectory);
        Directory.CreateDirectory(options.RestoreStagingDirectory);
        foreach (var directory in Directory.EnumerateDirectories(options.BackupDirectory, ".staging-*")) Directory.Delete(directory, true);
        foreach (var directory in Directory.EnumerateDirectories(options.RestoreStagingDirectory, ".staging-*")) Directory.Delete(directory, true);
    }

    private string ConnectionString => new SqliteConnectionStringBuilder { DataSource = _market.DatabasePath }.ToString();

    internal FinanceBackupInventory Inventory()
    {
        var backups = Directory.EnumerateFiles(_options.BackupDirectory, "*.manifest.json")
            .Select(path => JsonSerializer.Deserialize<FinanceBackupManifest>(File.ReadAllText(path), Json))
            .Where(value => value is { Status: "Complete" }).Cast<FinanceBackupManifest>()
            .OrderByDescending(value => value.CreatedAtUtc).ThenBy(value => value.BackupId, StringComparer.Ordinal).ToArray();
        return new(DateTimeOffset.UtcNow, "RESEARCH", backups, ClassifySources());
    }

    internal FinanceBackupManifest CreatePublicDomainBackup(DateTimeOffset createdAtUtc, string version)
    {
        var sources = ClassifySources();
        var included = sources.Where(x => x.BackupEligibility == FinanceBackupEligibility.Eligible && x.RightsClass == "PublicDomain").ToArray();
        if (included.Length == 0) throw new InvalidOperationException("No verified public-domain Finance revision is backup-eligible.");
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        var revisionIds = RevisionIds(connection, included.Select(x => x.Provider).ToHashSet(StringComparer.Ordinal));
        var payload = BuildPayload(connection, revisionIds);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, Json); var fingerprint = Sha(bytes);
        var backupId = $"finance-backup-{fingerprint[7..23]}";
        var existing = Path.Combine(_options.BackupDirectory, backupId + ".manifest.json");
        if (File.Exists(existing)) return JsonSerializer.Deserialize<FinanceBackupManifest>(File.ReadAllText(existing), Json)!;
        EnsureDisk(bytes.LongLength * 2);
        var staging = Path.Combine(_options.BackupDirectory, $".staging-{backupId}-{Guid.NewGuid():N}"); Directory.CreateDirectory(staging);
        try
        {
            var dataName = backupId + ".data.json"; var stagedData = Path.Combine(staging, dataName); File.WriteAllBytes(stagedData, bytes);
            if (Sha(File.ReadAllBytes(stagedData)) != fingerprint) throw new InvalidDataException("Backup staging checksum mismatch.");
            var manifest = new FinanceBackupManifest(backupId, createdAtUtc, FinanceBackupPolicyV1.Version, version, "Complete",
                included.OrderBy(x => x.Provider, StringComparer.Ordinal).ToArray(), payload.Revisions,
                payload.FeatureRevisionIds, payload.BacktestRunIds, payload.RobustnessEvaluationIds,
                [new(dataName, bytes.LongLength, fingerprint)], fingerprint);
            var stagedManifest = Path.Combine(staging, backupId + ".manifest.json"); File.WriteAllText(stagedManifest, JsonSerializer.Serialize(manifest, Json), new UTF8Encoding(false));
            File.Move(stagedData, Path.Combine(_options.BackupDirectory, dataName), false);
            File.Move(stagedManifest, existing, false);
            return manifest;
        }
        finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
    }

    internal bool Verify(string backupId)
    {
        var manifest = ReadManifest(backupId); if (manifest.Status != "Complete" || manifest.Artifacts.Count != 1) return false;
        var artifact = manifest.Artifacts[0]; var path = Path.Combine(_options.BackupDirectory, artifact.Path);
        if (!File.Exists(path) || new FileInfo(path).Length != artifact.Bytes) return false;
        var bytes = File.ReadAllBytes(path); if (Sha(bytes) != artifact.Sha256 || artifact.Sha256 != manifest.ContentFingerprint) return false;
        var payload = JsonSerializer.Deserialize<BackupPayload>(bytes, Json); return payload is not null &&
            payload.Revisions.Select(x => x.RevisionId).SequenceEqual(manifest.Revisions.Select(x => x.RevisionId), StringComparer.Ordinal) &&
            payload.Observations.Count == manifest.Revisions.Sum(x => x.ObservationCount);
    }

    internal FinanceRestoreDrillResult DrillRestore(string backupId)
    {
        if (!Verify(backupId)) throw new InvalidDataException("Backup verification failed; restore is rejected.");
        var manifest = ReadManifest(backupId); var artifact = manifest.Artifacts.Single(); EnsureDisk(artifact.Bytes);
        var staging = Path.Combine(_options.RestoreStagingDirectory, $".staging-{backupId}-{Guid.NewGuid():N}"); Directory.CreateDirectory(staging);
        try
        {
            var copy = Path.Combine(staging, artifact.Path); File.Copy(Path.Combine(_options.BackupDirectory, artifact.Path), copy);
            var bytes = File.ReadAllBytes(copy); if (Sha(bytes) != artifact.Sha256) throw new InvalidDataException("Restore staging checksum mismatch.");
            var payload = JsonSerializer.Deserialize<BackupPayload>(bytes, Json) ?? throw new InvalidDataException("Restore payload is invalid.");
            var identity = payload.Revisions.SequenceEqual(manifest.Revisions) && payload.Observations.Count == manifest.Revisions.Sum(x => x.ObservationCount);
            return new(backupId, true, identity, payload.Revisions.Count, payload.Observations.Count, manifest.ContentFingerprint, "VerifiedIsolatedThenRemoved");
        }
        finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
    }

    internal FinanceCorruptionDrillResult DrillCorruption(string backupId)
    {
        if (!Verify(backupId)) throw new InvalidDataException("Original backup must verify before the corruption drill.");
        var manifest=ReadManifest(backupId);var artifact=manifest.Artifacts.Single();EnsureDisk(artifact.Bytes);var staging=Path.Combine(_options.RestoreStagingDirectory,$".staging-corruption-{backupId}-{Guid.NewGuid():N}");Directory.CreateDirectory(staging);
        try{var copy=Path.Combine(staging,artifact.Path);File.Copy(Path.Combine(_options.BackupDirectory,artifact.Path),copy);using(var stream=new FileStream(copy,FileMode.Append,FileAccess.Write,FileShare.None))stream.WriteByte(0x00);var mismatch=Sha(File.ReadAllBytes(copy))!=artifact.Sha256;return new(backupId,mismatch,mismatch,"CorruptedCopyRejectedThenRemoved");}
        finally{if(Directory.Exists(staging))Directory.Delete(staging,true);}
    }

    private FinanceBackupManifest ReadManifest(string backupId)
    {
        if (string.IsNullOrWhiteSpace(backupId) || backupId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new ArgumentException("Invalid backup ID.");
        var path = Path.Combine(_options.BackupDirectory, backupId + ".manifest.json");
        return File.Exists(path) ? JsonSerializer.Deserialize<FinanceBackupManifest>(File.ReadAllText(path), Json)! : throw new KeyNotFoundException("Backup does not exist.");
    }

    private List<FinanceSourceProtection> ClassifySources()
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open(); var values = new List<FinanceSourceProtection>();
        using var command = connection.CreateCommand(); command.CommandText = TableExists(connection,"dataset_candidates") ? """
          SELECT o.provider,o.product,o.policy,c.license_class,c.provenance_result,c.state FROM observations o
          LEFT JOIN dataset_candidates c ON c.canonical_revision_id=o.revision_id
          GROUP BY o.provider,o.product,o.policy,c.license_class,c.provenance_result,c.state ORDER BY o.provider,o.product,o.policy
          """ : "SELECT provider,product,policy,NULL,NULL,NULL FROM observations GROUP BY provider,product,policy ORDER BY provider,product,policy";
        using var reader = command.ExecuteReader(); while (reader.Read()) values.Add(FinanceBackupPolicyV1.Classify(reader.GetString(0), reader.GetString(1), reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5), _market.EntitlementEndsAtUtc));
        return values;
    }

    private static string[] RevisionIds(SqliteConnection connection, HashSet<string> providers)
    {
        using var command = connection.CreateCommand(); command.CommandText = "SELECT DISTINCT revision_id,provider FROM observations ORDER BY revision_id";
        using var reader = command.ExecuteReader(); var ids = new List<string>(); while (reader.Read()) if (providers.Contains(reader.GetString(1))) ids.Add(reader.GetString(0)); return ids.ToArray();
    }

    private static BackupPayload BuildPayload(SqliteConnection connection, IReadOnlyList<string> revisionIds)
    {
        var revisionSet = revisionIds.ToHashSet(StringComparer.Ordinal); var observations = Rows(connection, "observations", "revision_id", revisionSet);
        var revisions = new List<FinanceBackupRevision>();
        foreach (var id in revisionIds.Order(StringComparer.Ordinal))
        {
            var rows = observations.Where(x => x["revision_id"] == id).ToArray(); using var command = connection.CreateCommand();
            command.CommandText = "SELECT checksum,created_utc,observation_count FROM revisions WHERE revision_id=$id"; command.Parameters.AddWithValue("$id", id); using var reader = command.ExecuteReader(); reader.Read();
            revisions.Add(new(id, rows[0]["provider"]!, rows[0]["product"]!, rows[0]["policy"]!, reader.GetString(0), rows.LongLength,
                rows.Min(x => x["session_date"]), rows.Max(x => x["session_date"])));
        }
        var featureIds = RelatedIds(connection, "feature_revisions", "revision_id", "source_revisions_json", revisionSet);
        var backtestIds = RelatedIds(connection, "backtest_runs", "run_id", "market_revisions_json", revisionSet, "feature_revision_id", featureIds);
        var robustnessIds = RelatedIds(connection, "robustness_evaluations", "evaluation_id", "market_revisions_json", revisionSet, "feature_revision_id", featureIds);
        var featureSet=featureIds.ToHashSet(StringComparer.Ordinal);var backtestSet=backtestIds.ToHashSet(StringComparer.Ordinal);var robustnessSet=robustnessIds.ToHashSet(StringComparer.Ordinal);
        var researchExperiments=Rows(connection,"research_experiments","robustness_evaluation_id",robustnessSet);
        var researchExperimentSet=researchExperiments.Select(x=>x["experiment_id"]!).ToHashSet(StringComparer.Ordinal);
        var researchHypothesisSet=researchExperiments.Select(x=>x["hypothesis_id"]!).ToHashSet(StringComparer.Ordinal);
        var researchRunSet=EligibleResearchRunIds(connection,researchExperimentSet);
        var tables = new List<BackupTable>
        {
            new("dataset_candidates", Rows(connection,"dataset_candidates","canonical_revision_id",revisionSet)),
            new("dataset_corporate_actions", RowsForCandidates(connection, revisionSet)),
            new("feature_revisions", Rows(connection,"feature_revisions","revision_id",featureSet)),
            new("feature_values", Rows(connection,"feature_values","revision_id",featureSet)),
            new("backtest_runs", Rows(connection,"backtest_runs","run_id",backtestSet)),
            new("backtest_events", Rows(connection,"backtest_events","run_id",backtestSet)),
            new("backtest_fills", Rows(connection,"backtest_fills","run_id",backtestSet)),
            new("backtest_equity", Rows(connection,"backtest_equity","run_id",backtestSet)),
            new("robustness_evaluations", Rows(connection,"robustness_evaluations","evaluation_id",robustnessSet)),
            new("robustness_run_references", Rows(connection,"robustness_run_references","evaluation_id",robustnessSet)),
            new("robustness_windows", Rows(connection,"robustness_windows","evaluation_id",robustnessSet)),
            new("robustness_parameter_sensitivity", Rows(connection,"robustness_parameter_sensitivity","evaluation_id",robustnessSet)),
            new("robustness_cost_sensitivity", Rows(connection,"robustness_cost_sensitivity","evaluation_id",robustnessSet)),
            new("research_hypotheses", Rows(connection,"research_hypotheses","hypothesis_id",researchHypothesisSet)),
            new("research_experiments", researchExperiments),
            new("research_runs", Rows(connection,"research_runs","run_id",researchRunSet)),
            new("research_run_experiments", Rows(connection,"research_run_experiments","run_id",researchRunSet))
        };
        return new(FinanceBackupPolicyV1.Version, revisions, observations, featureIds, backtestIds, robustnessIds, tables);
    }

    private static string[] RelatedIds(SqliteConnection connection, string table, string idColumn, string revisionsColumn,
        HashSet<string> revisions, string? featureColumn = null, IReadOnlyCollection<string>? featureIds = null)
    {
        if (!TableExists(connection, table)) return [];
        using var command = connection.CreateCommand(); command.CommandText = $"SELECT {idColumn},{revisionsColumn}{(featureColumn is null ? "" : "," + featureColumn)} FROM {table} ORDER BY {idColumn}";
        using var reader = command.ExecuteReader(); var ids = new List<string>(); while (reader.Read())
        {
            var source = JsonSerializer.Deserialize<string[]>(reader.GetString(1), Json) ?? [];
            if (source.Length > 0 && source.All(revisions.Contains) && (featureColumn is null || featureIds!.Contains(reader.GetString(2), StringComparer.Ordinal))) ids.Add(reader.GetString(0));
        }
        return ids.ToArray();
    }

    private static List<SortedDictionary<string,string?>> RowsForCandidates(SqliteConnection connection, HashSet<string> revisions)
    {
        if (!TableExists(connection,"dataset_corporate_actions")) return [];
        var candidates = Rows(connection,"dataset_candidates","canonical_revision_id",revisions).Select(x => x["candidate_id"]!).ToHashSet(StringComparer.Ordinal);
        return Rows(connection,"dataset_corporate_actions","candidate_id",candidates);
    }

    private static HashSet<string> EligibleResearchRunIds(SqliteConnection connection, HashSet<string> eligibleExperiments)
    {
        var eligibleRuns=new HashSet<string>(StringComparer.Ordinal);
        if(eligibleExperiments.Count==0||!TableExists(connection,"research_run_experiments"))return eligibleRuns;
        using var command=connection.CreateCommand();command.CommandText="SELECT run_id,experiment_id FROM research_run_experiments ORDER BY run_id,ordinal";
        using var reader=command.ExecuteReader();var runs=new Dictionary<string,List<string>>(StringComparer.Ordinal);
        while(reader.Read()){var run=reader.GetString(0);if(!runs.TryGetValue(run,out var experiments)){experiments=[];runs.Add(run,experiments);}experiments.Add(reader.GetString(1));}
        foreach(var run in runs)if(run.Value.Count>0&&run.Value.All(eligibleExperiments.Contains))eligibleRuns.Add(run.Key);
        return eligibleRuns;
    }

    private static List<SortedDictionary<string,string?>> Rows(SqliteConnection connection, string table, string filterColumn, HashSet<string> accepted)
    {
        if (accepted.Count == 0 || !TableExists(connection, table)) return [];
        using var command = connection.CreateCommand(); command.CommandText = $"SELECT * FROM {table} ORDER BY {filterColumn}"; using var reader = command.ExecuteReader(); var rows = new List<SortedDictionary<string,string?>>();
        while (reader.Read()) if (!reader.IsDBNull(reader.GetOrdinal(filterColumn)) && accepted.Contains(reader.GetString(reader.GetOrdinal(filterColumn))))
        {
            var row = new SortedDictionary<string,string?>(StringComparer.Ordinal); for (var i=0;i<reader.FieldCount;i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture); rows.Add(row);
        }
        return rows.OrderBy(x => string.Join('\u001f', x.Values), StringComparer.Ordinal).ToList();
    }

    private static bool TableExists(SqliteConnection connection, string table)
    { using var command=connection.CreateCommand();command.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";command.Parameters.AddWithValue("$name",table);return Convert.ToInt32(command.ExecuteScalar(),CultureInfo.InvariantCulture)>0; }
    private void EnsureDisk(long required) { var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(_options.BackupDirectory))!); if (drive.AvailableFreeSpace-required < _options.MinimumFreeBytesAfterOperation) throw new IOException("Finance backup/restore blocked by disk safety gate."); }
    private static string Sha(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private sealed record BackupPayload(string SchemaVersion, IReadOnlyList<FinanceBackupRevision> Revisions,
        IReadOnlyList<SortedDictionary<string,string?>> Observations, IReadOnlyList<string> FeatureRevisionIds,
        IReadOnlyList<string> BacktestRunIds, IReadOnlyList<string> RobustnessEvaluationIds, IReadOnlyList<BackupTable> Tables);
    private sealed record BackupTable(string Name, IReadOnlyList<SortedDictionary<string,string?>> Rows);
}

public interface IFinanceBackupReader { FinanceBackupInventory GetInventory(); }
internal sealed class FinanceBackupReader(FinanceDataProtectionStore store) : IFinanceBackupReader { public FinanceBackupInventory GetInventory() => store.Inventory(); }

internal static class FinanceDataProtectionMaintenanceCommand
{
    private static readonly JsonSerializerOptions Output = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    internal static bool TryRun(string[] args, IConfiguration configuration)
    {
        if (args.Length == 0 || !args[0].StartsWith("finance-backup-", StringComparison.Ordinal) && args[0] != "finance-quarantine-cleanup-drill") return false;
        var market = configuration.GetSection(EodhdFinanceOptions.Section).Get<EodhdFinanceOptions>() ?? new();
        var datasets = configuration.GetSection(FinanceDatasetOptions.Section).Get<FinanceDatasetOptions>() ?? new();
        var options = configuration.GetSection(FinanceDataProtectionOptions.Section).Get<FinanceDataProtectionOptions>() ?? new();
        _ = new EodhdMarketMemory(market); var protection = new FinanceDataProtectionStore(market, options);
        if (args[0] == "finance-backup-create" && args.Length == 1) { Console.WriteLine(JsonSerializer.Serialize(protection.CreatePublicDomainBackup(DateTimeOffset.UtcNow, "BB-085"), Output)); return true; }
        if (args[0] == "finance-backup-verify" && args.Length == 2) { Console.WriteLine($"backup={args[1]} verified={protection.Verify(args[1])}"); return true; }
        if (args[0] == "finance-backup-restore-drill" && args.Length == 2) { Console.WriteLine(JsonSerializer.Serialize(protection.DrillRestore(args[1]), Output)); return true; }
        if (args[0] == "finance-backup-corruption-drill" && args.Length == 2) { Console.WriteLine(JsonSerializer.Serialize(protection.DrillCorruption(args[1]), Output)); return true; }
        if (args[0] == "finance-backup-inventory" && args.Length == 1) { Console.WriteLine(JsonSerializer.Serialize(protection.Inventory(), Output)); return true; }
        if (args[0] == "finance-quarantine-cleanup-drill" && args.Length == 1)
        { var intake = new FinanceDatasetIntakeStore(market, datasets); var cutoff = DateTimeOffset.UtcNow.AddDays(-Math.Max(0, options.RejectedQuarantineRetentionDays)); Console.WriteLine(JsonSerializer.Serialize(intake.CleanupRejected(cutoff), Output)); return true; }
        throw new ArgumentException("Use finance-backup-create, finance-backup-inventory, finance-backup-verify <id>, finance-backup-restore-drill <id>, finance-backup-corruption-drill <id>, or finance-quarantine-cleanup-drill.");
    }
}
