# BB-131B — Synthetic Research Learning contract and replay proof

## Metadata

- Date: 2026-09-22.
- Baseline: `b0459b1e09be777f2ce11dd33a570af8a06c447a`.
- Branch: `bb-131b/synthetic-contract-replay-proof`.
- Status: **ACCEPTED / MERGED / CI VERIFIED**.
- Authority: separate explicit owner authorization for B; accepted [ADR 0038](../../../adr/0038-finance-research-learning-authority-contract.md)
  and [architecture contract](../../../architecture/finance/research-learning-contract.md) unchanged in decision.
- Finance: **RESEARCH / 0 SEK / NONE**. **AI MAY PROPOSE. DATA MUST PROVE. RISK MAY VETO.**

## Status

**ACCEPTED / MERGED / CI VERIFIED**.

Detta är en sanerad GitHub-version. Sanitized engineering evidence only: no real market data, credentials, private addresses, model
payloads, production database contents or raw sensitive logs. No deployment/runtime/owner UX
verification or market-performance claim. BB-130 remains closed; B is accepted on main as the bounded synthetic engineering proof only.

## Accepted publication — 2026-09-22

Owner/architect explicitly approved candidate `56ad7fd0290471bec926b2008384951a9bb8286a` from baseline
`b0459b1e09be777f2ce11dd33a570af8a06c447a`. Exact pre-merge checks found one commit ahead,
zero behind, matching parent/merge-base and the unchanged 14-file reviewed candidate.
Merge `4810af5f7050361b3071befdccb4caafa84902b4` has baseline as first parent and candidate as second parent.
Candidate and merge share tree `e7988a8f2b88a64fcb403142d9da1c0214e604d5`; content is identical.
[Exact merge CI 35730956813](https://github.com/T-bear/BigBrain/actions/runs/35730956813): **SUCCESS**.
Backend, frontend, documentation and secrets jobs all completed successfully. Actual steps passed:
backend checkout/setup -> restore -> dotnet format verify -> Release build -> tests;
frontend checkout/setup -> npm ci -> npm run format:check -> tests -> production build;
documentation verifier and Gitleaks. Historical local test results are not substituted for this run.

Publication reconciliation changes documentation only. No accepted implementation, source/test,
package/CI/schema/runtime configuration or ADR decision is altered. ADR 0038 remains Accepted;
BB-131A remains accepted architecture and BB-130 closed. BB-131C is NOT STARTED / NOT AUTHORIZED.
No deployment, real provider/model/AI integration or new scientific/trading authority occurred.
Finance **RESEARCH / 0 SEK / NONE**. Production autonomous learning, real-market learning,
adaptive-search journal/cross-cohort accounting, auth/security, export/data rights and statistical
profitability are not established; no PAPER/LIVE eligibility or execution authority follows.

Resolve the final reconciliation SHA from main using
`git log -1 --format=%H --grep='^docs: reconcile accepted BB-131B contract proof$' main`.
The final commit's GitHub Actions run must independently pass all four jobs and both actual
formatter checks; merge CI does not substitute for final reconciliation CI. Exact final SHA/run
are recorded in GitHub commit/Actions history and the publication handoff, avoiding a self-referential
commit hash inside its own content. No subsequent checkpoint is authorized.

Publication hygiene: documentation verifier PASS (249 Markdown files, 91 unique backlog IDs);
working-tree and staged diff checks PASS; Gitleaks history scan PASS (286 commits, no leaks);
staged secret scan PASS (no leaks). Only the ten publication documentation files are staged.

The implementation/recovery evidence below retains the original candidate scope.

## Recovery

The existing branch was reused at the exact baseline. Prior durable B work was only branch
creation/source inspection, with no implementation/tests/commit/remote candidate. The owner and
architect explicitly reconciled that finding before implementation continued. The local recovery
note was superseded; no missing work is presumed. Unrelated mockups and unpublished ADR 0006–0009
are preserved and excluded. No reset, clean, stash, branch recreation, rebase or force push.

A later publication resumption recovered the complete implementation and final passing test
results unchanged. Fourteen candidate files were staged; only the final report hygiene paragraph
remained unstaged. No B commit/remote existed yet. Final documentation checks and publication
continued from that point without repeating implementation or unchanged expensive tests.

## Changes and authority boundary

Only four new C# files implement this checkpoint:

| File | Responsibility |
| --- | --- |
| `src/BigBrain.Modules/Finance/ResearchLearningContracts.cs` | Typed development input, limits/history, draft/variant/falsification, admission state/reasons, committed proposal and native result references; immutable synthetic scope; new versioned normalization/fingerprint. |
| `src/BigBrain.Modules/Finance/ResearchLearningAdmission.cs` | Pure bounded closed-JSON admission; no engine, storage, network, provider or clock. |
| `tests/BigBrain.Api.Tests/ResearchLearningFixture.cs` | Test-only synthetic data, fake reasoner, single-invocation protocol/history/exposure, serialized manifests and isolated existing SQLite setup. |
| `tests/BigBrain.Api.Tests/ResearchLearningContractTests.cs` | Positive/negative contract, identity, holdout/budget/risk and persistence/reload/replay proofs. |

No existing C# source or test file changes. No API, DI registration, worker, scheduler, Brain
project, Web behavior, package, configuration, CI or schema changes. No production reasoner or
provider interface is introduced. Free-form question/rationale are bounded metadata only.

The minimal untrusted union is `Proposal` or `NoUsefulProposal`; only pure admission can construct
a `CommittedLearningProposal`. It constructs existing `ResearchHypothesis` vocabulary after
validation. It does not deserialize an external verdict/configuration into trusted execution.
The projection supplies initial development counts/checksum, cutoff, offered capability, exact
protocol limits and an explicitly empty initial history digest. It exposes no raw data or result
payload. This is the initial synthetic protocol, not a general historical-summary service.

Admission rejects unknown/extra/duplicate JSON keys, malformed/deep/oversized payloads, unknown
versions/discriminators, null/missing fields, unsupported strategy/parameter/binding/parent,
wrong digest, incompatible purpose/revisions/features/policies and exhausted/unknown exposure.
Wire output is bounded to 64 KiB/depth eight; question 1,024 and rationale 2,048 characters.
Numeric strings/expressions/nonfinite values are not coerced. The NoUsefulProposal shape cannot
carry variants or execution fields. Invalid admission invokes no deterministic engine.

## Frozen synthetic protocol and scientific accounting

One fixture instrument, `synthetic-market-v1` and `synthetic-feature-v1`, with 400 US-calendar
sessions and causal momentum 5/10/20 (close[t] minus close[t-period], null during warmup).
These identifiers label test fixtures only; their spelling is not proof of real-data provenance.
The trusted synthetic scope checks exact row membership/knowledge timestamps, complete feature
periods/warmup, purpose-specific existing eligibility and the unchanged default evaluator policy.
Unknown rights are refused for this restricted fixture; this does not revoke existing owner-
accepted limited private research elsewhere.

Only momentum/v1, period 20, one caller variant, one reasoner invocation, one evaluation and
concurrency one are offered. Automatic retries are zero. The three **effective scientific trials**
are engine-owned periods 5/10/20, explicitly disclosed, counted and checked against selection trials.
Benchmark/cost/walk-forward diagnostics are not presented as independent new hypotheses.

Preflight uses the existing partition function and a conservative bound on all internal engine
calls: fixed 30 plus four per complete default walk-forward window. This includes calls that
resolve to the same immutable run ID, so it can refuse plans whose unique-run total alone might
fit. It never shrinks a plan. A 900-session fixture exceeds 64 and rejects before execution;
the 400-session fixture fits without the engine's capped-window path. Actual unique persisted
run IDs are separately counted, alongside submissions, three admitted trials, evaluator calls
and reused results. Duplicate submissions link the original commitment, not independent success.

The fixture preserves rejected/no-useful/duplicate/failure history. Malformed output ends the
single invocation; changed rationale, model label, request ID or time cannot restart it. A
pending commitment without a result is not counted as a reused result. Commitment failure leaves
exposure consumed and no engine call. Deadline/iteration limits are declared (30/300 seconds);
unavailable/timeout behavior is simulated, not a production scheduler/timer implementation.

## Identity and holdout proof

New full SHA-256 identities use versioned ordinal property order, preserved ordered arrays, UTF-8
and invariant minimal decimal representation. Raw response checksum, normalized proposal identity
and scientific execution fingerprint are distinct. JSON order/decimal spelling/culture do not
change normalized identity; altered prose can change proposal lineage but not the experiment.
A pinned independent hash vector covers `{"a":0,"z":20}`.

The execution fingerprint binds the resolved plan, entire synthetic input/features, eligibility
facts/purpose, existing fill/calendar versions, fixed falsification, effective grid and research-
only risk boundary. Scientific changes change this fingerprint. The new boundary resolves
numerically equivalent plan representations to the existing engine's exact default snapshot and
normalizes synthetic decimal input scale before execution. Existing identity algorithms are
unchanged; old evidence is never recomputed or migrated.

Immutable snapshots prevent mutation of the original arrays/dictionaries after commitment.
A paired test changes protected holdout prices/features and proves the reasoner projection and
input digest remain identical, while the scientific execution fingerprint changes. No full
robustness result, holdout run/classification/risk proxy crosses the reasoner input type. Unknown
or consumed exposure never receives a false fresh-history projection. The entire population is
frozen before the existing combined selection/holdout call, with no intervening reasoner call.

The fixture ledger consumes exposure across renamed synthetic revisions/proposal wording and
restores consumption on manifest reload. This is **not** production cross-cohort overlap
adjudication, proof of unseen real data, or an atomic multi-process adaptive-search ledger.

## Existing engine, falsification and risk

The admitted scope uses `EvaluationPlan` and the unchanged
`DeterministicRobustnessEvaluator.Evaluate`, which constructs existing `BacktestRunConfiguration`
and calls `DeterministicBacktestEngine` with compiled `MomentumResearchStrategy`/benchmark.
No alternate selection, holdout, robustness, strategy, cost or fill calculation was added.

The typed fixture criterion is `validation.excessReturn <= 0` in fractional-return units;
zero/negative/positive/missing values are tested. Missing remains nullable/not evaluable. It is
an engineering criterion, not a calibrated profitability/significance policy. Native
`RobustnessVerdict` and `SelectionGovernanceOutcome` are retained exactly; no mapping from
MoreRobust to RobustCandidate, no universal AI score and no DSR/PBO claim.

The test-only risk gate exercises native DENY/HALT/INSUFFICIENT_DATA/missing evidence on the
same submission path and never manufactures current production ALLOW. Result references state
RESEARCH/NONE/engineering-only. Prospective risk evaluation still requires its existing current
evidence; this fixture is neither risk approval nor execution authority. Existing risk regressions
remain unchanged. Reasoner failure also leaves direct deterministic Finance independently usable.

## Persistence, reopen and replay

The persistence proof writes a bounded **test-only** commitment file with consumed reservation
before evaluator execution, then uses existing `FinanceBacktestPersistence.PersistBacktest`
for every underlying result in isolated temporary SQLite with the existing schema. A separate
bounded test-only result manifest carries committed plan/draft, native result references and
checksums. These files are not a new production datastore/journal.

A newly constructed existing reader reopens SQLite; complete serialized result content and
IDs/checksums match. Manifest identity, draft semantics, input/plan fingerprint and stored run
references are checked. A tampered draft (including one with a recomputed proposal hash) fails
validation. Reload preserves the spent protocol and consumed cohort; a duplicate links rather
than admitting a fresh run. A separate deterministic replay of the frozen plan makes **no new
reasoner call** and reproduces exact evaluation/result content and run IDs/checksums. Repeated
writer calls insert no extra scientific rows; immutable checksum conflicts reject.

Replay is engineering reproduction of consumed evidence, not a fresh confirmation or refunded
budget. Test file/SQLite writes are not one crash-atomic production transaction; production
journal/recovery/retention and authentication/export controls remain separately required.

## Evidence

Toolchain: .NET SDK **major 10, minor 0, patch 302**, repository's existing analyzer/warnings policies unchanged.
Final source-tree verification: restore PASS; Release build PASS (zero warnings/errors);
focused B **56/56**, relevant Finance regressions **74/74**, full API **720/720** and Sentinel
**32/32**, all without skipped/failed tests; full formatter verification exit 0.
Full-history Gitleaks 8.28.0 PASS (285 commits, no leaks). Documentation/link verifier PASS (249 Markdown files / 91 unique backlog IDs); working/staged
diff checks PASS; staged Gitleaks PASS, no leaks. Explicit staged inventory: four new C# files
and ten Markdown files only. No earlier partial run substitutes for the final source tree.

- `dotnet restore BigBrain.slnx`
- `dotnet build BigBrain.slnx --configuration Release --no-restore`
- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-build --filter FullyQualifiedName~ResearchLearningContractTests`
- Relevant regression filter: `FinanceDeterministicBacktestTests`, `FinanceRobustnessEvaluationTests`,
  `FinanceResearchDatasetTests`, `FinanceResearchCampaignTests`, `FinanceAutonomousResearchTests`,
  `FinanceBacktestPersistenceTests`, `FinanceRiskEngineTests` (OR-combined FullyQualifiedName filters).
- `dotnet test BigBrain.slnx --configuration Release --no-build`
- `dotnet format BigBrain.slnx --verify-no-changes --no-restore`
- `node scripts/verify-documentation.mjs`
- `git diff --check`; `git diff --cached --check`
- Gitleaks 8.28.0 full history and explicit staged-candidate scan with redaction.

The full-test TRX logger used `LogFilePrefix=bb131b-final` to retain separate API/Sentinel results.
The exact regression command was:

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-build --filter 'FullyQualifiedName~FinanceDeterministicBacktestTests|FullyQualifiedName~FinanceRobustnessEvaluationTests|FullyQualifiedName~FinanceResearchDatasetTests|FullyQualifiedName~FinanceResearchCampaignTests|FullyQualifiedName~FinanceAutonomousResearchTests|FullyQualifiedName~FinanceBacktestPersistenceTests|FullyQualifiedName~FinanceRiskEngineTests'
```

The fixture's per-protocol lock and concurrent-invocation test establish one invocation/evaluation
within that test instance; no cross-process production concurrency guarantee is claimed.

Initial development builds found new-code syntax/analyzer violations, corrected without policy
suppression. The sandbox build could not complete without execution permission; the same build
was run with permission. No pre-existing correctness/scientific/security/lineage blocker was
established. No raw logs are published.

## Security, scientific invariants and rollback

All pre-existing production/test files remain byte-identical to baseline. In particular strategy
math, cost/fill rules, OOS/holdout calculations, classifications, campaign outcomes, old identities,
entitlement and historical evidence are unchanged. Web/runtime/CI/package/schema boundaries are
unchanged. Builds/tests prove this synthetic boundary and existing regressions, not market validity.

No real data/provider/model/export, app-auth waiver, trading capability or deployment. Synthetic
100,000 backtest capital is the existing hypothetical simulation parameter, not allocated capital.
Finance remains **RESEARCH / 0 SEK / NONE**. Future real-data adaptation, production ledger,
cross-cohort accounting, statistics, rights/retention and authentication/security remain unimplemented.

Rollback is a separately authorized revert of this additive checkpoint; no production schema/data
migration or runtime rollback exists. Existing deterministic Finance has no dependency on the new
reasoner path. README/ARCHITECTURE/ADRs/other modules/runbooks assessed: no decision change needed.
Relevant current status/backlog/roadmaps/module/contract/testing/recovery/catalog are reconciled;
historical A/BB-130 evidence remains dated and unchanged.

## Remaining work

Independent owner/architect review and exact-candidate merge approval are complete. Production adaptive
accounting/journal/auth/rights integration remains future work; BB-131C is not authorized.
No current pre-existing blocker was established by this checkpoint.

## Resumption

BB-131B is ACCEPTED / MERGED / CI VERIFIED as the bounded synthetic engineering proof.
Use current main, the accepted-publication record above and canonical recovery documentation.
STOP — return published main to owner/architect for independent verification. BB-131C remains
NOT STARTED / NOT AUTHORIZED. No deployment or next-checkpoint permission follows.
