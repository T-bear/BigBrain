# BigBrain Current State

Inventerad 2026-07-30 från produktionskod, tester, Docker Compose och en
read-only-kontroll av den körande lokala installationen. Rapporten beskriver
nuvarande implementation och skiljer mellan implementerad funktion, faktisk
runtimekonfiguration och framtida arkitektur.

## Projektöversikt

BigBrain är idag ett webbaserat control plane för en privat Debian-baserad
hemserver. Den implementerade lösningen består av en modulär ASP.NET Core-backend,
ett React-gränssnitt och en separat Sentinel-process för read-only åtkomst till
operativsystemets metrics.

Den faktiska anropskedjan för värdmetrics är:

```text
Webbläsare
  -> BigBrain Web/nginx
  -> BigBrain API
  -> autentiserat Sentinel-protokoll över Unix-socket
  -> BigBrain Sentinel
  -> avgränsade read-only OS-källor
```

API:t läser inte Linux-metrics direkt och har inte Docker-socket, hostens
rotfilsystem, `/proc` eller `/sys` monterat. Sentinel är den enda
BigBrain-komponenten som läser operativsystemets metrics.

### Huvudprojekt och lösning

`BigBrain.slnx` innehåller sex .NET-projekt:

| Projekt | Nuvarande ansvar |
|---|---|
| `BigBrain.Api` | Versionssatt HTTP-API, mediaadapters och Control Plane-klient för Sentinel |
| `BigBrain.Modules` | Modulregister samt kontrakt för System, Docker och Media |
| `BigBrain.Sentinel.Contracts` | Delade, typade Sentinel-protokollkontrakt |
| `BigBrain.Sentinel` | Separat Sentinel-tjänst, protokoll, policyvalidering och metricsinsamling |
| `BigBrain.Api.Tests` | API-, modul-, media-, provider- och arkitekturtester |
| `BigBrain.Sentinel.Tests` | Sentinel-, collector-, konfigurations- och integrationstester |

Frontendprojektet `BigBrain.Web` ligger utanför .NET-lösningen och använder React
19, TypeScript 7, Vite 8, Vitest och Testing Library.

Det finns idag inga projekt för Brain, Worker, databas eller generell
notifieringsmotor.

### Containers och deployment

Repositoryts `compose.yaml` bygger och startar:

- `sentinel`: separat, icke-privilegierad .NET-container;
- `api`: ASP.NET Core API på hostport `18080`;
- `web`: statisk Vite-build i unprivileged nginx på hostport `13000`.

Den verifierade installationen kör alla tre containrarna. API och Web rapporterade
healthy vid inventeringen. Sentinel körde och levererade ett giltigt snapshot.

Deploymenten använder lokalt provisionerade certifikat och proof-nycklar under
den Git-ignorerade katalogen `.sentinel/`. Sentinel och API delar named volume
`sentinel-runtime` för Unix-socketen. Sentinel får dessutom två read-only
bind-mounts:

- serveridentitet och publik proof-nyckel;
- en explicit allowlistad storage-probe för `BigBrain Storage`.

API och Web har ingen Docker-socket eller generell host-mount.

## Dashboard

Frontend är en enda responsiv dashboardsida. Navigation sker med vanliga länkar
och hashankare; React Router eller separata klientroutes används inte.

Sidhuvudet visar:

- BigBrain-branding;
- rubriken `Server overview`;
- den statiska etiketten `Sprint 2`.

Sidomenyn hämtas från modulregistret och visar System, Docker och Media med aktuell
providerstatus. Mobilvyn har snabbnavigation till Hem, Sök, Kö och Tjänster.

### System status

Systempanelen visar:

- statusbadge;
- CPU usage med procent, progressbar och antal logiska processorer;
- RAM usage med procent, progressbar samt använt och totalt minne;
- ett separat progresskort per returnerad allowlistad disk;
- System uptime i dagar, timmar och minuter;
- Hostname;
- Temperature;
- tidpunkten `Last updated`;
- eventuella säkra varningar från Sentinel.

