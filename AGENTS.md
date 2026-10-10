# BigBrain – permanenta arbetsregler

## Roller

- Produktägare: användaren.
- Systemarkitekt och granskare: ChatGPT.
- Lead Developer: Codex.

## Omfattning och arbetssätt

- Arbeta endast inom detta repository.
- Inspektera befintlig kod och relevanta dokument innan ändringar görs.
- Följ den beslutade arkitekturen i `ARCHITECTURE.md` och dokumenterade Architecture Decision Records.
- Vid oklarheter eller konflikt med arkitekturen: stoppa och fråga innan implementation.
- Ändra inte andra servercontainers, tjänster eller konfigurationer utan uttryckligt uppdrag.
- Skapa inte abstraktioner, lager eller generell infrastruktur utan ett konkret och verifierat ansvar.
- Undvik microservices, message brokers, Redis, Kubernetes och dynamiska plugins tills ett verifierat behov finns och arkitekturbeslutet har dokumenterats.
- Varje sprint ska ha ett tydligt mål och en tydlig Definition of Done. Om en ny idé uppstår under sprinten och inte krävs för att uppnå sprintmålet ska den dokumenteras som en framtida förbättring i stället för att implementeras direkt.

## Kontinuitet och Adaptive Reasoning / Compute Economy

- Läs alltid `AGENTS.md` och minsta auktoritativa kontext som uppgiften kräver.
  [Start here](docs/START-HERE.md) visar dokumentauktoritet och läsordning för vald kontext;
  den riskbaserade omfattningen här styr tillämpningen. Läs inte rutinmässigt hela hierarkin
  vid lokala uppgifter där auktoritet och scope redan är tydliga.
- **NO UNDOCUMENTED SIGNIFICANT WORK:** betydande arkitektur-, status-, backlog-, roadmap-,
  runtime-, säkerhets- och återhämtningsbeslut samt viktiga tekniska lärdomar ska dokumenteras
  i rätt kanoniska dokument. Chatt och terminalhistorik är inte bestående source of truth.
- Använd lägsta resonemangsnivå som passar uppgiften; eskalera när komplexiteten kräver det.
  LOW: mekanisk dokumentation, formatering, repetitiva specificerade ändringar, isolerad
  UI/text och små mekaniska testuppdateringar. MEDIUM: normal implementation, flerfilsrefaktor,
  integration och vanlig felsökning. HIGH: Finance-korrekthet och beräkningar/modeller,
  marknadsdatasemantik/lineage, arkitekturbeslut, migrationer, säkerhetsgränser, samtidighet,
  race conditions, svår felsökning, stora riskfyllda refaktorer och kritisk kodreview.
- Compute economy tar bort redundant arbete, aldrig nödvändiga tester, verifiering, säkerhetsreview,
  fail-closed-beteende, dokumentation, vetenskaplig integritet eller migrationssäkerhet.
  Finance-korrekthet, säkerhet och arkitekturintegritet går före användningsoptimering.
  Policyn är uppgiftsklassificering, inte ett påstående att verktyget kan byta modellinställning.

### Riskbaserad läsning och verifiering

- Inom ägarens auktoriserade scope beslutar ChatGPT/systemarkitekten verifieringsnivå utifrån
  faktisk risk, ändringens påverkansområde och berörda komponenter. Codex följer nivån och gör
  samma bedömning när uppdraget inte anger någon. Ansvarskedjan är ägare → systemarkitekt → Codex.
- Under implementation används i första hand fokuserade tester för den ändrade ytan. Kör inte
  oförändrade fulla testsviter igen enbart för att en ny commit, push eller checkpoint tillkommit.
- En Review Checkpoint verifieras proportionellt mot sin delta och risk; full repository-verifiering
  är inte automatisk. En Merge Candidate får den bredaste verifiering som faktisk risk och
  påverkansområde motiverar. Mekaniska, dokumentationsbaserade, isolerade eller andra lågriskändringar
  kräver inte fulla testsviter enbart av processvana.
