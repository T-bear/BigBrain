# Family View — skolmåltider: källverifiering och minsta implementation

## Metadata

Detta är en sanerad GitHub-version. Inga hemligheter, privata data eller råloggar publiceras.

## Status

Datum: 2026-10-23. **REVIEW CHECKPOINT — PREPARATION ONLY; NOT IMPLEMENTED / NOT DEPLOYED.**
Baseline `712679fda646aced2a417adf696e59d4a7c0f57b`, branch
`docs/bb158-freeze-family-school-meals`. Publiceringsämne
`docs: freeze BB-158 and prepare Family school meals` identifierar exakt commit/tree i Gitmetadata.
Ingen kod, infrastruktur, skolautomation eller BB-158-fortsättning ingår.

Ägaren prioriterar Rosenfeldtsskolan och Musikugglan i Karlskrona i befintlig Family View.
[Family-kontraktet](../../../architecture/family-view-family-coordination.md) är en framtida epic,
inte godkänd implementation av generell Family Context. Meal Planner och Shopping List är befintliga
moduler med egen dataauktoritet. [BB-097](../dashboard/bb-097-family-obsidian-gold-reference-20260823.md)
anger implementerad/deployad Family-komposition men `Owner approved: no` för den visuella referensen;
texten skiljer accepterad komposition från ej slutgodkänd estetik. Ingen ny owner UX approval antas.
[Temakontraktet](../../../design-system/theme-contract-v1.md) och ADR0012 gäller.

## Evidence

### Källor och faktisk åtkomst

Vanliga publika GET, utan autentisering/cookies, CAPTCHA-lösning eller kringgående. Enstaka
målstyrda hämtningar, ingen crawl. Sidorna lästes 23 oktober 2026; nätverksåtkomst och menyer kan ändras.

