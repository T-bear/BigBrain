# BigBrain – Arkitekturförslag

**Status:** Arkitekturbaslinje, godkänd i huvudsak  
**Målgrupp:** Arkitekter, utvecklare, DevOps och produktansvariga  
**Tidshorisont:** Flerårig utveckling

> **Proposed Sentinel amendment:** The accepted baseline below uses the historical term `Host Agent`. ADR 0002 proposes replacing that narrower concept with `BigBrain Sentinel` as the exclusive boundary for node-local system access. Until ADR 0002 and its prerequisite PKI, transport, policy, privilege, schema, and v1 threat-model decisions are accepted, this note records the proposed direction without retroactively changing the accepted baseline. ADR 0001 remains the accepted minimum rule that Web API must not directly control Docker.

## Syfte

BigBrain ska vara en modulär plattform för administration och automatisering av en hemserver. Plattformen ska kunna växa utan att kärnan blir beroende av enskilda produkter som Jellyfin, Sonarr eller Home Assistant.

BigBrain är ett control plane ovanpå Debian, inte en egen Linuxdistribution. Begreppet komplett operativsystem är en produktvision, inte ett mål om att bygga eller underhålla en egen distribution, pakethanterare eller drivrutinsmodell. En framtida installationsimage får byggas ovanpå Debian, men ändrar inte denna ansvarsfördelning.

## 1. Rekommenderad övergripande arkitektur

BigBrain börjar som en modulär monolit med tydliga domängränser och en separat Host Agent. Separata processer används i övrigt bara där privilegier, säkerhet eller uppmätt arbetslast kräver det.

```text
Webbläsare
    |
Reverse proxy / TLS
    |
BigBrain Web UI
    |
BigBrain API / Control Plane
    |
    +-- Identitet och behörighet
    +-- Modulregister
    +-- Dashboard, notifieringar och audit log
    +-- Schemalagda jobb och integrationsmoduler
    |
    +---------------- BigBrain Host Agent
    |                      |
    |                      +-- Docker Engine
    |                      +-- Operativsystem
    |                      +-- Diskar, sensorer och loggar
    |
    +-- Jellyfin / Sonarr / Radarr / Prowlarr / qBittorrent
    +-- Home Assistant / Ollama
    +-- Kamera- och skrivarsystem
    +-- PostgreSQL och fil- eller objektlagring
```

### Huvudkomponenter

**Control Plane** hanterar användare, moduler, konfiguration, policies, jobb, händelser, auditdata och plattformens samlade API.

**Host Agent** är en liten separat tjänst för privilegierade värdoperationer. Webb- och API-processen ska inte ha direkt åtkomst till Docker-socketen eller generell shellåtkomst.

**Module Runtime** registrerar installerade moduler, funktioner, navigation, behörigheter, hälsostatus, jobb och integrationer.

**Integration Adapters** översätter interna kontrakt till externa produkters API:er. Externa modeller får inte läcka in i plattformskärnan.

**Web UI** är ett gemensamt applikationsskal där moduler bidrar med navigation, vyer och dashboardkomponenter.

**Background Worker** kör synkronisering, övervakning, notifieringar och långvariga jobb. Den kan deployas tillsammans med API:t initialt men ska vara logiskt separerad.

BigBrain ska integrera, övervaka och orkestrera etablerade produkter. Plattformen ska inte återimplementera Jellyfin, Home Assistant eller övriga specialistverktyg.

### Logiska huvuddelar

| Huvuddel | Ansvar |
|---|---|
| `BigBrain.Api` | Versionssatt HTTP-API, requestvalidering, Problem Details, autentiserings- och auktoriseringsgräns samt anrop till applikationsfall |
| `BigBrain.Brain` | AI Orchestration Layer: modelloberoende orkestrering, capability discovery, planförslag, verktygsval, godkännandeflöden, kvoter och AI-spårbarhet |
| `BigBrain.Modules` | Förstapartsmoduler, modulregister, modulmanifest och modulernas domän- och applikationslogik |
| `BigBrain.HostAgent` | Smalt privilegierat gränssnitt mot Debian, Docker Engine och värdresurser |
| `BigBrain.Worker` | Schemalagda, köade och långvariga arbeten utan att skapa en separat distribuerad plattform |
| `BigBrain.Web` | React-applikation, moduldriven navigation, routes och dashboard |
| `BigBrain.Shared` | Små stabila kontrakt och tvärgående primitiver som faktiskt delas av flera huvuddelar |

