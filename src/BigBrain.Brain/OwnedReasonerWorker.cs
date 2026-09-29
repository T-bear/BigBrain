using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace BigBrain.Brain;

// Not a process-management service. No attach/PID/name overloads; constructor creates the child.
internal sealed class OwnedReasonerWorker : IDisposable
{
    private readonly Process _child;
    private readonly SafeFileHandle? _identity;
    private bool _disposed;
    private bool _exitedAtDisposal;
    internal Stream Input => _child.StandardInput.BaseStream;
    internal Stream Output => _child.StandardOutput.BaseStream;
    internal Stream Error => _child.StandardError.BaseStream;
    internal bool Exited => !_disposed && _child.HasExited;
    internal int ExitCode => _child.ExitCode;
    internal bool IdentityConfirmed => _identity is not null || _child.HasExited;

    internal OwnedReasonerWorker(LocalReasonerRuntimeOptions options)
    {
        // Probe required identity primitive before spawn. Never fall back to numeric PID signaling.
        using var probe = OpenIdentity(Environment.ProcessId);
        if (SendSignal(probe, 0, IntPtr.Zero, 0) != 0) throw new Win32Exception(); // Availability/permission probe, no signal.
        var start = new ProcessStartInfo(options.WorkerExecutable)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(options.WorkerExecutable)!
        };
        start.Environment.Clear(); // No inherited credentials, profiler/startup hooks or model configuration.
        foreach (var argument in options.WorkerArguments) start.ArgumentList.Add(argument);
        _child = new Process { StartInfo = start };
        try
        {
            if (!_child.Start()) throw new LocalReasonerException(LocalReasonerFailure.WorkerFailed);
        }
        catch { _child.Dispose(); throw; }
        try
        {
            var identity = OpenIdentity(_child.Id); // Internal spawned child only, never protocol/user data.
            // .NET retains this child's exit state. If it was reaped before pidfd_open, reject the fd:
            // it could belong to a recycled PID. After this check, pidfd never retargets on PID reuse.
            if (_child.HasExited) identity.Dispose();
            else _identity = identity;
        }
        catch (Exception error) when (error is Win32Exception or IOException)
        {
            // Leave the runtime reservation occupied unless this exact child is confirmed exited.
            // No unsafe kill(PID) fallback, no re-attachment.
            // Constructor retains the child even when identity acquisition fails. Caller fails closed.
            _ = _child.HasExited;
        }
    }

    internal Task WaitAsync(CancellationToken token) => _child.WaitForExitAsync(token);

    internal async Task<bool> StopAsync(TimeSpan grace, TimeSpan reap)
    {
        if (_disposed) return _exitedAtDisposal; // Authority ended; disposal alone is not proof of exit.
        if (_child.HasExited) return true;
        if (_identity is null || _identity.IsClosed || _identity.IsInvalid) return false;
        try
        {
            Signal(15); // SIGTERM to retained identity, not a caller-selected signal.
            if (await WaitBounded(grace).ConfigureAwait(false)) return true;
            Signal(9); // SIGKILL only to same owned identity; no tree traversal.
            return await WaitBounded(reap).ConfigureAwait(false);
        }
        catch (Win32Exception) { return _child.HasExited; }
    }

    private async Task<bool> WaitBounded(TimeSpan timeout)
    {
        using var deadline = new CancellationTokenSource(timeout);
        try { await _child.WaitForExitAsync(deadline.Token).ConfigureAwait(false); return true; }
        catch (OperationCanceledException) { return _child.HasExited; }
    }
    private void Signal(int signal)
    {
        if (_disposed || _child.HasExited) return;
        if (SendSignal(_identity!, signal, IntPtr.Zero, 0) != 0 && Marshal.GetLastPInvokeError() != 3)
            throw new Win32Exception(); // ESRCH means original child is already gone; never lookup another PID.
    }
    public void Dispose()
    {
        if (_disposed) return;
        _exitedAtDisposal = _child.HasExited;
        _disposed = true; _identity?.Dispose(); _child.Dispose();
    }
    private static SafeFileHandle OpenIdentity(int child)
    {
        var fd = OpenPidFd(child, 0);
        if (fd < 0) throw new Win32Exception();
        return new SafeFileHandle((IntPtr)fd, ownsHandle: true);
    }

#pragma warning disable SYSLIB1054 // Small Linux/glibc ABI; avoids unsafe generated code in this contract library.
    [DllImport("libc.so.6", EntryPoint = "pidfd_open", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int OpenPidFd(int pid, uint flags);
    [DllImport("libc.so.6", EntryPoint = "pidfd_send_signal", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int SendSignal(SafeFileHandle pidfd, int signal, IntPtr info, uint flags);
#pragma warning restore SYSLIB1054
}
