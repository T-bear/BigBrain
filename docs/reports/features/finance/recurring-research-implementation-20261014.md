# Finance recurring research — model-free implementation

Detta är en sanerad GitHub-version.

Initial publication: 2026-10-14; final release correction: 2026-10-15. **REVIEW CHECKPOINT / NOT MERGE CANDIDATE / NOT OPERATIONALLY ACCEPTED.**
Implemented and tested functionality is distinguished below from the remaining contract/release gaps.
No deployment or activation was performed. The final authority section records narrowly scoped
read-only mount/capability inspection; it does not revalidate production evidence or operational health.

## Metadata

- Accepted baseline: `0851ee48a768348a001f1525f0b30c29584fd821`.
- Baseline tree: `685fbe8be345d25a4775a8f64ef7b0d62687798c`.
- Branch: `finance/recurring-research`.
- Original reviewed implementation: `dc3e5bd42e4c135c7927ca3f9c5cfedbd04e7cc9`;
  tree `ce791771859346ed784cf274d3f3f5a5c34c9808`, preserved without rewriting.
- Reviewed correction: `d7eaa4aa04c8de6af3832563d95e4c517bdd9218`;
  tree `5fb37874d8146f7fc216517ddfaa05c704fe4eea`.
- Previous authority checkpoint: `e57b8f30ae70a8a37f4b89730855c4278cd3939d`,
  tree `750cb1b7d586b1510524d41de492e12188691864`.
- Current publication subject: `review: clarify normal restart and manual restore safety stop`.
- Resolve the exact publication commit/tree from that unique subject and Git metadata; no recursive
  self-hash is embedded in this report. Main was verified unchanged before publication.
- Owner authorized implementation against contract review `ca2b0fddaf58b050bde3f43d36db12cfff47ab66`,
  tree `e1413c2367129e33acad43efe62dc91556a7346c`. Neither the contract nor release-analysis branch
  was merged to obtain this baseline.

## Normal restart and manual restore approval — 2026-10-15

**B. BLOCKED.** The owner now requires automatic normal restart, but only manual reconciliation
following backup restore. Automatic post-restore resumption is no longer a release requirement.
This supersedes that requirement in the earlier authority discussion; it does not supply evidence
that a particular startup is a normal restart. The existing production stop remains unchanged.

### Bounded finding and smallest missing dependency

The existing integration points were inspected, without executing recovery or querying production:
`SystemRecoveryCoordinator.ReadPreviousSession` reads clean state from restorable lifecycle SQLite;
`MayStartTimeSensitiveWork` checks readiness/clock/health, not database freshness.
`FinanceDataProtection.DrillRestore` verifies an isolated staging copy and removes it; it does not
mediate replacement of the production database or Docker volume. The backup/restore runbook describes
isolated drills and migration snapshots. `scripts/verify-bigbrain-recovery.sh` reads service/status
information; it is not a mandatory restore interlock. ADR0026/0028/0036 do not establish one.
The prior checkpoint's infrastructure evidence is reused; no new host inspection or BB-132K work.

**The one remaining dependency is a trustworthy distinction between ordinary restart and replacement
with older durable state.** With the revised requirement, the minimum candidate boundary is an
independently protected, mandatory restore interlock: every permitted production restore/volume
replacement must durably inhibit research startup *before* replacement, and the inhibitor must not
roll back with Finance/lifecycle/config backups. Missing or contradictory interlock state must deny
startup. Its custody, restore exclusions and exclusive control of replacement paths must be established;
an operator reminder, another writable file or optional restore hook is insufficient. An unmediated
storage rollback would still require independent consumption evidence; it is not covered by an
interlock that only handles voluntary restore commands.

No existing mechanism establishes these properties. Creating a flag here would incorrectly allow an
older self-consistent database to resume. **No code, flag, migration or manual-clear command is added.**
The smallest next owner/architect decision is approval of a concrete enforced restore/startup boundary
and its covered failure model, including who can replace volumes and how bypass is prevented. This is
a specific operational/security dependency, not a request for another general investigation. It is not
provisioned or approved by this checkpoint. If that boundary cannot be guaranteed within scope, the
existing stop must remain; a protected independent consumption witness remains an alternative, not a
new mandatory platform proposed by this task.

### Actual restart behavior and reconciliation limits

