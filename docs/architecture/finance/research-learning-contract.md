# BB-131A — Finance Research Learning contract

## BB-131C persistent governance addendum — 2026-09-22

**IMPLEMENTED / AUTOMATICALLY VERIFIED / REVIEW CANDIDATE ONLY.** Separately owner-authorized from `cb520994f3ae4222bab1ba31d8cc683abbc28991`.
[Implementation, ownership and limitations](../../reports/features/finance/bb-131c-persistent-learning-ledger-20260922.md). Existing EodhdMarketMemory and FinanceSchemaMigrator
own additive migration 94 and one bounded synthetic ledger; no second database or scientific engine.
The fixed B protocol reserves invocation, complete plan, three trials and exposure durably before
computation. Immediate SQLite transactions protect concurrent requests; failures retain spent authority.
Existing result stores remain scientific truth; ledger binds checked IDs/checksums and native verdicts.
No production reasoner, endpoint, new provider, autonomous loop, statistical or execution claim.
Future real-model access still needs auth/scoped authorization, audit, resource/egress limits,
export/retention rights and security review. Unknown real-data cohort relationships fail closed.
ADR 0038 stays Accepted. A/B evidence below is historical; no accepted decision is rewritten.
Finance **RESEARCH / 0 SEK / NONE**; no deployment. BB-131D NOT STARTED / NOT AUTHORIZED.


- Date: 2026-09-21
- Status: **ACCEPTED / MERGED / CI VERIFIED — architecture only**
- Baseline: `7747b8f715204dc3ccf753f170238b40238d1b11`
- Branch: `bb-131a/research-learning-architecture`
- Decision: [ADR 0038 — Accepted](../../adr/0038-finance-research-learning-authority-contract.md)
- Assessment/recovery/verification: [BB-131A report](../../reports/features/finance/bb-131a-research-learning-architecture-20260921.md)

BB-130 remains COMPLETE / EXIT APPROVED. BB-131A authorizes analysis and documentation,
not implementation. Owner/architect accepted the exact reviewed architecture candidate;
ADR 0038 is now Accepted. The original proposed contract/slice wording below describes future
implementation, not shipped capability. Existing accepted ADRs remain authoritative.
Finance remains **RESEARCH / 0 SEK / NONE**.

## BB-131B implementation addendum — 2026-09-22

Separately owner-authorized B is **ACCEPTED / MERGED / CI VERIFIED**.
[Implementation evidence](../../reports/features/finance/bb-131b-synthetic-contract-replay-proof-20260922.md) maps the minimal typed
boundary to this accepted decision. Two additive module files provide immutable synthetic scope,
closed JSON admission, full new identities and existing result references. Reasoner/orchestration,
cohort ledger, commitment/result manifests and SQLite replay are test-only. No accepted ADR decision
or existing engine changes. The initial projection supports only empty pre-invocation history;
no general adaptive projection or production ledger is claimed. Preflight conservatively counts
all engine calls (including duplicates), rejecting excessive plans without shrinking them.
Replay is reproduction of consumed engineering evidence, not fresh statistical confirmation.
Future real-data/auth/rights/retention/cross-cohort requirements below remain binding.
Historical A wording describes architecture acceptance, not a prohibition on the separately authorized B.
Finance RESEARCH / 0 SEK / NONE; no deployment or BB-131C authority.

## Objective and Definition of Done

Define the smallest extension through which a research reasoner can suggest a bounded
experiment, and Finance can independently reject it or produce reproducible research evidence.
**AI MAY PROPOSE. DATA MUST PROVE. RISK MAY VETO.**

A is complete for review when existing boundaries, structured contracts, adaptive-testing
controls, holdout protection, lineage, security prerequisites and a fixture-first B slice are
specified with source evidence and limitations. No provider, production capability, schema,
experiment, changed historical result or deployment is part of A. BB-131 as a whole is not
complete, and this proposal does not authorize B or a provider phase.

## Current capability map

Paths below are relative to the repository. REUSE AS-IS means reuse the named responsibility
and accepted semantics, not that its present API already accepts untrusted proposals. EXTEND
identifies a future integration gap; no extension is implemented in A.