I verifierad runtime var System `Healthy`. Hostname visas fortfarande som
`Unavailable` och Temperature som `Unavailable`, eftersom dessa fält inte samlas
in av nuvarande capabilities.

### Docker overview

Dockerpanelen visar status och antingen containerlista eller ett kontrollerat
unavailable-tillstånd. Nuvarande provider är
`UnavailableDockerInventoryProvider`, så användaren ser:

- status `Unavailable`;
- kortet `Integration not connected`;
- meddelandet `Docker inventory requires Sentinel integration.`;
- ingen containerlista och inga Dockeråtgärder.

Detta påverkas inte av att Dockercontainrar faktiskt körs på servern; BigBrain har
ännu ingen capability som inventerar dem.

### Media

Mediaområdet heter `Your media ecosystem` och innehåller följande paneler:

- `Mediatjänster`: snabbval till Jellyfin, Radarr, Sonarr, Prowlarr och
  qBittorrent. Ej konfigurerade webblänkar visas inaktiverade.
- `Hitta film och serier`: sökning i externa Sonarr/Radarr-resultat eller i
  befintliga Jellyfin/Sonarr/Radarr-bibliotek.
- `Media Jobs`: samlad kö-/jobbvy med filter för Active, Importing, Available,
  Failed och All.
- `Media health`: regelbaserad health score, sammanfattning, status och
  insamlingstid.
- `BigBrain Insights`: prioriterade informations-, varnings- och kritiska
  observationer.
- `Jellyfin`: Movies, Series, Episodes och Streams.
- `Sonarr`: Series, Monitored, Missing och Queue.
- `Radarr`: Movies, Missing, Queue och Upgrades.
- `Prowlarr`: Indexers, Online, RSS och Failures.
- `qBittorrent`: Active, Paused, Download och Free space.
- `Sonarr queue` och `Radarr queue`: titel, status och progress.
- `Recently added`: senaste objekt från Jellyfin.
- `Download details`: aktiva, pausade/stoppade och slutförda torrents.
- `Health warnings`: normaliserade varningar från Sonarr, Radarr och Prowlarr.

Den verifierade runtimebilden var:

- Media overall: `degraded`;
- health score: `78`, nivå `good`;
- media requests: enabled;
- alla fem browserlänkar: enabled;
- Jellyfin: konfigurerad och `degraded`;
- Sonarr: konfigurerad och `online`;
- Radarr: konfigurerad och `online`;
- Prowlarr: konfigurerad och `online`;
- qBittorrent: konfigurerad och `online`.

Dessa statusvärden är ögonblicksbilder och ändras med de externa tjänsternas
tillstånd.

## System Metrics

Frontend hämtar `GET /api/v1/system/overview` direkt vid sidladdning och därefter
var femte sekund. Överlappande systemanrop blockeras. Endpointen hämtar ett nytt
Sentinel-snapshot; frontend visar senaste lyckade värdet om en senare refresh
misslyckas.

### Uptime

- Datakälla: `Environment.TickCount64` i Sentinel, omräknat till sekunder.
- Sentinel capability: `Host.ReadUptime@1`.
- Publikt endpoint: `GET /api/v1/system/overview`.
- Uppdateringsintervall: cirka 5 sekunder från frontend.
- Status vid inventeringen: available.
- Visade fält: formaterade dagar, timmar och minuter.
- API-fält: `uptimeSeconds`.

### CPU

- Datakälla: två direkta, read-only läsningar av aggregatraden och CPU-raderna i
  `/proc/stat`.
- Sentinel capability: `Host.ReadCpu@1`.
- Sampling: cirka 250 millisekunder mellan två counter-snapshots.
- Publikt endpoint: `GET /api/v1/system/overview`.
- Uppdateringsintervall: cirka 5 sekunder från frontend.
- Status vid inventeringen: available.
- Visade fält: `usagePercent`, progressbar och `logicalProcessorCount`.
- Sentinel returnerar även `sampleWindowMilliseconds`; detta visas inte i UI.

