using Microsoft.Data.Sqlite;
using System.Globalization;

namespace BigBrain.Api.SystemRecovery;

public enum RuntimeLifecycleState { Starting, Recovering, Healthy, Degraded, Quiescing, Stopping, RecoveryRequired }
public enum PreviousShutdownState { Unknown, Clean, Unclean }
public enum RecoveryComponentState { Healthy, Degraded, Recovering, Unavailable }
public enum MissedRunPolicy { CatchUpOnce, SkipToNext, Manual, DerivedFromSourceState }

public sealed record SystemRecoveryOptions
{
    public const string SectionName = "SystemRecovery";
    public string DatabasePath { get; set; } = Path.Combine(Path.GetTempPath(), "bigbrain-recovery", "lifecycle.db");
    public string ClockSyncDirectory { get; set; } = "/host-timesync";
    public long LowDiskWarningBytes { get; set; } = 10L * 1024 * 1024 * 1024;
    public long LowDiskCriticalBytes { get; set; } = 2L * 1024 * 1024 * 1024;
}

public sealed record RecoveryStoreDefinition(string Id, string Owner, string Kind, string Path,
    bool Critical, string IntegrityMechanism, string BackupStatus, string RecoveryPolicy);
public sealed record RecoveryComponent(string Id, RecoveryComponentState State, bool Critical,
    string Summary, DateTimeOffset CheckedAtUtc);
public sealed record RecoveryAction(string Code, string Outcome, DateTimeOffset AtUtc);
public sealed record ScheduledJobPolicy(string Job, MissedRunPolicy Policy, string Reason);
public sealed record SystemRecoverySnapshot(RuntimeLifecycleState Overall, string BootId,
    DateTimeOffset BootedAtUtc, PreviousShutdownState PreviousShutdown, bool RecoveryCompleted,
    bool ClockSynchronized, string ClockSource, long? AvailableBytes, bool LowDisk,
    DateTimeOffset? LastCleanShutdownUtc, DateTimeOffset? LastIntegrityCheckUtc,
    IReadOnlyList<RecoveryComponent> Components, IReadOnlyList<RecoveryAction> RecoveryActions,
    IReadOnlyList<ScheduledJobPolicy> ScheduledJobs, int InterruptedJobs, string OperatingMode);

public sealed class SystemRecoveryCoordinator : BackgroundService
{
    private static readonly Action<ILogger, Exception?> CleanShutdownLog = LoggerMessage.Define(
        LogLevel.Information, new EventId(8201, "CleanShutdown"), "BigBrain lifecycle clean shutdown marker committed.");
    private static readonly Action<ILogger, RuntimeLifecycleState, PreviousShutdownState, Exception?> RecoveryLog = LoggerMessage.Define<RuntimeLifecycleState, PreviousShutdownState>(
        LogLevel.Information, new EventId(8202, "RecoveryCompleted"), "BigBrain recovery completed with state {State} and previous shutdown {Previous}.");
    private readonly SystemRecoveryOptions _options;
    private readonly ILogger<SystemRecoveryCoordinator> _logger;
    private readonly string _bootId;
    private readonly string _sessionId = Guid.NewGuid().ToString("N");
    private readonly DateTimeOffset _bootedAtUtc;
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object _gate = new();
    private RuntimeLifecycleState _state = RuntimeLifecycleState.Starting;
    private PreviousShutdownState _previous;
    private DateTimeOffset? _lastClean;
    private DateTimeOffset? _lastIntegrity;
    private bool _clockSynchronized;
    private long? _availableBytes;
    private readonly List<RecoveryComponent> _components = [];
    private readonly List<RecoveryAction> _actions = [];

    public static readonly ScheduledJobPolicy[] JobPolicies =
    [
        new("finance-eodhd-daily", MissedRunPolicy.CatchUpOnce, "Quota-safe only after durable journal, time and entitlement gates."),
        new("finance-features", MissedRunPolicy.DerivedFromSourceState, "Build only from a committed market revision."),
        new("finance-backtests", MissedRunPolicy.DerivedFromSourceState, "Build only from exact committed market and feature revisions."),
        new("finance-robustness", MissedRunPolicy.DerivedFromSourceState, "Build only from exact committed runs and revisions."),
        new("media-refresh", MissedRunPolicy.SkipToNext, "External integrations recover on their next bounded poll."),
        new("deep-integrity-check", MissedRunPolicy.Manual, "Potentially expensive and never required for local read availability.")
    ];

