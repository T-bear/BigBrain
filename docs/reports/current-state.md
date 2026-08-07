# BigBrain – nulägesinventering

> Historisk inventering från 2026-07-29. Den ska inte användas som aktuell runtime-status. Se [STATUS](../STATUS.md) och [Sprint 1-deploymenten](features/sprint-1/sprint-1-bugfix-deployment-20260807.md): Sprint 1, Calendar recovery och remediation är slutförda och manuellt godkända 2026-08-07.

Inventerad 2026-07-29 utifrån produktionskod och befintliga tester. Bedömningen gäller vad som finns i repositoryt, inte en verifiering mot en körande hemserver.

## 1–3. Funktionalitet och färdiggrad

### Implementerat och verkar komplett

- Ett ASP.NET Core-API, ett React/TypeScript-gränssnitt och ett gemensamt modulregister för System, Docker och Media.
- API-hälsa samt konsekventa Problem Details-svar för fel och okända routes.
- Mediadashboard med normaliserad status och begränsade listor från Jellyfin, Sonarr, Radarr, Prowlarr och qBittorrent. En leverantör kan fallera utan att övriga resultat försvinner.
- Mediasökning i befintligt bibliotek/Arr, extern film- och seriesökning, säkrad posterproxy och konfigurerbara länkar till mediatjänsterna.
- Samlad jobb-/kövy från Sonarr, Radarr och qBittorrent med filtrering, detaljvy, statusnormalisering, episodgruppering, polling/SSE och kontrollerad uppspelning via verifierad Jellyfin-träff.
- Begärandeflöde för film/serie: hämtning av tillåtna val, skrivskyddad förhandsgranskning och explicit bekräftelse som gör ett avgränsat POST-anrop till Sonarr eller Radarr. Bekräftelsen använder kortlivad token, idempotensnyckel och samtidighetsbegränsning.
- Responsiva laddnings-, tom-, degraderade- och feltillstånd samt mobil snabbnavigation.

### Delvis implementerat

- **System:** kontrakt, endpoint, modul och UI finns, men den registrerade providern returnerar alltid `Unavailable`; verkliga värdmätvärden saknas.
- **Docker:** kontrakt, endpoint, modul och UI finns, men providern returnerar alltid otillgänglig och en tom lista; verklig containerinventering saknas och inga Docker-åtgärder finns.
- **Sentinel:** separat bootstrap-process med konfigurerbar `/health`, versionsuppgift, strukturerad loggning och tomt capability-register finns. Den har inga värd-/Docker-capabilities, ingen Control Plane-kommunikation och ingår inte i nuvarande `compose.yaml`.
- **Persistens och säkerhet:** modulregister, mediebegärandetokens och idempotensresultat lagras endast i minnet. Autentisering, auktorisering, användare, auditlogg och databas finns inte.
- Mediafunktionerna är kodade och väl enhets-/komponenttestade, men verklig funktion beror på runtime-konfiguration, credentials och åtkomst till de fem externa tjänsterna. Denna inventering har inte kört ett integrationstest mot dem.

### Endast dokumenterat eller planerat

- `BigBrain.Brain`, Worker, Shared och ett fullt Sentinel/Host Agent-protokoll.
- PostgreSQL-baserad konfiguration/persistens, notifieringar, generell jobbmotor och audit.
- AI-orkestrering, automation, Home Assistant, externa moduler/plugins, observability-stacken och multi-node-stöd.
- README:s uppgift att *alla* mediaändringar är uppskjutna är inaktuell: Sonarr/Radarr-begärandeflödet finns i koden. Övriga mediatjänster är fortfarande skrivskyddade.

## 4. Användarvyer i frontend

Frontend är en sammanhängande dashboard med hashankare, inte separata klientroutes:

- **Hem / Server overview:** systemstatus, CPU, RAM, diskar, uptime, hostname och temperatur; visar i nuläget att Sentinel-integration saknas.
- **Docker overview:** tillgänglighet och containerlista; visar i nuläget att integrationen saknas.
- **Media / Tjänster:** total hälsa, tjänstekort, bibliotek/statistik, aktivitet och genvägar till konfigurerade tjänster.
- **Sök:** sökning i befintliga källor eller extern film-/seriesökning, poster/status och dialog för preview + bekräftad begäran.
- **Kö:** aktiva, pausade och slutförda mediajobb med filter, progress, detaljer och Jellyfin-uppspelningslänk när matchningen är verifierad.
- Mobilnavigationen länkar till **Hem**, **Sök**, **Kö** och **Tjänster**.

