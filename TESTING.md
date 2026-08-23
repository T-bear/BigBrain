# Testa BigBrain

## BB-097 Family reference validation

Family behavioral regression verifies the dedicated reference composition, absence of a normal-mode `DashboardWidget` wrapper, semantic page heading, settings access, meal tabs, shopping mode and calendar access. Existing Meal Planner, Shopping List, Calendar, AppShell, theme and navigation suites remain authoritative for detailed actions and accessibility. Pixel snapshots are deliberately not used. Separate manual browser evidence must render Obsidian Gold at 390 × 844 repeatedly, then 430 × 932 and approximately 1440 × 900, and compare composition, hierarchy, density, borders, accent use, material depth and dock placement with the supplied mockup. Fixture-only visual runs must be identified as such and never represented as deployed evidence.

BB-092 adds network-free tests for the allowlisted research feature registry, invalid IDs and bounds, deterministic hypothesis fingerprints, explainable complexity, OOS/cost/lineage fail-closed integrity, family attempt visibility, DSR/PBO `NOT_EVALUABLE`, and conservative read-only UI language. Remediation coverage also exercises partial-run evidence retention, persisted restart recovery, cross-key global single-flight, same-key failed-result idempotency, actual 3/5/2 attempt accumulation, bounded/filterable history, count reconciliation and target/horizon consistency. Final evidence-selection cases prove that a second feature/robustness generation wins over lexically earlier stale history, incomplete current families do not fall back, exact market lineage and approved strategy versions are mandatory, and repeated unchanged selection stays deterministic. It reuses BB-081's train/test leakage, cost monotonicity and real expanding-window tests. Research tests never call providers or create PAPER, broker, order, portfolio, LIVE/AUTO or risk-policy state.

BB-093 scheduler tests use injected times/direct orchestrator calls rather than real sleeps. They cover default-disabled startup, due completion, repeated ticks, completed and pre-run restart recovery, no catch-up storm, recovery/data deferral, manual-run busy deferral, current-evidence failure, option bounds, cancellation, bounded APIs, read-only UI wording and unchanged `RESEARCH / 0 SEK / NONE` authority. Readiness remediation additionally covers one-current/rest-stale rejection, full-universe recovery on the same opportunity, stale feature deferral, exact source-lineage mismatch, deterministic readiness, zero experiment evidence on partial acquisition and explicit cross-date supersession of deferred work.

BB-094 resource-governor tests inject deterministic `ISystemMetricsProvider` snapshots; they never depend on workstation load. Coverage includes healthy allow, independent and combined CPU/memory/disk pressure, critical-disk precedence, unavailable/stale/throwing metrics, option bounds, no-run deferral followed by same-opportunity completion, restart-readable compact audit, read-only API/UI state and unchanged `RESEARCH / 0 SEK / NONE` authority. Temperature remains explicitly unsupported and is not faked.

BB-095 operations tests use isolated SQLite stores and injected timestamps. They cover disabled/maintenance semantics, stale enabled scheduling, persistent readiness/resource waits, operational-versus-scientific failure classification, deduplicated incident streaks, success recovery, pre-run interruption, partial experiment preservation, post-run scheduler reconciliation, repeated reconciliation, bounded read APIs, compact metadata backup/restore and unchanged `RESEARCH / 0 SEK / NONE` authority. Hosted-service tests never wait for real scheduled time or contact providers.

BB-096 frontend regression covers the three stable theme IDs, default/fallback and migration aliases, local/server persistence, immediate switching, shared token completeness, five-item primary navigation, secondary AI/Admin access, Family relocation without functional removal, dashboard editing and the existing module interaction suites. Visual review uses temporary, uncommitted browser captures at mobile and desktop sizes; the local design mockup binaries are references and are not test fixtures.

BB-089 adds network-free policy, invariant and adversarial tests for deterministic identity,
version/config validation, ALLOW/REDUCE/DENY/HALT/INSUFFICIENT_DATA, EOD weekend freshness,
clock/lineage/instrument/price/health/volatility/liquidity/exposure failures, client-forged verdicts,
simulated daily loss/drawdown/consecutive losses, immutable idempotence and durable audited halt
recovery. Tests never create an order, broker connection, real portfolio or provider call.

