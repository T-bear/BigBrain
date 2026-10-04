# BB-132F1 — Controlled Activation & Deployment Gate

Detta är en sanerad GitHub-version. No credentials, raw provider payloads, private host paths or model content.

## Metadata

Baseline: `3a332c476ce6acf7007e463791ba416530c15302`, tree
`43380caec0119a22652b07e0614ccc3eef328639`, verified directly after fetch.
Branch: `bb-132f1/controlled-activation-gate`.
Publication subject: `review: implement BB-132F1 controlled activation gate`.
Resolve that unique commit and its tree from Git/GitHub; publication is not owner acceptance.

## Status

**MERGE CANDIDATE / IMPLEMENTED / AUTOMATICALLY VERIFIED / REVIEW CANDIDATE ONLY**.
No deployment, restart, service repair, credential configuration, provider request, model,
shadow evaluation, scientific grant mutation or trading occurred. Finance **RESEARCH / 0 SEK / NONE**.
The only prior tracked edit was the52-line operationalization handoff at the top of recovery.
Its exact removal reconstructed accepted HEAD byte-for-byte before branch creation, as explicitly
instructed. No other local file was discarded. Unrelated mockups/ADR0006–0009 are excluded.

## Changes

Operationalization found no accepted one-shot entry, no Compose Alpaca pass-through and no
verifiable running API source revision. This checkpoint addresses those three responsibilities
only. [Canonical operator, configuration, later activation and failure contract](../../../operations/runbooks/finance-controlled-acquisition.md).

- `finance-alpaca-daily-once <configured-id> <yyyy-MM-dd>` uses the existing early maintenance
  dispatch and exits before web/lifecycle/worker startup. The reserved Alpaca verb prefix prevents
  unknown maintenance commands falling through to host startup. No public mutation endpoint.
- Shared F configuration validation and guarded clock are reused. The one-shot requires disabled
  unattended runtime, configured transport/credentials/current policy and one reviewed AAPL/MSFT
  Equity/USD/XNAS mapping. SPY fails; no production universe is selected. One explicit prior New
  York session only; no current-day acceptance, discovery, looping, retries or scheduling.
- Existing recovery owner adds a standalone **read-only** maintenance prerequisite check for
  synchronized marker, existing SQLite integrity and critical disk space. It does not instantiate
  the appliance coordinator, create lifecycle sessions, mark shutdown clean or repair systemd.
  This gate requires the marker instead of the hosted plausible-year fallback. The failed-service
  observation from the earlier operational handoff remains a separate operator readiness issue;
  F1 does not assert its cause or repair/current health.
- Existing Finance owner adds read-only schema/watermark/bounded receipt-integrity preflight;
  existing acquisition and transaction remain unchanged. A local exclusive file handle prevents
  overlapping maintenance processes using the same Finance path; no new scientific cursor/store.
  Acquisition routes once through the unchanged Alpaca adapter and `ReobserveDailyAsync`.
- Same New/Duplicate/Revision semantics, original duplicate knowledge, later immutable revisions,
  checksum/provenance conflict rejection and before-commit cancellation. SIGINT/SIGTERM propagate.
  Receipt-only successful output; finite stage/failure category on failure, never exception/body text.
  An uncertain post-commit failure is retained and never automatically retried/refunded/deleted.
- Compose forwards external settings, no actual values installed. A bounded JSON deployment field
  creates the same instrument DTOs:1–4entries,16384characters/depth4, no unknown/duplicate properties,
  no ambiguity with indexed configuration. Credentials do not enable runtime.
- API assembly metadata and OCI label receive a trusted build-supplied revision; missing/invalid
  revision is UNKNOWN. `system-build-identity` reads the compiled value before config/host init.
  The build-only helper checks exact HEAD/origin-main and clean tracked state, archives the exact
  commit for Docker input and excludes ignored/untracked local inputs. Local secrets are not used
  for build interpolation. No HTTP system surface or immutable-identity override via environment.

