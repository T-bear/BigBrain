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

[deploy-standard.py](../../scripts/deploy-standard.py) tar aktuell accepterad main-SHA,
två redan byggda lokala `sha256:`-image-ID:n och befintlig Compose-konfiguration.
Images ska byggas ur samma rena Git-arkiv, med OCI-label
`org.opencontainers.image.revision=<main-SHA>` på **båda** images och API-buildarg
`BIGBRAIN_REVISION=<main-SHA>`. Tags/`latest`, pull och build ingår inte i byte/rollback.
Oberoende review binder imagebyggenas ursprung; en label ensam bevisar inte innehållet.

```sh
python3 scripts/deploy-standard.py --revision MAIN_SHA \
  --api-image sha256:API_ID --web-image sha256:WEB_ID --compose /absolute/current-compose.json
```

Utan `--execute` görs endast preflight. Efter separat giltig deploymentauktorisering
kör operatören samma kommando med `--execute` i befintlig `tmux`-session och med
befintlig åtkomst till Docker och backupkällorna; saknad åtkomst ger stopp, ingen sudo-installation.
Compose-filen ska återge **aktuell** konfiguration, inklusive privata overrides;
config-hash måste matcha båda körande containrarna. Inga flaggor, secrets, mounts,
privilegier eller nätverksmedlemskap får ändras. Konfiguration och kvitton lagras privat.

- Kräver aktuell remote main, ren checkout, PR #25:s commit i ancestry och fyra gröna
  main-jobb. Appdiff sedan båda runtime-revisionerna tillåter initialt endast
  `SchoolMealService.cs` och den borttagna `MealPlannerStore.SchoolMeals.cs`, utöver
  dokumentation/tester/driftskript/CI. Sökvägar ersätter inte semantisk schema-/säkerhetsreview.
- Återanvänder oförändrad sexdatabasbackup: SQLite onlinebackup med skrivskyddade källor,
  hash, integrity/FK/schema och isolerad readback. Färsk backup krävs före byte;
  ofullständiga kopior bevaras. Finance-data ingår inte. Detta är inte bevis för daglig
  backup eller produktionsrestore; [backupgrinden](../operations/backup-restore/README.md) kvarstår.
- Fryser exekveringskonfigurationen till endast API/Web och redan existerande externa
  volymer/nätverk. Delar lås med Web-only. Byter API före Web med ren graceful stop,
  `--no-deps --no-build --pull never`. Kontrollerar digest, runtimekonfiguration,
  containerhälsa, API `/health`, Web HTML/ikon, DB-identitet/schema och andra containrar.
- Vid känt start-/hälsofel återställs berörda images automatiskt, API före Web.
  **Ingen databasrestore eller Git-rollback:** senare hushållsskrivningar bevaras.
  Oren stop, schema-/datadrift, osäkert Docker-avbrott eller misslyckad rollback kräver
  manuell kontroll. Beständig journal spärrar ny körning efter ofullständigt försök;
  ingen automatisk retry eller bortstädning av tidigare körningar/backup sker.

Verifierat 2026-11-01: 124 offline-tester PASS (29 nya, 95 bevarade), syntetiska SQLite/WAL
samt simulerade Dockerfel. Verklig Compose-parser roundtrip med `$`-värden PASS, utan
Docker-daemonkontakt. Första testvarvet fann testharnessens mockning av Git-grinden och
arkivspärrarnas undantagstyper; korrigerat och hela den berörda sviten omkörd.
Parserprovet visade att Compose redan dollar-escapar sitt canonical output; snapshoten
bevarar nu det formatet och verifierar omrenderingen före backup/byte.
Detta är offline-verifiering, **inte** verifierad drift, riktig backup eller rollback på servern.
CI kör drifttesterna i befintligt backendjobb; inga nya obligatoriska CI-jobb införs.

Kvarstående driftgränser: oberoende T2-review, imageproveniens och återstartbar tidigare
image måste styrkas. Dokumenterad reparation i gamla API-containerns skrivbara lager,
legacy-bootstrap, avvikande Compose eller annan appdelta sedan runtime ger stopp.
Detta spår reparerar inte den historiska blandade runtime-versionen automatiskt.
Ingen skolmatskälla anropas av skriptet; ordinarie källobservation och ägarens iPhone/PWA
är separat PENDING. Finance/BB-158, schema-, privilegie- och nätverksändringar är utanför
spåret. All sådan diff är STRIKT och får ingen ny exekveringsauktorisering här.

Det [äldre paket-/approval-/workerspåret är fryst och arkiverat på plats](../operations/runbooks/approved-operations.md#fryst-och-arkiverat-skollunchspår--2026-11-01).