BB-088 adds network-free tests for weekday/provider-window scheduling, healthy weekend/no-provider
cycles, cadence timestamps, bounded read-only status/overview endpoints, repeat outcome evaluation,
actual market breadth, transparent POSITIVE/NEUTRAL/NEGATIVE aggregation, pending/sample honesty,
historical/prospective separation and absence of fake index, portfolio, real-time or order claims.
Runtime provider verification is separate and must not manufacture a weekend session.

BB-087 tests are network-free. They cover deterministic prediction identity, retry idempotence, knowledge cutoff/no-lookahead selection, strategy/parameter/source lineage, explicit horizon, pending-to-evaluated temporal progression, append-only outcomes, clock fail-closed, late-start anti-backfill, bounded/malformed read API, UI pending/sample honesty and absence of mutation/order controls. Full API, Sentinel and frontend regression plus Release/build/documentation/secrets/Compose checks remain required before publication.

BB-086 changes research/planning documentation only because all eight ETF-history candidates
failed closed before acquisition. No runtime contract or fixture changed. Publication verification
therefore runs the complete existing backend/frontend suites and builds, documentation verifier,
secret scan, Compose validation and `git diff --check`; ordinary tests remain network-free.

BB-085 tests are network-free and cover WIKI/EODHD/unknown source classification, deterministic
manifests, atomic/incomplete-state handling, SHA-256 corruption rejection, isolated restore
identity, derived lineage, disk gates, rejected/manual-review cleanup, idempotence and canonical
protection. Runtime drills use only existing local Finance evidence and make no provider calls.

BB-084 tests are network-free and use sanitized CSV/rights fixtures. They cover candidate
transitions, content/schema hashes, promotion PASS/FAIL/UNKNOWN, CSV quoting, ZIP traversal,
OHLCV/duplicate policy, symbol-bounded promotion, cross-source classification and idempotent
re-import. Live WIKI/Zenodo acquisition is a one-time maintenance verification, never an
ordinary test dependency. Long-history robustness also verifies the explicit run-budget cap.

BB-083 tests clean/unclean markers, idempotent recovery, missed-run policies and conservative
interrupted EODHD acquisition without live calls. systemd/reboot remain separate host tests;
CI need not run systemd as PID 1. The verifier prints sanitized states/counts only.

Detta dokument är en kort karta. Auktoritativa procedurer ligger i respektive runbook och modulkontrakt.

## Automatiska tester

Frontend:

```bash
cd src/BigBrain.Web
npm ci
npm test -- --run
npm run build
```

Backend och Sentinel med den lokala .NET 10 SDK:n, från repositoryroten och utan sudo:

```bash
dotnet restore BigBrain.slnx
dotnet build BigBrain.slnx --configuration Release --no-restore
dotnet test BigBrain.slnx --configuration Release --no-build --no-restore
```

Dokumentation och repositoryhygien:

```bash
node scripts/verify-documentation.mjs
git diff --check
docker compose config --quiet
```

## Verifieringskarta

