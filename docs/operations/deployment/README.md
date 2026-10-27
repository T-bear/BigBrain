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

## Uppföljning av riskpolicyn

**REVIEW CHECKPOINT, 2026-10-27 — policyn är inte antagen eller driftsatt genom publicering.**
Branch `docs/risk-policy-levels`, baseline `82fd7bba0dfe41196902bba200d05426f1af2bde`.
Införandet omfattar uppdragets sex uttryckliga policykorrigeringar. De hänvisade 43 tidigare
policybesluten saknas i tillgängligt underlag; fullständig avstämning mot dem återstår före
slutligt antagande. Nuvarande merge-/deploymentregler gäller. Oktober-gatewayen, admin-ACL:er,
frysningar och runtime är oförändrade. Vid kompletteringen efter checkpoint `017ae25` har
TESTING.md:s generella godkännanderegel anpassats till AGENTS avsnitt 8: befintliga krav före
ikraftträdandet, delegerad leverans enligt risknivå först efter separat uttryckligt ägarbeslut
med exakt ikraftträdande revision. Oberoende review identifierade denna lucka i första checkpointen.
Inget ikraftträdandebeslut registreras här; backupgrinden är fortsatt BLOCKED. Faktisk oberoende
granskning för nivå 3, högst två reviewvarv/reparationsförsök och separat iPhone-acceptans bevaras.
CI för denna review-publicering: **PENDING (inte körd)**; ordinarie workflow triggas av PR/main,
inte push till denna branch. Detta är inte ett påstående att en GitHub-körning har startat.
Policygranskningen behandlas som nivå 3/HIGH eftersom kritiska privilegie- och auktoritetsgränser
berörs; inga faktiska privilegier ändras. Dokumentationsvalidator, diffkontroll, riktad kontroll av
länkar/ankare, tabeller, bevarade befintliga skyddsregler och sanerad diff: PASS. Gitleaks finns
inte tillgängligt lokalt; riktad sekretesskontroll ersätter inte en full secrets-skanning.
Applikations-/drifttester: SKIP, ren dokumentationsändring. Ingen pilot eller ny backup körd.

Efter uttryckligt ikraftträdandebeslut enligt [AGENTS avsnitt 8](../../../AGENTS.md#8-git-och-review-checkpoints), inte dokumentationsmerge,
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

Startstatus: **NOT STARTED; 0/5 deploymenter efter ikraftträdande**, ännu inga mätresultat.
Ingen historisk deployment räknas retroaktivt. Tidigare verifiering visar SchoolLunch v3 på
`ff17bf46` och senare korrigeringar mergade i `82fd7bba`; se
[daterad operationsuppföljning](https://github.com/T-bear/BigBrain/blob/aa5db2a418654ad3dd8c0e3c6c8355fcea974e73/docs/operations/runbooks/approved-operations.md#approved-merges-completed-root-backup-gate-blocked--2026-10-27).
Detta är återanvänd historisk runtimeevidens, inte en ny driftkontroll. Kontrollera aktuell
runtime före en verklig nivå 1-pilot av skolmat. Om avsedd version redan körs görs ingen
konstgjord redeployment. Databas- eller privilegieändringar får inte ingå under nivå 1-etiketten.
Pilot startar först efter uttryckligt ikraftträdandebeslut och verifierade befintliga driftgrindar, med giltiga
tidigare godkännanden och leverantörsvillkor bevarade. Inget installerat skydd får kringgås.

## Controlled Finance daily acquisition / API revision

[BB-132F1 operator gate](../runbooks/finance-controlled-acquisition.md) defines the exact-revision
build helper, external Compose pass-through and maintenance-only single acquisition. UNKNOWN
revision is honest and blocks acquisition. Credentials do not enable observation runtime.
Publication is not permission to deploy, recreate services, configure keys or call Alpaca.
