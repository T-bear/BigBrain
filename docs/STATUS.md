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

## Future product planning and Family coordination

- Status: planned only; no feature, redesign, authentication flow, external integration or runtime
  change was implemented by the 2026-08-17 capture.
- Captured: a candidate BigBrain-wide Obsidian Gold/less-is-more design direction; passwordless,
  passkey, trusted-device and step-up investigation; external school meals and school-aware
  household menu generation; and Home Calendar past-day plus mobile swipe UX investigations.
- Family epic consolidation 2026-08-18: planned-only Family View & Family Coordination now links the
  existing school-meal/menu and Calendar/Home work with BB-029, BB-036 and BB-027 dependencies, and
  captures school schedules, holidays, study days, attendance-aware meals, member context,
  daily/weekly aggregation, generic conflict semantics, provenance/freshness and privacy. No feature,
  sprint, provider research, deployment or runtime change occurred.
- Priority: the active Finance direction remains the smallest deterministic foundation toward
  `FINANCE AUTONOMOUS RESEARCH v1`. These entries await normal prioritization and do not move the
  existing consolidation/security or penetration-testing gates.
- Documents: [Family epic](architecture/family-view-family-coordination.md),
  [backlog](BACKLOG.md#future-family-view--family-coordination-epic) and historical
  [planning record](reports/documentation/product-ux-auth-school-meals-backlog-capture-20260817.md).

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
# BB-090 complete (2026-08-16)

Repository history proves that the implementation commit is `6f6cc743b91b9d0c2cb52fb297d0215fd7125b50`, followed by publication commit `fb55537aa77dd54c7a5929e3f29084bb5f86252e`. The previously documented `6f6cc746ce5d8bea24d19ad48c3596cb9ed0de28` did not exist and is superseded as incorrect provenance.

Closure runtime evidence dated 2026-08-16: the stale one-off API container was identified as an old-image maintenance invocation that had instead run the web host for five days; it was gracefully stopped and removed after proving the named Finance volume was shared and persistent. A hash-verified full SQLite backup was created before migration. An isolated copy migrated to ordered versions `1,90,91,92` with identical before/after counts, followed by production migration, adjusted-history audit, API/Web deployment and repeated restart/idempotence checks. API, Web and Sentinel are healthy. The immutable 25 market revisions, 9,746 observations, 6 feature revisions, 473 backtests, 13 robustness evaluations, 48 shadow predictions, 0 outcomes and 0 risk evaluations survived unchanged.

The adjusted audit classified 24 EODHD revisions `RAW_AND_ADJUSTED_VALID` and `wiki-5713d7dccfa38f56` `ADJUSTED_SEMANTICS_INVALID`; the WIKI revision was not rewritten and adjusted research is denied while raw research remains valid. Macro promotion now goes through quarantine, hashes, rights/provenance, validation and promotion evidence. Production holds `fred-a98118273a99adc9`, 58,196 observations, one candidate and one revision as `REVISED_HISTORY_EXPLORATORY`; its latest deterministic explanatory context is Rates UNKNOWN, YieldCurve NORMAL, Inflation HIGH, Labor IMPROVING. This is not a point-in-time or trading claim.

The runtime now reports `fredApiKeyConfigured=true` without disclosing secret material. Official first-party `fred/series/observations` requests with `output_type=2` promoted CPIAUCSL and UNRATE for 2020-01-01–2020-03-01 over real-time period 2020-02-01–2021-02-01 as separate immutable revisions `fred-9300bb944c8c319d` and `fred-5800c2de8f2ef185`, 36 observations each, `POINT_IN_TIME_CAUSAL`. The original `fred-a98118273a99adc9` and all 58,196 `REVISED_HISTORY_EXPLORATORY` observations remain unchanged. UNRATE January 2020 proves a real revision from 3.6 (real-time start 2020-02-07) to 3.5 (2021-01-08). Knowledge time is conservatively the following day at 00:00 UTC; as-of selection and missing-vintage behavior fail closed without revised-history fallback.

Local finalization regression is 440 API, 32 Sentinel and 113 frontend tests, with Release/frontend/container builds, documentation verification and Compose validation green. API, Web and Sentinel passed two restart/recreate checks. Production retains 25 market revisions, 9,746 observations, 6 feature revisions, 473 backtests, 13 robustness evaluations, 48 shadow predictions (24 PENDING and 24 INVALIDATED), 0 outcomes and 0 risk evaluations; Sunday macro acquisition created no market session, prediction or outcome. Finalization commit `0f2ba9c3e7d5e7b3bf088c9094bdd79b3a35db1e` passed GitHub Actions run 55: backend, frontend, documentation and secrets all succeeded. Every BB-090 closure gate is satisfied; **BB-090 is COMPLETE**. Finance stays RESEARCH.

# BB-091 complete (2026-08-17)

Provider-neutral Riksbank/ECB Macro and FX implementation, migration 93, verified backup/isolated drill, 20-year bounded production acquisition, cross-provider comparison, exact retry idempotence, preservation and restart are green. Riksbank holds 15,065 and ECB 16,804 `REVISED_HISTORY_EXPLORATORY` observations; all 58,268 BB-090 FRED observations remain unchanged. Final local verification passed 449 API, 32 Sentinel and 113 frontend tests plus Release/frontend/API image/docs/Compose/diff/secret gates. Implementation commit `dc42d47f8712023984d653b0c34977145476b2e3` passed GitHub Actions run 31997421033. **BB-091 is COMPLETE.** Finance remains RESEARCH / 0 SEK with no PAPER, broker, order, LIVE/AUTO or self-learning.

# BB-092 complete (2026-08-22)

The bounded Autonomous Research v1 foundation is implemented, locally verified and published in `8f8358c990b871c986bf0ee30ce434cb68a2394e`. It reuses immutable daily features, deterministic backtests and genuine chronological OOS/walk-forward robustness; stores all experiment outcomes with pinned lineage, family attempt counts and deterministic identities; and exposes read-only API/UI evidence. Local verification passed 455 API, 32 Sentinel and 114 frontend tests plus Release/frontend/API-image/docs/Compose/diff/secret gates. GitHub Actions run 32587285183 passed backend, frontend, documentation and secrets. No deployed production runtime verification was performed. Finance remains RESEARCH / 0 SEK / execution authority NONE; Hard Risk Engine is unchanged. **BB-092 is COMPLETE.**

## BB-092 remediation/finalization (2026-08-22)

Correctness remediation is implemented, verified and published in `4f5e2b3272c92b3ad072bc13849b374824f50299`: complete failed-run reconstruction after interruption, immutable same-key failed-result semantics, explicit run/experiment lineage, durable global single-flight, actual attempt sums, consistent next-session/horizon-1 hypotheses, separate scientific verdict totals and bounded historical run/experiment reads. Current regression passed 461 API, 32 Sentinel and 114 frontend tests, Release/frontend/API-image builds, documentation, Compose, diff and sanitized secret-pattern gates. GitHub Actions run 32589143795 passed backend, frontend, documentation and secrets. The work remains BB-092 and does not begin the Scheduler/Orchestrator slice. **BB-092 is remediated/finalized and ready for separately approved operationalization planning.**

## BB-092 final evidence-selection remediation (2026-08-22)

A final narrow correction published in `6796953533fb72cb12e5546964033e2939cf74c0` selects only the exact latest robustness generation returned by the repository-native builder and requires coherent feature, market, checksum, relational and strategy-version evidence for both enabled families. Historical lexical ordering and stale cross-revision fallback are removed. Missing or conflicting current evidence fails closed before experiment creation. Local verification passed 465 API, 32 Sentinel and 114 frontend tests plus Release/frontend/API-image/documentation/Compose/diff/secret-pattern gates. GitHub Actions run 32590079318 passed backend, frontend, documentation and secrets. **BB-092 final remediation is complete.**

# BB-093 implementation (2026-08-22)

The disabled-by-default Finance Research Scheduler/Orchestrator v1 is implemented, verified and published in `0f952f88e3ec630b0a000e3d7565dd6274f63c09` over BB-092. It uses a 60-minute check, one 02:00 UTC current opportunity, deterministic session-date identity, durable opportunity journal, restart reconciliation, bounded deferral and read-only API/UI status. It neither polls providers nor changes prospective shadow, research methodology, Hard Risk or execution authority. Local verification passed 475 API, 32 Sentinel and 115 frontend tests plus Release/frontend/API-image/documentation/Compose/diff/secret-pattern gates. GitHub Actions run 32591633284 passed backend, frontend, documentation and secrets. **BB-093 is COMPLETE.** Continuous unattended research is not yet approved.

Readiness remediation on 2026-08-22 is implemented, verified and published in `adf930a6aacbfab2e314f558a0307b7643edb7e0`. It removes the unsafe `MAX(session_date)` readiness shortcut: all eight approved watchlist instruments must be current for the required market session, and a same-session feature revision must exactly match the canonical market-revision lineage and include every instrument. Temporary gaps defer with specific reasons; an older deferred opportunity is explicitly superseded at the next date. Local regression passed 479 API, 32 Sentinel and 115 frontend tests plus all applicable build/configuration/documentation gates. GitHub Actions run 32592856198 passed all four jobs. No provider polling, BB-092 scoring, Hard Risk or execution authority changed. **BB-093 is remediated and ready for Resource & Safety Governor review; that slice has not started.**

# BB-094 implementation (2026-08-22)

The Finance Resource & Safety Governor v1 is implemented, verified and published in `8f6e83904aa3b0a9e6995c22c699a7344777739d`. Scheduled research now evaluates trusted Sentinel-backed CPU, memory and configured-disk snapshots after recovery/readiness and before opportunity claim. Unavailable/stale/failed critical metrics defer fail-closed; critical disk blocks; all applicable reasons and compact evidence persist on the opportunity. Temperature and service activity are explicitly unsupported rather than inferred. Local verification passed 486 API, 32 Sentinel and 115 frontend tests plus all applicable build/configuration/documentation gates. GitHub Actions run 32595755361 passed backend, frontend, documentation and secrets. Scheduler default-off, manual BB-092, Research Integrity, Hard Risk and `RESEARCH / 0 SEK / NONE` authority remain unchanged. **BB-094 is COMPLETE; continuous unattended research still awaits a separately reviewed operations/recovery-hardening slice.**

# BB-095 implementation (2026-08-22)

Finance autonomous research operations/recovery v1 is implemented, verified and published in `31c11865b430a137b7eb5be77681158d1d6adb1f`. The read model derives from existing cadence, scheduler, governor, research and recovery truth; startup reconciliation preserves immutable experiment evidence. True operational failures are uniquely journaled and become attention-required after three consecutive opportunities, while scientific rejection and temporary deferral remain normal. Scheduler stale/persistent waits and explicit maintenance pause are visible through read-only API/UI. Local verification passed 495 API, 32 Sentinel and 115 frontend tests plus all applicable build/configuration/documentation gates. GitHub Actions run 32597319341 passed backend, frontend, documentation and secrets. Scheduler remains disabled by default and authority remains `RESEARCH / 0 SEK / NONE`. **BB-095 is complete and the foundation is eligible for a separate explicit deployment decision enabling continuous unattended RESEARCH; this publication did not activate it.**

# BB-096 implementation (2026-08-22)

BigBrain UX Sprint v1 is complete and published in `1293d36a04013bb65469ca2191f2f1683d52a50d`. The application now uses one mobile-first AppShell, one semantic component/token contract and the selectable Obsidian Gold, Arctic Wind and Forest Night themes. Hem is a calm launcher; existing meal, shopping and calendar tools moved intact to Familj; Media and Finance retain their complete functional surfaces; Settings/theme selection, AI and Admin are discoverable through Mer. Obsidian Gold is the default, legacy stored theme IDs migrate safely, PWA theme color follows the active theme and navigation reserves iOS safe areas. Local verification passed 495 API, 32 Sentinel and 115 frontend tests plus Release, frontend, API/Web image, documentation, Compose, diff and sanitized changed-file gates. A temporary cross-theme mobile/desktop view matrix was manually inspected. GitHub Actions run 32600293268 passed backend, frontend, documentation and secrets. No Finance research, scheduler, governor, Hard Risk or execution behavior changed.

## BB-096 appliance commissioning (2026-08-23)

Repository HEAD `27b67450cee61f0283a8eaaf7748a0d57274ea1c` was built and deployed through Compose without removing volumes. The served Web contains the BB-096 AppShell and three themes; real-browser checks passed theme switching/persistence, the five-destination mobile dock at 390 x 844, desktop rail at 1440 x 900, AI/Admin secondary navigation and representative Family, Media, Finance and Admin surfaces. All relevant read-only API smoke requests returned HTTP 200. Recovery was healthy after the controlled API/Web recreation. Finance stayed `RESEARCH / 0 SEK / NONE`; scheduler state, provider cadence and evidence counts were unchanged, with one deferred opportunity and zero runs/experiments. No source or runtime defaults changed. Product-owner manual UX review remains pending; see the sanitized [commissioning report](reports/deployments/bb-096-ux-commissioning-20260823.md).

# BB-097 implementation (2026-08-23)

Family now has a dedicated `FamilyExperience` composition and no longer renders through `DashboardWidget` in its normal mode. Meal and shopping modules expose an explicit Family presentation that removes the legacy collapsible shell while retaining their existing standalone contract and API behavior. The compact settings icon preserves theme, ordering and visibility controls; meals, shopping, calendar and truthful planned reminders form one continuous mobile-first flow. Obsidian Gold surfaces, borders, accent, typography and the bottom dock were refined against all three supplied local mockups.

Local verification passed 115 frontend tests, the frontend production build, 495 API tests, 32 Sentinel tests, Release build, documentation, Compose and diff gates. GitHub Actions runs 32618788138 and 32619102449 passed. Final commit `cb9c9caa0efc66119616767bc3d9507d9a83562b` was built as Web image `sha256:5f37b814b1ccb03dcd25353721a4ddd42e94a0f5bcea257f2b3389e59775f312` and deployed by recreating only Web without volume removal. The served PWA was opened at 390 × 844, 430 × 932 and 1440 × 900 with real Family data and Obsidian Gold; all four sections and meal tabs were present, no alerts or horizontal overflow occurred, and the dock was mobile-only. Recovery remained healthy/clean. Finance remained `RESEARCH / 0 SEK / NONE`, with zero experiments and unchanged scheduler operation. BB-097 is technically complete; owner visual approval is separately and explicitly pending. See the [implementation report](reports/features/dashboard/bb-097-family-obsidian-gold-reference-20260823.md).

Owner review retained the BB-097 Family architecture but rejected the remaining generic dark-web appearance. A 2026-08-23 premium art-direction refinement now introduces theme-owned canvas, three-level surface, edge/highlight, coherent shadow, accent-tonal and three-level text tokens. Obsidian Gold uses warm charcoal/stone materials and a restrained multi-tone metal accent; Forest Night uses deeper pine/smoked-green materials and bounded organic CSS illumination, rather than an Obsidian recolor. Family typography, tabs, Today marker, settings control and the shared floating dock use the same semantic material contract. The fixed page background and unbounded diagonal pseudo-light were replaced after investigating full-page capture artifacts. Normal 390 × 844 and 430 × 932 viewport frames plus scrolled frames were opened for both dark themes; Arctic Wind remained readable and overflow-free. Deployment and final CI evidence are recorded in the implementation report. Owner visual approval remains pending.

## Finance unattended-research commissioning (2026-08-22)

The actual appliance was updated to the accepted BB-092–095 chain and explicitly commissioned with the deployment-only scheduler override at 20:55 UTC. Source remains default-off; deployed cadence is 60-minute checks, 02:00 UTC and maximum two experiments. Recovery was healthy/clean, the real Sentinel-backed governor returned `ALLOW`, maintenance pause was false, the eight-instrument EODHD cadence remained unchanged and Finance stayed `RESEARCH / 0 SEK / NONE`. The first natural deterministic opportunity deferred on `featureLineageIncomplete`, producing zero runs and zero experiments. One controlled API restart preserved exactly one opportunity, zero runs and zero experiments with no incident or attention state. EODHD backup eligibility remains restricted/subscription-only. Narrow correction `5cc35f415c5314f702c719f0114979ac3f8a3821` maps that defer reason truthfully in aggregate operations status, passed GitHub Actions run 32598337042 and is deployed as API image `sha256:8f916403e8a672a7fe1aa6dc76e14cbcb86c9312bbb1e1fb35070af847153a2a`; it does not change the authoritative readiness gate. Scheduler rollback is the deployment override `FINANCE__RESEARCHSCHEDULER__ENABLED=false` plus API recreate, without deleting history.
