# BigBrain Status

BB-089 Hard Risk Engine foundation is implemented, deployed and restart-verified on 2026-08-16 from
baseline `7aa202bc88142399cd9a70085cec5bf4f23db16f`. Policy `research-eod-v1` centrally enforces
fail-closed RESEARCH-only data/time/lineage/health, universe/price/move, rolling-volatility,
volume-liquidity, hypothetical sizing/exposure and simulated loss/drawdown/loss-streak rules.
Evaluations are immutable/idempotent; durable system halt and explicit recovery transitions are
audited across reopen. Four risk endpoints and compact/detailed UI are read-only. Sunday runtime
preserved 24 valid pending, zero outcomes, 24 invalidated and zero retroactive risk evaluations;
cadence logged `no-provider-check`. Full local regression passed (423 API, 32 Sentinel, 111 Web),
both builds, docs, Compose and whitespace. Implementation commit `f8f2e1f871e5532ff906092545e8706d642b941b`
passed GitHub Actions run #48 (documentation, frontend, secrets and backend). Spread, sector and aggregate
portfolio remain explicitly not evaluable. Budget is 0 SEK; no PAPER, broker, order, LIVE/AUTO or
self-learning exists.

BB-088 is implemented, deployed and runtime/restart-verified on 2026-08-15 from baseline
`0ab8c7470a1b16e399728ea564cfeb34962d3069`. Implementation commit
`8653c44fd05fc76460e460ec5c0fef8d4cc9e7ed` passed GitHub Actions run #46: documentation,
frontend, secrets and backend all succeeded. The
Saturday drill correctly made no provider request and preserved latest session 2026-08-14, 24
valid pending predictions, zero outcomes and 24 invalidated audit rows across two restarts. Cadence
health/clock were healthy; no duplicate prediction/outcome appeared. Actual overview breadth was
3 up/5 down across eight tracked instruments, `BOOTSTRAPPING`, with no graph. One WIKI-eligible
backup and EODHD `SubscriptionOnly/restricted` classification survived. Local verification passed
412 API tests, 32 Sentinel tests, 111 frontend tests, both builds, docs and Compose. Finance remains
`RESEARCH`; no PAPER, broker, order, LIVE/AUTO, self-learning or paid service was added.

BB-087 is implemented, deployed and bounded-runtime/restart verified on 2026-08-15 from baseline `31156ec8e9780dc6622891cb4ba46050c302a6f6`. Eight current EODHD instruments at session 2026-08-14 produced 24 causally source-pinned pending predictions across three unchanged strategies; no outcome is yet knowable. The drill detected 24 earlier rows pinned to a WIKI feature revision, preserved them as `INVALIDATED`, then enforced exact source-revision membership and created correct evidence on `feature-c204a8133abaf8a2`. Restart retained 48 audit rows and did not duplicate the 24 valid predictions. Published implementation commit `86e92c431cffc1db37684bcfba5a2a785d7b05ad` passed GitHub Actions run #44 on 2026-08-15: documentation, frontend, secrets and backend all succeeded. Finance remains `RESEARCH`; there is no PAPER, broker, order, LIVE/AUTO or self-learning authority and no paid service.

BB-086 completed as a legitimate fail-closed research result on 2026-08-15 from baseline
`aee00db`. The existing WIKI artifact has zero SPY/QQQ/IWM rows. Eight bounded candidates were
reviewed; no source passed both underlying-rights/provenance and supported-access gates. Stooq's
single normal SPY request returned verification HTML, not CSV, and no challenge was solved.
No artifact, observation, revision, feature, backtest, robustness or backup was created; existing
WIKI/EODHD evidence is untouched. The approved future security/pentest baseline is now recorded
as a mandatory pre-LIVE roadmap gate. GitHub Actions run #42 passed frontend, backend, secrets and
documentation for the BB-086 publication. Finance remains `RESEARCH` at 0 SEK.

BB-085 is implemented, deployed, bounded-runtime-verified and restart-verified 2026-08-15
from baseline `69aad9d`.
`finance-provider-backup-v1` exports only provider-eligible revisions and exact derived
lineage through atomic staging plus deterministic SHA-256 manifests. WIKI revision
`wiki-5713d7dccfa38f56` restored identically in isolated staging; corruption was rejected.
EODHD remains `SubscriptionOnly/DeleteAtSubscriptionEnd` and outside indefinite backup.
Rejected cleanup retains manifest/hash evidence, manual review is conservative and canonical
promoted observations are not cleanup targets. API/UI are read-only; Finance remains RESEARCH.

