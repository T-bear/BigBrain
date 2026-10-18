using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BigBrain.Api.Media;

// Stable intent plus verified import outcome; preview/confirmation never imply file placement.
public sealed record MediaUrlImportInput(string? SourceUrl, string? FileName, string? Doi, string? Format);
public sealed record MediaUrlImportConfirmation(bool DetailsConfirmed, bool RightsConfirmed);
public sealed record MediaUrlImportDraft(
    string Id, string SourceUrl, string FileName, string Doi, string Format,
    string ProposedFileName, DateTimeOffset CreatedAtUtc, DateTimeOffset ExpiresAtUtc,
    string Status, DateTimeOffset? ConfirmedAtUtc, string BlockerCode)
{
    public string? Checksum { get; init; }
    public long? Bytes { get; init; }
    public string? RelativePath { get; init; }
    public string? Inode { get; init; }
    public string? LibraryId { get; init; }
    public string? ItemId { get; init; }
    public string? AudiobookshelfUrl { get; init; }
    public string? MessageOverride { get; init; }
    public bool CanStart => Status == "confirmed" && Format is "mp3" or "m4a";
    public string Message => MessageOverride ?? Status switch
    {
        "confirmed" => "Uppgifterna är bekräftade. Starta import separat för att hämta ljudet.",
        "downloading" => "Hämtning påbörjad. En avbruten körning startas aldrig om automatiskt.",
        "importing" => "Ljudet valideras och placeras av importtjänsten.",
        "indexing" => "Filen är placerad. Väntar på exakt Audiobookshelf-indexering.",
        "completed" => "Filen är verifierad i Audiobookshelf. DOI-kopplingen är sparad.",
        "failed" => "Importtjänsten avvisade filen eller kunde inte placera den säkert. Kontrollera filformat och importstatus; ingen automatisk omkörning görs.",
        "uncertain" => "Resultatet är osäkert. Kontrollera status; starta inte en ny import som ersättning.",
        _ => "Ingen fil har hämtats. MP3 och M4A (AAC-LC) kan importeras utan konvertering."
    };
}

public static class MediaUrlImportValidation
{
    private static readonly string[] LocalSuffixes = [".local", ".localhost", ".internal", ".lan", ".home", ".arpa"];

