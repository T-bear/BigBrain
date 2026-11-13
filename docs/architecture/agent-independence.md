# Agent Independence — ägarbeslut 2026-11-13

Status: **Godkänt ägarbeslut om framtida arkitekturkrav; integration inte implementerad eller aktiverad.**
Område: BigBrains AI-arkitektur och utvecklingskedja för PREDEPLOY/TEST.
PREDEPLOY färdigställs först. Beslutet kompletterar befintliga ADR:er, säkerhetskrav,
delegeringsregler och produktionsspärrar; det ersätter dem inte.

## Ägarskap och utbytbarhet

BigBrain äger arbetskedjan; externa AI-agenter är utbytbara utförare. BigBrain behåller
arbetsflöden, uppgiftsfördelning, beslut/godkännanden, säkerhetsgrindar, behörigheter,
granskningsresultat, beslutshistorik, loggning och spårbarhet. Ingen extern AI-leverantör
får bli ett oumbärligt beroende för grundfunktionen utan uttryckligt nytt ägarbeslut.
GitHub Copilot, Claude och Gemini är exempel på framtida alternativ, inte valda integrationer.

Framtida agenter ska anslutas genom ett gemensamt leverantörsoberoende gränssnitt eller
adaptrar. Leverantörsspecifik funktion hålls utanför arbetskedjans centrala logik; en ny
kompatibel agent ska inte kräva ändringar i den logiken. Uppgifter, konfiguration,
revisionsspår, granskningar och beslut bevaras i BigBrains egna eller andra öppna,
dokumenterade format. Ett byte kräver inte migrering av centrala projektdata från en
AI-leverantör. Detta beslut föreskriver inget nytt ramverk, register eller API i PREDEPLOY v1.

## Avaktivering, återgång och återaktivering

Ägaren ska kunna avaktivera en integrerad agent med en enkel administrativ inställning,
utan kodändring eller ny deployment. Nya uppdrag ska då stoppas och pågående uppdrag
stoppas eller avslutas säkert enligt en fastställd policy. Agentens åtkomst ska kunna
återkallas separat utan att andra agenter påverkas.

Vid avaktivering, fel eller borttagning används en annan **uttryckligen godkänd** agent
om sådan finns. Är funktionen frivillig kan kedjan fortsätta utan den. Är funktionen
obligatorisk och godkänd ersättare saknas stoppas berört steg säkert. Automatisk återgång
får aldrig kringgå säkerhetsgrindar, obligatoriska granskningar eller ägarbeslut.
Återaktivering sker genom konfiguration när integrationen fortfarande är kompatibel
och behörighets- och säkerhetskraven är uppfyllda.

## Identiteter, granskning och miljögränser

Varje agent ska ha separat identitet, minsta uppgiftsbundna behörighet, återkallningsbara
autentiseringsuppgifter och spårbara åtgärder/resultat. Den får inte ärva andra agenters
behörigheter. TEST och produktion ska ha tydliga åtskilda åtkomstgränser.

Implementerare, kodgranskare, test-/kvalitetsagent och oberoende arkitekturgranskare ska
kunna vara skilda utförare. Granskningsuppdrag ger inte automatiskt rätt att godkänna,
slå samman eller driftsätta. Utförare och frivilliga granskningssteg ska kunna bytas genom
konfiguration; obligatorisk granskning får inte avaktiveras utan behörigt beslut.

Befintligt [TEST-mandat](../operations/checkpoint-workflow.md#syntetisk-test--ägarbeslut-2026-11-13)
och [TEST-runbook](../operations/runbooks/test-environment.md) gäller oförändrat.
Helt syntetisk profil använder inga verkliga API-nycklar, providers, marknadsdata, modeller
eller orderflöden. Syntetisk autonomi är inte produktionsgodkännande. Finance behåller sin
riskklassificering och befintliga regler för merge, produktion och icke-syntetiska profiler.
Operationsverktyg för utvecklingsagenter ska fortsatt respektera ADR 0001:s Dockergräns;
beslutet flyttar inte host-/Dockerstyrning till produktens API, Web eller Brain.

## Acceptans för en framtida integration

Följande ska demonstreras för den framtida integrationen, **inte som nya grindar för pågående PREDEPLOY**:

1. Registrering utan ändring av kärnans arbetsflödeslogik.
2. Tilldelning av en avgränsad uppgift i TEST.
3. Loggning av agentens åtgärder och resultat.
4. Administrativ avaktivering.
5. Inga nya uppdrag till avaktiverad agent.
6. Säker fortsättning eller stopp enligt fastställd policy.
7. Fortsatt funktion för övriga agenter och systemdelar.
8. Byte till annan kompatibel implementation.
9. Separat återkallning av agentens behörigheter.
10. Ingen kringgång av produktionsgrind eller obligatorisk granskning.

## Prioritering och mandat

Nu: fortsätt PREDEPLOY enligt befintlig plan, beakta principen vid relevanta beslut och
undvik praktiskt onödiga hårdkodade AI-leverantörsberoenden. Ingen Claude-, Gemini- eller
Copilot-integration, nytt abonnemang, API-kostnad, arkitekturgranskning eller testgrind
krävs av detta beslut. Det befintliga verktygsflödet är inte härmed certifierat som en
färdig implementation av framtidens separata agentidentiteter eller avaktivering.

Efter fungerande PREDEPLOY: utvärdera kvalitet, tillförlitlighet, kostnad och säkerhet;
välj vid behov en första extern testagent och verifiera anslutning, avaktivering, återgång
och spårbarhet. Permanent integration föreslås därefter för separat ägarbeslut.
Detta är framtida planering, ingen automatisk integrationsbeställning.

Utvecklingsansvariga väljer teknisk utformning inom befintligt mandat. Stödjer arkitekturen
redan principerna krävs ingen ytterligare ändring nu. Anpassningar med betydande merarbete,
nya beroenden eller försening dokumenteras som framtida förbättring och utförs inte nu.
Ägaren kontaktas endast vid verkligt arkitekturbeslut, nytt kostnadsåtagande eller utökat
säkerhetsmandat. I övrigt räcker kort bekräftelse: dokumenterat och inte blockerande.