BB-084 is implemented, deployed and runtime-verified 2026-08-15 from baseline `d9eadbb3`.
Finance now has a fail-closed quarantine/manifest/validation/promotion pipeline. WIKI candidate
`dd5127…a43e` passed and promoted 3,722 AAPL/MSFT/JPM/XOM/JNJ rows as
`wiki-5713d7dccfa38f56`; Zenodo DOI 10.5281/zenodo.20192822 remains manual review with zero
canonical rows. Feature `feature-3833eb92bb641e51` and three robustness v3 evaluations are
deployed; 523/175 train/test sessions resolve the old insufficiency verdict to MIXED/MIXED/
FRAGILE without creating trading authority. EODHD memory and retention remain independent.

BB-083 resilience baseline 2026-08-12 is deployed at Compose level. Durable lifecycle,
clean/unclean state, storage/clock/disk gates, recovery API/UI, Sentinel readiness and stop
grace are active. API PID-1 crash recovered as `UNCLEAN`; Finance requests remained 16 with
no duplicate evidence. Host unit install, Docker restart and reboot are blocked by interactive
sudo; physical power test is pending. Finance remains `RESEARCH`; no shutdown/trading API.

- Senast uppdaterad: 2026-08-12 (Europe/Stockholm)
- Verifierad mot commit: `eaa3a0446356316dc21b2fee3e0e0a2b30c5211c` (BB-083 resilience
  documentation baseline on `origin/main`).
- Runtime senast verifierad: 2026-08-12 (BB-083 container-runtime: Compose deployment and
  API PID-1 crash recovery verified; host install, Docker-daemon restart/reboot and physical
  power-cycle are not verified)

Status skiljer uttryckligen mellan implementerat, automatiskt verifierat, deployat och manuellt verifierat. Detaljerad evidens finns i [rapportkatalogen](reports/REPORT-CATALOG.md).

## Sprint 1 – slutförd

- Status: Slutförd 2026-08-07. Sprint 1-fixarna och remediationen efter deploymentregressionen är implementerade, automatiskt verifierade, deployade och manuellt godkända av produktägaren.
- Root cause: en ren deployexport saknade root-runtimekonfigurationen och den ännu opublicerade kalendermodulen. Integrationsvärden blev tomma och kalenderns befintliga volume monterades inte. Separat lagrades tema endast per klients `localStorage`.
- Data: kalenderdatabasen raderades inte och verifierades med `integrity=ok`; 39 händelser och 2 importer fanns kvar före återanslutning. Kalender-API läser åter data.
- Remediation: API/Web använder åter avsedd konfiguration och persistent storage. Ett globalt allowlistat Theme API, en persistent settingsvolym och ThemeProvider synkroniserar tema mellan klienter.
- Automatiskt verifierat: 99 frontendtester, production build, 207 API-tester, 32 Sentinel-tester, healthy API/Web, kalender- och integrationskontrakt samt dokumentationsgrindar.
- Manuell verifiering: godkänd av produktägaren för Dashboardinställningar, Download Control, Shopping List, Ofta köpt, kalenderåterställning, integrationer och delat persistent tema. BB-028, BB-030–BB-032, BB-034–BB-035 och BB-037–BB-038 är klara.
- Incidentrapport: [Sprint 1 deployment regression](reports/incidents/sprint-1-deployment-regression-20260807.md).

## Dashboard Views och Widget Framework Phase 1

- Status: Implementerat, automatiskt verifierat, deployat och manuellt visuellt godkänt av produktägaren.
- Implementerat: Hem, Media, AI och Admin; registerbaserade widgets; widgetbibliotek; redigeringsläge; synlighet, ordning, drag-and-drop, knappbaserad flytt och kollapsning; versionerad lokal persistens med säker fallback.
- Deployat: BigBrain Web. Ingen backend-, Compose- eller runtimekonfigurationsändring krävdes.
- Manuellt verifierat: mobilnavigationen, de fyra vyerna och layouten godkändes 2026-08-04.
- Kända begränsningar: profilsynkronisering, delade dashboards, mallar, roller, serverpersistens och fria storlekar är framtida arbete i BB-027.
- Sprint 1-buggfix 2026-08-07: Dashboardinställningar samlar Tema, redigeringsläge och widgetbibliotek bakom ett tillgängligt kugghjul. Fixen är automatiskt testad, production-byggd, headless-verifierad, deployad och manuellt godkänd på mobil och desktop.
- Dokument: [arkitektur](architecture/dashboard-widget-framework.md), [ADR 0014](adr/0014-dashboard-views-and-widget-framework.md), [runbook](operations/runbooks/dashboard-widget-framework-verification.md), [rapporter](reports/features/dashboard/).

