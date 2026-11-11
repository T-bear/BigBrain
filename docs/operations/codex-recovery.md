# Codex interrupted-run recovery

Avstämt 2026-10-24 mot main `7b3beb5288ddf7010990d17adf0635c0d59f0790`.
Detta är ingången för aktiva avbrott och osäkra återhämtningsbehov, inte en checkpointlogg.
[AGENTS avsnitt 9](../../AGENTS.md#9-avbrott-och-blockerad-implementation) styr återupptagning;
[STATUS](../STATUS.md) skiljer accepterad kod från drift och manuell acceptans.

## DOC-2A — publicerad beslutspunkt

Fas A `338459078e9130cd7f500fbfc182ec245f65d28a` och hanteringen av U1–U3 är ägargodkända.
Fas B på `docs/doc-2a-recovery-status-inventory` konsoliderar enbart dokumentation.
[Inventering, destinationer och verifiering](../reports/documentation/doc-2a-recovery-status-inventory-20261024.md#fas-b--godkänd-konsolidering-2026-10-24)
bär evidensen; exakt publicerings-SHA/tree finns i Gitmetadata. Nästa steg är oberoende review,
inte automatisk fortsättning. Ingen merge, deployment eller 2B/2C är auktoriserad.

Inget nytt pågående implementationsavbrott har verifierats i DOC-2A. Det är inte ett besked att alla
äldre driftincidenter är avslutade. Orelaterade ospårade designmockuper, fyra Sentinel-ADR-utkast
samt YouTube-diagnostik bevaras; deras innehåll har inte ändrats eller publicerats här.

## BB-158 — fryst, drift och recovery ej omverifierade

**FROZEN / OWNER DECISION REQUIRED TO RESUME.**
[Kanoniskt beslut och bevarad operativ evidens](bb158-freeze-20261023.md#f-driftinventering-och-evidensgränser).
Accepterad kod och ägarrapporterad deployment bevisar inte aktuella images, aktivering, backupstatus,
biblioteksintegritet eller att osäkra importer/avbrutna deploymentförsök är avslutade.
Dessa frågor kvarstår **UNKNOWN / NOT REVERIFIED**, inte PASS eller en ny körorder.

Äldre paket och körningsresultat ska bevaras, inte återstartas, skrivas om eller återanvändas.
Ingen automatisk retry, databasåterställning eller nedgradering vid osäkert tillstånd.
Tidigare Sonarr-varningar, scanner-/filrättighets-, backup-/rollback-, aktivitets- och
sessionsoberoende återhämtningsgrindar är inte stängda genom dokumentationsflytten.
Framtida åtgärd kräver nytt avgränsat ägarbeslut enligt frysningens återupptagningsprotokoll.
[Historiska checkpointförlopp](../history/codex-recovery-before-doc-2a-20261024.md#bb-158--sonarr-queue-duration-correction-2026-10-21).

## Finance Release 1 — 2026-11-11

**INTERRUPTED — MANUAL REVIEW REQUIRED** för release/återaktivering, ingen avbruten driftoperation.
Den äldre DOC-2A-notens administrativa frysning ersätts av upptiningen 2026-11-08 och
ägarens avgränsade Release 1-uppdrag. G3a/G3b ger fortsatt ingen modellauktoritet.
Branch `checkpoint/finance-release1-20261111`, accepterad baseline
`e63aaa4dc30718f2620f2880a6f2b847e6091d89`; kandidat/tree i Gitmetadata/PR.
[Beständig implementation, runtime, tester och blockerare](../reports/features/finance/finance-bb-087-prospective-shadow-foundation-20260815.md#release-1-checkpoint--2026-11-11).
Inga produktionsdata, secrets, grants, containers eller inställningar ändrade; EODHD förblir avstängt.
Ny separat korrigeringsauktorisering 2026-11-11 omfattar kausalitet, bevarade orsaker och tekniskt
underlag; ingen merge/deployment/aktivering. Följ samma PR #81. Ägarens villkorade BB-132K-val A
ligger i separat draft PR #82 och lämnas orört. Granska slutkandidaten; nästa operativa steg kräver
godkänd skyddad outcome-läsväg, befintliga backup-/restorekvitton och extern testklient/åtkomstväg.
Manifest v2 är olåst. Inga av de 906 prognoserna/858 utfallen har ändrats.
Ingen omkörning av gamla BB-132K-paket eller kringgång av STANDARD:s Finance-spärr.
Orelaterade ospårade arbetsfiler bevarade. Fortsätt samma checkpoint; ingen historikskrivning.

## Övriga öppna verifierings- och acceptansfrågor

Skollunch v2 är mergad, inte ett kvarstående implementationsavbrott; deployment och källaktivering
har separat beslutspunkt. Media/Mediekontroll, designsystemets samlade ägaracceptans och
Download Controls kvarstående säkerhetsverifiering får inte avslutas genom arkivering.
Se [aktuella begränsningar i STATUS](../STATUS.md#säkerhet-och-kvarstående-acceptans) och dess källor.

## Historik och nya avbrott

[Fullständigt recoveryarkiv före DOC-2A](../history/codex-recovery-before-doc-2a-20261024.md)
bevarar alla 68 ursprungliga rubriker, datum, SHA, beslut och experimentresultat.
En publicerad historisk checkpoint är inte automatiskt ett pågående avbrott.
Vid ett faktiskt nytt avbrott används denna fil enligt AGENTS: ange rätt avbrottsstatus,
baseline/branch, opublicerad delta, giltig evidens, återstående arbete och säker nästa åtgärd.
Den [historiska mallen](../history/codex-recovery-before-doc-2a-20261024.md#recovery-note-template)
bevaras som historik; aktuella handoffkrav finns i AGENTS, inte i en ny parallell process.

## Skollunch — publicerad driftförberedelse, 2026-10-27

Ingen avbruten produktionsoperation. `ops/school-lunch-approved-worker` utgår från main
`b23e00d20a24bdeb459949ab335c8b9cd3599bd3`; exakt checkpoint hämtas ur branchens Gitmetadata.
Den granskade applikationskandidaten `12fe4ef71ac6a0158a3a40530e47a731a99af249` är oförändrad
på `fix/school-lunch-cache-expiry`. Merge och deployment är inte utförda.
[Worker, tester, observerad runtime och kvarstående godkännanden](runbooks/approved-operations.md#checkpoint-evidence-and-next-decision)
är det bestående underlaget. Nästa steg är exakt mergegodkännande och oberoende granskning av
worker-koden före root-godkännande; inga färdiga produktionspaket eller godkännanden finns här.
Orelaterade oregistrerade filer har bevarats. Återanvänd testresultaten när motsvarande kod är oförändrad.


## SchoolLunch mixed-runtime recovery — 2026-10-29

Uppdatering 2026-11-01: det gamla paket-/approval-/workerspåret är
[fryst och arkiverat på plats](runbooks/approved-operations.md#fryst-och-arkiverat-skollunchspår--2026-11-01).
Följande tidigare recoverysekvens är historik och får inte återstartas. Ingen driftåtgärd
har utförts och runtimeosäkerheten kvarstår. [STANDARD](../policy/lanes.md) är separat
reviewförberedelse; dess preflight ska stoppa vid osäker rollback-image eller runtimeavvikelse.

**INTERRUPTED — MANUAL REVIEW REQUIRED** for production recovery, not a new production attempt.
Baseline `a68c0247ea7a78ad5fadf6400ed8bc2e22ed35c0`; branch
`ops/school-lunch-mixed-runtime-recovery`. Exact checkpoint SHA/tree is in Git metadata.
The [bounded correction, private preparation and evidence](../reports/features/family/school-meals-preparation-20261023.md#mixed-runtime-recovery-checkpoint--2026-10-29)
retain the failed mixed runtime: API `3cf409e` with temporary repair, Web `ff17bf46`, both healthy;
active interlock/claims untouched. Corrected accepted API and rebuilt Web artifacts are prepared.
84 offline tests PASS; current-backup compatibility acceptance and protected receipt review remain
administratively blocked. The private draft deliberately lacks verified backup/recovery bindings
and cannot serve as an approved executable package. No local application delta or production action.
Next: independent exact-candidate review, followed by the report's single administrative recovery
sequence. No autonomous interlock reset, obsolete backup reuse, deployment or source request.

## Morgonbladet checkpoint 1 — avstämd 2026-11-04

Det tidigare avbrottet vid `4cc68a4` är hanterat genom ägarens separat auktoriserade,
enda avgränsade CSS-korrigering. Inget pågående implementationsavbrott återstår här.
PR #22 / `review/chatgpt-20261030-230941-1430648`; verifierad main
`c2d33099fe5af6d51a70a943b4006c7ea8f5f0bd`.
[Scope, jämförelse, historiskt fel, aktuell verifiering och artefakter](../reports/features/newspaper/checkpoint1-20261104.md)
är det kanoniska underlaget. Publiceringscommit/tree finns i Gitmetadata.
Nästa steg är oberoende review och återstående manuell iPhone/PWA-/utskriftsacceptans.
Ingen merge, deployment, ytterligare automatisk korrigering eller checkpoint 2 är auktoriserad.
Orelaterade working-tree-filer i huvudarbetskopian är orörda.
