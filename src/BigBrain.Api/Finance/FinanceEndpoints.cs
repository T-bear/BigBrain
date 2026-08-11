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
        return endpoints;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
