# ADR 0022: Owner-accepted zero-cost personal market research

- Status: Accepted
- Date: 2026-08-11
- Accepted by: Product owner through BB-076

## Context

ADR 0021 established the provider-neutral retention gate and treated every missing lifecycle
grant as blocking. BB-075 consequently found no zero-cost source with explicit wording for
every internal artifact. That evidence remains valid, but the product owner has now accepted
a narrowly bounded residual risk for legitimate, free, read-only personal research sources.

## Decision

Finance entitlement evidence is capability-scoped and records one of:

- `ExplicitProviderGrant`;
- `OwnerAcceptedPersonalResearch`;
- `HumanConfirmationRequired`;
- `Denied`.

`OwnerAcceptedPersonalResearch` is valid only for a 0-SEK, legitimately offered source used
privately by the owner for non-commercial read-only research, when available evidence is
compatible and no identified term prohibits the exact automation, retention, research or
other capability being enabled. The policy records provider/product, allowed and prohibited
capabilities, evidence date, rationale and owner-acceptance version.

Silence is not a universal grant. Owner acceptance cannot override an explicit prohibition,
paid-license or prior-permission requirement, technical access control, redistribution
restriction, or an unidentified source. Decisions remain fail-closed per capability.

Trading, broker access, orders, PAPER/LIVE execution, customer funds, commercial operation,
paid subscribers and redistribution remain outside this policy and retain their strict gates.
Finance remains RESEARCH.

## Consequences

ADR 0021 is not rewritten and remains the provider-neutral architecture. BB-075 remains the
historical result under its stricter evidence rule. BB-076 may authorize a bounded capability
despite residual silence, but only when both its evidence and technical access gates pass.

The initial Stooq daily-download candidate met the residual-risk evidence class for bounded
personal historical research, but its public CSV request returned a JavaScript verification
challenge instead of data. BigBrain will not automate around that control, so no Stooq
adapter or data activation follows from this ADR.

## Rejected alternatives

- Treat any publicly visible data as permitted: rejected.
- Make authorization provider-wide: rejected; every capability is evaluated separately.
- Let owner acceptance override negative terms or access controls: rejected.
- Extend this exception to trading or commercial use: rejected.