- Hög verifieringsnivå kvarstår för Finance-korrekthet/vetenskaplig integritet, marknadsdata-lineage
  och temporal integritet, databasmigrationer, samtidighet/race conditions, säkerhetsgränser samt
  PAPER/LIVE/trading/kapitalauktoritet. Historiska tunga Finance-checkpoints visar sin egen risknivå;
  de är inte ceremonimallar för varje framtida uppgift.
- Giltig evidens får återanvändas medan relevant tree/kod och antaganden är oförändrade. När exakt
  godkänt kandidat-tree mergas oförändrat mot förväntad oförändrad main räcker normalt exakt
  SHA/tree-verifiering och relevant CI; samma lokala verifiering upprepas inte automatiskt.
- Codex får när som helst utöka läsning/testning vid konkret större påverkansområde, arkitekturell
  osäkerhet, vetenskaplig risk, säkerhetsrisk eller annat verifierat behov. Ange kort varför.
  Detta utökar inte uppdragets scope eller befogenheter; arkitekturkonflikt och blockerregler gäller.

## Säkerhet och data

- Lägg aldrig hemligheter, lösenord, tokens eller API-nycklar i kod, loggar eller frontend.
- Montera aldrig Docker-socketen i API eller Web.
- Använd minsta möjliga behörighet för processer, containers, API:er och användare.
- Destruktiva funktioner kräver tydlig auktorisering, uttrycklig bekräftelse och audit.
- Radera eller skriv aldrig över användardata.
- Ändra aldrig andra servercontainers eller deras data som bieffekt av utveckling eller test.

## Arkitektur- och API-regler

- Moduler får inte läsa eller skriva andra modulers datalager direkt.
- Externa tjänster kapslas bakom adapters.
- Alla publika API:er versionssätts.
- Alla API-fel använder Problem Details.
- `BigBrain.Brain` innehåller endast AI-orkestrering enligt `ARCHITECTURE.md`; det är inte ett allmänt dumpningslager.
- AI får endast använda deklarerade capabilities och strukturerade verktygskontrakt genom det vanliga auktoriserade API:t.
- Godtycklig tredjepartskod får inte laddas i API-, Web- eller Brain-processen.

## Kvalitet och redovisning

- Nya funktioner ska ha relevanta tester.
- Kör build och tester enligt den riskbaserade verifieringsnivån ovan. Redovisa också återanvänd
  evidens och varför en kontroll inte är tillämplig; dokumentationsändringar behöver inte kodtester.
- Redovisa exakt vilka filer och beteenden som ändrats samt vilka build- och testkommandon som körts.
- Om build eller test inte kan köras ska orsaken redovisas tydligt.

### Systemarkitektens review-trigger

När ägaren säger **"Codex är klar"** ska ChatGPT behandla det som en review-trigger, inte som tillräcklig färdigevidens. ChatGPT ska själv hämta den aktiva checkpoint-branchens exakta Review Checkpoint SHA och tree, jämföra med rätt accepterad main-baseline och läsa rapport, diff, evidens, status, backlog och testunderlag från GitHub och därefter ge ägaren en förenklad systemarkitektgranskning: vad som gjorts, vad som är partiellt eller återstår, problem/risker, vad ägaren bör verifiera manuellt och rekommenderat nästa steg. Owner UX approval får endast registreras efter ägarens uttryckliga godkännande.

## Git

- Ägarens permanenta Review Checkpoint-auktorisering nedan tillåter commit/push av sammanhängande arbete inom en redan auktoriserad checkpoint. Merge kräver separat explicit godkännande av exakt SHA. Ingen reset, rebase, force push eller historieskrivning; orelaterat arbete bevaras.
- Skriv inte över eller återställ användarens befintliga ändringar.

## Interrupted-run recovery

Om en körning avbryts innan avgränsat arbete är klart ska giltiga working-tree-ändringar bevaras och inte göras om eller kastas. Sammanhängande ofullständigt arbete får publiceras som REVIEW CHECKPOINT enligt den permanenta auktoriseringen nedan; det får inte kallas MERGE CANDIDATE utan uppfyllda acceptance criteria och genomförd riskmotiverad verifiering. Skriv när möjligt en sanerad återhämtningsnot enligt `docs/operations/codex-recovery.md` med baseline/source-of-truth-SHA, git status, ändrade filer, exakt klart/återstående arbete, körda test/build-resultat, blockerare/antaganden och nästa exakta åtgärd. Använd endast denna enda plats och statusen `INTERRUPTED — SAFE TO RESUME` eller `INTERRUPTED — MANUAL REVIEW REQUIRED`.