## Kalender och Heroma-import

- Status: Implementerat, automatiskt verifierat, deployat och manuellt godkänt. Kalenderdata och importhistorik bevarades genom deploymentincidenten och är åter verifierade efter remediation.
- Implementerat: server-side `.xlsx`-parser för verifierad svensk Heroma-månadskalender, flera filer med partiell framgång, kortlivad preview, transaktionell confirm, exakt dubblettskydd, Replace/Merge/Cancel, konfliktstopp, SQLite-persistens, importhistorik, veckovy på Hem och responsiv månadsvy med direkt synliga tider.
- Formatverifiering: den privata lokala samplefilens signatur, sheet/dimensioner, calendar-grid, tidsmönster, specialetiketter, merge och dokumentegenskaper analyserades utan publicering av råvärden. Originalet importerades inte och finns inte i Git.
- Automatiskt verifierat: syntetisk workbook-parser, dag/kväll, specialtyper, flera intervall, okänd/ledig, overnight, ogiltig struktur, persistence, duplicate, replace/merge conflict samt frontendens vecka, månad, navigation, flerfils-preview, Escape och fokusåterställning.
- Deployat: BigBrain API och Web är healthy. Kalenderns read-only week/import-history-endpoints svarar HTTP 200 och läser befintlig persistent data.
- Manuellt verifierat: Produktägaren har godkänt aktuell arbetsvecka, importerad schemadata, importhistorik och mobil layout efter remediationen.
- Dokument: [Kalender](modules/calendar.md), [Heroma-kunskap](knowledge/heroma-schedule-import.md), [ADR 0015](adr/0015-calendar-heroma-import-boundary.md) och [verifieringsrunbook](operations/runbooks/calendar-verification.md).

## Matlista och Inköpslista

- Status: Implementerade, deployade och manuellt runtimeverifierade.
- Implementerat: familjefokuserade mobilflöden för veckoplanering och inköp.
- Sprint 1-buggfix 2026-08-07: konservativ skrivvariantskontroll och uttryckligt `Lägg till ändå` är implementerade för nya varor; ”Ofta köpt” använder läsbara semantiska färger i samtliga dokumenterade states. Fixarna är automatiskt testade, production-byggda, headless-verifierade, deployade och manuellt godkända.
- Kända begränsningar: BB-001–BB-003 och BB-016 återstår separat. Gemensam realtidssynk är endast planerad i BB-036.
- Dokument: [Matlista](knowledge/meal-planner.md) och [Inköpslista](knowledge/shopping-list.md).

## Media

- Status: Media Search, Media Jobs och dashboardöversikt är implementerade och deployade.
- Implementerat: normaliserade läsflöden och smala, uttryckligt bekräftade mutationsgränser via adapters; inga generella externa proxyer.
- Kända begränsningar: externa leverantörers tillgänglighet och versionskontrakt verifieras separat.
- Dokument: [modulkontrakt](modules/media.md), [media-stack](knowledge/media-stack.md), [verifieringsrunbook](operations/runbooks/media-integration-verification.md).

### Smart Shuffle

- Status: Implementerad, publicerad och deployad.
- Automatiskt verifierat: backend- och frontendkontrakt, säker sessionsmappning och playbackstatus.
- Manuellt verifierat: ett uttryckligt knapptryck i BigBrain startade rätt avsnitt på vald Samsung Tizen-TV och korrekt `NowPlayingItem` bekräftades 2026-08-04.
- Kända begränsningar: naturlig completion/långtidstestning och Jellyfins missvisande nästa-avsnitt-UI återstår i BB-014 och BB-018.
- Dokument: [Media](modules/media.md), [ADR 0011](adr/0011-smart-shuffle-jellyfin-remote-playback-boundary.md), [rapportkatalog](reports/REPORT-CATALOG.md).

### Download Control

