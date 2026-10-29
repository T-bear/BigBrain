# Riskpolicy och arbetsinsats

Bindande detaljer till [AGENTS.md](../../AGENTS.md); avsnittsnummer i den bevarade texten avser AGENTS.md.

Kärnans fail-closed-regel och backupgrinden ligger kvar i [AGENTS §3](../../AGENTS.md#3-riskbaserad-arbetsinsats).

## 3. Riskbaserad arbetsinsats

### Risknivå för uppdraget

Risknivå anges i uppdraget utifrån faktisk konsekvens och berörd behörighetsgräns.
Vid osäkerhet används den högre nivån. Codex får inte själv sänka risknivån för att undvika
granskning. Ett bekvämlighetsmål gör inte en databas- eller privilegieändring till nivå 1.

| Risknivå | Omfattning och gräns |
| --- | --- |
| 1 – hemmabekvämlighet | Begränsad presentation och bekvämlighetsfunktion, exempelvis läsbaserad skollunchvisning, utan ändrad datalagring, åtkomstkontroll eller privilegieökning. Befintliga leverantörs-, säkerhets- och driftvillkor gäller. |
| 2 – data/åtkomst | Ändringar som påverkar beständiga data, databasstruktur eller åtkomst till data, utan nivå 3-konsekvens. Dataintegritet, återhämtning och avgränsad åtkomst ska verifieras. |
| 3 – Finance/pengar/kritiska privilegier | Finance är alltid nivå 3. Pengar, handels-/kapitalauktoritet och kritiska privilegier kräver motsvarande hög verifiering och oberoende granskning. Permanenta generella root-, sudo- och Docker-root-ekvivalenta privilegieökningar är alltid nivå 3. |

Risknivå är inte ett merge-, deployment-, root- eller upptiningsgodkännande. Befintliga
giltiga godkännanden ska återanvändas inom sin omfattning; installerade skydd och aktuella
kontrakt får inte kringgås. Oktober-gatewayen lämnas oförändrad; att behålla, förenkla eller
ersätta den kräver separat beslut. Admin-ACL:er ändras inte; personer och enheter med
administratörsåtkomst är ännu inte beslutade. Inga nya betaltjänster eller handelsrättigheter följer.

### Proportionell arbetsinsats och verifiering

Använd lägsta lämpliga arbetsinsats; eskalera vid konkret behov. Detta klassificerar uppgiften,
inte ett påstående att agenten kan ändra modellinställningar. LOW/MEDIUM/HIGH nedan beskriver
arbetsinsats, inte de tre risknivåerna eller rätt att sänka ett uppdrags granskning.

| Nivå | Typisk omfattning |
| --- | --- |
| LOW | Mekanisk dokumentation/formatering, isolerad UI/text, små mekaniska teständringar. |
| MEDIUM | Normal implementation, flerfilsrefaktor, integration och vanlig felsökning. |
| HIGH | Finance-korrekthet, beräkningar/modeller och vetenskaplig integritet; marknadsdatasemantik/lineage och temporalitet; arkitekturbeslut, migrationer, säkerhetsgränser, samtidighet/race conditions, svår felsökning, stora riskfyllda refaktorer och kritisk kodreview; PAPER/LIVE/trading/kapitalauktoritet. |

Inom ägarens scope beslutar arkitekten verifieringsnivå efter risk och påverkansområde;
Codex följer nivån och gör samma bedömning när den inte anges. Ansvarskedjan är ägare → arkitekt → Codex.
Nya funktioner ska ha relevanta tester. Kör fokuserade tester först; efter en avgränsad korrigering
verifieras ändringen och berörda beroenden. Utöka vid konkret risk eller obligatoriskt krav.
Review Checkpoints verifieras proportionellt; Merge Candidates får den bredaste verifiering som
faktisk risk motiverar, inte rutinmässigt hela repositoryts testsviter.

Återanvänd giltig evidens när relevant tree/kod, beroenden och antaganden är oförändrade.
Ny commit/push kräver inte i sig omtest. Vid oförändrat godkänt kandidatträd och förväntad main
räcker normalt SHA/tree-verifiering och relevant CI. Historiska tunga Finance-kontroller är
inte ceremonimallar för andra uppgifter. Rena dokumentändringar behöver inte applikationstester.
Redovisa körda kommandon/resultat, återanvänd evidens och varför kontroller saknas eller inte gäller.
