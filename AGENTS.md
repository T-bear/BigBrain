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

## Git

- Gör aldrig commit, push, reset, rebase, force push eller annan force-operation utan uttryckligt godkännande.
- Skriv inte över eller återställ användarens befintliga ändringar.
