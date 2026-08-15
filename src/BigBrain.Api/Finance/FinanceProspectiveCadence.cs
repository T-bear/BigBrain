using System.Globalization;
using System.Text.Json;
using BigBrain.Api.SystemRecovery;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

public sealed record FinanceCadenceOptions
{
    public const string Section = "Finance:Cadence";
    public int InternalCheckMinutes { get; set; } = 30;
    public int ProviderWindowStartUtcHour { get; set; } = 22;
}

internal sealed record FinanceCadenceSnapshot(bool Enabled,string Provider,string ObservationClass,string Health,
    DateTimeOffset? LastProviderCheckUtc,DateTimeOffset? LastSuccessfulAcquisitionUtc,DateOnly? LatestCanonicalSession,
    DateTimeOffset? LastPredictionUtc,DateTimeOffset? LastOutcomeUtc,int Pending,int Evaluated,int Invalidated,
    bool ClockIntegrity,string NextAction,string PollingPolicy,string OperatingMode);

internal static class FinanceCadenceSchedule
{
    internal static bool IsProviderWindow(DateTimeOffset nowUtc,int startHour) =>
        nowUtc.Offset==TimeSpan.Zero && nowUtc.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday && nowUtc.Hour>=startHour;
}

internal sealed partial class EodhdMarketMemory
{
    private static void InitializeCadenceStorage(SqliteConnection connection)
    {
        using var command=connection.CreateCommand();command.CommandText="""
          CREATE TABLE IF NOT EXISTS finance_cadence(
            singleton INTEGER PRIMARY KEY CHECK(singleton=1),last_check_utc TEXT,last_success_utc TEXT,
            last_outcome TEXT NOT NULL,latest_session TEXT,updated_utc TEXT NOT NULL);
          INSERT OR IGNORE INTO finance_cadence VALUES(1,NULL,NULL,'not-run',NULL,'1970-01-01T00:00:00Z');
          """;command.ExecuteNonQuery();
    }

    internal void RecordCadenceCheck(DateTimeOffset atUtc,bool providerChecked,bool acquisitionSucceeded,string outcome)
    {
        using var connection=new SqliteConnection(ConnectionString);connection.Open();
        var latest=OptionalDate(connection,"SELECT MAX(session_date) FROM observations WHERE provider='EODHD'");
        Execute(connection,null,"UPDATE finance_cadence SET last_check_utc=CASE WHEN $checked=1 THEN $at ELSE last_check_utc END,last_success_utc=CASE WHEN $success=1 THEN $at ELSE last_success_utc END,last_outcome=$outcome,latest_session=$session,updated_utc=$at WHERE singleton=1",
            ("$at",atUtc.ToString("O")),("$checked",providerChecked?1:0),("$success",acquisitionSucceeded?1:0),("$outcome",outcome),("$session",latest?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??(object)DBNull.Value));
    }

    internal FinanceCadenceSnapshot CadenceSnapshot(bool enabled,bool clockIntegrity,int startHour,int checkMinutes)
    {
        using var connection=new SqliteConnection(ConnectionString);connection.Open();using var command=connection.CreateCommand();
        command.CommandText="SELECT last_check_utc,last_success_utc,last_outcome,latest_session FROM finance_cadence WHERE singleton=1";
        using var reader=command.ExecuteReader();reader.Read();
        DateTimeOffset? lastCheck=reader.IsDBNull(0)?null:DateTimeOffset.Parse(reader.GetString(0),CultureInfo.InvariantCulture);
        DateTimeOffset? lastSuccess=reader.IsDBNull(1)?null:DateTimeOffset.Parse(reader.GetString(1),CultureInfo.InvariantCulture);
        var outcome=reader.GetString(2);DateOnly? session=reader.IsDBNull(3)?null:DateOnly.Parse(reader.GetString(3),CultureInfo.InvariantCulture);reader.Close();
        var shadow=ShadowCatalog(null,null,null,null,null,200);var now=DateTimeOffset.UtcNow;
        var invalidated=Scalar(connection,"SELECT COUNT(*) FROM shadow_predictions WHERE state='Invalidated'");
        var next=!enabled?"Cadence disabled":!clockIntegrity?"Waiting for clock integrity":FinanceCadenceSchedule.IsProviderWindow(now,startHour)?"Eligible for bounded provider check":"Waiting for next weekday EOD provider window";
        return new(enabled,Provider,"CURRENT EOD / PROSPECTIVE EOD",outcome.Contains("failure",StringComparison.Ordinal)?"Degraded":"Healthy",lastCheck,lastSuccess,session,
            shadow.Predictions.Select(x=>(DateTimeOffset?)x.CreatedAtUtc).Max(),TimeOrNull(connection,"SELECT MAX(evaluated_utc) FROM shadow_outcomes"),
            shadow.Pending,shadow.Evaluated,invalidated,clockIntegrity,next,
            $"internal check every {checkMinutes} minutes; at most one successful provider cycle per UTC day, weekdays after {startHour}:00 UTC","RESEARCH");
    }