### Memory

- Datakälla: direkt, read-only parsing av `MemTotal` och `MemAvailable` i
  `/proc/meminfo`.
- Sentinel capability: `Host.ReadMemory@1`.
- Publikt endpoint: `GET /api/v1/system/overview`.
- Uppdateringsintervall: cirka 5 sekunder från frontend.
- Status vid inventeringen: available.
- Visade fält: `usagePercent`, progressbar, `usedBytes` och `totalBytes`.
- API:t innehåller även `availableBytes`, men Systemkortet visar inte detta värde
  separat.

`usedBytes` beräknas som `totalBytes - availableBytes`.

### Disk

- Datakälla: .NET `DriveInfo` mot varje explicit konfigurerad `sentinelPath`.
- Sentinel capability: `Host.ReadDisk@1`.
- Publikt endpoint: `GET /api/v1/system/overview`.
- Uppdateringsintervall: cirka 5 sekunder från frontend.
- Status vid inventeringen: available.
- Visade fält per disk: neutral `displayName`, `usagePercent`, progressbar,
  `usedBytes`, `totalBytes` och `availableBytes`.
- API-fält per disk: `filesystemId`, `displayName`, `totalBytes`, `usedBytes`,
  `availableBytes` och `usagePercent`.

Nuvarande allowlist innehåller exakt:

- `filesystemId`: `bigbrain`;
- `displayName`: `BigBrain Storage`.

Rå host-sökväg, mount point, enhets-ID och filsystemstyp returneras inte till API
eller UI. Vid runtimekontrollen rapporterade den allowlistade lagringen ungefär
2,68 TiB totalt. Värdet är dynamiskt.

### Systemstatus och partiella fel

Snapshot blir `available` när uptime, CPU, memory och samtliga allowlistade diskar
är available. API mappar detta till `Healthy`. Partiella resultat blir
`Degraded`; ett enskilt metricfel tar inte bort giltiga syskonvärden. Om Sentinel
inte kan nås faller providern tillbaka till ett säkert `Unavailable`-svar utan
direkt hoståtkomst.

## Övervakade tjänster

### BigBrain-tjänster

- Sentinel körs och används för System Metrics.
- API körs och har process-health samt versionssatta endpoints.
- Web körs och proxyar `/api/` och `/health` till API.

### Externa mediatjänster

BigBrain har implementerade och i aktuell runtime konfigurerade adapters för:

- Jellyfin;
- Sonarr;
- Radarr;
- Prowlarr;
- qBittorrent.

API-containern är ansluten till det externa Docker-nätverket `bigbrain_default`
och når tjänsterna genom deras containernamn och HTTP-API:er. Ett fel hos en
provider ska ge degraderat resultat utan att övriga providers försvinner.

Containern `homepage` kör också på samma server och nätverk, men BigBrain har
ingen adapter, modul, panel eller övervakning för den.

BigBrain övervakar inte Sentinel, API och Web som en generell tjänstelista.
Systemets sidebarstatus och container-health är separata mekanismer.

## API

### Endpoints som den renderade frontenden använder

| Metod | Endpoint | Returnerar |
|---|---|---|
| GET | `/api/v1/modules` | System-, Docker- och Media-moduler med status, routes, widgets och capabilities |
| GET | `/api/v1/system/overview` | Aktuellt SystemOverview från Sentinel |
| GET | `/api/v1/docker/containers` | Explicit unavailable DockerInventory med tom lista |
| GET | `/api/v1/modules/media` | Aggregerad mediaöversikt, health score, insights och providerdata |
| GET | `/api/v1/modules/media/service-links` | Tillåtna browserlänkar till de fem mediatjänsterna |
| GET | `/api/v1/modules/media/search` | Sökresultat från befintliga Jellyfin/Sonarr/Radarr-källor |
| GET | `/api/v1/modules/media/lookup` | Extern film-/seriesökning via Sonarr och Radarr |
| GET | `/api/v1/modules/media/jobs?limit=50` | Normaliserade jobb från Sonarr, Radarr och qBittorrent |
| GET | `/api/v1/modules/media/play/{id}` | Verifierad Jellyfin-uppspelningsmetadata för tillgängligt media |
| GET | `/api/v1/modules/media/add-options/series` | Tillåtna Sonarr-val inför en begäran |
| GET | `/api/v1/modules/media/add-options/movie` | Tillåtna Radarr-val inför en begäran |
| POST | `/api/v1/modules/media/requests/preview` | Validerad preview och kortlivad request-token |
| POST | `/api/v1/modules/media/requests/confirm` | Idempotent bekräftelse som lägger till i Sonarr eller Radarr |
| GET | `/api/v1/modules/media/posters/{token}` | Proxyad, validerad posterbild; URL:en kommer i sökresultat |