`BigBrain.Brain` är inte ett allmänt dumpningslager eller namnet på all affärslogik. Där får endast kod som direkt hör till AI-orkestrering placeras: modelladapters, prompt- och kontextpolicy, strukturerade verktygskontrakt, planering, godkännandeflöden, AI-kvoter, timeout och AI-specifik telemetri. Modulernas domänlogik, vanliga API-use cases, datalagring, Host Agent-operationer, generella hjälpfunktioner och UI-logik får inte placeras där. Brain initierar endast åtgärder genom samma auktoriserade API- och applikationskontrakt som andra klienter.

## 2. Projektstruktur

```text
bigbrain/
├── src/
│   ├── BigBrain.Api/
│   ├── BigBrain.Brain/
│   ├── BigBrain.Modules/
│   ├── BigBrain.HostAgent/
│   ├── BigBrain.Worker/
│   ├── BigBrain.Web/
│   └── BigBrain.Shared/
├── deploy/
│   ├── compose/
│   ├── images/
│   ├── migrations/
│   └── examples/
├── docs/
│   ├── architecture/
│   ├── adr/
│   ├── api/
│   ├── operations/
│   ├── security/
│   └── modules/
├── tests/
│   ├── architecture/
│   ├── integration/
│   ├── end-to-end/
│   └── security/
└── tools/
```

Föreslagen intern struktur för en backendmodul:

```text
module/
├── Domain/
├── Application/
├── Infrastructure/
├── Api/
├── Contracts/
└── Tests/
```

Alla kataloger ska inte skapas i förväg. Strukturen är en målbild; tomma lager utan verkligt ansvar ökar komplexiteten.

## 3. Val av tekniker och motivering

### Backend

**ASP.NET Core på aktuell LTS-version av .NET.** Detta är ett fattat teknikbeslut. Plattformen ger stark typning, hög prestanda och bra stöd för API:er, bakgrundstjänster, dependency injection, OpenAPI, hälsokontroller och långlivade affärssystem. Nuvarande utvecklingsmiljö saknar .NET; verktygskedjan måste installeras eller containeriseras före implementation.

### Frontend

**React, TypeScript och Vite utan micro-frontends initialt.** Detta är ett fattat teknikbeslut. Kombinationen ger ett moget ekosystem för administrativa gränssnitt och återanvändbara dashboardkomponenter. Ett gemensamt UI-skal minskar distributions- och versionskomplexitet.

### Data

**PostgreSQL** lagrar konfiguration, metadata, behörigheter och auditdata. Varje modul äger sitt schema eller sina tabeller. JSONB används bara där flexibel metadata är motiverad.

Redis eller en message broker är inte grundkrav. De införs först när verkliga krav på distribuerad cache, låsning eller separata konsumenter finns.

Filer och stora objekt lagras på filsystem eller i S3-kompatibel objektlagring. Stora mediafiler, dokument, modeller och kamerainspelningar ska inte lagras som blobs i PostgreSQL.

### Kommunikation

REST/JSON används för API:er. Server-Sent Events eller SignalR används för realtidsstatus. Interna domänhändelser används i samma process initialt. Extern broker införs först när tjänster distribueras separat.

### Observability

OpenTelemetry, strukturerade loggar, Prometheus-kompatibla metrics och Grafana används för diagnostik. BigBrain presenterar en förenklad operatörsvy.

### Testning

Teststrategin omfattar enhetstester för domänlogik, integrationstester med riktiga beroenden, kontraktstester för externa API:er, end-to-end-tester för kritiska flöden och arkitekturtester för modulgränser.

## 4. Modulprincip

En modul representerar en sammanhållen produktförmåga, inte bara en teknisk katalog. Varje modul deklarerar:

- Unikt ID och semantisk version.
- Navigation och routes.
- Dashboard-widgets.
- API-endpoints, capabilities och behörigheter.
- Konfigurationsschema och hälsostatus.
- Bakgrundsjobb.
- Publicerade och konsumerade händelser.
- Dashboardkomponenter.
- Dataägarskap och migreringar.
- Externa beroenden.

### Regler

1. En modul får inte läsa eller skriva direkt i en annan moduls tabeller.
2. Kommunikation sker via publika kontrakt eller domänhändelser.
3. Kärnan innehåller inte produktspecifik integrationslogik.
4. Externa API-modeller översätts i respektive adapter.
5. En modul ska kunna inaktiveras utan plattformskrasch.
6. Otillgängliga tjänster ger degraderad funktion, inte systemkrasch.
7. Konfiguration valideras, versionshanteras och migreras.
8. Behörighet är explicit och deny-by-default.

