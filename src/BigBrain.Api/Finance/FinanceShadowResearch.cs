using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BigBrain.Api.SystemRecovery;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

internal enum FinanceShadowState { Pending, Evaluated, InsufficientData, MissedProspectiveWindow, Invalidated }
internal sealed record FinanceShadowPrediction(string PredictionId,string InstrumentId,string Symbol,DateOnly SessionDate,
    string Provider,string SourceRevisionId,DateTimeOffset ObservationKnowledgeUtc,DateTimeOffset KnowledgeCutoffUtc,
    string FeatureRevisionId,string StrategyId,string StrategyVersion,string ParameterFingerprint,string Signal,
    string Horizon,DateTimeOffset CreatedAtUtc,FinanceShadowState State,string OperatingMode,IReadOnlyList<string> ReasonCodes);
internal sealed record FinanceShadowOutcome(string OutcomeId,string PredictionId,DateOnly TargetSession,string SourceRevisionId,
    DateTimeOffset KnowledgeUtc,decimal ReferenceClose,decimal RealizedClose,decimal RealizedReturn,string DirectionResult,
    DateTimeOffset EvaluatedAtUtc);
internal sealed record FinanceShadowCatalog(DateTimeOffset GeneratedAtUtc,string OperatingMode,string ObservationClass,
    IReadOnlyList<FinanceShadowPrediction> Predictions,int Total,int Pending,int Evaluated,int Insufficient,int Missed,
    string EvidenceMaturity);
internal sealed record FinanceShadowStatus(string OperatingMode,string Provider,string ObservationClass,bool JournalHealthy,
    bool TemporalIntegrity,int Pending,DateTimeOffset? LastPredictionUtc,DateTimeOffset? LastOutcomeUtc,string AutomaticTrigger,
    string ExecutionAuthority);

internal static class FinanceShadowIdentity
{
    internal const string Horizon = "next-eligible-source-session-close-v1";
    internal static string Parameters(IReadOnlyDictionary<string,decimal> values) => Hash(JsonSerializer.Serialize(
        values.OrderBy(x=>x.Key,StringComparer.Ordinal).ToDictionary(x=>x.Key,x=>x.Value)));
    internal static string Prediction(string instrument,DateOnly session,string sourceRevision,string featureRevision,
        StrategyIdentity strategy,string parameters,DateTimeOffset cutoff) => "shadow-"+Hash(
        $"{instrument}|{session:yyyy-MM-dd}|{sourceRevision}|{featureRevision}|{strategy.Id}|{strategy.Version}|{parameters}|{Horizon}|{cutoff:O}")[7..23];
    internal static string Outcome(string prediction,string revision,DateOnly session)=>"outcome-"+Hash($"{prediction}|{revision}|{session:yyyy-MM-dd}")[7..23];
    private static string Hash(string value)=>"sha256:"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

internal sealed partial class EodhdMarketMemory
{
    private static void InitializeShadowStorage(SqliteConnection connection)
    {
        using var command=connection.CreateCommand();command.CommandText="""
          CREATE TABLE IF NOT EXISTS shadow_predictions(
            prediction_id TEXT PRIMARY KEY,instrument_id TEXT NOT NULL,symbol TEXT NOT NULL,session_date TEXT NOT NULL,
            provider TEXT NOT NULL,source_revision_id TEXT NOT NULL,observation_knowledge_utc TEXT NOT NULL,
            knowledge_cutoff_utc TEXT NOT NULL,feature_revision_id TEXT NOT NULL,strategy_id TEXT NOT NULL,
            strategy_version TEXT NOT NULL,parameter_fingerprint TEXT NOT NULL,signal TEXT NOT NULL,horizon TEXT NOT NULL,
            created_utc TEXT NOT NULL,state TEXT NOT NULL,operating_mode TEXT NOT NULL,reasons_json TEXT NOT NULL,
            UNIQUE(instrument_id,session_date,source_revision_id,feature_revision_id,strategy_id,strategy_version,parameter_fingerprint,horizon));
          CREATE TABLE IF NOT EXISTS shadow_outcomes(
            outcome_id TEXT PRIMARY KEY,prediction_id TEXT NOT NULL UNIQUE REFERENCES shadow_predictions(prediction_id),
            target_session TEXT NOT NULL,source_revision_id TEXT NOT NULL,knowledge_utc TEXT NOT NULL,
            reference_close TEXT NOT NULL,realized_close TEXT NOT NULL,realized_return TEXT NOT NULL,
            direction_result TEXT NOT NULL,evaluated_utc TEXT NOT NULL);
          CREATE INDEX IF NOT EXISTS ix_shadow_predictions_read ON shadow_predictions(session_date DESC,prediction_id);
          """;command.ExecuteNonQuery();
    }

