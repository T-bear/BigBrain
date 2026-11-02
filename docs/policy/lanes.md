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

Skriptet hittar nuvarande Compose-filer, projektkatalog och env-filer från båda
containrarnas matchande Compose-labels; vanlig projekt-`.env` används annars.
Valfri `--compose /absolute/compose.yaml` väljer en befintlig vanlig Compose-fil.
Ingen manuellt upplöst JSON, förbyggd image eller nytt root-godkännande behövs.
Config-hash måste matcha körande API/Web. Saknad åtkomst eller okända inputs ger stopp.

1. Kontrollera aktuell remote main, ren checkout, PR #25:s commit i ancestry och fyra
   gröna main-jobb. Appdiff sedan båda runtime-revisionerna tillåter initialt endast
   `SchoolMealService.cs` och borttagna `MealPlannerStore.SchoolMeals.cs`, utöver
   dokumentation/tester/driftskript/CI. Semantisk schema-/säkerhetsreview kvarstår.
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
Verifiering 2026-11-02: **142 offline-tester PASS**, inklusive de två felen, bevarade
skrivningar, låsta bygginputs, Compose-upptäckt och nekad release vid bygg-/probefel.
Verklig Compose-parser och dokumentvalidator PASS. CI kör drifttesterna i befintligt
backendjobb. Detta bevisar styrflödet med simulerad Docker och syntetisk SQLite/WAL;
verklig imagebuild, isolerad image-start och produktionsbyte är **NOT RUN** i PR-arbetet.

Kvarstår: andra T2-reviewvarvet och driftverifiering efter giltig acceptans/auktorisering.
Runtime-reparationer, okända overrides, legacy-bootstrap, ej återstartbar gammal image
eller annan appdelta ger stopp. Isolerade prov bevisar inte externa integrationers
funktion; källobservation och iPhone/PWA är PENDING. Daglig backup/restore-grinden för
schemaändringar kvarstår i [backupunderlaget](../operations/backup-restore/README.md).
All Finance-/BB-158-, schema-, privilegie- eller nätverksändring är utanför spåret.

Det [äldre paket-/approval-/workerspåret är fryst och arkiverat på plats](../operations/runbooks/approved-operations.md#fryst-och-arkiverat-skollunchspår--2026-11-01).
