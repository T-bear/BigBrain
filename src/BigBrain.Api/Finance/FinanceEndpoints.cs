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
        return endpoints;
    }

    private static IResult ShadowResult(Func<object?> read)
    {
        try{return Results.Json(read(),JsonOptions);}
        catch(ArgumentException exception){return Results.Problem(statusCode:400,title:"Invalid shadow research query",detail:exception.Message,extensions:new Dictionary<string,object?>{{"code","finance.shadow.invalidQuery"}});}
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
