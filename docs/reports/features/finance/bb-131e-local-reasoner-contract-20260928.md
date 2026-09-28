# BB-131E — Local Reasoner Contract & Isolation Foundation

Detta är en sanerad GitHub-version. No credentials, private addresses, raw provider/model payloads
or sensitive host identifiers are included. All new scientific test data are deterministic synthetic fixtures.

## Metadata

- Date: 2026-09-28.
- Baseline: `e2f9cd761d0c8ed7569939b9ada908e6de37cf3f`.
- Branch: `bb-131e/local-reasoner-contract`.
- State: **ACCEPTED / MERGED / CI VERIFIED**.
- Authority: accepted ADR 0038/0039 and owner-authorized E; no first-model invocation authorization.
- Finance: **RESEARCH / 0 SEK / NONE**. AI MAY PROPOSE. DATA MUST PROVE. RISK MAY VETO.
- Explicit owner/architect acceptance and unchanged merge; no deployment/runtime verification.

## Status

**ACCEPTED / MERGED / CI VERIFIED**. No deployment.

### Accepted publication — 2026-09-28

Explicit owner approval applies to exact reviewed candidate `b7e3d226644576e84b59135f285bccdc36626c52`.
Pre-merge main, candidate parent and merge-base: `e2f9cd761d0c8ed7569939b9ada908e6de37cf3f`.
Remote SHA resolved directly; one commit ahead/zero behind, exactly the reviewed 19 paths and unchanged
contract-only semantics. No unrelated local work entered the merge.
Merge: `a020dbac0899f3db663644c038d116e9b012f081`.
First parent: `e2f9cd761d0c8ed7569939b9ada908e6de37cf3f`.
Second parent: `b7e3d226644576e84b59135f285bccdc36626c52`.
Candidate tree and merge tree: `bbe3f43e4da91fb36654c615274f48f4dbae1631` — identical.