## 5. API-endpoints

| Metod | Endpoint | Funktion |
|---|---|---|
| GET | `/health` | Processhälsa för API-containern |
| GET | `/api/v1/system/health` | Versionssatt systemhälsa |
| GET | `/api/v1/modules` | Registrerade moduler och aktuell providerstatus |
| GET | `/api/v1/system/overview` | Systemmått; för närvarande `Unavailable` |
| GET | `/api/v1/docker/containers` | Dockerinventering; för närvarande otillgänglig/tom |
| GET | `/api/v1/modules/media` | Aggregerad mediadashboard |
| GET | `/api/v1/modules/media/service-links` | Tillåtna externa tjänstelänkar |
| GET | `/api/v1/modules/media/posters/{token}` | Proxy för validerad extern poster |
| GET | `/api/v1/modules/media/search?query=...` | Sökning i Jellyfin, Sonarr och Radarr |
| GET | `/api/v1/modules/media/lookup?query=...&mediaType=...` | Extern Arr-lookup för film/serie |
| GET | `/api/v1/modules/media/jobs` | Filtrerad, aggregerad jobb-/kölista |
| GET | `/api/v1/modules/media/jobs/events` | SSE-ström med jobbsnapshots |
| GET | `/api/v1/modules/media/jobs/{id}` | Jobbdetalj via opakt ID |
| GET | `/api/v1/modules/media/library-status` | Biblioteksstatus för extern identitet |
| GET | `/api/v1/modules/media/play/{id}` | Verifierad Jellyfin-uppspelningsmetadata |
| GET | `/api/v1/modules/media/add-options/series` | Tillåtna Sonarr-val |
| GET | `/api/v1/modules/media/add-options/movie` | Tillåtna Radarr-val |
| POST | `/api/v1/modules/media/requests/preview` | Validerad, skrivskyddad förhandsgranskning |
| POST | `/api/v1/modules/media/requests/confirm` | Idempotent bekräftelse till Sonarr/Radarr |

Sentinel har dessutom en separat, konfigurerbar `GET /health`; processen exponeras inte av nuvarande Compose-konfiguration.

## 6. Tester och täckning

- **Backend – `BigBrain.Api.Tests`:** HTTP-kontrakt och Problem Details; modulregister; mediahälsopoäng; adaptermappning och sanerade fel; sökning/lookup; posterproxy och tjänstelänkar; jobbnormalisering, deduplicering, cache, filter, Jellyfin-matchning och leverantörsisolering; preview/confirm, tokenvalidering, idempotens och exakt tillåtna Arr-POST; arkitekturgränser och skydd mot läckage av intern konfiguration.
- **Sentinel – `BigBrain.Sentinel.Tests`:** bootstrap-hälsa, DI-livslängd, deterministisk version, konfigurationsvalidering, avsaknad av management-endpoints/adapters och begränsad konfiguration.
- **Frontend – Vitest/Testing Library:** system- och Docker-tillstånd/polling; mediadashboard och widgetordning; sökning, lookup, posterfallback, tjänstelänkar och mobilnavigation; jobbfilter, progress, SSE/polling och uppspelning; hela requestdialogens preview/confirm-, fokus- och felhantering.
- Det finns inga verkliga end-to-end-tester mot de externa media tjänsterna, ingen browser-E2E-svit och inga tester av en Control Plane–Sentinel-integration. Testerna kördes inte under denna skrivskyddade inventering.

## 7. Prioriterade nästa steg

1. Koppla System och Docker till den beslutade Sentinel-gränsen så dashboardens två grundvyer visar verkliga data.
2. Lägg till autentisering och auktorisering, särskilt framför Sonarr/Radarr-bekräftelsen.
3. Persistenta och auditera mediebegäranden/idempotensresultat så de överlever API-omstart.
4. Lägg till avgränsade integrationstester mot testinstanser av Jellyfin/Arr/qBittorrent och ett kritiskt browserflöde.
5. Synkronisera README och modulmetadata med det implementerade mediebegärandeflödet.
