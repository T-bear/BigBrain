# Git, checkpoints och återhämtning

Bindande detaljer till [AGENTS.md](../../AGENTS.md); avsnittsnummer i den bevarade texten avser AGENTS.md.

Policyn gäller enligt [ikraftträdandebeslutet](deployment/README.md#ägarbeslut-om-riskpolicyns-ikraftträdande--2026-10-27).
Git-skydd och recovery-ingång ligger kvar i [AGENTS §8–9](../../AGENTS.md#8-git-och-review-checkpoints); tidigare godkännandeväg finns under [Historisk väg](#historisk-väg).

## 8. Git och Review Checkpoints

- **REVIEW CHECKPOINT:** permanent auktorisering till commit/push av sammanhängande arbete inom
  redan auktoriserat scope för oberoende review. Kan vara ofullständigt, blockerat eller innehålla
  misslyckade experiment; naturlig reviewpunkt, experimentfel, beslut eller STOP ska vara beständigt.
- **MERGE CANDIDATE:** checkpoint vars acceptanskriterier och riskmotiverade verifiering är uppfyllda.

### Delegerad teknisk leverans efter uttryckligt ikraftträdande

Det på förhand godkända uppdragets scope, risknivå och riskram avgränsar leveransen. För nivå 1–3 ersätter
ikraftträdandebeslutet tillsammans med uppdragsauktoriseringen rutinmässigt separat exakt
mergegodkännande, deploymentgodkännande och STOP efter checkpoint. Det ger inte rätt att starta
ett nytt uppdrag. Nivå 3 kräver dessutom godkänd faktisk oberoende granskning av den exakta
kandidaten enligt tabellen. Inget nytt manuellt ägargodkännande av varje SHA eller deployment
krävs inom denna ram. Separat ägarbeslut krävs vid utökad ekonomisk risk, nya kostnader, utökning
utanför mandatet, hävning av frysningar och andra uttryckligen reserverade beslut. Saknas
auktorisering för berörd åtgärd eller ändras scope krävs nytt beslut innan åtgärden utförs.

| Risknivå | Delegering inom godkänt uppdrag efter ikraftträdande |
| --- | --- |
| 1 | Autonom teknisk leverans, inklusive merge och deployment, med fokuserad verifiering, relevanta CI-kontroller och befintliga drift-/leverantörsgrindar. |
| 2 | Autonom teknisk leverans med relevanta data-, åtkomst-, säkerhets- och återhämtningskontroller. Databasdeployment kräver separat granskning tills nivå 2-backupgrinden i avsnitt 3 är verifierad; grinden är fortsatt BLOCKED enligt backupunderlaget. |
| 3 | Autonom merge och deployment inom på förhand godkänt uppdrag och riskram, först efter godkänd faktisk oberoende granskning av exakt kandidat-SHA/tree mot rätt main-baslinje. Granskningsutfall och kandidatidentitet dokumenteras; ändrad kandidat kräver ny tillämplig granskning. Ingen ny manuell SHA- eller deploymentauktorisering från ägaren krävs inom ramen. Finance är alltid nivå 3 och förblir fryst. |

Även delegerad merge ska binda exakt kandidat-SHA/tree till verifierad main-baslinje, kontrollerad
diff och tillämpliga required checks. Verifiera oförändrade identiteter omedelbart före merge;
vid avvikelse eller blockerande kontroll: stoppa. Bevara historiken, verifiera publicerad main och
relevant CI för exakt merge-SHA före deployment. Dokumentera faktiskt levererad revision och
verifieringsresultat. Ingen rätt följer att kringgå GitHub-skydd, installerad gateway, root-approvals,
backup/recovery, leverantörsgränser eller bindande P0/P1. Oktober-gatewayen och admin-ACL:er ändras
inte genom delegeringen. Ekonomiska gränser, sekretess och samtliga frysningar gäller oförändrat.

Teknisk leverans är inte manuell produktacceptans. Obligatorisk iPhone/PWA-acceptans utförs av
ägaren och redovisas separat som återstående tills uttryckligt godkänd; CI eller autonoma
kontroller får inte registrera den som PASS. Två reviewvarv, två automatiska reparationsförsök
och resursramen i avsnitt 7 gäller även delegerad leverans.

### Publicering och spårbarhet för båda godkännandevägarna

Verifiera `origin/main` före och efter push och att remote HEAD matchar publicerad lokal HEAD.
Inga orelaterade ändringar får följa med. Dokumentationsändringar verifieras, committas separat
när endast dokumentation ändrats och pushas; lämna dem inte lokalt för att kod saknas.
Om kod inte får pushas uppdateras relevant dokumentation lokalt och samtliga väntande filer redovisas.
Commit/tree hämtas ur Gitmetadata för publiceringscommiten, inte genom självrefererande SHA i texten.
Checkpointevidensen ska kunna identifieras entydigt även efter senare reviewcommits.

## 9. Avbrott och blockerad implementation

Nästa session läser AGENTS, synkar GitHub, inspekterar working tree och relevant recovery,
bevarar orelaterat arbete och återanvänder giltiga resultat. Slutför ursprungligt scope före nytt
arbete; vid konflikt mellan repo och not, stoppa och rapportera i stället för att gissa.
En publicerad ofullständig checkpoint är inte automatiskt ett avbrott. Vid publicering avstäms
berörd recovery mot faktiskt tillstånd; lämna ingen falsk aktiv avbrottsstatus eller acceptansuppgift.

Vid reproducerbar **befintlig** korrekthets-, säkerhets-, vetenskaps- eller lineage-defekt som
kräver stopp får en avgränsad blocker-branch publiceras med endast säkra karakteriseringstester
och sanerad evidens: **BLOCKER HANDOFF — NOT MERGEABLE**. Avsiktligt felande test är tillåtet
endast med tydligt fel/resultat; det får aldrig kallas grön implementation. Ingen produktionsfix
eller refaktor får följa med. Rapportera verifierad remote-SHA och stoppa för arkitektreview.
Korrigering kräver separat uttryckligen auktoriserad checkpoint från verifierad main.
Blocker-branchen får inte mergas; main förblir accepterad auktoritet. Inget deploymenttillstånd
eller Git-undantag följer. Vanlig ofullständig implementation är inte automatiskt denna felkategori.

## 10. Långvariga processer

Ingen aktiv vänteloop: upprepa inte wait, polling eller statuskommandon utan ny information,
avvikelse eller beslutspunkt. Återuppta för analys/beslut, inte bara för att se att arbete pågår.
Kör långvariga deterministiska processer sessionsoberoende när säkert och auktoriserat.
Verifiera livslängd, ägarskap, resursgränser och beständig resultatlagring innan processen lämnas;
anta inte att en bakgrundsprocess överlever frånkoppling. Status, resultat och fel ska kunna läsas
av nästa session utan omstart. Använd befintliga mekanismer, inte ny infrastruktur av processvana.
En kritisk process får aldrig lämnas utan säker fel-/avbrottshantering. Om körning måste lämnas,
ge sanerad recovery enligt avsnitt 9 med tillstånd, resultatplats, hinder och nästa beslutspunkt.

## Historisk väg

Historik före det dokumenterade ikraftträdandet; ingen aktuell arbetsauktorisering.

### Ikraftträdande och gemensamma Git-skydd

Den delegerade modellen nedan är **INTE AKTIV** genom publicering eller merge av denna
dokumentation. Den träder i kraft först genom ett separat uttryckligt ägarbeslut som anger
exakt ikraftträdande revision (Git-SHA för policytexten). Beslutet ska dokumenteras beständigt
med revision och omfattning i befintlig deploymentdokumentation. Saknas ett verifierbart sådant
beslut gäller befintliga merge- och deploymentkrav för samtliga risknivåer. Detta uppdrag och
dess publicering aktiverar inte modellen. Tidigare giltiga godkännanden bevaras inom sin scope.

### Befintlig godkännandeväg före ikraftträdande

Följande exakta SHA-/stopp-/godkännanderegler gäller alla uppdrag före ikraftträdandet.
Efter ikraftträdandet gäller den delegerade modellen nedan även nivå 3; agentens egen
kontroll eller CI ersätter aldrig dess krav på faktisk oberoende kandidatgranskning.

- **ACCEPTED SHA:** exakt kandidat-SHA som arkitekten granskat mot rätt main-baseline, ägaren
  uttryckligen godkänt och som verifierats oförändrad före merge. Ingen annan SHA får mergas.

Kontrollera scope/secrets, publicera kod, tester och nödvändig dokumentation tillsammans,
rapportera enligt avsnitt 11 och **STOPPA**. Publicering är aldrig acceptance, merge-, deployment-,
utökat scope-, nästa checkpoint- eller tradingtillstånd. Fortsättning kräver continue-beslut inom
auktoriserad scope; lägg nya commits ovanpå historiken till nästa reviewpunkt eller avslut.

Före merge: verifiera exakt godkänd SHA, oförändrad förväntad main och relevanta required checks.
Avvikelse kräver STOP/re-review. Pusha kontrollerad merge och verifiera relevant main-CI för exakt
SHA; återanvänd giltig lokal evidens. Deployment kräver separat uttrycklig auktorisering.
Om bestående fakta kräver en separat reconciliation-commit ska relevant CI verifieras för dess SHA.
