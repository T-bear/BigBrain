# Deployment

Appliance boot/shutdown uses the [lifecycle runbook](../runbooks/bigbrain-appliance-lifecycle.md).
Docker and `bigbrain.service` must be enabled. The unit performs Compose preflight/readiness
and bounded stop without embedding secrets. External media and `/mnt/media` are noncritical.

## Finance EODHD Free

EODHD is disabled by default. The named `finance-market-data` volume is mounted only into
API at `/finance-data`; it is not part of backup automation. Configure secrets and lifecycle
through `FINANCE__EODHD__APITOKEN`, `FINANCE__EODHD__ENABLED`,
`FINANCE__EODHD__ACCOUNTACTIVE` and, only after verified termination,
`FINANCE__EODHD__ENTITLEMENTENDSATUTC`. Never render resolved Compose configuration when a
token is present. Use the [retention/deletion runbook](../runbooks/finance-eodhd-retention-deletion.md)
for enablement, expiry and destructive maintenance.

BB-078 deployed the enabled read-only path on 2026-08-11. The persistent volume contains
real EODHD Free memory and must be preserved on recreation. Use the runbook's sanitized
runtime-evidence command to verify counts/replay without printing the token or raw payloads.

Grundstruktur. Ingen ny normativ deploymentprocedur införs i denna fas.

## Ägarbeslut om riskpolicyns ikraftträdande – 2026-10-27

