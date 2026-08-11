# BigBrain

BigBrain är ett modulärt control plane och familjegränssnitt för en Debian-baserad hemserver. Produkten samlar dagliga familjeflöden, mediahantering, systemstatus och framtida AI-funktioner i en gemensam React-applikation och ett versionssatt ASP.NET Core-API.

## Produktvision

BigBrain ska utgå från vad användaren vill göra, inte från hur underliggande tjänster är byggda. Vardagliga funktioner ska vara enkla, mobila och säkra. Tekniska detaljer ska finnas i Admin. Externa mutationer ska vara smala, objektspecifika, bekräftade och auditerbara.

## Aktuell arkitektur

- **BigBrain Web:** React, TypeScript och Vite; gemensamt applikationsskal, designsystem och kompilerade förstapartswidgets.
- **BigBrain API:** modulär ASP.NET Core-monolit med versionssatta API:er och Problem Details.
- **BigBrain Modules:** domän- och integrationsgränser för System, Media, Matlista, Inköpslista, Kalender och Finance.
- **BigBrain Sentinel:** separat minsta-behörighetsgräns för lokala systemcapabilities; Web och API monterar aldrig Docker-socketen.
- **Integration adapters:** Jellyfin, Sonarr, Radarr, Prowlarr och qBittorrent kapslas bakom typade adapters.

Se [arkitekturbaslinjen](ARCHITECTURE.md), [ADR-indexet](docs/indexes/adr.md) och [arbetsreglerna](AGENTS.md).

## Dashboard Views och Widget Framework

Webbgränssnittet har fem vyer utan sidomladdning:

- **Hem:** Matlista, Inköpslista, Kalenderns kompakta veckovy samt en tydligt ej implementerad Påminnelseplatshållare.
- **Media:** sökning, pågående jobb, Smart Shuffle, Download Control och mediaintegrationer.
- **Finance:** read-only research-watchlist, entitlementstatus, historiskt minne och gap-aware diagram.
- **AI:** tydligt ej implementerade platshållare för kommande AI-funktioner.
- **Admin:** serverstatus, containers, integrationer och teknisk information.

`DashboardRegistry`, `ApplicationWidgetRegistry`, `WidgetProvider` och `DashboardWorkspace` ger stabila widget-ID:n, metadata, bibliotek, visa/dölj, kollapsning, drag- och knappbaserad omordning samt versionerad lokal persistence. Tema, redigeringsläge och widgetbibliotek nås samlat genom Dashboardinställningar. Phase 1 är implementerad, automatiskt verifierad, Web-deployad och manuellt visuellt godkänd; den senare inställningsfixen är automatiskt verifierad men ännu inte deployad. Per-user/shared dashboards, synk, roller och användarvalda storlekar är framtida arbete i BB-027.

Se [widgetarkitekturen](docs/architecture/dashboard-widget-framework.md) och [Proposed ADR 0014](docs/adr/0014-dashboard-views-and-widget-framework.md).

## Huvudfunktioner

### Familj

- **Matlista:** maträtter, taggar, familjeschema, generering, måltidsbyte, sparade matsedlar och utskrift.
- **Inköpslista:** permanent aktiv lista, exakt dubblettskydd, konservativ upptäckt av skrivvarianter, förslag, ofta köpt, inköpssessioner och inlärd butiksordning.
- **Kalender:** persistent veckokalender, responsiv månadsvy och säker server-side flerfilsimport från verifierade Heroma `.xlsx`-scheman. Se [modulkontraktet](docs/modules/calendar.md).

### Media

