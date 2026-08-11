# Testa BigBrain

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
  BB-074 tests prove fail-closed RESEARCH/no-provider/no-order API state and deterministic
  synthetic mapping. Web tests cover navigation, no-real-money and entitlement warnings,
  empty/synthetic/stale/gap/memory/chart states, native keyboard controls and no trade UI.
  Sprint 1 testar decimalprecision, invariants, UTC, provider-neutral fixture-data,
  strategy-/orderseparation, fail-closed risk/policy, NO TRADE/REJECTED-journal,
  korrelationskedja och att endast PAPER kan skapa ett lokalt paper-intent.

## Live-säkerhetsregel

Automatiska tester använder fakes/mocks och får aldrig anropa live write-endpoints. De får inte starta Jellyfin-uppspelning, ta bort eller ändra torrents, mutera Sonarr/Radarr/Prowlarr, ändra media, starta om externa tjänster eller använda riktiga credentials. Verkliga mutationer får endast ske genom dokumenterat UI-flöde efter uttrycklig användaråtgärd och separat scope.

Media har både read- och smala write-kontrakt. Påståendet att Media saknar POST/write-endpoints är historiskt och gäller inte dagens implementation. Läs [Mediamodulen](docs/modules/media.md) för aktuella gränser.