- Status: MVP implementerad och deployad; fortsatt säkerhetshärdning pågår.
- Automatiskt verifierat: opaka ID:n, live-revalidering, preview/bekräftelse, exakt ett mål, filbevarande respektive separat destruktivt kontrakt, samtidighetsskydd och sanerade fel.
- Manuellt verifierat: minst ett användarstyrt filbevarande borttagande från qBittorrent via BigBrain. Destruktiv `deleteFiles=true` är inte fullständigt produktionsverifierad.
- Kända begränsningar: destruktiv borttagningsverifiering i BB-019, återstående destruktiv masshantering i BB-020, BB-021–BB-022 samt manuell Retry-verifiering i BB-023. BB-040/BB-033:s återstående kvalitativa UX-evidens följs efter release i BB-041.
- Sprint 2-status 2026-08-10: avslutad och deploymenten godkänd av produktägaren. Dashboard, gemensamt tema/temasynk, kalender och bevarat importerat arbetsschema, Download Control, pause/resume, batchhantering, diagnostik, mobilvy och övriga integrationer är manuellt verifierade. BB-024 och BB-026 är klara; den levererade icke-destruktiva delen av BB-020 är godkänd. Retry i BB-023 är implementerad, automatiskt verifierad och deployad men kunde inte manuellt verifieras eftersom ingen naturligt felande eller problematisk nedladdning fanns. Detta är varken blockerare eller konstaterad defekt. Batch-delete är uppskjuten. BB-033 har endast fått begränsat namn-/undertitelförtydligande.
- Sprint 1-buggfix 2026-08-07: informationspaneler, kort, header, progress och långa namn är breddbegränsade utan horisontell widgetscroll. Fixen är automatiskt testad, production-byggd, headless-verifierad, deployad och manuellt godkänd på mobil.
- Sprint 3 closure 2026-08-10: standardvyn prioriterar fel/problem, aktiva och köade/pausade före klara poster. Klara är kompakt och kollapsad i Alla-vyn men direkt åtkomlig via kontroll eller Klara-filter. Förklaringar skiljer själva Nedladdningskön från Medieflödets väg till biblioteket. 103 frontendtester och production build är gröna. Endast Web deployades; Web är healthy, API och externa container-ID:n samt API-volymer är oförändrade och Calendar/settings/Download Control läser fortsatt korrekt. Produktägaren accepterar den omedelbara smoke-nivån utan blockerande fynd och Sprint 3 är stängd. Den kvalitativa långtidsutvärderingen är uppskjuten till BB-041 och är inte ett känt fel eller en sprintblockerare. BB-040/BB-033 förblir Pågår tills deras fulla DoD har separat evidens.
- Dokument: [Media](modules/media.md), [ADR 0013](adr/0013-safe-qbittorrent-download-removal-boundary.md), [runbook](operations/runbooks/download-control-safe-removal.md), [qBittorrent](knowledge/qbittorrent.md).

## Designsystem och teman

- Status: designsystem v1 och Obsidian Gold är implementerade, automatiskt verifierade och deployade i BigBrain Web.
- Implementerat: tokenbaserade teman, persistens och säker fallback. Standardtemat ändrades inte.
- Jellyfin: separata CSS-adaptrar finns. Den befintliga BigBrain-adaptern och Obsidian Gold-underlaget ska inte sammanblandas; Obsidian Gold är inte installerat som Jellyfin Custom CSS och är inte Tizen-verifierat.
- Jellyfin följer inte BigBrains aktuella runtime-tema: serverinstallerad Custom CSS körs i en separat Jellyfin-klient och kan inte läsa BigBrains lokala `data-theme` eller localStorage. Automatisk koppling skulle bryta ADR 0012:s fristående, manuellt publicerade adaptergräns och implementeras därför inte i Sprint 1.
- Kända begränsningar: manuell tvärmodul- och Tizen-verifiering följs i BB-015.
- Dokument: [temakontrakt](design-system/theme-contract-v1.md), [manuell verifiering](design-system/manual-verification.md), [ADR 0012](adr/0012-design-system-theme-contract-and-jellyfin-adapter.md), [Jellyfin-adapter](../themes/jellyfin/README.md).

## Sentinel och systemstatus