- Existing ledger/history can reopen and exact completed attempts can replay without consuming work.
  Fixture tests demonstrate remaining/spent budget, uncertainty stops and protected history across reopen.
- New real-origin inference/evaluation remains refused on **both normal and restored startup** with
  `finance.research.recurring.restoreReconciliationRequired`. This is safe refusal, not successful
  automatic normal restart or reliable restore classification. Maintenance acknowledgement cannot clear it.
- Manual reconciliation is **not implemented** because trustworthy restore identification/consumption
  evidence is unavailable. Any later reconciliation must bind the original program/grant and retained
  consumption, reservations, uncertainty and protected-data exposure to independently trustworthy history.
  It may not lower consumption, erase uncertain attempts or refill any quota.
- If that history cannot be established, the old authorization must remain unusable. A separate explicit
  new authorization decision must not automatically reuse previously exposed protected cohorts; unknown
  exposure is a refusal condition. Neither an approval click nor a fresh program ID supplies this proof.

### Verification for this checkpoint

Only five existing canonical documents change. Source, tests, build and configuration remain identical
to d7eaa4a. Reused executed evidence: full API **1177 passed /0 failed /8 intentional real-model skips**
and Release build **0 warnings /0 errors**, with commands recorded below. No executable tests rerun.

| Requested scenario | Existing evidence and honest limit |
| --- | --- |
| Normal restart, remaining budget/history | `TwoDisjointIterationsPersistNativeScienceAndSanitizedHistoryAcrossReopen` proves fixture continuation; production continuation is blocked. |
| Exhausted quota | `ExhaustedBudgetAndClockRegressionStopBeforeReasoner` and two-iteration replay retain spent authority. |
| Older database | `RealAuthorityNeverResumesFromCleanReopenOrConsistentOldRestore` refuses both a reopened store and its consistent isolated snapshot. It proves refusal, not rollback detection or successful reconciliation of later spending. |
| Missing/contradictory restore information | No independent restore information protocol exists; unconditional production denial remains. No invented protocol tests claimed. |
| Interrupted/concurrent/duplicate work | `ConcurrentInvocationAndCrashDoNotRefundOrCreateAnotherExperiment`, negative-outcome and replay tests retain reservations/no extra execution. |
| Manual reconciliation | Unsupported; no passing manual-resume test claimed and no warning-dismiss bypass introduced. |
| History and holdout protection | `ProtectedHoldoutChangesDoNotChangeVisibleInputOrLaterHistoryDigest` and sealed-receipt regression evidence reused. |

Publication verification: documentation verifier, working/staged whitespace, staged Gitleaks and
exact scope/unchanged implementation comparison. No new build/full suite, model/provider call,
deployment, database/secret change or restore exercise. Finance **RESEARCH / 0 SEK / NONE**.
STOP after the single additive review checkpoint; automatic recurring release remains blocked.

## Final authority reconciliation — 2026-10-15

Historical decision context; the revised requirement and current verdict appear above.

**B. BLOCKED — no qualifying independent monotonic authority exists in the inspected accepted
implementation/deployment contract.** No source/test/configuration change. The enforced production
stop from d7eaa4a remains intact. This is the task's explicit safety stop, not a new implementation plan.

### Existing mechanisms and actual boundaries

| Candidate | Verified evidence | Why it cannot authorize safe automatic resumption |
| --- | --- | --- |
| Finance ledger/receipts/operations | FinanceRecurringResearch and FinanceResearchOperations persist in the Finance SQLite owner. | A complete older database preserves internally valid checksums while rolling back consumption. |
| Lifecycle journal | SystemRecoveryCoordinator stores sessions/events/clean markers in a separate SQLite store. Compose and scoped live mount inspection show separate Finance/lifecycle **local-driver, local-scope, writable volumes** in the same API container. | Separate path/volume is not an anti-rollback authority. API can write both; accepted recovery scope includes both. Clean/unclean, clock sanity and quick_check do not prove highest consumed sequence. |
| Local file outside Finance | No existing protected marker backend, dedicated writer capability or enforced restore exclusion was found. | A mode0600 file, rename/fsync, checksum/HMAC or local hash chain cannot detect restoration of its own older valid prefix. A second directory or disk alone does not establish the required protection. |
| Sentinel | Program exposes read-only metrics/snapshot capability; SentinelRequestAuthorizer replay cache is an in-memory ConcurrentDictionary. ADR0003 freezes Sentinel v1 as read-only. | No durable conditional monotonic update bound to Finance program/grant/consumption exists. Adding privileged authority mutation would exceed an extension of an existing capability and require a separately approved boundary. |
| Hardware-backed counter | Read-only sysfs enumeration found **0 visible TPM devices** in this execution environment. No accepted TPM/counter integration/provisioning contract exists in main. | Not proof that physical hardware is absent, and not authority to provision hardware or add a platform. It supplies no usable, attested backend here. |