### Moduldriven dashboard

Dashboarden läser navigation, routes och registrerade widgetdefinitioner från modulregistret och renderar dem dynamiskt utifrån aktiv modul, hälsostatus och användarens behörigheter. Widgetkontraktet ska beskriva identitet, placering, datakälla, behörighetskrav, laddnings- och feltillstånd samt en kompilerad förstapartskomponent.

Förstapartsmoduler levereras och kompileras tillsammans initialt. Godtycklig frontendkod från tredjepartsmoduler får inte laddas direkt i huvudapplikationen. Framtida externa moduler bidrar i första hand med deklarativ metadata och API-data; eventuell separat UI-yta måste isoleras och omfattas av en särskild säkerhetsmodell.

### Framtida externa moduler

Inget fullständigt pluginsystem implementeras i första versionen. Arkitekturen förbereds genom stabila interna gränser och följande framtida modell:

1. Ett versionssatt Module SDK definierar manifest, capabilities, API- och händelsekontrakt.
2. Modulmanifest signeras och verifieras innan registrering.
3. Plattformen kontrollerar SDK-, API- och plattformskompatibilitet före aktivering.
4. Externa moduler körs processisolerat i separata containers med minsta möjliga behörighet.
5. Kommunikation sker över autentiserade och versionssatta API:er, inte genom intern kodladdning eller direkt databasåtkomst.
6. En tydlig uppgraderings- och deprecationspolicy anger supportfönster, migreringsväg och avveckling.

Godtycklig tredjepartskod ska inte laddas i huvudprocessen.

### AI Orchestration Layer

AI är en plattformsförmåga i `BigBrain.Brain`, inte bara en vanlig integrationsmodul. Lagret är modelloberoende och kan använda lokal inferens, exempelvis Ollama, eller framtida externa modeller genom adapters.

AI Orchestration Layer får:

- Läsa capabilities som moduler uttryckligen publicerat i modulregistret.
- Sammanställa kontext och föreslå åtgärdsplaner.
- Begära att godkända kommandon startas via det vanliga versionssatta och auktoriserade API:t.
- Använda strukturerade, validerade verktygskontrakt med definierade argument och resultat.

Lagret får aldrig kringgå autentisering, auktorisering, bekräftelse, auditlogg, modulgränser eller Host Agent. Det får ingen fri shellåtkomst och ingen direkt åtkomst till Docker-socket, modulers databaser eller värdresurser. Destruktiva eller känsliga åtgärder kräver uttryckligt mänskligt godkännande nära exekveringstillfället; ett AI-förslag är aldrig ett godkännande.

Varje körning omfattas av resurs- och tokenkvoter, timeout, samtidighetsgränser, sandboxning av eventuell innehållsbearbetning och tydlig strukturerad loggning. Loggen ska koppla modell, modellversion, användare, capability, föreslagen plan, godkännande, API-anrop och resultat utan att okontrollerat lagra hemligheter eller känsligt innehåll. Modelladapters betraktas som opålitliga gränser, och modellutdata valideras alltid mot verktygskontraktet.

## 5. API-struktur

```text
/api/v1/system/*
/api/v1/auth/*
/api/v1/modules/*
/api/v1/notifications/*
/api/v1/audit/*
/api/v1/jobs/*
/api/v1/modules/{moduleId}/*
```

Exempel:

```text
GET  /api/v1/system/health
GET  /api/v1/modules
GET  /api/v1/modules/docker/containers
POST /api/v1/modules/docker/containers/{id}/actions/restart
GET  /api/v1/modules/media/library
GET  /api/v1/jobs/{jobId}
```

Principer:

- OpenAPI är det maskinläsbara kontraktet.
- API-versionering är explicit.
- Problem Details används för fel.
- Långvariga mutationer returnerar jobb-ID.
- Idempotency keys används för känsliga operationer.
- Listor stöder filtrering, sortering och cursor-baserad paginering.
- Correlation ID följer varje anrop.
- Destruktiva operationer modelleras som tydliga actions.
- Webbgränssnittet använder samma auktoriserade API som andra klienter.

GraphQL rekommenderas inte initialt. REST är enklare att dokumentera, säkra och felsöka för systemoperationer.