Produktägaren har den **27 oktober 2026 uttryckligen beslutat att aktivera** den redan accepterade
riskbaserade delegeringsmodellen. Detta registrerar ett fattat beslut, inte ett nytt policyförslag.
Auktoritativ policyrevision är **`cb7d1e70bc28cc3c4db4c15aa872f426f82d09f8`**;
[AGENTS avsnitt 3, 7 och 8 vid denna revision](https://github.com/T-bear/BigBrain/blob/cb7d1e70bc28cc3c4db4c15aa872f426f82d09f8/AGENTS.md)
styr nivåer, granskning och delegering. Policyn accepterades ursprungligen via
[PR #6](https://github.com/T-bear/BigBrain/pull/6), kandidat
`0ba2845226a016d9ab73bd445981ee0309001e12`; AGENTS och TESTING är oförändrade mellan kandidaten
och angiven policyrevision.

**Ikraftträdandevillkor:** beslutet får tillämpas först när denna beslutsdokumentation har mergats
till main och relevant main-CI för just den mergecommiten är godkänd. Verifiera publicerad merge-SHA
och workflowens motsvarande `head_sha`; `backend`, `frontend`, `documentation` och `secrets` ska
alla vara `completed/success`. Publicering, PR-CI eller tidigare main-CI uppfyller inte detta villkor.
När båda villkoren är uppfyllda gäller beslutet utan ett nytt allmänt policybeslut. GitHub-merge
och CI-kvitton fastställer den faktiska ikraftträdandetidpunkten; denna checkpoint påstår inte
att merge eller ikraftträdande redan har skett.

Beslutet gäller **endast nya, uttryckligen auktoriserade uppdrag från denna tidpunkt**, inom deras
godkända scope och riskram. Ingen retroaktiv eller generell auktorisering av äldre uppdrag följer.
Tidigare giltiga driftbeslut behåller sin ursprungliga omfattning men får ingen utökad delegering.

- Nivå 1: autonom teknisk leverans inom godkänt scope och befintliga kontroller.
- Nivå 2: autonom leverans med relevanta kontroller; [backupgrinden](../backup-restore/README.md#nivå-2-grind--evidens-och-återstående-verifiering)
  för autonoma databasdeploymenter förblir **BLOCKED**. Separat granskning krävs tills faktisk
  daglig backup och ett lyckat isolerat återställningsprov från verklig backup har verifierats.
- Nivå 3: autonom merge/deployment inom på förhand godkänt uppdrag och riskram först efter
  godkänd faktisk oberoende granskning av exakt kandidat-SHA/tree mot rätt main-baslinje.
  Inget nytt rutinmässigt ägargodkännande av varje SHA/deployment krävs inom denna ram.

Finance och hela BB-158 förblir [frysta](../freezes.md). Utökad ekonomisk risk, nya kostnader,
mandatutökning, hävning av frysningar och andra reserverade beslut kräver separat ägarbeslut.
Gateway, admin-ACL, root-approvals, säkerhetsgränser, leverantörsvillkor och användardata lämnas
oförändrade; ingen produktionsåtgärd följer av registreringen. P1-reglerna, två ordinarie reviewvarv,
högst två avgränsade reparationsförsök, verifierat säker rollback eller bevarat tillstånd vid hinder,
resursram och sekretess gäller enligt policyn. Obligatorisk iPhone/PWA-acceptans förblir ägarens
separata manuella acceptans och ersätts inte av CI eller teknisk leverans.

**Övergångsregel för denna aktiverings-PR:** befintligt krav på uttryckligt ägargodkännande av
exakt kandidat-SHA gäller. Aktiveringsbeslutet är inte ett förhandsgodkännande av en okänd kandidat.
[Ruleset 25987490](https://github.com/T-bear/BigBrain/rules/25987490) verifierades aktivt 2026-10-27:
PR, uppdaterad main och de fyra CI-kontrollerna krävs; force-push och deletion skyddas, inga
bypass-aktörer finns. GitHub kräver noll numeriska review approvals, vilket inte ersätter policyns
krav på faktisk oberoende granskning där den gäller. Reglerna ändras inte och ska kontrolleras
vid merge. Repositoryts förbud mot squash/rebase gäller även om rulesetet tekniskt tillåter dem.

## Uppföljning av riskpolicyn

Tidigare checkpointstatus och verifieringshistorik bevaras i
[dokumentet före beslutsregistreringen](https://github.com/T-bear/BigBrain/blob/cb7d1e70bc28cc3c4db4c15aa872f426f82d09f8/docs/operations/deployment/README.md#uppföljning-av-riskpolicyn).
Historiska formuleringar om ännu ej fattat policybeslut eller väntande granskning är inte aktuella
stoppvillkor; det daterade ägarbeslutet och dess ikraftträdandevillkor ovan gäller.

Efter uppfyllda ikraftträdandevillkor ovan, inte enbart dokumentationsmerge,
registreras de fem första verkliga deploymenterna här, med länkar till befintliga driftkvitton.
Ingen ny rapportserie eller mätinfrastruktur behövs. För varje leverans anges revision/run-ID,
risknivå, datum, utfall, antal extra ägaringrepp och deras orsak, blockerade försök samt rollbacks.
Extra ägaringrepp är begärd ytterligare manuell ägaråtgärd efter det ursprungliga auktoriserade
uppdraget, inklusive upprepade beslut och nödvändiga operatörsåtgärder om ägaren utför dem.
Obligatorisk iPhone/PWA-acceptans redovisas separat. Administratörsåtgärd utförd av annan person
redovisas som sådan, inte som ett ägaringrepp. Antalet får inte döljas genom omklassificering.

Mål: högst två extra ägaringrepp totalt för fem deploymenter. Notera blockerade leveransförsök
även om deployment ännu inte genomförts; de får inte försvinna ur mätningen. Ett stopp före
containerbyte är ingen deployment och fyller inte en av de fem platserna. En verklig deployment
med rollback räknas och markeras som sådan. Mätmålet kringgår inga bindande kontroller.

Uppföljning 2026-10-30: **2/5 verkliga deploymenter** efter ikraftträdandet,
varav en med rollback och en lyckad. Båda gäller Web-only STANDARD (nivå 1).

- **1/5 – 2026-10-30, PR #17:** [PR](https://github.com/T-bear/BigBrain/pull/17),
  main-revision `32d2193c9f1bce4051f935943ad5f47b79e541cb`,
  [main-CI](https://github.com/T-bear/BigBrain/actions/runs/41038495504) godkänd.
  Kandidaten byggdes och Web-containern startades, men hälsokontrollen
  misslyckades. Diagnostiken visade att Nginx nekades läsning av
  `default.conf`. Automatisk **ROLLBACK PASS**; föregående Web-image
  återställdes och verifierades healthy med HTTP 200 för startsida och ikon.
  iPhone/PWA-acceptans: inte utförd för den misslyckade kandidaten.
  Extra ägaringrepp: **minst 3** separata diagnostikkörningar, utöver
  korrigerings- och publiceringsåtgärder; exakt totalantal inte avstämt.
  Korrigeringen publicerades i [PR #18](https://github.com/T-bear/BigBrain/pull/18).
- **2/5 – 2026-10-30, PR #19:** [PR](https://github.com/T-bear/BigBrain/pull/19),
  main-revision `4719fe1c6cb25dbd370d411355fe41a487e7ed00`,
  [main-CI](https://github.com/T-bear/BigBrain/actions/runs/41044717981) godkänd.
  Kandidatbygge, Web-byte, containerhälsa och HTTP-kontroller godkändes;
  deploy-skriptet rapporterade **PASS**, exitkod 0, utan rollback.
  Endast Web återskapades enligt skriptets rapport.
  Extra ägaringrepp efter det auktoriserade uppdraget: **0 rapporterade**.
  Separat obligatorisk iPhone/PWA-acceptans: **PASS, uttryckligen godkänd av ägaren**.

Runtimeutfallen ovan bygger på ägarens redovisade terminalresultat.
Separata publicerade runtimekvitton saknas; PR- och CI-länkarna styrker
kodrevision och automatiska kontroller, inte runtimeutfallet.
Målet om högst två extra ägaringrepp kan inte redovisas som uppfyllt:
minst tre är belagda efter första försöket.

Ingen historisk deployment räknas retroaktivt. Tidigare verifiering visar SchoolLunch v3 på
`ff17bf46` och senare korrigeringar mergade i `82fd7bba`; se
[daterad operationsuppföljning](https://github.com/T-bear/BigBrain/blob/aa5db2a418654ad3dd8c0e3c6c8355fcea974e73/docs/operations/runbooks/approved-operations.md#approved-merges-completed-root-backup-gate-blocked--2026-10-27).
Detta är återanvänd historisk runtimeevidens, inte en ny driftkontroll. Kontrollera aktuell
runtime före en verklig nivå 1-pilot av skolmat. Om avsedd version redan körs görs ingen
konstgjord redeployment. Databas- eller privilegieändringar får inte ingå under nivå 1-etiketten.
Pilot får starta som ett nytt uttryckligen auktoriserat uppdrag efter uppfyllda ikraftträdandevillkor
och verifierade befintliga driftgrindar. Leverantörsvillkor bevaras; inget installerat skydd får kringgås.

## Controlled Finance daily acquisition / API revision

[BB-132F1 operator gate](../runbooks/finance-controlled-acquisition.md) defines the exact-revision
build helper, external Compose pass-through and maintenance-only single acquisition. UNKNOWN
revision is honest and blocks acquisition. Credentials do not enable observation runtime.
Publication is not permission to deploy, recreate services, configure keys or call Alpaca.