| Capability | Exact existing boundary | Classification and limit |
| --- | --- | --- |
| Canonical revision identity | `src/BigBrain.Modules/Finance/CanonicalMarketData.cs`, `DatasetRevisionAssembly.cs`; `src/BigBrain.Api/Finance/CanonicalDatasetRevisionIdentityV2.cs` | REUSE AS-IS: exact source/product/revision/checksum, causal knowledge time. V2 applies to future promotions; never recompute legacy IDs. |
| Noncanonical owner research | `FinanceResearchDatasets.cs`: `FinanceResearchDatasetItem`, `ResearchCatalog`, `RunBoundedResearchBacktest`; module `ResearchDatasetLineage` | REUSE AS-IS for purpose-specific limited evidence; distinct from canonical eligibility. Current maintenance runner is buy-and-hold with `research-no-features-v1`, not an arbitrary feature/strategy executor. |
| Eligibility and entitlement | `ResearchDatasetEligibilityPolicyV1`, `ResearchDatasetFacts`, `ResearchCapabilityDecision`; `MarketDataEntitlements.cs`; ADR 0021/0022/0027 | REUSE AS-IS. EXTEND only the admission projection that binds the exact purpose and rights decision. EligibleWithLimitations is not globally eligible; private research permission is not external AI export permission. |
| Causal features | `DailyFeatureEngine.cs`, immutable feature revisions, ADR 0023; `FinanceResearchFeatureLibrary` | REUSE AS-IS. Registered context features are not necessarily executable strategy inputs. Unsupported feature/data combinations stop. |
| Strategy and parameter identity | `IResearchBacktestStrategy`, `StrategyIdentity`; `BuyAndHoldResearchStrategy`, `MomentumResearchStrategy`, `SmaCrossoverResearchStrategy` | REUSE AS-IS compiled strategies only. EXTEND a typed admission allowlist; no code/DSL/plugins. Actual parameter keys are `period`, `fastPeriod`, `slowPeriod`. |
| Backtest and cost/fill/sizing | `DeterministicBacktestEngine`, `BacktestRunConfiguration`, `BacktestCostModel`, `BacktestFillModel` | REUSE AS-IS BB-123 v2. Pin exact configuration and checksum, not merely a model label. Simulation capital is hypothetical, never spendable capital. |
| OOS/holdout | `AntiOverfittingPolicy`, `ResearchDataPartition`, `SelectionGovernanceEvidence`, `DeterministicRobustnessEvaluator`; `FinanceRobustnessStore.cs` | REUSE AS-IS numerical/selection rules; EXTEND admission, cross-proposal exposure accounting and commitment before execution. Existing prior-use lookup covers exact strategy/revision/feature/date scope, not every overlapping future experiment. |
| Robustness | `EvaluationPlan`, `RobustnessEvaluationResult`, parameter grid/cost ladder/walk-forward | REUSE AS-IS. Existing evaluator does both selection and holdout in one call; no development-only switch. Do not expose its full result to an adaptive reasoner on an active holdout. |
| Hypotheses and integrity | `AutonomousResearch.cs`: `ResearchHypothesis`, `ResearchComplexity`, `ResearchIntegrityVerdict`, `FinanceResearchContracts.Assess` | REUSE vocabulary; EXTEND proposal/falsification/provenance envelope. Existing hypothesis IDs are generated by fixed internal logic, not accepted from AI. DSR/PBO remain NotEvaluable. |
| Autonomous orchestration | `FinanceAutonomousResearch.cs`: `RunAutonomousResearch`, `SelectCurrentResearchEvaluations`, `research_run_experiments` | REUSE idempotency, single-flight and negative-history principles. EXTEND only after separate authorization: runner currently builds/selects fixed current evidence and cannot execute arbitrary pinned proposals. Do not route a proposal into it and claim it was honored. |
| Campaigns | `FinanceResearchCampaigns.cs`: `ResearchCampaignDefinition`, `ResearchCampaignVariant`, limits, scorecard, disposition | REUSE population/lineage/bounded-ledger concepts; EXTEND for proposal admission. Actual BB-129A emits categorical ineligible/insufficient results with no backtest reference; it does not execute OOS research. Campaign `lookback`/`fast`/`slow` labels are not engine parameter keys; no implicit alias mapping. |
| Risk | `FinanceRiskEngine.cs`: `FinanceRiskProposal`, `FinanceRiskEvaluation`, server-owned policy and durable halts; ADR 0030 | REUSE AS-IS for applicable prospective RESEARCH proposals. It needs current EOD/cadence/health/exposure evidence, not just historical returns. No fabricated risk ALLOW for a historical learning iteration. |
| Result vocabulary | `RobustnessVerdict`, `SelectionGovernanceOutcome`, `ResearchExperimentVerdict`, `ResearchCampaignDisposition`, `FinanceRiskVerdict` | REUSE AS-IS as typed source results. No new universal profitability/robustness score. Scientific result and operational outcome stay separate. |
| Immutable storage/replay | `FinanceBacktestPersistence.PersistBacktest`, `FinanceBacktestStore.cs`, `FinanceRobustnessStore.cs`; existing Finance SQLite | REUSE AS-IS result writers/readers. EXTEND future learning envelope/admission journal only after storage design approval. Readers deserialize historical JSON; they are not universal integrity validators. |
| API and Web | `FinanceEndpoints.cs`, reader interfaces; `src/BigBrain.Web/src/finance/FinanceObservation.tsx`, `useFinanceObservation.ts`, `useFinanceBacktestDetails.ts`, `financeSnapshotCache.ts` | EXTEND later with a narrow authorized evidence projection. Current human-facing responses include holdout/selection details; do not give a reasoner unrestricted API access. Browser stale cache is display-only, never admission evidence. Preserve BB-128B/C behavior. |
| Scheduler/resource governor/operations | `FinanceResearchScheduler.cs`, `FinanceResearchResourceGovernor.cs`, `FinanceResearchOperations.cs`; ADR 0034–0036 | NOT NEEDED FOR BB-131A or fixture B. Existing bounded scheduling/recovery may be reused later. Operational Allow is not scientific/risk authority; manual research is outside the scheduled governor. |
| Model adapters/educational inputs | Brain's logical orchestration responsibility in ARCHITECTURE; module's planned educational-source role | FUTURE. No new Brain project/service/provider/SDK/source-ingestion subsystem is needed now. |
| Execution authority | Proposed ADR 0017–0020 and master-roadmap promotion gates | FUTURE and outside BB-131. No reachable broker/order/PAPER/LIVE/AUTO/capital path. |