    private static DateTimeOffset? TimeOrNull(SqliteConnection connection,string sql){using var command=connection.CreateCommand();command.CommandText=sql;var value=command.ExecuteScalar() as string;return value is null?null:DateTimeOffset.Parse(value,CultureInfo.InvariantCulture);}
    internal DateOnly? LatestEodhdSession(){using var connection=new SqliteConnection(ConnectionString);connection.Open();return OptionalDate(connection,"SELECT MAX(session_date) FROM observations WHERE provider='EODHD'");}
}

internal sealed class FinanceProspectiveCadenceWorker(EodhdFinanceOptions provider,FinanceCadenceOptions cadence,
    EodhdMarketMemory memory,SystemRecoveryCoordinator recovery,ILogger<FinanceProspectiveCadenceWorker> logger):BackgroundService
{
    private static readonly Action<ILogger,string,Exception?> Cycle=LoggerMessage.Define<string>(LogLevel.Information,new EventId(8801,"FinanceCadenceCycle"),"Finance cadence cycle: {Outcome}.");
    private static readonly Action<ILogger,string,Exception?> Failed=LoggerMessage.Define<string>(LogLevel.Warning,new EventId(8802,"FinanceCadenceFailed"),"Finance cadence failed closed: {FailureType}.");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await recovery.WaitUntilRecoveredAsync(stoppingToken);
        while(!stoppingToken.IsCancellationRequested)
        {
            var now=DateTimeOffset.UtcNow;
            if(provider.Enabled&&provider.AccountActive&&recovery.MayStartTimeSensitiveWork)
            {
                try
                {
                    var outcome="no-provider-check";var providerChecked=false;var acquisitionSucceeded=false;
                    if(!string.IsNullOrWhiteSpace(provider.ApiToken)&&EodhdEntitlement.AllowsAcquisition(provider,now)&&
                       FinanceCadenceSchedule.IsProviderWindow(now,cadence.ProviderWindowStartUtcHour))
                    {
                        outcome=await AcquireAsync(now,stoppingToken);
                        providerChecked=outcome!="already-checked-today";
                        acquisitionSucceeded=outcome is "new-canonical-session" or "provider-check-no-new-session";
                    }
                    try{memory.BuildFeatures();}catch(InvalidOperationException){/* no committed source state */}
                    var created=memory.RunShadowCycle(now,true);if(created>0)outcome=$"{outcome}; predictions-created={created}";
                    memory.RecordCadenceCheck(now,providerChecked,acquisitionSucceeded,outcome);Cycle(logger,outcome,null);
                }
                catch(Exception exception) when(!stoppingToken.IsCancellationRequested&&(exception is HttpRequestException or JsonException or InvalidDataException or SqliteException or TaskCanceledException))
                {memory.RecordCadenceCheck(now,true,false,"failure");Failed(logger,exception.GetType().Name,null);}
            }
            await Task.Delay(TimeSpan.FromMinutes(Math.Clamp(cadence.InternalCheckMinutes,5,360)),stoppingToken);
        }
    }

    private async Task<string> AcquireAsync(DateTimeOffset now,CancellationToken token)
    {
        var to=DateOnly.FromDateTime(now.UtcDateTime);var from=to.AddYears(-1);var requested=0;var succeeded=0;
        var before=memory.LatestEodhdSession();
        using var adapter=new EodhdAdapter(provider);
        foreach(var instrument in EodhdCatalog.Watchlist)
        {
            if(!memory.ShouldAcquire(instrument.ProviderSymbol,to))continue;
            requested++;var started=DateTimeOffset.UtcNow;
            var acquisitionId=memory.RecordStarted(instrument,from,to,started);
            try{var result=await adapter.FetchAsync(instrument.ProviderSymbol,from,to,token);memory.Store(instrument,result.Bars,result.Payload,from,to,started,DateTimeOffset.UtcNow,result.Retries,acquisitionId);succeeded++;}
            catch(Exception exception) when(exception is HttpRequestException or JsonException or InvalidDataException or TaskCanceledException){memory.RecordFailure(instrument,from,to,started,exception.GetType().Name,acquisitionId);}
            await Task.Delay(TimeSpan.FromSeconds(1),token);
        }
        if(requested==0)return "already-checked-today";
        if(succeeded==0)return "failure";
        if(succeeded<requested)return "partial-provider-failure";
        var after=memory.LatestEodhdSession();
        return after.HasValue&&(!before.HasValue||after.Value>before.Value)?"new-canonical-session":"provider-check-no-new-session";
    }
}
