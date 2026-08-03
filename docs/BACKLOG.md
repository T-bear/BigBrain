# BigBrain Backlog

Senast uppdaterad: 2026-08-03

Detta dokument samlar verifierade buggar, teknisk skuld och framtida förbättringar som ännu inte är implementerade.

Prioritet:

- P0 – kritiskt fel eller risk för dataförlust
- P1 – blockerande eller tydligt störande fel
- P2 – viktigt men arbetet kan fortsätta
- P3 – förbättring eller lågprioriterad teknisk skuld

Status:

- Ny
- Bekräftad
- Planerad
- Pågår
- Klar
- Avvisad

---

## Buggar

### BB-001 – Ingen synlig återkoppling när en dubblettvara stoppas

- Modul: Inköpslista
- Typ: UX / felhantering
- Prioritet: P2
- Status: Bekräftad
- Upptäckt: 2026-08-02

#### Beskrivning

När användaren försöker lägga till en vara som redan finns i inköpslistan skapas ingen dubblett, vilket är korrekt. Däremot visas inget synligt meddelande eller någon dubblettdialog för användaren.

#### Nuvarande beteende

- API eller datalager stoppar dubbletten.
- Ingen extra vara läggs till.
- Användaren får ingen tydlig förklaring.

#### Förväntat beteende

Användaren ska få en tydlig återkoppling, exempelvis:

- `Korv finns redan på listan.`
- möjlighet att öka antal;
- möjlighet att visa den befintliga varan;
- möjlighet att avbryta och fortsätta skriva.

#### Risk

Användaren kan tro att knapptryckningen inte fungerade och försöka flera gånger.

#### Avgränsning

Ingen ändring av dubblettregeln eller datamodellen krävs. Felsök frontendens hantering av API-svaret och dubblettdialogens rendering.

#### Definition of Done

- Ett stoppat dubblettförsök ger synlig återkoppling.
- Ingen dubblett skapas.
- Fokus återgår till ett logiskt ställe.
- Dialogen visas ovanför handlingsläget.
- Fungerar på mobil och desktop.
- Regressionstest finns.

---

### BB-002 – Bakgrundssidan kan scrollas bakom handlingsläget

- Modul: Inköpslista
- Typ: Mobil UX / modalhantering
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-02

#### Beskrivning

När Inköpslistas fullskärmsläge är öppet går det ibland att scrolla innehållet bakom fullskärmsytan.

#### Nuvarande beteende

- Bakgrundsinnehållet kan ibland förflyttas.
- Felet verkar vara intermittent.
- Det är ännu inte fastställt vilka steg som alltid reproducerar det.

#### Förväntat beteende

När handlingsläget är öppet ska endast inköpslistans interna innehåll kunna scrollas. Sidan bakom ska vara helt låst.

#### Misstänkta utlösare

- mobilens tangentbord öppnas eller stängs;
- radmeny öppnas;
- dubblettdialog öppnas;
- handlingsläget öppnas och stängs flera gånger;
- iOS ändrar höjden på den visuella viewporten.

#### Definition of Done

- Bakgrundsscroll är låst under hela handlingsläget.
- Ursprunglig scrollposition återställs när läget stängs.
- Ingen hoppande sida vid öppning eller stängning.
- Verifierat i riktig Chromium och manuellt på iPhone.
- Regressionstest eller reproducerbar browserkontroll finns.

---

### BB-003 – Scrollindikator visas trots att listan verkar rymmas

- Modul: Inköpslista
- Typ: Mobil layout / overflow
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-02

#### Beskrivning

I handlingsläget kan en scrollbar eller scrollindikator ibland visas på höger sida trots att listans innehåll inte ser ut att kräva scrollning.

#### Nuvarande beteende

- Scrollindikatorn visas intermittent.
- Det är oklart om en liten dold overflow faktiskt finns.
- Felet kan vara kopplat till viewporthöjd, tangentbord eller fokus.

#### Förväntat beteende

Ingen intern scrollindikator ska visas när hela innehållet ryms i viewporten.

#### Tekniska kontrollpunkter

- `100vh` kontra `100dvh`;
- marginaler eller padding som skapar några pixels overflow;
- fokus- eller keyboardförändringar;
- dubbla scrollcontainers;
- `min-height`, footer och safe-area-insets;
- portalerade menyer eller dialoger som påverkar dokumenthöjden.

#### Definition of Done

- En kort lista ger ingen onödig scrollbar.
- En lång lista scrollar endast i avsedd intern container.
- Samma beteende vid 320×844 och 390×844.
- Tangentbord, radmeny och dialog orsakar inte falsk overflow.

---

## Dokumentationsstyrning

### BB-004 – Arkivera ARR-incidentens mellanrapporter

- Modul: Dokumentation/Operations
- Typ: Dokumentationsskuld
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Arkivera mellanrapporter additivt med manifest och checksummor.
- Motiv: Skilja slutrapport och aktiv diagnostik från historisk evidens.
- Avgränsning: Ingen radering; flytt kräver separat review och godkännande.
- Risk: Brutna referenser eller förlorad spårbarhet.
- Definition of Done: Godkänt manifest, checksummor, uppdaterade index och verifierade länkar.
- Relaterade dokument: `docs/indexes/documentation.md`, ADR 0010, extern `arr-incident-index.txt`.

