# Finance recurring research — model-free implementation

Detta är en sanerad GitHub-version.

Initial publication: 2026-10-14; final release correction: 2026-10-15. **REVIEW CHECKPOINT / NOT MERGE CANDIDATE / NOT OPERATIONALLY ACCEPTED.**
Implemented and tested functionality is distinguished below from the remaining contract/release gaps.
No deployment, activation or current host-state verification was performed.

## Metadata

- Accepted baseline: `0851ee48a768348a001f1525f0b30c29584fd821`.
- Baseline tree: `685fbe8be345d25a4775a8f64ef7b0d62687798c`.
- Branch: `finance/recurring-research`.
- Original reviewed implementation: `dc3e5bd42e4c135c7927ca3f9c5cfedbd04e7cc9`;
  tree `ce791771859346ed784cf274d3f3f5a5c34c9808`, preserved without rewriting.
- Current publication subject: `review: gate recurring authority and compose local reasoner`.
- Resolve the exact publication commit/tree from that unique subject and Git metadata; no recursive
  self-hash is embedded in this report. Main was verified unchanged before publication.
- Owner authorized implementation against contract review `ca2b0fddaf58b050bde3f43d36db12cfff47ab66`,
  tree `e1413c2367129e33acad43efe62dc91556a7346c`. Neither the contract nor release-analysis branch
  was merged to obtain this baseline.

## Final release correction — 2026-10-15

**Decision: REVIEW CHECKPOINT / NOT MERGE CANDIDATE for unattended release.**
This is one bounded correction on the same branch, not a new recovery or architecture project.

| Blocker | Actual result |
| --- | --- |
| Restored consumption authority | **Safely blocked; automatic rollback detection remains unavailable.** Scheduler and Finance kernel refuse every new non-fixture invocation with `finance.research.recurring.restoreReconciliationRequired`. No configuration/clean-start flag clears it. |
| Production local composition | **Implemented, default disabled.** First-party `BigBrain.Finance.Hosting` composes the same API process, existing scheduler and LocalReasonerRuntime without API↔Brain dependencies. No grant/profile is installed or automatically created. |

A normal restart preserves the same budget/history and completed replay performs no work. However,
Finance and lifecycle data restored consistently can be indistinguishable from a normal restart.
A same-store hash or clean lifecycle marker is not an independent authority witness. Consequently
**both normal and uncertain production reopen are blocked from resuming consumable authority**.
This is deliberate conservatism, not a claim to distinguish an undetectable full restore. Model-free
fixture execution remains test-only; it cannot admit real-origin receipts, and hosting cannot select it.
An owner saying “resume” or changing MaintenancePaused does not bypass the kernel. No refill,
reset, second database, new schema migration or operational restore drill was introduced.

The smallest remaining security decision is how already-consumed authority can be reconciled against
independent non-rolled-back evidence. There is no automatic reconciliation command or invented external
witness. Per this task's explicit stop rule, that question stops here rather than opening a side project.
No unattended release or production inference is recommended while this concrete stop remains.

Composition is an alternative executable entry for the **same existing API host**, not a second
service/container/scheduler. Docker packaging publishes it, while maintenance and build identity retain
API ownership. No production build/deployment occurred. API keeps only a narrow trusted composition
callback; Brain has no Finance-store dependency. The root disposes the owned runtime after API exit,
including failure. Native worker/isolation/grammar/runtime implementation are unchanged in this correction.

Trusted external startup Profile requires owner and artifact/isolation evidence references, UTC approval
and expiry, and every runtime resource/path/argument value. The complete profile hash binds the frozen
reasoner identity. Missing, future, expired or out-of-bounds profiles fail closed. No real profile ships.
The ordinary runtime limit remains at most30s; the historical180s acceptance exception is not exposed.
If real use needs another bound, it requires a separately approved resource-policy decision, not reuse
of old acceptance authority. Artifact/isolation references are attestations to reviewed provisioning,
not automatic validation of a server that this task did not inspect or modify.

Existing runtime audit is projected into a closed, at-most-four-entry array in the reserved Finance
attempt. It retains invocation ID, phase/outcome, fixed failure, time/duration, transport/config hashes
and exit status. It stores no raw prompt/reply/path and is excluded from visible research history.
Terminal audit survives evaluation reservation and reopen; failure remains subject to existing runtime
cleanup/disable behavior. No parallel audit store is added.