- **Media Search:** bibliotekssökning och kontrollerad Sonarr-/Radarr-preview och bekräftelse.
- **Media Jobs:** normaliserade köer, status, filter och säkra detaljer.
- **Smart Shuffle:** servervalt episodflöde och användarstyrd fjärruppspelning på verifierad Jellyfin for Tizen-session.
- **Nedladdningskö / Download Control:** säker qBittorrent-listning, opaka ID:n, diagnostik, paus/återuppta/retry, begränsad partiell batch samt objektspecifik säker borttagning. Sprint 3:s statusprioritering och kompakta historik är implementerade, automatiskt verifierade, Web-deployade och tekniskt accepterade utan blockerande fel; den kvalitativa långtidsutvärderingen fortsätter separat i BB-041. Sprint 2 är deployad och godkänd; Retry väntar separat på manuell verifiering när ett naturligt felande jobb finns.
- **Media Overview:** Jellyfin, Sonarr, Radarr, Prowlarr och qBittorrent med partiell felisolering.

Se [Mediamodulen](docs/modules/media.md), [Smart Shuffle-ADR](docs/adr/0011-smart-shuffle-jellyfin-remote-playback-boundary.md) och [Download Control-ADR](docs/adr/0013-safe-qbittorrent-download-removal-boundary.md).

### System och Sentinel

Systemstatus läser allowlistade uptime-, CPU-, minnes- och diskcapabilities genom Sentinel. Dockerinventeringens fortsatta arkitekturarbete är separat från Media-adapters. Sentinel-filer med Proposed beslut är inte automatiskt accepterade.

### Finance – research foundation