### Findings that constrain the design

BB-123 preserves exact next-session fills, hypothetical spread/slippage/fees and versioned
identities; daily bars do not prove intraday liquidity or correct corporate actions.
BB-124 uses 60/20/20 with 50-session embargoes, minimum 126/40/40 sessions and validation-only
selection. Its 75% positive validation-excess/family-median control is an explicit heuristic,
not a multiple-testing significance guarantee. BB-092's family-attempt visibility and integrity
check do not impose a cumulative adaptive-search budget; DSR/PBO checks remain NotEvaluable.

BB-129A's accepted 24 attempts (15 ineligible, nine insufficient, zero robust candidates)
are not numerical OOS results. Its eight-dataset/three-variant/24-attempt limits are different
units from a robustness evaluation's underlying backtest-run budget. BB-127 research revisions
lack the accepted immutable feature/selection chain required by that campaign. Do not promote
or synthesize this missing chain in BB-131A/B.

`BuildRobustnessEvaluations` returns same-version evidence and detects prior evaluated holdout
for exact strategy/feature/market/date scope. A new model, family name, overlapping revision,
universe or date choice must not become a way for a future learner to claim unseen evidence.
This is an extension prerequisite, not evidence that today's fixed runner was exploited.

## Ownership and authority

```text
Finance-owned authorized evidence projection (no protected holdout)
  -> reasoner port: one bounded structured suggestion, or NoUsefulProposal
  -> Finance admission: schema + rights + lineage + policies + budget + duplicate/holdout checks
  -> committed, frozen exact execution plan
  -> existing deterministic backtest / robustness / applicable risk evaluation
  -> immutable source results + learning lineage references
  -> permitted summary for a later, separately admitted research question
```

Reasoner may identify unanswered questions, missing evidence, bounded variants and falsification
criteria, or decline to propose. Its explanations are untrusted commentary. It cannot create
eligibility, set a verdict, invent an evidence reference, rewrite history, change policy,
choose hidden data after outcomes, tune calculations, unhalt risk or authorize an external action.
No textual instruction, tool-call request or confidence score confers authority.

Finance owns contracts, admission, calculation and persistence. Future Brain code owns only
model-neutral reasoning orchestration and model adapters, and calls the normal authorized
Finance application/API boundary. It receives no database, file-path, shell, generic HTTP,
provider, scheduler-control or execution capability. Web continues to render server truth.
No parallel engine, database, service, general experiment platform or generic plugin layer.

## Proposed versioned contracts

Proposed contract identifier: `finance-research-learning-v1`. This is a documentation contract,
not a shipped type or accepted wire schema. Public HTTP, if ever authorized, remains `/api/v1`
and Problem Details; A/B adds no route. Unknown version, property, enum or capability rejects.