    internal int RunShadowCycle(DateTimeOffset nowUtc,bool temporalIntegrity)
    {
        if(nowUtc.Offset!=TimeSpan.Zero)throw new ArgumentException("Shadow evaluation clock must be UTC.",nameof(nowUtc));
        if(!temporalIntegrity)return 0;
        using var connection=new SqliteConnection(ConnectionString);connection.Open();
        InvalidateBrokenLineage(connection);
        var created=0;
        var latest=ReadLatestBars(connection);
        foreach(var bar in latest)
        {
            if(bar.Acquired>nowUtc)continue;
            if(nowUtc.Date-bar.Session.ToDateTime(TimeOnly.MinValue)>TimeSpan.FromDays(3))continue;
            var later=HasLaterKnownBar(connection,bar.Instrument,bar.Session,nowUtc);
            if(later)continue; // Never manufacture prospective evidence after the horizon became knowable.
            var feature=ReadFeatureContext(connection,bar.Instrument,bar.Session,nowUtc);
            if(feature is null)continue;
            foreach(var strategy in Strategies())
            {
                var parameterFingerprint=FinanceShadowIdentity.Parameters(strategy.Parameters);
                var id=FinanceShadowIdentity.Prediction(bar.Instrument,bar.Session,bar.Revision,feature.Value.Revision,
                    strategy.Identity,parameterFingerprint,nowUtc);
                var intent=strategy.Evaluate(new(new InstrumentId(bar.Instrument),bar.Session,nowUtc,bar.Close,
                    feature.Value.Values,new(0,0,bar.Close,0)));
                var insufficient=intent.ReasonCodes.Contains("feature.warmup-or-unavailable",StringComparer.Ordinal);
                var prediction=new FinanceShadowPrediction(id,bar.Instrument,bar.Symbol,bar.Session,Provider,bar.Revision,
                    bar.Acquired,nowUtc,feature.Value.Revision,strategy.Identity.Id,strategy.Identity.Version,
                    parameterFingerprint,intent.Kind.ToString(),FinanceShadowIdentity.Horizon,nowUtc,
                    insufficient?FinanceShadowState.InsufficientData:FinanceShadowState.Pending,"RESEARCH",intent.ReasonCodes);
                var inserted=InsertPrediction(connection,prediction);created+=inserted;
                if(inserted==1)
                {
                    var previous=ReadPreviousClose(connection,bar.Instrument,bar.Session);
                    feature.Value.Values.TryGetValue("volatility.20",out var volatility);
                    feature.Value.Values.TryGetValue("volume.ratio.20",out var volumeRatio);
                    var proposal=new FinanceRiskProposal("proposal-"+prediction.PredictionId,bar.Instrument,strategy.Identity.Id,
                        strategy.Identity.Version,parameterFingerprint,prediction.PredictionId,bar.Revision,feature.Value.Revision,
                        bar.Session,bar.Acquired,nowUtc,nowUtc,"RESEARCH",intent.Kind.ToString(),bar.Close,previous,
                        intent.Kind==ResearchIntentKind.TargetLong?RiskPolicy.ResearchCapital*.075m:RiskPolicy.ResearchCapital*.01m,
                        feature.Value.Values.ContainsKey("volatility.20")?volatility:null,feature.Value.Values.ContainsKey("volume.ratio.20")?volumeRatio:null,
                        temporalIntegrity,true,true,!insufficient,true,true,0,0,0);
                    RecordRiskEvaluation(proposal);
                }
            }
        }
        EvaluatePending(connection,nowUtc);
        return created;
    }

