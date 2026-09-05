# BB-130 baseline architecture and code review

## Metadata

- Date: 2026-09-05
- Baseline: `7fd89a5ccbe9be82699dc70950f461d3fbb6589c`
- Scope: sanitized owner-supplied architect findings, rechecked against source;
  BB-130A continuity and reconciliation. This is not a new independent external audit.
- Related plan: [BB-130A–D](../../architecture/bb-130-stabilization.md)

## Status

BB-130A is complete, documentation verified, published and CI verified at checkpoint 58563475bfed7bdddece87d11895adeec83d9c30. BB-130B measurements and
BB-130C/D implementation are not complete. No deployment or new owner UX acceptance
is part of A. Repository inspection is distinct from runtime or penetration testing.

## Strengths to preserve

The modular monolith has explicit integration adapters and a separate Sentinel
trust boundary. Finance has deterministic immutable evidence, exact lineage,
fail-closed entitlement/risk/research gates and purpose-specific research eligibility.
SQLite is an established shared physical Finance store; replacing it is not justified.
Backend nullable/warnings-as-errors/analyzers and substantial regression tests provide
useful characterization. BB-128B/C preserves usable cached Finance and has explicit
physical-owner evidence. The report system already supports sanitized durable knowledge.

## Findings and remediation

Severity denotes change risk/priority, not a demonstrated exploit or measured latency.

| Finding | Severity and evidence | BB-130 direction |
| --- | --- | --- |
| FinanceObservation owns unrelated state and rendering | High refactor risk: observation/cache/retry, selection/chart, overview/risk/autonomous and research detail effects share one component | C: cohesive hooks/panels while preserving B/C cache, retry, safety and accessibility contracts |
| FinanceDatasetIntakeStore mixes pipeline and storage | High correctness risk: download, quarantine, CSV/XLSX parsing, validation, promotion, lifecycle/catalog and campaign partials | C: extract existing responsibilities and characterize fail-closed behavior first |
| EodhdMarketMemory owns provider-neutral research persistence | High ownership risk: feature/backtest/robustness/risk/research partial classes use provider-specific owner | C: clarify Finance storage ownership, retain single DB and exact lineage |
| Oversized Program composition | Medium maintenance risk: explicit family/media/Finance/options/workers all registered in API Program | C: explicit registration extensions preserving lifetime/order |
| Split structural schema authority | High migration risk: migrator versions 1/90–93 coexist with store CREATE/ALTER operations | C: one structural authority; isolated legacy/restart/rollback/concurrency tests |
| Uneven quality gates | Medium: Web package has test/build, no lint/format; CI already runs backend/frontend/docs/full-history secrets | D: minimal deterministic gates, no unrelated mass rewrite |
| Documentation drift | High continuity risk: BB-128C accepted header conflicts with pending backlog/report/test wording; ADR 0005 index differs from body | A: source/evidence-based reconciliation and canonical read order |
| Mixed architecture lifecycle | High decision risk: historical Host Agent/PostgreSQL/OIDC/early-sprint text reads as current implementation | A: explicit accepted/current/historical/future scopes without erasing history |
| Application auth boundary incomplete | High future-authority prerequisite: no general AddAuthentication/UseAuthentication/RequireAuthorization wiring in API Program | A: document known prerequisite; preserve existing confirmation/Finance/Sentinel gates |
| Loading bursts | Medium performance hypothesis: App starts four reads and polls system at 5 seconds; Finance adds five initial reads | B: instrument and measure before optimizing; no claimed speedup yet |

Baseline code references: `src/BigBrain.Web/src/App.tsx`,
`src/BigBrain.Web/src/finance/FinanceObservation.tsx`,
`src/BigBrain.Api/Program.cs`, `src/BigBrain.Api/Finance/FinanceDatasetIntake.cs`,
`FinanceResearchDatasets.cs`, `FinanceResearchCampaigns.cs`, `EodhdFinanceMarketData.cs`,
`FinanceFeatureStore.cs`, `FinanceBacktestStore.cs`, `FinanceRobustnessStore.cs`,
`FinanceRiskEngine.cs` and `FinanceSchemaMigrations.cs`.

The source graph is not a runtime measurement: modules, Docker inventory, recovery
and system overview start globally. Finance observation, overview, risk status,
risk evaluations and autonomous research start on Finance mount. Features, datasets,
backtests, robustness, backups, shadow, scheduler, governor and operations already
wait for Details & research; selected catalog entries can cause dependent detail reads.
B must measure overlap, bytes and timing for Home/Finance and relevant Media behavior.