- Dashboard/widgetramverk, persistence, responsiv kontroll, Web-only deployment och rollback: [dashboardrunbook](docs/operations/runbooks/dashboard-widget-framework-verification.md).
- Kalender/Heroma: [modulkontrakt](docs/modules/calendar.md), [import-runbook](docs/operations/runbooks/heroma-schedule-import.md) och [verifieringsrunbook](docs/operations/runbooks/calendar-verification.md). Verkliga Heroma-filer får aldrig användas i automatiska tester; workbooks genereras syntetiskt.
- Media API och read-only providerkontroll: [Media integration verification](docs/operations/runbooks/media-integration-verification.md).
- Smart Shuffle: [Mediamodulen](docs/modules/media.md), [ADR 0011](docs/adr/0011-smart-shuffle-jellyfin-remote-playback-boundary.md) och samma media-runbook.
- Download Control: [säker borttagningsrunbook](docs/operations/runbooks/download-control-safe-removal.md), [ADR 0013](docs/adr/0013-safe-qbittorrent-download-removal-boundary.md) och [ADR 0016](docs/adr/0016-safe-download-control-command-and-partial-batch-boundary.md). Automatiska tester får aldrig mutera riktiga torrents.
- Designsystem och teman: [manuell verifieringsplan](docs/design-system/manual-verification.md), [theme contract](docs/design-system/theme-contract-v1.md) och [Jellyfin-runbook](docs/operations/runbooks/jellyfin-bigbrain-theme.md).
- qBittorrentdiagnostik: [queue/peer-runbook](docs/operations/runbooks/qbittorrent-queue-and-peer-diagnosis.md).
- Aktuell verifieringsstatus: [STATUS](docs/STATUS.md).
- Finance: [testing and validation strategy](docs/architecture/finance/testing-and-validation.md),
  including invariant, simulation, paper, sandbox, failure-injection, reconciliation,
  security, UI/accessibility, performance and soak layers. No Finance test may access a
  live broker or real credentials.
  Market-data tests must prove fail-closed entitlement, immutable provenance, derived
  lineage, correction supersession and retention/deletion scope with synthetic fixtures
  until an exact provider/product is entitlement-cleared, selected and explicitly approved
  for activation; BB-071 evidence alone does not activate a provider.
  BB-075 fail-closed tests additionally assert that the runtime reports the current
  zero-cost entitlement gate rather than superseded State B wording, while every ingestion,
  storage, broker, PAPER and LIVE flag remains false. BB-076 entitlement tests cover
  zero-cost/versioned owner acceptance, capability-specific denial precedence, paid-source
  rejection and fail-closed confirmation/denied evidence.
  BB-077 EODHD tests use documented-shape sanitized JSON fixtures and cover parsing,
  impossible data, 429 retry bounds, symbol mapping, durable SQLite restart/idempotency,
  content-addressed payloads, deterministic exact-revision replay, expiry blocking,
  deletion preview/confirmation/receipt, unrelated-file protection and sanitized API/UI.
  BB-078 adds a network-free runtime-evidence projection test and runs the command against
  the deployed volume before/after restart. It exposes only request/catalog counts, symbols,
  coverage, revision IDs, payload-reference integrity, causal knowledge-time status and replay checksums; never token or
  raw payload content. The single bounded provider acquisition is runtime evidence, not an
  ordinary automated-test dependency.
  BB-079 adds hand-verifiable formula tests for returns, SMA/EMA, momentum, population
  volatility, Wilder RSI/ATR and volume features; edge, warmup/gap, deterministic checksum,
  correction lineage and explicit future-horizon/no-lookahead tests; SQLite reopen/
  idempotency, retention deletion scope, bounded feature API and responsive feature UI.
  Runtime feature builds consume only the existing local memory and must not trigger an
  EODHD request.
  BB-080 adds hand-verifiable next-open/cash/position/whole-share/fee/slippage/exit/final-equity tests; explicit future-bar/feature and same-close no-lookahead proofs; repeated-run identity/checksum/journal/curve determinism; insufficient-cash, warmup, repeated-signal, missing-next-session and retention inventory coverage. Real runs are offline maintenance commands and must not call a provider.
  BB-081 adds chronological 60/40, 70/30 and 80/20 split tests, configurable embargo/no-overlap checks, bounded-grid/isolated-peak tests, higher-cost monotonicity, explicit insufficient train/test/walk-forward evidence, future-feature invisibility, test-mutation isolation, earlier walk-forward stability, evaluation ID/checksum determinism, SQLite retention and read-only UI language coverage. Evaluation commands read local memory only.
  BB-045 policy/provenance tests use only `ExampleData` synthetic fixtures and cover
  exact provider/product scope, missing/unknown/denied/expired policy, persistence,
  post-subscription retention, immutable revision state and raw/derived lineage.
  Canonical-normalization tests additionally cover historical symbol boundaries, MIC venue
  distinction, overlap/unknown mapping rejection, decimal daily OHLCV invariants, raw and
  adjusted classification, dividends, exact split ratios, immutable revision/policy
  references, duplicates/conflicts and repeatable output. No calendar is guessed: future
  expected no-trading days, unknown missing observations and provider gaps remain distinct.
  Session/replay tests use an explicit `Europe/Stockholm` fixture calendar and verify UTC/DST,
  invalid/ambiguous local times, closure/unknown/missing/provider-gap distinctions,
  invalid-observation quarantine, historical ticker resolution, explicit dividends/splits,
  immutable revision binding, no-lookahead, range bounds and deterministic event order.
  Revision-assembly tests verify original/corrected as-of views, inclusive availability,
  immutable old revisions, explicit linear supersession, correction references/cycles,
  deterministic multi-correction order, policy/provenance, corporate-action time,
  inherited session/gap evidence and rejection of future/unavailable membership.
  Acquisition tests require exact multi-use entitlement before adapter invocation and cover
  deterministic requests/batches, synthetic-only identity, unauthorized provider/retention,
  repeated batches, overlapping pagination, correction supersession, journal evidence,
  canonical normalization, explicit provider gaps, immutable revision assembly, repeated
  replay/no-lookahead and absence of secret-bearing contract fields.
  Persistence-foundation tests cover deterministic manifests/checksums, immutable exact
  revision roundtrip, idempotent duplicate append, explicit conflicts, correction lineage,
  gap/action queries, policy-scoped enumeration/deletion receipts, partial-write rejection,
  replay compatibility and no-lookahead. Run the reproducible fixture benchmark with
  `dotnet run --project tools/BigBrain.Finance.PersistenceBenchmarks -c Release --no-build -- --full`;
  it writes only process-scoped temporary files and compares JSONL/SQLite without external IO.
  Live-observation tests use only an injected synthetic feed and explicit UTC evidence.
  They cover event/provider/received/knowledge causality, honest delay classification,
  deterministic and out-of-order delivery, duplicate/correction preservation, missing/
  outage/session events, fail-closed entitlement, immutable versioned prediction/outcome,
  cost-aware prospective metrics, no-lookahead and absence of broker/order/secret surfaces.
  The 2026-08-11 combined historical/live provider gate is documentation-only. It changes
  no domain/runtime code and adds no .NET test delta; source links, scorecard evidence and
  fail-closed language are covered by documentation verification and `git diff --check`.
  The BB-071 resolution is likewise documentation-only: the existing 376-test synthetic
  baseline is rerun, while no provider/network acceptance test is authorized.
  BB-082 follows the legitimate blocked path and changes no executable contract. Provider
  research is verified through dated primary-source links, bounded request accounting,
  documentation link/BB-ID validation and `git diff --check`; no live market-data test,
  fixture parser test or runtime deployment is applicable because no adapter/data exists.
  BB-074 tests prove fail-closed RESEARCH/no-provider/no-order API state and deterministic
  synthetic mapping. Web tests cover navigation, no-real-money and entitlement warnings,
  empty/synthetic/stale/gap/memory/chart states, native keyboard controls and no trade UI.
  Sprint 1 testar decimalprecision, invariants, UTC, provider-neutral fixture-data,
  strategy-/orderseparation, fail-closed risk/policy, NO TRADE/REJECTED-journal,
  korrelationskedja och att endast PAPER kan skapa ett lokalt paper-intent.

