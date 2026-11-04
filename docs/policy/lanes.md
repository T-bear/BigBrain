# Leveransspår

Detta dokument är aktivt när både PR #15 och riskpolicyns ersättning
(PR #16) finns i `main`. Fram till dess gäller befintlig riskpolicy.

## STANDARD — Web-only

Endast UI-/teständringar i tillåtna filer under `src/BigBrain.Web/src/`,
`src/BigBrain.Web/public/icons/`, `src/BigBrain.Web/public/manifest.webmanifest`
och `src/BigBrain.Web/index.html`. All annan diff följer API+Web-regeln nedan eller `STRIKT`.
API-klienter, API-kontrakt, Dockerfile, nginx, Compose, databas, rättigheter,
nätverk och funktioner som kräver nytt API ingår inte. Sökvägstestet kan inte
i sig bevisa API-kompatibilitet; nya API-behov får aldrig döljas som UI-ändring.

Efter grön PR-CI startar ägaren en `tmux`-session på BigBrain
(`tmux new -s bigbrain-web-deploy`) och kör där ett enda kommando:
`scripts/deploy-web-only.sh <PR-nummer>`. Kommandot är godkännandet.
Det kontrollerar diff och Compose före merge, bygger en kandidat medan gamla
Web kör, mergar via `gh`, inväntar grön main-CI och byter enbart Web.
Samma kommando kan återupptas om PR är mergad men CI dröjde.
Föregående image sparas som `bigbrain-sprint1-web:previous` och återställs
vid misslyckad start/hälsa. Kontrollera HTTP `/` och
`/icons/bigbrain-32.png`. Git main backas inte automatiskt vid rollback.
Ägarens iPhone/PWA-acceptans sker efter deployment. PR-beskrivningen räcker;
ingen separat granskningsrapport för kommande Web-only-leveranser.

## STANDARD — API+Web (T2, review checkpoint 2026-11-01)

Ägarens andra och sista Skollunch-PR inför detta spår; första är
[PR #25](https://github.com/T-bear/BigBrain/pull/25), `283060d`.
**Inte aktivt i produktion:** denna PR får endast publiceras för oberoende granskning.
Ingen merge eller deployment ingår. Skriptet är **T2** (nivå 2, drift/dataåterhämtning)
och kräver oberoende granskning av exakt kandidat före acceptans/användning.
Efter acceptans i main ersätter spåret oktober-gatewayens paket/approvals för denna
avgränsade API+Web-leverans. Det ger inga nya OS-/Docker-/sudo-rättigheter.

[deploy-standard.py](../../scripts/deploy-standard.py) tar aktuell accepterad main-SHA.
Utan `--execute` görs endast läsande preflight. Efter separat giltig deploymentauktorisering
kör operatören i befintlig `tmux`-session, med befintlig Docker-/backupåtkomst:

```sh
python3 scripts/deploy-standard.py --revision MAIN_SHA --execute
```

Skriptet hittar nuvarande Compose-filer, projektkatalog och env-filer från containrarnas
Compose-labels; vanlig projekt-`.env` används annars. Den verifierade kombinationen
äldre API-gateway/vanlig Web-Compose kan rekonstrueras i minnet enligt kontrollen nedan.
Valfri `--compose /absolute/compose.yaml` väljer en befintlig vanlig Compose-fil.
Ingen manuellt upplöst JSON, förbyggd image eller nytt root-godkännande behövs.
Config-hash måste matcha körande API/Web. Okända/saknade inputs eller konflikter ger stopp;
den skyddade gateway-filen behöver inte läsas om rekonstruktionen verifieras fullt ut.

1. Kontrollera aktuell remote main, ren checkout, PR #25:s commit i ancestry och fyra
   gröna main-jobb. Båda runtime-revisionerna måste vara tillgängliga Git-commits i
   accepterad ancestry. Container- och image-etiketter måste matcha; en äldre Web-image
   med ärvd nginx-etikett kräver dessutom exakt image-bunden BuildKit-proveniens för
   BigBrain-baslinjen. Varken en flyttbar tagg eller OCI-etiketten ersätter detta bevis.
   Varje apps diff bedöms från dess egen runtime-revision: Web-källor som inte kopieras
   av API:s Dockerfile räknas endast mot Web-baslinjen, och omvänt för API:s källträd.
   Gemensamma/okända bygg- och konfigurationsinputs undantas inte. API-deltan tillåter
   `SchoolMealService.cs`, borttagna `MealPlannerStore.SchoolMeals.cs`, `SchoolMealSourceClient.cs`
   och `SchoolMeals.cs` under `src/BigBrain.Api/MealPlanner/`. Web tillåter endast
   `src/BigBrain.Web/src/api.ts`, `src/BigBrain.Web/src/meal-planner/SchoolLunch.tsx` och
   `src/BigBrain.Web/src/meal-planner/SchoolLunch.test.tsx`. Dessa fem tillägg är reviewarbete
   enligt avsnittet nedan. Dokumentation/tester/driftskript/CI tillåts som tidigare.
   Därmed blockeras fortfarande nya Finance-/BB-158-, schema-, rättighets- och nätverks-
   ändringar. Sökvägstestet ersätter aldrig semantisk schema-/säkerhetsreview.
2. Bygg **både API och Web medan originalen kör** från ett `git archive` av exakt SHA.
   Lås varje Dockerfile-bas till hämtad registry-digest före build; sätt OCI-revision
   på båda images och API-buildarg. Spara Git-tree, arkivhash, basdigests, byggfiler
   och slutliga image-ID:n privat. Arbetskopians oregistrerade filer ingår aldrig.
   Underlaget gör källor/baser reproducerbara; byte-identiska builds påstås inte.
3. Återanvänd oförändrad sexdatabasbackup: skrivskyddad SQLite onlinebackup,
   integrity/FK/schema, hash och isolerad readback. Finance-data ingår inte.
   Färsk verifierad backup krävs; ofullständiga kopior bevaras.
4. Bevisa start **och ren omstart** för tidigare och nya API/Web-images före bytet.
   Kör ordinarie image-entrypoint och faktisk non-root-UID med `network none`, utan
   publicerade portar eller produktionsmounts. API använder separata kopior av de sex
   backupdatabaserna; externa funktioner inaktiveras endast i proben, Finance-lagring
   är tom tmpfs. Webs upstreamnamn pekar på loopback. Kontrollera hälsa, ren stop och
   oförändrat kopieschema efter båda starterna. Originalbackupen hashkontrolleras igen.
   Ett misslyckat prov stoppar leveransen medan originaltjänsterna fortsätter köra.
5. Verifiera main/runtime/data igen; byt API före Web med digestbundna images och
   `--no-deps --no-build --pull never`. Delat Web-only-lås och fryst snapshot tillåter
   endast API/Web och befintliga externa volymer/nätverk. Verifiera hälsa, HTML/ikon,
   runtimekonfiguration, DB-identitet/schema och oförändrade övriga containrar.

Vid känt start-/hälsofel sker automatisk image-rollback, **aldrig databasrestore eller
Git-rollback**. En identifierad ersättningscontainer med exitkod 1 (även efter avbruten
omstartsloop) får bytas tillbaka. Originalcontainern kräver fortfarande ren stop;
främmande container, OOM, schema-/datadrift, osäkert Docker-avbrott eller misslyckad
rollback kräver manuell kontroll. Senare hushållsskrivningar bevaras. Beständig journal
spärrar osäkra omförsök; inga tidigare paket, approvals, backuper eller körningar raderas.
De unikt skapade probcontainrarna stannar; privata kopior/kvitton bevaras under en privat katalog med läge 0700.
Endast kopiornas interna kataloger/filer får skrivläge för probens UID. Inga produktions-
rättigheter, flaggor, nätverk, schema eller Finance-/BB-158-resurser ändras.

T2-review av `c883e78` fann två **P1**: rollback nekade exitkod 1 och bygg-/omstartsprov
saknades i flödet. Den korrigerade diffen i samma PR behöver andra oberoende reviewvarvet.
Verifiering 2026-11-02: **171 offline-tester PASS**, inklusive de två felen, bevarade
skrivningar, låsta bygginputs, Compose-upptäckt och nekad release vid bygg-/probefel.
Riktade tester med verkliga lokala Git-historiker täcker olika API-/Web-baslinjer,
okända/ej accepterade revisioner och fortsatt stopp för förbjudna app-/bygg-/konfigurationsdeltan.
Samma test med accepterad mellanliggande Web-ändring reproducerar felet i `e3f5e3b`.
Verklig Compose-parser och dokumentvalidator PASS. CI kör drifttesterna i befintligt
backendjobb. Detta bevisar styrflödet med simulerad Docker och syntetisk SQLite/WAL;
verklig imagebuild, isolerad image-start och produktionsbyte är **NOT RUN** i PR-arbetet.

Kvarstår: andra T2-reviewvarvet och driftverifiering efter giltig acceptans/auktorisering.
Runtime-reparationer, okända overrides, legacy-bootstrap, ej återstartbar gammal image
eller annan appdelta ger stopp. Isolerade prov bevisar inte externa integrationers
funktion; källobservation och iPhone/PWA är PENDING. Daglig backup/restore-grinden för
schemaändringar kvarstår i [backupunderlaget](../operations/backup-restore/README.md).
All Finance-/BB-158-, schema-, privilegie- eller nätverksändring är utanför spåret.

### Avgränsad backuphjälpare — T2-checkpoint 2026-11-02

Nytt ägarauktoriserat uppdrag från `55ebaa9`: endast backupåtkomst, tre filer,
en PR och ett oberoende T2-reviewvarv. Ingen produktionsdatabasåtkomst, merge eller
deployment ingår. `backup_support.py` är oförändrad. Äldre formuleringar ovan om
ännu ej mergade #25/#26 är historik; båda ingår i denna accepterade startbaseline.

**Standard-preflight förblir läsande:** inga nya containrar, volymer, images,
kataloger eller backuper. Den verifierar fem volymer via daemonens `volume inspect`
(Name/Mountpoint, lokal driver utan options) mot API:s mounts, men läser inga
produktionsdatabasfiler från värden. Datainventory/schema/backup rapporteras
**DEFERRED / BLOCKED**, exitkod 2; detta är inte en godkänd full förkontroll.
Endast framtida separat auktoriserad `--execute` får genomföra hjälparsteget.

Hjälparen använder exakt
`python@sha256:b658b05ce9c4eb97ec6fc19424818a1f31993659c2b505a532f470e83807815d`,
`--pull never`, verifierad SQLite **3.53.4** och UID:GID **1654:1654**.
Nätverket är `none`; alla capabilities tas bort, `no-new-privileges` gäller,
minne/swap begränsas till 512 MiB, CPU till 1, processer till 32 och processen till
180 sekunder. Ingen Docker-socket, API-miljö, extra grupp, port eller Finance-volym
följer med. Image-ID och faktisk containerkonfiguration/mountlista verifieras före
start. Endast de fem befintliga volymerna monteras med `volume-nocopy`, på deras
verifierade absoluta källsökvägar så att oförändrad `backup.sources()` kan användas.
Alias, överlapp, saknad/extra mount och okänd volymidentitet ger stopp.

**Avgränsat undantag från skrivskyddad montering:** de fem källvolymerna måste
monteras skrivbara för att SQLite ska kunna skapa WAL/SHM när de saknas. Alla
originalanslutningar använder fortfarande `mode=ro` och `query_only`; ingen
`immutable=1`, rå originalkopiering, checkpoint eller journal-mode-ändring görs.
UID 1654 äger originalfilerna: skrivbar montering innebär därför en kvarstående
filsystemsskrivrisk, inte ett tekniskt bevis att processen bara kan skriva sidofiler.
Risken avgränsas av låst image/kod, kort livslängd, exakt volymlista, samma non-root-UID,
frånvaro av nätverk/socket och kontroller före/efter. Katalogernas device/inode/UID/GID/
mode och databasernas motsvarande identitet samt schema jämförs inne i hjälparen.
Katalogerna måste vara 1654:0/0755; databasfiler och kvarvarande sidofiler måste vara
1654:1654 med 0600 eller 0644. Nya sidofiler skapas med umask 0077 och är åtkomliga
för API:s samma UID. Normal borttagning av sidofiler vid SQLite-close är tillåten;
levande databasers innehåll, storlek och mtime är inte oföränderlighetskrav.
Finance är helt utanför mounts/backup. Befintlig Media acquisition-databas ingår i
det redan accepterade sexdatabaskontraktet; inga BB-158-funktioner körs eller ändras.

**Beständighet och ägarskap:** varje engångshjälpare skapar exklusivt
`/tmp/bigbrain-standard-backup` med ägare 1654:1654 och mode 0700 i sitt eget
beständiga containerlager, inte tmpfs eller värdens delade `/tmp`. Ingen annan
process/tjänst delar katalogen; ingen oskyddad staging på värden används.
Det skrivbara containerlagret behövs för denna privata destination; källvolymernas
ägare och katalogrättigheter ändras aldrig. `run_backup()` skriver sina vanliga
`backups/<backup-id>/` med 0700-kataloger, 0600-filer och ofullständigt manifest före
kopiering. `verify_set()` verifierar alla sex databaser, hash, integrity/FK/schema
och isolerad återläsning. Konsistens gäller per databas, inte atomärt över sex.

Containern avslutas och bevaras utan `--rm`. `docker cp` **utan `-a`** kopierar
resultatet till operatörens separata
`~/.local/state/bigbrain-standard/run-<id>/bigbrain-standard-backup-<id>/output/`.
Värdoperatören måste ha UID 1000; kopian ägs av denne. Kataloger måste vara 0700,
filer 0600, utan symlänkar/hårdlänkar; filerna och katalogerna synkas till disk.
Värden kör oförändrad `verify_set()` igen och jämför manifesthash/runtimeidentitet.
`receipt.json` anger den relativa backupvägen. Endast denna verifierade kopia används
i de befintliga isolerade start-/omstartsproven. Källcontainerns privata backup och
journal bevaras även efter kopiering, utan automatisk städning eller restore.

Hjälparens namn/intent journalförs före skapande, ID före start. Avbrott, krasch,
timeout, kopieringsfel eller misslyckad verifiering ger **MANUAL_REVIEW_REQUIRED**;
vid hård krasch kan hjälparjournalen sluta på STARTED och manifestet på INCOMPLETE.
Containerlagret, partiella värdkopior och journaler bevaras. Ingen automatisk omkörning
eller återställning tillåts. Nya kortlivade hjälpare verifierar datans identitet/schema
före/efter byte och inför rollback; de omfattas av samma säkerhetsgräns. Inget API/Web-
byte får passera utan verifierad backup/readback och befintliga start-/omstartsprov.
Detta ändrar inte backupgrinden för schemaändringar eller kravet på separat drifttillstånd.

Verifieringsunderlag för denna checkpoint anges i PR med exakt SHA/tree. Det opt-in-
styrda syntetiska Docker-testet använder enbart egna nya volymer och ovanstående image;
ordinarie CI kör offline-felen och markerar Docker-testet SKIP när det inte beställts.
Första syntetiska försöket stoppade på fixturens GID: `docker cp -a` till container
använde containeranvändarens grupp. Fixturen korrigerades till uttryckliga tar-metadata
utan `-a`; produktionsgränsen 1654:0/0755 ändrades inte. Ingen sådan ägarjustering finns
i produktionsflödet. Verifiering 2026-11-02: **179 offline-tester PASS**, ett Docker-test
SKIP i standardkörningen; separat opt-in-körning av samma Docker-test **PASS** med
WAL utan sidofiler, samtidig skrivning under onlinebackup, SIGKILL mitt i backup,
kvarvarande sidofilers behörigheter och API-ekvivalent fortsatt läs-/skrivåtkomst.
Både fullständig och ofullständig backup/journal kopierades till UID 1000 och
bevarades efter att testhjälparna tagits bort; korrupt/ofullständig kopia avvisades.
Verklig hjälparorkestrering kördes endast mot fem nybildade syntetiska volymer.
Dokumentvalidator PASS. Alla testägda containrar/volymer städades efter kontrollen.
Produktions-WAL, verklig backup och deployment är **NOT RUN**.

### Skrivskyddad kompatibilitetskontroll 2026-11-02

**De tre kompatibilitetskontrollerna PASS med korrigerad kod; full release ännu BLOCKED.**
Båda tjänsterna är running/healthy med oförändrade image-ID:n och starttider. Ingen
root-autentisering, build, omstart, databaskontroll eller deployment utfördes.

| Tjänst | Aktuell image | Container- och image-etikett för revision |
| --- | --- | --- |
| API | `sha256:c26a862197ca5d7aca1c87d7cd5bb097f33959c8f303e97a5ae3ff1d160cae44` | `a68c0247ea7a78ad5fadf6400ed8bc2e22ed35c0` |
| Web | `sha256:4f3b10e760541a7d0dbbef4460cbbb0848cfbf1b5288745d2c4f480234885a4b` | `c4edb2fc1a693a7bdae4ab2bbc1c7fe63622290f` — **upstream, inte BigBrain** |

Vid `46aec78` var första verkliga stoppet `ComposeProvenanceMissing`: API pekar på det arkiverade
gateway-spårets skyddade `release.compose.json`; Web pekar på vanlig `compose.yaml`.
API-filen är inte läsbar med nuvarande åtkomst. Web-konfigurationens renderade hash
matchar Web men **inte API**; att bara ange Web-filen med `--compose` löser inte grinden.
Ursprungen förklaras av API:s gateway-start 29 oktober och Web-only-starten 30 oktober.
Korrigeringen läser Webs vanliga Compose, kräver dess aktuella hash och ersätter endast
API:s image, revisionslabel och kompletta miljö med verifierad runtime. Dubbla/saknade
miljövärden nekas; dollar-escaping bevaras. Därefter måste **båda** renderade config-hasharna
matcha runtime och JSON-modellen klara en oförändrad Compose-omrendering. Användare,
startkonfiguration, mounts och nätverk ändras inte. Även befintliga Finance-/BB-158-flaggor
bevaras exakt; vanlig Compose ensam innehåller andra värden. Default-preflight skriver inga
snapshots. Vid separat auktoriserad `--execute` sparas baseline/proveniens och release/
rollback privat utanför Git, i katalog 0700 med exklusiva filer 0600; inga hemligheter skrivs ut.

`A /host-timesync` är den deklarerade katalogmonteringen av `/run/systemd/timesync`, inte
en påvisad applikationsreparation. Endast exakt denna A-rad i API får passera efter kontroll
av deklarerad och faktisk bind-mount, source/target, read-only, rprivate, inga källsymlänkar
och samma katalog/inode/enhet på värd och i container. Identiteten kontrolleras igen före
och efter byte. C/D-rader, underkataloger, liknande namn och andra oväntade ändringar nekas.
Befintliga volatila kataloger (`/tmp`, `/var/cache/nginx`, `/var/run`, `/run`) avgränsas med
exakta sökvägsgränser, inklusive katalogernas egen metadata. Ingen generell mount-exkludering införs.

Webs `source`/`url`/`title` anger nginx upstream; revisionsetiketten är en verifierad
[nginx-commit](https://github.com/nginx/docker-nginx-unprivileged/commit/c4edb2fc1a693a7bdae4ab2bbc1c7fe63622290f).
BigBrains GitHub svarar att denna commit saknas; lokala objekt/refs/reflogs saknar den också.
Web-Dockerfile och Web-only-skriptet sätter ingen egen OCI-revision, så basimagens etikett
ärvs. Den tidigare rapporterade revisionen identifierar alltså inte BigBrain-källorna.
Aktuell image har kandidat-taggen `8029bb2853a0913670e6dd4ff67c72f0ea439199`, som finns
i accepterad main, men en flyttbar tagg är inte byggproveniens och används inte som reservbaslinje.
Efterföljande skrivskyddad kontroll hittade ett slutfört lokalt BuildKit-bygge och dess
SLSA-proveniensbilaga som binder **exakt ovanstående Web-image** till BigBrain-commit
`8029bb2853a0913670e6dd4ff67c72f0ea439199`, i accepterad main. Koden kräver en entydig
match från befintlig lokal default-builder, rätt Web-Dockerfile/repository, komplett ren
Git-SHA och samstämmiga revisions-/repositoryvärden i byggpost och dess digestvalda bilaga.
Saknat, motsägande, okänt eller flertydigt underlag stoppar; beviset omkontrolleras före byte.
Detta är lokalt byggunderlag, inte en extern signerad attest eller garanti om byte-identisk
ombyggnad; gamla bilagans beroendekompletthet är inte fullständig. Acceptans/ancestry och
applikationsdelta kontrolleras fortfarande av `git_guard()`; runtime-etiketter ändras aldrig.

Verklig skrivskyddad delverifiering med korrigerad kod: rekonstruktion/båda hasharnas match,
BuildKit-bindningen och API/Webs writable-layer-/mountkontroller **PASS**. Webs Nginx-konfig
matchar accepterat Git-innehåll och är läsbar; inga oväntade beständiga Web-ändringar finns.
Återstartbarhet bevisas fortfarande först av det separat auktoriserade isolerade start-/
omstartsprovet. Full preflight/release är inte godkänd: #25/#26 är ännu inte mergade,
oberoende T2-review samt alla backup-/rollback-/dataintegritetsgrindar kvarstår.
Kontrollen ger inget merge- eller deploymenttillstånd.

Det [äldre paket-/approval-/workerspåret är fryst och arkiverat på plats](../operations/runbooks/approved-operations.md#fryst-och-arkiverat-skollunchspår--2026-11-01).

### Avgränsad SDK-korrigering — T2-checkpoint 2026-11-02

Baseline `7db1e23e956ab963097bc6d7872db206bb9057a7`. Den auktoriserade STANDARD-körningen
`run-185ce6e2e4ec48aabc17b68b182d10e5` stoppades i API-bygget med `PRE_SWITCH_FAILED`
(BuildKit `3llmez5rq88vyo3phyo3olqr8`, restore exitkod 155). Den flytande SDK-taggen
`10.0` gav **10.0.403**, medan accepterad `global.json` kräver **10.0.300/latestPatch**.
Enligt [Microsofts SDK-regler](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json#rollforward)
tillåter detta endast samma feature band, alltså 10.0.3xx. Enligt incidentunderlaget förblev
API/Web healthy; ingen produktionsbackup, tjänsteväxling eller databasrestore genomfördes.
Den tidigare körningen och dess bevis lämnas orörda och omkörs inte i detta uppdrag.

MCR-verifiering 2026-11-02: `10.0.303` svarade **404**, men officiella **10.0.302** finns.
Dess manifestlista har digest
`sha256:72dd743782f2ae7e5476fd64f6a460045e3998dc862218b80e6944cba79a01b0`.
Skriptet väljer `mcr.microsoft.com/dotnet/sdk:10.0.302`, hämtar och digestlåser den med
befintlig mekanism. Endast den tillfälligt genererade API-Dockerfilen ändras. Efter kopiering
av arkivets `global.json` måste `dotnet --version` vara exakt 10.0.302 före restore.
Image-metadata räcker inte: kontrollen kör SDK-resolvern med det accepterade kontraktet.
Felaktig/otillgänglig image eller ändrat SDK-kontrakt stoppar före databashjälparen och bytet.
Privat `build.json` får SDK-version, vald digest och hash för oförändrad `global.json`
efter lyckade byggen. Alla övriga baser förblir digestlåsta; Git-arkiv, källträd,
ordinarie Dockerfiles, revisions-/källproveniens och befintliga skyddsgrindar bevaras.

Offline-verifiering: **183 PASS, ett befintligt opt-in-Docker-test SKIP** (184 tester totalt).
Regressionerna täcker godkänd 10.0.300/10.0.302, nekad 10.0.403 och andra inkompatibla
versioner, saknad digest, pull-/SDK-byggfel före databasåtkomst samt bevarat arkiv/källträd.
Verkliga isolerade SDK-byggen på linux/amd64 med `--network none`, utan volymmounts:
**10.0.302 PASS** för SDK-innehåll, resolver med accepterad `global.json` och restore av
ett minimalt net10.0-projekt utan externa paket; **10.0.403 avvisad som förväntat**.
Detta är inte ett fullständigt API/Web-deploymentbygge. Dokumentvalidator PASS;
exakt kandidat-SHA/tree, diff och CI-resultat redovisas i PR.
Detta uppdrag omfattar exakt skriptet, STANDARD-testfilen och denna dokumentation.
Oberoende T2-review krävs före merge. Ingen merge, `--execute`, deployment, produktions-
volymåtkomst eller ändring av körande containrar ingår. Finance, BB-158,
`backup_support.py`, databasåtkomst och deploymentbehörigheter är oförändrade.

### Avgränsat runtimeundantag — T2-checkpoint 2026-11-02

Baseline `77f0deed3b65a317e0afc42f050316b4ca985a3d`. Enligt ägarens incidentunderlag
stoppade `run-1f5a9487e1b649ef90c3e7b2632f3e1e` med `PRE_SWITCH_FAILED` efter två
lyckade byggen: API-imagens enda identifierade kontraktsdelta var `DOTNET_VERSION`
och `ASPNET_VERSION`, båda **10.0.10 → 10.0.14**. Web-kontraktet var oförändrat;
ingen backuphjälpare eller tjänsteväxling startades. Incidenten omkörs inte här.

Korrigeringen tillåter endast dessa två API-imagevärden att ändras. Båda måste vara
entydiga numeriska `major.minor.patch`, lika inom respektive image, inom samma
major/minor som originalet och utan nedgradering. Övriga image-runtimefält och
miljöposter jämförs exakt enligt befintligt kontrakt; Web får inget undantag.
Miljötext är inte runtimebevis: den tillfälliga digestlåsta API-Dockerfilens sista
byggsteg kör `dotnet --list-runtimes` som image-användaren och kräver lyckad exit
och exakt de två förväntade shared frameworks, versionerna och installationsvägarna.
Extra/saknade runtimes eller avvikande innehåll stoppar bygget före databasåtkomst.
Endast lyckat bygge skapar det processinterna bevis som `bind_images()` kräver.
Privat `build.json` binder kontrollen, basdigest, Dockerfile-hash och versionspar till
slutlig image-digest, accepterad Git-revision/tree och källarkivhash. Ingen extern
receipt, flyttbar tagg eller ensam OCI-/miljöetikett kan aktivera undantaget.

En exakt kandidatmiljö härleds från originalet och verifierad image. Befintliga
Compose-overrides bevaras, även när de anger gamla versionsvärden; det faktiska
runtimeinnehållet styrks av byggkontrollen. Ett partiellt override som skulle ge
inkonsistenta versionsvärden avvisas. Samma digestbundna förväntan används i
isolerade kandidatprober, `wait_healthy()` och `stop()` inför rollback. Originalets
kontroll före byte och återgång till originalimage använder fortsatt originalmiljön.
Ersättnings-ID registreras efter den avgränsade Compose-operationen och kontrolleras
igen vid hälsa/stopp. Främmande ID/image, miljö-/behörighetsdrift, OOM och okända
exitkoder ger ingen automatisk rollback; känt exit **1** får återställas efter övriga
kontroller. Osäker återgång slutar fortsatt i `MANUAL_REVIEW_REQUIRED` utan DB-restore.

Verifiering 2026-11-02: **196 offline-tester PASS, ett befintligt opt-in-Docker-test
SKIP** (197 totalt), inklusive 101 PASS i STANDARD-testfilen. Regressionerna täcker
patchacceptans med byggbevis, felaktiga versioner/proveniens, övriga kontraktsändringar,
lyckat byte utan falskt `ReplacementConfigChanged`, exakt originalkontroll och
säker/osäker rollback. Genererad shellkontroll provas med syntetiskt runtimeinnehåll
och felstatus. Docker-/byggorkestreringen är mockad; verkligt nytt API/Web-bygge,
produktionsdatabasåtkomst och deployment är **NOT RUN**. Befintliga säkerhetsgrindar
kvarstår. Exakt SHA/tree, fullständig diff, CI och begränsningar finns i checkpoint-PR:n.
**STOPP för oberoende ChatGPT T2-review; ingen merge eller deployment är auktoriserad.**

### STANDARD – oanvända Compose-volymer (T2-checkpoint 2026-11-02)

Den tredje auktoriserade STANDARD-körningen på `e3ea2c4` stoppade med
`PRE_SWITCH_FAILED` efter lyckade API/Web-byggen. Release- och rollback-
snapshots innehöll tio toppnivåvolymer, medan Compose renderade sju.
En skrivskyddad jämförelse verifierade att de tre utelämnade definitionerna
inte refererades av de projicerade API/Web-tjänsterna och att återstående
konfigurationer var identiska efter just denna avgränsning.

`release_config()` deklarerar nu endast namngivna volymer som de båda
tjänsterna refererar till. Samtliga behålls som `external: true`.
Odeklarerade referenser avvisas. `snapshot_matches()`, backupkontroller,
runtimekontrakt, frysningar och driftgrindar är oförändrade.

Denna checkpoint är en kodkorrigering, inte deploymentacceptans.
Produktionsbackup, återstartstester och tjänstebyte är fortfarande
NOT RUN för den korrigerade revisionen. Oberoende T2-review krävs
före merge. Ingen omkörning eller produktionsändring ingår.

### Skollunch och STANDARD-guardfix — T2-checkpoint 2026-11-03

Baseline `2cdb71df1e166f8ff8e644e1014b1902cb08f5a1`; en gemensam checkpoint med
[Skollunchkontraktet](../knowledge/meal-planner.md#skollunch--behovsstyrd-omvalidering-och-ux-b-t2-checkpoint-2026-11-03).
Ingen merge eller deployment. Tidigare läsande preflight mot denna main identifierade API/Web
på `6ecf6f4b34b044c41164fa75d7297010499eaa2e` och två faktiska hinder: `ComposeEnvironmentMissing`
för körande Compose-label `/dev/null`, samt fem exakta filer utanför `APP_FILES`.
CLI stannade med exitkod 1 före config-hashkontrollen; separat oförändrad `git_guard` bekräftade
`OutsideStandardScope`. Det var inte det avsiktliga uppskjutna avslutet med exitkod 2.
Detta är daterad preflightevidens, inte ny runtimeverifiering i implementationsuppdraget.

Tillåtelselistan utökas med exakt följande filer, utan SHA-undantag eller katalogwildcards:

| Fullständig sökväg | Beteende/beroende och risk | Nödvändig regression/gräns |
| --- | --- | --- |
| `src/BigBrain.Api/MealPlanner/SchoolMealSourceClient.cs` | Skollunchens HTTP-adapter, validatorer och cachemetadata; fel kan ge otillåten återanvändning eller anrop. | Cachevillkor, 200/304, Retry-After, anropsgolv och källisolering. |
| `src/BigBrain.Api/MealPlanner/SchoolMeals.cs` | DTO, ren parser och offlinefixtures; PR #31 tillför giltighetstid. | API-serialisering, RSS/HTML-underlag och bevarad databasgräns. |
| `src/BigBrain.Web/src/api.ts` | Delad klient/kontraktsfil; PR #31 tillför endast Skollunchens giltighetstid. | TypeScript/build och semantisk granskning av varje delta; sökvägen ensam begränsar inte andra moduler. |
| `src/BigBrain.Web/src/meal-planner/SchoolLunch.tsx` | Presentation och anrop via befintligt API. | Fail-closed-utgång, UX B, fixtures och frånvaro av polling/dubbla anrop. |
| `src/BigBrain.Web/src/meal-planner/SchoolLunch.test.tsx` | Skollunchens frontendtester, ligger utanför rotens `tests/`. | Exakt testfil tillåts; närliggande filer och bygginputs avvisas. |

Ingen ny Finance-/BB-158-sökväg eller schema-, rättighets-, nätverks-, Dockerfile-, Compose-
eller beroendefil tillåts. Modulerna ändras inte i aktuell diff. Den delade `api.ts` innebär en
uttrycklig granskningsrisk: framtida orelaterade ändringar där omfattas inte av mandatet även om
sökvägskontrollen passerar. Semantisk säkerhets-/schemareview och samtliga andra grindar kvarstår.

Miljökällor valideras vid discovery och på nytt före Compose-användning. Endast den bokstavliga
`/dev/null`, utan symlänk i någon sökvägsdel, får vara specialfil: `lstat` måste visa teckenenhet
med major/minor **1:3**. Även Compose-modellens tomma miljökälla kontrolleras. Andra enheter,
FIFO, kataloger, symlänkar (även trasiga), relativa och explicit saknade miljöfiler avvisas.
Vanliga absoluta filer fortsätter fungera. Oförändrad implicit `.env`-regel: om inga env-labels
angivits och projektets `.env` saknas används verifierad `/dev/null`; en explicit felaktig källa
får aldrig ersättas med detta fallback. Inga privata miljövärden eller sökvägar publiceras.

Config-hash, driftidentitet, ancestry, CI, backup, isolerade start-/omstartsprov, journal och
rollbackgrindar är oförändrade. Offlinefallen verifierar simulerad Docker och syntetisk SQLite;
produktionspreflight körs inte på en oaccepterad kandidat. Ny läsande preflight mot accepterad
main och därefter separat auktoriserade driftkontroller återstår.
[Testresultat och kvarstående hinder](../reports/features/family/school-meals-preparation-20261023.md#demand-refresh-and-standard-guards--review-checkpoint-2026-11-03).
