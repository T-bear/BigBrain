using System.Globalization;

namespace BigBrain.Api.Finance;

internal sealed record MacroProviderArtifact(string Provider,string SeriesId,string SourceUrl,byte[] Content,DateTimeOffset AcquiredAtUtc);

internal interface IMacroEvidenceProvider
{
    string Provider { get; }
    Task<MacroProviderArtifact> AcquireAsync(string seriesId,DateOnly from,DateOnly to,CancellationToken token);
}

internal sealed class RiksbankMacroProvider : IMacroEvidenceProvider
{
    private static readonly HashSet<string> Allowed=["SECBREPOEFF","SEKEURPMI","SEKUSDPMI"];
    public string Provider=>"RIKSBANK";
    public async Task<MacroProviderArtifact> AcquireAsync(string seriesId,DateOnly from,DateOnly to,CancellationToken token)
    {
        if(!Allowed.Contains(seriesId)||from>to||to.DayNumber-from.DayNumber>8000)throw new ArgumentException("Riksbank acquisition exceeds the bounded pack or range.");var path=$"https://api.riksbank.se/swea/v1/Observations/{seriesId}/{from:yyyy-MM-dd}/{to:yyyy-MM-dd}";using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(30)};using var response=await client.GetAsync(path,HttpCompletionOption.ResponseHeadersRead,token);response.EnsureSuccessStatusCode();var bytes=await response.Content.ReadAsByteArrayAsync(token);if(bytes.Length>5_000_000)throw new InvalidDataException("Riksbank artifact exceeds the bounded size.");return new(Provider,seriesId,path,bytes,DateTimeOffset.UtcNow);
    }
}

internal sealed class EcbMacroProvider : IMacroEvidenceProvider
{
    private static readonly HashSet<string> Allowed=["EXR.D.USD.EUR.SP00.A","EXR.D.SEK.EUR.SP00.A","FM.D.U2.EUR.4F.KR.MRR_FR.LEV"];
    public string Provider=>"ECB";
    public async Task<MacroProviderArtifact> AcquireAsync(string seriesId,DateOnly from,DateOnly to,CancellationToken token)
    {
        if(!Allowed.Contains(seriesId)||from>to||to.DayNumber-from.DayNumber>8000)throw new ArgumentException("ECB acquisition exceeds the bounded pack or range.");var split=seriesId.Split('.',2);var flow=split[0];var key=split[1];var path=$"https://data-api.ecb.europa.eu/service/data/{flow}/{key}?startPeriod={from.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)}&endPeriod={to.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)}&format=csvdata&detail=dataonly";using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(30)};client.DefaultRequestHeaders.Accept.ParseAdd("text/csv");using var response=await client.GetAsync(path,HttpCompletionOption.ResponseHeadersRead,token);response.EnsureSuccessStatusCode();var bytes=await response.Content.ReadAsByteArrayAsync(token);if(bytes.Length>5_000_000)throw new InvalidDataException("ECB artifact exceeds the bounded size.");return new(Provider,seriesId,path,bytes,DateTimeOffset.UtcNow);
    }
}
