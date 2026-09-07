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

- Läs [Start here](docs/START-HERE.md) för obligatorisk läsordning och dokumentauktoritet.
- **NO UNDOCUMENTED SIGNIFICANT WORK:** betydande arkitektur-, status-, backlog-, roadmap-,
  runtime-, säkerhets- och återhämtningsbeslut samt viktiga tekniska lärdomar ska dokumenteras
  i rätt kanoniska dokument. Chatt och terminalhistorik är inte bestående source of truth.
- Använd lägsta resonemangsnivå som passar uppgiften; eskalera när komplexiteten kräver det.
  LOW: mekanisk dokumentation, formatering, repetitiva specificerade ändringar, isolerad
  UI/text och små mekaniska testuppdateringar. MEDIUM: normal implementation, flerfilsrefaktor,
  integration och vanlig felsökning. HIGH: Finance-korrekthet och beräkningar/modeller,
  marknadsdatasemantik/lineage, arkitekturbeslut, migrationer, säkerhetsgränser, samtidighet,
  race conditions, svår felsökning, stora riskfyllda refaktorer och kritisk kodreview.
- Compute economy får ALDRIG användas för att hoppa över tester, verifiering, säkerhetsreview,
  fail-closed-beteende, dokumentation, vetenskaplig integritet eller migrationssäkerhet.
  Finance-korrekthet, säkerhet och arkitekturintegritet går före användningsoptimering.
  Policyn är uppgiftsklassificering, inte ett påstående att verktyget kan byta modellinställning.

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
- Kör tillämplig build och relevanta tester efter ändringar.
- Redovisa exakt vilka filer och beteenden som ändrats samt vilka build- och testkommandon som körts.
- Om build eller test inte kan köras ska orsaken redovisas tydligt.

### Systemarkitektens review-trigger

När ägaren säger **"Codex är klar"** ska ChatGPT behandla det som en review-trigger, inte som tillräcklig färdigevidens. ChatGPT ska läsa senaste publicerade GitHub source of truth, granska relevanta commits samt status-, backlog-, test- och rapportunderlag och därefter ge ägaren en förenklad systemarkitektgranskning: vad som gjorts, vad som är partiellt eller återstår, problem/risker, vad ägaren bör verifiera manuellt och rekommenderat nästa steg. Owner UX approval får endast registreras efter ägarens uttryckliga godkännande.

## Git

- Gör aldrig commit, push, reset, rebase, force push eller annan force-operation utan uttryckligt godkännande.
- Skriv inte över eller återställ användarens befintliga ändringar.

## Interrupted-run recovery

Om en körning avbryts innan avgränsat arbete är klart ska giltiga working-tree-ändringar bevaras och inte göras om eller kastas. Ofullständigt arbete får inte committas utan uttryckligt godkännande. Skriv när möjligt en sanerad återhämtningsnot enligt `docs/operations/codex-recovery.md` med baseline/source-of-truth-SHA, git status, ändrade filer, exakt klart/återstående arbete, körda test/build-resultat, blockerare/antaganden och nästa exakta åtgärd. Använd endast denna enda plats och statusen `INTERRUPTED — SAFE TO RESUME` eller `INTERRUPTED — MANUAL REVIEW REQUIRED`.

En senare Codex-session ska läsa `AGENTS.md`, synka/verifiera GitHub, inspektera working tree och återhämtningsnoten, säkerställa att orelaterade ändringar bevaras och fortsätta giltigt verifierat arbete utan onödig omkörning. Slutför ursprunglig scope före nytt arbete. Om repositoryt och noten motsäger varandra: stoppa och rapportera konflikten i stället för att gissa. Återhämtningsnoten får aldrig innehålla hemligheter, credentials, privata adresser, råa känsliga loggar eller förbjudna identifierare/data. GitHub är source of truth mellan färdiga sessioner; working tree och återhämtningsnoten beskriver uttryckligen ofärdigt lokalt arbete.

## Documentation and publication completion rule

