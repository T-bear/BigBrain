using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using BigBrain.Modules.Finance;
using Microsoft.Win32.SafeHandles;

namespace BigBrain.Api.Finance;

// One bounded, independently provisioned consumption journal, not a result store or grant issuer.
// Host root is trusted to exclude it from all Finance/lifecycle/config restore operations.
internal sealed class RecurringResearchAuthority
{
    private const int MaximumBytes = 131072;
    private const int MaximumEntries = 64;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly Func<FileStream> _open;
    private readonly string _enrollmentChecksum;
    internal Action? AfterAdvanceTestHook { get; set; }
    private sealed record Entry(string Version, int Sequence, string Program, string Grant, string State,
        int Invocations, int Evaluations, int Trials, int Calls, int Seconds, string Previous, string Checksum);

    // Only friend tests supply an unprotected fixture stream; no configuration can select this factory.
    internal RecurringResearchAuthority(Func<FileStream> openFixture, string enrollmentChecksum)
    { _open = openFixture; _enrollmentChecksum = enrollmentChecksum; }

    internal static RecurringResearchAuthority FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Finance:RecurringAuthority");
        var path = section["JournalPath"];
        var checksum = section["EnrollmentChecksum"];
        var decision = section["RestoreBoundaryDecision"];
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) ||
            string.IsNullOrWhiteSpace(checksum) || checksum.Length != 71 || !checksum.StartsWith("sha256:", StringComparison.Ordinal) || !checksum[7..].All(Uri.IsHexDigit) ||
            string.IsNullOrWhiteSpace(decision) || decision.Length > 256 || decision.Any(char.IsControl))
            throw new RecurringResearchReconciliationRequiredException();
        var finance = configuration.GetSection(EodhdFinanceOptions.Section).Get<EodhdFinanceOptions>() ?? new();
        var lifecycle = configuration.GetSection(BigBrain.Api.SystemRecovery.SystemRecoveryOptions.SectionName)
            .Get<BigBrain.Api.SystemRecovery.SystemRecoveryOptions>() ?? new();
        foreach (var root in new[] { Path.GetDirectoryName(Path.GetFullPath(finance.DatabasePath))!,
            Path.GetDirectoryName(Path.GetFullPath(lifecycle.DatabasePath))! })
        {
            var relative = Path.GetRelativePath(root, path);
            if (relative != ".." && !relative.StartsWith("../", StringComparison.Ordinal) && !Path.IsPathFullyQualified(relative))
                throw new RecurringResearchReconciliationRequiredException();
        }
        return new(() => ProtectedAppendJournal.Open(path), checksum);
    }

    // Explicit offline enrollment output only. Startup never seeds, recreates or repairs a journal.
    internal static string Enrollment(RecurringResearchProgram program)
    {
        if (program.State != RecurringResearchState.Ready || program.Invocations != 0 || program.Evaluations != 0 ||
            !program.Attempts.IsEmpty || program.Trials != 0 || program.Calls != 0 || program.ReservedSeconds != 0)
            throw new RecurringResearchReconciliationRequiredException();
        return JsonSerializer.Serialize(Make(program, 0, ""), Json) + "\n";
    }
    internal static string EnrollmentChecksum(string enrollment) =>
        (JsonSerializer.Deserialize<Entry>(enrollment, Json) ?? throw new InvalidDataException()).Checksum;

    internal void Require(RecurringResearchProgram state) => Access(state, null);
    internal void Advance(RecurringResearchProgram previous, RecurringResearchProgram next) => Access(previous, next);

    private void Access(RecurringResearchProgram previous, RecurringResearchProgram? next)
    {
        try
        {
            using var stream = _open();
            // Nonblocking exclusive kernel lock; busy is a safe refusal, never a retry loop.
            if (NativeFlock(stream.SafeFileHandle, 2 | 4) != 0) throw new IOException();
            if (stream.Length is <= 0 or > MaximumBytes) throw new InvalidDataException();
            stream.Position = 0;
            var bytes = new byte[checked((int)stream.Length)]; stream.ReadExactly(bytes);
            if (bytes[^1] != (byte)'\n') throw new InvalidDataException();
            var lines = new UTF8Encoding(false, true).GetString(bytes).Split('\n');
            if (lines.Length < 2 || lines.Length > MaximumEntries + 1) throw new InvalidDataException();
            Entry? last = null;
            foreach (var line in lines[..^1])
            {
                var entry = JsonSerializer.Deserialize<Entry>(line, Json) ?? throw new InvalidDataException();
                if (entry.Version != "finance-recurring-authority-v1" || entry.Checksum != LearningIdentity.Hash(entry with { Checksum = "" }) ||
                    entry.Sequence != (last?.Sequence + 1 ?? 0) || entry.Previous != (last?.Checksum ?? "") ||
                    entry.Program != previous.Id || entry.Grant != LearningIdentity.Hash(previous.Authorization) ||
                    entry.Invocations < (last?.Invocations ?? 0) || entry.Evaluations < (last?.Evaluations ?? 0) ||
                    entry.Trials != entry.Evaluations * 3 || entry.Calls != entry.Evaluations * 64 ||
                    entry.Seconds != entry.Invocations * previous.Authorization.Budget.CompletionSeconds ||
                    entry.Invocations > previous.Authorization.Budget.Invocations || entry.Evaluations > previous.Authorization.Budget.Evaluations ||
                    last is null && (entry.Checksum != _enrollmentChecksum || entry.Invocations != 0 || entry.Evaluations != 0))
                    throw new InvalidDataException();
                last = entry;
            }
            if (last is null || last != Make(previous, last.Sequence, last.Previous)) throw new InvalidDataException();
            if (next is null) return;
            if (next.Id != previous.Id || LearningIdentity.Hash(next.Authorization) != last.Grant ||
                next.Invocations < previous.Invocations || next.Evaluations < previous.Evaluations ||
                next.ReservedSeconds < previous.ReservedSeconds || last.Sequence + 1 >= MaximumEntries)
                throw new InvalidDataException();
            var record = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Make(next, last.Sequence + 1, last.Checksum), Json) + "\n");
            if (stream.Length + record.Length > MaximumBytes) throw new InvalidDataException();
            stream.Position = stream.Length; stream.Write(record); stream.Flush(flushToDisk: true);
            AfterAdvanceTestHook?.Invoke(); // crash witness: durable journal ahead of uncommitted SQLite
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or
            InvalidDataException or ArgumentException or OverflowException)
        { throw new RecurringResearchReconciliationRequiredException(); }
    }
    private static Entry Make(RecurringResearchProgram state, int sequence, string previous)
    {
        var entry = new Entry("finance-recurring-authority-v1", sequence, state.Id, LearningIdentity.Hash(state.Authorization), LearningIdentity.Hash(state),
            state.Invocations, state.Evaluations, state.Trials, state.Calls, state.ReservedSeconds, previous, "");
        return entry with { Checksum = LearningIdentity.Hash(entry) };
    }
    [DllImport("libc.so.6", EntryPoint = "flock", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int NativeFlock(SafeFileHandle fd, int operation);
}

// Linux statx ABI, descriptor-relative traversal, no shell or permission mutation.
internal static class ProtectedAppendJournal
{
    private const int DirectoryFlags = 0x10000 | 0x20000 | 0x80000; // DIRECTORY, NOFOLLOW, CLOEXEC
    [StructLayout(LayoutKind.Explicit, Size = 256)]
    private struct Stat
    {
        [FieldOffset(0)] internal uint Mask;
        [FieldOffset(8)] internal ulong Attributes;
        [FieldOffset(16)] internal uint Links;
        [FieldOffset(20)] internal uint Uid;
        [FieldOffset(28)] internal ushort Mode;
        [FieldOffset(56)] internal ulong AttributesMask;
    }
    internal static FileStream Open(string path)
    {
        if (!OperatingSystem.IsLinux() || IntPtr.Size != 8 || NativeUid() == 0)
            throw new RecurringResearchReconciliationRequiredException();
        var status = File.ReadAllLines("/proc/self/status");
        foreach (var field in new[] { "CapEff", "CapPrm", "CapAmb" })
            if (status.Single(x => x.StartsWith(field + ":", StringComparison.Ordinal)).Split(':')[1].Trim() != "0000000000000000")
                throw new RecurringResearchReconciliationRequiredException();
        if (status.Single(x => x.StartsWith("NoNewPrivs:", StringComparison.Ordinal)).Split(':')[1].Trim() != "1")
            throw new RecurringResearchReconciliationRequiredException();
        var full = Path.GetFullPath(path);
        if (full != path) throw new RecurringResearchReconciliationRequiredException();
        SafeFileHandle directory = Handle(NativeOpenAt(-100, "/", DirectoryFlags));
        try
        {
            Check(directory, directory: true);
            var parts = full.Split('/', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts[..^1])
            {
                var next = Handle(NativeOpenAt(directory.DangerousGetHandle().ToInt32(), part, DirectoryFlags));
                directory.Dispose(); directory = next; Check(directory, directory: true);
            }
            // Kernel O_APPEND is required: managed append seeking alone is insufficient.
            var file = Handle(NativeOpenAt(directory.DangerousGetHandle().ToInt32(), parts[^1], 2 | 0x400 | 0x20000 | 0x80000));
            try
            {
                Check(file, directory: false);
                var filesystem = Marshal.AllocHGlobal(256);
                try
                {
                    if (NativeStatFs(file, filesystem) != 0 || Marshal.ReadInt64(filesystem) != 0xef53)
                        throw new RecurringResearchReconciliationRequiredException(); // local ext4 only
                }
                finally { Marshal.FreeHGlobal(filesystem); }
                return new FileStream(file, FileAccess.ReadWrite, 4096, false);
            }
            catch { file.Dispose(); throw; }
        }
        finally { directory.Dispose(); }
    }
    private static void Check(SafeFileHandle handle, bool directory)
    {
        if (NativeStat(handle, "", 0x1000, 0x7ff, out var value) != 0)
            throw new RecurringResearchReconciliationRequiredException();
        RequireMetadata(value.Mask, value.Uid, value.Mode, value.Links, value.AttributesMask, value.Attributes, directory);
    }
    internal static void RequireMetadata(uint mask, uint uid, ushort mode, uint links, ulong attributesMask, ulong attributes, bool directory)
    {
        if ((mask & 0xf) != 0xf || uid != 0 || (mode & 0xf000) != (directory ? 0x4000 : 0x8000) ||
            (directory ? (mode & 0x12) != 0 : links != 1 || (mode & 7) != 0 ||
                (attributesMask & 0x20) == 0 || (attributes & 0x20) == 0))
            throw new RecurringResearchReconciliationRequiredException();
    }
    [DllImport("libc.so.6", EntryPoint = "fstatfs", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int NativeStatFs(SafeFileHandle fd, IntPtr buffer);
    private static SafeFileHandle Handle(int fd) => fd >= 0 ? new(new IntPtr(fd), true) : throw new RecurringResearchReconciliationRequiredException();
    [DllImport("libc.so.6", EntryPoint = "openat", SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int NativeOpenAt(int directory, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);
    [DllImport("libc.so.6", EntryPoint = "statx", SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int NativeStat(SafeFileHandle fd, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags, uint mask, out Stat value);
    [DllImport("libc.so.6", EntryPoint = "geteuid")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern uint NativeUid();
}
