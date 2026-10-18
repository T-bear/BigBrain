using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BigBrain.Api.Media;

public sealed record DirectAudioResult(string Id, string Sha256, string Format, string Name, long Size,
    string Status, string RelativePath, string Inode, string LibraryId, string ItemId);

public interface IDirectAudioImporter
{
    Task RequireAvailableAsync(CancellationToken token);
    Task<DirectAudioResult> ImportAsync(MediaUrlImportDraft draft, DownloadedAudio file, CancellationToken token);
    Task<DirectAudioResult> InspectAsync(string id, CancellationToken token);
}

public sealed class LibrarrDirectAudioImporter(HttpClient http) : IDirectAudioImporter
{
    public async Task RequireAvailableAsync(CancellationToken token)
    {
        using var response = await http.GetAsync("api/v1/direct-audio-capabilities", HttpCompletionOption.ResponseHeadersRead, token);
        var value = await ReadAsync<Capabilities>(response, token);
        if (value.Contract != "bb158-direct-audio-v1" || value.MaximumBytes != DirectAudioTransport.MaximumBytes || value.Formats is not ["mp3"])
            throw MediaUrlImportValidation.Invalid("importUnavailable", "Librarr saknar den verifierade direktimportfunktionen.", 503);
    }
    private sealed record Capabilities(string Contract, long MaximumBytes, string[] Formats);

    public async Task<DirectAudioResult> ImportAsync(MediaUrlImportDraft draft, DownloadedAudio file, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "api/v1/direct-audio/" + draft.Id);
        request.Headers.Add("X-Audio-Sha256", file.Checksum);
        request.Headers.Add("X-Audio-Format", draft.Format);
        request.Headers.Add("X-Audio-Name", Uri.EscapeDataString(draft.FileName));
        request.Content = new StreamContent(file.Stream);
        request.Content.Headers.ContentType = new("audio/mpeg");
        request.Content.Headers.ContentLength = file.Size;
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        return await ReadAsync<DirectAudioResult>(response, token);
    }
    public async Task<DirectAudioResult> InspectAsync(string id, CancellationToken token)
    {
        using var response = await http.GetAsync("api/v1/direct-audio/" + id, HttpCompletionOption.ResponseHeadersRead, token);
        return await ReadAsync<DirectAudioResult>(response, token);
    }
    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken token)
    {
        if (response.StatusCode != HttpStatusCode.OK)
            throw MediaUrlImportValidation.Invalid("importUnavailable", "Importtjänsten kunde inte verifiera resultatet. Ingen automatisk omkörning görs.", 503);
        await response.Content.LoadIntoBufferAsync(16384, token);
        return await response.Content.ReadFromJsonAsync<T>(token) ?? throw new HttpRequestException("Missing import result.");
    }
}