Händelser namnges och versionssätts:

```text
docker.container.status-changed.v1
monitoring.alert.raised.v1
media.download.completed.v1
module.health.changed.v1
```

En händelse är ett historiskt faktum. Händelser och kommandon ska inte blandas.

## 6. Säkerhet

BigBrain kan få kontroll över värddator, filer, kameror och hemautomation. Säkerhet är därför en kärnfunktion.

### Identitet och auktorisering

- OpenID Connect och OAuth 2.0.
- Authentik, Keycloak eller annan etablerad OIDC-leverantör.
- Lokal nödadministratör endast som dokumenterad fallback.
- Rollbaserad behörighet initialt.
- Resursbaserade policies för känsliga operationer.
- Multifaktorautentisering via identitetsleverantören.

Exempel på rättigheter:

```text
docker.containers.read
docker.containers.restart
docker.containers.delete
files.read
files.write
cameras.live.read
ai.models.manage
system.settings.manage
```

### Host Agent

Host Agent har ett litet och granskningsbart API, tillåter bara uttryckliga operationer, använder mTLS eller kortlivade tokens, exponeras aldrig mot internet, validerar varje kommando lokalt och auditloggar privilegierade operationer.

Direktmontering av `/var/run/docker.sock` i webb- eller API-containern ska undvikas. Docker-socketen motsvarar i praktiken rootåtkomst till värden.

### Övriga krav

- TLS vid fjärråtkomst.
- Docker secrets eller separat secret store.
- Kryptering av känsliga integrationsuppgifter.
- CSRF-skydd vid cookieautentisering.
- Strikt CORS och Content Security Policy.
- Rate limiting och skydd mot brute force.
- Fullständig audit log.
- Backup med verifierad återställning.
- Dependency scanning, container scanning och SBOM.
- Kontrollerade uppdateringar med rollback.

Hemnätverket är inte en säkerhetsgräns. Plattformen ska förutsätta komprometterade IoT-enheter, felkonfigurerade proxies, skadliga filer och stulna API-nycklar.

## 7. Docker-struktur

Core-profil:

- `reverse-proxy`
- `bigbrain-api`
- `bigbrain-web`
- `bigbrain-worker`
- `bigbrain-host-agent`
- `postgres`

Valfria profiler:

- `authentik` eller `keycloak`
- `prometheus`, `grafana` och `loki`
- `redis`
- `ollama`
- `jellyfin`
- `sonarr`, `radarr` och `prowlarr`
- `qbittorrent`

BigBrain ska kunna integrera med redan installerade tjänster och ska inte kräva att själv äga varje container.

### Nätverk

| Nätverk | Ansvar |
|---|---|
| `edge` | Reverse proxy och explicit exponerade tjänster |
| `application` | UI, API och worker |
| `data` | API eller worker och databaser |
| `management` | Control Plane och Host Agent |

Databaser, Docker API och interna administrationsportar publiceras inte externt.

Volymer separeras för applikationskonfiguration, databasdata, media, nedladdningar, dokument, AI-modeller, kamerainspelningar och backup. Sökvägar modelleras som namngivna lagringsplatser. UID, GID, åtkomst och backupklass dokumenteras per lagringsplats.

### Deployment

| Miljö | Rekommendation |
|---|---|
| Utveckling | Docker Compose |
| En hemserver | Produktionsanpassad Compose |
| Flera servrar | Agentbaserad arkitektur |
| Kubernetes | Endast vid verkliga fler-nods- eller organisationskrav |

Kubernetes är inte ett tidigt mål.

## 8. Skalning under flera år

### Teknisk utveckling

1. Börja med modulär monolit.
2. Mät innan komponenter bryts ut.
3. Separera worker när bakgrundslasten kräver det.
4. Flytta CPU- och GPU-jobb till dedikerade workers.
5. Inför broker först vid distribuerade konsumenter.
6. Lägg till flera Host Agents för flera servrar.
7. Bryt ut moduler endast av tydliga skalnings-, säkerhets- eller ägarskäl.

### Kontrakt

- Semantic Versioning för Module SDK.
- Definierad supportperiod för API-versioner.
- Automatiserade och testade databasmigreringar.
- Append-only-kontrakt inom en händelseversion.
- Dokumenterad deprecation med användningstelemetri.

### Organisation