No D/E scientific contracts, adapter HTTP semantics, schema version, finite/reasoner contracts,
Sentinel contract, grant, result engine or trading path changed. Existing ADRs remain unchanged.
No new architecture decision is asserted Accepted by implementation.

## Changed-file responsibility

- `.dockerignore`: exclude local environment/identity material from build context.
- `compose.yaml`: external Finance pass-through and trusted build revision argument only.
- `scripts/build-bigbrain-api.sh`: exact accepted-main archive build; no deployment.
- `src/BigBrain.Api/BigBrain.Api.csproj`, `src/BigBrain.Api/Dockerfile`, `src/BigBrain.Api/BuildRevision.cs`:
  compiled/OCI identity and honest UNKNOWN.
- `src/BigBrain.Api/Program.cs`: early finite maintenance/identity dispatch, shared configuration binding.
- `src/BigBrain.Api/Finance/FinanceObservationMaintenanceCommand.cs`: bounded command and sanitized output.
- `src/BigBrain.Api/Finance/FinanceObservationRuntime.cs`: shared unchanged validation/clock,
  bounded external mapping representation; unattended behavior remains independently disabled.
- `src/BigBrain.Api/Finance/FinanceMarketObservations.cs`: read-only maintenance evidence preflight,
  unchanged receipt acquisition/revision transactions.
- `src/BigBrain.Api/SystemRecovery/SystemRecovery.cs`: standalone read-only local prerequisites;
  existing hosted coordinator behavior unchanged.
- `tests/BigBrain.Api.Tests/FinanceObservationMaintenanceTests.cs`: deterministic complete command path.
- `tests/deployment/verify-finance-activation.py`: actual Compose parse with isolated environment,
  build-helper tests using disposable Git and Docker double; no daemon/network/image build.
- `docs/operations/runbooks/finance-controlled-acquisition.md`: authoritative operator contract.
- `docs/operations/deployment/README.md`, `docs/architecture/finance/market-data-memory-and-provenance.md`:
  discovery links and concise canonical scope.
- `docs/STATUS.md`, `docs/BACKLOG.md`: candidate state and deferred real activation.
- `docs/operations/codex-recovery.md`: current publication/continuity handoff.
- this report and `docs/reports/REPORT-CATALOG.md`: sanitized evidence and exact Git resolution.

## Evidence

Verification date2026-10-04; model/provider-free.

Focused command-path tests cover disabled-runtime requirement, absent/malformed keys, unknown/
invalid/SPY/mismatched/effective-date mappings, bounded/duplicate/unknown JSON, policy/deletion,
invalid/weekend/holiday/current/future day, unknown revision/command, extra arguments, exactly one
request, failure/no retry, New/Duplicate/Revision, signal-token cancellation/before-commit rollback,
uncertain committed evidence, concurrent exclusion, missing stores/clock marker/critical disk/schema,
clock regression/reopen, checksum corruption, mapping provenance conflict and sanitized output.
Actual accepted fixture transport/parser/Finance storage is used; fixtures remain labelled fixtures.
Reopen and sealed before/after knowledge queries prove noninterference after a later revision.
Science/shadow/backtest/robustness tables stay empty; the authority chain has no model or engine call.

Commands/results:

