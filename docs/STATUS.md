# BigBrain Status

## Projektstatus

- Version: `0.1.0-alpha`
- Senaste uppdatering: 2026-08-01
- Senaste commit före denna sprint: `c1358d6d89a3954175eb040dbfbf5149bf71f95f`
- Aktiv branch: `main`
- Senaste verifierade build: 2026-08-01, backend Release, BigBrain API-image och frontend production build OK

---

## Nuvarande sprint

Kort sammanfattning:

- Mål: fastställ exakt varför Jellyfin visades som `degraded` och korrigera endast en tydligt verifierad grundorsak i BigBrain.
- Definition of Done: hela anropskedjan verifierad, minsta säkra korrigering genomförd, runtime och samtliga build-/testgrindar gröna.
- Resultat: klart. Jellyfin och Media overall rapporterar nu `online` genom både API och frontendproxy.

---

## Implementerat

- Modulärt ASP.NET Core-API och React/Vite-dashboard.
- Modulregister och versionssatta API:er för System, Docker och Media.
- Sentinel som separat process med autentiserat lokalt protokoll över Unix-socket.
- `Host.ReadUptime@1`, `Host.ReadCpu@1`, `Host.ReadMemory@1` och `Host.ReadDisk@1`.
- System Dashboard med uptime, CPU, minne och allowlistade diskar.
- Media Dashboard för Jellyfin, Sonarr, Radarr, Prowlarr och qBittorrent.
- Media Search, extern film-/seriesökning och säker posterproxy.
- Media Jobs med köer, filter, detaljvy, SSE/polling och verifierad Jellyfin-uppspelning.
- Förhandsgranskade och explicit bekräftade Sonarr-/Radarr-begäranden med kortlivad token och idempotens.
- Service Overview, media health score och regelbaserade insights.
- Prioriterad dashboard med Media Search och snabbval före detaljerad system- och Dockertelemetri.
- Minimerbara dashboardmoduler med tillgängliga kontroller och versionssatt lokal layout-persistence.
- Docker-, API- och Web-images med health checks för API och Web.
- FlareSolverr `3.5.0` som Compose-tjänst med healthcheck, konfigurerbar hostport/loggnivå och anslutning till medianätverket.
- Prowlarr indexer proxy `FlareSolverr` på `http://flaresolverr:8191/`, avgränsad med matchande tagg till Torrent[CORE].

---

## Kända problem

### Docker inventory unavailable

Status: Open

Beskrivning: Docker-endpoint och UI finns, men providern returnerar en tom lista och `Docker inventory requires Sentinel integration.` Ingen Docker-capability är implementerad.

### Jellyfin degraded

Status: Resolved 2026-08-01

Grundorsak: BigBrains Jellyfin-adapter använde `GET /Items/Latest` utan användar-ID. Jellyfin 10.11.11 svarade `400` och loggade `Guid can't be empty` från `UserLibraryController.GetLatestMedia`. De övriga fyra Jellyfin-anropen svarade `200`, men adapterns avsiktliga partial-failure-regel gjorde ett enda misslyckat delanrop till providerstatus `degraded`.

Åtgärd: adaptern använder nu det API-nyckelkompatibla `GET /Items` med rekursiv sökning, samma typfilter och explicit sortering på `DateCreated` fallande. Svaret normaliseras genom adapterns befintliga `Items`-mappning. Ingen Jellyfin-konfiguration eller data ändrades.

### Prowlarr update available

Status: Open

Beskrivning: Prowlarr är online men rapporterar att version `2.5.2.5491` finns tillgänglig.

---

## Senaste verifiering

Datum: 2026-08-01

Verifierat:

- Backend build: Release OK i .NET 10 SDK-container, 0 varningar och 0 fel.
- Backend tester: 158 OK, 0 failed, 0 skipped; dessutom 19 fokuserade mediaadaptertester OK.
- Frontend tester: 42 OK i 8 testfiler, inklusive modulordning, expandering, minimering, ARIA och lokal persistence.
- Frontend production build: OK.
- Jellyfin direkt: `/health` svarade `200 Healthy`; `System/Info` svarade med version `10.11.11`; den nya `/Items`-frågan svarade `200` med åtta sorterade poster.
- BigBrain API: `/api/v1/modules/media` rapporterade Jellyfin `online`, åtta nyligen tillagda poster och Media overall `online`.
- Frontendproxy: samma mediaendpoint via Web rapporterade Jellyfin och Media overall `online`.
- Providerregression: Jellyfin, Sonarr, Radarr, Prowlarr och qBittorrent rapporterade samtliga `online`; provideranropen svarade `200`.
- Mobil verifiering: senast 2026-07-31, 390 × 844 viewport utan horisontell scroll eller överlappande kontroller; sökningen visas i första vyn och bottennavigationen täcker inte innehåll.
- Compose build: API-imagen byggdes om i production/Release och startades med korrigeringen. Full build av Sentinel och Web kördes senast 2026-07-31.
- Compose-konfiguration: senast verifierad 2026-07-31 med Sentinel, API, Web och FlareSolverr.
- Runtime: Sentinel, API, Web och FlareSolverr kör; API, Web och FlareSolverr är healthy.
- FlareSolverr: `/health` svarar på hostport 8191 och från Prowlarr via `http://flaresolverr:8191/health`.
- Prowlarr: FlareSolverr-proxytest och `Test All Indexers` OK; fem av fem indexers giltiga.
- Torrent[CORE]: Cloudflare-challenge upptäckt och löst via FlareSolverr med efterföljande `200 OK`.
- API: health, System, Docker och Media svarar.
- Frontend: svarar och renderar BigBrain-applikationen.
- Regression: befintliga Jellyfin-, Sonarr-, Radarr-, Prowlarr- och qBittorrent-containrar är fortsatt running; Jellyfin är container-healthy.

---

## Runtime

- System: Healthy; uptime, CPU, minne och `BigBrain Storage` verifierade.
- Media: Online; Jellyfin, Sonarr, Radarr, Prowlarr och qBittorrent online. Health score är `68`/`actionRecommended` på grund av separata provider-healthvarningar, inte providerstatus.
- Docker: Unavailable i BigBrain; inventory-capability saknas.
- Sentinel: Running.
- API: Healthy.
- Web: Healthy.
- FlareSolverr: Healthy; nåbar från Prowlarr på det externa medianätverket.
- Prowlarr: Online; FlareSolverr-proxy aktiv för Torrent[CORE].
- Senast verifierad: 2026-08-01.

---

## Nästa sprint

Rekommenderat nästa mål: autentisering och auktorisering för Control Plane, avgränsat som en separat säkerhetssprint med eget arkitekturbeslut och Definition of Done.

Övriga verifierade öppna kandidater, utan inbördes prioritering:

1. Docker inventory via Sentinel.
2. Persistent audit och idempotens för mediabegäranden.
3. Drag-and-drop för användarstyrd modulordning.
4. PWA update-flöde med service worker och tydlig användarkontroll.

---

## Teknisk skuld

- Hostname och temperature samlas inte in.
- Docker inventory och samtliga Dockeråtgärder saknas.
- Autentisering, auktorisering, användare och generell auditlogg saknas.
- Mediabegärandetokens och idempotensresultat lagras endast i minnet.
- Browser-E2E och verkliga integrationstester mot mediatjänster saknas.
- Jellyfin-dashboardens delanropsfel loggar HTTP-status men inte ett säkert operation-ID per endpoint, vilket gjorde det misslyckade delanropet svårare att identifiera. Ingen bredare observabilityändring gjordes i denna sprint.
- Dashboardlayouten lagras endast lokalt per webbläsare och saknar användarbunden synkronisering.
- Drag-and-drop för modulordning och PWA auto-update är inte implementerade.
- Brain, Worker och databas är inte implementerade.
- Cloudflare-challenges kan vara intermittenta; första Torrent[CORE]-testet nådde 60 sekunders timeout, medan fyra efterföljande försök lyckades på cirka 15–16 sekunder.

---

## Senaste Codex-session

- Datum: 2026-08-01
- Syfte: diagnostisera och, vid verifierat BigBrain-fel, korrigera Jellyfin `degraded`.
- Resultat: inkompatibelt `/Items/Latest`-anrop ersatt med en API-nyckelkompatibel, sorterad `/Items`-fråga; Jellyfin och Media overall är verifierat `online`.
- Commitmeddelande: `fix(media): restore Jellyfin recently added status`.
- Nästa rekommenderade steg: autentisering och auktorisering i en separat säkerhetssprint.