| Concept | Minimal structured content and ownership |
| --- | --- |
| Research question | Embedded bounded question text (max 1,024 characters), target/horizon capability ID and optional parent hypothesis reference. No separate question table or engine. Text cannot alter the structured target. |
| Existing ResearchHypothesis | Reuse existing feature IDs, target/horizon, universe, exact market/feature references and family/fingerprint concepts. Finance constructs the trusted record only after validation; do not deserialize a model response directly into it. Existing v1 IDs/results remain unchanged. |
| ReasonerInput | Contract/projection version, immutable input checksum, opaque authorized scope handle, knowledge cutoff, permitted capability choices, bounded evidence summaries and complete family-attempt/exposure/budget summary. Generated by Finance, not the model. |
| ProposalDraft | Discriminated `Proposal` or `NoUsefulProposal`; input checksum, scope handle, structured target ID, bounded rationale, parent reference, 1–3 declared variants and 1–8 falsification criteria. No mode, risk verdict, execution command, new policy or source URL field. |
| ExperimentVariant | Supported StrategyIdentity plus complete exact numeric parameter map and an offered dataset/feature binding handle. No expressions, code, arbitrary ranges, implicit defaults or model-selected cost/holdout/risk thresholds. All declared variants execute or acquire an explicit non-executed outcome. |
| EvidenceReference | Tagged kind (dataset revision, feature revision, backtest, robustness, research experiment, campaign, risk evaluation), immutable ID, source contract/version and stored checksum where available. Finance verifies scope, existence, lineage and rights. Never arbitrary path/URL/SQL. If no native checksum exists, use a versioned projection checksum; do not pretend one is a native identity. |
| FalsificationCriterion | Allowlisted metric/gate ID, development/validation phase, comparison enum, decimal threshold, unit and required sample-policy reference. Missing measurement gives NotEvaluable, never zero. Conditions are conjunctive and can only strengthen immutable mandatory gates; no user-defined expression/JSONPath. Selection criterion and tie-break remain server-owned. |
| CommittedProposal | Finance-generated immutable proposal ID/hash, exact normalized draft, exact input/evidence references, approved protocol/family/holdout-cohort IDs, complete resolved execution plan and budget reservation. Includes policy versions AND immutable policy snapshots/checksums where no canonical policy entity exists. |
| LearningResult | Operational state/reasons, proposal/iteration/input references, actual attempted variant/run/evaluation IDs and checksums, exact original typed scientific classifications and limitations, optional matching risk evidence, missing-evidence reasons. Optional AI explanation is separately labelled and never overwrites a source verdict. |

Proposed wire limits: input/output each at most 64 KiB UTF-8, JSON depth at most eight,
maximum 32 evidence references, 16 bounded summaries and 2,048 characters per rationale;
parameter count at most two for the first allowlist. Reject duplicates in object keys/arrays
where set semantics apply; reject nonfinite numbers, null required fields and silent truncation.
If complete governance history cannot fit, provide a deterministic aggregate with counts and a
history digest, plus bounded ordered examples; never present a selected-success-only history.
The trusted validator reads the complete ledger, not this condensed reasoner view.

The draft union has two closed shapes. `Proposal` requires version, discriminator,
inputChecksum, scopeHandle, targetId, question, rationale, parentHypothesisRef (explicitly null
for a root), variants and falsificationCriteria. `NoUsefulProposal` permits only version,
discriminator, inputChecksum, scopeHandle, reasonCode (`InsufficientEvidence` or
`NoSupportedQuestion`) and bounded explanation; variant/execution fields are forbidden.
References and IDs are opaque bounded strings resolved from the offered scope; arbitrary new IDs
are not capabilities. Strategy parameters are decimal values, not numeric expressions or strings.
Criterion comparisons are `LessThanOrEqual`, `GreaterThanOrEqual` or `Equal`, constrained by the
chosen metric's offered policy; units and phase must match that metric exactly. Initial policy
accepts only the specified validation-excess criterion. This contract must become explicit
closed types/schema and negative tests in B, not permissive dynamic dictionaries at the boundary.

### Exact execution binding

A committed plan references the existing `BacktestRunConfiguration` and `EvaluationPlan`
semantics: exact dataset kind/revision/checksum, instrument/universe identity, feature revision
and source membership, strategy/version/complete parameters, simulation/fill/sizing versions,
all cost values/versions, calendar, dates, seed, embargo, thresholds, grid, selection rule,
robustness model, applicable risk-policy snapshot and entitlement decision evidence.
A dataset ID alone or `latest` is never sufficient. Canonical and noncanonical identities do
not become aliases. No caller can replace knowledge cutoff with wall-clock completion time.

Initial executable proposal allowlist is **momentum/v1 with period=20 only**. Its existing
robustness diagnostic/selection grid remains period 5/10/20: those three effective trials must
be disclosed and counted even though the reasoner offers one reference variant. All three corresponding causal feature periods must be
available in the pinned feature revision; a momentum20 feature alone is insufficient. Benchmark
buy-and-hold is an engine-owned control, not a selectable new hypothesis. SMA may be supported
later only through an explicitly reviewed mapping to actual feature periods and exact engine
keys; BB-129A's labels/40/80 periods are not silently adopted.