| Skola | Officiell väg och leverantör | Faktiskt verifierat / begränsning |
| --- | --- | --- |
| Rosenfeldtsskolan | [Karlskrona: Skolans matsedel](https://www.karlskrona.se/skola-och-forskola/matsedel-skolan) hänvisar i sökindexerad kommuntext till skolmat.info. [Publik portal](https://www.skolmat.info/matsedlar). | Kommunens live-sida gav robotverifiering i webbläsverktyget; ingen kringgång. Portalen gav HTTP200/HTML utan inloggning med vanlig GET. Exakt Rosenfeldt-facilityId och skolans veckodata **inte verifierade**. |
| Musikugglan | [Officiell matsedel](https://www.tantgron.nu/musikugglan/matsedel), länkad från skolans egen sida. Iframe pekar på [Tant Gröns matsedel](https://www.tantgron.nu/matsedel/). Leverantör i teknisk mening: egen webbpublicering på tantgron.nu, inte bevis för extern menytjänst. | Båda HTTP200, utan inloggning/CAPTCHA. Iframe är vanlig HTML, rubrik `Matsedel v. 43`, tabell med fem svenska veckodagar och rättstext. Separat mellanmålsavsnitt får inte blandas in i lunch. |

Musikugglans lunchtext innehåller även alternativ, till exempel huvudrätt och vegorätt i samma cell.
Ingen maskinläsbar alternativetikett, datum, år eller källa-uppdateringstid observerades i menyn.
Bevara originaltext och ordning; snedstreck får inte generellt bli en semantisk maträttsdelare.
Det observerade veckonumret motsvarar aktuell ISO-vecka vid hämtningen, men år/datum är i så fall
**härledda**, inte källangivna. Inga tidigare/framtida veckolänkar eller kalenderflöden hittades i
den avgränsade matsedeln. Ingen generell slutsats att sådana aldrig kan finnas.

### Strukturerad källa: verifierad respektive referensinformation

- Musikugglan: enkel HTML-parsning av fast officiell iframe är minsta verifierade hämtmetod.
  Inget dokumenterat JSON/API/ICS har verifierats. Följ inte godtyckliga iframe-adresser från användare.
- Skolmat.info: dokumenterat offentligt API har **inte** verifierats. En
  [pinnad teknisk referens](https://github.com/Kaptensanders/skolmat/blob/7c5203201cfff1a3ce84f336d4f57261decdc5a1/custom_components/skolmat/menu.py)
  beskriver `GET /api/public/matsedlar/{facilityId}?year=...&week=...` och JSON
  `ok`, `week.year`, `week.weekNumber`, `week.days[MONDAY…].isOpen`, `rows[].text`.
  Detta är tredjepartskod, inte verifierat svar från Rosenfeldtsskolan, officiellt API-avtal eller
  användningsrätt. Inget exempel-ID från en annan skola återanvänds. Ingen API-förfrågan gjordes.
- Officiell skola→provider-identitet och ett tillåtet verkligt veckosvar måste fastställas innan
  implementationen får påstå stöd för Rosenfeldtsskolan. Om detta bekräftas: JSON via befintlig
  System.Text.Json, ingen HTML-parser för den leverantören. Två skolor innebär inte automatiskt två
  implementationskopior; använd en adapter per faktiskt format/leverantör.

## Security

### Villkor och stoppgräns

[Skolmat.info robots.txt](https://www.skolmat.info/robots.txt) gav HTTP200 och `User-Agent: *`,
`Disallow: /`. Vid detta fynd stoppades vidare automatiserad undersökning av portalen. Det är en
konkret automatiseringssignal, inte ett juridiskt utlåtande. Inget uttryckligt automatiserings- eller
cacheavtal är verifierat. Kommunens länk ersätter inte sådant underlag. Nästa steg är ett godkänt
åtkomstsätt/besked från leverantören/kommunen och rätt skollänk, inte fortsatta URL-gissningar.

[Tant Grön robots.txt](https://www.tantgron.nu/robots.txt) begränsar `/admin`; matsedeln är inte
disallowed där. Detta är **inte** en licens eller uttrycklig automatiseringsrätt. I lästa meny-/skolsidor
fanns inga sådana villkor. Rättigheter, låg hämtningsfrekvens och cachebehållning kvarstår
`MANUAL_REVIEW` enligt befintlig backlog. Ägaren kan inhämta besked; Codex har inte kontaktat skolan.

## Datum, veckor och frånvaro

- Använd ISO-år + ISO-vecka och DateOnly i Europe/Stockholm; ingen UTC-midnatt som flyttar lunchdag.
- Musikugglan: acceptera bara entydig rubrik/veckodagstabell. Föreslagen v1 visar hämtad aktuell vecka,
  med `dateBasis=derivedFromCurrentIsoWeek`; vid mismatch/årsskiftesoklarhet visas okänt, inte gissade datum.
  Utan källans årtal kan gammalt material inte uteslutas enbart för att veckonumret matchar.
- Andra veckor utan publicerad data visas som `notPublished`, inte kopia av vecka43 och inte lov.
  Veckonavigation kan finnas även om en källa saknar valt veckoinnehåll; obegränsat historikstöd utlovas inte.
- Skolmat-referensens `isOpen=false` är inte verifierad lovklassificering. Bevara eventuell stängdstatus
  som källuppgift; härled inte elevnärvaro, skollov eller fritidsmat från frånvaro av rätter.
- Skilj tom publicerad dag, stängd enligt källa, inte publicerat, fel, inaktuellt och okänd datumgrund.
  `fetchedAtUtc` ersätter inte `sourceUpdatedAtUtc`; det senare får vara null.

## Changes

### Föreslagen minsta KISS-implementation — inte godkänd kod

Placera läsbaserad skolmåltidsintegration i befintlig Meal Planner-API-modul; Family är presentation.
Ingen generell Family Context-motor, jämförelsemotor, AI, ny tjänst, databas eller scheduler.
En liten adapter för Tant Grön HTML och, **om åtkomst/identitet verifieras**, en för skolmat.info JSON.
Inga slutpunkter tar godtycklig URL. Skol-ID:n mappar till konfigurerade, verifierade källor på servern.
Återanvänd HttpClientFactory, cancellation och Problem Details. Föreslagna små gränser: timeout5s,
max512KiB/svar, begärd vecka per skola, ingen automatisk retry-loop; fast tillåten HTTPS-host och
inga följda omdirigeringar utan källverifiering. Ingen exekvering av hämtad HTML/JavaScript.

Föreslaget read-only kontrakt under befintlig modul:
`GET /api/v1/modules/meal-planner/school-meals?schoolId=...&year=...&week=...`.
Normaliserat svar:

- skola: stabilt lokalt ID, visningsnamn, provider/source-ID och officiell källänk;
- vecka: ISO-år/vecka, status, `fetchedAtUtc`, nullable `sourceUpdatedAtUtc`, `isStale`, `dateBasis`;
- dagar: DateOnly, måltidstyp `lunch`, status och ordnad lista av originalbenämningar;
- varje alternativ: originaltext, nullable källetikett och ordning; inga fabricerade veg-/allergiantaganden.

Använd separat läs-DTO; återanvänd `MealPlannerMealTypes.Lunch` och datumsemantik, inte `Meal`/`ScheduleDay`
som kräver hushållsidentitet/portioner. Skolmat ska inte skrivas till hushållets schema eller skapa
inköpsrader. Framtida jämförelse använder datum, originaltext och alternativ via kontraktet; liknande
rätter som spaghetti med köttfärssås/pasta bolognese kräver senare normalisering, inte exaktsträngsantagande.

Begränsad processcache, exempelvis två skolor × aktuell/föregående/nästa vecka, föreslagen TTL6h och
max24h märkt stale vid fel, endast efter accepterade leverantörsvillkor. Ingen DB behövs i v1.
Cacheförlust vid API-omstart är accepterad teknisk begränsning; om historiska Musikugglan-menyer krävs
över omstarter behövs separat scopebeslut om befintlig lagring, inte dold ny databas.

## UI och berörda filer

Behåll FamilyExperience och dess befintliga Meal Planner-sektion. Föreslå en tydligt avskild
“Skollunch” under veckans matsedel, gemensam veckoväljare och två skolrader per vardag; på mobil
vertikal stapling. Alternativt skolväljare om ägaren föredrar mindre höjd. Inte separat Home-modul.
Visa originaltext, källa/senast hämtat och ärliga tom-/felstatusar. Ändra inte hushållets middagsval.
44px tryckytor, tangentbord/fokus, skärmläsare, iPhone-safe-area och alla tre teman enligt tokens.
Nytt UI-specimen i UX Lab markeras review/candidate, aldrig owner-approved utan beslut.

Konkreta planerade ytor (inte skapade/ändrade av uppdraget):

- `src/BigBrain.Api/MealPlanner/SchoolMeals.cs` och fokuserade leverantörsfiler: DTO/adaptrar/cache.
- `MealPlannerEndpoints.cs`, `Program.cs`: endpoint och befintlig DI/HttpClient-registrering.
- `src/BigBrain.Web/src/meal-planner/MealPlanner.tsx`, liten `SchoolMeals.tsx`, `src/api.ts`:
  läsflöde, veckonavigation, cancellation och tydliga tillstånd. Behåll `family` presentation variant.
- `src/BigBrain.Web/src/family/FamilyExperience.tsx` endast om sektionens placering behöver justeras;
  befintlig `styles/family.css`, tokens och `ux-lab/UXLab.tsx` för nödvändigt specimen.
- API/MealPlanner-tests och frontend SchoolMeals/MealPlanner-tests. ShoppingListStore och dess API
  lämnas orörda; ingen korsmodulläsning av databas.

## Tester och kommande Definition of Done

Fokuserade offline-fixturer: två verkliga källformat när tillåtna, veckor/årsskifte, fler alternativ,
frånvarande/ändrad HTML, felaktiga datum, stängd/ej publicerad, timeout/stale/cache/cancellation,
XSS-säker textrendering, ISO-veckonavigation och inga hushålls-/Shopping List-mutationer. Testa
nätverksgränser/okänd skola utan generella säkerhetsramverk. Relevanta frontendbuild och regressioner;
inte rutinmässigt hela repositoryt. Därefter tillåtna verkliga källor och ägarens iPhone/PWA-acceptans.

Färdig funktion kräver båda skolornas verkliga verifierade luncher vecka för vecka, växling mellan
veckor/skolor eller samtidig visning, korrekt saknad/lov/stale/felhantering, bibehållna modulgränser,
strukturerad framtida middagsjämförelse, automatiska tester och uttrycklig ägaracceptans. Ingen AI.
En fungerande Musikugglan-parser ensam uppfyller inte tvåskolorsmålet.

## Remaining work

### Blockerare, komplexitet och nästa beslut

1. **Rosenfeldt:** exakt officiell skolidentifiering + tillåten automatisk åtkomst/JSON behöver styrkas.
   Robots-disallow och kommunens robotkontroll får inte kringgås. Ingen mer crawl föreslås.
2. **Musikugglan:** aktuell HTML fungerar; automatiserings-/cachevillkor och år-/historikförväntningar
   behöver avgöras. Ingen bekräftelse att tom vecka betyder lov.
3. **UX/scope:** ägaren godkänner nästa avgränsade implementation och aktuell-vecka-begränsning för
   källor utan arkiv, samt om båda skolor visas samtidigt. Visuell slutacceptans kommer efter test.

Bedömd komplexitet: **MEDIUM** när källgrindarna är uppfyllda, två små adaptrar och ett läs-UI;
leverantörsåtkomst är osäkerheten, inte behov av plattform. Rekommenderad minsta sammanhängande sprint:
read-only skolmåltider i befintlig Family/Meal Planner med ovanstående DoD. Starta inte automatiskt.
Ingen ny ADR bedöms nödvändig för detta förslag.

## Dokumentationsverifiering och bevarande

Denna rapport och BB-158-frysningen är rena Markdown-ändringar från accepterad main.
Dokumentationsvalidator/länkar, whitespace, sekretess och kodtree-bevarande kontrolleras före push.
Ingen kodtestsvit körs; det finns ingen skolmatsimplementation att testa. Endast de ovan redovisade
publika GET-observationerna är ny källevidens. Inga hemligheter, privata kontakter eller råloggar publiceras.

## Resumption

Efter oberoende granskning: ägaren beslutar om den avgränsade skolmåltidsimplementationen.
Inhämta rätt Rosenfeldt-källidentitet och tillåtet åtkomstsätt samt avgör Musikugglans datum-/veckogräns
innan källa aktiveras. Återanvänd fynden; ingen ny generell källutredning behövs. BB-158 förblir fryst.