- Status: grundläggande systemstatus är deployad; den bredare Sentinel-arkitekturen är föreslagen och inte godkänd som generell mutationsplattform.
- Kända begränsningar: lokala, ännu inte publicerade Sentinel-förslag är inte del av denna baseline.
- Dokument: [arkitektur](architecture/sentinel-architecture.md), [kunskap](knowledge/sentinel.md), [ADR-index](indexes/adr.md).

## Finance

- Current phase: RESEARCH.
- Current milestone: M2 / BB-045 in progress. M1 is complete; the first provider-neutral
  entitlement/provenance, identity/normalization, session/replay and immutable revision slices are
  implemented, automatically verified and not deployed.
- Completed work: M0 planning plus decimal-based money/price/quantity/risk primitives,
  UTC market observations, provider-neutral market-data and strategy contracts, explicit
  risk/policy decision boundary, in-memory append-oriented decision journal, future
  paper-domain records and deterministic reference pipeline.
- Completed research: BB-046 compared eight provider candidates. Twelve Data is the
  primary Nordic/global EOD candidate; Tiingo and Massive are specialized US alternatives.
  Daily raw OHLCV plus separate corporate actions is the recommended M2 scope.
- Completed research: BB-072 compared ten free/free-adjacent source products with dated
  first-party evidence. None passed the complete retention/non-display/backtesting gate.
  EODHD Free Starter is the best conditional evaluation lead and Twelve Data Basic the
  Nordic technical lead; neither is selected or authorized. Decision: DO NOT INGEST YET.
- Architecture baseline: collect once/reuse when permitted, explicit provenance and
  fail-closed entitlement checks govern local market-data memory. Free/legal sources are
  evaluated first, but free access is not retention permission. Decision/outcome evidence
  and controlled learning are versioned and cannot mutate active/live strategies.
- Implemented BB-045 slice: typed market-data uses; Allowed/Denied/Unknown policy;
  provider/product/evidence/validity/retention/deletion metadata; immutable dataset
  revision and provenance models; raw/derived parent lineage; stable reason codes and a
  deterministic evaluator where missing, mismatched, expired or uncertain scope denies.
- Implemented canonical slice: stable Equity/ETF identity independent of ticker; lifecycle,
  currency, venue and MIC; inclusive effective-date symbol mappings; decimal daily OHLCV;
  raw/adjusted basis; cash dividends; exact rational splits; quality findings and
  deterministic duplicate/conflict handling with immutable revision/policy provenance.
- Implemented session/replay slice: explicit Trading/Closed/Unknown sessions, IANA timezone
  conversion with DST ambiguity rejection, closure/missing/provider-gap/invalid/unknown
  classifications and immutable-revision replay with availability timestamps, historical
  provider references, corporate-action events and stable same-time ordering.
- Implemented revision slice: immutable member snapshots, parent ancestry, explicit
  original→replacement corrections, linear supersession, inclusive availability-as-of
  selection and deterministic ordering. Old revision IDs remain exactly reproducible;
  invalid references, cycles, branches, future members and scope changes fail explicitly.
- Implemented acquisition slice: provider-neutral requests and immutable batches carry
  exact source/product, canonical/provider identity, range, timezone, pagination,
  provenance, completeness and destination revision. A fail-closed gate requires all
  storage/backtest/derived uses before adapter execution. The `SyntheticFixture` adapter,
  deterministic retries/overlap, acquisition journal and orchestration reuse existing
  normalization, gap/replay and revision assembly without IO or persistence.
- Implemented persistence-foundation slice: immutable dataset manifests bind revision,
  coverage, counts, checksum, acquisition/policy/provenance, storage format and deletion
  obligations. A provider-neutral contract plus in-memory correctness reference supports
  immutable append, exact/range/action/gap queries, lineage, integrity, scoped enumeration
  and auditable payload deletion without retaining licensed facts in deletion receipts.
  JSONL and SQLite fixture benchmarks measured up to 1,260,000 rows; the evidence supports
  a provisional immutable-file + transactional SQLite catalog/index direction, not an
  activated production store or final ADR.
- Implemented BB-073 synthetic live-learning slice: provider-neutral current observations
  distinguish event/provider/received/knowledge time and honest real-time/delayed/EOD
  freshness. A deterministic fixture feed represents out-of-order delivery, duplicate,
  correction, session, missing observation and outage. The broker-free fixture shadow rule
  appends immutable strategy/config/feature/risk/build-bound predictions and later outcomes,
  then calculates version-isolated prospective return/excursion/volatility/cost metrics.