public sealed class MediaUrlImportService(AudiobookAcquisitionStore store, IDirectAudioTransport transport,
    IDirectAudioImporter importer, MediaOptions options, TimeProvider clock) : IDisposable
{
    private readonly SemaphoreSlim flight = new(1, 1);
    public void Dispose() => flight.Dispose();

    public async Task<MediaUrlImportDraft> StartAsync(string id, CancellationToken token)
    {
        if (!options.Librarr.DirectAudioImportEnabled || string.IsNullOrWhiteSpace(options.Librarr.ApiKey) || string.IsNullOrWhiteSpace(options.Audiobookshelf.LibraryId))
            throw MediaUrlImportValidation.Invalid("importDisabled", "Direktimport är inte aktiverad i serverns konfiguration.", 503);
        if (!await flight.WaitAsync(0, token)) throw MediaUrlImportValidation.Invalid("importBusy", "En import hanteras redan.", 409);
        MediaUrlImportDraft? draft = null;
        try
        {
            // Capability failure does not consume authorization or contact the source.
            await importer.RequireAvailableAsync(token);
            draft = await store.StartUrlImportAsync(id, clock.GetUtcNow(), token);
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(token);
            limit.CancelAfter(TimeSpan.FromSeconds(150));
            await using var file = await transport.FetchAsync(draft, limit.Token);
            draft = draft with { Status = "importing", Checksum = file.Checksum, Bytes = file.Size };
            await store.SaveUrlImportAsync(draft, limit.Token);
            var result = await importer.ImportAsync(draft, file, limit.Token);
            if (result.Status is "indexing" or "completed" && result.LibraryId != options.Audiobookshelf.LibraryId)
                throw MediaUrlImportValidation.Invalid("libraryMismatch", "Fel Audiobookshelf-bibliotek.", 502);
            draft = WithLibraryLink(Apply(draft, result));
            await store.SaveUrlImportAsync(draft, limit.Token);
            return draft;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException or AudiobookAcquisitionException or JsonException)
        {
            if (draft is not null)
            {
                // A lost response can follow a provider commit. Never refund/retry/re-download.
                draft = draft with { Status = "uncertain", BlockerCode = "inspectionRequired" };
                await store.SaveUrlImportAsync(draft, CancellationToken.None);
                return draft;
            }
            throw;
        }
        finally { flight.Release(); }
    }

    public async Task<MediaUrlImportDraft> InspectAsync(string id, CancellationToken token)
    {
        if (!await flight.WaitAsync(0, token)) throw MediaUrlImportValidation.Invalid("importBusy", "En import hanteras redan.", 409);
        try
        {
            var draft = (await store.ListUrlImportsAsync(token)).SingleOrDefault(v => v.Id == id)
                ?? throw MediaUrlImportValidation.Invalid("draftNotFound", "Importen finns inte.", 404);
            if (draft.Status is not ("indexing" or "uncertain" or "importing" or "downloading")) return draft;
            if (draft.Checksum is null) return draft with { MessageOverride = "Avbruten hämtning. Kräver manuell avstämning; ingen automatisk omkörning." };
            var result = await importer.InspectAsync(id, token);
            if (result.Status is "indexing" or "completed" && result.LibraryId != options.Audiobookshelf.LibraryId)
                throw MediaUrlImportValidation.Invalid("libraryMismatch", "Fel Audiobookshelf-bibliotek.", 502);
            draft = WithLibraryLink(Apply(draft, result));
            await store.SaveUrlImportAsync(draft, token);
            return draft;
        }
        finally { flight.Release(); }
    }

    private MediaUrlImportDraft WithLibraryLink(MediaUrlImportDraft draft)
    {
        if (draft.Status != "completed" || !SafeId(draft.ItemId)
            || !Uri.TryCreate(options.Audiobookshelf.PublicUrl, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("https" or "http") || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            return draft;
        return draft with { AudiobookshelfUrl = uri.AbsoluteUri.TrimEnd('/') + "/item/" + Uri.EscapeDataString(draft.ItemId!) };
    }

    private static bool SafeId(string? id) => id is { Length: > 0 and <= 128 } && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    public static MediaUrlImportDraft Apply(MediaUrlImportDraft draft, DirectAudioResult result)
    {
        if (result.Id != draft.Id || result.Sha256 != draft.Checksum || result.Size != draft.Bytes
            || result.Format != draft.Format || result.Name != draft.FileName)
            throw MediaUrlImportValidation.Invalid("identityMismatch", "Importresultatets identitet stämmer inte.", 502);
        if (result.Status == "failed") return draft with { Status = "failed", BlockerCode = "providerRejected" };
        var expected = $"BigBrain Imports/{draft.Id}/{draft.ProposedFileName}";
        if (result.Status is not ("indexing" or "completed") || result.RelativePath != expected
            || result.Inode is null || !Regex.IsMatch(result.Inode, @"\A[0-9]{1,20}\z", RegexOptions.CultureInvariant) || !SafeId(result.LibraryId)
            || result.Status == "completed" && !SafeId(result.ItemId))
            throw MediaUrlImportValidation.Invalid("indexUnverified", "Exakt biblioteksfil kunde inte verifieras.", 502);
        return draft with
        {
            Status = result.Status,
            RelativePath = result.RelativePath,
            Inode = result.Inode,
            LibraryId = result.LibraryId,
            ItemId = result.ItemId,
            BlockerCode = ""
        };
    }
}