Falsification v1 for the fixture uses `validation.excessReturn <= 0` (fractional return,
not percent) as failure, plus the unchanged engine sample/selection/robustness gates. It is
not a profitability declaration or a newly calibrated scientific rule. Holdout failure remains
an engine-owned terminal test; the reasoner cannot choose its metric or threshold after seeing it.

### Identity and normalization

Reject ambiguous payloads before normalization. Canonicalize validated structured values using
a versioned explicit field order, UTF-8, ordinal ID/map ordering, UTC timestamps, ISO dates and
invariant finite decimal values without equivalent spelling/scale ambiguity. Preserve ordered
variant/trial sequences where order is meaningful; normalize declared sets only. Reject unknown
keys and incompatible units rather than guessing. Bind resolved exact references and policy
snapshots, not model-authored checksum claims. B must pin test vectors for culture/order/decimal
normalization. Do not change any existing Finance identity algorithm.

Use full SHA-256 for the new committed proposal and retain the full digest even if a display ID
is shortened. Separate an exact sanitized response/input digest from the **execution fingerprint**:
changing rationale wording, JSON key order, provider, request ID or timestamp must not create a
fresh executable experiment or budget. A new rationale can be linked as duplicate evidence.
Existing `FinanceResearchContracts.Fingerprint` serializes an object; it is not by itself a
canonicalizer for arbitrary untrusted JSON. Existing hypothesis IDs remain referenced as-is.

## Multiple-hypothesis governance

This proposal extends BB-124/092/129 accounting around the same engines; it does not introduce
an alternative statistics engine or claim a p-value/FDR guarantee.

### Family and protocol identity

Finance, not the reasoner, registers a research protocol before any reasoner call: objective,
target/horizon, evidence universe, allowed strategy families/parameter cells, development and
protected cohorts, selection/falsification rules, complete effective trial population and budgets.
Family membership is assigned from this registry and parent lineage, not generated prose.
Renaming a hypothesis, switching models, changing an idempotency key, shifting a threshold or
using a new overlapping revision cannot reset family/program history. Novel or ambiguous family
membership rejects pending human review; semantic-near-duplicate detection by an LLM is not trusted.
Within the first finite allowlist, exact parameter cells and parent/protocol membership make
near-duplicate accounting deterministic. Even distinct allowed cells consume the same budget.

### Proposed conservative first protocol limits

| Unit | Proposed initial ceiling | Accounting |
| --- | --- | --- |
| Reasoner invocations / submitted proposals | 1 / 1 per frozen protocol | No repair prompt, automatic retry or second suggestion. Malformed/no-useful output terminates this protocol invocation. |
| Families / caller variants / instruments | 1 / 1 / 1 | Reference momentum20; three engine-owned period trials counted separately, not hidden. |
| Robustness evaluations | 1 | Exact existing plan; five cost scenarios, benchmark and walk-forward work are predeclared diagnostics, not five new independent hypotheses. |
| Underlying backtest runs | At most 64 | Existing EvaluationPlan.MaximumRuns plus a preflight plan/budget check; charge actual unique runs, preserve reused-run links. Not BB-129A's 24 dataset/variant attempts. |
| Concurrency / automatic retries | 1 / 0 | Existing single-flight/idempotency principles. Timeout/restart does not refund unknown work or renew the protocol. |
| Reasoner deadline / whole-iteration cap | 30 / 300 seconds | Proposed engineering bounds; never accept partial scientific output or silently omit trials to meet deadline. |
| Fresh holdout release | At most one frozen selection per cohort | Benchmark and selected strategy form the declared comparison; no second adaptive candidate gets a fresh claim over overlapping data. |

These numbers are proposed engineering limits, not experimentally established statistical power.
For B they constrain only the synthetic fixture. A later multi-iteration production protocol
requires separate review of cumulative family/program limits and dependence between cohorts;
there is no authorization to loop indefinitely under new protocol IDs. A proposal for more than
one caller variant rejects in the initial policy even though the general schema can represent three.

Count every admitted hypothesis/parameter trial, including failed, rejected, timed-out, duplicate
and reused experiments through linked history. Keep submission count, admitted scientific trials,
actual engine executions and reused results as distinct counters (a duplicate must not create
new computation or be presented as an independent observation). Never erase losses or only retain
survivors. Excluded/ineligible variants retain reason codes. No stopping on an attractive interim
result, no post-hoc budget enlargement, no choosing which failed trial to hide.

A future production extension must atomically persist proposal commitment, budget reservation and
exclusive holdout-cohort reservation BEFORE computation. Separate processes/keys cannot each take
the last slot. Existing SQLite transaction/single-flight patterns are reusable; current research
rows do not already implement this cross-proposal ledger. Uncertain completion stays reserved;
recovery reconciles exact run IDs, never reruns with a fresh key or restores an unseen claim.