Dokumentation är en del av Definition of Done. Efter varje implementation, buggfix, ändring, deployment, arkitekturbeslut eller verifiering ska Codex bedöma om följande behöver uppdateras: `README.md`, `docs/STATUS.md`, `docs/BACKLOG.md`, `ARCHITECTURE.md`, `docs/modules/*.md`, `docs/architecture/*.md`, `docs/adr/*.md`, `docs/knowledge/*.md`, `docs/operations/**/*.md`, `docs/indexes/*.md`, `TESTING.md`, `docs/reports/**` samt relevanta runbooks, säkerhets- och rollbackinstruktioner. Endast relevanta dokument ändras, men kontrollen ska alltid göras.

### Statusprincip

- Skilj uttryckligen mellan planerat, implementerat, automatiskt verifierat, deployat, manuellt verifierat, blockerat, känt begränsat och ersatt.
- Beskriv aldrig något som färdigt utan faktisk evidens.
- Daterad runtime- eller testinformation ska ha datum och scope; historisk evidens ersätter inte aktuell status.

### Backlogprincip

- Registrera nya verifierade buggar, begränsningar och uppskjutna funktioner när de inte löses i samma uppdrag.
- Markera en post som klar först när dess Definition of Done är verifierad.
- BB-ID:n ska vara unika. Ändra inte andra posters prioritet eller status utan evidens.

### Rapportprincip

När ett uppdrag tillför långsiktigt relevant kunskap ska Codex skapa eller uppdatera en sanerad rapport i repositoryt, uppdatera rapportkatalogen eller uttryckligen dokumentera varför en lokal rapport inte publiceras. Lokala fullrapporter får behållas som intern evidens, men relevant sanerad kunskap ska göras tillgänglig i GitHub. Hemligheter, interna identiteter, privata adresser, råloggar och känsliga paths får inte publiceras.

### Commit- och pushprincip

- Repositoryts publicerade dokumentation och Git-historik är source of truth mellan agentsessioner; en ny agent ska kunna återskapa aktuell status utan terminalhistorik.
- Dokumentationspublicering innebär aldrig deployment eller runtimeändring. Sådana åtgärder kräver separat uttrycklig auktorisering.
- När ett uppdrag är verifierat och användaren har godkänt push ska kod, tester och tillhörande dokumentation committas och pushas tillsammans i en eller flera tydliga commits.
- Om endast dokumentation ändrats ska den verifieras, få en separat dokumentationscommit och pushas. Dokumentation får inte lämnas lokalt enbart för att ingen kod ändrades.
- Om kod inte får pushas ska relevant dokumentation ändå uppdateras lokalt och samtliga väntande filer redovisas.
- Inga orelaterade ändringar får följa med. Ingen force push. `origin/main` ska verifieras före och efter push.
- Vid ren analys eller read-only-verifiering där ingen dokumentation behöver ändras ska slutsvaret ange att dokumenten granskats utan uppdateringsbehov.

### Kompakt ägargodkännande vid stopp

### Permanent checkpoint-branch workflow

`main` är endast owner/architect-accepted, merged source of truth. Varje bounded
implementation checkpoint arbetar på en separat branch skapad från verifierad
`origin/main`, till exempel `bb-130c/backtest-result-identity`.

En komplett, testad branch får pushas som **REVIEW CANDIDATE ONLY**. Det är inte
acceptans i main, deployment, runtime approval eller tillstånd att fortsätta med
nästa checkpoint. Codex stoppar efter branch-push. Merge till main får ske endast
efter explicit owner approval av exakt granskad branch-SHA. Före merge verifieras
att branch-SHA är oförändrad och att origin/main inte avancerat oväntat; annars
stoppas arbetet för re-review. Efter merge pushas main, CI väntas in och verifieras,
och status/recovery avstäms mot slut-SHA innan nästa branch skapas.

Ingen force push används. En branch per checkpoint gäller; inga långlivade
utvecklingsbrancher. GitHub-branch, kanonisk dokumentation och recovery-note ska
räcka för arkitektgranskning utan terminalhistorik. Befintliga krav på separat
deployment-, konto-, credential-, provider- och high-authority-godkännande gäller.

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
