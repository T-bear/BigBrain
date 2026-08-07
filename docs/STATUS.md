# BigBrain Status

- Senast uppdaterad: 2026-08-07 (Europe/Stockholm)
- Verifierad mot commit: `1b62b90`
- Runtime senast verifierad: 2026-08-04

Status skiljer uttryckligen mellan implementerat, automatiskt verifierat, deployat och manuellt verifierat. Detaljerad evidens finns i [rapportkatalogen](reports/REPORT-CATALOG.md).

## Dashboard Views och Widget Framework Phase 1

- Status: Implementerat, automatiskt verifierat, deployat och manuellt visuellt godkänt av produktägaren.
- Implementerat: Hem, Media, AI och Admin; registerbaserade widgets; widgetbibliotek; redigeringsläge; synlighet, ordning, drag-and-drop, knappbaserad flytt och kollapsning; versionerad lokal persistens med säker fallback.
- Deployat: BigBrain Web. Ingen backend-, Compose- eller runtimekonfigurationsändring krävdes.
- Manuellt verifierat: mobilnavigationen, de fyra vyerna och layouten godkändes 2026-08-04.
- Kända begränsningar: profilsynkronisering, delade dashboards, mallar, roller, serverpersistens och fria storlekar är framtida arbete i BB-027.
- Sprint 1-buggfix 2026-08-07: Dashboardinställningar samlar Tema, redigeringsläge och widgetbibliotek bakom ett tillgängligt kugghjul. Fixen är automatiskt testad, production-byggd och verifierad i headless Chromium för mobil, tablet och desktop, men inte deployad eller manuellt verifierad på fysisk enhet.
- Dokument: [arkitektur](architecture/dashboard-widget-framework.md), [ADR 0014](adr/0014-dashboard-views-and-widget-framework.md), [runbook](operations/runbooks/dashboard-widget-framework-verification.md), [rapporter](reports/features/dashboard/).

## Matlista och Inköpslista

- Status: Implementerade, deployade och manuellt runtimeverifierade.
- Implementerat: familjefokuserade mobilflöden för veckoplanering och inköp.
- Sprint 1-buggfix 2026-08-07: konservativ skrivvariantskontroll och uttryckligt `Lägg till ändå` är implementerade för nya varor; ”Ofta köpt” använder läsbara semantiska färger i samtliga dokumenterade states. Fixarna är automatiskt testade, production-byggda och verifierade i headless Chromium för samtliga teman och states, men inte deployade eller manuellt verifierade på fysisk enhet.
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
- Kända begränsningar: BB-019–BB-026 täcker återstående verifiering, masshantering, ARR-recovery, retention, retry, paus/återuppta och diagnostik.
- Sprint 1-buggfix 2026-08-07: informationspaneler, kort, header, progress och långa namn är breddbegränsade utan horisontell widgetscroll. Fixen är automatiskt testad, production-byggd och verifierad i headless Chromium för mobil, tablet och desktop, men inte deployad eller manuellt verifierad på fysisk enhet.
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

## Säkerhets- och publiceringsnotering

Inga mediafiler eller externa tjänsters konfiguration ändrades av Dashboard Phase 1 eller dokumentationskonsolideringen. Aktuellt kvarvarande arbete finns i [BACKLOG](BACKLOG.md); arbets- och completion-regler finns i [AGENTS](../AGENTS.md).