### BB-005 – Revidera README mot aktuell implementation

- Modul: Projektdokumentation
- Typ: Dokumentationsskuld
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Uppdatera funktioner, läsordning och länkar efter faktagranskning.
- Motiv: README speglar inte hela verifierade implementationen.
- Avgränsning: Ingen arkitekturändring eller ny funktion.
- Risk: Felaktig onboarding.
- Definition of Done: Kort, kodverifierad README med auktoritativa länkar.
- Relaterade dokument: `README.md`, `docs/indexes/documentation.md`.

### BB-006 – Dela upp och korta STATUS.md

- Modul: Projektdokumentation
- Typ: Informationsarkitektur
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Begränsa STATUS till aktuellt läge och placera historisk verifiering efter review.
- Motiv: Filen blandar sprintlogg, runtimeevidens, problem och produktstatus.
- Avgränsning: Bevara historik; inga flyttar utan review.
- Risk: Förlust av evidens eller dubbla sanningar.
- Definition of Done: Definierat ansvar, indexerad historik och verifierade länkar.
- Relaterade dokument: `docs/STATUS.md`, `docs/indexes/baselines.md`, ADR 0010.

### BB-007 – Besluta om en enda produktroadmap

- Modul: Produktstyrning
- Typ: Governance
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Fastställ ROADMAP.md:s ansvar och relation till backlog och stabiliseringsplan.
- Motiv: Parallella planeringsytor skapar otydlig prioritet.
- Avgränsning: Ingen reprioritering före produktägarreview.
- Risk: Konkurrerande planer styr arbetet.
- Definition of Done: En normativ roadmap med ägare, scope och länkar.
- Relaterade dokument: `ROADMAP.md`, `docs/BACKLOG.md`, `STABILIZATION_PLAN.md`.

### BB-008 – Aktivera eller avveckla CHANGELOG-policy

- Modul: Releasehantering
- Typ: Governance
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Besluta om CHANGELOG ska underhållas och definiera trigger och format.
- Motiv: En tom eller oägd changelog ger falska förväntningar.
- Avgränsning: Ingen retroaktiv historik utan verifierbar källa.
- Risk: Releaseförändringar blir svåra att följa.
- Definition of Done: Dokumenterad policy och första tillämpning, eller tydlig avveckling.
- Relaterade dokument: `CHANGELOG.md`, `ROADMAP.md`.

### BB-009 – Klassificera Proposed ADR 0002 och 0006–0009

- Modul: Arkitektur
- Typ: ADR-review
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Granska om förslagen ska accepteras, ersättas eller förbli Proposed.
- Motiv: Långvarigt Proposed-läge gör auktoriteten oklar.
- Avgränsning: Status ändras endast genom explicit arkitekturreview.
- Risk: Implementation och föreslaget beslut divergerar.
- Definition of Done: Varje ADR har dokumenterat reviewbeslut och evidens.
- Relaterade dokument: `docs/adr/0002-sentinel-exclusive-system-access.md`, `docs/adr/0006-sentinel-local-transport-identity-and-request-proof.md`, `docs/adr/0007-sentinel-v1-system-metrics-schemas-and-compatibility.md`, `docs/adr/0008-sentinel-system-metrics-policy-classification-and-audit.md`, `docs/adr/0009-sentinel-system-metrics-packaging-privilege-and-supply-chain.md`.

### BB-010 – Flytta TESTING.md till rätt runbookstruktur

- Modul: Kvalitet
- Typ: Dokumentationsstruktur
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Separera testpolicy från körinstruktioner och senare placera dem rätt.
- Motiv: Normativ policy och procedur bör vara tydligt åtskilda.
- Avgränsning: Ingen flytt i denna fas; länkar granskas först.
- Risk: Brutna länkar eller otydlig Definition of Done.
- Definition of Done: Godkänd målstruktur, bevarad historik och gröna länkar.
- Relaterade dokument: `TESTING.md`, `docs/operations/README.md`.

### BB-011 – Konsolidera STABILIZATION_PLAN

- Modul: Produktstyrning
- Typ: Dokumentationsskuld
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Separera roadmap, teknisk skuld och verifierade buggar.
- Motiv: En plan ska inte fungera som osorterad felinkorg.
- Avgränsning: Inget stängs eller flyttas utan evidens och review.
- Risk: Prioriteringar och problemstatus misstolkas.
- Definition of Done: Varje punkt har rätt hemvist, ägare och status.
- Relaterade dokument: `STABILIZATION_PLAN.md`, `ROADMAP.md`, `docs/BACKLOG.md`.

### BB-012 – Införa återkommande baseline-review

- Modul: Operations/Dokumentation
- Typ: Governance
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Definiera intervall, scopeägare och current-post per baselinetyp.
- Motiv: Daterad evidens får inte bli odaterad sanning.
- Avgränsning: Ingen automation innan retention och secret-scan godkänts.
- Risk: Föråldrade baselines styr felsökning och beslut.
- Definition of Done: Godkänd kalender, ägare, statusmodell och indexuppdatering per scope.
- Relaterade dokument: `docs/indexes/baselines.md`, ADR 0010.
