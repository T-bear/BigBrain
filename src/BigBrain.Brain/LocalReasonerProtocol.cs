using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BigBrain.Modules.Finance;

namespace BigBrain.Brain;

// Inherited pipes, one request and one reply. Never a command/tool channel.
internal static class LocalReasonerProtocol
{
    internal const int MaximumBytes = 65536;
    internal const int HeaderBytes = 8;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { MaxDepth = 8, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

    internal static byte[] Request(LearningDevelopmentInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        // Bound every variable-sized member before serializer allocation. This is not eligibility.
        string[] texts = [input.Version, input.ProjectionVersion, input.ScopeHandle, input.TargetId,
            input.DevelopmentChecksum, input.AllowedStrategy, input.HistoryChecksum, input.InputChecksum];
        if (texts.Any(x => x is null || x.Length is 0 or > 80)) throw Failure();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(input, Json);
        if (bytes.Length > MaximumBytes) throw Failure();
        return bytes;
    }

    internal static LearningDevelopmentInput ReadRequest(byte[] bytes)
    {
        var text = Decode(bytes);
        using var doc = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 8 });
        Unique(doc.RootElement);
        var input = JsonSerializer.Deserialize<LearningDevelopmentInput>(text, Json) ?? throw Failure();
        // Exact closed canonical projection: catches missing/getter fields and altered fixed limits/history.
        using var canonical = JsonDocument.Parse(Request(input));
        if (!JsonElement.DeepEquals(doc.RootElement, canonical.RootElement)) throw Failure();
        return input;
    }

    internal static string Decode(byte[] bytes)
    {
        if (bytes.Length is 0 or > MaximumBytes) throw Failure();
        try { return StrictUtf8.GetString(bytes); }
        catch (DecoderFallbackException) { throw Failure(); }
    }

    internal static async Task WriteAsync(Stream stream, byte[] bytes, CancellationToken token)
    {
        if (bytes.Length is 0 or > MaximumBytes) throw Failure();
        var header = new byte[HeaderBytes];
        "BRF1"u8.CopyTo(header);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), bytes.Length);
        await stream.WriteAsync(header, token).ConfigureAwait(false);
        await stream.WriteAsync(bytes, token).ConfigureAwait(false);
        await stream.FlushAsync(token).ConfigureAwait(false);
    }

    internal static async Task<byte[]> ReadAsync(Stream stream, CancellationToken token, Action? headerReceived = null)
    {
        var header = new byte[HeaderBytes];
        await stream.ReadExactlyAsync(header, token).ConfigureAwait(false);
        if (!header.AsSpan(0, 4).SequenceEqual("BRF1"u8)) throw Failure();
        var length = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(4));
        if (length is <= 0 or > MaximumBytes) throw Failure(); // Before payload allocation/read.
        headerReceived?.Invoke();
        var bytes = new byte[length];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        var trailing = new byte[1];
        if (await stream.ReadAsync(trailing, token).ConfigureAwait(false) != 0) throw Failure();
        return bytes; // EOF is required only after the explicitly sized single frame.
    }

    private static void Unique(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in element.EnumerateObject())
            { if (!keys.Add(field.Name)) throw Failure(); Unique(field.Value); }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) Unique(item);
    }
    private static LocalReasonerException Failure() => new(LocalReasonerFailure.InvalidTransport);
}