Alla API-fel som frontendklienten hanterar använder Problem Details med en stabil
`code` där applikationen behöver särskild feltext.

### Implementerade endpoints som nuvarande UI inte anropar aktivt

- `GET /health`: API-processens Docker-health.
- `GET /api/v1/system/health`: versionssatt API-hälsa.
- `GET /api/v1/system/sentinel/ping`: Control Plane-diagnostik mot Sentinel.
- `POST /api/v1/system/sentinel/read-system-metrics`: rått Sentinel-snapshot via
  Control Plane.
- `GET /api/v1/modules/media/jobs/events`: SSE-ström för mediajobb. En
  frontendhelper finns, men nuvarande `MediaJobs` använder polling och anropar
  inte helpern.
- `GET /api/v1/modules/media/jobs/{id}`: jobbdetalj; klienthelper finns men den
  renderade vyn använder redan detaljerna i jobbsnapshoten.
- `GET /api/v1/modules/media/library-status`: biblioteksstatus för en extern
  identitet.

## Frontend

Frontendens rotkomponent är `App`. Den renderar:

- moduldriven sidebar;
- Systempanelen;
- Dockerpanelen;
- `MediaDashboard`;
- `MobileNavigation`.

Mediaområdet delas vidare upp i:

- `MediaServiceLinks`;
- `MediaSearch` med bibliotekssökning, extern lookup och `MediaRequestDialog`;
- `MediaJobs` och `MediaJobCard`;
- ett kompilerat `WidgetRegistry` som ordnar mediawidgets i hero, insights,
  service overview, activity och details.

Routing är hashbaserad ankarnavigation. Det finns ingen React Router-konfiguration
och ingen separat sida per modul.

### Polling och API-anrop

- System overview: initialt och var 5:e sekund.
- Media overview: initialt, manuellt via Refresh, vid återgång till synlig flik
  och var 45:e sekund när dokumentet är synligt.
- Media Jobs: initialt, vid återgång till synlig flik och var 12:e sekund när
  dokumentet är synligt.
- Modulregister, Dockerinventering och mediatjänstlänkar: en gång när respektive
  komponent monteras.
- Sökning, lookup, add-options, preview, confirm och play-metadata: endast efter
  relevant användarinteraktion eller när ett spelbart jobb visas.

## Sentinel

### Capabilities och registry

När protokollet är aktiverat innehåller registry fem capabilities:

- `Host.ReadUptime@1`;
- `Host.ReadCpu@1`;
- `Host.ReadMemory@1`;
- `Host.ReadDisk@1`;
- `Inventory.ReadSnapshot@1`.

De fyra host-capabilities är read-only. `Inventory.ReadSnapshot@1` samlar dem i
ett gemensamt snapshot med per-sektion-status och säkra varningar.

### Snapshot

Control Plane begär en fast uppsättning sektioner och fält. Godtyckliga paths,
fields, capabilityversioner eller selectors accepteras inte.

Snapshotet innehåller:

- unikt snapshot-ID;
- node-ID;
- collection timestamp;
- status;
- uptime-, CPU-, memory- och disksektioner;
- säkra warnings.

Diskinsamlingen använder en konfigurerad allowlist. Tomma och dubbla ID:n, tomma
displaynamn, relativa paths och paths som inte finns avvisas vid startup.