- Automated verification 2026-08-11: .NET 10 restore/build passed; 344 API tests and 32
  Sentinel tests passed (376 total). This includes 16 live observation/entitlement/shadow/
  outcome/metric/no-order tests plus all prior persistence and no-lookahead invariants.
- Implemented BB-074 early read-only Finance observation UI: Finance-owned typed snapshot
  and `GET /api/v1/modules/finance/observation` expose RESEARCH safety, provider/entitlement,
  configured watchlist, freshness/session/quality/history and sanitized memory/revision/
  provenance. Production defaults to no provider, no observations and denied real ingestion/
  storage. The responsive view labels fixtures and chart gaps and has no trade/order control.
  API and Web are deployed and technically runtime-verified; not manually product-owner
  verified and M8 has not started.
  Verification 2026-08-11: solution build passed with zero warnings/errors; 351 API and 32
  Sentinel tests passed; 106 Web tests and the Web production build passed; documentation,
  Compose and whitespace gates passed. Runtime API/Web health, fail-closed Finance response,
  405 mutation denial, mobile/desktop headless layout and persisted Calendar/theme/Media
  read smoke passed. No external browser request or market feed was observed.
- BB-071 entitlement update 2026-08-11: a human Twelve Data representative confirmed the
  submitted private/self-hosted personal scope is supported on a qualifying Personal plan,
  including local storage/retention, research/testing, post-termination retention, derived
  data, audit metadata and owner-personal-funds investment use. Basic is evaluation/trial
  only and insufficient. Twelve Data is an entitlement-cleared **paid fallback / qualified
  candidate**, not selected or active.
- Historical free-live research ranked Twelve Data Basic conditionally, but that rank is
  superseded by human evidence that Basic is evaluation/trial only. Alpaca Basic/free IEX
  is now the next cost-first candidate; its entitlement is unresolved. EODHD Free remains
  limited/delayed and Alpha Vantage realtime/delayed US data premium-only. No provider is authorized.
- Combined gate: Twelve Data Personal is legally qualified for the submitted scope but paid;
  free operational suitability is **NO** for Basic. Nasdaq Nordic delayed files still require
  prior approval and are not a canonical OHLCV/corporate-action product. Free Nordic
  eligibility remains unknown. Decision remains **DO NOT INGEST** pending selection.
- The earlier public-evidence state **HUMAN CONFIRMATION REQUIRED** is superseded for a
  qualifying Twelve Data Personal plan by direct provider correspondence. It is not cleared
  for Basic, commercial/paying-subscriber, redistribution, customer/third-party, unknown
  market or materially different use. No plan, account, key, adapter or real data exists.
- Cost-first provider selection remains open. Next safe task: resolve Alpaca Basic/free IEX
  entitlement for the same private scope, without creating an account, key, SDK or adapter;
  then compare any authorized adequate zero-cost option with the Twelve Data paid fallback
  and request explicit product-owner selection before first authorized ingestion.
- Documentation verification 2026-08-11: documentation gate passed for 127 Markdown files
  and 74 unique BB IDs; `git diff --check` and Compose configuration validation passed.
  Source build/test suites were not run because this slice changes no production source.
- BB-075 zero-cost gate 2026-08-11: external Finance market-data budget is exactly 0 SEK.
  Fresh first-party review of Alpaca, Stooq, Yahoo/yfinance, Nasdaq Data Link, EODHD, Alpha
  Vantage, Finnhub, FMP and direct/open alternatives found no exact source with complete
  automation, local retention, replay/backtest and artifact-lifecycle rights. Result:
  **FAIL CLOSED / NO PROVIDER ACTIVATED**. No account, key, adapter, payload or real data exists.
- Current candidates: Alpaca Basic/free IEX is HUMAN CONFIRMATION REQUIRED; EODHD Free
  Starter is the second clarification track. Twelve Data Personal is entitlement-cleared
  but inactive because it is paid. Production remains no-provider/no-real-data.
- Implemented status correction: the safe Finance observation response now exposes
  `ZERO-COST ENTITLEMENT GATE` and `zero-cost-provider-unresolved` instead of superseded
  BB-071 State B wording. Safety flags remain RESEARCH with ingestion, real storage, PAPER,
  LIVE and broker false. Automatically verified: 7 focused tests, 351 API tests, 32 Sentinel
  tests and 106 Web tests plus backend/Web production builds passed. Not deployed: no
  provider passed and no runtime activation was warranted.
