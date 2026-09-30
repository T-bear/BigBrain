using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Runtime.ExceptionServices;
using System.Text;
using BigBrain.Modules.Finance;

namespace BigBrain.Brain;

// Not registered in API. Trusted composition supplies the artifact, private reservation directory
// and bounded operational sink. Finance must reserve invocation first and re-admit every reply.
public sealed class LocalReasonerRuntime : IResearchReasoner, IAsyncDisposable
{
    private readonly LocalReasonerRuntimeOptions _options;
    private readonly Action<LocalReasonerAudit> _audit;
    private readonly string _workerHash;
    private readonly Lock _gate = new();
    private readonly CancellationTokenSource _disabled = new();
    private TaskCompletionSource? _active;
    private bool _enabled;
    private bool _disposed;

    public LocalReasonerRuntime(LocalReasonerRuntimeOptions options, Action<LocalReasonerAudit> audit)
    {
        ArgumentNullException.ThrowIfNull(options); ArgumentNullException.ThrowIfNull(audit);
        options.Validate();
        _options = options; _audit = audit; _enabled = options.Enabled;
        _workerHash = Hash(Encoding.UTF8.GetBytes(options.WorkerExecutable + "\0" + string.Join('\0', options.WorkerArguments)));
    }

    // One-way kill switch. Re-enabling requires new explicit trusted composition, never automatic retry.
    public void Disable()
    {
        lock (_gate)
        {
            _enabled = false;
            if (!_disposed) _disabled.Cancel();
        }
    }

