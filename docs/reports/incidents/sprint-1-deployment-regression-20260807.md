# Sprint 1 deployment regression

> Detta är en sanerad GitHub-version. Secrets, privata adresser, container-ID:n, råloggar och användardata har utelämnats.

## Metadata

- Datum: 2026-08-07
- Scope: Sprint 1 API/Web-deployment, kalenderpersistence, integrationskonfiguration och delat tema
- Ursprunglig deploycommit: `5669284774e7386c5947fc39e3f2796cee60e7c8`
- Backlogg: BB-030, BB-031, BB-034, BB-035, BB-037 och BB-038

## Status

Produktägaren underkände deploymenten. Root cause är verifierad och remediation är implementerad, automatiskt verifierad och deployad. Manuell produktägarverifiering väntar; Sprint 1 är fortsatt pågående.

## Evidence

- Den felaktiga API-runtimekonfigurationen hade tomma integrationsvärden medan repositoryts avsedda Compose-konfiguration rapporterade dem som satta.
- Kalenderns named volume fanns kvar men var inte monterad i den felaktigt deployade API-containern.
- En read-only databaskopia gav `integrity=ok` och innehöll 39 kalenderhändelser samt 2 importposter.
- Efter remediation svarade health, kalendervecka, importhistorik och Theme API med HTTP 200. Kalendern returnerade befintliga händelser/importer och integrationslänkarna var åter konfigurerade.
- 99 frontendtester, production build, 207 API-tester och 32 Sentinel-tester passerade.

## Changes

Deploymenten återkopplades till repositoryts befintliga runtimekonfiguration och kalender-volume. Kalendermodulen och dess persistens ingår åter i API/Web-builden. Ett nytt smalt Settings-ansvar tillför ett allowlistat `GET/PUT /api/v1/settings/theme`, en dedikerad SQLite-volume och en frontend-ThemeProvider. Vid första körning kan befintligt lokalt tema seeda den ännu okonfigurerade serverinställningen; därefter är servervärdet auktoritativt.

SettingsStore initieras vid API-start så en otillgänglig settings-volume stoppar startup i stället för att döljas bakom generell health. Containerkatalogen ägs av API-användaren.

## Security

Inga externa mediaåtgärder utfördes och ingen användardata raderades eller skrevs över. Konfigurationskontrollen redovisade endast `SET`/`EMPTY`, aldrig värden. Theme API accepterar endast tre deklarerade tema-ID:n och returnerar Problem Details för andra värden. Ingen autentiserad användarmodell finns ännu; inställningen är därför uttryckligen global för familjeinstallationen.

## Remaining work

Produktägaren måste manuellt verifiera tema på mobil/desktop, kalenderdata/importhistorik, integrationsstatus och samtliga tidigare Sprint 1-flöden. Ingen post får markeras Klar och Sprint 2 får inte startas före godkänd återrapportering.

## Resumption

Utgå från [STATUS](../../STATUS.md), [BACKLOG](../../BACKLOG.md) och denna incidentrapport. Minsta säkra nästa steg är produktägarens manuella read-only-verifiering; därefter uppdateras status och eventuell Sprint 1-stängning i en separat pass.
