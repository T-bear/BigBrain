# Codex interrupted-run recovery

Avstämt 2026-10-24 mot main `7b3beb5288ddf7010990d17adf0635c0d59f0790`.
Detta är ingången för aktiva avbrott och osäkra återhämtningsbehov, inte en checkpointlogg.
[AGENTS avsnitt 9](../../AGENTS.md#9-avbrott-och-blockerad-implementation) styr återupptagning;
[STATUS](../STATUS.md) skiljer accepterad kod från drift och manuell acceptans.

## PREDEPLOY — avslutat inom beslutat scope, 2026-11-14

Det tidigare scanneravbrottet är avslutat genom PR90/main942ff65, exakt historisk
fingerprint, fullhistorik597 commits och tillämplig CI PASS. Ägaren har därefter uttryckligen
godkänt test1–4 samt test5 (Safari/hemskärm). Ingen fortsatt reparationsloop eller aktivt
PREDEPLOY-implementationsavbrott återstår. [Kanonisk slutacceptans](runbooks/test-environment.md#predeploy-owner-acceptance-complete--2026-11-14)
anger exakt källa, artefakt, review/CI, andra tidsbegränsade testfönstret och evidensgränser.
Andra fönstrets normala sluttid är18:55 Stockholm; installerad cleanup gäller oförändrad.
Ingen permanent/bootdrift, produktionsändring eller Finance-aktivering följer. Historisk
startosäkerhet förblir uttryckligen riskaccepterad, inte orsaksförklarad. Övrig recovery nedan
är orörd och har eget scope.

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

## Finance — integration återupptagen, 2026-11-20

**LOKAL INTEGRATION GENOMFÖRD — REVIEW PENDING.** Codex sandbox nekade
Gitmetadata-skrivning och NuGetåtkomst, men efter Codex avslut slutfördes
de två upplösta dokumentkonflikterna i den auktoriserade Remote Desktop-miljön.
PR #81-mergen `48fc3192540c4323566e96f79fb7ef48f44238c6` bevarades;
PR #111-mergen sparades som `6917cede66e8ca18e516bb2833923ebcfab81faf`.
Kombinerad Release-build PASS med 0 fel/varningar; 86/86 isolerade audit-tester
och 84/84 fokuserade Finance API-tester PASS; `dotnet format --verify-no-changes`
PASS. Kod- och testevidens från båda granskade källor är byteidentisk.
Dokumentationsmetadata justerades efter lokal verifiering. Ingen produktionsdata
lästes eller ändrades. Separat publiceringscommit, required GitHub CI och
oberoende risk-3-review är ännu inte godkända; ingen merge/deployment.
[Samlad integrationsrapport](../reports/features/finance/finance-bb-087-prospective-shadow-foundation-20260815.md#owner-authorized-integration-checkpoint--2026-11-20).

Ägaren har återupptagit det kreditavbrutna integrationsuppdraget, risk 3, på
`checkpoint/finance-integration-20261120` från verifierad main
`1e3d203477cc81e8b1e58780dd4a758926469b7d`. Endast integration, syntetisk verifiering och
publicering för oberoende review är auktoriserade. RESEARCH / 0 SEK / NONE består enligt
[kanoniskt upptiningsbeslut](finance-freeze-20261016.md#aktuellt-ägarbeslut--administrativ-upptining-2026-11-08).

Exakta källor bevaras genom mergehistorik: PR #81 `afcf5d79a62be2831ff002014a9dfe972384f01f`
(ursprunglig baseline `e63aaa4dc30718f2620f2880a6f2b847e6091d89`) och PR #111
`1d332cd8c3fc7b97be79718c1462e64c0146b6da` (baseline samma main som ovan).
Deras brancher och separat PR #82 lämnas orörda. [Release 1-evidens](../reports/features/finance/finance-bb-087-prospective-shadow-foundation-20260815.md#release-1-checkpoint--2026-11-11)
och [offline-auditens evidens och operatörsförslag](../reports/features/finance/finance-bb-087-prospective-shadow-foundation-20260815.md#protected-outcome-audit-design-checkpoint--2026-11-20)
bevarar tidigare resultat och begränsningar. Nästa tekniska steg följer integrationsuppdraget,
inte äldre instruktioner att fortsätta PR #81 eller #111 separat.

**INTERRUPTED — MANUAL REVIEW REQUIRED** kvarstår för skyddad produktionsaudit/release/
återaktivering; inget driftkommando är avbrutet. G3a BLOCKED / G3b NOT RUN, manifest v2 olåst,
faktisk audit av de historiskt rapporterade 858 utfallen NOT READ. Inga 906 prognoser eller
858 utfall har ändrats. Daterad ingress-/backupevidens ger inte tillstånd till Finance-release.
Ingen realdata, snapshot/kopia, operatörskörning, provider/modell/trading, merge till main eller
deployment. BB-132K-attestering och dess separata dispositionsgrindar bevaras; passerad
omprövningsdag medger varken automatisk förlängning eller radering. Ingen omkörning av gamla
paket, journalreset, grantförnyelse eller kringgång av STANDARD:s Finance-spärr.
[Historiska recoveryförlopp](../history/codex-recovery-before-doc-2a-20261024.md#historical-qwen-integration--2026-10-16)
är fortsatt evidens, inte körorder.


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