Changed implementation surface: `BigBrain.slnx`, API project/friend boundary, Program/Dockerfile,
FinanceRecurringResearch, FinanceResearchScheduler, FinanceResearchOperations; new three-file
`src/BigBrain.Finance.Hosting`; API test project and recurring tests. Canonical contract, STATUS,
BACKLOG and recovery updated; existing catalog link remains sufficient. No unrelated files staged.

### Correction verification

- Focused recurring tests after composition/gate/audit implementation: **24 PASS / 0 FAIL / 0 SKIP**.
- New coverage includes ordinary and isolated old-copy reopen blocked before reasoner; immutable
  budget read/replay; disabled/missing/future/180s profiles and missing grant; real owned **proof worker**
  through production configuration/composition for proposal/evaluation and decline, durable sanitized
  audit, and unchanged API/Brain dependency direction. No Qwen/model artifact/network is used.
- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore`:
  **1177 PASS / 0 FAIL / 8 intentional real-model SKIP**, 1185 total,2m38s. Includes the additional
  profile expiry/regression/identity witness and all25 recurring tests, scheduler/governor/maintenance,
  recovery, concurrency, scientific, finite-session and owned-runtime regressions.
- `dotnet publish src/BigBrain.Finance.Hosting/BigBrain.Finance.Hosting.csproj -c Release --no-restore -o <isolated review output> -p:BigBrainBuildRevision=UNKNOWN`:
  **PASS**. API appsettings, both runtimeconfig files and Brain dependency included. Running only
  `system-build-identity` through each published entry returned `revision=UNKNOWN` exactly. No API
  server or database owner was started for this packaging smoke check. No Docker build/deployment.
- `dotnet build BigBrain.slnx -c Release --no-restore`: **PASS, 0 warnings / 0 errors**.
- `dotnet format BigBrain.slnx --no-restore --verify-no-changes --include` with the seven changed
  C# files: only whitespace diagnostics in FinanceRecurringResearch and its tests. Corrected with
  `dotnet format whitespace BigBrain.slnx --no-restore --include` for those two files, then reverified.
  No semantic code changed after the passing API suite/build; their evidence is reused.
- `node scripts/verify-documentation.mjs`: **PASS,267 Markdown files /91 unique backlog IDs**.
- `git diff --check`, `git diff --cached --check`: **PASS**. Scope:17 intended files; unrelated
  untracked mockups and ADR0006–0009 remain excluded/preserved.
- `gitleaks protect --staged --redact --no-banner`: **PASS, no leaks** using the installed local binary.
- Initial new-project build required dependency restore; subsequent compile exposed an entry namespace
  and analyzer issue, both corrected before the passing focused run. No failing check is hidden.
- Prior checkpoint's two-iteration/restart/budget/concurrency/replay/holdout tests remain in the suite.
  Existing runtime cancellation/kill/reap, finite-session, scientific and provider-lineage regressions
  are reused where unchanged. No unrelated frontend/Sentinel execution or BB-132K work.

### Concrete next decision

Review this exact safety stop and completed composition. **Release remains blocked by independent
consumption reconciliation**, not by a general need for more analysis. Required later owner decisions:
that narrowly defined authority mechanism, actual eligible scientific data/cohorts, finite budget,
explicit artifact/resource profile and any counted real-model validation, then deployment/activation.
No current approval or profile is fabricated. No code here exposes program creation through startup
or HTTP. Finance RESEARCH / 0 SEK / NONE.

## Status

The following initial implementation evidence is historical; the correction above supersedes its
production-composition gap and makes its restore limitation an enforced safety stop.

The new Finance-owned program explicitly freezes a finite grant, one instrument/source/mapping,
scientific eligibility evidence, reasoner identity, two disjoint cohort opportunities and expiry.
No grant is created by migration, configuration, startup, reopening or a new request ID.
The same SQLite owner selects and validates daily receipts, seals knowledge cutoff, builds existing
daily features and the unchanged momentum scientific plan, and reserves cumulative invocation/runtime
and evaluation/trial/call authority before work. Exact replay returns retained state without execution.

The existing scheduler gains a mutually exclusive recurring mode; maintenance, recovery, governor,
work cadence and durable opportunity journal remain authoritative. Internal composition binds the
existing reasoner port through the accepted narrow delegate and completion/cleanup bound. A fake
reasoner and the owned model-free BRF1 proof worker exercise this path; no production fake is registered.

Two fixture cohorts enter through the existing observation owner. Iteration1 is admitted/evaluated,
its native results persist, Finance reopens, and iteration2 sees only the allowed history and evaluates
once under the remaining shared grant. No second research engine, strategy language, result database,
generic scheduler or model training was added. Receipt fixtures retain fixture origin; no fixture is
labelled as a real acquisition. Production admission requires AcquiredProviderDaily.

The operational boundary is deliberately different from a synthetic success claim: fixed reference
validation excess return can be shown to the next model invocation; protected values/classifications,
full result IDs and receipt identities cannot. Full evidence identities and visible identities are
separate. Actual evaluator completion timing stays private; history release uses the precommitted next
opportunity and fails closed if completion misses it. Later evidence does not change an earlier frozen scope. Late historical acquisition that
missed the next-session-open knowledge boundary is ineligible, regardless of its source date.

## Changes

- New `src/BigBrain.Modules/Finance/RecurringResearch.cs`: bounded explicit authorization/cohort/history.
- New `src/BigBrain.Api/Finance/FinanceRecurringResearch.cs`: admission/projection, same-owner durable
  program, reservations, result linkage/reopen and bounded orchestration.
- New `src/BigBrain.Api/Finance/FinanceScientificLease.cs`: existing legacy work registers in-flight
  ownership, preventing a racing recurring grant; program authority excludes subsequent legacy starts.
- `FinanceSchemaMigrations.cs`: additive99, empty tables, no grant seed or old evidence rewrite.
- `FinanceResearchScheduler.cs`, `FinanceResearchOperations.cs`: recurring mode/readiness/status,
  existing governor/recovery/maintenance and journal integration.
- `FinanceAutonomousResearch.cs`, `FinanceBacktestStore.cs`, `FinanceRobustnessStore.cs`,
  `FinanceResearchCampaigns.cs`, `FinanceResearchDatasets.cs`: shared exclusion at existing entries.
- `FinanceLearningLedger.cs`, `FinanceFiniteResearchSession.cs`: recognize additive99; old grants
  remain unchanged and cannot run concurrently with the new explicit program.
- `ResearchLearningContracts.cs`, `ResearchLearningReply.cs`: optional safe recurring history and
  explicit versioned real scope; strict schema/vocabulary/falsification remain unchanged.
- `src/BigBrain.LocalReasoner.Worker/worker.cpp`: describe supplied recurring history and instrument
  truthfully; grammar, model parameters, resource controls and isolation are unchanged.
- New `tests/BigBrain.Api.Tests/FinanceRecurringResearchTests.cs`; small causal updates in
  `FinanceObservationMappingTests.cs` and `FiniteLocalReasonerIntegrationTests.cs`.
- Canonical contract, STATUS, BACKLOG, report catalog and recovery link to this evidence.
  Git diff is the exhaustive file inventory; unrelated mockups and ADR0006–0009 are excluded.

## Evidence

No test requires a provider credential,
network market call or model artifact. Eight opt-in real-model tests remain deliberately skipped.

Final broad verification:

- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore`:
  **1170 PASS / 0 FAIL / 8 intentional real-model SKIP**, 1178 total. This includes the strengthened
  history-bearing holdout noninterference test and independent budget ceiling witnesses.
