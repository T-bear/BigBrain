# BB-131A — Finance Research Learning architecture and contract

Detta är en sanerad GitHub-version. No credentials, private addresses, raw model/provider
payloads, raw market rows or production database contents are included.

## Metadata

- Date: 2026-09-21
- Baseline: `7747b8f715204dc3ccf753f170238b40238d1b11`
- Branch: `bb-131a/research-learning-architecture`
- Scope: recovered read-only source analysis plus documentation; no implementation
- Contract: [Research Learning](../../../architecture/finance/research-learning-contract.md)
- Decision: [ADR 0038 — Accepted](../../../adr/0038-finance-research-learning-authority-contract.md)
- Finance: **RESEARCH / 0 SEK / NONE**

## Status

**ACCEPTED / MERGED / CI VERIFIED — architecture only.** Explicit owner/architect approval
applies to the exact candidate below. No implementation, deployment, runtime/security or owner UX
verification is claimed. BB-131B is NOT STARTED / NOT AUTHORIZED.
BB-130 remains COMPLETE / EXIT APPROVED on accepted main. This candidate neither reopens
stabilization nor accepts previously proposed trading ADRs.

## Accepted publication

- Original baseline: `7747b8f715204dc3ccf753f170238b40238d1b11`.
- Approved candidate: `1eb50979a7923f115b4b182fbee15de735ea1f7e`; one commit ahead, zero behind.
- Merge: `5c5557297aa121acbcde2b7e6adcd6b32eae2e8d`; parents are baseline and approved candidate.
- Candidate and merge trees identical: `bef025f159e8fe478f1dc191cd6e122e362b728d`.
- [Merge CI 35641586823](https://github.com/T-bear/BigBrain/actions/runs/35641586823): SUCCESS
  for backend, frontend, documentation and secrets on that exact merge SHA. Actual backend
  restore, dotnet format verification, Release build/tests and frontend npm ci, format:check,
  tests/build all executed successfully. Documentation verifier and Gitleaks also passed.
- Reconciliation checks: documentation/link verifier PASS (248 Markdown files / 91 backlog IDs),
  working/staged diff checks PASS, Gitleaks 8.28.0 full-history (284 commits) and staged scans
  PASS with no leaks. Source/test/package/CI/runtime diff is empty.
- ADR 0038 is Accepted as architecture only. Publication reconciliation changes status/evidence
  in the same 12 documents; it does not alter the accepted design or authorize BB-131B.
- No deployment, provider/SDK/model integration, schema/source/test/package/CI/runtime change,
  Finance experiment or scientific/trading authority change. Finance RESEARCH / 0 SEK / NONE.
- Final reconciliation SHA is resolved by the main commit titled
  `docs: reconcile accepted BB-131A architecture`. Publication completion requires exact-final-SHA
  CI success, including both actual formatter steps; merge CI is not substituted for it.

The candidate analysis and recovery observations below retain their original scope/date.

## Recovery reconstruction

| Field | Recovered fact |
| --- | --- |
| Original task | BB-131A analysis/documentation interrupted by usage-credit exhaustion |
| Baseline/current main | Exact expected `7747b8f715204dc3ccf753f170238b40238d1b11` after fetch |
| Local branch/HEAD | Intended branch already existed, HEAD exactly baseline; reused unchanged |
| Staged/unstaged work | Both tracked diffs empty at recovery |
| Remote candidate/history | `git ls-remote --heads origin bb-131a/research-learning-architecture` returned no branch; no local candidate commit |
| Existing artifacts/recovery note | No BB-131A report/contract/ADR and no filled BB-131A interruption note existed |
| Unrelated local work | Untracked design mockups and unpublished ADR 0006–0009 preserved/excluded |
| Completed and valid before resume | Branch creation; earlier source-reading context reused, then completed/checked against unchanged source |
| Prior verification | No durable BB-131A hygiene/test results; none assumed. Existing BB-130 evidence remains historical source |
| Remaining at recovery | Complete source/capability analysis, contracts/governance/B proposal, docs, hygiene, publication |
| Blocker/contradiction | No Git/recovery contradiction or newly reproduced current product defect established |
| First incomplete action | Complete source-backed authority/governance design, without recreating branch or running Finance |

No reset, clean, stash, rebase, overwrite, duplicate commit or force push was used.
The single canonical [recovery document](../../../operations/codex-recovery.md) records current
handoff; this table is durable completed-checkpoint evidence, not a second active recovery note.

## Evidence

### Source and accepted evidence inspected

Read AGENTS, START-HERE, ARCHITECTURE, ROADMAP, STATUS/BACKLOG/TESTING, Finance module/master
roadmap and relevant ADRs. Accepted ADR 0021/0022/0023/0024/0025/0030/0033–0036 constrain rights,
causality, deterministic results, holdout, hard risk and existing autonomous operations. Proposed
ADR 0017–0020 is future direction, not current trading authority. Source owns actual behavior.

| Accepted evidence | What BB-131A preserves |
| --- | --- |
| [BB-123](finance-bb-123-transaction-cost-slippage-fill-realism-20260831.md) | Versioned daily next-session fill/cost assumptions, immutable replay; no intraday liquidity proof |
| [BB-124](finance-bb-124-anti-overfitting-oos-governance-20260901.md) | Frozen selection population, embargoed 60/20/20, single-use holdout, conservative family-breadth rule; DSR/PBO not evaluable |
| [BB-127](finance-bb-127-owner-research-dataset-xlsx-20260901.md) | Separate purpose-specific noncanonical eligibility, owner-limited rights and immutable dataset lineage; no silent canonical promotion |
| [BB-128B](finance-bb-128b-read-only-resilience-20260902.md) / [BB-128C](finance-bb-128c-design-system-conformance-20260903.md) | Last-known-good display cache, one-request/one-loader/degraded behavior; stale UI data has no authority |
| [BB-129A](finance-bb-129a-multi-dataset-campaign-20260903.md) | 24 categorical attempts, zero robust candidates, missing accepted feature/selection lineage; no actual OOS performance claim |
| [BB-130 exit](../platform/bb-130-final-exit-assessment-20260921.md) | Closed stabilization, both formatter gates, retained reader/schema/security debt and no new runtime approval |

The contract's capability map names exact module/API/Web files/types. Important observations:

- `RunAutonomousResearch` generates fixed internal hypotheses from current robustness evidence;
  it is not a generic pinned proposal executor. `ResearchHypothesis` already exists and is reused.
- `RunResearchCampaign` currently records ineligible/insufficient categorical outcomes without
  running the backtest/robustness chain. Its parameter labels differ from strategy constructors.
- `DeterministicRobustnessEvaluator.Evaluate` performs validation selection and holdout inside
  one call. Future commitment/exposure checks must precede that call. Current exact-lineage
  prior-use detection does not prove safe adaptive reuse across overlapping cohorts/revisions.
- BB-092 records family attempts; its multiple-testing integrity check and BB-124 breadth rule
  are not a formal guarantee against repeatedly proposing until a favorable result appears.
- `FinanceBacktestPersistence` supplies immutable, atomic result storage. Existing readers are
  not a generic trust validator for arbitrary new learning envelopes or untrusted references.
- `FinanceRiskEngine` evaluates current prospective RESEARCH evidence; it cannot infer current
  provider/cadence health or applicable risk approval from a historical backtest.
- Existing public read models can reveal holdout details. Future reasoner input requires a
  Finance-owned projection, not full-response serialization, Web cache or a database dump.

These are scoped capability/extension limits. No new current reproducible security/scientific/
lineage defect requiring BLOCKER HANDOFF was established; no exploit or production-data probe
was attempted. Do not treat this finding as blanket security approval.

Existing test source reviewed includes `FinanceDeterministicBacktestTests`,
`FinanceRobustnessEvaluationTests`, `FinanceAutonomousResearchTests`,
`FinanceResearchDatasetTests`, `FinanceResearchCampaignTests`, `FinanceRiskEngineTests` and
`FinanceBacktestPersistenceTests`. Specific existing evidence covers validation-only selection,
contaminated holdout, negative/noise controls, family attempts, stale-generation rejection,
idempotency, retained partial experiments, single-flight/restart, campaign replay and exact
stored-result/checksum replay/rollback. These are existing tests, not new BB-131A test results.

### Proposed architecture and scientific safeguards

Finance supplies bounded evidence -> fake/future reasoner proposes -> independent fail-closed
admission freezes exact identities/policies/budget -> existing engines execute -> source-typed
immutable results and lineage. Brain remains orchestration only. No execution capability exists
in the proposed learning interface.

Reuse existing hypothesis/strategy/configuration/result types. Add only a versioned input/draft/
committed-envelope/result-reference contract, embedded question and typed falsification criteria.
The reasoner cannot author server policy, risk verdict, dataset eligibility or execution commands.
Exact references bind purpose, dataset/feature/strategy/cost/fill/split/robustness/risk identity.
Unknown references/capabilities, ambiguous JSON, unsupported parameters or insufficient authority
fail before execution. Independent execution fingerprint prevents prose/model/key changes from
creating a fresh experiment.

Start with one reasoner invocation, one family/one caller variant/one synthetic instrument and
one frozen robustness plan; account for its three momentum grid trials and all internal work,
with at most 64 underlying runs. These are proposed engineering bounds, not significance proof.
No autonomous retries or replenished budgets. Preserve failed/rejected/duplicate/skipped history.
Future production needs atomic commitments/reservations and overlap-aware family/holdout exposure
accounting; consumed evidence cannot become fresh through aliases or a new revision/prompt.

Protect active holdout rows, derived summaries and outcome-revealing classifications. Closed
cohort results can inform a later exploratory question only with consumed lineage and independent
fresh confirmation prerequisites. Freeze the full engine population before the combined evaluator.
No DSR/PBO/statistical guarantee is invented. External educational material remains tagged idea
provenance, never scientific proof; no Investopedia ingestion.

Persist exact authorized input and normalized proposal, full identity/checksums, policy versions,
parent/family/cohort history, all outcomes and semantically distinct timestamps under applicable
retention rights. Replay from committed inputs does not call the reasoner. Model output itself
need not be deterministic. Missing/expired evidence remains unavailable rather than fabricated.

Unavailable/timeout/no-useful/malformed output never invokes Finance. Interrupted committed work
must retain reservations and reconcile immutable results, not start a fresh search. Risk vetoes
cannot be overridden; no current risk approval is fabricated for historical learning.

The design is informed by primary [adaptive-data-analysis research](https://arxiv.org/abs/1411.2664)
and [OWASP's prompt-injection guidance](https://cheatsheetseries.owasp.org/cheatsheets/LLM_Prompt_Injection_Prevention_Cheat_Sheet.html),
consulted 2026-09-21. These motivate conservative limits and independent controls; they do not
certify BigBrain's implementation or justify reusable-holdout/statistical claims.

### Proposed BB-131B, not implementation

Smallest recommended slice: pure typed validation plus test-only fake reasoner/orchestration,
one synthetic momentum reference experiment through the unchanged evaluator, underlying runs
persisted/reopened via the existing SQLite writer/readers, and a bounded **test-only** learning/
robustness manifest for exact replay assertions. Verify rejection prevents engine calls, input
redaction, duplicate/normalization/budget/holdout/risk boundaries and deterministic result identity.

This proves an engineering contract without a provider, runtime endpoint/worker or production
schema. It does not prove production crash-safe adaptive accounting, real-world scientific
validity, rights to model export or authentication. Those need separate later checkpoints.
If existing machinery cannot be bound without changing scientific behavior, B must stop for review.

### Verification for this documentation candidate

- Recovery: `git fetch origin`, branch/HEAD/status, staged/unstaged diffs, history and exact remote
  branch lookup established the table above. No completed implementation work was discarded.
- Accepted-main CI [35608150279](https://github.com/T-bear/BigBrain/actions/runs/35608150279)
  re-read from GitHub: head `7747b8f715204dc3ccf753f170238b40238d1b11`, completed/success.
  This is baseline evidence, not CI acceptance of this candidate.
- `node scripts/verify-documentation.mjs`: PASS, 248 Markdown files / 91 unique backlog IDs,
  including local link resolution. Initial sandbox attempt was blocked by `spawnSync git EPERM`;
  the same verifier passed with the required execution permission, without a source change.
- `git diff --check`: PASS. Full tracked diff reviewed; new contract/ADR/report reviewed in full.
- `gitleaks git --log-opts=--all --redact --no-banner` (8.28.0): PASS, 283 commits, no leaks.
- `git diff --cached --check`: PASS. `gitleaks git --pre-commit --staged --redact
  --no-banner`: PASS, no leaks. Exact staged inventory: 12 Markdown documents only;
  unrelated untracked work excluded. Final fetch must retain the exact baseline before push.
- `git diff --exit-code 7747b8f715204dc3ccf753f170238b40238d1b11 -- src tests .github
  Directory.Build.props global.json BigBrain.slnx compose.yaml deploy scripts`: exit 0;
  implementation/test/package/CI/runtime boundaries unchanged.
- No application builds/full tests/audit rerun: source, test, dependency, workflow and runtime
  files are unchanged. This is permitted by the docs-only task; proposed B tests are not passed tests.
- No runtime/database inspection, research execution, provider call, deployment or owner UX test.

## Changes

Documentation only: new architecture contract, Proposed ADR 0038 and this report; relevant current
planning/status in ROADMAP, STATUS, BACKLOG, Finance module/master roadmap, TESTING, recovery,
ADR index and report catalog. No historical report or accepted ADR rewritten. BB-130 remains closed.
ARCHITECTURE/README/START-HERE, stabilization plan, other modules/knowledge/indexes/runbooks and
security/rollback documentation assessed: existing authority remains correct; no unrelated edits.

Rollback would be a separately authorized documentation revert; there is no runtime rollback.

## Security

No real reasoner, provider/SDK/key/network integration, tool authority, schema or endpoint was added.
The general application-auth boundary remains incomplete; RESEARCH-only does not make exposure safe.
Only docs A and a future isolated synthetic B can proceed without the broader auth/security sprint.
Production reasoning requires authenticated/scoped access, independent validation/audit, rights to
export/retain each input/output and reviewed egress/resource limits. Protected holdout must remain
outside model/tool context. Broker/orders/PAPER/LIVE/AUTO/capital remain prohibited.
Finance stays **RESEARCH / 0 SEK / NONE**; existing scientific results and fail-closed behavior are
unchanged. No deployment, provider activation or Research Learning production implementation.

## Remaining work

Independent owner/architect review of the exact candidate and ADR 0038 is complete.
BB-131B implementation and all production prerequisites still need separate authorization. Open future questions include production journal
schema/recovery/retention, cross-cohort overlap decisions, real-data feature lineage, model-export
rights, stronger auth and statistically justified confirmation for adaptive research. None is
silently implemented or waived. Architecture acceptance does not authorize BB-131B.

## Resumption

Use current GitHub main, this branch's exact remote commit, canonical recovery and the contract.
No interrupted BB-130 work remains. Resolve candidate identity from
`origin/bb-131a/research-learning-architecture`; its parent must be the baseline above.
STOP — return published main to owner/architect for independent verification.
Do not deploy, start BB-131B, select a provider or perform another checkpoint.