- Dokumenterat modulägarskap.
- Architecture Decision Records.
- Kodägare och granskningsregler.
- Gemensamma kvalitetsgrindar.
- Standardiserad modulmall.
- Automatiserade kontrakts- och arkitekturtester.

Produktnivåerna separeras i obligatorisk kärna, förstapartsmoduler, externa integrationer, experimentella moduler och tredjepartsmoduler.

## 9. Risker och rekommendationer

| Risk | Konsekvens | Rekommendation |
|---|---|---|
| För bred produktvision | Många halvfärdiga moduler | Slutför kärnan och högst två referensmoduler först |
| Eget operativsystem för tidigt | Stor säkerhets- och underhållsbörda | Bygg ett control plane ovanpå etablerad Linux |
| Microservices från start | Onödig driftkostnad | Börja med modulär monolit |
| Direkt Docker-socket i API | Ett intrång kan ge full värdkontroll | Använd isolerad Host Agent |
| Dynamiska plugins för tidigt | Kompatibilitets- och säkerhetsproblem | Börja med förstapartsmoduler |
| Universellt datalager | Hård koppling och svåra migreringar | Kräv dataägarskap per modul |
| Obegränsad lokal AI | Resursbrist och osäkra verktygsanrop | Använd köer, kvoter, sandboxning och behörigheter |
| Otydligt filägarskap | Dataförlust och felaktiga rättigheter | Dokumentera lagring samt UID/GID-strategi |
| Automatisk containerhantering | Avbrott eller dataförlust | Kräv dry-run, bekräftelse, audit, backup och rollback |
| Föränderliga tredjeparts-API:er | Trasiga integrationer | Använd adapters, kontraktstester och kompatibilitetsmatris |
| Kamera- och dokumentdata | Integritetsproblem | Lokal behandling, retention och strikt åtkomst |
| Egen autentisering | Ökad risk för kontokapning | Använd etablerad OIDC-leverantör |
| Otestad backup | Permanent dataförlust | Automatisera backup och återkommande restore-test |

Rekommenderad första vertikal är en minimal plattformskärna med System-modul och moduldriven dashboard. Identitet, Host Agent, Docker och Jellyfin tillkommer stegvis efter att kärnkontrakten har verifierats. Hela produktlistan ska inte levereras samtidigt.

### Architecture Decision Records

Väsentliga, långlivade eller svåråterkalleliga arkitekturbeslut dokumenteras som Architecture Decision Records under `docs/adr/`. Varje ADR ska minst innehålla status, datum, kontext, beslut, konsekvenser och övervägda alternativ. Ett accepterat ADR ändras inte retroaktivt; ett nytt ADR ersätter det tidigare beslutet med tydlig referens. Pull requests som ändrar en beslutad gräns ska länka till ett nytt eller uppdaterat beslutsförslag.

Föreslagen första ADR-lista:

- ADR-001: Control plane ovanpå Debian.
- ADR-002: Modulär monolit.
- ADR-003: Separat Host Agent.
- ADR-004: REST och versionssatta API:er.
- ADR-005: Moduldriven dashboard.
- ADR-006: AI Orchestration Layer.
- ADR-007: PostgreSQL.
- ADR-008: Ingen dynamisk tredjepartskod i huvudprocessen.

## 10. Roadmap för Sprint 1–10

Antagande: tvåveckorssprintar med ett testbart inkrement per sprint.

### Sprint 1 – Minimal körbar kärna

Sprint 1 får endast skapa:

- Repositorystruktur.
- `AGENTS.md` och arkitekturdokumentation.
- React-, TypeScript- och Vite-skelett.
- ASP.NET Core API-skelett.
- Ett enkelt modulregister i minnet.
- En System-modul.
- `GET /api/v1/system/health`.
- `GET /api/v1/modules`.
- En enkel moduldriven dashboard med platshållarwidgets.
- Docker Compose för den minimala kärnan.
- Health checks.
- Relevanta tester för API, modulregister och dashboardens första kärna.

Ingen databas, autentisering, Host Agent, Docker-socket, Jellyfin eller riktig AI-integration får implementeras i Sprint 1. Inga generella plugin-, broker- eller distribuerade infrastrukturlager ska föregripas.

**Resultat:** Minsta körbara vertikal som bevisar API, modulregistrering och moduldriven UI-komposition.

### Sprint 2 – Kontrakt och kvalitet

- Förfina modulmanifest, capabilities, routes och widgetkontrakt.
- Etablera OpenAPI, Problem Details och API-kontraktstester.
- Införa strukturerad loggning, konfigurationsvalidering och arkitekturtester.
- Skriva och acceptera de första ADR:erna.

