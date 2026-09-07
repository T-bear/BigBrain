# BB-130C — backtest result identity review candidate

## Metadata

- Date: 2026-09-07.
- Baseline main: `904694f3992400d4d58e587afd4832e0bf7f857a`.
- Review branch: `bb-130c/backtest-result-identity`.
- Scope: frontend backtest detail identity/loading only. No backend, calculation,
  persistence, schema, provider or deployment work.
- Detta är en sanerad GitHub-version. No credentials, private identities, raw market
  payloads or sensitive runtime logs are included.

## Status

REVIEW CANDIDATE PUSHED — NOT MERGED TO MAIN. The current pushed tip of `bb-130c/backtest-result-identity` is awaiting owner/architect review and merge approval.

## Evidence

The prior published characterization reproduced summary(B) beside curve(A). The
focused regression failed against that behavior before the correction, for the intended
reason. The corrected tests cover A→B pending/success/failure, rapid A→B→C stale
completion, close/reopen, refresh independence and unmount cancellation.

## Changes

The previous UI could show summary(B) beside curve(A) while B was pending. The invariant
is now that selected catalog identity, result-specific status and visualization refer
to the same `runId`. `useFinanceBacktestDetails` owns catalog acquisition, selection,
result loading, identity validation, loading/error/retry state and abort handling.
Robustness remains outside the hook.

Selecting B immediately invalidates A, clears its curve and shows a compact busy status.
A result renders only when its returned `runId` matches B. Failure shows B's summary,
an explicit error and retry, without any curve. Rapid A→B→C completion from an aborted
older request cannot replace C. Close/reopen retains the existing selected-result
semantics; observation refresh does not restart detail reads.

## Verification

- Focused FinanceObservation tests: 35/35.
- Full Web suite: 191/191 in 26 files.
- Production Web build: passed.
- Documentation verification, diff checks and staged secrets scan: passed after final staging.

## Security

Finance remains **RESEARCH / 0 SEK / NONE**. No scientific, provider, entitlement,
broker/order, PAPER/LIVE/AUTO, persistence or schema semantics changed.

## Remaining work

Remaining review questions:

Review the calmness of the loading/error presentation on mobile and whether analogous
robustness selected-summary/result coupling merits a later separate checkpoint. No such
robustness change is included here.

## Resumption

Review this branch and the canonical recovery note. After branch push, stop; do not merge
main or start another checkpoint. Merge requires explicit approval of the exact branch SHA.