Read-only observations used scoped Docker Compose API identity and mount metadata only, plus volume
Driver/Scope and filesystem lookup. Finance/lifecycle volume driver/scope are verified local/local;
physical filesystem/device lookup was unavailable, including noninteractive privileged lookup.
**Current device identity and physical independence remain unverified.** No private mount source,
container environment, secret, database content or provider payload is published. More filesystem
read permission alone would not add the missing monotonic capability/protection contract.

Historical recovery-scope evidence was read from the existing BB-132K execution report at
`0e798221debeffdb7f3286114bd1b01383e1af39`: Finance and lifecycle are both in the inventoried full
recovery scope. This is reuse of its documented scope, not a new backup/drill or assertion about the
latest archive. ADR0028 supplies checksum/restore integrity and inherited retention rules, not rollback
resistance. ADR0026/0036 supply lifecycle/maintenance recovery, not an independent consumed-budget head.
Untracked Sentinel ADR0006–0009 were excluded as implementation authority; proposed prose is not a
provisioned service. No existing server component was changed or granted new privileges.

### Exact missing dependency and minimum action

**One dependency: a provisioned, independently protected monotonic consumption witness with a reviewed
custody and restore boundary.** The owner/architect must identify and explicitly authorize the concrete
primitive and its writer/administrator/reset rules: excluded from Finance/lifecycle restores, protected
against older valid state replacement, missing/reset identity always fail-closed, and durable conditional
advance binding program ID, authorization hash, cumulative consumption and sequence. Ordinary API file
write access or a declaration that a directory is “outside backup” does not establish this guarantee.

The minimum next action is that specific storage/privilege/restore-boundary decision and provisioning
proof, **not another broad review sprint and not permission to bypass the current stop**. No available
accepted component implements it. Creating a new database/service or widening Sentinel mutation
therefore cannot be represented as the permitted small Finance correction. If no such primitive can
be approved within the owner's constraints, automatic post-restart research remains blocked.

Ordering alone cannot replace this prerequisite: SQLite-first with an unadvanced witness leaves a
crash mismatch; witness-first may conservatively burn an unexecuted reservation, but a rollbackable
witness can still restore spent budget. A future protocol must refuse mismatches/uncertain commits and
check before both inference and evaluation; no optimistic reconciliation/refund is acceptable. No
protocol or marker was implemented or claimed proven against crashes/concurrent manipulation here.

### Verification and scope

- Fetch verified main `0851ee48a768348a001f1525f0b30c29584fd821`, tree
  `685fbe8be345d25a4775a8f64ef7b0d62687798c`; branch HEAD/remote initially exactly d7eaa4a above.
- Reused unchanged d7eaa4a code/test evidence: **1177 PASS /0 FAIL /8 intentional real-model SKIP**,
  Release build0 warnings/errors, existing proof-worker composition, budget/replay/concurrency/history
  and `RealAuthorityNeverResumesFromCleanReopenOrConsistentOldRestore` refusal witness.
- No new executable authority backend exists; consequently new marker crash/manipulation tests were
  **not run** and are not asserted passing. No full test/build rerun for this documentation-only delta.
- Publication checks: documentation verifier, working/staged whitespace, staged Gitleaks, exact
  five-document scope and unchanged source/test/build/configuration tree against d7eaa4a.
- Same branch, additive documentation checkpoint only. No BB-132K execution, database opening/write,
  provider/model call, grant creation, deployment, activation, SHADOW or trading.

Finance **RESEARCH /0 SEK /NONE**. STOP after publication for independent review; not mergeable as an
unattended Finance Automation Release. The previous composition correction remains available unchanged.

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
