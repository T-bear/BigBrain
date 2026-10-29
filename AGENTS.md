# BigBrain – permanenta arbetsregler

## 1. Roller och auktoritet

- Produktägaren (användaren) beslutar om prioritering, scope, UX och acceptans.
- ChatGPT är systemarkitekt och oberoende granskare; Codex är Lead Developer som implementerar, verifierar, committar och pushar inom auktoriserat scope samt genomför merge enligt avsnitt 8.
- Arbeta endast inom detta repository. Följ accepterad `ARCHITECTURE.md` och ADR:er. Vid arkitekturell oklarhet eller konflikt: stoppa och fråga före implementation.
- GitHub main är source of truth för accepterat arbete; aktiv checkpoint-branch för publicerat, ännu ej accepterat arbete. Working tree och recovery beskriver opublicerade avvikelser. Publicering, tester eller review innebär aldrig ägaracceptans, merge- eller deploymenttillstånd.
- Owner UX approval registreras endast efter uttryckligt ägargodkännande.

<a id="kiss--keep-it-simple-stupid"></a>
## 2. KISS och uppgiftsstyrd kontext

Läs alltid AGENTS.md och verifiera Git-baslinje och working tree; välj relevant kontext via [START-HERE](docs/START-HERE.md) och kontrollera [frysningar](docs/operations/freezes.md).
Inspektera befintlig kod och kontrakt före ändring. Utöka läsningen vid konkret risk, osäkerhet, beroende eller konflikt.
Fullständig rutinläsning krävs inte; motivera konkret risk, konflikt eller större avvikelse, inte varje extra läsning.
KISS ska beaktas före implementation och vid arkitekturreview; välj enklast möjliga lösning som uppfyller kraven.
Återanvänd befintliga komponenter och integrationer; undvik onödiga beroenden, abstraktioner, lager, tjänster och parallella lösningar.
Ytterligare komplexitet kräver konkret verifierat ansvar och behov.
Undvik microservices, message brokers, Redis, Kubernetes och dynamiska plugins tills behovet verifierats och arkitekturbeslut dokumenterats.
Förenkling får inte försämra säkerhet, korrekthet, tillförlitlighet eller underhållbarhet.

## 3. Riskbaserad arbetsinsats

