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

- Arbete: etablera `docs/STATUS.md` som enda källa för aktuell implementationsstatus.
- Klart: nuläget har verifierats mot kod, tester, Git, Compose och körande API/frontend.
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
- Runtime: Sentinel, API och Web kör; API och Web är healthy.
- API: health, System, Docker och Media svarar.
- Frontend: svarar och renderar BigBrain-applikationen.

---

## Runtime

- System: Healthy; uptime, CPU, minne och `BigBrain Storage` verifierade.
- Media: Degraded; Jellyfin degraded, övriga fyra providers online.
- Docker: Unavailable i BigBrain; inventory-capability saknas.
- Sentinel: Running.
- API: Healthy.
- Web: Healthy.
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

---

## Senaste Codex-session

- Datum: 2026-07-30
- Syfte: etablera en levande och verifierad projektstatus.
- Resultat: `docs/STATUS.md` skapad; inga kodändringar gjorda.
- Commit: ingen commit skapad; baserad på `ce05f0c323ae78d2b08efd8791b019e7353089dc`.
- Nästa rekommenderade steg: låt produktägaren besluta nästa sprint och uppdatera endast förändrade statussektioner när den avslutas.
