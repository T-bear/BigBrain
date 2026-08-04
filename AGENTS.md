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

- När ett uppdrag är verifierat och användaren har godkänt push ska kod, tester och tillhörande dokumentation committas och pushas tillsammans i en eller flera tydliga commits.
- Om endast dokumentation ändrats ska den verifieras, få en separat dokumentationscommit och pushas. Dokumentation får inte lämnas lokalt enbart för att ingen kod ändrades.
- Om kod inte får pushas ska relevant dokumentation ändå uppdateras lokalt och samtliga väntande filer redovisas.
- Inga orelaterade ändringar får följa med. Ingen force push. `origin/main` ska verifieras före och efter push.
- Vid ren analys eller read-only-verifiering där ingen dokumentation behöver ändras ska slutsvaret ange att dokumenten granskats utan uppdateringsbehov.

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