- `dotnet build BigBrain.slnx -c Release --no-restore`: **PASS, 0 warnings / 0 errors**.
- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~FinanceRecurringResearchTests`:
  **20 PASS / 0 FAIL / 0 SKIP**, including late-knowledge/fractional-volume rejection.
- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~FinanceRecurringResearchTests|FullyQualifiedName~FinanceResearchSchedulerTests|FullyQualifiedName~FinanceResearchOperationsTests'`:
  **45 PASS / 0 FAIL / 0 SKIP**, after correcting truthful running/next-opportunity diagnostics.
- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~FinanceRecurringResearchTests`:
  **20 PASS / 0 FAIL / 0 SKIP** after the final precommitted history-release correction. This recompiles
  the changed API in Release; unchanged full-suite and solution-component evidence is reused for
  these narrow admission/status/projection corrections.
- `dotnet format BigBrain.slnx --no-restore --verify-no-changes --include <changed C# paths from this checkpoint>`:
  **PASS** for all changed C# files. Final three-file delta also **PASS**: FinanceRecurringResearch.cs,
  FinanceResearchScheduler.cs and FinanceRecurringResearchTests.cs.
- `node scripts/verify-documentation.mjs`: **PASS**,267 Markdown files/91 unique backlog IDs.
  The initial report failed the required-heading check; its headings/sanitization notice were corrected.
