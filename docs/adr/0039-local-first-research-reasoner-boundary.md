# ADR 0039: Local-first Research Learning reasoner boundary

- Status: Proposed
- Date: 2026-09-23
- Checkpoint: BB-131D — architecture/security design only
- Baseline: `269fbd7e786a09ba2bf39fe77492d62d8056a52a`

## Context

Accepted ADR 0038 assigns scientific authority to Finance. B/C implement only a bounded synthetic
contract and persistent governance. The owner now requires a viable local-AI path without a paid
API dependency. Current application auth, model isolation and operational audit are not complete.
A local model is still untrusted; private local research permission does not imply cloud export.

## Proposed decision

Use a provider-neutral one-request/one-response reasoner port logically owned by Brain. Its primary
adapter targets an isolated on-host runtime, with no model tools, arbitrary files, shell, database,
network, provider or execution capability. Finance alone projects authorized development evidence,
admits proposals and owns budgets, exposure, deterministic engines/results and risk interaction.

Local inference needs affirmative local-use rights. Optional future external inference separately
needs explicit export rights, provider/owner authorization, security/retention review and cost limits.
External defaults disabled. Local failure never triggers cloud fallback. Provider/model identity is
audit metadata and cannot renew scientific opportunity. Weak-model quality affects usefulness only.

[The proposed detailed contract](../architecture/finance/local-first-reasoner-boundary.md) defines
source evidence, locality axes, auth, parsing, injection containment, limits, audit, failure/disable
behavior, abuse tests and distinct first-local/first-external invocation gates. Numeric runtime
capacity and model choice await evidence; no compatibility/performance claim is made.

## Alternatives

- Require paid cloud for correctness: rejected; Finance proves correctness independently.
- Trust local output or share Finance SQLite: rejected; locality does not confer authority.
- Load model tooling into API with general tools: rejected; unnecessary privilege and resource risk.
- Generic autonomous agent/tool loop: deferred; one bounded proposal or NoUsefulProposal is sufficient.

## Consequences

ADR 0038 remains Accepted and unchanged. This proposed decision adds no project, code, schema,
runtime, provider, credentials, model installation/inference or deployment. Runtime provisioning
must respect existing Sentinel/host boundaries. Broader auth/adaptive-research/rights gaps remain.
Finance RESEARCH / 0 SEK / NONE; no PAPER/LIVE/AUTO/broker/orders/capital. Acceptance of this ADR
would accept design only, not authorize implementation or first model invocation.
