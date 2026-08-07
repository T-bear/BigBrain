# Sprint 1 UX bugfix deployment

> Detta är en sanerad GitHub-version. Containeridentiteter, interna adresser, råloggar, privata sökvägar och hemligheter har utelämnats.

## Metadata

- Datum: 2026-08-07
- Scope: BigBrain API + Web
- Relaterad commit: `5669284774e7386c5947fc39e3f2796cee60e7c8`
- Berörda backloggposter: BB-030, BB-031, BB-034 och BB-035

## Status

Deploymenten underkändes efter produktägarens test på grund av konfigurations-, kalender- och temaregressioner. Remediation är deployad men produktägarens nya manuella verifiering väntar; se [incidentrapporten](../../incidents/sprint-1-deployment-regression-20260807.md). Sprint 1 är inte slutverifierad.

## Evidence

- En ren export av den publicerade committen användes som test-, build- och deploymentkälla.
- Frontend: 91 tester godkända och production build godkänd.
- Backend: 198 API-tester och 32 Sentinel-tester godkända.
- Compose-konfiguration, dokumentationsverifiering och diffkontroll godkändes.
- Dokumentationsverifieringen omfattade 82 Markdown-filer och 36 unika BB-ID:n före denna rapports tillkomst.
- API och Web blev healthy med restart count 0 och svarade HTTP 200 efter deployment.

## Changes

Deploymenten publicerade de redan implementerade fixarna för samlade Dashboardinställningar, responsiv Download Control, konservativ upptäckt av snarlika inköpsvaror samt förbättrad läsbarhet för ”Ofta köpt”. Shopping Lists jämförelsemodell finns i både API och Web, varför API återskapades först och Web därefter.

Endast BigBrain API och BigBrain Web återskapades. Sentinel, Jellyfin, qBittorrent, Sonarr, Radarr, Prowlarr och FlareSolverr behöll sina befintliga containeridentiteter och körstatus. Ingen Sprint 2-funktionalitet ingick.

## Security

Inga externa mediatjänster, torrents, mediafiler eller externa tjänsters data muterades. Normalt API-startbeteende använde befintlig Shopping List-persistens utan schemaändring. Rapporten innehåller inga secrets, tokens, credentials, privata adresser, container-ID:n, interna identiteter eller råloggar.

## Remaining work

Produktägaren ska manuellt verifiera Dashboardinställningar på mobil, Download Controls layout och långa namn, Shopping Lists skrivvarianter och negativa matchningsfall samt ”Ofta köpt” i samtliga teman och relevanta interaktionstillstånd. BB-030, BB-031, BB-034 och BB-035 ska inte beskrivas som manuellt verifierade före återrapportering. Framtida realtidssynk ligger fortsatt separat i BB-036.

## Resumption

Utgå från [STATUS](../../../STATUS.md), [BACKLOG](../../../BACKLOG.md) och denna rapport. Minsta säkra nästa steg är produktägarens read-only manuella UI-verifiering följd av en separat dokumentations- och publikationspass; börja inte Sprint 2 inom verifieringspasset.
