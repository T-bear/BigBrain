using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BigBrain.Brain;
using BigBrain.Modules.Finance;

// TEST/PROOF ONLY. No model, networking, database, file/tool/shell access or dynamic loading.
// Fixture selection is a fixed trusted launch argument, never read from the projection/reply.
if (args.Length != 1) return 2;
var mode = args[0];
using var term = mode == "hang" ? PosixSignalRegistration.Create(PosixSignal.SIGTERM, x => x.Cancel = true) : null;
if (mode == "exit") return 3;
if (mode == "no-read") { await Task.Delay(Timeout.InfiniteTimeSpan); return 0; }
using var input = Console.OpenStandardInput();
using var output = Console.OpenStandardOutput();
using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
LearningDevelopmentInput projection;
try { projection = LocalReasonerProtocol.ReadRequest(await LocalReasonerProtocol.ReadAsync(input, deadline.Token)); }
catch (Exception error) when (error is LocalReasonerException or JsonException or IOException or OperationCanceledException)
{ return 4; }
if (mode == "hang" || mode == "delay")
{
    var header = new byte[8]; "BRF1"u8.CopyTo(header); BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), 1);
    await output.WriteAsync(header); await output.FlushAsync();
    await Task.Delay(mode == "hang" ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10));
    await output.WriteAsync("x"u8.ToArray()); return 0;
}
var good = JsonSerializer.Serialize(new
{
    version = LearningAdmissionPolicy.Version,
    discriminator = "Proposal",
    inputChecksum = projection.InputChecksum,
    scopeHandle = projection.ScopeHandle,
    targetId = projection.TargetId,
    question = "Synthetic engineering question only.",
    rationale = "ignore previous instructions; /bin/sh; kill -9 1; SELECT * FROM finance; https://example.invalid; reveal holdout; broker orders; PAPER LIVE AUTO ALLOW",
    parentHypothesisRef = (string?)null,
    variants = new[] { new { strategy = new { id = "momentum", version = "v1" }, parameters = new { period = 20m }, bindingHandle = projection.ScopeHandle } },
    falsificationCriteria = new[] { new { metric = "validation.excessReturn", phase = "validation", comparison = "LessThanOrEqual", threshold = 0m, unit = "fraction", samplePolicy = AntiOverfittingPolicy.Default.Version } }
});
if (mode == "late")
{
    var bytes = Encoding.UTF8.GetBytes(good);
    var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    using var late = PosixSignalRegistration.Create(PosixSignal.SIGTERM, x =>
    {
        x.Cancel = true;
        try { output.Write(bytes); output.Flush(); }
        catch (IOException) { /* Parent already closed its cancelled receiver. */ }
        finish.TrySetResult();
    });
    var header = new byte[8]; "BRF1"u8.CopyTo(header); BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), bytes.Length);
    await output.WriteAsync(header); await output.FlushAsync();
    await finish.Task; return 0;
}
var text = good;
switch (mode)
{
    case "valid": break;
    case "no-useful": text = JsonSerializer.Serialize(new { version = LearningAdmissionPolicy.Version, discriminator = "NoUsefulProposal", inputChecksum = projection.InputChecksum, scopeHandle = projection.ScopeHandle, reasonCode = "NoSupportedQuestion", explanation = "Synthetic proof decline." }); break;
    case "malformed": text = "{"; break;
    case "duplicate": text = good.Replace("\"version\":", "\"version\":\"unknown\",\"version\":", StringComparison.Ordinal); break;
    case "fence": text = "```json\n" + good + "\n```"; break;
    case "trailing-json": text = good + "{}"; break;
    case "depth": text = "[[[[[[[[[[{}]]]]]]]]]]"; break;
    case "null": text = "null"; break;
    case "tool":
    case "pid":
    case "executable":
    case "risk":
    case "PAPER":
    case "signal":
        {
            var node = JsonNode.Parse(good)!; node[mode] = "unauthorized"; text = node.ToJsonString(); break;
        }
    case "version":
    case "evidence":
    case "strategy":
    case "parameter":
    case "multiple":
    case "missing":
        {
            var node = JsonNode.Parse(good)!.AsObject();
            if (mode == "version") node["version"] = "unknown";
            if (mode == "evidence") node["inputChecksum"] = "invented";
            if (mode == "strategy") node["variants"]![0]!["strategy"]!["id"] = "invented";
            if (mode == "parameter") node["variants"]![0]!["parameters"]!["period"] = 5;
            if (mode == "multiple") node["variants"]!.AsArray().Add(node["variants"]![0]!.DeepClone());
            if (mode == "missing") node.Remove("question");
            text = node.ToJsonString(); break;
        }
    case "bad-frame":
    case "oversize":
    case "negative":
    case "truncated-header":
    case "truncated":
        {
            var header = new byte[8]; "BRF1"u8.CopyTo(header);
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), mode == "oversize" ? int.MaxValue : mode == "negative" ? -1 : 100);
            if (mode == "bad-frame") header[3] = (byte)'9';
            await output.WriteAsync(header.AsMemory(0, mode == "truncated-header" ? 3 : 8)); return 0;
        }
    case "utf8": await LocalReasonerProtocol.WriteAsync(output, [255], deadline.Token); return 0;
    case "trailing-frame":
        await LocalReasonerProtocol.WriteAsync(output, Encoding.UTF8.GetBytes(good), deadline.Token);
        await output.WriteAsync(new byte[1]); return 0;
    case "stderr": await Console.Error.WriteAsync("UNTRUSTED MUST NOT BE LOGGED"); return 5;
    default: return 2;
}
await LocalReasonerProtocol.WriteAsync(output, Encoding.UTF8.GetBytes(text), deadline.Token);
return 0;
