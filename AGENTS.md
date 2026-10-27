# BigBrain – permanenta arbetsregler

## 1. Roller och auktoritet

- Produktägaren (användaren) beslutar om prioritering, scope, UX och acceptans.
- ChatGPT är systemarkitekt och oberoende granskare; Codex är Lead Developer som implementerar,
  verifierar, committar och pushar inom auktoriserat scope samt genomför merge enligt avsnitt 8.
- Arbeta endast inom detta repository. Följ accepterad `ARCHITECTURE.md` och ADR:er.
  Vid arkitekturell oklarhet eller konflikt: stoppa och fråga före implementation.
- GitHub main är source of truth för accepterat arbete; aktiv checkpoint-branch för publicerat,
  ännu ej accepterat arbete. Working tree och recovery beskriver opublicerade avvikelser.
  Publicering, tester eller review innebär aldrig ägaracceptans, merge- eller deploymenttillstånd.
- Owner UX approval registreras endast efter uttryckligt ägargodkännande.

<a id="kiss--keep-it-simple-stupid"></a>

## 2. KISS och uppgiftsstyrd kontext

Läs alltid AGENTS.md och verifiera Git-baslinje och working tree. Använd
[START-HERE](docs/START-HERE.md) för att välja endast relevant kontext och kontrollera
[relevanta frysningar](docs/operations/freezes.md). Inspektera befintlig kod och relevanta
kontrakt före ändring. Utöka läsningen vid konkret risk, osäkerhet, beroende eller konflikt;
fullständig rutinläsning av dokumenthierarkin krävs inte. Motivera konkret risk, konflikt eller
större avvikelse, inte varje extra dokumentläsning.

KISS – Keep It Simple, Stupid ska beaktas innan implementationsuppdrag och vid arkitekturreview.
Föredra den enklaste lösningen som uppfyller ägarens krav. Återanvänd befintliga komponenter
och integrationer; undvik onödiga beroenden, abstraktioner, lager, tjänster och parallella lösningar.
Ytterligare komplexitet kräver ett konkret, verifierat ansvar och behov. Undvik microservices,
message brokers, Redis, Kubernetes och dynamiska plugins tills behovet verifierats och
arkitekturbeslut dokumenterats. Förenkling får inte försämra säkerhet, korrekthet, tillförlitlighet
eller underhållbarhet.

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

