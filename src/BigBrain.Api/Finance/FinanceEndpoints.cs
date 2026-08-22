using BigBrain.Modules.Finance;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BigBrain.Api.Finance;

public static class FinanceEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/modules/finance/observation", (IFinanceObservationReader reader) =>
            Results.Json(reader.GetSnapshot(), JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/features", (string? instrumentId, string? featureId,
            DateOnly? from, DateOnly? to, DateTimeOffset? knowledgeAsOfUtc, int? limit, IFinanceFeatureReader reader) =>
            Results.Json(reader.GetSnapshot(instrumentId, featureId, from, to, knowledgeAsOfUtc, limit ?? 260), JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/backtests", (IFinanceBacktestReader reader) => Results.Json(reader.GetCatalog(), JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/backtests/{runId}", (string runId, int? eventOffset, int? eventLimit,
            int? fillOffset, int? fillLimit, IFinanceBacktestReader reader) =>
        {
            if (reader.GetResult(runId) is not { } result) return Results.NotFound();
            var boundedEventOffset=Math.Max(0,eventOffset??0);var boundedEventLimit=Math.Clamp(eventLimit??250,1,500);
            var boundedFillOffset=Math.Max(0,fillOffset??0);var boundedFillLimit=Math.Clamp(fillLimit??250,1,500);
            return Results.Json(result with { Events=result.Events.Skip(boundedEventOffset).Take(boundedEventLimit).ToArray(),
                Fills=result.Fills.Skip(boundedFillOffset).Take(boundedFillLimit).ToArray(), EquityCurve=result.EquityCurve.Take(500).ToArray() },JsonOptions);
        });
        endpoints.MapGet("/api/v1/modules/finance/robustness",(IFinanceRobustnessReader reader)=>Results.Json(reader.GetCatalog(),JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/robustness/{evaluationId}",(string evaluationId,IFinanceRobustnessReader reader)=>
            reader.GetEvaluation(evaluationId) is { } result?Results.Json(result,JsonOptions):Results.NotFound());
        endpoints.MapGet("/api/v1/modules/finance/datasets",(IFinanceDatasetReader reader)=>Results.Json(reader.GetCatalog(),JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/backups",(IFinanceBackupReader reader)=>Results.Json(reader.GetInventory(),JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/shadow/predictions",(string? instrument,string? strategy,string? state,
            DateOnly? from,DateOnly? to,int? limit,EodhdMarketMemory memory)=>ShadowResult(() => memory.ShadowCatalog(instrument,strategy,state,from,to,limit??50)));
        endpoints.MapGet("/api/v1/modules/finance/shadow/predictions/{id}",(string id,EodhdMarketMemory memory)=>
            ShadowResult(() => memory.ShadowPrediction(id)));
        endpoints.MapGet("/api/v1/modules/finance/shadow/scorecard",(EodhdMarketMemory memory)=>
            Results.Json(memory.ShadowCatalog(null,null,null,null,null,50),JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/shadow/status",(EodhdMarketMemory memory,BigBrain.Api.SystemRecovery.SystemRecoveryCoordinator recovery)=>
            Results.Json(memory.ShadowStatus(recovery.MayStartTimeSensitiveWork),JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/overview",(EodhdMarketMemory memory,EodhdFinanceOptions provider,
            FinanceCadenceOptions cadence,BigBrain.Api.SystemRecovery.SystemRecoveryCoordinator recovery)=>
            Results.Json(memory.Overview(provider,cadence,recovery.MayStartTimeSensitiveWork),JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/cadence/status",(EodhdMarketMemory memory,EodhdFinanceOptions provider,
            FinanceCadenceOptions cadence,BigBrain.Api.SystemRecovery.SystemRecoveryCoordinator recovery)=>
            Results.Json(memory.CadenceSnapshot(provider.Enabled,recovery.MayStartTimeSensitiveWork,cadence.ProviderWindowStartUtcHour,cadence.InternalCheckMinutes),JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/risk/status",(EodhdMarketMemory memory)=>Results.Json(memory.RiskStatus(),JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/risk/policy",(EodhdMarketMemory memory)=>Results.Json(memory.RiskPolicySnapshot(),JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/risk/evaluations",(int? limit,EodhdMarketMemory memory)=>ShadowResult(()=>memory.RiskEvaluations(limit??50)));
        endpoints.MapGet("/api/v1/modules/finance/risk/evaluations/{id}",(string id,EodhdMarketMemory memory)=>ShadowResult(()=>memory.RiskEvaluation(id)));
        endpoints.MapGet("/api/v1/modules/finance/macro/status",(FinanceMacroMemory memory)=>Results.Json(memory.Snapshot().Status,JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/macro/series",(FinanceMacroMemory memory)=>Results.Json(memory.Snapshot(),JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/macro/context",(FinanceMacroMemory memory,MacroRegion region,DateTimeOffset asOfUtc,MacroEvidenceClass evidenceClass)=>Results.Json(memory.AsOf(region,asOfUtc,evidenceClass),JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/macro/regime",(FinanceMacroMemory memory)=>memory.Snapshot().LatestRegime is { } regime?Results.Json(regime,JsonOptions):Results.NotFound());
        endpoints.MapGet("/api/v1/modules/finance/research/regime-analysis",(FinanceMacroMemory macro,EodhdMarketMemory market)=>Results.Json(macro.Analyze(market),JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/research/autonomous",(EodhdMarketMemory memory)=>Results.Json(memory.AutonomousResearchSnapshot(),JsonOptions));
        endpoints.MapGet("/api/v1/modules/finance/research/autonomous/runs",(int? offset,int? limit,EodhdMarketMemory memory)=>ResearchQuery(()=>memory.ResearchRuns(offset??0,limit??25)));
        endpoints.MapGet("/api/v1/modules/finance/research/autonomous/runs/{runId}",(string runId,EodhdMarketMemory memory)=>ResearchQuery(()=>memory.ResearchRun(runId),true));
        endpoints.MapGet("/api/v1/modules/finance/research/autonomous/experiments",(int? offset,int? limit,string? family,string? verdict,string? state,string? hypothesis,string? run,EodhdMarketMemory memory)=>ResearchQuery(()=>memory.ResearchExperiments(offset??0,limit??25,family,verdict,state,hypothesis,run)));
        endpoints.MapGet("/api/v1/modules/finance/research/autonomous/experiments/{experimentId}",(string experimentId,EodhdMarketMemory memory)=>ResearchQuery(()=>memory.ResearchExperiment(experimentId),true));
        endpoints.MapPost("/api/v1/modules/finance/research/autonomous/run",(AutonomousResearchRunRequest request,EodhdMarketMemory memory)=>
        {
            try{return Results.Json(memory.RunAutonomousResearch(request.IdempotencyKey,request.MaximumExperiments??FinanceResearchContracts.MaximumTotalExperimentsPerRun),JsonOptions);}
            catch(ArgumentException exception){return Results.Problem(statusCode:400,title:"Invalid autonomous research request",detail:exception.Message,extensions:new Dictionary<string,object?>{{"code","finance.research.invalidRequest"}});}
            catch(AutonomousResearchBusyException exception){return Results.Problem(statusCode:409,title:"Autonomous research already running",detail:"Another bounded research run is active.",extensions:new Dictionary<string,object?>{{"code","finance.research.alreadyRunning"},{"currentRunId",exception.CurrentRunId}});}
            catch(CurrentResearchEvidenceUnavailableException exception){return Results.Problem(statusCode:409,title:"Current research evidence unavailable",detail:"The complete current robustness evidence set is unavailable; no older evidence was substituted.",extensions:new Dictionary<string,object?>{{"code","finance.research.currentEvidenceUnavailable"},{"reason",exception.ReasonCode}});}
            catch(InvalidOperationException exception){return Results.Problem(statusCode:409,title:"Autonomous research unavailable",detail:exception.Message,extensions:new Dictionary<string,object?>{{"code","finance.research.conflict"}});}
        });
        return endpoints;
    }

    private static IResult ShadowResult(Func<object?> read)
    {
        try{return Results.Json(read(),JsonOptions);}
        catch(ArgumentException exception){return Results.Problem(statusCode:400,title:"Invalid shadow research query",detail:exception.Message,extensions:new Dictionary<string,object?>{{"code","finance.shadow.invalidQuery"}});}
    }

    private static IResult ResearchQuery(Func<object?> read,bool notFound=false)
    {
        try{var value=read();return value is null&&notFound?Results.Problem(statusCode:404,title:"Autonomous research evidence not found",detail:"The requested research evidence does not exist.",extensions:new Dictionary<string,object?>{{"code","finance.research.notFound"}}):Results.Json(value,JsonOptions);}
        catch(ArgumentException exception){return Results.Problem(statusCode:400,title:"Invalid autonomous research query",detail:exception.Message,extensions:new Dictionary<string,object?>{{"code","finance.research.invalidQuery"}});}
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
