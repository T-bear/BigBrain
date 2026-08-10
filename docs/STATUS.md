# BigBrain Status

- Senast uppdaterad: 2026-08-10 (Europe/Stockholm)
- Verifierad mot commit: Finance-planeringen publiceras i detta uppdrag; senaste runtimeevidens är oförändrad.
- Runtime senast verifierad: 2026-08-10 (produktägarens manuella Sprint 2-verifiering; ingen runtime ändrades under closure)

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
- Current milestone: M0 architecture and safety specification complete.
- Completed work: master roadmap, module/capability architecture, threat/risk model,
  progressive modes, backtest/paper/broker plans, ADR proposals, runbooks, test strategy
  and granular BB-042–BB-070 backlog.
- Active work: none.
- Next safe task: BB-044 / M1 read-only Finance domain skeleton, without broker SDK,
  credentials, market feed, order endpoint or trading runtime.
- Blockers: product-owner prioritization for M1; all documented gates block real money.
- Owner approval required: every promotion toward live or greater autonomy.
- Live trading enabled: **NO**.
- Current trading mode: **NOT IMPLEMENTED**.
- Dokument: [master roadmap](architecture/finance/master-roadmap.md),
  [module](modules/finance.md), [threat model](security/finance-threat-model.md).

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