- `git diff --check` and `git diff --cached --check`: **PASS**.
- `gitleaks protect --staged --redact --no-banner` (installed local Gitleaks binary): **PASS, no leaks**.
  Scope reviewed:25 intended files; unrelated untracked work excluded.

Earlier development evidence, retained honestly:

- Initial new two-iteration/readiness tests: 2 PASS after fixing version99 recognition and reusing
  the accepted receipt identity serializers. Initial failing runs exposed those defects; they were fixed.
- Focused integration/runtime/scheduler/mapping/SHADOW selection: 130 PASS / 1 FAIL. Failure was a
  legacy busy-response compatibility regression introduced by the new exclusion; corrected.
- First full API run: 1164 PASS / 2 FAIL / 8 SKIP. Failures were legacy concurrent idempotent backtest
  builds and the accepted no-API-to-Brain dependency assertion. Both were corrected: legacy builders
  retain their existing concurrency; trusted composition retains the narrow delegate and no API Brain reference.
- Corrected focused selection: 115 PASS / 0 FAIL / 0 SKIP. It included recurring research, EODHD
  integration, reasoner contract, research operations, scheduler and autonomous-research tests.
- The final checks include the strengthened history-bearing holdout noninterference witness and each
  I/E/T/C/W budget stop, not just the earlier positive sequence.

No unrelated frontend/Sentinel suite is needed: their code/contracts are unchanged. The API suite,
Release solution build and scoped formatting cover the changed persistence/orchestration dependencies.
Existing isolation/cleanup, parser, daily science and SHADOW regression evidence is reused and exercised
by applicable API tests. No new BB-132K restore drill or native model execution was performed.

## Remaining work

1. **Consistently stale restore remains an unresolved contract gate.** A complete rollback of program,
   receipts and results to an internally consistent older database cannot be detected using checksums
   from that same database. Current recovery readiness does not independently attest consumed authority.
   Partial/native exposure conflicts, ordinary reopen and in-flight uncertainty are checked; they do
   not prove complete rollback resistance. Restored authority must stay paused pending reviewed
   consumption reconciliation. No parallel witness/ledger or recovery project was invented here.
2. **Production composition/provisioning is not wired by configuration.** The internal scheduler path
   is functional and tested, but startup registers neither a real reasoner binding nor an automatic
   program creator. A mere recurring-mode flag cannot activate inference. An approved runtime profile
   and a narrowly reviewed provisioning/host binding are needed for actual unattended local inference.
   This is remaining integration work as well as an owner authorization gate, not a credential-only step.
3. **Data eligibility is not established for the live installation.** Program inputs require actual
   rights, identity, raw price/corporate-action and complete timely observation evidence. Historical
   downloads cannot be backdated. At least the native partition minima must be satisfied within
   each disjoint cohort; the two old live receipts alone cannot support this research path.
4. **No automatic replacement program or uncertain-result finalization.** One program per database,
   at most two cohorts, existing1,000-receipt projection cap. Reserved or indeterminate execution
   stops for review, even if some native results exist. No grant refill, arbitrary continuation or
   silent replay is available. A crashed legacy lease also requires review; startup does not erase it.
5. **No hard evaluator wall-time preemption.** The accepted synchronous evaluator is bounded by
   frozen sessions/trials/calls. It is not abandoned on timeout. Real inference must use a separately
   approved bounded runtime profile including cleanup; historical180s acceptance authority is spent.

## Resumption

The shortest next step is review of this concrete implementation and a decision specifically on
restore-consumption reconciliation and the trusted production binding/profile. There is no proposal
for another general architecture or consolidation sprint. Once those two concrete integration gates
are settled, owner must approve exact scientific data evidence, I/E/T/C/W, cohort dates/expiry,
resource profile and a counted real-model validation, followed separately by deployment/activation.
This checkpoint cannot truthfully authorize an unattended Finance Automation Release yet.

## Security

Finance **RESEARCH / 0 SEK / NONE**. No real model or provider invocation, production database/config
change, credentials, deployment, recovery operation, trading, capital, Qwen tools, SHADOW automation
or grant renewal. Model rationales remain untrusted metadata; fixture returns prove engineering
boundaries, not profitability. No secrets, private paths, raw payloads or server state are published.
After publication STOP for independent architect review; no merge is authorized.