Finance har en implementerad, read-only RESEARCH-grund med säkra numeriska värdeobjekt,
provider-neutrala market-data-/strategikontrakt samt fail-closed risk-, policy-, besluts-
och journalmodeller. BB-045 har nu även starka entitlement-, provenance-, dataset revision-
och raw/derived-lineagetyper samt en deterministisk fail-closed evaluator. Den fixture-baserade
grunden omfattar också kanonisk instrumentidentitet, effective-dated provider-symbolhistorik,
daglig OHLCV, utdelningar, exakta splitkvoter, kvalitetsfynd, deterministisk normalisering,
explicita marknadssessioner/gap och revision-bunden historisk replay utan lookahead.
Immutable in-memory datasetrevisioner bevarar nu observation membership och applicerar
corrections/supersession först vid deras explicita availability-gräns utan att skriva om
äldre revisions-ID:n.
Ett nytt fixture-only acquisition-lager binder framtida providerbatcher till exakt request,
policy, provenance och destination revision, journalför deterministiska utfall och återanvänder
befintlig normalisering, gap/replay och revision assembly. Dess `SyntheticFixture`-adapter
kan inte auktorisera eller representera en riktig provider.
Ett nytt fixture-only manifest- och persistencekontrakt definierar immutable append,
integritet, revisionsfrågor och policyavgränsad deletion. En in-memory referens och en
reproducerbar JSONL/SQLite-benchmark använder endast syntetiska EOD-rader; mätningen stödjer
provisoriskt SQLite som transaktionellt katalog/index tillsammans med immutable filer för
payload, men aktiverar ingen produktionslagring. Ingen verklig providerpersistence,
verklig provideradapter, executor, brokerintegration eller live trading
är implementerad eller deployad. Den publicerade planen går från RESEARCH via backtesting
och PAPER till eventuellt policy-governed AUTO. Se den kanoniska
[Finance master roadmap](docs/architecture/finance/master-roadmap.md) och
[Finance-modulen](docs/modules/finance.md). Finance följer “free first” och “collect once,
reuse when permitted”: providerneutral lokal historik prioriteras kostnadsmedvetet, men
okänd licens eller retention stoppar lagring och användning.
BB-072:s daterade jämförelse av tio gratis eller gratisnära källprodukter fann ingen som
verifierar hela kombinationen varaktig lokal retention och personlig non-display
backtesting. Rekommendationen är `DO NOT INGEST YET`; EODHD Free Starter och Twelve Data
Basic är villkorade evidensspår, inte valda providers.
En synthetic-only live-observationsgrund skiljer nu market event-, provider-, received-
och knowledge-tid, beskriver realtid/delay ärligt och simulerar observationer, sessioner,
luckor, outage, dubbletter och corrections utan väggklocka. En uttryckligt icke-handlande
teststrategi skapar immutable shadow predictions och separata senare outcomes/metrics;
ingen order- eller brokeryta finns. Ny skriftlig mänsklig provider-evidens klargör att
Twelve Data Basic endast är evaluation/trial och att BigBrains beskrivna privata användning
kräver en betald Personal-plan. För den planen stöds lokal lagring och retention,
research/testing, post-termination retention, derived data, auditmetadata och
investeringsbeslut med endast ägarens egna medel. Twelve Data är därför en entitlement-
cleared betald fallback, inte vald eller aktiverad provider. Cost-first-grinden går härnäst
vidare med osänd Alpaca Basic/free IEX entitlement-research; inget konto, key, adapter eller
verklig data har skapats. Ingen fri svensk/nordisk källa är berättigandeverifierad.
Produktägaren har nu satt Finance externa market-data-budget till exakt **0 SEK** tills ett
nytt explicit beslut tas. En färsk sweep av Alpaca, Stooq, Yahoo/yfinance, Nasdaq Data Link,
EODHD, Alpha Vantage, Finnhub, FMP och direkta exchange/open-spår fann ingen källa med
komplett verifierad automation-, retention- och research/backtesting-rätt. Finance förblir
fail-closed utan riktig data; Twelve Data Personal är en inaktiv entitlement-cleared paid
fallback. API-grinden visar nu `ZERO-COST ENTITLEMENT GATE` i stället för ersatt BB-071 State B.
BB-076 inför en capability-specifik `OwnerAcceptedPersonalResearch`-klass för legitima
0-SEK-källor där inga identifierade villkor förbjuder avgränsad privat research. Klassen
kan aldrig åsidosätta negativa villkor, betalningskrav eller tekniska åtkomstkontroller.
Stooqs download nådde evidensklassen men ett smoke-anrop gav en JavaScript-kontroll i stället
för CSV; ingen kontroll kringgicks och ingen källa aktiverades.
BB-077 revaliderar den nuvarande produkten **EODHD Free** (€0, 20 anrop/dag, ett års EOD)
och implementerar en credential-bound read-only adapter. SQLite plus content-addressed raw
payload ger lokal revisionsmedveten memory, deterministic replay och Finance API/UI-stöd.
Providern är disabled utan ägarens fria API-key; ingen riktig observation har ännu hämtats.
Aktiv lagring/analys är tillåten för privat research, men alla täckta kopior måste raderas
inom en månad efter account/subscription expiry genom preview och explicit bekräftelse.
BB-074 ger nu Finance en navigerbar, responsiv read-only observationsvy och ett versionssatt
`GET /api/v1/modules/finance/observation`. Produktionsdefault är RESEARCH, ingen auktoriserad
provider, noll observationer och nekad ingestion/lagring av real data. Syntetiska UI-fixtures
är alltid märkta; detta är tidig M2-observation och startar inte M8:s tradingdashboard.
API- och Web-komponenterna för BB-074 är deployade och tekniskt runtime-verifierade
2026-08-11. Produktägaren har inte ännu lämnat separat manuell UI-verifiering.

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

Backend kan köras med repositoryts lokala .NET 10 SDK:

```bash
dotnet restore BigBrain.slnx
dotnet build BigBrain.slnx --configuration Release --no-restore
dotnet test BigBrain.slnx --configuration Release --no-build --no-restore
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
- [Finance master roadmap](docs/architecture/finance/master-roadmap.md) – fas, gates och nästa säkra Finance-arbete.
- [AGENTS.md](AGENTS.md) – permanenta arbets-, dokumentations- och publiceringsregler.
- [Tidig historik](docs/history/early-sprints.md) – historiska sprintbaselines som inte längre är produktöversikt.

## CI

GitHub Actions kör backend restore/build/test, frontend install/test/build, dokumentationsgrind och secret scan. CI deployar inte, ansluter inte till hemmaservern och använder inga live write-endpoints.