[Exact merge CI 36466024491](https://github.com/T-bear/BigBrain/actions/runs/36466024491): **SUCCESS**.
Actual required jobs and steps inspected, all SUCCESS:

- Backend checkout/setup, `dotnet restore BigBrain.slnx`,
  `dotnet format BigBrain.slnx --verify-no-changes --no-restore`,
  `dotnet build BigBrain.slnx --configuration Release --no-restore`,
  `dotnet test BigBrain.slnx --configuration Release --no-build`.
- Frontend checkout/setup, `npm ci`, `npm run format:check`, `npm test -- --run`, `npm run build`.
- Documentation: `node scripts/verify-documentation.mjs`.
- Secrets: `gitleaks/gitleaks-action@v2`.

BB-131E **ACCEPTED / MERGED / CI VERIFIED**. Reasoner contract IMPLEMENTED AND AUTOMATICALLY
VERIFIED; production reasoner NOT IMPLEMENTED. Local model NOT SELECTED / NOT INSTALLED /
NOT DOWNLOADED / NOT INVOKED. External AI OPTIONAL FUTURE ONLY / NOT CONFIGURED / NO CLOUD FALLBACK.
No required paid AI. ADR 0038/0039 remain Accepted. Finance **RESEARCH / 0 SEK / NONE**;
PAPER not authorized or implemented by E; LIVE/AUTO not authorized; broker/orders/capital NONE.
No deployment authorized or performed. No next checkpoint started or authorized.

Acceptance covers only the reviewed contract/parser/test foundation; no implementation change is
made during publication. First-real-model auth/use-rights, audit, native isolation/transport,
resource/receive limits, cancellation/kill enforcement and provenance/capacity gates remain.
No interrupted E work remains. Final documentation-only reconciliation is identified by
`git log -1 --format=%H --grep='^docs: reconcile accepted BB-131E reasoner contract$' main`.
Its own exact-SHA CI must pass before handoff; GitHub commit/Actions history and the final handoff
identify that SHA/run without embedding a commit's own hash in its contents.

## Recovery and characterization

The interrupted session left only the correctly based E branch, source/architecture inspection and
90/90 passing B/C characterization tests (56 B + 34 C; no skipped tests). No E implementation,
commit or remote candidate existed. Resume verified baseline/HEAD, branch, tracked-clean state and
retained TRX evidence; reused that analysis/test result and preserved unrelated mockups/ADR 0006–0009.
The single recovery note previously described completed D; it did not claim a missing E implementation.

Actual inspected boundaries: no Brain project before E; Modules owns LearningDevelopmentInput,
closed JSON admission and scientific identities; EodhdMarketMemory owns C ledger/persistence;
API Program has no complete general authentication/authorization pipeline. Existing provider/Sentinel
authentication does not authenticate a research principal. No API composition/auth code is changed.

## Placement and contracts

`src/BigBrain.Brain` is a non-deployable .NET class library with one interface:
`Task<LearningReasonerReply> ReasonAsync(LearningDevelopmentInput, CancellationToken)`.
It references Modules contracts only; no API/SQLite/network/model package, executable, runtime
registration or service/container. API does not reference Brain; only the existing test project does.
A library is the smallest enforceable assembly boundary for Brain's logical ownership, without
misplacing orchestration in Finance or creating a second proposal schema/shared platform.

Finance's new `ResearchLearningReply.cs` extracts B's parser unchanged in policy and adds a closed
reply union: sealed Proposal carrying the existing LearningProposalDraft, or sealed NoUsefulProposal
carrying the existing reason/explanation. Only Finance assembly construction is available; there are
no public constructors, extension cases or authority fields. Both retain the exact original bounded
JSON for lossless Finance re-admission and the existing response checksum. No reserialization,
default filling, aliases or repair occur. Parsing success is explicitly NOT admission or eligibility.

`ResearchLearningAdmission.cs` calls the same extracted parser, preserving B's context-check order
with an internal Finance callback. Eligibility, budgets, exposure, duplicate checks and commitment
remain in Finance. The public parser accepts only the development projection, never full scope,
ledger snapshots, human API responses, holdout or result payloads. Finance still rechecks exact raw
response against its own current authoritative scope/state; a forged caller projection grants nothing.

## Wire validation and scientific authority

Existing limits remain 64 KiB decoded UTF-8, depth 8, question 1,024/rationale 2,048 characters.
Closed properties, duplicate keys at all depths, version/discriminator, null/missing fields,
trailing/fenced JSON, single variant, momentum/v1 period20, exact evidence/binding and fixed
falsification all reuse B's checks. No tool/capability/risk/PAPER/LIVE/AUTO property is accepted.
URL/SQL/code/injected prose is inert metadata, never fetched/evaluated/rendered or given authority.
The parser receives a complete string: future transports MUST separately enforce receive/allocation
limits. This checkpoint does not claim to implement a bounded streaming transport.

The only IResearchReasoner implementation is a private deterministic test fake. It stores configured
wire text/failure mode and receives the typed projection plus cancellation only; no network/file/DB/
engine/service-locator dependency. Parsing failures are separate operational exceptions, not proposals.
The test caller, not the fake, owns isolated SQLite and orchestration.

The end-to-end proof uses existing synthetic momentum20 scope and C sequence:
enroll -> commit ReserveLearningInvocation -> fake -> shared parser -> ReserveLearningProposal ->
StartLearningExecution -> DeterministicRobustnessEvaluator -> existing result persistence -> completion.
Three engine-owned 5/10/20 trials, one caller variant/invocation, zero retries and the <=64 run bound
remain. Native verdicts/IDs/checksums are preserved; no MoreRobust promotion or profitability claim.
The reasoner cannot select holdout, reserve budget, execute science, create eligibility or risk ALLOW.

Reopen retains completion, spent invocation/trials/exposure and immutable result references.
Duplicate/changed wording links C's original result without a new invocation or engine start.
B/C's existing replay, crash/restart, concurrency and risk regression tests remain unchanged.
No ledger/schema/migration, old identity algorithm, strategy/cost/fill/OOS/holdout or result changes.

## Decline, failures, risk and isolation limits

NoUsefulProposal is a normal typed result; C retains its existing decline history/terminal state,
spent invocation, zero trials/engine starts and no result. No retry. Unavailable/timeout, malformed
output and cancellation retain C's spent invocation and a failure; no weaker policy or fallback.
The test harness maps operational failures to C's existing bounded ReasonerUnavailable/ReasonerTimeout
codes; parser diagnostics are asserted in tests, not claimed as a new durable operational audit.

Pre-cancelled work never reserves/invokes. In-flight cancellation propagates; a simulated adapter
ignoring cancellation and returning late is discarded before admission. Existing deterministic
Finance remains usable independently. Risk DENY/HALT/INSUFFICIENT_DATA/missing evidence terminates
reserved test work without refund or execution. No production risk ALLOW is fabricated.

Reflection/source tests pin the port's sole two parameters, two closed reply cases, Brain's one
exported interface, absence of API/SQLite/HTTP dependencies and absence of API->Brain registration.
This is an application contract proof, NOT a sandbox: future native runtime isolation, process
termination, resource watchdog, auth/permissions, durable operational audit and kill switch are
still unimplemented. No production orchestration loop or fake is shipped as AI functionality.

## Security

Zero paid AI dependency, SDK, model/runtime, API credential or external AI call. No cloud fallback
or external adapter exists. No model is selected, installed, downloaded or invoked. Synthetic test
rights do not expand real-provider entitlement, StrategyTraining, local inference or external export
permission. No new endpoint or fabricated authenticated principal/audit identity. No deployment,
PAPER/LIVE/AUTO, broker/orders/capital or provider activation. Scientific safety remains Finance-owned.

## Evidence

Final source-tree verification PASS on 2026-09-28. Baseline B/C **90/90** before parser extraction.
Focused final E/B/C **136/136** (E 46, B 56, C 34), full API **800/800** and Sentinel **32/32**;
zero failed/skipped tests. Solution restore with force-evaluate PASS, including the new project
reference graph. Release build **zero warnings/errors**. Full format verify **exit 0**.

The full suite includes unchanged relevant scientific regressions: deterministic backtest 10,
robustness 13, research datasets 9, campaigns 10, autonomous research 15, backtest persistence 4,
risk 13 (**74/74**), plus B/C and the rest of the backend. No extra full-suite rerun was needed.
Frontend/API composition/ledger/schema/scientific engine/identity/ADR/runtime/CI baseline diffs
are empty; no frontend work or local frontend run was required.

Documentation/link/index verifier PASS: **254 Markdown files / 91 backlog IDs**. Full-history
Gitleaks PASS: **291 commits, no leaks**. Working and staged diff checks PASS. Explicit staged inventory: exactly 19 intended files
(seven solution/project/source/test files, twelve Markdown documents). Staged Gitleaks PASS/no leaks;
publication evidence is the exact branch commit and owner handoff. No raw test logs or TRX files are published.

Commands (development formatting was restricted to the four E C# files):

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --filter 'FullyQualifiedName~ResearchLearningContractTests|FullyQualifiedName~FinanceLearningLedgerTests' --logger 'trx;LogFileName=bb131e-baseline.trx'
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --filter 'FullyQualifiedName~ResearchReasonerContractTests|FullyQualifiedName~ResearchLearningContractTests|FullyQualifiedName~FinanceLearningLedgerTests' --logger 'trx;LogFileName=bb131e-focused.trx'
dotnet restore BigBrain.slnx --force-evaluate
dotnet build BigBrain.slnx --configuration Release --no-restore
dotnet test BigBrain.slnx --configuration Release --no-build --logger 'trx;LogFilePrefix=bb131e-final'
dotnet format BigBrain.slnx --verify-no-changes --no-restore
node scripts/verify-documentation.mjs
git diff --check
git diff --cached --check
gitleaks git --log-opts=--all --redact --no-banner
gitleaks git --pre-commit --staged --redact --no-banner
```
During development, a new theory initially exposed an internal enum in a public signature (compile
failure, fixed in test declaration). A new input-mismatch test incorrectly assumed 400 vs 401 sessions
must change development digest; partition rounding can preserve the development projection. The
fixture now uses a distinct development population. This was a test assumption, not a scientific
policy defect; protected holdout noninterference is independently tested.

## Changes and unchanged boundaries

Implementation: Brain csproj/interface; solution entry; test-project reference; extracted Finance
parser/closed replies; admission delegation; one new focused test file. Existing B/C test files,
C ledger, schema, engines, identities, API composition, Web, package versions, CI, runtime/provider/
deployment configuration and historical reports/ADRs are unchanged. No unrelated files are staged.
Canonical current-state addenda and this report/catalog/recovery document the narrow candidate.
README, unrelated module/runbooks and security policies need no change: no user-facing/runtime
capability is enabled; the D security gates remain binding.

## Remaining work

Before any real local model: separate owner authorization and accepted implementation of scoped
identity/use rights, durable sanitized audit, isolated transport, receive/resource limits, cancellation/
kill enforcement, pinned model/runtime provenance and measured capacity, plus adversarial native
isolation tests. Application contract tests do not satisfy those operational gates. Optional external
adapters require their additional export/provider/privacy/egress/cost authorization; none is required
for a qualified local path. Initial synthetic-only/empty-history and C general-cohort/backup limitations
remain. No subsequent checkpoint is authorized.

## Resumption

The candidate is identified by the exact remote tip of `bb-131e/local-reasoner-contract` with
parent `e2f9cd761d0c8ed7569939b9ada908e6de37cf3f`; the single canonical recovery note and this
report provide review evidence without terminal history. No interrupted implementation remains.
Rollback is a separately reviewed revert of E's contract/extraction and solution references;
there is no database migration or runtime deployment to undo. Existing B/C persistence is untouched.

STOP — return accepted BB-131E main to owner/architect for independent post-merge verification
and planning of the next bounded checkpoint. No model installation/invocation, next checkpoint or deployment.
