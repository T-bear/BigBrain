# BigBrain Finance – tilläggsmandat v1.1
## Riskproportionell säkerhet och förenklad forskningsmiljö

- **Datum:** 2026-11-08
- **Risknivå:** 3 – Finance
- **Omfattning:** Finance Research Release 1
- **Giltighet:** Från uttryckligt ägargodkännande 2026-11-08 till 2026-11-18, eller tidigare återkallelse eller avslutad Release 1.
**Beslut:** Godkänt av produktägaren 2026-11-08 i ChatGPT. Publicering är dokumentation av beslutet, inte merge- eller driftgodkännande.

Relaterade källor: [Finance-frysningen](finance-freeze-20261016.md), [AGENTS.md](../../AGENTS.md), [befintligt Release 1-mandat från 2026-11-06](https://github.com/T-bear/BigBrain/blob/28e08469f248921922ece380523a0ac7f0a0f6db/docs/operations/finance-release1-mandate-20261106.md) (vid beslutstillfället endast i [draft-PR #40](https://github.com/T-bear/BigBrain/pull/40)), samt [checkpoint-arbetsflödet](checkpoint-workflow.md).

## 1. Syfte

Produktägaren ger systemarkitekten (ChatGPT) mandat att ompröva och förenkla den tekniska säkerhetsarkitekturen för Finance Research Release 1.

Syftet är att minska onödig teknisk komplexitet, förkorta felsökningskedjor och möjliggöra tillförlitlig historisk forskning utan att försämra skyddet för data, server, behörigheter eller ekonomiska tillgångar.

**Säkerhetsåtgärder ska motiveras av identifierade risker och verifierbara skyddsbehov, inte av maximal teknisk isolering som självändamål.**

## 2. Förhållande till tidigare mandat och frysning

Detta mandat kompletterar det av ägaren godkända Finance Research Release 1-mandatet från 2026-11-06. Det är ett avgränsat undantag från Finance-frysningen för arkitekturanalys, förenklingsförslag och uttryckligen tillåtet förberedelsearbete enligt nedan; det häver inte frysningen i övrigt.

Finance behåller formell risknivå 3 och status **RESEARCH / 0 SEK / NONE**.

Vid konflikt gäller tillägget endast för den uttryckligen avgränsade förenklingsuppgiften. Övriga tidigare beslut och begränsningar fortsätter att gälla. Ett publicerat tillägg gör inte den äldre draft-PR:n accepterad i main.

## 3. Arkitektens mandat

ChatGPT får självständigt:

1. Granska befintliga säkerhetsmekanismer och identifiera överkomplexitet.
2. Jämföra alternativa, enklare tekniska lösningar.
3. Besluta vilka mekanismer som är nödvändiga för en avgränsad modellfri offlineforskningsmiljö.
4. Föreslå borttagning eller ersättning av mekanismer som inte tillför en konkret säkerhetsfördel i det aktuella hot- och konsekvensscenariot.
5. Fastställa proportionella verifieringskrav och Definition of Done för förenklingsuppgiften.
6. Avsluta felsökningsspår där kostnaden och komplexiteten inte står i rimlig proportion till skyddsvärdet.

Besluten ska dokumenteras med riskbedömning och relevanta tekniska belägg. Arkitekten behöver inte inhämta ett nytt ägarbeslut för varje teknisk designjustering som håller sig inom mandatets oförändrade skyddsgränser.

## 4. Skydd som inte får försvagas

Följande är obligatoriskt oavsett teknisk lösning:

- Produktionsdatabaser och originaldata skyddas från ändring.
- Verifierade forskningskopior används med avgränsade läsrättigheter.
- Ingen tillgång till handelsnycklar, hemligheter eller andra moduldatabaser.
- Inga nätverksanrop till dataleverantörer under offlineforskning.
- Ingen PAPER-, LIVE-, AUTO- eller annan handelsauktoritet.
- Dataproveniens, vetenskaplig reproducerbarhet, kronologisk integritet och rättighetsvillkor bevaras.
- Ingen permanent root-, sudo- eller Docker-root-ekvivalent åtkomst.
- Ingen modifiering av Finance-produktion, databasschema eller befintliga tjänster utan separat tillstånd.

Enklare teknisk isolering får väljas endast när dessa skydd kan uppfyllas och verifieras.

## 5. Tillåten förenkling och förberedelse

Arkitekten får välja en enklare lösning än nuvarande specialbyggda systemd-isolering för modellfri offlineverifiering.

Exempelvis får standardiserade Linux-behörigheter, separat användaridentitet, kontrollerade skrivskyddade kopior och begränsad processexekvering utvärderas som alternativ.

Det är inte obligatoriskt att behålla exakt samma systemd-namnrymder eller startmekanism om skyddsmålen kan uppfyllas på ett verifierbart enklare sätt.

ChatGPT och Codex får inom uppdraget analysera, dokumentera och förbereda ändringar av fristående verifieringsskript och syntetiska tester på isolerade arbetsbrancher. Oprivilegierade tester med syntetisk data får köras inom godkänd arbetsmiljö.

Ändringar som påverkar produktionskod, verkliga datarättigheter, administratörsprivilegier eller drift kräver fortsatt separat godkännande.

## 6. Genomförande och beslutsmetod

Arbetet ska följa denna ordning:

1. Identifiera konkret risk och säkerhetsmål.
2. Fastställ vilka befintliga skydd som faktiskt behövs.
3. Jämför den befintliga lösningen med högst två rimliga alternativ.
4. Välj den enklaste lösning som uppfyller samtliga obligatoriska krav.
5. Verifiera lösningen genom fokuserade tester och oberoende granskning.

Spekulativa säkerhetsändringar och upprepade testkörningar utan ny diagnostisk information ska undvikas.

Ett befintligt blockerande säkerhetskrav får inte kringgås. Om förenklingen kräver en ändring av ett accepterat säkerhetskontrakt ska den exakta kontraktsändringen granskas och godkännas separat innan den används.

## 7. Definition of Done

Förenklingsuppgiften är färdig när:

- Den valda säkerhetsmodellen är dokumenterad och motiverad.
- Obligatoriska skydd har verifierats.
- Kvarstående risker och osäkerheter är redovisade.
- Relevanta syntetiska tester är godkända.
- En oberoende granskning har bedömt lösningens säkerhetsgränser.
- Eventuella ändringar finns i en isolerad, spårbar granskningsbranch.

Uppfylld DoD för förenklingsuppgiften innebär inte automatiskt att Finance Release 1 är accepterad eller att en historisk körning får startas.

## 8. Begränsningar och stoppvillkor

Arbetet stoppas vid:

- Risk för åtkomst till eller förändring av produktionsdata.
- Behov av obehörig privilegieökning.
- Otillräcklig isolering eller obevisade skyddsgränser.
- Oförklarade skillnader i dataintegritet eller forskningsresultat.
- Nya P0/P1-fynd som inte kan hanteras inom mandatet.

Rootbaserade systemd-prov, verkliga Finance-datakörningar utanför redan godkänd åtkomst, deployment, merge och D3-exekvering får inte genomföras enbart med stöd av detta tillägg.

Qwen/G3a och övriga tidigare frysta delar förblir blockerade.

Retention, data-disposition, leverantörsvillkor, nya kostnader och handel förblir reserverade för produktägaren. Mandatet ändrar inga befintliga tidsfrister, inklusive BB-132K.

## 9. Verktyg och roller

ChatGPT ansvarar för arkitektur, tekniska vägval, riskbedömning och oberoende granskning. Codex är fortsatt Lead Developer för större implementationer.

GitHub används som sanningskälla, Context7 för teknisk dokumentationskontroll, Acumen för relevanta externa kunskapsluckor och Remote Desktop Commander för avgränsad serverdiagnostik.

Verktygsåtkomst innebär ingen utökad teknisk eller ekonomisk auktorisering.

## 10. Rapportering, publicering och acceptans

Arkitekten redovisar ett sammanhållet beslutsunderlag med rekommenderad lösning, verifierade skydd, kvarstående risker och nästa beslutspunkt.

Dokumentations- och granskningsändringar följer BigBrains befintliga checkpoint- och PR-arbetsflöde. Inget automatiskt merge- eller deploymenttillstånd tillkommer genom detta mandat.

Produktägaren kan när som helst återkalla mandatet.

**Godkänt av ägaren:** 2026-11-08 — ”Jag godkänner, implementera det via remote desktop och fungerande beprövade github vägar tack.”