## Live-säkerhetsregel

Automatiska tester använder fakes/mocks och får aldrig anropa live write-endpoints. De får inte starta Jellyfin-uppspelning, ta bort eller ändra torrents, mutera Sonarr/Radarr/Prowlarr, ändra media, starta om externa tjänster eller använda riktiga credentials. Verkliga mutationer får endast ske genom dokumenterat UI-flöde efter uttrycklig användaråtgärd och separat scope.

Media har både read- och smala write-kontrakt. Påståendet att Media saknar POST/write-endpoints är historiskt och gäller inte dagens implementation. Läs [Mediamodulen](docs/modules/media.md) för aktuella gränser.
# BB-090 test additions

Network-free fixtures and tests cover macro release/knowledge cutoffs, vintage selection, forward-fill only after knowledge time, migration restart/idempotence, New York DST, regular holidays, weekends and bounded exceptional closures. Dataset/risk regression covers adjusted semantics, provider-aware promotion, typed insufficient/warmup categories and exact prediction-risk lineage. Live FRED acquisition is a bounded maintenance drill and never an automated-test dependency.

BB-090 closure adds empty/legacy/interrupted/concurrent migration coverage, rejected Macro quarantine candidates, strict evidence-class selection, Juneteenth and exact DST transition dates, immutable invalid WIKI adjusted evidence, configurable provider-neutral risk policies and deterministic multi-verdict frontend aggregation. Finalization additionally verifies the official FRED JSON `output_type=2` column schema and rejects non-vintage response shapes. The 2026-08-16 finalization regression passed 440 API, 32 Sentinel and 113 frontend tests. Production migration drills must compare `finance-evidence-counts` before/after; secrets are never test output.

# BB-091 test additions

Network-free sanitized Riksbank JSON and ECB SDMX CSV fixtures cover selected-series identity, policy/FX values, explicit base/quote semantics, malformed artifacts, rights denial, quarantine rejection, exact-artifact idempotence, cross-provider EUR/SEK tolerances and region/evidence-class as-of isolation. Live official acquisition is maintenance evidence only. Current-history bootstrap remains revised-history exploratory.