    public async Task<LearningReasonerReply> ReasonAsync(LearningDevelopmentInput input, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("Linux runtime required.");
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_enabled || _disposed) throw new LocalReasonerException(LocalReasonerFailure.Disabled);
            if (_active is not null) throw new LocalReasonerException(LocalReasonerFailure.Busy);
            _active = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        var id = Guid.NewGuid(); var watch = Stopwatch.StartNew();
        var outcome = LocalReasonerOutcome.Failed; LocalReasonerFailure? failure = null;
        var inputHash = ""; string? responseHash = null;
        int? workerExitCode = null; bool? workerCleanupRequired = null;
        LearningAdmissionReason? replyRejection = null;
        FileStream? reservation = null; OwnedReasonerWorker? worker = null;
        var path = Path.Combine(_options.CoordinationDirectory, ".local-reasoner-reservation");
        var stopped = true; var audited = false;
        LearningReasonerReply? reply = null; Exception? pendingError = null;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disabled.Token);
        deadline.CancelAfter(_options.InvocationTimeout);
        try
        {
            byte[] request;
            try { request = LocalReasonerProtocol.Request(input); }
            catch (LocalReasonerException) { throw new LocalReasonerException(LocalReasonerFailure.InvalidInput); }
            inputHash = Hash(request);
            try
            {
                reservation = new FileStream(path, new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    Options = FileOptions.WriteThrough,
                    UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite
                });
                reservation.WriteByte(1); reservation.Flush(flushToDisk: true);
            }
            catch (IOException) when (reservation is null && File.Exists(path))
            { throw new LocalReasonerException(LocalReasonerFailure.Busy); }
            Audit(LocalReasonerAuditPhase.Started, LocalReasonerOutcome.Started);
            deadline.Token.ThrowIfCancellationRequested();
            worker = new OwnedReasonerWorker(_options);
            if (!worker.IdentityConfirmed) throw new LocalReasonerException(LocalReasonerFailure.StopUnconfirmed);
            var response = LocalReasonerProtocol.ReadAsync(worker.Output, deadline.Token, () => Audit(LocalReasonerAuditPhase.ResponseStarted, LocalReasonerOutcome.Started));
            await Task.WhenAll(Send(worker, request, deadline.Token), RejectError(worker.Error, deadline.Token), response).ConfigureAwait(false);
            await worker.WaitAsync(deadline.Token).ConfigureAwait(false);
            if (worker.ExitCode != 0) throw new LocalReasonerException(LocalReasonerFailure.WorkerFailed);
            deadline.Token.ThrowIfCancellationRequested();
            var bytes = await response.ConfigureAwait(false);
            var parsed = LearningReplyParser.Parse(LocalReasonerProtocol.Decode(bytes), input);
            if (parsed.Reply is null)
            {
                replyRejection = parsed.Rejection;
                throw new LocalReasonerException(LocalReasonerFailure.InvalidReply) { ReplyRejection = replyRejection };
            }
            responseHash = Hash(bytes);
            deadline.Token.ThrowIfCancellationRequested(); // Disable/cancellation never admits a late reply.
            outcome = parsed.Reply is LearningReasonerReply.Proposal ? LocalReasonerOutcome.Proposal : LocalReasonerOutcome.NoUsefulProposal;
            reply = parsed.Reply;
        }
        catch (OperationCanceledException error)
        {
            if (cancellationToken.IsCancellationRequested || _disabled.IsCancellationRequested)
            { outcome = LocalReasonerOutcome.Cancelled; pendingError = error; }
            else { failure = LocalReasonerFailure.Timeout; pendingError = new LocalReasonerException(failure.Value); }
        }
        catch (LocalReasonerException error) { failure = error.Failure; pendingError = error; }
        catch (Exception error) when (error is IOException or Win32Exception or InvalidOperationException or UnauthorizedAccessException
            or EntryPointNotFoundException or DllNotFoundException)
        { failure = LocalReasonerFailure.WorkerFailed; pendingError = new LocalReasonerException(failure.Value); }
        finally
        {
            try
            {
                if (worker is not null)
                {
                    workerCleanupRequired = !worker.Exited;
                    stopped = await worker.StopAsync(_options.GracePeriod, _options.ReapTimeout).ConfigureAwait(false);
                    if (stopped && worker.Exited) workerExitCode = worker.ExitCode;
                }
                if (!stopped) { failure = LocalReasonerFailure.StopUnconfirmed; outcome = LocalReasonerOutcome.Failed; }
                ObserveLateCancellation();
                Audit(LocalReasonerAuditPhase.Completed, outcome); audited = true;
            }
            catch (LocalReasonerException error) { pendingError = error; }
            finally
            {
                worker?.Dispose(); reservation?.Dispose();
                try
                {
                    // Crash, uncertain termination or failed terminal audit never silently frees the slot.
                    if (reservation is not null && stopped && audited)
                    {
                        try { File.Delete(path); }
                        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                        {
                            failure = LocalReasonerFailure.StopUnconfirmed;
                            pendingError = new LocalReasonerException(failure.Value);
                            try { Audit(LocalReasonerAuditPhase.Completed, LocalReasonerOutcome.Failed); }
                            catch (LocalReasonerException auditError) { pendingError = auditError; }
                        }
                    }
                }
                finally
                {
                    lock (_gate)
                    {
                        if (failure is not null && failure != LocalReasonerFailure.Busy || !stopped || !audited) _enabled = false;
                        _active!.TrySetResult(); _active = null;
                    }
                }
            }
        }
        if (!stopped) pendingError = new LocalReasonerException(LocalReasonerFailure.StopUnconfirmed);
        if (pendingError is not null) ExceptionDispatchInfo.Capture(pendingError).Throw();
        // A trusted sink/cleanup can take time or trigger disable. Never deliver an expired reply.
        ObserveLateCancellation();
        if (pendingError is not null)
        {
            lock (_gate) { _enabled = false; }
            Audit(LocalReasonerAuditPhase.Completed, outcome);
            ExceptionDispatchInfo.Capture(pendingError).Throw();
        }
        return reply!;

        void ObserveLateCancellation()
        {
            if (pendingError is not null || !deadline.IsCancellationRequested) return;
            responseHash = null;
            if (cancellationToken.IsCancellationRequested || _disabled.IsCancellationRequested)
            { outcome = LocalReasonerOutcome.Cancelled; pendingError = new OperationCanceledException(deadline.Token); }
            else
            {
                failure = LocalReasonerFailure.Timeout; outcome = LocalReasonerOutcome.Failed;
                pendingError = new LocalReasonerException(failure.Value);
            }
        }

        void Audit(LocalReasonerAuditPhase phase, LocalReasonerOutcome result)
        {
            try
            {
                _audit(new(id, phase, result, failure, DateTimeOffset.UtcNow, watch.ElapsedMilliseconds, inputHash, _workerHash, responseHash)
                { WorkerExitCode = workerExitCode, WorkerCleanupRequired = workerCleanupRequired, ReplyRejection = replyRejection });
            }
            catch (Exception) { failure = LocalReasonerFailure.AuditFailed; throw new LocalReasonerException(failure.Value); }
        }
    }

    private static async Task Send(OwnedReasonerWorker worker, byte[] request, CancellationToken token)
    {
        await LocalReasonerProtocol.WriteAsync(worker.Input, request, token).ConfigureAwait(false);
        worker.Input.Close();
    }
    private static async Task RejectError(Stream error, CancellationToken token)
    {
        // Raw worker stderr is never logged or buffered. A single byte fails this proof protocol.
        if (await error.ReadAsync(new byte[1], token).ConfigureAwait(false) != 0)
            throw new LocalReasonerException(LocalReasonerFailure.InvalidTransport);
    }
    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    public async ValueTask DisposeAsync()
    {
        Disable(); Task? pending;
        lock (_gate) { pending = _active?.Task; }
        if (pending is not null) await pending.ConfigureAwait(false);
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true; _disabled.Dispose();
        }
    }
}
