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

## Finance — fryst, skydds- och återhämtningsgrindar kvarstår

**FROZEN / OWNER DECISION REQUIRED TO RESUME. RESEARCH / 0 SEK / NONE.**
[Finance-frysningen](finance-freeze-20261016.md) styr före äldre commissioning och fortsättningsnoter.
G1/G2 är dokumenterade PASS; G3a BLOCKED och G3b NOT RUN. Aktuell host-, journal-, mapping-,
artefakt- och authority-status är inte omverifierad. Ingen grantförnyelse, inferens eller aktivering.

[Rättigheter, retention, dataintegritet och BB-132K:s disposal-frister](finance-freeze-20261016.md#f-driftinventering--daterad-evidens-ingen-driftmutation)
kräver fortsatt efterlevnad; faktisk privat deadline/disposal-status är okänd och frysningen
förlänger inga frister. Inga privata artefakter granskas eller åtgärdas med stöd av DOC-2A.
[Historiska recoveryförlopp](../history/codex-recovery-before-doc-2a-20261024.md#historical-qwen-integration--2026-10-16)
är bevarad evidens, inte aktiva exekveringsgodkännanden.

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
Applikationskandidat `12fe4ef71ac6a0158a3a40530e47a731a99af249` och worker
`22ae923c136a747eac020d8716cdbe92306d7376` är nu ägargodkänt mergade; verifierad main är
`82fd7bba0dfe41196902bba200d05426f1af2bde`. Deployment är inte genomförd.
[Mergebevis och root-blockerare](runbooks/approved-operations.md#approved-merges-completed-root-backup-gate-blocked--2026-10-27)
är det bestående underlaget. Både verify-backup och backup-approved nekades; intern orsak är
okänd utan administratörens granskning av skyddade underlag. Inga blinda omförsök. Befintligt
deploymentgodkännande gäller; nästa steg är administratörens avgränsade kontroll enligt runbooken.
Orelaterade oregistrerade filer har bevarats. Återanvänd testresultaten när motsvarande kod är oförändrad.