    public SystemRecoveryCoordinator(SystemRecoveryOptions options, ILogger<SystemRecoveryCoordinator> logger)
    {
        _options = options;
        _logger = logger;
        _bootedAtUtc = DateTimeOffset.UtcNow;
        _bootId = ReadBootId();
        Initialize();
        (_previous, _lastClean) = ReadPreviousSession();
        StartSession(); // Clean is deliberately false until bounded graceful shutdown completes.
    }

    public Task WaitUntilRecoveredAsync(CancellationToken cancellationToken) => _ready.Task.WaitAsync(cancellationToken);
    public bool MayStartTimeSensitiveWork => _ready.Task.IsCompletedSuccessfully && _clockSynchronized && _state is RuntimeLifecycleState.Healthy or RuntimeLifecycleState.Degraded;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lock (_gate) _state = _previous == PreviousShutdownState.Clean ? RuntimeLifecycleState.Starting : RuntimeLifecycleState.Recovering;
        Journal("startup.previous-shutdown", _previous.ToString().ToUpperInvariant());
        RunFastRecovery();
        _ready.TrySetResult();
        return Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_gate) _state = RuntimeLifecycleState.Quiescing;
        Journal("shutdown.quiescing", "started");
        _ready.TrySetCanceled(cancellationToken);
        try { await base.StopAsync(cancellationToken); }
        finally
        {
            lock (_gate) _state = RuntimeLifecycleState.Stopping;
            MarkClean();
            Journal("shutdown.clean", "completed");
            CleanShutdownLog(_logger, null);
        }
    }

    public SystemRecoverySnapshot Snapshot()
    {
        lock (_gate)
        {
            return new(_state, _bootId, _bootedAtUtc, _previous, _ready.Task.IsCompletedSuccessfully,
                _clockSynchronized, "systemd-timesync-marker", _availableBytes,
                _availableBytes is { } value && value < _options.LowDiskWarningBytes,
                _lastClean, _lastIntegrity, _components.ToArray(), _actions.TakeLast(32).ToArray(),
                JobPolicies, _components.Count(x => x.Summary.Contains("interrupted", StringComparison.OrdinalIgnoreCase)), "RESEARCH");
        }
    }

    private void RunFastRecovery()
    {
        var now = DateTimeOffset.UtcNow;
        var definitions = StoreDefinitions();
        var results = new List<RecoveryComponent>();
        foreach (var store in definitions)
        {
            var state = RecoveryComponentState.Healthy;
            var summary = "Fast open/write check passed.";
            try
            {
                var directory = store.Kind == "sqlite" ? Path.GetDirectoryName(store.Path)! : store.Path;
                Directory.CreateDirectory(directory);
                var probe = Path.Combine(directory, $".recovery-{Guid.NewGuid():N}.tmp");
                using (var stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1,
                    FileOptions.WriteThrough)) { stream.WriteByte(1); stream.Flush(true); }
                File.Delete(probe);
                if (store.Kind == "sqlite" && File.Exists(store.Path))
                {
                    using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = store.Path, Mode = SqliteOpenMode.ReadOnly }.ToString());
                    connection.Open();
                    using var command = connection.CreateCommand(); command.CommandText = "PRAGMA quick_check(1);";
                    if (!string.Equals(command.ExecuteScalar() as string, "ok", StringComparison.Ordinal)) throw new InvalidDataException("SQLite quick check failed.");
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SqliteException or InvalidDataException)
            { state = store.Critical ? RecoveryComponentState.Unavailable : RecoveryComponentState.Degraded; summary = $"Fast check failed: {exception.GetType().Name}."; }
            results.Add(new(store.Id, state, store.Critical, summary, now));
        }
        var root = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(_options.DatabasePath))!);
        _availableBytes = root.AvailableFreeSpace;
        _clockSynchronized = Directory.Exists(_options.ClockSyncDirectory) &&
            (File.Exists(Path.Combine(_options.ClockSyncDirectory, "synchronized")) || DateTimeOffset.UtcNow.Year >= 2025);
        results.Add(new("clock", _clockSynchronized ? RecoveryComponentState.Healthy : RecoveryComponentState.Degraded, false,
            _clockSynchronized ? "Clock synchronization marker or bounded sanity gate passed." : "Time-sensitive workers remain blocked.", now));
        var availableBytes = _availableBytes ?? 0;
        results.Add(new("disk", availableBytes < _options.LowDiskCriticalBytes ? RecoveryComponentState.Unavailable : availableBytes < _options.LowDiskWarningBytes ? RecoveryComponentState.Degraded : RecoveryComponentState.Healthy,
            true, $"Available bytes: {availableBytes.ToString(CultureInfo.InvariantCulture)}.", now));
        lock (_gate)
        {
            _components.Clear(); _components.AddRange(results); _lastIntegrity = now;
            var criticalFailure = results.Any(x => x.Critical && x.State == RecoveryComponentState.Unavailable);
            _state = criticalFailure ? RuntimeLifecycleState.RecoveryRequired : results.Any(x => x.State != RecoveryComponentState.Healthy) ? RuntimeLifecycleState.Degraded : RuntimeLifecycleState.Healthy;
            _actions.Add(new("fast-integrity-check", criticalFailure ? "recovery-required" : "completed", now));
        }
        Journal("startup.fast-recovery", _state.ToString().ToUpperInvariant());
        RecoveryLog(_logger, _state, _previous, null);
    }

    private RecoveryStoreDefinition[] StoreDefinitions()
    {
        return
        [
            new("lifecycle", "System", "sqlite", _options.DatabasePath, true, "SQLite quick_check + transactions", "Host-local; sanitized journal", "Fail closed, never recreate silently"),
            new("meal-planner", "Meal Planner", "directory", "/data", false, "SQLite transactions", "Not automated", "Degrade module"),
            new("shopping-list", "Shopping List", "directory", "/shopping-data", false, "SQLite transactions", "Not automated", "Degrade module"),
            new("calendar", "Calendar", "directory", "/calendar-data", false, "SQLite transactions", "Not automated", "Degrade module"),
            new("settings", "Settings", "directory", "/settings-data", false, "SQLite transactions", "Not automated", "Degrade module"),
            new("finance-memory", "Finance", "directory", "/finance-data", false, "SQLite WAL + checksummed immutable payloads", "Excluded; EODHD deletion-bound", "Read-only degraded; never recreate")
        ];
    }

    private void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_options.DatabasePath)!);
        using var c = Open(); using var command = c.CreateCommand(); command.CommandText = """
            PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;
            CREATE TABLE IF NOT EXISTS sessions(session_id TEXT PRIMARY KEY,boot_id TEXT NOT NULL,started_utc TEXT NOT NULL,clean INTEGER NOT NULL DEFAULT 0,clean_utc TEXT);
            CREATE TABLE IF NOT EXISTS events(id INTEGER PRIMARY KEY AUTOINCREMENT,boot_id TEXT NOT NULL,at_utc TEXT NOT NULL,code TEXT NOT NULL,outcome TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS ix_events_boot ON events(boot_id,id);
            """; command.ExecuteNonQuery();
    }
    private (PreviousShutdownState, DateTimeOffset?) ReadPreviousSession()
    {
        using var c = Open(); using var command = c.CreateCommand(); command.CommandText = "SELECT clean,clean_utc FROM sessions WHERE session_id<>$id ORDER BY started_utc DESC LIMIT 1"; command.Parameters.AddWithValue("$id", _sessionId);
        using var reader = command.ExecuteReader(); if (!reader.Read()) return (PreviousShutdownState.Unknown, null);
        return (reader.GetInt32(0) == 1 ? PreviousShutdownState.Clean : PreviousShutdownState.Unclean,
            reader.IsDBNull(1) ? null : DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture));
    }
    private void StartSession() { using var c = Open(); using var command = c.CreateCommand(); command.CommandText = "INSERT INTO sessions(session_id,boot_id,started_utc,clean,clean_utc) VALUES($session,$boot,$at,0,NULL)"; command.Parameters.AddWithValue("$session", _sessionId); command.Parameters.AddWithValue("$boot", _bootId); command.Parameters.AddWithValue("$at", _bootedAtUtc.ToString("O")); command.ExecuteNonQuery(); }
    private void MarkClean() { var now=DateTimeOffset.UtcNow; using var c=Open(); using var command=c.CreateCommand(); command.CommandText="UPDATE sessions SET clean=1,clean_utc=$at WHERE session_id=$id";command.Parameters.AddWithValue("$id",_sessionId);command.Parameters.AddWithValue("$at",now.ToString("O"));command.ExecuteNonQuery();_lastClean=now; }
    private void Journal(string code,string outcome){using var c=Open();using var command=c.CreateCommand();command.CommandText="INSERT INTO events(boot_id,at_utc,code,outcome) VALUES($id,$at,$code,$outcome)";command.Parameters.AddWithValue("$id",_bootId);command.Parameters.AddWithValue("$at",DateTimeOffset.UtcNow.ToString("O"));command.Parameters.AddWithValue("$code",code);command.Parameters.AddWithValue("$outcome",outcome);command.ExecuteNonQuery();}
    private SqliteConnection Open(){var c=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=_options.DatabasePath}.ToString());c.Open();return c;}
    private static string ReadBootId(){try{return File.ReadAllText("/proc/sys/kernel/random/boot_id").Trim();}catch{return $"process-{Guid.NewGuid():N}";}}
}