En senare Codex-session ska läsa `AGENTS.md`, synka/verifiera GitHub, inspektera working tree och återhämtningsnoten, säkerställa att orelaterade ändringar bevaras och fortsätta giltigt verifierat arbete utan onödig omkörning. Slutför ursprunglig scope före nytt arbete. Om repositoryt och noten motsäger varandra: stoppa och rapportera konflikten i stället för att gissa. Återhämtningsnoten får aldrig innehålla hemligheter, credentials, privata adresser, råa känsliga loggar eller förbjudna identifierare/data. GitHub main är source of truth för accepterat arbete; den aktiva checkpoint-branchens publicerade Review Checkpoints är source of truth för pågående, ännu ej accepterat arbete. Working tree och återhämtningsnoten beskriver uttryckligen opublicerade avvikelser och avbrott.

## Documentation and publication completion rule

Dokumentation är en del av Definition of Done. Bedöm uppdateringsbehov utifrån uppgiftens delta
och risk; uppdatera minsta kanoniska underlag som framtida utvecklare behöver. Det kan vara
status/backlog, arkitektur/ADR, modulkontrakt, testinstruktion, rapport, recovery eller runbook.
Läs eller uppdatera inte hela dokumentträdet rutinmässigt. Duplicera inte samma status/evidens över
STATUS, BACKLOG, roadmap, moduldokument, recovery och rapport om de inte har skilda ansvar; länka
till den auktoritativa uppgiften. Betydande fakta får fortfarande inte lämnas enbart i chatten.

Skapa ingen rutinmässig post-merge reconciliation-commit. Avstäm dokumentation endast när
acceptansen skapar en bestående source-of-truth-faktauppgift som faktiskt kräver en dokumentändring.

### Statusprincip

- Skilj uttryckligen mellan planerat, implementerat, automatiskt verifierat, deployat, manuellt verifierat, blockerat, känt begränsat och ersatt.
- Beskriv aldrig något som färdigt utan faktisk evidens.
- Daterad runtime- eller testinformation ska ha datum och scope; historisk evidens ersätter inte aktuell status.

### Backlogprincip

- Registrera nya verifierade buggar, begränsningar och uppskjutna funktioner när de inte löses i samma uppdrag.
- Markera en post som klar först när dess Definition of Done är verifierad.
- BB-ID:n ska vara unika. Ändra inte andra posters prioritet eller status utan evidens.

### Rapportprincip

Bevara långsiktigt relevant kunskap i rätt befintlig kanonisk dokumentation. Skapa/uppdatera en
sanerad rapport och rapportkatalog när separat checkpoint-evidens behövs för oberoende review;
en liten workflow-/dokumentändring behöver inte en parallell rapport. Lokala fullrapporter får
behållas som intern evidens, men relevant sanerad kunskap ska finnas i GitHub. Hemligheter, interna
identiteter, privata adresser, råloggar och känsliga paths får inte publiceras.

### Commit- och pushprincip

- Repositoryts publicerade dokumentation och Git-historik är source of truth mellan agentsessioner; en ny agent ska kunna återskapa aktuell status utan terminalhistorik.
- Dokumentationspublicering innebär aldrig deployment eller runtimeändring. Sådana åtgärder kräver separat uttrycklig auktorisering.
- Privat/rå driftstatus, hemligheter, råa providerpayloads och känsliga privata uppgifter får aldrig
  publiceras. Sanerad operationell evidens som behövs för oberoende arkitektreview publiceras däremot
  som handoff på en review-branch/checkpoint, även när driftåtgärden inte kräver dokumentändring på
  main. Publicering är inte acceptance eller merge: Codex stoppar efter push och ChatGPT granskar
  branchen självständigt. Main får operativ dokumentation endast när den granskade informationen
  faktiskt hör hemma i bestående source of truth, enligt ordinarie exakt-SHA-godkännande.