- Repository gates: documentation verification passed for 128 Markdown files and 75 unique
  BB IDs; Compose configuration and `git diff --check` passed. The documentation command
  required an unsandboxed rerun after a sandbox-only `spawnSync git EPERM`.
- BB-076 policy update 2026-08-11: Finance now distinguishes explicit provider grants,
  owner-accepted personal research, human confirmation and denial per capability. Owner
  acceptance is restricted to legitimate 0-SEK private read-only research and cannot
  override an explicit prohibition, payment/permission requirement or access control.
- Stooq daily historical download reached owner-accepted residual entitlement evidence for
  the bounded personal use, but the official CSV smoke returned a JavaScript proof-of-work
  verification page rather than data. No bypass was attempted, so activation remains
  fail-closed. No provider/account/key/adapter/real memory/replay/deployment changed.
- EODHD remains conditionally explicit while subscribed with its deletion duty and absent
  account/key/lifecycle. Alpaca retained IEX use still requires human clarification.
- BB-077 2026-08-11 revalidated the renamed current **EODHD Free** tier: €0, 20 calls/day,
  past-year EOD and private non-commercial storage/manipulation/analysis while active. All
  copies must be deleted within one month after expiry. This capability is authorized under
  ADR 0022; post-expiry use/retention and redistribution are denied.
- Implemented and automatically verified: direct server adapter, bounded retries/rate,
  eight-symbol mapping, SQLite/content-addressed durable memory, acquisition journal,
  revision-aware read projection, deterministic replay, compact API/UI retention status and
  preview-confirm-delete receipts. Corporate actions, intraday and live remain disabled.
- BB-078 runtime evidence 2026-08-11: credential presence was verified as a boolean only;
  EODHD Free is enabled with active account and unset entitlement end. Exactly eight external
  EOD requests succeeded without retry for the full watchlist. The durable store contains
  2 008 real observations, eight payloads and eight revisions covering 2025-08-11 through
  2026-08-10. No symbol failed or was rejected.
- Deployed verification: API/Web healthy; the API reports REAL, EODHD Free, delayed/closed,
  durable persistence and active retention. All eight exact-revision replay checks repeated
  with identical checksums. API/Web recreation preserved counts/revisions/payloads and the
  worker skipped today's completed symbols, leaving request count at eight. Headless mobile
  and desktop checks rendered all instruments and chart without console errors or overflow.
- BB-079 runtime evidence 2026-08-11: `core-daily-v1` built locally from the same eight
  immutable market revisions without another EODHD request. Revision
  `feature-5d0397a53d094a2f` contains 42 168 values (39 616 available, 2 552 warmup, zero
  reported quality issues) over 2025-08-11–2026-08-10. Rebuild checksum/idempotency,
  no-lookahead tests, SQLite persistence/restart, feature API/UI and retention inventory
  were verified. Dependent EODHD feature artifacts are in deletion scope.
- BB-080 runtime evidence 2026-08-12: the first offline deterministic M3 engine binds exact market/feature revisions, strategy/version/parameters, next-open simulation, whole-share sizing, cost model and seed to immutable run IDs/checksums. Six BB-078/079 runs for buy-and-hold, SMA10/20 and momentum20 under zero and conservative costs are persisted, API/UI-visible and idempotent across restart. Golden/no-lookahead/cost/retention tests pass. Deployment-day scheduled ingestion ran independently; the backtest engine made no provider request and old exact runs remain immutable.
- BB-081 runtime evidence 2026-08-12: `chronological-oos-walk-forward/v1` and `transparent-robustness-score-v2` bind 16 exact market revisions plus `feature-a04bcf61e20a79ec`. The 70/30 plan with 50-session embargo yields 176/26 sessions and three expanding walk-forward windows. Seventy unique runs cover three strategies, seven bounded parameter variants and fifteen cost points. All verdicts are correctly `INSUFFICIENT_DATA`; SMA and momentum underperform buy-and-hold in the primary test. Immutable API/UI evidence, deletion lineage, no-leakage and restart determinism are deployed/runtime-verified. The evaluator made no provider request; Finance remains RESEARCH.
- BB-082 provider reassessment 2026-08-12: Stooq remains owner-accepted only at the
  entitlement layer, but both its public terms route and daily CSV route returned an active
  JavaScript verification control. No challenge solution, browser automation or alternate
  endpoint was attempted. EODHD Free still exposes about one year; Alpha Vantage full daily
  history and Nasdaq Data Link US EOD are paid. The legitimate blocked path therefore
  applies: zero historical downloads, no adapter/runtime/deployment change and no new
  features/backtests/evaluations. Existing EODHD memory and BB-078–081 evidence remain intact;
  all three robustness verdicts remain `INSUFFICIENT_DATA` and Finance remains `RESEARCH`.
