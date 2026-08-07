# BigBrain

BigBrain är ett modulärt control plane och familjegränssnitt för en Debian-baserad hemserver. Produkten samlar dagliga familjeflöden, mediahantering, systemstatus och framtida AI-funktioner i en gemensam React-applikation och ett versionssatt ASP.NET Core-API.

## Produktvision

BigBrain ska utgå från vad användaren vill göra, inte från hur underliggande tjänster är byggda. Vardagliga funktioner ska vara enkla, mobila och säkra. Tekniska detaljer ska finnas i Admin. Externa mutationer ska vara smala, objektspecifika, bekräftade och auditerbara.

## Aktuell arkitektur

- **BigBrain Web:** React, TypeScript och Vite; gemensamt applikationsskal, designsystem och kompilerade förstapartswidgets.
- **BigBrain API:** modulär ASP.NET Core-monolit med versionssatta API:er och Problem Details.
- **BigBrain Modules:** domän- och integrationsgränser för System, Media, Matlista och Inköpslista.
- **BigBrain Sentinel:** separat minsta-behörighetsgräns för lokala systemcapabilities; Web och API monterar aldrig Docker-socketen.
- **Integration adapters:** Jellyfin, Sonarr, Radarr, Prowlarr och qBittorrent kapslas bakom typade adapters.

Se [arkitekturbaslinjen](ARCHITECTURE.md), [ADR-indexet](docs/indexes/adr.md) och [arbetsreglerna](AGENTS.md).

## Dashboard Views och Widget Framework

Webbgränssnittet har fyra vyer utan sidomladdning:

- **Hem:** Matlista, Inköpslista samt tydligt ej implementerade Kalender- och Påminnelseplatshållare.
- **Media:** sökning, pågående jobb, Smart Shuffle, Download Control och mediaintegrationer.
- **AI:** tydligt ej implementerade platshållare för kommande AI-funktioner.
- **Admin:** serverstatus, containers, integrationer och teknisk information.

`DashboardRegistry`, `ApplicationWidgetRegistry`, `WidgetProvider` och `DashboardWorkspace` ger stabila widget-ID:n, metadata, bibliotek, visa/dölj, kollapsning, drag- och knappbaserad omordning samt versionerad lokal persistence. Tema, redigeringsläge och widgetbibliotek nås samlat genom Dashboardinställningar. Phase 1 är implementerad, automatiskt verifierad, Web-deployad och manuellt visuellt godkänd; den senare inställningsfixen är automatiskt verifierad men ännu inte deployad. Per-user/shared dashboards, synk, roller och användarvalda storlekar är framtida arbete i BB-027.

Se [widgetarkitekturen](docs/architecture/dashboard-widget-framework.md) och [Proposed ADR 0014](docs/adr/0014-dashboard-views-and-widget-framework.md).

## Huvudfunktioner

### Familj

- **Matlista:** maträtter, taggar, familjeschema, generering, måltidsbyte, sparade matsedlar och utskrift.
- **Inköpslista:** permanent aktiv lista, exakt dubblettskydd, konservativ upptäckt av skrivvarianter, förslag, ofta köpt, inköpssessioner och inlärd butiksordning.

### Media

- **Media Search:** bibliotekssökning och kontrollerad Sonarr-/Radarr-preview och bekräftelse.
- **Media Jobs:** normaliserade köer, status, filter och säkra detaljer.
- **Smart Shuffle:** servervalt episodflöde och användarstyrd fjärruppspelning på verifierad Jellyfin for Tizen-session.
- **Download Control:** säker qBittorrent-listning, opaka ID:n, preview/bekräftelse och filbevarande standardborttagning; destruktiv borttagning har strängare riskgrindar och återstående manuell härdning.
- **Media Overview:** Jellyfin, Sonarr, Radarr, Prowlarr och qBittorrent med partiell felisolering.

Se [Mediamodulen](docs/modules/media.md), [Smart Shuffle-ADR](docs/adr/0011-smart-shuffle-jellyfin-remote-playback-boundary.md) och [Download Control-ADR](docs/adr/0013-safe-qbittorrent-download-removal-boundary.md).

### System och Sentinel

Systemstatus läser allowlistade uptime-, CPU-, minnes- och diskcapabilities genom Sentinel. Dockerinventeringens fortsatta arkitekturarbete är separat från Media-adapters. Sentinel-filer med Proposed beslut är inte automatiskt accepterade.

### Designsystem och teman

BigBrain använder semantiska `--bb-`-tokens, tillgängliga komponenttillstånd och temana Mörkt, Ljust och Obsidian Gold. Jellyfin-adaptrar är separata manuella CSS-artefakter; Obsidian Gold-varianten är inte automatiskt installerad i Jellyfin.

Se [theme contract v1](docs/design-system/theme-contract-v1.md), [manuell designkontroll](docs/design-system/manual-verification.md) och [Proposed ADR 0012](docs/adr/0012-design-system-theme-contract-and-jellyfin-adapter.md).

## Verifieringsnivåer

Dokumentationen skiljer på implementerat, automatiskt verifierat, deployat och manuellt verifierat. [STATUS](docs/STATUS.md) är den aktuella verklighetsbilden; sanerade rapporter under [docs/reports](docs/reports/README.md) innehåller daterad evidens. Lokala fullrapporter är intern evidens och publiceras inte rått.

## Lokal utveckling

Frontend:

```bash
cd src/BigBrain.Web
npm ci
npm test -- --run
npm run build
```

Backend körs i den dokumenterade .NET 10 SDK-containern:

```bash
docker run --rm --user "$(id -u):$(id -g)" \
  --volume "$PWD:/workspace" --workdir /workspace \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test BigBrain.slnx --configuration Release --artifacts-path /tmp/bigbrain-artifacts
```

Se [TESTING.md](TESTING.md) för testkartan och [operationsindexet](docs/indexes/operations.md) för deployment och runtimeverifiering.

## Säker runtimekonfiguration

Kopiera `.env.example` till den Git-ignorerade `.env` och ge filen rättighet `0600`. Lägg aldrig credentials, cookies, privata URL:er eller råa externa identiteter i Git, frontend, rapporter eller diagnostik. Validera Compose med `docker compose config --quiet` utan att skriva ut den effektiva konfigurationen.

## Dokumentationskarta

- [STATUS](docs/STATUS.md) – aktuell implementerings-, deployment- och verifieringsstatus.
- [BACKLOG](docs/BACKLOG.md) – kvarvarande verifierat arbete och kända begränsningar.
- [Dokumentationsindex](docs/indexes/documentation.md) – auktoritet och läsordning.
- [Knowledge-index](docs/indexes/knowledge.md) – system- och integrationskunskap.
- [Operationsindex](docs/indexes/operations.md) – runbooks, deployment och recovery.
- [ADR-index](docs/indexes/adr.md) – beslut och Proposed förslag.
- [Rapportindex](docs/indexes/reports.md) – sanerad GitHub-evidens och lokal rapportpolicy.
- [AGENTS.md](AGENTS.md) – permanenta arbets-, dokumentations- och publiceringsregler.
- [Tidig historik](docs/history/early-sprints.md) – historiska sprintbaselines som inte längre är produktöversikt.

## CI

GitHub Actions kör backend restore/build/test, frontend install/test/build, dokumentationsgrind och secret scan. CI deployar inte, ansluter inte till hemmaservern och använder inga live write-endpoints.