- Kod, tester och tillhörande dokumentation publiceras tillsammans vid en sammanhängande reviewpunkt, även vid misslyckad acceptance eller blocker. Klassificera REVIEW CHECKPOINT eller, först efter genomförd riskmotiverad verifiering och uppfyllda acceptance criteria, MERGE CANDIDATE. Publication är aldrig acceptance.
- Om endast dokumentation ändrats ska den verifieras, få en separat dokumentationscommit och pushas. Dokumentation får inte lämnas lokalt enbart för att ingen kod ändrades.
- Om kod inte får pushas ska relevant dokumentation ändå uppdateras lokalt och samtliga väntande filer redovisas.
- Inga orelaterade ändringar får följa med. Ingen force push. `origin/main` ska verifieras före och efter push.
- Vid ren analys eller read-only-verifiering där ingen dokumentation behöver ändras ska slutsvaret ange att dokumenten granskats utan uppdateringsbehov.

### Kompakt ägargodkännande vid stopp

### Blocker handoff — not mergeable

Vid en reproducerbar befintlig korrekthets-, säkerhets-, vetenskaps- eller lineage-defekt
som kräver stopp får Codex publicera en avgränsad blocker-branch med endast säkra
karakteriseringstester och sanerad dokumentation/evidens. Status ska vara
**BLOCKER HANDOFF — NOT MERGEABLE**. Avsiktligt felande karakterisering är tillåten
om felet och testresultatet tydligt redovisas; det får aldrig kallas grön implementation.
Ingen produktionskorrigering eller refaktor får följa med. Rapportera exakt verifierad
remote-SHA och stoppa. ChatGPT/systemarkitekten granskar GitHub-underlaget; ägaren
ska inte behöva klistra in terminalhistorik. En korrigering kräver ett separat uttryckligen
auktoriserat checkpoint från verifierad main. Blocker-branchen får inte mergas till main.
Main förblir accepterad source of truth; blocker-evidens ersätter den inte. Ingen force
push och inget deployment-tillstånd följer av detta undantag. Orelaterat arbete bevaras.

### Permanent checkpoint-branch workflow

`main` är source of truth för owner/architect-accepterat, mergat arbete. Den aktiva
bounded checkpoint-branchen är dessutom source of truth för publicerat pågående,
ännu ej accepterat arbete. En branch per checkpoint från verifierad `origin/main`;
fortsätt samma branch genom reviewvarv, skapa ingen parallell implementation.

- **REVIEW CHECKPOINT:** commit/push av aktuellt sammanhängande checkpoint-tillstånd
  för oberoende review. Får vara ofullständigt, blockerat eller ha misslyckade experiment.
  Naturlig reviewpunkt, experimentfel, blocker, beslut eller STOP får inte lämna ägaren
  beroende av terminalutskrift. Ägaren auktoriserar sådan commit/push inom befintlig scope.
- **MERGE CANDIDATE:** en Review Checkpoint där all riskmotiverad checkpoint-verifiering
  genomförts och acceptance criteria bedöms uppfyllda. Publication innebär fortfarande inte acceptance.
- **ACCEPTED SHA:** exakt Merge Candidate SHA som ChatGPT granskat mot rätt main-baseline,
  ägaren uttryckligen godkänt och som verifierats oförändrad före merge. Ingen annan SHA får mergas.

Review-loop: bounded arbete → scope-/secrets-kontroll → commit/push med kod, tester,
rapport och handoff → rapportera branch, exakt remote SHA, tree, baseline, reviewstatus
samt kort granskningsorsak → **STOP**. Ägaren behöver bara skriva **"Codex är klar"**;
ChatGPT hämtar själv GitHub-underlaget. Misslyckade eller ej körda checks redovisas exakt,
aldrig som gröna. Hemligheter, rå känslig data och orelaterat arbete får aldrig publiceras.

Vid continue-beslut: fortsätt auktoriserat arbete på SAMMA branch, lägg nya commits ovanpå
publicerad historik och publicera nästa exakta Review Checkpoint. Ingen amend, squash,
rebase eller force push av reviewhistorik. Loopen pågår tills Merge Candidate, explicit
avslut eller ersättande owner/architect-beslut. Granskning ändrar inte scope automatiskt.