### Transport och proof

- HTTP/2 med JSON över Unix domain socket.
- TLS 1.3 och ömsesidig certifikatkontroll.
- API verifierar exakt betrodd Sentinel-certifikatdata.
- Sentinel verifierar exakt betrodd Control Plane-certifikatdata.
- Varje snapshotrequest har message ID, node ID och 30 sekunders expiry.
- ECDSA/SHA-256-proof binds till capability, version och hash av de ordnade
  argumenten.
- Sentinel avvisar fel node, fel key ID, ogiltig/utgången proof och replayat
  message ID.
- HTTP-klienten har fem sekunders timeout.

### Health

Sentinels bootstrap-health finns på konfigurerad `/health` och returnerar endast
processstatus, version, capability count och kontrolltid. Den exponeras inte som
en hostport i Compose.

Protocol Ping finns på `/sentinel/v1/ping` över den autentiserade Unix-socketen.
Control Plane exponerar dessutom den versionssatta diagnostikendpointen
`/api/v1/system/sentinel/ping`.

## Docker

### BigBrain-containrar

| Container/service | Nätverk | Kommunikation |
|---|---|---|
| `web` | Compose default | HTTP till `api:8080`; hostport `13000` |
| `api` | Compose default och externa `bigbrain_default` | HTTP från Web, Unix-socket till Sentinel, HTTP till mediatjänster; hostport `18080` |
| `sentinel` | Compose default | Autentiserad HTTP/2 över Unix-socket; ingen hostport |

### Volymer och mounts

- `sentinel-runtime`: named volume delad av API och Sentinel för socketen.
- `.sentinel/server`: read-only identitetsmaterial i Sentinel.
- `.sentinel/client`: read-only identitetsmaterial i API.
- `.sentinel/storage-probes/bigbrain`: read-only storage-probe i Sentinel.

### Externa containrar verifierade vid inventeringen

- `jellyfin`;
- `sonarr`;
- `radarr`;
- `prowlarr`;
- `qbittorrent`;
- `homepage`.

De fem första har BigBrain-mediaadapters. `homepage` har det inte. Externa
containrar definieras inte av repositoryts `compose.yaml` och hanteras inte av
BigBrain.

## Tester

Senaste fulla verifieringen efter Host.ReadDisk-implementationen gav:

- Release-build: godkänd med 0 varningar och 0 fel;
- `BigBrain.Api.Tests`: 125 godkända tester;
- `BigBrain.Sentinel.Tests`: 32 godkända tester;
- backend totalt: 157 godkända tester;
- frontend: 37 godkända tester i 8 testfiler;
- frontend production build: godkänd.

Backendtesterna täcker bland annat:

- API-kontrakt, Problem Details och modulregister;
- mediaadapters, health score, sökning, lookup, posterproxy, jobb och requests;
- idempotens, preview/confirm och providerisolering;
- Sentinel-till-SystemOverview-mappning;
- arkitekturgränser och skydd mot intern konfigurationsläcka.

Sentineltesterna täcker bland annat:

- bootstrap-health och konfigurationsvalidering;
- uptime, CPU-, memory- och diskberäkningar;
- diskallowlist, ogiltiga värden och isolering av diskfel;
- capability registry och requestvalidering;
- autentiserad integration över Unix-socket;
- avvisning av obetrott Control Plane-certifikat.

Frontendtesterna täcker bland annat:

- System- och Dockerstatus samt systempolling;
- riktiga CPU-, RAM-, disk- och uptimevärden;
- mediawidgets och degraderade tillstånd;
- sökning, requestdialog, mediajobb och mobilnavigation.

Det finns ingen browserdriven end-to-end-svit och inga automatiserade
integrationstester mot de verkliga Jellyfin/Arr/qBittorrent-instanserna.

## Funktioner som användaren faktiskt har idag