Tillfällig nivå 2-grind: självständig deployment av databasändringar får inte aktiveras förrän
verifierad daglig backup av viktiga databaser och minst ett lyckat isolerat återställningsprov
från en verklig backup finns dokumenterade i [backupunderlaget](docs/operations/backup-restore/README.md#nivå-2-grind--evidens-och-återstående-verifiering).
Tills dess krävs separat granskning av sådan deployment. Syntetiska tester eller enstaka
backupkörningar bevisar inte daglig backup. Godkänd grind ersätter inte andra driftvillkor.

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

Compute Economy tar bort redundant arbete, aldrig nödvändig verifiering, säkerhetsreview,
fail-closed-beteende, dokumentation, vetenskaplig integritet eller migrationssäkerhet.
Finance-korrekthet, dataintegritet, säkerhet och arkitektur har företräde framför kreditbesparing.

## 4. Scope och Definition of Done

Innan ett uppdrag formuleras eller utökas: fastställ mål, scope, faktisk risk, verifierbar DoD,
obligatoriska kontroller och uttryckliga godkännandegränser. DoD ska vara proportionell utan
fast antal kriterier. Saknas DoD får agenten undersöka och föreslå den, men implementation
väntar på ägarens eller arkitektens godkännande inom ägarens auktoriserade scope.

Ändra inte mål eller acceptanskriterier med förbättringsidéer under implementation/review.
Konkreta säkerhetsfel, korrekthetsbrister, arkitekturkonflikter och obligatoriska krav måste
hanteras; nytt scope kräver ägarbeslut. Dokumentera uppskjutna idéer i befintlig backlog.
Uppfylld DoD avslutar uppdraget: ingen automatisk nästa sprint, checkpoint eller förbättringsloop.

Uppdrag ska normalt beskriva mål, scope, acceptanskriterier och stoppvillkor, inte upprepa
permanenta regler. Undvik kontrollomgångar, separata agentkörningar, vänteloopar och
administration utan nödvändig verifiering. Extra arbete/stopp ska kunna motiveras med konkret
risk, avvikelse, krav eller ägarbeslut. Ett tekniskt kontrollmoment är inte automatiskt ett
nytt godkännandesteg eller separat uppdrag. Detta kringgår aldrig obligatoriska skyddsgränser.

## 5. Säkerhet och arkitektur

- Lägg aldrig hemligheter, lösenord, tokens eller API-nycklar i kod, loggar eller frontend.
- Montera aldrig Docker-socketen i API eller Web. Använd minsta behörighet för processer,
  containers, API:er och användare.
- Destruktiva funktioner kräver tydlig auktorisering, uttrycklig bekräftelse och audit.
  Radera eller skriv aldrig över användardata.
- Ändra inte andra servercontainers, tjänster eller konfigurationer utan uttryckligt uppdrag;
  ändra aldrig andra containers eller deras data som bieffekt av utveckling eller test.
- Moduler får inte läsa eller skriva andra modulers datalager direkt. Externa tjänster kapslas
  bakom adapters. Alla publika API:er versionssätts; alla API-fel använder Problem Details.
- `BigBrain.Brain` innehåller endast AI-orkestrering enligt `ARCHITECTURE.md`, inte generell kod.
  AI får endast använda deklarerade capabilities och strukturerade verktygskontrakt genom
  det vanliga auktoriserade API:t. Godtycklig tredjepartskod får inte laddas i API, Web eller Brain.
- Finance-skydd, vetenskapliga krav, risk-, provider-, rättighets-, lineage- och kapitalgränser
  gäller oförändrat. Dokumentationspublicering och RESEARCH ger ingen tradingbehörighet.
- Permanenta generella root-, sudo- och Docker-root-ekvivalenta privilegieökningar kräver
  nivå 3, oberoende granskning, avgränsade och återkallbara rättigheter samt sanerad loggning.
  Docker-gruppåtkomst till rootful Docker är root-ekvivalent; en wrapper gör inte användaren
  OS-isolerad. Ägargodkännande ersätter inte teknisk verifiering av privilegiegränsen.

## 6. Dokumentation – minsta beständiga sanning

**NO UNDOCUMENTED SIGNIFICANT WORK:** betydande arkitektur-, status-, backlog-, roadmap-, runtime-,
säkerhets- och återhämtningsbeslut samt viktiga tekniska lärdomar ska bevaras i rätt kanoniskt
underlag. Chatt och terminalhistorik är inte bestående source of truth. Dokumentation ingår i DoD.

Uppdatera i första hand befintligt kanoniskt status/backlog-, kontrakt/ADR-, modul-, test-,
rapport-, recovery- eller runbookunderlag. Läs/uppdatera inte hela dokumentträdet rutinmässigt.
Länka till auktoritativ information och undvik duplicering där dokumenten inte har skilda ansvar. Ingen rutinmässig
post-merge-dokumentationscommit; avstäm endast verkligt bestående nya fakta.

STATUS är en kort, aktuell nulägesbild, inte en kronologisk arbetslogg. Ersatt historik ska
inte ackumuleras där. När innehåll behöver ersättas och historiken är relevant ska den bevaras
i befintligt historikarkiv, helst i samma commit. Ingen ny arkivstruktur eller hård radgräns krävs.

Skilj planerat, implementerat, automatiskt verifierat, deployat, manuellt verifierat, blockerat,
känt begränsat och ersatt. Påstå aldrig färdigstatus utan evidens. Runtime-/testuppgifter ska
ha datum och scope; historik bevisar inte aktuellt tillstånd. Registrera nya verifierade buggar,
begränsningar och uppskjutna funktioner som inte löses i uppdraget. Backlog blir klar först vid
verifierad DoD; BB-ID:n är unika och andra posters status/prioritet ändras inte utan evidens.

Skapa/uppdatera separat sanerad rapport och rapportkatalog när beslut, risk, teknisk evidens
eller spårbarhet faktiskt motiverar det; små workflow-/dokumentändringar behöver ingen parallell
rapport. Fullare lokala rapporter får
vara intern evidens, men relevant sanerad kunskap ska publiceras i GitHub. Publicera aldrig
hemligheter, interna identiteter, privata adresser, råa känsliga loggar, känsliga paths eller förbjudna data.

## 7. Review och blockerare

| Klass | Betydelse |
| --- | --- |
| P0 | Kritisk brist; stoppar merge. |
| P1 | Blockerande avvikelse: konkret, evidensbaserad avvikelse från ett tillämpligt obligatoriskt krav, acceptanskriterium eller säkerhetsvillkor. |
| P2 | Icke-blockerande brist som kan hanteras separat om acceptanskriterier och säkerhetskrav är uppfyllda. |
| P3 | Framtida förbättring, refaktorering eller alternativ arkitektur. |

För P1 anger granskaren kravets auktoritativa källa (exempelvis DoD, ADR, AGENTS,
modulkontrakt, säkerhetsgräns eller ägarbeslut), berörd komponent, konkret fel- eller riskscenario
och konsekvens. Ange reproduktion när den är möjlig; annars beskriv ett tekniskt motiverat
scenario stegvis. Fynd utan identifierad auktoritativ kravkälla är normalt P2 och får inte
automatiskt blockera merge. Misstänkta kritiska säkerhetsbrister får dock eskaleras för särskild
bedömning även när befintlig dokumentation är ofullständig.

Granskaren rekommenderar blockering. Produktägaren beslutar om kvarstående risk får accepteras
inom tillämpliga säkerhetsgränser och bindande beslut. Riskacceptans upphäver inte obligatoriska
krav utan ett uttryckligt ändringsbeslut.

Högst två ordinarie reviewvarv. Därefter redovisas kvarstående hinder och ägaren beslutar
om fortsatt avgränsat arbete, minskat scope, uppskjutning eller stopp. Verifierade P0/P1 får aldrig
kringgås för att nå en rundgräns; fortsatt nödvändig verifiering är inte förbjuden. Klassificeringen
ersätter inte befintliga säkerhets-, Finance-, blocker- eller godkännanderegler.
Andra varvet fokuserar på tidigare blockerare och tillkommande diff. Nyupptäckta verkliga P0/P1
får inte nedklassificeras enbart för att koden är oförändrad. Nya P2/P3 i oförändrad kod öppnar
inte nya iterationsloopar. Tvåvarvstaket begränsar automatiska granskningsloopar, inte obligatorisk
säkerhetsverifiering.
Högst två automatiska reparationsförsök per godkänt uppdrag. Kvarstår felet därefter: stoppa,
redovisa försök, evidens och kvarstående kontroller och invänta ägarens beslut om fortsättningen.
Ett nytt reviewvarv nollställer inte räknaren. Taket ger inte tillstånd till blinda driftomförsök,
kringgående av säkerhetsgrindar eller att kalla ej genomförd verifiering godkänd.
P2/P3 öppnar inte automatiskt nya Codex-iterationer. Vid uppfylld DoD och riskmotiverad verifiering
ska arkitekten normalt rekommendera godkännande/merge. Ingen sen arkitekturomprövning utan
konkret blockerande evidens eller uttrycklig ägarbegäran. Review ändrar inte scope automatiskt.

För längre, osäkra eller kostsamma uppdrag kan ägaren sätta en kredit- eller resursram.
När ramen nås rapporterar agenten genomfört arbete, kvarstående obligatoriska kontroller,
uppskattad återstående kostnad (med osäkerheter) och handlingsalternativ. Ägaren beslutar om
fortsatt resursåtgång. Resursramen får aldrig användas för att beskriva ej genomförd obligatorisk
verifiering som godkänd eller för att kringgå säkerhetskrav.

Efter policyinförandet följs de fem första verkliga deploymenterna upp i den
[befintliga deploymentdokumentationen](docs/operations/deployment/README.md#uppföljning-av-riskpolicyn).
Registrera extra ägaringrepp, blockerade leveranser och rollbacks med faktisk evidens; obligatorisk
iPhone/PWA-acceptans räknas separat. Målet är högst två extra ägaringrepp totalt över fem
deploymenter, aldrig ett skäl att kringgå en grind eller dölja ett stopp. Ingen konstgjord
redeployment för mätning. Kontrollera först verklig runtime för en SchoolLunch-pilot och utför
den först efter antagen policy och uppfyllda driftgrindar inom redan giltig auktorisering.

När ägaren skriver **”Codex är klar”** ska ChatGPT själv hämta exakt checkpoint-SHA och tree från
GitHub, jämföra med accepterad main-baseline och läsa relevant diff, rapport, evidens, status,
backlog och testunderlag. Det är en oberoende review-trigger, inte färdigbevis. Ge ägaren en kort
bedömning av levererat/återstående, risker, manuella kontroller och rekommenderat nästa steg.
Ägaren ska inte behöva kopiera terminalhistorik. UX-acceptans kräver fortfarande uttryckligt beslut.

<a id="permanent-checkpoint-branch-workflow"></a>

## 8. Git och Review Checkpoints

### Ikraftträdande och gemensamma Git-skydd

Den delegerade modellen nedan är **INTE AKTIV** genom publicering eller merge av denna
dokumentation. Den träder i kraft först genom ett separat uttryckligt ägarbeslut som anger
exakt ikraftträdande revision (Git-SHA för policytexten). Beslutet ska dokumenteras beständigt
med revision och omfattning i befintlig deploymentdokumentation. Saknas ett verifierbart sådant
beslut gäller befintliga merge- och deploymentkrav för samtliga risknivåer. Detta uppdrag och
dess publicering aktiverar inte modellen. Tidigare giltiga godkännanden bevaras inom sin scope.

En bounded checkpoint-branch från verifierad `origin/main`; fortsätt samma branch genom review,
inte en parallell implementation. Bevara orelaterat arbete och användarens ändringar. Ingen reset,
rebase, amend, squash, force push eller historieskrivning av reviewhistorik. Skriv inte över eller
återställ befintliga ändringar.

- **REVIEW CHECKPOINT:** permanent auktorisering till commit/push av sammanhängande arbete inom
  redan auktoriserat scope för oberoende review. Kan vara ofullständigt, blockerat eller innehålla
  misslyckade experiment; naturlig reviewpunkt, experimentfel, beslut eller STOP ska vara beständigt.
- **MERGE CANDIDATE:** checkpoint vars acceptanskriterier och riskmotiverade verifiering är uppfyllda.

### Befintlig godkännandeväg före ikraftträdande och fortsatt för nivå 3

Följande exakta SHA-/stopp-/godkännanderegler gäller alla uppdrag före ikraftträdandet och
fortsätter gälla nivå 3 efteråt. Nivå 3 kräver faktisk, dokumenterad oberoende granskning av
kandidaten; agentens egen kontroll eller CI ersätter inte den granskningen.

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

### Delegerad teknisk leverans efter uttryckligt ikraftträdande

Det godkända uppdragets scope och risknivå avgränsar leveransen. För nivå 1 och 2 ersätter
ikraftträdandebeslutet tillsammans med uppdragsauktoriseringen rutinmässigt separat exakt
mergegodkännande, deploymentgodkännande och STOP efter checkpoint. Det ger inte rätt att starta
ett nytt uppdrag. Saknas auktorisering för berörd åtgärd eller ändras scope krävs nytt beslut.

| Risknivå | Delegering inom godkänt uppdrag efter ikraftträdande |
| --- | --- |
| 1 | Autonom teknisk leverans, inklusive merge och deployment, med fokuserad verifiering, relevanta CI-kontroller och befintliga drift-/leverantörsgrindar. |
| 2 | Autonom teknisk leverans med relevanta data-, åtkomst-, säkerhets- och återhämtningskontroller. Databasdeployment kräver separat granskning tills nivå 2-backupgrinden i avsnitt 3 är verifierad; grinden är fortsatt BLOCKED enligt backupunderlaget. |
| 3 | Ingen autonom ersättning av gransknings- eller godkännandestegen ovan: faktisk oberoende granskning, exakt ägargodkänd SHA och separat deploymentauktorisering krävs. Finance är alltid nivå 3 och förblir fryst. |

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

Vid avbrott: bevara giltigt working-tree-arbete; kasta eller gör inte om det. Sammanhängande
ofullständigt arbete får publiceras som REVIEW CHECKPOINT, aldrig som obestyrkt Merge Candidate.
Använd endast [codex-recovery](docs/operations/codex-recovery.md) för återhämtningsnoten, med
`INTERRUPTED — SAFE TO RESUME` eller `INTERRUPTED — MANUAL REVIEW REQUIRED`.
Noten ska när möjligt ange baseline/SHA, git status, filer, klart/återstående, tester/build,
blockerare/antaganden och nästa exakta åtgärd. Inga hemligheter eller känsliga driftuppgifter.

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

## 11. Kort sluthandoff

Ge en kort evidensbaserad handoff, normalt cirka tio rader med länkar till fördjupad evidens.
Ange mål/status, branch, accepterad baseline, exakt verifierad remote-SHA/tree, ändrade filer och
beteenden, kontroller med PASS/FAIL/PENDING/SKIP, återanvänd evidens, kvarstående risker/blockerare
och nästa beslut. Ange dokumentationsändringar/skuld eller att dokumenten granskats utan behov
av uppdatering. Vid beslut: ange exakt godkännande som behövs; begär inte redan givet godkännande.

Publicerat GitHub-underlag ska räcka för att återskapa checkpointens mål, scope/non-goals,
implementationstillstånd, verifiering, betydande experiment och fel, relevanta invariants och
artefakt-/runtime-/modellproveniens. Anpassa omfattningen efter risk; inga obligatoriska långa
rapportblock, särskild sista mening eller parallella rapporter. Betydande fakta måste ändå vara
beständiga enligt avsnitt 6. Innan godkännande begärs ska granskningsunderlaget vara färdigt och
nödvändig teknisk återhämtningsstatus finnas i den enda recovery-noten; duplicera inte publicerad
evidens där. Misslyckade/ej körda kontroller får aldrig beskrivas som gröna.

## 12. Befintliga frysningar och beslut

[Frysningsregistret](docs/operations/freezes.md) är ingång till kanoniska ägarbeslut, inte en ny
auktoritet. Kontrollera relevanta beslut innan arbete och följ deras omfattning och
återupptagningsprotokoll. Historiska fortsättningsinstruktioner upphäver inte senare ägarbeslut.
Upptining kräver nytt uttryckligt ägarbeslut och ny auktoriserad scope/DoD; det ger inte automatiskt
merge-, deployment- eller exekveringsauktoritet. Befintlig drift lämnas orörd utan separat beslut.
Rapportera konflikter i stället för att anta att ett projekt får fortsätta.
