using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace BigBrain.Api.Media;

public interface IDirectAudioTransport
{
    Task<DownloadedAudio> FetchAsync(MediaUrlImportDraft draft, CancellationToken token);
}

public sealed class DownloadedAudio(FileStream stream, string checksum, long size) : IAsyncDisposable
{
    public FileStream Stream { get; } = stream;
    public string Checksum { get; } = checksum;
    public long Size { get; } = size;
    public ValueTask DisposeAsync() => Stream.DisposeAsync();
}

public sealed class DirectAudioTransport(
    Func<string, CancellationToken, Task<IPAddress[]>>? resolve = null,
    Func<IPEndPoint, CancellationToken, ValueTask<Stream>>? connect = null) : IDirectAudioTransport
{
    public const long MaximumBytes = 64 * 1024 * 1024;

    // This initial adapter deliberately supports public IPv4 only. Mixed/private/IPv6 answers fail closed.
    public static bool IsPublicAddress(IPAddress ip)
    {
        if (ip.AddressFamily != AddressFamily.InterNetwork) return false;
        var b = ip.GetAddressBytes();
        return b[0] is not (0 or 10 or 127) && b[0] < 224
            && !(b[0] == 100 && b[1] is >= 64 and <= 127)
            && !(b[0] == 169 && b[1] == 254)
            && !(b[0] == 172 && b[1] is >= 16 and <= 31)
            && !(b[0] == 192 && (b[1] == 168 || b[1] == 0 || b[1] == 2 || b[1] == 88 && b[2] == 99))
            && !(b[0] == 198 && (b[1] is 18 or 19 || b[1] == 51 && b[2] == 100))
            && !(b[0] == 203 && b[1] == 0 && b[2] == 113);
    }

    public async ValueTask<Stream> ConnectPublicAsync(string hostname, CancellationToken token)
    {
        var addresses = await (resolve?.Invoke(hostname, token) ?? Dns.GetHostAddressesAsync(hostname, token));
        if (addresses.Length == 0 || addresses.Any(ip => !IsPublicAddress(ip)))
            throw new HttpRequestException("Destination denied.");
        var endpoint = new IPEndPoint(addresses[0], 443);
        if (connect is not null) return await connect(endpoint, token);
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(endpoint, token);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch { socket.Dispose(); throw; }
    }

    public async Task<DownloadedAudio> FetchAsync(MediaUrlImportDraft draft, CancellationToken token)
    {
        _ = MediaUrlImportValidation.Normalize(new(draft.SourceUrl, draft.FileName, draft.Doi, draft.Format));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(60));
        using var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = TimeSpan.FromSeconds(10),
            MaxResponseHeadersLength = 16,
            ConnectCallback = (context, cancellation) => ConnectPublicAsync(context.DnsEndPoint.Host, cancellation)
        };
        using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        using var request = new HttpRequestMessage(HttpMethod.Get, draft.SourceUrl)
        { Version = HttpVersion.Version11, VersionPolicy = HttpVersionPolicy.RequestVersionExact };
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        return await ReadResponseAsync(response, draft.Format, deadline.Token);
    }

    public static bool MatchesMime(string format, string? mime) => format switch
    {
        "mp3" => string.Equals(mime, "audio/mpeg", StringComparison.OrdinalIgnoreCase),
        "m4a" => string.Equals(mime, "audio/mp4", StringComparison.OrdinalIgnoreCase) || string.Equals(mime, "audio/x-m4a", StringComparison.OrdinalIgnoreCase),
        _ => false
    };

    public static async Task<DownloadedAudio> ReadResponseAsync(HttpResponseMessage response, string format, CancellationToken token)
    {
        if (response.StatusCode != HttpStatusCode.OK || format is not ("mp3" or "m4a")
            || response.Content.Headers.ContentEncoding.Count != 0
            || !MatchesMime(format, response.Content.Headers.ContentType?.MediaType)
            || response.Content.Headers.ContentLength is > MaximumBytes or < 8)
            throw MediaUrlImportValidation.Invalid("sourceRejected", "Källan måste ge en direkt ljudfil med MIME-typ som motsvarar valt MP3/M4A-format. Omdirigering, annat format eller för stor fil avvisas.", 422);
        var fileOptions = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.ReadWrite,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous | FileOptions.DeleteOnClose
        };
        if (!OperatingSystem.IsWindows()) fileOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        var stream = new FileStream(Path.Combine(Path.GetTempPath(), "bb158-" + Guid.NewGuid().ToString("N")), fileOptions);
        try
        {
            if (OperatingSystem.IsLinux()) File.Delete(stream.Name); // anonymous open inode: crash cannot strand staged audio
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using var source = await response.Content.ReadAsStreamAsync(token);
            var buffer = new byte[65536];
            long size = 0;
            int count;
            while ((count = await source.ReadAsync(buffer, token)) > 0)
            {
                size += count;
                if (size > MaximumBytes) throw MediaUrlImportValidation.Invalid("sourceTooLarge", "Filen överskrider 64 MiB.", 422);
                hash.AppendData(buffer.AsSpan(0, count));
                await stream.WriteAsync(buffer.AsMemory(0, count), token);
            }
            if (size < 8 || response.Content.Headers.ContentLength is long expected && expected != size)
                throw MediaUrlImportValidation.Invalid("sourceIncomplete", "Ljudfilen är ofullständig.", 422);
            await stream.FlushAsync(token);
            stream.Position = 0;
            return new(stream, Convert.ToHexStringLower(hash.GetHashAndReset()), size);
        }
        catch { await stream.DisposeAsync(); throw; }
    }
}
