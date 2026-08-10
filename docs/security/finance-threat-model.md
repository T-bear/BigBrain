# Finance threat model

Status: planning baseline; review again before credentials, sandbox or live connectivity.

M1 review: the implementation adds only domain types, in-memory evidence and a read-only
module registration. It adds no network client, broker SDK, credential configuration,
secret-bearing fixture, external order endpoint, persistence or logging sink. Candidate,
risk, policy and decision remain separate, with missing safety evidence failing closed.

## Assets and adversaries

High-value assets are broker credentials, authorization/mode policy, account and position
state, order intent, market data integrity, decision evidence and emergency controls.
Threats include stolen credentials, malicious/compromised data providers, prompt or
strategy manipulation, confused-deputy calls, replay/duplicate orders, policy bypass,
frontend tampering, log leakage, dependency compromise, operator error and uncertain
broker execution.

## Required controls

- Credentials are injected at runtime with least privilege; never Git, Web, Brain,
  prompts, URLs, telemetry, journal or logs. Paper/live secrets are separated, rotated
  and revocable with a tested procedure.
- Broker network access belongs only to the adapter boundary. UI and AI receive typed,
  minimized capabilities and cannot select arbitrary endpoints.
- Risk, authorization, mode and immutable-preview checks run server-side for every order.
- Policy changes and mode promotions require explicit owner authorization and audit.
- Idempotency, nonce/expiry and reconciliation defend replay and ambiguous execution.
- Market observations carry source, version and freshness; stale/abnormal data fails safe.
- Supply-chain dependencies are pinned/scanned and financial SDKs are added only after
  broker selection and review.
- Append-oriented evidence is access-controlled, retained and tamper-evident enough for
  reconstruction; secrets and unnecessary personal/account data are excluded.
- STOP ALL TRADING and automated circuit breakers remain available when strategies fail.

## Residual risk and gates

Trading can lose capital even when controls work. Backtests may be biased and future
markets may differ. Broker/data outages, legal change and credential compromise remain
residual risks. No live mode is acceptable until threat-model review, incident response,
rotation/revocation, reconciliation, emergency drills and owner risk acceptance pass.

BB-046 review: future market-data credentials remain runtime-only adapter secrets. Terms
and entitlements are security policy: ingestion must fail closed when product, market,
retention or rate-limit scope is unknown. Provider corrections and stale/partial data are
untrusted inputs; immutable provenance prevents silent historical rewriting.

BB-071 review: licensing and retention are enforced as data-governance policy, not
inferred from API access. Public cancellation clauses for shortlisted providers require
deletion, so no adapter may be enabled until an approved product or written exception
defines storage duration, non-display backtesting, derived evidence and deletion duties.
Dataset deletion must never silently destroy referenced evidence; if the license cannot
support reproducible retention, the provider is ineligible for BB-045.

Market-data memory adds risks of license laundering through canonical/derived copies,
backup retention beyond entitlement, poisoned corrections, incomplete lineage and a
model-training use not granted by the source policy. Controls are a versioned entitlement
envelope, explicit allowed-use enum, fail-closed evaluation, immutable revisions/checksums,
quarantine/quality states and deletion across primary/normalized/backup copies. “Derived”
never implies unrestricted, and model outputs cannot self-promote into live policy.

Implementation review 2026-08-10: the first BB-045 slice adds only immutable domain state
and a pure evaluator. Evidence is an opaque validated reference, not content or a secret.
There is no logger, raw payload, network client, provider credential, persistence or
execution path. Exact provider/product matching prevents a policy from authorizing data
from another product accidentally.