    internal FinanceShadowCatalog ShadowCatalog(string? instrument,string? strategy,string? state,DateOnly? from,DateOnly? to,int limit)
    {
        if(limit<1||limit>200)throw new ArgumentException("Limit must be between 1 and 200.",nameof(limit));
        if(to<from)throw new ArgumentException("Range end cannot precede start.",nameof(to));
        if(instrument is {Length:>80}||strategy is {Length:>80}||state is {Length:>40})throw new ArgumentException("Shadow filter is invalid.");
        if(state is not null&&!Enum.TryParse<FinanceShadowState>(state,true,out _))throw new ArgumentException("Unknown shadow state.",nameof(state));
        using var connection=new SqliteConnection(ConnectionString);connection.Open();
        var all=ReadPredictions(connection,instrument,strategy,state,from,to,limit);
        using var counts=connection.CreateCommand();counts.CommandText="SELECT CASE WHEN o.outcome_id IS NOT NULL THEN 'Evaluated' ELSE p.state END,COUNT(*) FROM shadow_predictions p LEFT JOIN shadow_outcomes o ON o.prediction_id=p.prediction_id GROUP BY CASE WHEN o.outcome_id IS NOT NULL THEN 'Evaluated' ELSE p.state END";
        var map=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);using(var reader=counts.ExecuteReader())while(reader.Read())map[reader.GetString(0)]=reader.GetInt32(1);
        var total=map.Values.Sum();return new(DateTimeOffset.UtcNow,"RESEARCH","CURRENT EOD / PROSPECTIVE EOD",all,total,
            map.GetValueOrDefault(nameof(FinanceShadowState.Pending)),map.GetValueOrDefault(nameof(FinanceShadowState.Evaluated)),
            map.GetValueOrDefault(nameof(FinanceShadowState.InsufficientData)),map.GetValueOrDefault(nameof(FinanceShadowState.MissedProspectiveWindow)),
            total<20?"BOOTSTRAPPING":total<100?"EARLY":"DEVELOPING");
    }

    internal FinanceShadowPrediction? ShadowPrediction(string id)
    {
        if(string.IsNullOrWhiteSpace(id)||id.Length>80||!id.StartsWith("shadow-",StringComparison.Ordinal))throw new ArgumentException("Malformed prediction ID.",nameof(id));
        using var connection=new SqliteConnection(ConnectionString);connection.Open();return ReadPredictions(connection,null,null,null,null,null,200).SingleOrDefault(x=>x.PredictionId==id);
    }

    internal FinanceShadowStatus ShadowStatus(bool temporalIntegrity)
    {
        using var connection=new SqliteConnection(ConnectionString);connection.Open();
        static DateTimeOffset? Time(SqliteConnection c,string sql){using var x=c.CreateCommand();x.CommandText=sql;var v=x.ExecuteScalar() as string;return v is null?null:DateTimeOffset.Parse(v,CultureInfo.InvariantCulture);}
        using var count=connection.CreateCommand();count.CommandText="SELECT COUNT(*) FROM shadow_predictions WHERE state='Pending'";
        return new("RESEARCH",Provider,"CURRENT EOD / PROSPECTIVE EOD",true,temporalIntegrity,Convert.ToInt32(count.ExecuteScalar(),CultureInfo.InvariantCulture),
            Time(connection,"SELECT MAX(created_utc) FROM shadow_predictions"),Time(connection,"SELECT MAX(evaluated_utc) FROM shadow_outcomes"),
            "after durable EOD acquisition/recovery; idempotent source-state scan","NONE — research evidence only; no orders");
    }