- Öppna en responsiv BigBrain-dashboard i webbläsaren.
- Se aktuell Systemstatus och senaste insamlingstid.
- Se verklig host-uptime formaterad i dagar, timmar och minuter.
- Se aktuell CPU-belastning och antal logiska processorer.
- Se använt och totalt RAM samt procentuell användning.
- Se verklig kapacitet, användning och ledigt utrymme för allowlistad
  `BigBrain Storage`.
- Få kontrollerade unavailable/degraded-tillstånd när Sentinel eller enskilda
  metrics fallerar.
- Se modulstatus för System, Docker och Media.
- Se sammanställd status för Jellyfin, Sonarr, Radarr, Prowlarr och qBittorrent.
- Se regelbaserad Media health score och konkreta insights.
- Se bibliotekstal, indexerstatus, köer, torrents, hastighet, ledigt utrymme,
  warnings och nyligen tillagda media.
- Söka efter film och serier i befintliga bibliotek.
- Göra extern film- och seriesökning genom Sonarr och Radarr.
- Välja rotmapp, kvalitetsprofil, monitoring och eventuell sökning efter add.
- Granska en preview och uttryckligen bekräfta att en film läggs till i Radarr
  eller en serie i Sonarr.
- Se samlade mediajobb med status, progress, ETA, episoder och providerdetaljer.
- Filtrera jobb och visa fler resultat.
- Öppna en verifierad Jellyfin-länk för ett tillgängligt objekt.
- Använda konfigurerade snabbval till externa mediatjänsters egna webbgränssnitt.
- Manuellt uppdatera mediaöversikten.

## Funktioner som ännu inte finns

- Dockerinventering och visning av verkliga containrar.
- Start, stop, restart, delete, exec eller andra Dockeroperationer.
- Hostname, operativsystem, arkitektur och temperatur från Sentinel.
- Load average, nätverk, processer, sensorer eller generell hostinformation.
- Historiska systemmetrics, tidsseriegrafer och trendanalys.
- Notifieringar och alerts med leveranskanaler.
- Autentisering, användare, roller och auktorisering.
- Databas eller annan varaktig BigBrain-persistens.
- Central auditlogg och Sentinel audit-spool.
- AI-assistent och `BigBrain.Brain`.
- Automation Engine och generell bakgrundsworker.
- Home Assistant, kamera-, skrivare-, UPS- eller GPU-integration.
- Multi-node/fleet-hantering.
- Dynamiska eller externa plugins.
- Generell övervakning av godtyckliga tjänster eller containrar.
- Browserbaserade end-to-end-tester.

## Teknisk skuld

- ADR 0005 är modifierad men inte committad; ADR 0006–0009 är fortfarande
  otrackade Proposed-utkast.
- Sentinel saknar den lokala metadata-audit/audit-spool som beskrivs i
  arkitekturdokumentationen.
- Full PKI-livscykel, automatisk certifikatrotation och central audit ingestion
  finns inte.
- Sentinel-certifikat, proof-nycklar och storage-probe provisioneras manuellt
  utanför Git.
- Compose väntar på att Sentinel-processen startar men har ingen separat
  Sentinel-healthcheck.
- Docker Compose-projektnamnet och den synliga frontendbadgen heter fortfarande
  `bigbrain-sprint1` respektive `Sprint 2` trots senare implementationer.
- README och `docs/reports/current-state.md` beskriver fortfarande System Metrics
  som otillgängligt och är inaktuella jämfört med implementationen.
- Hostname, operating system och architecture finns kvar i publika
  `SystemOverview`, men mappas alltid till `Unavailable`.
- Temperature finns kvar i `SystemOverview` och UI men är alltid `null`/
  `Unavailable`.
- Frontend har en SSE-helper för mediajobb, men den renderade jobbkomponenten
  använder 12-sekunders polling.
- Media request preview, idempotensresultat och övrig runtime-state ligger i
  minnet och överlever inte API-omstart.
- Media requests är implementerade utan användarautentisering eller audit.
- Det finns inga automatiserade integrationstester mot den verkliga
  mediamiljön och ingen browser-E2E-svit.