- Known limitations: no live/near-live feed, corporate-action ingestion, robust out-of-sample strategy validation,
  full Risk Engine, paper executor, broker adapter or trading runtime. The deployed Finance
  API/UI is read-only observation only.
- Blockers: no zero-cost live/near-live source is selected; strategy and trading gates remain. ADR 0021 was accepted after explicit
  product-owner architecture review on 2026-08-10; its acceptance does not activate a
  provider. All documented gates block real money.
- Owner approval required: every promotion toward live or greater autonomy.
- Live trading enabled: **NO**.
- Current trading mode: **RESEARCH** (domain/default and module status only; not deployed).
- Dokument: [master roadmap](architecture/finance/master-roadmap.md),
  [module](modules/finance.md), [threat model](security/finance-threat-model.md),
  [market-data memory](architecture/finance/market-data-memory-and-provenance.md).

## Säkerhets- och publiceringsnotering

## Sprint 3 – Download Control-navigation och begriplighet

- Status 2026-08-10: **Stängd**. Implementation complete; automated verification, production build, Web-deployment och teknisk runtimeverifiering pass. Omedelbar produktägar-smoke accepterad utan blockerande fynd. Extended manual UX evaluation är deferred till BB-041 och blockerar inte sprintstängningen.
- Sprintmål: göra Download Control snabbt att överblicka och navigera med lång historik samt tydliggöra skillnaden mellan nedladdningskön och Medieflöde, utan att ändra mutationsgränser.
- Levererat scope: BB-040 och återstående UX-del av BB-033; långtidsvalidering följs i BB-041.
- Varför: BB-040 är en verifierad responsiv användbarhetsfriktion efter Sprint 2; BB-033 är närliggande informationsarkitektur och kan lösas i samma vy utan nya provider- eller datakontrakt.
- Beroenden: befintlig Download Control-listning, filter/urval/batch-state och Media Jobs-livscykel; inga nya externa API:n krävs.
- Risk: låg till medel. Huvudrisken är regression i urval/filter/batch och att färdiga poster blir svårare att hitta.
- Sprintnivåns acceptance criteria: relevanta kontroller och aktiva/problematiska poster nås utan lång scroll; färdiga poster är enkla att hitta; mobil, desktop, tangentbord och skärmläsare fungerar; batchurval och säkra mutationer är oförändrade; användaren förstår skillnaden mellan Nedladdningskö och Medieflöde; automatiska regressionstester och manuell responsiv verifiering är godkända.
- Ingår uttryckligen inte: ny Retry-logik, framtvingat feltest, destruktiv batch/delete, retention/rensning, Arr-recovery, nya providerintegrationer, realtidssynk eller implementation av BB-039.
- Faktisk omfattning: frontend/UX, regressionstester och tillhörande dokumentation; inga backend- eller delade kontrakt ändrades.

Retry-verifieringen i BB-023 utförs separat när ett naturligt säkert testobjekt finns och är inte ett villkor för att starta eller avsluta Sprint 3.

Inga mediafiler eller externa tjänsters konfiguration ändrades av Dashboard Phase 1 eller dokumentationskonsolideringen. Aktuellt kvarvarande arbete finns i [BACKLOG](BACKLOG.md); arbets- och completion-regler finns i [AGENTS](../AGENTS.md).
# BB-090 in progress (2026-08-16)

FRED macro and Finance correctness implementation is locally built and fully regression-tested but not deployed or yet published. The isolated authoritative FRED drill promoted 58,196 observations from five public-domain/citation-requested series as `REVISED_HISTORY_EXPLORATORY`; no production macro claim is available. DST/session, adjusted-close, provider-neutral promotion, typed risk reason, exact risk/prediction lineage and regime-aware backtest grouping are implemented. Production backup/migration/deployment is blocked by a pre-existing long-running API one-off container holding the Finance database; it was preserved. Adjusted-history audit remains pending. Finance stays RESEARCH.
