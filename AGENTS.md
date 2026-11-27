# BigBrain – permanenta arbetsregler

BigBrain är ett privat hemmaprojekt. Pengar och behörighetsgränser skyddas hårt; resten får gå sönder, rullas tillbaka och återskapas. En regel som inte skyddar mot ekonomisk skada, dataförlust eller överskridna behörighetsgränser ska vara kort eller saknas.

**Finance KISS – ägarinriktning 2026-11-27:** Finance är ett privat forsknings- och investeringsverktyg för en enda ägares egna tillgångar, inte en enterprise- eller revisionsprodukt. Kräv endast kontroller som konkret skyddar kapital, behörigheter, användardata, källrättigheter eller forskningsresultatens riktighet. Separera praktiskt spårbar intern RESEARCH från *oberoende verifierad prospektiv evidens*; lokal tid/NTP, hashkedja och egen journal är inte extern tidsattest. Låt inte evidensambitioner eller organisationsprocesser utan påvisat behov blockera säkert avgränsat forskningsarbete. Finanshandel, nycklar, root och ändringar av skyddsregler förblir nivå 3. Gällande nivå-2-villkor och verifieringskrav står kvar tills de faktiskt är uppfyllda. [Kanonisk avgränsning](docs/policy/risk-policy.md#finance--privat-kiss-inriktning-2026-11-27).

## 1. Roller och auktoritet

- Produktägaren (användaren) beslutar om prioritering, scope, UX och acceptans.
- ChatGPT är systemarkitekt och oberoende granskare; Codex är Lead Developer som implementerar, verifierar, committar och pushar inom auktoriserat scope samt genomför merge enligt avsnitt 8.
- Arbeta endast inom detta repository. Följ accepterad `ARCHITECTURE.md` och ADR:er. Vid arkitekturell konflikt: stoppa och fråga före implementation.
- GitHub main är source of truth för accepterat arbete; aktiv checkpoint-branch för publicerat, ännu ej accepterat arbete. Publicering, tester eller review innebär aldrig ägaracceptans, merge- eller deploymenttillstånd.
- Owner UX approval registreras endast efter uttryckligt ägargodkännande.

<a id="kiss--keep-it-simple-stupid"></a>
## 2. KISS och uppgiftsstyrd kontext

Läs AGENTS.md, verifiera Git-baslinje och working tree, och läs därefter bara den kontext uppgiften kräver via [START-HERE](docs/START-HERE.md). Kontrollera [frysningar](docs/operations/freezes.md). Historik och äldre rapporter läses bara vid riktad felsökning eller när ett aktuellt dokument hänvisar dit.
Välj enklaste lösning som uppfyller kraven och återanvänd befintliga komponenter. Undvik nya beroenden, lager och tjänster utan verifierat behov. Förenkling får inte försämra säkerhet eller korrekthet.

## 3. Riskbaserad arbetsinsats

[Risknivåer och arbetsinsats](docs/policy/risk-policy.md) gäller. Risknivån följer vad en ändring kan göra och vilken åtkomst koden faktiskt har, inte vilken modul den ligger i:

- Nivå 1, experiment och bekvämlighet: UI, media, prototyper, testdata, dokumentation och isolerad forskning utan ekonomiska eller privilegierade förmågor.
- Nivå 2, normal drift: beständiga data, databasstruktur, API-beteende, schemaläggning och åtkomst till hushållsdata.
- Nivå 3, ekonomisk säkerhet: order, PAPER/LIVE, handels- eller kapitalauktoritet, ekonomiska nycklar eller konton, root, sudo, Docker-root, Docker-socket, administratörsåtkomst, en väg från en vanlig modul till dessa, samt ändringar av skyddsreglerna själva.

Finance med handelsförmåga är alltid nivå 3. Finance RESEARCH på historisk data är nivå 2 först när ägaren i `docs/STATUS.md` har noterat att isoleringsvillkoren och RESEARCH-garantin i risk-policyn är verifierade; tills dess är Finance nivå 3.
Risknivå anges i uppdraget. Vid verklig osäkerhet om en nivå 3-trigger används den högre nivån; Codex får inte sänka nivån för att undvika granskning. Risknivå ger ingen ny auktorisering.
Tillfällig nivå 2-grind: självständig deployment av databasändringar får inte aktiveras förrän
verifierad daglig backup av viktiga databaser och minst ett lyckat isolerat återställningsprov
från en verklig backup finns dokumenterade i [backupunderlaget](docs/operations/backup-restore/README.md#nivå-2-grind--evidens-och-återstående-verifiering).
Tills dess krävs separat granskning av sådan deployment. Provet görs en gång och därefter periodiskt, inte per ändring.
Förenkling tar bort redundant arbete, aldrig nödvändig verifiering, säkerhetsreview, fail-closed-beteende eller migrationssäkerhet. Finance-korrekthet, dataintegritet och säkerhet har företräde framför kreditbesparing.

## 4. Scope och Definition of Done

Fastställ mål, scope, faktisk risk, verifierbar DoD och uttryckliga godkännandegränser före nytt eller utökat uppdrag. DoD är proportionell, normalt 5–8 punkter; saknas den får agenten föreslå den och invänta godkännande.
Ändra inte mål eller acceptanskriterier med förbättringsidéer; uppskjutna idéer går till befintlig backlog. Nytt scope kräver ägarbeslut. Uppfylld DoD avslutar uppdraget utan automatisk sprint, checkpoint eller förbättringsloop.
Undvik kontrollomgångar, vänteloopar och administration utan nödvändig verifiering. Ett kontrollmoment är inte automatiskt ett nytt godkännandesteg; obligatoriska skyddsgränser får aldrig kringgås.

## 5. Säkerhet och arkitektur

- Lägg aldrig hemligheter, lösenord, tokens eller API-nycklar i kod, loggar eller frontend.
- Montera aldrig Docker-socketen i API eller Web. Använd minsta behörighet för processer, containers, API:er och användare.
- Vanliga moduler (Media, Family, UI, experiment) får inte nå Finance-nycklar, handelsfunktioner, Docker-socketen eller administrativa privilegier. Gränsen upprätthålls av OS-rättigheter, containerisolering och nätverksregler, inte bara av instruktioner till agenter.
- Destruktiva funktioner kräver tydlig auktorisering, uttrycklig bekräftelse och audit. Radera eller skriv aldrig över användardata. Ändra inte andra containers, tjänster eller konfigurationer utan uttryckligt uppdrag.
- Moduler läser eller skriver inte andra modulers datalager direkt. Externa tjänster kapslas bakom adapters; publika API:er versionssätts och API-fel använder Problem Details.
- `BigBrain.Brain` innehåller endast AI-orkestrering. AI får bara använda deklarerade capabilities genom det auktoriserade API:t; godtycklig tredjepartskod laddas inte i API, Web eller Brain.
- Finance-ADR:erna om risk, rättigheter, lineage, vetenskaplig integritet och kapitalgränser gäller som tekniska krav. Dokumentationspublicering och RESEARCH ger ingen tradingbehörighet. Före simulerad eller riktig handel krävs separat ägarbeslut och förlustgränser som inte enbart beror på strategikod.
- Permanenta generella root-, sudo- och Docker-root-ekvivalenta privilegieökningar kräver nivå 3, oberoende granskning, avgränsade och återkallbara rättigheter samt sanerad loggning. Docker-gruppåtkomst till rootful Docker är root-ekvivalent. Ägargodkännande ersätter inte teknisk verifiering av privilegiegränsen.

## 6. Dokumentation – minsta beständiga sanning

Betydande arkitektur-, status-, runtime-, säkerhets- och återhämtningsbeslut bevaras i rätt kanoniskt dokument; chatt är inte source of truth. Uppdatera först befintliga kanoniska dokument och länka i stället för att duplicera. Läs eller uppdatera inte hela trädet rutinmässigt, och små ändringar behöver varken separat rapport eller post-merge-dokumentationscommit.
STATUS är nulägesbild, inte arbetslogg. Skilj planerat, implementerat, verifierat, deployat, blockerat och ersatt; påstå aldrig färdigstatus utan evidens och datera runtime- och testuppgifter. Nya verifierade buggar och uppskjutna funktioner registreras i backlog med unika BB-ID:n.
Separat sanerad rapport behövs bara när beslut, risk eller spårbarhet motiverar det. Publicera aldrig hemligheter, interna identiteter, privata adresser, råa känsliga loggar eller känsliga paths.

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
Verifierade P0/P1 får aldrig kringgås för att nå en rundgräns.
Andra varvet fokuserar på tidigare blockerare och tillkommande diff. Nyupptäckta verkliga P0/P1 får inte nedklassificeras enbart för att koden är oförändrad.
Nya P2/P3 i oförändrad kod öppnar inte nya iterationsloopar. Tvåvarvstaket begränsar automatiska granskningsloopar, inte obligatorisk säkerhetsverifiering.
Oberoende granskning krävs på nivå 3 och där ett leveransspår i [lanes.md](docs/policy/lanes.md) uttryckligen kräver den. I övrigt räcker CI och agentens egenkontroll.

[Granskarbeteende](docs/operations/review-guide.md) styr reparationsförsök, säker rollback, resursram och oberoende GitHub-review vid ”Codex är klar”.

<a id="permanent-checkpoint-branch-workflow"></a>
## 8. Git och Review Checkpoints

[Checkpoint-arbetsflödet](docs/operations/checkpoint-workflow.md) styr delegering, publicering och historisk väg. Denna version gäller för nya uttryckligen auktoriserade uppdrag.
En bounded checkpoint-branch från verifierad `origin/main`; fortsätt samma branch genom review, inte en parallell implementation. Bevara orelaterat arbete och användarens ändringar. Ingen reset, rebase, amend, squash, force push eller historieskrivning av reviewhistorik. Skriv inte över eller återställ befintliga ändringar.
Ingen merge utan verifierad kandidat-SHA/tree, main-baslinje, diff och required checks enligt risknivå; nivå 3 kräver faktisk oberoende kandidatgranskning.
Vid avvikelse eller blockerande kontroll: stoppa. Verifiera publicerad main och CI på exakt merge-SHA före deployment.
Ingen delegering kringgår GitHub-skydd, gateway, root-approvals, backup/recovery, leverantörsvillkor, ekonomiska gränser eller bindande P0/P1.
Reserverade ägarbeslut (nytt scope, ny kostnad eller ekonomisk risk, upptining) och obligatorisk iPhone/PWA-acceptans kvarstår; CI ersätter aldrig manuell acceptans.

## 9. Avbrott och blockerad implementation

Vid avbrott: bevara giltigt working-tree-arbete. Ofullständigt arbete får publiceras som REVIEW CHECKPOINT, aldrig som obestyrkt Merge Candidate. Använd endast [codex-recovery](docs/operations/codex-recovery.md) för återhämtningsnoten, med `INTERRUPTED — SAFE TO RESUME` eller `INTERRUPTED — MANUAL REVIEW REQUIRED`, utan hemligheter.
[Avbrott och blocker-handoff](docs/operations/checkpoint-workflow.md#9-avbrott-och-blockerad-implementation) gäller; en blocker-branch får inte mergas.

## 10. Långvariga processer

Följ [reglerna för långvariga processer](docs/operations/checkpoint-workflow.md#10-långvariga-processer): ingen aktiv vänteloop utan ny information, avvikelse eller beslutspunkt.

## 11. Kort sluthandoff

Ge kort evidensbaserad handoff, normalt cirka tio rader med länkar till fördjupad evidens: mål/status, branch, verifierad remote-SHA/tree, ändrade filer och beteenden, kontroller med PASS/FAIL/PENDING/SKIP, kvarstående risker och nästa beslut.
Begär inte redan givet godkännande. Misslyckade eller ej körda kontroller får aldrig beskrivas som gröna. Inga obligatoriska långa rapportblock; anpassa omfattningen efter risk.

## 12. Befintliga frysningar och beslut

[Frysningsregistret](docs/operations/freezes.md) är ingång till kanoniska ägarbeslut, inte en ny auktoritet. Kontrollera relevanta beslut innan arbete och följ deras omfattning och
återupptagningsprotokoll. Historiska fortsättningsinstruktioner upphäver inte senare ägarbeslut.
Upptining kräver nytt uttryckligt ägarbeslut och ny auktoriserad scope/DoD; det ger inte automatiskt merge-, deployment- eller exekveringsauktoritet. Befintlig drift lämnas orörd utan separat beslut.
Rapportera konflikter i stället för att anta att ett projekt får fortsätta.