### Holdout protection and adaptive learning

Do not give the reasoner protected rows, derived features, returns, charts, distributions,
selected-holdout run IDs, holdout-linked risk metrics, full robustness payloads or classifications
that indirectly reveal active holdout performance. Suppress outcome-derived rejection details
until the cohort is closed. An opaque capability handle may identify an approved split without
permitting arbitrary data reads. Redaction includes aggregate/proxy/instrument-overlap paths.

Commit all variants, selection rule and allowed internal engine grid before calling the current
combined evaluator. The reasoner never participates between validation selection and holdout.
After unblinding, mark the cohort consumed across the entire research program, descendants and
reasons/models, including aborted or rejected attempts that revealed a statistic. Exposure through
a human/operator or another channel counts too; a new prompt cannot make previously seen data unseen.
Overlapping instruments/sessions, adjusted/reissued copies or revised features remain tainted
unless independent review proves a genuinely new eligible cohort. Unknown exposure fails closed.
A hash is an identity aid, not proof that datasets are independent or holdout is secret.

Existing already-published evaluations may inform exploratory questions with their consumed label.
They cannot be relabelled as fresh confirmation. A future learner can see a closed cohort's negative
and positive outcomes, but its descendants need independently qualified untouched evidence before
confirmation. No protected real-market holdout is claimed in B: synthetic data are engineering
fixtures, and possible model pretraining exposure is another reason not to claim historical data
are unknown to an external model. Fresh prospective evidence may ultimately be needed.

Stop on budget exhaustion, no useful proposal, repeated/ambiguous proposal, data/rights/lineage
gap, unknown cohort exposure, policy drift, persistence ambiguity or operational failure.
Advancement requires unchanged eligibility/causality/cost/OOS/holdout/robustness controls and
complete trial history; missing evidence stays missing. An engineering pass, `MoreRobust` or
`Challenger` never promotes strategy lifecycle, mode, shadow deployment or capital.
DSR/PBO/CSCV remain NotEvaluable; no new significance method is implemented or promised.

