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
        return endpoints;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