- `dotnet restore BigBrain.slnx`: PASS; dependencies already restored.
- `dotnet build BigBrain.slnx -c Release --no-restore`: PASS,0warnings/0errors.
- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-build --no-restore`
  with filter `FinanceObservationMaintenanceTests|FinanceObservationRuntimeTests|FinanceMarketObservationTests|AlpacaDailyMarketObservationTests|ProspectiveDailyShadowTests|TwelveDataMarketObservationTests`
  (each term prefixed `FullyQualifiedName~`): **185 PASS,0failed,0skipped**.
- Initial F1/F-only run:95 PASS. Broader first API run1109 PASS/8controlled-model SKIP. A subsequent
  final safety change reserved the Alpaca maintenance prefix; its additional negative test is included
  in final focused185. Final API verification after that code change is recorded below.
- `python3 tests/deployment/verify-finance-activation.py`: PASS. Uses `/dev/null` instead of real
  `.env`; captures resolved fixture-only configuration without printing it. A Docker double proves
  exact accepted build argument/context, ignored/untracked-secret exclusion, dirty/mismatch refusal
  and absence of start/deployment operations. `bash -n scripts/build-bigbrain-api.sh`: PASS.
- Isolated `dotnet publish ... -p:BigBrainBuildRevision=1111111111111111111111111111111111111111`
  into a temporary output followed by `system-build-identity` returned exactly that **fixture SHA**.
  Runtime `BigBrainBuildRevision`/`GitRevision` environment overrides did not change it. Default build
  process returned `revision=UNKNOWN`. Neither command initializes host/Finance. Actual invalid
  `finance-alpaca-unknown` process exited1 with sanitized Configuration failure and no host startup.
- Initial sandbox denied MSBuild local IPC; rerun with authorized local IPC succeeded. Early new-code
  analyzer failures (serializer-option caching and test cancellation arguments) were corrected before
  the passing tests above. No failing scientific test, provider retry or parser relaxation was hidden.
- No frontend/Sentinel test suites: those implementations/contracts are untouched. Release solution
  still builds them. No migration tests added: schema/migrations unchanged; D/E persistence/reopen/
  lineage and full API coverage apply. No Docker daemon build/deployment needed for fixture config checks.

Final code: **1110 API PASS,0failed,8 deliberately SKIPPED real-model tests** (1118total).
`dotnet format BigBrain.slnx --verify-no-changes --no-restore`: PASS, no changes.
`git diff --check` and staged scope check: PASS;21 intended files only.
Gitleaks8.28.0 `git --pre-commit --staged --redact`: PASS, no leaks in exact candidate content.
Documentation verifier initially caught missing report schema headings/sanitization notice; the
report was corrected, with no implementation change. Final `node scripts/verify-documentation.mjs`: **PASS,265 Markdown files/91 unique backlog IDs**; staged secrets/diff checks PASS. Sandbox Node/Git invocation needed
escalated execution; no check was bypassed. Full tests were not rerun for documentation-only edits.

## Security

Only trusted external configuration selects secrets and reviewed mappings; command arguments cannot
supply credentials, provider URLs, process tools or configuration overrides. No real secrets were
read/configured/printed by this checkpoint. Fixed adapter transport, receive limits and scientific
identity rules remain intact. A clean exact-SHA archive is the build input; no runtime revision override.
Single-host maintenance lock and finite output do not confer runtime activation or trading authority.
No production service/container was touched; fixture-only Compose/build checks contact no daemon.

## Remaining work

This proves the capability using deterministic fixtures, not real Alpaca entitlement/data or deployed
service health. Later owner authorization is still required for deployment/recreation, reviewed current
mapping/policy, local credentials and exactly one real call. No first-call allowance is consumed by F1.
Image label/assembly agreement assumes a trusted build process; it does not attest a malicious builder.
The operator must compare a known revision to exact accepted main; UNKNOWN is rejected automatically.
The standalone marker check is not continuous NTP attestation or certification of the appliance unit.
Clock regression and existing persisted watermarks remain final guards. Existing capacity/retention
uncertainty from D/E/F remains. Over1000receipts refuses bounded maintenance integrity projection.

Local same-path locking is not distributed multi-host coordination and cannot arbitrate a separately
misconfigured enabled host. Use the deployed container's unchanged environment and keep unattended
runtime disabled. Repeated manual command execution requires fresh owner authority; there is no new
persistent authorization-token ledger. Failure after possible commit needs evidence review, no retry.
No operator may treat test fixtures as actual prospective market evidence or infer profit/trading rights.

## Resumption

Next: STOP after publication for independent review of this exact candidate. Do not merge, deploy,
restart, configure live keys/universe, call Alpaca/Qwen, activate unattended collection, run SHADOW,
renew grants, trade or start another checkpoint.
