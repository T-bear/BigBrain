# BigBrain Status

## Projektstatus

- Version: `0.1.0-alpha`
- Senaste uppdatering: 2026-07-30
- Senaste commit: `ce05f0c323ae78d2b08efd8791b019e7353089dc`
- Aktiv branch: `main`
- Senaste verifierade build: 2026-07-30, Release och samtliga Compose-images OK

---

## Nuvarande sprint

Kort sammanfattning:

- Arbete: konfigurera och verifiera FlareSolverr som indexer proxy i Prowlarr.
- Klart: Prowlarr använder FlareSolverr för Torrent[CORE]; Cloudflare-challenge och samtliga indexertester är verifierade.
- Återstår: inget inom sprintens avgränsning.

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
- Docker-, API- och Web-images med health checks för API och Web.
- FlareSolverr `3.5.0` som Compose-tjänst med healthcheck, konfigurerbar hostport/loggnivå och anslutning till medianätverket.
- Prowlarr indexer proxy `FlareSolverr` på `http://flaresolverr:8191/`, avgränsad med matchande tagg till Torrent[CORE].

---

## Kända problem

### Docker inventory unavailable

Status: Open

Beskrivning: Docker-endpoint och UI finns, men providern returnerar en tom lista och `Docker inventory requires Sentinel integration.` Ingen Docker-capability är implementerad.

### Jellyfin degraded

Status: Investigating

Beskrivning: Jellyfin är nåbar och konfigurerad men rapporterar `degraded` eftersom delar av dashboarddata inte kunde hämtas vid senaste runtimekontrollen.

### Prowlarr update available

Status: Open

Beskrivning: Prowlarr är online men rapporterar att version `2.5.2.5491` finns tillgänglig.

---

## Senaste verifiering

Datum: 2026-07-30

Verifierat:

- Backend build: Release OK i .NET 10 SDK-container.
- Backend tester: 157 OK, 0 failed, 0 skipped.
- Frontend tester: 37 OK i 8 testfiler.
- Frontend production build: OK.
- Compose production build: Sentinel, API och Web OK.
- Compose-konfiguration: giltig med Sentinel, API, Web och FlareSolverr.
- Runtime: Sentinel, API, Web och FlareSolverr kör; API, Web och FlareSolverr är healthy.
- FlareSolverr: `/health` svarar på hostport 8191 och från Prowlarr via `http://flaresolverr:8191/health`.
- Prowlarr: FlareSolverr-proxytest och `Test All Indexers` OK; fem av fem indexers giltiga.
- Torrent[CORE]: Cloudflare-challenge upptäckt och löst via FlareSolverr med efterföljande `200 OK`.
- API: health, System, Docker och Media svarar.
- Frontend: svarar och renderar BigBrain-applikationen.
- Regression: befintliga Jellyfin-, Sonarr-, Radarr-, Prowlarr- och qBittorrent-containrar är fortsatt running.

---

## Runtime

- System: Healthy; uptime, CPU, minne och `BigBrain Storage` verifierade.
- Media: Degraded; Jellyfin degraded, övriga fyra providers online.
- Docker: Unavailable i BigBrain; inventory-capability saknas.
- Sentinel: Running.
- API: Healthy.
- Web: Healthy.
- FlareSolverr: Healthy; nåbar från Prowlarr på det externa medianätverket.
- Prowlarr: Online; FlareSolverr-proxy aktiv för Torrent[CORE].
- Senast verifierad: 2026-07-30.

---

## Nästa sprint

Nästa sprint är inte beslutad i repositoryt. Verifierade öppna kandidater, utan inbördes prioritering:

1. Docker inventory via Sentinel.
2. Jellyfin diagnostics.
3. Autentisering och auktorisering.
4. Persistent audit och idempotens för mediabegäranden.

---

## Teknisk skuld

- Hostname och temperature samlas inte in.
- Docker inventory och samtliga Dockeråtgärder saknas.
- Autentisering, auktorisering, användare och generell auditlogg saknas.
- Mediabegärandetokens och idempotensresultat lagras endast i minnet.
- Browser-E2E och verkliga integrationstester mot mediatjänster saknas.
- Brain, Worker och databas är inte implementerade.
- Cloudflare-challenges kan vara intermittenta; första Torrent[CORE]-testet nådde 60 sekunders timeout, medan fyra efterföljande försök lyckades på cirka 15–16 sekunder.

---

## Senaste Codex-session

- Datum: 2026-07-30
- Syfte: konfigurera och verifiera FlareSolverr i Prowlarr.
- Resultat: Torrent[CORE] använder FlareSolverr och samtliga fem indexers passerar Prowlarrs test.
- Commit: `feat(prowlarr): configure FlareSolverr integration`.
- Nästa rekommenderade steg: felsök Jellyfins degraderade status.
