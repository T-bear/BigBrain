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
   initialt endast `SchoolMealService.cs` och borttagna `MealPlannerStore.SchoolMeals.cs`;
   Web tillåter ingen ny appdelta. Dokumentation/tester/driftskript/CI tillåts som tidigare.
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