Före merge: verifiera exakt godkänd branch-SHA, oförändrad förväntad main och relevanta
required checks. Avvikelse kräver STOP/re-review. Efter kontrollerad merge pushas main,
relevant main-CI verifieras för exakt SHA. Återanvänd giltig lokal evidens enligt riskregeln ovan.
Dokumentation/recovery ändras bara vid faktiskt bestående uppdateringsbehov; om en separat
reconciliation-commit behövs verifieras dess relevanta CI för dess eget SHA. Ingen automatisk nästa checkpoint.

GitHub-underlaget anpassas till uppgiftens risk och relevans, utan onödiga parallella dokument.
Det ska självt räcka för att fastställa checkpoint-ID/mål, baseline, branch, review-SHA,
tree, reviewstatus, aktuellt implementationstillstånd, ändrade filer, tester/resultat,
experiment, konkreta fel/blockers, artefakt-/runtime-/modellprovenance, invariants,
dokumentationsstatus, uttryckliga non-goals och nästa beslut/arbete. Checkpointens rapport
bär specifik evidens/historik; `docs/operations/codex-recovery.md` stöder avbrott och
opublicerad delta. Commit/tree-identiteter hämtas ur Git/GitHub-metadata för rapportens
publiceringscommit (ingen omöjlig självrefererande SHA i dess eget innehåll); rapporten
ska ge en entydig identifierare/resolution även efter senare reviewcommits.

REVIEW CHECKPOINT ger inte merge approval, acceptance, deployment-, scope-, nästa
checkpoint- eller tradingbehörighet. Säkerhets-, science-, risk-, provider- och
kapitalgränser gäller oförändrat. Regeln om pre-existing blocker handoff ovan gäller
fortsatt dess snäva felkategori; vanlig ofullständig implementation får publiceras som
Review Checkpoint utan att felaktigt klassificeras som en befintlig defekt.

När fortsatt arbete kräver ägarens godkännande ska det granskningsbara underlaget först
vara färdigt och full teknisk återhämtningsstatus finnas i `docs/operations/codex-recovery.md`.
Avsluta godkännandefrågan med följande kompakta block, endast dessa fält (utelämna
Finance/safety-fältet när det inte är tillämpligt). Lägg det efter DOCUMENTATION STATUS
men före den obligatoriska sista dokumentationsmeningen. Begär inte redan givet godkännande igen.

```text
OWNER APPROVAL BLOCK

- Task/checkpoint:
- Baseline SHA:
- What changed:
- Tests/result:
- Important discovered behavior or risk:
- Finance/safety invariants:
- Publication state:
- Exact approval requested:
```

Blocket är en kort beslutsöversikt, inte en andra recovery-not. Ägaren ska inte behöva
kopiera terminaltranskript: publicerad GitHub-historik/dokumentation är fortsatt source
of truth, och den enda recovery-noten beskriver fullständigt opublicerat arbete.
Befintliga krav på uttryckligt Git-godkännande, sanering och verifiering gäller oförändrat.

### Obligatoriskt slutblock

Varje framtida uppdrag ska avslutas med:

```text
DOCUMENTATION STATUS

- Documentation reviewed: yes/no
- Documentation updated: yes/no
- Status updated: yes/no/not applicable
- Backlog updated: yes/no/not applicable
- Architecture or ADR updated: yes/no/not applicable
- Reports updated: yes/no/not applicable
- Documentation committed: yes/no
- Documentation pushed: yes/no
- Published commit: <SHA eller ej tillämpligt>
- Updated document locations:
  - <path eller none>
- Remaining documentation debt:
  - none eller konkret lista
```

Den absolut sista meningen i varje framtida Codex-svar ska vara `Dokumenten är uppdaterade: <kommaseparerad lista med faktiska sökvägar>.` Om inga dokument behövde ändras ska den vara exakt `Dokumenten är granskade och inga uppdateringar behövdes.` Den sista meningen får inte utelämnas.