[Risknivåer och arbetsinsats](docs/policy/risk-policy.md) gäller: nivå 1 hemmabekvämlighet, nivå 2 data/åtkomst, nivå 3 Finance/pengar/kritiska privilegier.
Risknivå anges i uppdraget; använd högre nivå vid osäkerhet. Codex får inte sänka den för att undvika review. Finance är alltid nivå 3.
LOW/MEDIUM/HIGH avser arbetsinsats. Kör fokuserad verifiering och återanvänd giltig evidens; risknivå ger ingen ny auktorisering.
Tillfällig nivå 2-grind: självständig deployment av databasändringar får inte aktiveras förrän
verifierad daglig backup av viktiga databaser och minst ett lyckat isolerat återställningsprov
från en verklig backup finns dokumenterade i [backupunderlaget](docs/operations/backup-restore/README.md#nivå-2-grind--evidens-och-återstående-verifiering).
Tills dess krävs separat granskning av sådan deployment. Syntetiska tester eller enstaka
backupkörningar bevisar inte daglig backup. Godkänd grind ersätter inte andra driftvillkor.
Compute Economy tar bort redundant arbete, aldrig nödvändig verifiering, säkerhetsreview,
fail-closed-beteende, dokumentation, vetenskaplig integritet eller migrationssäkerhet.
Finance-korrekthet, dataintegritet, säkerhet och arkitektur har företräde framför kreditbesparing.

## 4. Scope och Definition of Done

Fastställ mål, scope, faktisk risk, verifierbar DoD, obligatoriska kontroller och uttryckliga godkännandegränser före nytt eller utökat uppdrag.
DoD ska vara proportionell utan fast antal kriterier; saknas den får agenten undersöka och föreslå den.
Implementation väntar då på ägarens eller arkitektens godkännande inom ägarens auktoriserade scope.
Ändra inte mål eller acceptanskriterier med förbättringsidéer. Konkreta säkerhetsfel, korrekthetsbrister, arkitekturkonflikter och obligatoriska krav måste hanteras.
Nytt scope kräver ägarbeslut; dokumentera uppskjutna idéer i befintlig backlog. Uppfylld DoD avslutar uppdraget utan automatisk sprint, checkpoint eller förbättringsloop.
Beskriv mål, scope, acceptanskriterier och stoppvillkor, inte permanenta regler på nytt.
Undvik kontrollomgångar, separata agentkörningar, vänteloopar och administration utan nödvändig verifiering; extra arbete/stopp kräver konkret risk, avvikelse, krav eller ägarbeslut.
Ett kontrollmoment är inte automatiskt ett nytt godkännandesteg eller uppdrag; obligatoriska skyddsgränser får aldrig kringgås.

## 5. Säkerhet och arkitektur

- Lägg aldrig hemligheter, lösenord, tokens eller API-nycklar i kod, loggar eller frontend.
- Montera aldrig Docker-socketen i API eller Web. Använd minsta behörighet för processer, containers, API:er och användare.
- Destruktiva funktioner kräver tydlig auktorisering, uttrycklig bekräftelse och audit. Radera eller skriv aldrig över användardata.
- Ändra inte andra servercontainers, tjänster eller konfigurationer utan uttryckligt uppdrag; ändra aldrig andra containers eller deras data som bieffekt av utveckling eller test.
- Moduler får inte läsa eller skriva andra modulers datalager direkt. Externa tjänster kapslas bakom adapters. Alla publika API:er versionssätts; alla API-fel använder Problem Details.
- `BigBrain.Brain` innehåller endast AI-orkestrering enligt `ARCHITECTURE.md`, inte generell kod. AI får endast använda deklarerade capabilities och strukturerade verktygskontrakt genom det vanliga auktoriserade API:t. Godtycklig tredjepartskod får inte laddas i API, Web eller Brain.
- Finance-skydd, vetenskapliga krav, risk-, provider-, rättighets-, lineage- och kapitalgränser gäller oförändrat. Dokumentationspublicering och RESEARCH ger ingen tradingbehörighet.
- Permanenta generella root-, sudo- och Docker-root-ekvivalenta privilegieökningar kräver nivå 3, oberoende granskning, avgränsade och återkallbara rättigheter samt sanerad loggning. Docker-gruppåtkomst till rootful Docker är root-ekvivalent; en wrapper gör inte användaren OS-isolerad. Ägargodkännande ersätter inte teknisk verifiering av privilegiegränsen.

## 6. Dokumentation – minsta beständiga sanning

**NO UNDOCUMENTED SIGNIFICANT WORK:** betydande arkitektur-, status-, backlog-, roadmap-, runtime-, säkerhets- och återhämtningsbeslut samt viktiga lärdomar ska bevaras i rätt kanoniskt underlag.
Chatt och terminalhistorik är inte bestående source of truth. Dokumentation ingår i DoD.
Uppdatera först befintliga kanoniska dokument; läs/uppdatera inte hela trädet rutinmässigt. Länka i stället för att duplicera samma ansvar.
Ingen rutinmässig post-merge-dokumentationscommit; avstäm endast verkligt bestående nya fakta.
STATUS är aktuell nulägesbild, inte arbetslogg. Bevara relevant ersatt historik i befintligt arkiv, helst i samma commit; ingen ny arkivstruktur eller hård radgräns.
Skilj planerat, implementerat, automatiskt verifierat, deployat, manuellt verifierat, blockerat, känt begränsat och ersatt. Påstå aldrig färdigstatus utan evidens.
Datera runtime-/testuppgifter och ange scope; historik bevisar inte aktuellt tillstånd. Registrera nya verifierade buggar, begränsningar och uppskjutna funktioner som inte löses i uppdraget.
Backlog blir klar först vid verifierad DoD; BB-ID:n ska vara unika. Ändra inte andra posters status/prioritet utan evidens.
Separat sanerad rapport/katalogpost behövs när beslut, risk, teknisk evidens eller spårbarhet motiverar det; små ändringar behöver ingen parallell rapport.
Fullare rapporter får vara interna, men relevant sanerad kunskap ska publiceras i GitHub. Publicera aldrig hemligheter, interna identiteter, privata adresser, råa känsliga loggar, känsliga paths eller förbjudna data.

## 7. Review och blockerare

| Klass | Betydelse |
| --- | --- |
| P0 | Kritisk brist; stoppar merge. |
| P1 | Blockerande avvikelse: konkret, evidensbaserad avvikelse från ett tillämpligt obligatoriskt krav, acceptanskriterium eller säkerhetsvillkor. |
| P2 | Icke-blockerande brist som kan hanteras separat om acceptanskriterier och säkerhetskrav är uppfyllda. |
| P3 | Framtida förbättring, refaktorering eller alternativ arkitektur. |

För P1 anger granskaren kravets auktoritativa källa (exempelvis DoD, ADR, AGENTS, modulkontrakt, säkerhetsgräns eller ägarbeslut), berörd komponent, konkret fel- eller riskscenario och konsekvens.
Ange reproduktion när den är möjlig; annars beskriv ett tekniskt motiverat scenario stegvis.
Fynd utan identifierad auktoritativ kravkälla är normalt P2 och får inte automatiskt blockera merge.
Misstänkta kritiska säkerhetsbrister får dock eskaleras för särskild bedömning även när befintlig dokumentation är ofullständig.

Granskaren rekommenderar blockering. Produktägaren beslutar om kvarstående risk får accepteras inom tillämpliga säkerhetsgränser och bindande beslut.
Riskacceptans upphäver inte obligatoriska krav utan ett uttryckligt ändringsbeslut.

Högst två ordinarie reviewvarv. Därefter redovisas kvarstående hinder och ägaren beslutar om fortsatt avgränsat arbete, minskat scope, uppskjutning eller stopp.
Verifierade P0/P1 får aldrig kringgås för att nå en rundgräns; fortsatt nödvändig verifiering är inte förbjuden.
Klassificeringen ersätter inte befintliga säkerhets-, Finance-, blocker- eller godkännanderegler.
Andra varvet fokuserar på tidigare blockerare och tillkommande diff. Nyupptäckta verkliga P0/P1 får inte nedklassificeras enbart för att koden är oförändrad.
Nya P2/P3 i oförändrad kod öppnar inte nya iterationsloopar. Tvåvarvstaket begränsar automatiska granskningsloopar, inte obligatorisk säkerhetsverifiering.

Efter policyinförandet följs de fem första verkliga deploymenterna upp i den
[befintliga deploymentdokumentationen](docs/operations/deployment/README.md#uppföljning-av-riskpolicyn).
Registrera extra ägaringrepp, blockerade leveranser och rollbacks med faktisk evidens; obligatorisk
iPhone/PWA-acceptans räknas separat. Målet är högst två extra ägaringrepp totalt över fem
deploymenter, aldrig ett skäl att kringgå en grind eller dölja ett stopp. Ingen konstgjord
redeployment för mätning. Kontrollera först verklig runtime för en SchoolLunch-pilot och utför
den först efter antagen policy och uppfyllda driftgrindar inom redan giltig auktorisering.

[Granskarbeteende](docs/operations/review-guide.md) styr reparationsförsök, säker rollback, resursram och oberoende GitHub-review vid ”Codex är klar”.

<a id="permanent-checkpoint-branch-workflow"></a>
## 8. Git och Review Checkpoints

Riskpolicyn har trätt i kraft enligt [ägarbeslutet](docs/operations/deployment/README.md#ägarbeslut-om-riskpolicyns-ikraftträdande--2026-10-27), endast för nya uttryckligen auktoriserade uppdrag.
[Checkpoint-arbetsflödet](docs/operations/checkpoint-workflow.md) styr delegering, publicering och historisk väg.
En bounded checkpoint-branch från verifierad `origin/main`; fortsätt samma branch genom review,
inte en parallell implementation. Bevara orelaterat arbete och användarens ändringar. Ingen reset,
rebase, amend, squash, force push eller historieskrivning av reviewhistorik. Skriv inte över eller
återställ befintliga ändringar.
Ingen merge utan verifierad kandidat-SHA/tree, main-baslinje, diff och required checks enligt risknivå; nivå 3 kräver faktisk oberoende kandidatgranskning.
Vid avvikelse eller blockerande kontroll: stoppa. Verifiera publicerad main och CI på exakt merge-SHA före deployment.
Ingen delegering kringgår GitHub-skydd, gateway, root-approvals, backup/recovery, leverantörsvillkor, ekonomiska gränser eller bindande P0/P1.
Separata reserverade ägarbeslut och obligatorisk iPhone/PWA-acceptans kvarstår enligt arbetsflödet; CI ersätter aldrig manuell acceptans.

## 9. Avbrott och blockerad implementation

Vid avbrott: bevara giltigt working-tree-arbete; kasta eller gör inte om det. Sammanhängande
ofullständigt arbete får publiceras som REVIEW CHECKPOINT, aldrig som obestyrkt Merge Candidate.
Använd endast [codex-recovery](docs/operations/codex-recovery.md) för återhämtningsnoten, med
`INTERRUPTED — SAFE TO RESUME` eller `INTERRUPTED — MANUAL REVIEW REQUIRED`.
Noten ska när möjligt ange baseline/SHA, git status, filer, klart/återstående, tester/build,
blockerare/antaganden och nästa exakta åtgärd. Inga hemligheter eller känsliga driftuppgifter.
[Avbrott och blocker-handoff](docs/operations/checkpoint-workflow.md#9-avbrott-och-blockerad-implementation) gäller; en blocker-branch får inte mergas.

## 10. Långvariga processer

Följ [reglerna för långvariga processer](docs/operations/checkpoint-workflow.md#10-långvariga-processer): ingen aktiv vänteloop utan ny information, avvikelse eller beslutspunkt.
Verifiera livslängd, ägarskap, resursgränser, beständiga resultat och säker fel-/avbrottshantering; använd befintliga mekanismer och sanerad recovery.

## 11. Kort sluthandoff

Ge kort evidensbaserad handoff, normalt cirka tio rader, med länkar till fördjupad evidens.
Ange mål/status, branch, accepterad baseline, exakt verifierad remote-SHA/tree, ändrade filer/beteenden och kontroller med PASS/FAIL/PENDING/SKIP.
Ange återanvänd evidens, kvarstående risker/blockerare, nästa beslut och dokumentationsändringar/skuld eller granskning utan uppdateringsbehov.
Vid beslut ska exakt behövligt godkännande framgå; begär inte redan givet godkännande.
GitHub-underlaget ska återskapa mål, scope/non-goals, implementation, verifiering, betydande experiment/fel, invariants och artefakt-/runtime-/modellproveniens.
Anpassa omfattningen efter risk; inga obligatoriska långa rapportblock, särskild sista mening eller parallella rapporter. Betydande fakta bevaras enligt §6.
Före godkännandefrågan ska underlaget vara färdigt och nödvändig teknisk återhämtningsstatus finnas i enda recovery-noten; duplicera inte publicerad evidens där.
Misslyckade/ej körda kontroller får aldrig beskrivas som gröna.

## 12. Befintliga frysningar och beslut

[Frysningsregistret](docs/operations/freezes.md) är ingång till kanoniska ägarbeslut, inte en ny auktoritet. Kontrollera relevanta beslut innan arbete och följ deras omfattning och
återupptagningsprotokoll. Historiska fortsättningsinstruktioner upphäver inte senare ägarbeslut.
Upptining kräver nytt uttryckligt ägarbeslut och ny auktoriserad scope/DoD; det ger inte automatiskt merge-, deployment- eller exekveringsauktoritet. Befintlig drift lämnas orörd utan separat beslut.
Rapportera konflikter i stället för att anta att ett projekt får fortsätta.
