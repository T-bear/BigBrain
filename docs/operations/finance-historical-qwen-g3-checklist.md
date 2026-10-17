# FIN-OPS-G3-20261016 — Historical Qwen release gates

> **FINANCE: FROZEN — OWNER DECISION REQUIRED TO RESUME.**
> Administrativ status: FROZEN. Tekniskt kvarstår G1 PASS / G2 PASS / G3a BLOCKED / G3b NOT RUN. Ingen fortsatt G3a-granskning eller körning är auktoriserad av checklistan.
> [Kanoniskt frysdokument och återupptagningsprotokoll](finance-freeze-20261016.md).

This is the owner-approved gate definition for this release only. It does not change AGENTS.md,
ADR authority, the [research contract](../architecture/finance/research-learning-contract.md),
or grant additional inference/deployment authority. Current evidence belongs in the
[dated operation report](../reports/features/finance/historical-qwen-operation-20261016.md#g3a-release-completion--2026-10-16).
The [hosting runbook](runbooks/finance-historical-hosting.md#g3a-model-free-systemd-preflight) carries operator steps.

## Gates and current disposition

| Gate | Required evidence | Current disposition |
| --- | --- | --- |
| G1 — Merge | Owner approval of exact tree; applicable CI; no unresolved P0/P1 in accepted implementation scope | PASS: candidate87b08d3 merged unchanged as70b659f; tree6433c956; CI38863227447 SUCCESS. Hosting readiness was not implied. |
| G2 — Deploy | Authorized deployment; compiled/OCI/image identity; API health; no unintended AAPL/other-service effect | PASS for API deployment; report records exact identities/config comparison. The API is **not** a Qwen-ready environment. |
| G3a — Activation readiness | Actual host isolation, protected artifacts, private profile, finite grant/enrollment, independent append-only journal, `RecurringAuthorityMatched`; no open P0/P1 | **BLOCKED**: operator reports one diagnostic capture with16 rejected mounts. Private propagation correction prepared in Git and statically tested only; inherited targets unresolved. No successful host preflight or downstream authority proof. |
| G3b — Historical run | Exactly one separately approved invocation and persisted outcome, including unsuccessful outcomes; readback verification | NOT RUN. This G3a task forbids `finance-historical-run`. |

G3a is not passed by a static unit check, mock filesystem, successful Docker loader test, an old
preflight result, directory presence or owner approval alone. Actual final security context must be
proven. A new context, mount, binary or effective unit change invalidates affected prior evidence.
G3a success does not itself authorize G3b. A technically unsuccessful run after correct authorization
is a run outcome, not automatically a release defect; preserve its spent/uncertain authority.

## Invariants

1. Finance remains **RESEARCH / 0 SEK / NONE**, without trading or capital authority.
2. Protected holdout, outcomes and complete source identity never reach the model.
3. I1/E1/T3/C64/W33s is finite: no automatic retry, refund, renewal or replacement program.
4. Protected authority must match before inference and evaluation.
5. Actual model SHA-256 must match the approved profile and grant.
6. No lookahead; knowledge time and source provenance retain accepted semantics.
7. No secrets, private grants, sensitive paths or private data are published.
8. Source rights, inherited retention and deletion obligations remain binding.
9. No native model execution without proven process, resource and access isolation.

The model is pinned Qwen3-1.7B-Q4_K_M; approved SHA-256:
`b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897`.
Ordinary inference deadline is30s, grace250ms and reap2s, within33s completion authority.
The [earlier BB-132A observation](../reports/features/finance/bb-132a-local-model-preflight-20260929.md)
was **50.458s**, exceeding this30s deadline by20.458s under a different historical180s authorization.
It neither predicts success here nor authorizes a deadline increase. Timeout must be recorded without retry.

## Severity and review rules

| Severity | Meaning | Release effect |
| --- | --- | --- |
| P0 | Verified invariant violation or serious data-loss/economic-damage risk | STOP |
| P1 | Insufficient evidence for an invariant on the selected execution path | STOP that gate/path |
| P2 | Bounded defect with no invariant impact | Document relevant backlog item |
| P3 | Improvement, refactor or future idea | Defer; document only if relevant |

Evidence may be file/line code review, a test, static analysis or actual operation; state its scope.
Only P0/P1 block this release. At most two correction rounds within one independent review. An
unresolved P0/P1 cannot be approved away: owner may stop, postpone or reduce scope. This does not
override any required grant, rights or exact-SHA approval. Missing host evidence is not converted
to PASS by repeated analysis or repeated application test suites.

## G3a completion record

Retain private, invocation-bound results and a sanitized report covering:

- Actual systemd ControlGroup, controller/worker placement, effective UID/GID1654, zero capabilities,
  NoNewPrivs1, exact worker resource values, owned-child migration/kill/reap/empty result and writable mounts.
- Root-protected regular artifact copies, full final-environment loader closure including libgomp,
  pinned model hash and source/build manifest; persistent coordination1654/0700.
- Explicit exclusion of authority from **all** applicable restore sets; protected parent directories.
- Read-only historical inspect: revision/wiki/source/artifact commitments; exactly748 retained sessions
  and fixed first500 (2014-01-02–2015-12-24), without exposing protected data to the model.
- Actual approved profile identity, current UTC validity, exact source-bound finite grant; no invented dates.
- Enrollment once through the existing owner (may migrate/write), exact enrollment bytes plus newline,
  journal checksum/fsync/append-only protection, then `RecurringAuthorityMatched` in final context.
- AAPL/API identity/configuration unchanged; no research scheduler, prospective research or inference start.

Keep the historical and prospective operator paths distinct. Authority-check uses six disabled gates
**only in its own process**; never disable production AAPL to make maintenance validation pass.
Current blockers, checks and next administrator step are recorded once in the operation report.
The latest [partial propagation correction](../reports/features/finance/historical-qwen-operation-20261016.md#g3a--private-propagation-preparation-and-unresolved-target-gate)
supersedes the earlier preparation status without treating static checks or a diagnostic as gate acceptance.