    private static IResearchBacktestStrategy[] Strategies()=>[new BuyAndHoldResearchStrategy(),new SmaCrossoverResearchStrategy(),new MomentumResearchStrategy()];
    private static List<(string Instrument,string Symbol,DateOnly Session,decimal Close,DateTimeOffset Acquired,string Revision)> ReadLatestBars(SqliteConnection c)
    {using var x=c.CreateCommand();x.CommandText="""SELECT instrument_id,symbol,session_date,close,acquired_utc,revision_id FROM (SELECT instrument_id,symbol,session_date,close,acquired_utc,revision_id,ROW_NUMBER() OVER(PARTITION BY instrument_id ORDER BY session_date DESC,acquired_utc DESC,revision_id DESC) r FROM observations WHERE provider=$p) WHERE r=1""";x.Parameters.AddWithValue("$p",Provider);using var r=x.ExecuteReader();var a=new List<(string,string,DateOnly,decimal,DateTimeOffset,string)>();while(r.Read())a.Add((r.GetString(0),r.GetString(1),DateOnly.Parse(r.GetString(2),CultureInfo.InvariantCulture),decimal.Parse(r.GetString(3),CultureInfo.InvariantCulture),DateTimeOffset.Parse(r.GetString(4),CultureInfo.InvariantCulture),r.GetString(5)));return a;}
    private static bool HasLaterKnownBar(SqliteConnection c,string instrument,DateOnly session,DateTimeOffset cutoff){using var x=c.CreateCommand();x.CommandText="SELECT EXISTS(SELECT 1 FROM observations WHERE provider=$p AND instrument_id=$i AND session_date>$d AND acquired_utc<=$k)";x.Parameters.AddWithValue("$p",Provider);x.Parameters.AddWithValue("$i",instrument);x.Parameters.AddWithValue("$d",session.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));x.Parameters.AddWithValue("$k",cutoff.ToString("O"));return Convert.ToInt32(x.ExecuteScalar(),CultureInfo.InvariantCulture)==1;}
    private static (string Revision,Dictionary<string,decimal> Values)? ReadFeatureContext(SqliteConnection c,string instrument,DateOnly session,DateTimeOffset cutoff)
    {using var rev=c.CreateCommand();rev.CommandText="SELECT fr.revision_id FROM feature_revisions fr JOIN observations o ON o.instrument_id=$i AND o.session_date=$d WHERE fr.created_utc<=$k AND o.revision_id=$source AND EXISTS(SELECT 1 FROM json_each(fr.source_revisions_json) WHERE value=o.revision_id) ORDER BY fr.created_utc DESC,fr.revision_id DESC LIMIT 1";rev.Parameters.AddWithValue("$i",instrument);rev.Parameters.AddWithValue("$d",session.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));rev.Parameters.AddWithValue("$source",ReadSourceRevision(c,instrument,session));rev.Parameters.AddWithValue("$k",cutoff.ToString("O"));var id=rev.ExecuteScalar() as string;if(id is null)return null;using var x=c.CreateCommand();x.CommandText="SELECT definition_id,value FROM feature_values WHERE revision_id=$r AND instrument_id=$i AND session_date=$d AND knowledge_utc<=$k AND state='Available' AND value IS NOT NULL";x.Parameters.AddWithValue("$r",id);x.Parameters.AddWithValue("$i",instrument);x.Parameters.AddWithValue("$d",session.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));x.Parameters.AddWithValue("$k",cutoff.ToString("O"));using var reader=x.ExecuteReader();var values=new Dictionary<string,decimal>(StringComparer.Ordinal);while(reader.Read())values[reader.GetString(0)]=decimal.Parse(reader.GetString(1),CultureInfo.InvariantCulture);return(id,values);}
    private static string ReadSourceRevision(SqliteConnection c,string instrument,DateOnly session){using var x=c.CreateCommand();x.CommandText="SELECT revision_id FROM observations WHERE provider=$p AND instrument_id=$i AND session_date=$d ORDER BY acquired_utc DESC,revision_id DESC LIMIT 1";x.Parameters.AddWithValue("$p",Provider);x.Parameters.AddWithValue("$i",instrument);x.Parameters.AddWithValue("$d",session.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));return (string)x.ExecuteScalar()!;}
    private static decimal ReadPreviousClose(SqliteConnection c,string instrument,DateOnly session){using var x=c.CreateCommand();x.CommandText="SELECT close FROM observations WHERE provider=$p AND instrument_id=$i AND session_date<$d ORDER BY session_date DESC,acquired_utc DESC LIMIT 1";x.Parameters.AddWithValue("$p",Provider);x.Parameters.AddWithValue("$i",instrument);x.Parameters.AddWithValue("$d",session.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));var value=x.ExecuteScalar() as string;return value is null?0:decimal.Parse(value,CultureInfo.InvariantCulture);}
    private static void InvalidateBrokenLineage(SqliteConnection c){using var x=c.CreateCommand();x.CommandText="""
      UPDATE shadow_predictions SET reasons_json='["lineage.feature-revision-does-not-contain-source-revision"]' WHERE state='Invalidated';
      UPDATE shadow_predictions SET state='Invalidated',reasons_json='["lineage.feature-revision-does-not-contain-source-revision"]' WHERE state!='Invalidated' AND NOT EXISTS(SELECT 1 FROM feature_revisions fr,json_each(fr.source_revisions_json) j WHERE fr.revision_id=shadow_predictions.feature_revision_id AND j.value=shadow_predictions.source_revision_id);
      """;x.ExecuteNonQuery();}
    private static int InsertPrediction(SqliteConnection c,FinanceShadowPrediction p){using var x=c.CreateCommand();x.CommandText="INSERT OR IGNORE INTO shadow_predictions VALUES($id,$i,$s,$d,$p,$r,$o,$k,$f,$si,$sv,$pf,$signal,$h,$created,$state,$mode,$reasons)";foreach(var v in new[]{("$id",(object)p.PredictionId),("$i",p.InstrumentId),("$s",p.Symbol),("$d",p.SessionDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),("$p",p.Provider),("$r",p.SourceRevisionId),("$o",p.ObservationKnowledgeUtc.ToString("O")),("$k",p.KnowledgeCutoffUtc.ToString("O")),("$f",p.FeatureRevisionId),("$si",p.StrategyId),("$sv",p.StrategyVersion),("$pf",p.ParameterFingerprint),("$signal",p.Signal),("$h",p.Horizon),("$created",p.CreatedAtUtc.ToString("O")),("$state",p.State.ToString()),("$mode",p.OperatingMode),("$reasons",JsonSerializer.Serialize(p.ReasonCodes))})x.Parameters.AddWithValue(v.Item1,v.Item2);return x.ExecuteNonQuery();}
    private static void EvaluatePending(SqliteConnection c,DateTimeOffset now){var pending=ReadPredictions(c,null,null,nameof(FinanceShadowState.Pending),null,null,2000);foreach(var p in pending){using var x=c.CreateCommand();x.CommandText="SELECT session_date,close,acquired_utc,revision_id FROM observations WHERE provider=$p AND instrument_id=$i AND session_date>$d AND acquired_utc<=$k ORDER BY session_date,acquired_utc,revision_id LIMIT 1";x.Parameters.AddWithValue("$p",Provider);x.Parameters.AddWithValue("$i",p.InstrumentId);x.Parameters.AddWithValue("$d",p.SessionDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));x.Parameters.AddWithValue("$k",now.ToString("O"));using var r=x.ExecuteReader();if(!r.Read())continue;var target=DateOnly.Parse(r.GetString(0),CultureInfo.InvariantCulture);var close=decimal.Parse(r.GetString(1),CultureInfo.InvariantCulture);var known=DateTimeOffset.Parse(r.GetString(2),CultureInfo.InvariantCulture);var revision=r.GetString(3);r.Close();using var original=c.CreateCommand();original.CommandText="SELECT close FROM observations WHERE instrument_id=$i AND session_date=$d AND revision_id=$r";original.Parameters.AddWithValue("$i",p.InstrumentId);original.Parameters.AddWithValue("$d",p.SessionDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));original.Parameters.AddWithValue("$r",p.SourceRevisionId);var reference=decimal.Parse((string)original.ExecuteScalar()!,CultureInfo.InvariantCulture);var ret=close/reference-1;var direction=p.Signal==nameof(ResearchIntentKind.TargetLong)?(ret>0?"CORRECT":"INCORRECT"):p.Signal==nameof(ResearchIntentKind.TargetFlat)?(ret<=0?"CORRECT":"INCORRECT"):"NOT_APPLICABLE";var oid=FinanceShadowIdentity.Outcome(p.PredictionId,revision,target);Execute(c,null,"INSERT OR IGNORE INTO shadow_outcomes VALUES($o,$p,$d,$r,$k,$a,$b,$return,$result,$e)",( "$o",oid),("$p",p.PredictionId),("$d",target.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),("$r",revision),("$k",known.ToString("O")),("$a",reference.ToString(CultureInfo.InvariantCulture)),("$b",close.ToString(CultureInfo.InvariantCulture)),("$return",ret.ToString(CultureInfo.InvariantCulture)),("$result",direction),("$e",now.ToString("O")));}}
    private static List<FinanceShadowPrediction> ReadPredictions(SqliteConnection c,string? instrument,string? strategy,string? state,DateOnly? from,DateOnly? to,int limit){using var x=c.CreateCommand();x.CommandText="SELECT p.prediction_id,p.instrument_id,p.symbol,p.session_date,p.provider,p.source_revision_id,p.observation_knowledge_utc,p.knowledge_cutoff_utc,p.feature_revision_id,p.strategy_id,p.strategy_version,p.parameter_fingerprint,p.signal,p.horizon,p.created_utc,CASE WHEN o.outcome_id IS NOT NULL THEN 'Evaluated' ELSE p.state END,p.operating_mode,p.reasons_json FROM shadow_predictions p LEFT JOIN shadow_outcomes o ON o.prediction_id=p.prediction_id WHERE ($i IS NULL OR p.instrument_id=$i) AND ($strategy IS NULL OR p.strategy_id=$strategy) AND ($state IS NULL OR CASE WHEN o.outcome_id IS NOT NULL THEN 'Evaluated' ELSE p.state END=$state) AND ($from IS NULL OR p.session_date >= $from) AND ($to IS NULL OR p.session_date <= $to) ORDER BY p.session_date DESC,p.prediction_id LIMIT $limit";x.Parameters.AddWithValue("$i",instrument??(object)DBNull.Value);x.Parameters.AddWithValue("$strategy",strategy??(object)DBNull.Value);x.Parameters.AddWithValue("$state",state??(object)DBNull.Value);x.Parameters.AddWithValue("$from",from?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??(object)DBNull.Value);x.Parameters.AddWithValue("$to",to?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??(object)DBNull.Value);x.Parameters.AddWithValue("$limit",limit);using var r=x.ExecuteReader();var a=new List<FinanceShadowPrediction>();while(r.Read())a.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),DateOnly.Parse(r.GetString(3),CultureInfo.InvariantCulture),r.GetString(4),r.GetString(5),DateTimeOffset.Parse(r.GetString(6),CultureInfo.InvariantCulture),DateTimeOffset.Parse(r.GetString(7),CultureInfo.InvariantCulture),r.GetString(8),r.GetString(9),r.GetString(10),r.GetString(11),r.GetString(12),r.GetString(13),DateTimeOffset.Parse(r.GetString(14),CultureInfo.InvariantCulture),Enum.Parse<FinanceShadowState>(r.GetString(15)),r.GetString(16),JsonSerializer.Deserialize<string[]>(r.GetString(17))??[]));return a;}
}

internal sealed class FinanceShadowWorker(EodhdFinanceOptions options,EodhdMarketMemory memory,SystemRecoveryCoordinator recovery,ILogger<FinanceShadowWorker> logger):BackgroundService
{
    private static readonly Action<ILogger,int,Exception?> Created=LoggerMessage.Define<int>(LogLevel.Information,new EventId(8701,"ShadowCreated"),"Finance shadow predictions created: {Count}; mode RESEARCH; no execution authority.");
    private static readonly Action<ILogger,string,Exception?> Failed=LoggerMessage.Define<string>(LogLevel.Warning,new EventId(8702,"ShadowFailedClosed"),"Finance shadow cycle failed closed: {Type}.");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken){await recovery.WaitUntilRecoveredAsync(stoppingToken);if(!options.Enabled||!options.AccountActive)return;await Task.Delay(TimeSpan.FromSeconds(18),stoppingToken);try{var count=memory.RunShadowCycle(DateTimeOffset.UtcNow,recovery.MayStartTimeSensitiveWork);if(count>0)Created(logger,count,null);}catch(Exception e)when(e is InvalidOperationException or SqliteException){Failed(logger,e.GetType().Name,null);}}
}