Adaptive reuse needs additional statistical assumptions beyond a fixed held-out test, as explained
by [Dwork et al., Preserving Statistical Validity in Adaptive Data Analysis](https://arxiv.org/abs/1411.2664).
Our inference for this repository is to restrict the first protocol to a frozen synthetic proof,
not implement reusable-holdout algorithms or advertise adaptive statistical validity.

## Reasoner input and validation

Allowed summaries are server-created from permitted development evidence: source-typed prior
classifications, metric ID/value/unit/sample coverage, robustness limitations, causal context,
purpose-specific dataset capabilities, missing-evidence codes, applicable risk outcomes and
complete negative-history accounting. Each summary includes exact provenance and whether it is
engineering-only, exploratory or from a consumed cohort. No arbitrary raw data, full database,
private paths, credentials, browser cache or unbounded text. Input selection/order/redaction has
its own version and digest. Missing units/unknown rights do not become zero values or grants.

Validate in this order before any engine call:

1. Bounded parse; known schema/version/discriminator; reject extra/duplicate keys, invalid values,
   free-form tools/code, impossible references and contradictory NoUsefulProposal content.
2. Resolve input digest, authorized scope and current rights/provenance from Finance-owned state.
   Prevent arbitrary reference enumeration; recheck changed entitlement/halt/policy at commitment.
3. Resolve strategy/version and exact parameter allowlist; verify feature definitions/periods,
   dataset purpose/schema, price/currency/calendar compatibility, causal cutoffs and policy snapshots.
   No implied aliases, `latest`, global eligibility or model-created feature revision.
4. Normalize and recompute identity; compare exact and family/parameter duplicates, parent graph
   (no cycles), program history and exposure/overlap. Unknown relationship rejects.
5. Expand all engine-owned grid/cost/benchmark/window work and reserve the complete budget and
   holdout scope atomically. Reject rather than silently shrinking an excessive proposal.
6. Persist commitment and validation evidence before invoking the exact compiled engine with the
   frozen resolved configuration. Never call a different `latest`-based runner as a substitute.

UNKNOWN => FAIL CLOSED applies to authorization, required scientific inputs and unsupported
capabilities. It does not retroactively revoke ADR 0022's explicit owner-accepted *limited private
research* decision: preserve that limitation, refuse unsupported conclusions, and require a
separate explicit decision for model-provider disclosure. No inferred redistribution permission.

## Results, risk and reproducibility

Keep exact existing typed results: robustness `InsufficientData/Fragile/Mixed/MoreRobust`,
selection `Pass/Fail/InsufficientData/Contaminated`, autonomous
`Rejected/Inconclusive/Promising/Challenger/NotEvaluable`, campaign
`Rejected/InconclusiveNotEvaluable/SurvivedInitialScreen/RobustCandidate` and risk verdicts.
These are not interchangeable. Do not map `MoreRobust` directly to campaign `RobustCandidate`,
map missing evaluations to zero, or turn an admission rejection into a measured scientific loss.
LearningResult references the evaluator's actual output plus missing-evidence/admission reasons;
no second scientific classifier. UI labels may explain the source vocabulary without rewriting it.

Risk is a separate deterministic authority. Historical learning may reference an existing matching
risk evaluation or state that current prospective risk evidence was not evaluated/applicable.
Never synthesize current provider/cadence health from historical bars or use old ALLOW for a new
proposal. DENY/HALT/INSUFFICIENT_DATA cannot be overridden; even ALLOW means hypothetical RESEARCH
only. The current risk engine's declared spread/sector limitations remain visible. B can exercise
a synthetic deny/missing-evidence fixture, but cannot claim production risk approval.

Retain contract/projection/normalization/protocol/engine versions; sanitized future adapter/model
identity and generation settings when applicable; exact permitted input projection plus references
and digest; bounded sanitized response/draft; normalized proposal and execution fingerprint;
family/parent/cohort/selection/budget events; every validation failure and admitted/skipped attempt;
exact run/evaluation/result checksums, classifications, limitations and optional commentary.
Distinguish data event/session time, source knowledge time, research knowledge cutoff, proposal
received/committed time, execution start/completion and persistence time. Audit wall-clock times
are not an input to deterministic experiment identity. Reject impossible ordering, never backdate.

Model generation need not be repeatable. Deterministic replay starts from the frozen proposal,
resolved evidence and exact engine/policy versions without calling the reasoner. Reopened results
must have identical underlying run IDs/checksums and relevant structured outcomes. Revalidate
stored identities/checksums and relationships at the new trust boundary; an existing JSON reader
is not such a validator. Hash equality alone is not authenticity or license permission.

A production learning journal would belong to the same Finance persistence boundary, referencing
existing result rows rather than copying scientific truth into a parallel store. Its schema,
transaction/restart/retention integration needs a later explicit implementation decision. Do not
assume current result_json can be arbitrarily extended to store the complete admission ledger.

Retention follows source rights, including derived inputs, summaries, prompts and model output.
Preserve negative results for the permitted lifecycle, with safe deletion/tombstone evidence where
rights require removal. Replay then honestly becomes unavailable; never retain prohibited derived
data to satisfy a reproducibility slogan. Provider keys, credentials and unrestricted raw model
payloads are not audit data. A malformed response can leave reason/size/hash evidence without
storing unsafe text; hashes/references alone are not a promise of content replay.

## Failure and degradation

| Condition | Required response |
| --- | --- |
| Unavailable reasoner, timeout or NoUsefulProposal | Record bounded operational outcome; no engine call, no auto retry, existing Finance remains independent. |
| Malformed/unsupported/excessive/unauthorized draft | Record sanitized validation rejection and stable reason; no execution or policy relaxation. |
| Duplicate | Link original immutable outcome; no extra experiment, no new independent success, no renewed holdout. |
| Insufficient data, ineligible purpose, stale lineage or contamination | Preserve native NotEvaluable/InsufficientData/admission reason as applicable; never try another dataset/threshold to obtain a pass. |
| Risk veto or policy/rights change | Deny proposed advancement/admission as applicable, preserve deterministic evidence, do not unhalt or rewrite policy. |
| Persistence unavailable/conflict | No execution without durable commitment; no success without durable result. Quarantine conflict for human review. |
| Interrupted committed iteration | Reconcile exact reservation and result IDs on restart; completed evidence reused, unknown work remains consumed/reserved, incomplete state retained. No automatic fresh-key retry. |
| Result cannot be replayed under current retention rights | Expose unavailable evidence and provenance limitation, not a replacement calculation or fabricated old result. |

Future operational status can reuse Pending/Running/Completed/Failed plus validation reason codes;
NoUsefulProposal is a reasoning outcome, not a strategy verdict. Do not alter today's scheduled
operations/circuit breakers or make them depend on reasoner availability.

## Security and external knowledge

General application auth is explicitly incomplete in ARCHITECTURE and the accepted BB-130 exit.
Existing research POST/read routes and home-network placement are not proof of a secure AI
capability boundary. Source review here is not a penetration test or a newly reproduced exploit.
Docs A and a strictly local synthetic test fixture B can proceed before that broader sprint because
they expose no service, real dataset, credentials or external actions. **RESEARCH-only is not a
waiver for real model access or remote mutation.**

Before any production reasoner integration: require reviewed authenticated principal/scope,
per-resource Finance read/propose permission, deny-by-default capabilities, identity-bound audit,
rate/resource/token limits, egress allowlist, data export/retention rights and prompt-injection/
output-handling tests. No arbitrary URL fetch, shell, SQL, dynamic code or tool recursion. Render
commentary as untrusted text. Schema conformance alone cannot establish authorization or prevent
indirect injection; enforce independent controls outside the model. This follows the separation
and least-privilege guidance in the [OWASP prompt-injection prevention guidance](https://cheatsheetseries.owasp.org/cheatsheets/LLM_Prompt_Injection_Prevention_Cheat_Sheet.html).
No SDK, model or provider is selected in A.

Broker, orders, PAPER, LIVE, AUTO, capital allocation, provider activation and other high-authority
external actions remain blocked by the existing separate owner/security/entitlement gates and
are not capabilities of BB-131. Research output cannot self-promote into those later stages.

Future educational material attaches as a separately tagged **idea source**, with rights/provenance
and retrieval/version metadata. It is not an EvidenceReference proving a Finance outcome.
Investopedia remains planned-only: concept -> proposal -> deterministic evidence -> robustness/risk.
No scraping, ingestion or external educational integration. Even benign source text is untrusted
and may neither expose protected holdout nor change policy.

## Proposed BB-131B — fixture contract and replay proof

Smallest useful next slice, **NOT STARTED / NOT AUTHORIZED by A**:

1. Add only the minimal typed Finance proposal/input/result contracts and pure fail-closed
   validation needed for the initial momentum20 policy; fake reasoner and orchestration live
   in tests. Do not create a Brain project, runtime registration, endpoint, worker or provider.
2. Use one deterministic synthetic instrument with pinned synthetic market/feature revisions,
   existing strategy/backtest/robustness engine and policies unchanged. Input projection contains
   development evidence only. Freeze the complete engine plan/grid before evaluation. Synthetic
   outcome labels must always remain engineering-only, including any positive control.
3. Prove accepted draft -> exact existing EvaluationPlan -> evaluator/underlying BacktestResults.
   Persist the underlying runs through `FinanceBacktestPersistence` into isolated temporary
   SQLite using existing schema; reopen through the existing reader and independently compare
   IDs/checksums/content. Serialize the learning envelope and robustness result in a **test-only**
   bounded manifest for restart/replay assertions, not a second production evidence store.
4. Replay the frozen proposal without invoking the fake reasoner, reproduce existing evaluation
   and run checksums, verify immutable conflict rejection and absence of new duplicate rows.
   Include invalid/duplicate/over-budget/unknown-exposure/rights and risk-denial fixtures.
   Verify invalid output never calls an engine; no-useful/timeout does not affect direct Finance.
5. Assert no hidden-holdout field/proxy crosses the input boundary, culture/order normalization
   invariance, semantic changes alter fingerprints, altered prose cannot reset budgets, all
   engine-owned trials are counted, no claim of fresh holdout after simulated exposure and no
   promotion to risk/execution authority. Preserve native missing-evidence outcomes.

B DoD is a reviewed, reproducible **contract proof**, not a deployed learning loop or production
crash-safe admission ledger. No real provider/data/holdout, production schema change, new API,
scheduler integration, strategy/calculation change or actual scientific conclusion. Required
implementation verification would include focused contract/scientific/persistence regressions,
backend Release build/full tests, existing format gate, docs/diff/secrets checks, and unchanged
Web/runtime/Finance safety boundaries. Counts and success cannot be claimed before implementation.

Real-data adaptive iteration must wait for separate authorization of exposure/family accounting,
atomic production admission/recovery/retention, complete evidence lineage and authentication/
authorization. A fake fixture cannot prove those production properties. If B cannot bind the
existing engine without changing scientific behavior, stop for architecture review rather than
expand it. No campaign success, feature-lineage repair or statistical method is smuggled into B.

## Review questions and limits

Owner/architect approved ADR 0038, the conservative one-invocation initial protocol, synthetic-only
B proposal and explicit real-data/security prerequisites as architecture, not implementation authority. Open future choices:
production journal schema and crash atomicity; protocol/cohort overlap adjudication; data rights
for any future provider; statistical power/confirmation procedure for genuinely adaptive research.
These block production expansion, not documentation publication. No newly reproduced current
correctness/security/scientific/lineage defect requiring blocker handoff was established.

BB-123/124/127/128/129 historical evidence is preserved. No tests, code, schema, packages, CI,
provider state, calculations, scientific results or runtime were changed or executed by A.
No deployment or owner UX/security approval is claimed.
