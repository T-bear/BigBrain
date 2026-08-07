# Sprint 1 deployment regression

> Detta är en sanerad GitHub-version. Secrets, privata adresser, container-ID:n, råloggar och användardata har utelämnats.

## Metadata

- Datum: 2026-08-07
- Scope: Sprint 1 API/Web-deployment, kalenderpersistence, integrationskonfiguration och delat tema
- Ursprunglig deploycommit: `5669284774e7386c5947fc39e3f2796cee60e7c8`
- Backlogg: BB-030, BB-031, BB-034, BB-035, BB-037 och BB-038

## Status

Incidenten är löst och stängd. Root cause verifierades, remediation implementerades, testades och deployades, och produktägaren har manuellt verifierat och godkänt det återställda systemet. Sprint 1 är slutförd.

## Evidence

- Den felaktiga API-runtimekonfigurationen hade tomma integrationsvärden medan repositoryts avsedda Compose-konfiguration rapporterade dem som satta.
- Kalenderns named volume fanns kvar men var inte monterad i den felaktigt deployade API-containern.
- En read-only databaskopia gav `integrity=ok` och innehöll 39 kalenderhändelser samt 2 importposter.
- Efter remediation svarade health, kalendervecka, importhistorik och Theme API med HTTP 200. Kalendern returnerade befintliga händelser/importer och integrationslänkarna var åter konfigurerade.
- 99 frontendtester, production build, 207 API-tester och 32 Sentinel-tester passerade.

## Changes

Deploymenten återkopplades till repositoryts befintliga runtimekonfiguration och kalender-volume. Kalendermodulen och dess persistens ingår åter i API/Web-builden. Ett nytt smalt Settings-ansvar tillför ett allowlistat `GET/PUT /api/v1/settings/theme`, en dedikerad SQLite-volume och en frontend-ThemeProvider. Vid första körning kan befintligt lokalt tema seeda den ännu okonfigurerade serverinställningen; därefter är servervärdet auktoritativt.

SettingsStore initieras vid API-start så en otillgänglig settings-volume stoppar startup i stället för att döljas bakom generell health. Containerkatalogen ägs av API-användaren.

### Root cause

- Deployment utan repositoryts root-`.env` gjorde att integrationskonfigurationen saknades i API-containern.
- Den publicerade runtimekällan återanslöt inte den befintliga Calendar-persistencevolymen.
- Kalenderdata raderades aldrig; volymen och databasen var intakta.
- Theme state var enhetslokal i `localStorage` före remediation.
- Den nya Settings-volymen hade initialt fel ägarskap, vilket upptäcktes genom Theme API-verifieringen.

### Resolution

- Runtimekonfigurationen återställdes utan att secrets publicerades.
- Befintlig Calendar-persistence återanslöts och dess data blev åter läsbar.
- Ett gemensamt persistent Theme API och ThemeProvider infördes.
- Settings-persistensens ägarskap och eager startup-validering korrigerades.
- API och Web byggdes om, deployades och verifierades healthy.
- Produktägaren verifierade manuellt det återställda systemet och godkände resultatet.

## Security

Inga externa mediaåtgärder utfördes och ingen användardata raderades eller skrevs över. Konfigurationskontrollen redovisade endast `SET`/`EMPTY`, aldrig värden. Theme API accepterar endast tre deklarerade tema-ID:n och returnerar Problem Details för andra värden. Ingen autentiserad användarmodell finns ännu; inställningen är därför uttryckligen global för familjeinstallationen.

## Remaining work

Ingen incidentremediation återstår. Kalenderdata är bevarad och manuellt verifierad. Framtida funktioner, inklusive realtidssynk i BB-036, ligger kvar som separat backlogg och ingick inte i incidentåtgärden.

## Resumption

Utgå från [STATUS](../../STATUS.md), [BACKLOG](../../BACKLOG.md) och denna incidentrapport. Incidenten kräver ingen ytterligare återupptagning om inte samma konfigurations- eller persistencefel återkommer.