**Resultat:** Stabil förstapartskontraktbas utan extern infrastruktur.

### Sprint 3 – Identitet och säkerhetsgrund

- Integrera OIDC, policies, audit och secrets.
- Inför säkerhetsheaders, rate limiting och sessionspolicy.

**Resultat:** Autentiserad plattform med spårbara administrativa åtgärder.

### Sprint 4 – Persistens och operativ grund

- Inför PostgreSQL och modulägt schema för kärnmetadata.
- Etablera migreringar, backupprincip och verifierat restore-test.
- Inför auditdatastruktur för kommande privilegierade funktioner.

**Resultat:** Kontrollerad persistens och återställningsbar kärndata.

### Sprint 5 – Host Agent read-only

- Definiera agentprotokoll och säker registrering.
- Läs systemmetrics och hantera offline-status.
- Begränsa och auditlogga agentoperationer.

**Resultat:** Säker read-only-övervakning av en värd.

### Sprint 6 – Docker read-only

- Lista containers, images, volumes och networks.
- Visa hälsa, resurser och begränsade loggar.
- Verifiera att endast agenten når Docker-socketen.

**Resultat:** Dockeröversikt utan muterande operationer.

### Sprint 7 – Kontrollerade Docker-operationer

- Inför start, stop och restart med finmaskig behörighet.
- Inför asynkrona jobb, idempotens, felåterhämtning och audit.

**Resultat:** Säker och spårbar containerhantering.

### Sprint 8 – Dashboard, jobb och notifieringar

- Bygg modulär dashboard och realtidsstatus.
- Inför varningsregler och notifieringsabstraktion.
- Säkerställ responsivitet och grundläggande tillgänglighet.

**Resultat:** Sammanhållen operatörsvy.

### Sprint 9 – AI-kontrakt och första externa integration

- Definiera Brain-gräns, modelladapter och strukturerade verktygskontrakt utan autonom exekvering.
- Implementera förslag och godkännandeflöde mot ofarliga test-capabilities.
- Bygg en begränsad read-only Jellyfin-adapter som referens för externa tjänster.
- Verifiera kvoter, timeout, loggning och att auktoriseringsgränsen inte kan kringgås.

**Resultat:** Säker, begränsad referens för AI-orkestrering och extern integration.

### Sprint 10 – Härdning och första release

- Genomför end-to-end-, säkerhets- och prestandatester.
- Inför dependency- och containerskanning samt SBOM.
- Skapa releaseartefakter, dokumentera installation och genomför pilot.

**Resultat:** Begränsad men produktionsmässig första version.

Efter Sprint 10 rekommenderas följande ordning: Sonarr, Radarr och Prowlarr; qBittorrent; filer och dokument; Home Assistant; Ollama; kameror; 3D-skrivare; DnD; multi-host; externt Module SDK.

## Beslutad arkitekturbaslinje

1. **Produktgräns:** BigBrain är ett control plane ovanpå Debian, inte en egen Linuxdistribution.
2. **Arkitekturstil:** Systemet börjar som modulär monolit med separat Host Agent.
3. **Backend:** ASP.NET Core på aktuell .NET LTS.
4. **Frontend:** React, TypeScript och Vite utan micro-frontends initialt.
5. **AI:** AI är en säker plattformsförmåga i `BigBrain.Brain` och använder endast auktoriserade, strukturerade verktygskontrakt.
6. **Dashboard:** Navigation, routes och widgets drivs av modulregistret; förstaparts-UI kompileras tillsammans.
7. **Externa moduler:** Arkitekturen förbereds för ett versionssatt och isolerat Module SDK, men inget fullständigt pluginsystem byggs i första versionen.
8. **Data:** PostgreSQL är planerad primär databas med dataägarskap per modul, men införs inte i Sprint 1.
9. **Privilegierad åtkomst:** Endast Host Agent får i framtiden nå Docker Engine och värdoperationer.
10. **Deployment:** Docker Compose används för utveckling och single-host; Kubernetes är inget initialt krav.
11. **Säkerhet:** Deny-by-default, finmaskiga rättigheter, audit, secrets, verifierad restore och säkerhetsskanning är releasekrav när respektive förmåga införs.
12. **Leveransomfång:** Sprint 1 är uttryckligen begränsad till den minimala körbara kärnan ovan.
