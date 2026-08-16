using System.Globalization;

namespace BigBrain.Api.Finance;

internal sealed class FredApiClient(FinanceFredOptions options)
{
    internal async Task<byte[]> GetVintageObservationsAsync(string seriesId,DateOnly observationFrom,DateOnly observationTo,DateOnly realtimeStart,DateOnly realtimeEnd,CancellationToken token)
    {
        options.Validate();if(!options.Enabled||!options.ApiKeyConfigured)throw new InvalidOperationException("FRED vintage acquisition is disabled or its API key is not configured.");if(seriesId is not ("CPIAUCSL" or "UNRATE")||observationFrom>observationTo||realtimeStart>realtimeEnd||(observationTo.DayNumber-observationFrom.DayNumber)>370)throw new ArgumentException("Vintage drill scope is invalid or exceeds one year.");
        var query=$"/fred/series/observations?series_id={Uri.EscapeDataString(seriesId)}&api_key={Uri.EscapeDataString(options.ApiKey)}&file_type=json&output_type=2&observation_start={observationFrom.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)}&observation_end={observationTo.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)}&realtime_start={realtimeStart.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)}&realtime_end={realtimeEnd.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)}&limit={options.MaximumVintageObservations}";
        using var client=new HttpClient{BaseAddress=new(options.BaseUrl),Timeout=TimeSpan.FromSeconds(options.TimeoutSeconds)};using var response=await client.GetAsync(query,HttpCompletionOption.ResponseHeadersRead,token);response.EnsureSuccessStatusCode();var bytes=await response.Content.ReadAsByteArrayAsync(token);if(bytes.Length>5_000_000)throw new InvalidDataException("FRED vintage response exceeds the bounded artifact size.");return bytes;
    }
}
