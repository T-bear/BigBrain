# Finance emergency stop runbook

Status: design-only; Finance runtime and STOP ALL TRADING are not implemented.

## Intended trigger and behavior

Trigger on owner action, daily/rolling loss limit, reconciliation mismatch, stale or
abnormal data, broker uncertainty, strategy/system failure or suspected compromise.
STOP ALL TRADING must atomically enter HALTED, prevent new exposure and strategy orders,
cancel only policy-eligible pending orders, preserve safe exits when policy permits and
display the state prominently.

## Future operator sequence

1. Confirm HALTED through an independent read path; do not submit test orders.
2. Capture sanitized journal/policy/mode references and broker health.
3. Reconcile broker orders, fills, positions and cash before any retry or exit decision.
4. Revoke credentials on suspected compromise and follow broker incident procedures.
5. Classify safe pending-order cancellation and safe exits under current policy.
6. Resume only after root cause, reconciliation, policy recovery conditions and explicit
   product-owner approval are documented.

If execution is uncertain, never retry blindly. This draft becomes operational only
after implementation-specific endpoints, authorization, notification and rollback are
tested without exposing credentials or account details.