    public static MediaUrlImportInput Normalize(MediaUrlImportInput input)
    {
        var source = input.SourceUrl?.Trim() ?? "";
        // Structural validation only: never resolve, fetch, follow redirects or inspect media here.
        if (source.Length is < 1 or > 2048 || source.Any(char.IsControl)
            || !Uri.TryCreate(source, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || uri.Port != 443
            || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0
            || uri.HostNameType != UriHostNameType.Dns || IPAddress.TryParse(uri.Host, out _)
            || !uri.Host.Contains('.') || uri.Host.EndsWith('.')
            || LocalSuffixes
                .Any(suffix => uri.Host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
            throw Invalid("invalidSource", "Ange en direkt HTTPS-länk utan IP-adress, lokalt värdnamn, inloggning eller URL-parametrar.");

        var format = input.Format?.Trim().ToLowerInvariant();
        if (format is not ("mp3" or "m4a"))
            throw Invalid("unsupportedFormat", "Välj MP3 eller M4A. Konvertering är inte tillgänglig.");
        if (!uri.AbsolutePath.EndsWith("." + format, StringComparison.OrdinalIgnoreCase))
            throw Invalid("unsupportedSource", "Endast direkta länkar med vald ljudändelse kan förhandsgranskas. YouTube, videosidor och konvertering stöds inte.");

        var name = input.FileName?.Trim() ?? "";
        if (name.Length == 0) name = "Ljudimport";
        if (name.Length > 80)
            throw Invalid("invalidFileName", "Filnamnet får vara högst 80 tecken.");
        try { name = name.Normalize(NormalizationForm.FormC); }
        catch (ArgumentException) { throw Invalid("invalidFileName", "Filnamnet innehåller ogiltiga tecken."); }
        if (name.Length is < 1 or > 80 || name.StartsWith('.') || name.EndsWith('.')
            || name.Contains("..", StringComparison.Ordinal)
            || name.Any(c => !char.IsLetterOrDigit(c) && !" -_().–'".Contains(c))
            || Regex.IsMatch(name, @"^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])(?:\.|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            throw Invalid("invalidFileName", "Ange ett filnamn på 1–80 tecken utan sökväg, kontrolltecken eller reserverat namn.");

        var doi = (input.Doi?.Trim() ?? "").ToLowerInvariant();
        if (doi.StartsWith("https://doi.org/", StringComparison.Ordinal)) doi = doi[16..];
        if (doi.StartsWith("doi:", StringComparison.Ordinal)) doi = doi[4..].Trim();
        if (doi.Length > 255 || !Regex.IsMatch(doi, @"\A10\.[0-9]{4,9}/[a-z0-9][a-z0-9._;()/:-]*\z", RegexOptions.CultureInvariant))
            throw Invalid("invalidDoi", "Ange DOI som 10.1234/exempel. Formatkontrollen verifierar inte registrering eller rättigheter.");
        return new(uri.AbsoluteUri, name, doi, format);
    }

    internal static AudiobookAcquisitionException Invalid(string code, string message, int status = 400) => new(code, message, status);
}

// Reuse the existing Media-owned SQLite file, connection and write gate. No new database or worker.
public sealed partial class AudiobookAcquisitionStore
{
    public async Task<MediaUrlImportDraft> PreviewUrlImportAsync(MediaUrlImportInput input, DateTimeOffset now, CancellationToken token)
    {
        var normalized = MediaUrlImportValidation.Normalize(input);
        var id = Guid.NewGuid().ToString("N");
        var draft = new MediaUrlImportDraft(id, normalized.SourceUrl!, normalized.FileName!, normalized.Doi!, normalized.Format!,
            $"{normalized.FileName} -- {id}.{normalized.Format}", now, now.AddMinutes(15), "previewed", null, "safeImportUnavailable");
        await gate.WaitAsync(token);
        try
        {
            await using var connection = await OpenAsync(token);
            using var transaction = connection.BeginTransaction();
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT COUNT(*) FROM MediaUrlImportDrafts";
            if (Convert.ToInt64(await command.ExecuteScalarAsync(token), System.Globalization.CultureInfo.InvariantCulture) >= 200)
                throw MediaUrlImportValidation.Invalid("draftLimit", "Gränsen på 200 sparade importavsikter är nådd. Inga poster raderas automatiskt.", 409);
            command.CommandText = "INSERT INTO MediaUrlImportDrafts(Id,DraftJson) VALUES($id,$json)";
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(draft));
            await command.ExecuteNonQueryAsync(token);
            transaction.Commit();
            return draft;
        }
        finally { gate.Release(); }
    }

    public async Task<MediaUrlImportDraft> StartUrlImportAsync(string id, DateTimeOffset now, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            await using var connection = await OpenAsync(token);
            using var transaction = connection.BeginTransaction();
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT DraftJson FROM MediaUrlImportDrafts";
            var drafts = new List<MediaUrlImportDraft>();
            await using (var reader = await command.ExecuteReaderAsync(token))
                while (await reader.ReadAsync(token)) drafts.Add(JsonSerializer.Deserialize<MediaUrlImportDraft>(reader.GetString(0))!);
            var draft = drafts.SingleOrDefault(v => v.Id == id)
                ?? throw MediaUrlImportValidation.Invalid("draftNotFound", "Importen finns inte.", 404);
            if (!draft.CanStart || now < draft.CreatedAtUtc || now >= draft.ExpiresAtUtc)
                throw MediaUrlImportValidation.Invalid("startRejected", "Importen är inte startbar. Förbrukade eller avbrutna importer får inte köras om.", 409);
            if (drafts.Any(v => v.Status is "downloading" or "importing"))
                throw MediaUrlImportValidation.Invalid("importBusy", "En aktiv eller avbruten import kräver avstämning först.", 409);
            draft = draft with { Status = "downloading" };
            command.CommandText = "UPDATE MediaUrlImportDrafts SET DraftJson=$json WHERE Id=$id";
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(draft));
            await command.ExecuteNonQueryAsync(token);
            transaction.Commit();
            return draft;
        }
        finally { gate.Release(); }
    }

    public async Task SaveUrlImportAsync(MediaUrlImportDraft draft, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            await using var connection = await OpenAsync(token);
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE MediaUrlImportDrafts SET DraftJson=$json WHERE Id=$id";
            command.Parameters.AddWithValue("$id", draft.Id);
            command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(draft));
            if (await command.ExecuteNonQueryAsync(token) != 1) throw new IOException("Missing import identity.");
        }
        finally { gate.Release(); }
    }