## Sentinel reconciliation decision and limits

Owner/architect decision during BB-130A: reconcile only verified current System Metrics
behavior; do not accept new security/privilege semantics. ADR 0005 retains Proposed
for full conformance, documents current code, and preserves the original proposal as
historical requirements. ADR 0003 remains the accepted frozen authority, including
ADR 0002's inherited exclusive-access/security principles.

The pre-existing local ADR revision proposed moving PKI lifecycle, normative
compatibility, durable audit, supply-chain and packaging evidence from implementation
prerequisites to release gates; making the signed overlay optional; and explicitly
permitting an in-process reader with a conditional helper requirement. Those proposals
are not accepted through this reconciliation. Their scope is preserved here for
future BB-009 review rather than lost or silently published as authority.

Existing code has pinned mTLS over a Unix socket, ECDSA proof/node/expiry/replay checks,
fixed request sections/fields/selectors and read-only metric collection. Identified
gaps remain: container delivery versus native-service wording; in-process collection
without full privilege-model acceptance; signed overlay lifecycle; explicit certificate
validity/EKU and lifecycle negative evidence; comprehensive Control Plane response
validation and bounds; and audit logging that lacks the stronger identity/policy/timing
guarantees and logs a fixed outcome before completion. Tests passing does not close
these gaps. No live exploit, credential probe or runtime security reconfiguration was
performed. The existing BB-009 and security/pentest items own follow-up.

## Evidence

- Fetched origin and verified HEAD/origin/main both equal the baseline above.
- Read AGENTS and recovery, applicable architecture/ADRs, status/backlog, report
  conventions, current Finance contracts/evidence, source and existing test maps.
- `dotnet test tests/BigBrain.Sentinel.Tests/BigBrain.Sentinel.Tests.csproj --configuration Release --no-restore`:
  passed 32/32 on 2026-09-05. Scope includes integration with the existing Control Plane
  client; it does not establish every stronger historical security requirement.
- Control Plane provider: dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~SentinelSystemMetricsProviderTests: passed 2/2. First attempt hit sandbox MSBuild named-pipe permission denial; approved rerun passed.
- node scripts/verify-documentation.mjs: passed 220 Markdown files / 90 unique backlog IDs.
- docker compose config --quiet and git diff --check: passed.
- No separate full local application build/regression is needed for this documentation-only A change; CI still runs all existing jobs. Staged patch scan via git diff --cached piped to docker run --rm -i --network none zricethezav/gitleaks:v8.28.0 stdin --redact --no-banner passed with no leaks. Actions run 33982397667 passed backend restore/Release build/full tests, frontend install/full tests/build, documentation and full-history secrets. origin/main matched HEAD before and after push.
- BB-128C explicit owner acceptance already exists in the published report/header and
  STATUS: after the single-loader micro-fix the owner stated `Jag är nöjd`. A reconciles
  stale pending language; it does not invent another UX approval.
- BB-129A remains the published bounded campaign result: 24 attempts, no robust
  candidates, honest ineligible/insufficient-evidence outcomes. No rerun or retuning.

## Changes

A adds `docs/START-HERE.md` and the ordered BB-130 plan, references continuity and
adaptive reasoning from AGENTS, reconciles ADR/index/architecture and current
status/backlog/roadmap/testing/report text. Existing ROADMAP remains the product
direction authority; Finance master roadmap retains its domain gates. Recovery stays
in its single canonical file. No code, schema, scientific calculation, provider,
runtime configuration or UI behavior changes in A.

## Security

Detta är en sanerad GitHub-version. No credentials, private addresses, private
identities, raw sensitive logs, provider payloads or sensitive machine paths are included.
Application authentication/authorization is a prerequisite before broker/trading,
Docker/camera control, high-impact Home Assistant operations or broader network
exposure. Existing media confirmation tokens and dashboard permission metadata do
not establish user identity. No OIDC implementation is introduced by BB-130A.
API/Web receive no Docker socket, shell or new host access.

## Remaining work

A validation/publication is complete. B measurement/improvement, C ownership refactors
and D quality gates remain. Scientific sample/lineage limitations, general auth,
Sentinel conformance, physical appliance recovery gates and domain roadmap decisions
are not solved by documentation. No Finance expansion or high-authority activation.

## Resumption

Start at [continuity](../../START-HERE.md), verify published main and working tree,
then follow the [BB-130 plan](../../architecture/bb-130-stabilization.md). Once A is
published, measure B's baseline before editing triggers. Interrupted unpublished work
is recorded only in [recovery](../../operations/codex-recovery.md).