    public async Task<IReadOnlyList<MediaUrlImportDraft>> ListUrlImportsAsync(CancellationToken token)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DraftJson FROM MediaUrlImportDrafts ORDER BY rowid DESC LIMIT 200";
        await using var reader = await command.ExecuteReaderAsync(token);
        var items = new List<MediaUrlImportDraft>();
        while (await reader.ReadAsync(token)) items.Add(JsonSerializer.Deserialize<MediaUrlImportDraft>(reader.GetString(0))!);
        return items;
    }

    public async Task<MediaUrlImportDraft> ConfirmUrlImportAsync(string id, MediaUrlImportConfirmation input, DateTimeOffset now, CancellationToken token)
    {
        if (!Guid.TryParseExact(id, "N", out _) || !input.DetailsConfirmed || !input.RightsConfirmed)
            throw MediaUrlImportValidation.Invalid("confirmationRequired", "Bekräfta uppgifterna och att du har rätt att hämta och lagra innehållet.");
        await gate.WaitAsync(token);
        try
        {
            await using var connection = await OpenAsync(token);
            using var transaction = connection.BeginTransaction();
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT DraftJson FROM MediaUrlImportDrafts WHERE Id=$id";
            command.Parameters.AddWithValue("$id", id);
            var json = await command.ExecuteScalarAsync(token) as string
                ?? throw MediaUrlImportValidation.Invalid("draftNotFound", "Importavsikten finns inte.", 404);
            var draft = JsonSerializer.Deserialize<MediaUrlImportDraft>(json)!;
            if (draft.ConfirmedAtUtc is not null) return draft; // Idempotent; no external side effect.
            if (now < draft.CreatedAtUtc || now >= draft.ExpiresAtUtc)
                throw MediaUrlImportValidation.Invalid("previewExpired", "Förhandsgranskningen har löpt ut. Granska uppgifterna igen.", 409);
            draft = draft with { Status = "confirmed", ConfirmedAtUtc = now, BlockerCode = "" };
            command.CommandText = "UPDATE MediaUrlImportDrafts SET DraftJson=$json WHERE Id=$id";
            command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(draft));
            await command.ExecuteNonQueryAsync(token);
            transaction.Commit();
            return draft;
        }
        finally { gate.Release(); }
    }
}

public static class MediaUrlImportEndpoints
{
    public static IEndpointRouteBuilder MapMediaUrlImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/modules/media/url-imports");
        group.MapGet("/", async (AudiobookAcquisitionStore store, CancellationToken token) => Results.Ok(await store.ListUrlImportsAsync(token)));
        group.MapPost("/preview", (MediaUrlImportInput input, AudiobookAcquisitionStore store, TimeProvider clock, CancellationToken token) =>
            Execute(async () => Results.Ok(await store.PreviewUrlImportAsync(input, clock.GetUtcNow(), token))));
        group.MapPost("/{id}/confirm", (string id, MediaUrlImportConfirmation input, AudiobookAcquisitionStore store, TimeProvider clock, CancellationToken token) =>
            Execute(async () => Results.Ok(await store.ConfirmUrlImportAsync(id, input, clock.GetUtcNow(), token))));
        group.MapPost("/{id}/start", (string id, MediaUrlImportService service, CancellationToken token) =>
            Execute(async () => Results.Ok(await service.StartAsync(id, token))));
        group.MapPost("/{id}/inspect", (string id, MediaUrlImportService service, CancellationToken token) =>
            Execute(async () => Results.Ok(await service.InspectAsync(id, token))));
        return app;
    }

    private static async Task<IResult> Execute(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (AudiobookAcquisitionException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: "Importen kunde inte förberedas", detail: exception.SafeMessage,
                extensions: new Dictionary<string, object?> { ["code"] = exception.Code });
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or JsonException or OperationCanceledException)
        {
            return Results.Problem(statusCode: 503, title: "Importtjänsten kunde inte nås",
                detail: "Kontrollera sparad status. Ingen automatisk omkörning görs.");
        }

    }
}
