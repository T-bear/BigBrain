# Family View — skolmåltider: källverifiering och minsta implementation

## Metadata

Detta är en sanerad GitHub-version. Inga hemligheter, privata data eller råloggar publiceras.

## Status

Aktuell läsning: [Skollunch v2-plan](#skollunch-v2--plan-för-hämtning-vid-behov-2026-10-23).
V1 och v2-planen är mergade och CI-verifierade. [V2-implementation](#skollunch-v2--default-off-implementation-2026-10-23) är under review, default-off och inte deployad. Äldre daterade statusar bevaras.

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

## Skollunch v1 — offline implementation 2026-10-23

**REVIEW CHECKPOINT — IMPLEMENTED OFFLINE / NOT DEPLOYED / NO EXTERNAL ACCESS.**
Branch `family/school-lunch-v1` from main `7f3dfd149ec5776b792a993faa2711cb33cc572e`.
Publication subject `feat(family): add offline school lunch preview` resolves commit/tree via Gitmetadata.
Owner authorization now permits offline implementation, superseding preparation-only scope above.
Live-source permissions remain unresolved. BB-158 and Finance are untouched and frozen.

### Source evidence and preserved reviews

The owner supplied Rosenfeldtskolan facilityId `C9643AEF-D05B-463E-84CB-76BEB47FF685`, RSS URL
`https://www.skolmat.info/api/public/matsedlar/C9643AEF-D05B-463E-84CB-76BEB47FF685/rss?period=current-week`
and one actual item for 2026-10-19. This is **owner-reported manual retrieval**, not a Codex live test.
The supplied channel/version/TTL/atom-link description covers five dates, but only the quoted Monday
item is available as literal data. The small offline RSS envelope uses that item; it does not fabricate
the other four lunches. TTL 5 is not treated as permission or a configured fetching interval.

Earlier documentation remains preserved on separate, unmerged branches:
`docs/rosenfeldt-school-meal-source` at `55f781aa4b54646815305656b2f0e1f588dac986` and
`docs/skolmat-api-verification` at `5394066139faefd20eed8a0e8e94e77a4bad541c`.
Their source-access and third-party JSON findings were read, not merged/copied or rewritten. The new
owner-supplied RSS identity supersedes UNKNOWN for offline parser configuration only, not access rights.

No raw Musikugglan HTML fixture was found in the repository/local source inventory. Prior notes describe
week heading, weekday table and separate snack section but not sufficient exact DOM evidence for a
reliable parser. **HTML parser NOT IMPLEMENTED**, per the explicit owner fallback. `sourceEvidenceMissing`
is returned with no dishes and unknown date provenance. No guessed HTML or fake school menu is used.
The smallest next prerequisite is an owner-supplied saved HTML response from the already identified
Tant Grön page, including week heading, lunch table and snack boundary. No new live request was made.

### Implemented components and contract

- `src/BigBrain.Api/MealPlanner/SchoolMeals.cs`: typed week/day DTOs, fixed school/source identities,
  bounded RSS 2.0 parser and explicit offline sample. .NET XmlReader prohibits DTD and disables
  XmlResolver; input capped at 512 Ki characters. Required fields, facility/date GUID, ISO week,
  duplicate dates and malformed XML fail closed. Ordered duplicate dish alternatives are preserved.
  XML decoding precedes bounded `<br>` splitting and HTML text decoding. No vegetarian labels,
  HTML execution, household storage or HTTP dependency.
- `MealPlannerEndpoints.cs`: `GET /api/v1/modules/meal-planner/school-meals?schoolId=rosenfeldt|musikugglan&year=2026&week=43`.
  Unknown school/invalid ISO week returns Problem Details 400. Normal mode always returns offline or
  missing-source-evidence; there is no configuration flag that activates live HTTP.
  Optional `fixture=true` requires ASP.NET Development environment; other environments return 403.
- Week DTO: school ID/name, fixed source name/link, requested ISO year/week, status, `dateBasis`,
  nullable `fetchedAtUtc`, `isFixture`, five weekday slots with date/status/ordered dishes. Slots are
  the requested calendar, not evidence that a menu exists. Published RSS dates use `sourceGuid`;
  unavailable/invalid data uses `unknown`. `fetchedAtUtc` is null because nothing was fetched.
  Missing days are `missing`, never inferred holidays; wrong fixture week is `notPublished`.
- `SchoolLunch.tsx`, `api.ts`, `FamilyExperience.tsx`, scoped `family.css`: separate Skollunch section
  showing both schools without changing meals, shopping or navigation. Opening Family causes **zero
  school API/source calls**. In development only, explicit button loads both local API fixtures for
  fixed week 43/2026 with a persistent TESTDATA/non-live label. No navigation that could relabel it
  as current. Cancellation prevents late updates after closing/unmounting. Loading/error states remain
  explicit. Source links are ordinary user navigation, not automatically fetched resources.
- UX Lab includes an EXPERIMENTELL offline specimen; no owner approval inferred. Existing BBButton,
  Family structure and theme tokens retained; 44px actions/links, focus indication and wrapping text.

### Verification and limits

Focused backend: `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --no-restore --filter
'FullyQualifiedName~SchoolMealsTests|FullyQualifiedName~MealPlannerApiTests' -v quiet`: **22 PASS**,
including API/production fixture gating and unchanged household meal readback. XML/XXE rejection,
entities/br, duplicate alternatives/dates, size limit, missing days, invalid identity/date and ISO
boundary covered. Initial build caught nullable/analyzer errors; corrected before passing tests.
Frontend: `npm test -- --run src/meal-planner/SchoolLunch.test.tsx src/meal-planner/MealPlanner.test.tsx
src/ux-lab/UXLab.test.tsx`: **19 PASS**. `npm run build`: **PASS** (TypeScript + Vite).
No broad Finance/backend suites or external tests. No network or storage dependency exists in the
school service; no Shopping List production file changes. Household regression suite reused above.
Additional Family composition, formatting, documentation/link, whitespace and staged secrets checks
are recorded at publication below. Browser/iPhone visual acceptance is **NOT RUN**; responsive CSS
and rendering tests do not prove device acceptance. HTML structure tests are **NOT RUN** because the
parser is intentionally blocked by missing evidence, rather than tested against invented markup.

### Running the offline review and next decision

Run the existing API in Development and Web via its existing development command; Family → Skollunch
→ Visa testvecka 43, 2026. Only the supplied Monday dishes appear; other Rosenfeldt days remain missing,
Musikugglan displays its evidence limitation. Production builds hide this control; API independently
enforces the environment gate. No live fallback, schedule or persistence is introduced.

A future external adapter requires separate owner approval after access/cache terms, a verified HTML
fixture, and source-specific request/error tests. It would use existing Meal Planner HttpClient
mechanisms and fixed sources; this checkpoint provides no live activation switch. No database or
migration needed. Remaining UX decision: owner review on iPhone/PWA and presentation acceptance.
The offline DoD permits the documented Musikugglan evidence fallback; the two-school live feature
is **not complete**, and this delivery is a Review Checkpoint rather than a live Merge Candidate.

Publication checks: Family integration test
`npm test -- --run src/App.test.tsx -t 'starts on the calm Home launcher'`: **1 PASS**, 17 unrelated
tests intentionally unselected. An initial mistyped filter selected zero tests and is not counted
as evidence. Scoped `dotnet format ... --include` and Prettier check: **PASS**.
`node scripts/verify-documentation.mjs`: **PASS**, 274 Markdown files / 91 backlog IDs.
`git diff --check`: **PASS**. No source-provider request or real menu download occurred.

## Skollunch v1 — completion review 2026-10-23

**REVIEW CHECKPOINT — OFFLINE ONLY, NOT DEPLOYED.** Same branch `family/school-lunch-v1`,
main baseline `7f3dfd149ec5776b792a993faa2711cb33cc572e`; previous checkpoint
`d85d9996602a2767183f719a308d11e4dce1d1fb` remains intact. Publication subject
`feat(family): parse offline Musikugglan lunch excerpt` resolves this additive commit/tree.
Earlier sections describe their dated checkpoint; this section supersedes the HTML-parser limitation.

### Actual inputs and remaining evidence gap

Neither `rss.rss` nor `musikugglan.html` was found in the repository or checked local attachment/temp
locations. Files attached only in ChatGPT are not claimed to have been read. **No original source file
was used and no external request was made.** The owner's normalized HTML excerpt and five stated dish
texts are embedded verbatim as `MusikugglanSchoolMeals.ExampleHtml`; it is not the original HTML.
The existing RSS example retains the only available real item, Monday 2026-10-19. The remaining four
real lunches cannot be reconstructed and are not invented. A five-item RSS regression uses explicitly
synthetic text solely to prove date/row handling, not as a menu shown to the user.

### Implementation and semantics

`MusikugglanSchoolMeals.cs` accepts the known unique Matsedel v. NN heading, its immediately following
lunch table and the subsequent matching Mellanmål v. NN boundary. It ignores comments/script/style
blocks while locating the fragment, then parses only the bounded table with .NET XmlReader/XElement.
HTML entities are decoded and XML-escaped before parsing, br is normalized, inline p/br spacing and
text whitespace are normalized. DTD/external resolution are disabled. Input length, regex time,
element count and nesting are bounded. No new dependency or general HTML engine is introduced.
Unknown tags, malformed table/cells, ambiguous heading, duplicate/out-of-order weekdays and broken
snack boundary return `invalid`. Valid absent/empty weekday rows return `missing`; an empty table or
a different week returns `notPublished`. The snack text after its heading is never included.

This is a deliberately narrow HTML-fragment contract, not a claim to accept arbitrary browser-valid
HTML. Omitted closing tags, unquoted attributes or structural changes may be rejected safely. The
original full page must still be replayed offline before claiming compatibility with that file.
Dish text/slash punctuation is retained as one ordered string per cell; no vegetarian classification.

The shared DTO remains unchanged. `dateBasis=sourceGuid` identifies RSS dates.
`dateBasis=htmlWeekRequestedYear` explicitly means the HTML supplied the week **but not the year**;
DTO year/dates are derived from the requested display year. This is not evidence of the source year.
The preview is pinned to owner-observed display year 2026 and rejects other fixture years. Missing
or invalid data keeps its existing honest statuses. No database, household or Shopping List writes.

Family displays separate school sections: Rosenfeldt week 43/2026 and Musikugglan week 44 with source
year explicitly unknown. The fixed test requests cannot present those menus as one current week.
The banner reads “TESTDATA – manuellt hämtade exempel, inte aktuell live-matsedel.” Each section
also discloses excerpt/original-file limits. No fetch occurs on opening Family; only explicit
Development preview loads the local API. Production fixture rejection, cancellation, existing
components and theme behavior remain intact. No external HTTP adapter or activation flag exists.

### Verification, completion limit and next action

Focused tests add the five-item synthetic RSS, exact GUID dates, all five owner-supplied HTML lunches,
entity/whitespace/br handling, snack exclusion, slash retention, missing rows, malformed structure,
wrong week/year, ambiguous headings and resource limits. API tests cover both local parsers and
Production rejection; existing household readback and no-request frontend tests remain.
Results are recorded below after execution. No full repository suites or live/source-provider tests.

**Remaining:** full original-file verification (especially the actual HTML envelope/entities and the
four missing RSS lunches), iPhone/PWA owner acceptance and separate source-access/activation approval.
The offline implementation can now show both supplied source formats, but the complete real RSS week
is not available. This is a review checkpoint with that explicit evidence limitation, not a claim of
full real-file or live acceptance. The smallest next action is to make the two originals available
in the repository workspace for a bounded offline replay after review. No merge or deployment here.

Completion verification: **35 backend tests PASS** using the same focused SchoolMealsTests /
MealPlannerApiTests filter; backend compiled with analyzers. The first transitional run was 21 PASS /
1 FAIL because the old test still expected the removed missing-HTML status; it was replaced with
wrong-week/year and actual HTML assertions before the final passing run. **19 frontend tests PASS**
(SchoolLunch, MealPlanner, UXLab); **TypeScript/Vite production build PASS**. Scoped dotnet format
and Prettier check **PASS**. Documentation validator/local links **PASS** (274 Markdown / 91 backlog
IDs), whitespace check **PASS**. Earlier Family composition evidence is reused: its embedding and
navigation are unchanged. No new live, browser or original-file evidence is implied by these checks.


## Skollunch v1 — original-file verification 2026-10-23

**REVIEW CHECKPOINT — OFFLINE ONLY, NOT DEPLOYED.** Same `family/school-lunch-v1` branch;
main remains `7f3dfd149ec5776b792a993faa2711cb33cc572e`, prior checkpoint
`6e0988d25153d4bfc52242754bb1b456be30637f`. Publication subject
`fix(family): verify school lunch original fixtures` identifies this commit/tree through Git metadata.
Earlier missing-original statements above are historical and superseded by this section.

Both owner-provided files were available and read locally, without downloading anything:

| Original | Bytes | SHA-256 |
| --- | --- | --- |
| rss.rss | 2684 | `41cc0bd732e0b876cb67eecec1366c8937493183d13bdb08344d29170c41f034` |
| musikugglan.html | 1976 | `f0c8571e042a7967d51bfd681e3ac584e1ad6425890718cba9fb35ec12c9d30c` |

The temporary originals are not added to Git. Existing embedded offline fixtures now contain the
full RSS menu and the original HTML lunch/snack section, retaining nonbreaking spaces and padding;
unrelated stylesheet/page chrome and repeated empty HTML tail are omitted. Line endings are
normalized in source. No invented lunches remain in preview or the five-day positive tests.
Synthetic malformed/entity cases remain deliberately isolated negative/normalization tests.

Initial full-file replay: RSS **PASS**, HTML **FAIL** (`invalid`). The actual HTML includes an empty
strong element before the title and empty paragraphs before/after the table. The former strict
heading/table boundary rejected these. The minimal correction removes whitespace-only strong and
p elements before locating that same bounded fragment. Nonempty intervening content remains rejected,
with a regression test. Existing DTD prohibition, null XML resolver, size/regex/depth limits, week
checks, required snack boundary and narrow allowed tags remain intact. No dependencies were added.

Final replay reads both complete original files and compares every resulting date and ordered dish
against the embedded preview. Rosenfeldt has all five GUID dates 2026-10-19–23 and two alternatives
per day, including identical Thursday/Friday alternatives. XML/HTML entity decoding preserves
ampersands. Musikugglan has all five lunches; whitespace is normalized, slash remains within the
original dish string, and none of the snack section becomes lunch. HTML verifies week 44 only;
year 2026 remains owner-observed display context, never a source-verified year. Wrong-week/year
fixture requests remain unavailable. The UI retains separate historical test weeks and explicit
TESTDATA labeling, with obsolete missing-file text removed.

Verification commands and results:

- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --no-restore --filter
  'FullyQualifiedName~SchoolMealsTests|FullyQualifiedName~SchoolMealsOriginalReplayTests|FullyQualifiedName~MealPlannerApiTests' -v quiet`:
  **38 PASS** (36 retained backend cases and 2 full-original replay cases). The two local-only original replay cases use the
  owner files; their temporary harness is removed before publication. Published regression cases
  use repository fixtures, not a private filesystem path.
- `npm test -- --run src/meal-planner/SchoolLunch.test.tsx src/meal-planner/MealPlanner.test.tsx src/ux-lab/UXLab.test.tsx`:
  **19 PASS**; `npm run build`: **PASS** (TypeScript/Vite).
- Existing API tests cover Development-only fixtures, Production rejection and unchanged household
  meals. Parsers/endpoint have no HTTP client or storage write path; opening Family triggers no
  school request. No external provider call was made. No network-capable source adapter was added.
- Previous unchanged Family composition/theme evidence is reused. No full repository suite,
  browser/iPhone test, merge, deployment or source activation was performed.

Remaining: independent review and owner iPhone/PWA acceptance; external retrieval still requires
separate access/activation approval and implementation. HTML parsing remains deliberately bounded
to the verified structure, not a general HTML engine. No original-file blocker remains. Finance and
BB-158 stay frozen and untouched.

Publication checks: scoped `dotnet format BigBrain.slnx --no-restore --include` for the two parser
files and SchoolMealsTests **PASS**; scoped Prettier **PASS**. Documentation/link validator
`node scripts/verify-documentation.mjs` **PASS** (274 Markdown files, 91 backlog IDs).
`git diff --cached --check` **PASS**; staged Gitleaks **PASS**, no leaks. Original file hashes
remained unchanged after tests. Only the five school-lunch code/test files and six relevant
existing documentation files are included; no tmp originals or local-only replay harness.

## Skollunch v2 — plan för hämtning vid behov, 2026-10-23

**REVIEW CHECKPOINT — ENDAST PLANERING.** Branch `docs/school-lunch-v2-preparation` från
accepterad main `6d7f6de68f618f6c06b0845b659983ec12529f96`, tree
`23736fbf23a29fc7ad59b8105e28bf0527bff046`. Publiceringsämne
`docs(family): prepare bounded school lunch live fetching` identifierar planens commit/tree.
Detta avsnitt är aktuell v2-plan; äldre JSON-, veckonavigations- och cacheförslag ovan är historik.
Ingen implementation, extern menyhämtning, kontakt med leverantör, merge eller deployment ingår.

### Verifierat nuläge och evidensgräns

V1 är mergad, med [CI 39959910684](https://github.com/T-bear/BigBrain/actions/runs/39959910684)
**SUCCESS** för exakt main-SHA ovan, verifierat i GitHub Actions REST i denna kontroll.
Deployment är inte utförd eller verifierad i detta uppdrag. Originalfilernas föregående offline-
verifiering och testresultat återanvänds; inga parser-/applikationstester kördes om.
`SchoolMeals.cs` och `MusikugglanSchoolMeals.cs` är rena parsrar. `MealPlannerEndpoints.cs`
returnerar offline utan fixture; fixture kräver `IsDevelopment()`. `SchoolLunch.tsx` anropar
bara lokal testvy efter knapptryck, med separata historiska veckor. Ingen liveadapter/cache finns.

### Åtkomstvillkor — blockerare före liveaktivering

| Källa | Teknisk evidens som återanvänds | Tillåten automatisk användning/cache |
| --- | --- | --- |
| [Rosenfeldt RSS](https://www.skolmat.info/api/public/matsedlar/C9643AEF-D05B-463E-84CB-76BEB47FF685/rss?period=current-week) | Ägarhämtad RSS 2.0, korrekt facilityId, GUID-datum och fem poster; `ttl=5` samt atom self-link. | **BLOCKED för aktivering:** tidigare robots-observation `Disallow: /`; ingen verifierad RSS-specifik tillåtelse eller cachelicens. TTL är en teknisk cacheledtråd, inte tillstånd eller krav på femminuterspollning. |
| [Musikugglan/Tant Grön](https://www.tantgron.nu/matsedel/) | Ägarhämtad HTML med lunch och mellanmål; vecka men inget år. | **UNVERIFIED, blockerar aktivering:** tidigare robots-observation begränsade `/admin`, men inget uttryckligt automatiserings-/cachevillkor är verifierat. |

Avgränsad webbsökning efter officiella API/RSS-/användningsvillkor gav inget verifierbart
integrationsavtal för dessa två källor. Tredjepartsintegrationer och andra kommuners öppna data
används inte som rättighetsbevis. Försök att läsa de två kända robots.txt-adresserna med webbverktyget
kunde inte återge dokumenten; de tidigare robots-fynden ovan är därför daterad återanvänd evidens,
inte en ny verifiering av nuvarande innehåll. Inga menyendpoints anropades eller gissades, inga
alternativa domäner eller User-Agent-trick användes. Robots är en automatiseringssignal, inte i sig
ett juridiskt avgörande. Frånvaron av hittade villkor bevisar inte att villkor saknas.

Minsta nästa åtkomståtgärd: ägaren tillhandahåller ett officiellt publicerat villkor eller besked som
omfattar just RSS-/HTML-hämtning i privat familjesystem, hämtningsfrekvens och cachelagring/stale-visning.
För Skolmat.info måste även förhållandet till robots-begränsningen klarläggas. Codex skickar inget.
Kontaktväg för Musikugglan finns på [skolans officiella kontaktsida](https://www.tantgron.nu/kontakt).
Ägarens implementationsbeslut ersätter inte källans åtkomstvillkor.

### Minsta tekniska lösning — föreslagen, inte implementerad

- Behåll befintlig endpoint, skol-ID:n, DTO-grund, parsrar och Family-sektion. En liten
  modullokal `SchoolMealSourceClient` hämtar de två fasta HTTPS-adresserna; format väljer befintlig
  parser. En liten `SchoolMealService` håller högst två källposter i minnet och samordnar samtidiga
  anrop per källa. Ingen generell adapterplattform eller ny cachetjänst behövs.
- Använd `IHttpClientFactory` i `Program.cs`, cancellation och Problem Details enligt befintliga
  mönster. `Media/MediaPosters.cs` är referens för named client, ResponseHeadersRead och felmappning;
  återanvänd mönstret, inte Media-modulens tjänst eller tillstånd.
- Föreslagen konfiguration under befintlig `MealPlanner`-sektion: `SchoolMeals.Enabled=false`
  samt separata `RosenfeldtEnabled=false` och `MusikugglanEnabled=false`. Hämta aldrig för en
  avstängd källa. Options binds vid processstart som befintliga `MealPlannerOptions`; ingen dold
  hot-reload eller ny administrationsendpoint. Eventuell senare omstart kräver deploymentgodkännande.
- Inga användarstyrda URL:er, cookies, credentials, JavaScript eller följande av inbäddade länkar.
  Stäng automatisk redirect; 3xx blir otillgänglig källa tills ny adress verifierats. Använd endast
  fasta publika HTTPS-källor. Begränsa även strömmad/dekomprimerad body till 512 KiB, inte enbart
  Content-Length; behåll parserns separata teckengräns. Total deadline 5 s ska omfatta headers
  **och body** via cancellation-token (HttpClient-timeout ensam räcker inte med ResponseHeadersRead).
- Konkreta berörda ytor vid senare implementation: nya små källklient/servicefiler under
  `src/BigBrain.Api/MealPlanner/`, `MealPlannerContracts.cs` (options), `SchoolMeals.cs` (DTO),
  `MealPlannerEndpoints.cs`, `Program.cs`, `src/BigBrain.Web/src/api.ts`,
  `meal-planner/SchoolLunch.tsx` och befintliga skolmåltidstester. Endast nödvändiga lokala CSS-/UX-Lab-
  tillstånd tillkommer. Inga ändringar av hushållsstore, Shopping List, Finance eller BB-158.

### Cache, fel och datum — föreslagen policy att godkänna

| Fall | Beteende |
| --- | --- |
| Family öppnas | Två lokala read-only API-anrop, ett per skola, avbryts vid navigation. Ingen frontendpollning. Avstängd källa ger offline utan utgående HTTP. |
| Cache miss/utgången | Högst ett pågående försök per källa. Föreslagen minsta intervall 6 h mellan försök, även vid fel/veckoskifte, delad av alla klienter. Ingen intern retry-loop. |
| Belastning | Vid kontinuerlig användning cirka fyra försök per källa och dygn, åtta totalt vid stabil API-process. Inga försök utan efterfrågan. Omstart tömmer minnet och kan ge ett nytt försök; detta är ingen persistent dygnskvot. Striktare leverantörskrav kan kräva annan policy och stoppar aktivering tills löst. |
| Giltig cache | Återge normaliserad meny med `fetchedAtUtc`, separat `lastCheckedAtUtc` och nullable `sourceUpdatedAtUtc`. Hämtningstid bevisar inte när menyn ändrades. HTTP validators används endast om faktiskt erbjudna; 304 kräver befintlig matchande post. |
| Timeout/HTTP/ogiltigt svar | Returnera explicit `unavailable` eller `invalid`. Tidigare giltig meny får visas endast märkt `isStale=true`, inom samma ISO-vecka och högst 24 h efter senaste lyckade validering, om cachevillkoren tillåter. Fel ändrar inte senaste lyckade tid. Ingen fallback till fixture. |
| 429/Retry-After | Ingen omedelbar retry. Nästa försök tidigast efter både lokal intervall och giltig Retry-After. Okänt/felaktigt besked ger säkert fel, inte tätare hämtning. |
| no-store/striktare villkor | Respektera källans cachevillkor; ingen stale-lagring i strid med dem. Om föreslagen cache inte medges måste policyn avgöras före aktivering, inte ersättas med obegränsad hämtning. |
| Giltig tom meny/annan vecka | `notPublished`; ingen maskering med tidigare meny. En saknad dag är `missing`, aldrig gissat lov. Ingen upstreamhämtning för godtyckliga historiska/framtida API-veckor. |
| ISO-årsskifte | Serverns Europe/Stockholm-datum och ISOWeek.GetYear/GetWeekOfYear bestämmer aktuell vecka, med TimeProvider för test. RSS-GUID måste matcha exakt ISO-år/vecka; kalenderår får inte ersätta ISO-år. |
| HTML utan år | Matchande källvecka får mappas till aktuell ISO-vecka med `htmlWeekRequestedYear`, tydligt härlett år. Återanvänd aldrig föregående års/veckas cache som aktuell. Veckomatch ensam bevisar inte att källtexten uppdaterats det året; denna kvarstående osäkerhet måste accepteras uttryckligen eller hindra visning. |

V2:s lilla DTO-delta är freshness-/felmetadata, inte en ny måltidsmodell. Fel vid ogiltiga
klientparametrar förblir Problem Details 400; källfel ska kunna visas per skola utan att slå ut den
andra. Status `published` avser källa inom begärd vecka, inte garanterat nypublicerad information.
Inga råa HTML-fel eller upstreamdetaljer exponeras för användaren. Byte av vecka ogiltigförklarar
synlig föregående veckomeny även om cache-TTL återstår. Efter omstart finns ingen menyhistorik.

Original-HTML:en angav vecka 44 när ägaren observerade den under vecka 43. Det visar att publicisten
kan byta innehåll i förväg; det ger inte ett generellt framtids-API. V2 visar då att aktuell veckas
meny saknas, inte vecka 44 som vecka 43. Ingen historik-/framtidsnavigation föreslås.

### Family View och avgränsad Definition of Done

På iPhone: befintlig Skollunch-sektion, aktuell veckorubrik, båda skolor vertikalt och måndag–fredag
med originalalternativ i ordning. Behåll teman, fokus och touchytor. Visa källa, senast kontrollerat,
härledda datum och tydliga offline-/fel-/stale-tillstånd per skola. Ingen ny navigation. Development-
exemplen behålls bakom uttryckligt separat testval med TESTDATA och sina historiska veckor; aldrig
som livefelreserv. Hushållets middagar och inköpslista lämnas orörda.

Minsta kommande sprint, bedömd **MEDIUM**, efter separat implementationsbeslut:

1. Två fasta HTTP-källor, default-off options och bounded hämtning/cache ovan inom Meal Planner.
2. Återanvänd parsrar och versionerad endpoint; korrekt freshness, datumgrund och separat källfel.
3. Family öppnar aktuell vecka genom lokal API-läsning; test/live-data hålls åtskilda.
4. Fokuserade tester med stub HttpMessageHandler och styrbar klocka: default-off gör noll HTTP-anrop,
   samtidiga klienter ger en hämtning, TTL/cooldown, 304/429/redirect, timeout inklusive body,
   storleksgräns, ogiltigt svar, cacheförlust, gammalt/annat veckoinnehåll, ISO-årsskifte och HTML utan år.
   Befintliga parserfixturer återanvänds. Inga verkliga nätverksanrop i automatiska tester.
5. API-/frontendtester verifierar isolerade skolfel, cancellation, tydlig stale/testmärkning och inga
   mutationer av måltidsplanering/inköpslista; relevant build. Ingen rutinmässig full repository-svit.
6. Teknisk Review Checkpoint skiljs från liveacceptans: tillåtna verkliga hämtningar för båda skolor,
   separat deployment/aktivering och ägarens iPhone/PWA-acceptans krävs innan livefunktionen kallas klar.

### Exakta beslut före nästa etapp

- Godkänner ägaren en avgränsad default-off implementation av aktuell vecka för båda skolor,
  utan historik/framtidsvy eller påverkan på middagsplaneringen?
- Vilket officiellt underlag tillåter automatisk hämtning och cache för respektive källa,
  inklusive RSS trots tidigare robots-disallow? Detta är **kvarstående aktiveringsblockerare**.
- Godkänns föreslaget 6 h-intervall, högst 24 h märkt stale inom samma vecka och förlust av cache
  vid omstart, förutsatt att leverantörernas villkor medger detta?
- Accepteras Musikugglans tydligt härledda år/datum, med veckomismatch som saknad meny och utan
  möjlighet att bevisa källans publiceringsår? Annars krävs källstöd för årtal före livevisning.

Inget av dessa beslut antas genom publicering av planen. Ingen ny ADR behövs för det föreslagna
återbruket; eventuell faktisk konflikt ska hanteras före implementation. Stoppa efter review.

Planens verifiering: dokumentationsvalidator inklusive lokala länkar **PASS** (274 Markdown-filer,
91 backlog-ID:n), `git diff --check` **PASS**. Inga applikationstester eller byggen behövs för denna
rena dokumentationsdelta. Main och produktionskod lämnas oförändrade; scope/secrets kontrolleras
vid publicering. V1:s giltiga test-/CI-evidens återanvänds utan omkörning.

## Skollunch v2 — default-off implementation, 2026-10-23

**MERGE CANDIDATE — IMPLEMENTED / OFFLINE VERIFIED / NOT DEPLOYED / SOURCES DISABLED.**
Branch `family/school-lunch-v2` from accepted main `1125b65393e99d3606fbdb00d67cbd02d5582f36`.
The owner approved merging plan commit `783fd507ee6155cea02809e51a01a62f7ff16390`; that merge
preserved tree `29fe30b25357611393db9f7234eb7ed630c7d5f7` and
[CI39961868198](https://github.com/T-bear/BigBrain/actions/runs/39961868198) is **SUCCESS**.
Only the documentation plan has been merged. Implementation publication subject
`feat(family): add disabled school lunch fetching and cache` identifies the candidate SHA/tree.
Implementation merge requires independent review and owner approval of its exact SHA; deployment
requires a subsequent separate clearance. No deployment or production inspection occurred here.

### Delivered scope and files

- `SchoolMealSourceClient.cs`: two hardcoded HTTPS destinations, .NET HttpClientFactory transport,
  no redirects/cookies/credentials, pooled-connection lifetime six hours, GET only. Total five-second
  deadline covers headers and streamed body; 512 KiB bound applies after gzip/deflate decoding.
  UTF-8 and source-specific RSS/XML or HTML media types are required. Only a complete HTTP200 body
  or validated conditional304 is accepted. Raw upstream errors/content are not exposed as errors.
- `SchoolMealService.cs`: singleton with exactly two cache entries and one semaphore per school.
  Current ISO year/week comes from TimeProvider in Europe/Stockholm. Same-school concurrent callers
  share cached results; other schools are independent. Six hours between attempts includes failures,
  cancelled attempts and week rollover; no scheduler, retry loop, database or persistent cache.
- `MealPlannerContracts.cs`, `Program.cs`: default-off options and scoped module registrations.
  Existing API-wide TimeProvider is reused. All three new options default to false:
  `MealPlanner:SchoolMeals:Enabled`, `RosenfeldtEnabled`, `MusikugglanEnabled` (the latter two
  under the same SchoolMeals section). No production configuration was changed.
- `SchoolMeals.cs`: additive DTO fields `lastCheckedAtUtc`, `sourceUpdatedAtUtc`, `isStale` and
  `sourceStatus`. Existing parser behavior and data ownership remain unchanged.
- `MealPlannerEndpoints.cs`: same versioned read-only route. Omitting both year/week selects
  the server's current Stockholm ISO week; supplying only one or invalid input gives Problem Details400.
  Requests for other weeks do not cause external traffic. `fixture=true` still requires Development
  and never goes through the live client. Responses use HTTP `Cache-Control: no-store` so browser
  caching cannot bypass the service's week/freshness decision.
- Web `api.ts`, `SchoolLunch.tsx`: two local API reads when Family opens, per-school loading/errors,
  source/date provenance and timestamps, stale warning, cancellation on unmount/mode change. No
  polling, new navigation, global state or household editing. Development fixtures remain an explicit
  separate historical test mode, never a fallback. `UXLab.tsx` uses a static offline specimen without
  requests, preserving the lab's established no-network contract and experimental review status.
- Focused `SchoolMealLiveTests.cs`, existing `SchoolMealsTests.cs` evidence, `MealPlannerApiTests.cs`,
  SchoolLunch/App frontend tests. Existing RSS/HTML originals are not reread or republished.

### Exact cache and failure behavior

ETag/Last-Modified are sent only for an existing matching cached menu. A 304 without such a menu is
invalid. Successful304 updates validation time, not original fetch time. Valid empty/different-week
content replaces old data with `notPublished`; it is not hidden behind stale data. Missing days do not
mean holidays. Week rollover removes the prior menu/validators but preserves attempt cooldown.

On timeout, malformed content or HTTP failure, previously published data may be returned as stale
only for the same ISO week, up to 24 hours after last successful validation and only if original
cache policy allows it. Failed attempts never refresh that validation time. `must-revalidate`
prevents stale reuse. No-store/no-cache, remaining max-age below six hours (including HTTP Age),
or earlier Expires without max-age return `cachePolicy` and discard old content/validators. This
conservatively blocks incompatible source caching; it does not compensate with more frequent requests.
Retry-After on429 can extend the six-hour cooldown; there is no immediate retry. Raw errors stay internal.

A cancelled caller may cancel its fetch; the semaphore is released and cooldown remains. Other
waiting callers then receive the honest error/stale result. On process restart both entries disappear,
so another on-demand attempt is possible; this is explicitly not a durable daily quota. No database
writes, migrations or automatic database restore are involved.

HTML year remains derived, not source-verified; week mismatch is notPublished. The fixed original
week44 example does not authorize a future-menu API. Source access/cache rights and owner acceptance
of derived year remain activation gates. Strict content-type/UTF-8/cache behavior has only offline
verification; actual allowed HTTP headers may require a separately reviewed compatibility correction.

### Verification and remaining acceptance

Use stub HttpMessageHandler instances and injected clocks only; zero school-provider HTTP calls.
Tests cover disabled flags, concurrency, cache interval, stale expiry, invalid/empty replacement,
conditional304, 302/206/500 rejection, 429 cooldown, no-store/no-cache/max-age/Expires, must-revalidate,
content type/encoding, bounded streaming with/without Content-Length, header/body timeout, cancellation,
cache loss, separate sources and Stockholm ISO rollover. API integration checks server-selected week,
per-school failure and unchanged household meals. Frontend tests check automatic local API reads,
separate failure/stale display, historical mode and discarded late responses; UX Lab stays offline.

Transient verification findings were corrected within scope: disposable semaphore ownership required
Entry.Dispose; a test array triggered a style analyzer; the new mounted read exposed UX Lab's no-request
contract, now retained with static offlinePreview; prior static offline-heading expectation and test
DOM cleanup were updated. No assertion protecting source isolation or parser correctness was removed.
Final commands/results are recorded below; no full repository suite is required.

Remaining gates: independent architecture review, exact implementation-SHA merge approval, separate
API/Web deployment clearance, verified source rights/cache conditions and explicit source activation,
then authorized live verification and owner iPhone/PWA acceptance. Neither source was activated and
no live/end-to-end success is claimed. Finance and BB-158 remain frozen and untouched.

### Later operational handoff — not executed

Keep all three flags false through any approved deployment. Reuse the existing API-then-Web build,
health/identity and safe recovery process; preserve mounts, databases and all unrelated services.
Before any later activation, record source-specific permission/cache evidence and configure only
the corresponding flags through the existing private configuration mechanism. Options are startup
bound; an API restart is a separately authorized operational action, not an endpoint toggle.
To disable: set global Enabled=false using that same mechanism and approved restart; no file/data
cleanup or database/image rollback is needed to disable fetching. In-memory cache is lost on restart.
The current task does not execute this procedure or imply that deployment prerequisites are verified.


Final verification for this candidate:

- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --no-restore --filter
  'FullyQualifiedName~SchoolMeal|FullyQualifiedName~MealPlannerApiTests' -v quiet`: **63 PASS**, zero
  failed/skipped; API and tests compiled with analyzers. Earlier 57-case pass was extended for
  cancellation, encoding, HTTP expiry/partial responses and endpoint integration, not repeated by habit.
- `npm test -- --run src/meal-planner/SchoolLunch.test.tsx src/meal-planner/MealPlanner.test.tsx
  src/ux-lab/UXLab.test.tsx src/App.test.tsx`: **21 focused component tests PASS**; the old App heading
  failed before its contract update. Subsequent `npm test -- --run src/App.test.tsx`: **18 PASS**.
  Together **39 current frontend tests PASS**, reusing the unchanged 21 component results.
- `npm run build`: **PASS**, TypeScript and Vite. Scoped `dotnet format BigBrain.slnx --no-restore
  --include ...` and Prettier for changed frontend files: **PASS**.
- `node scripts/verify-documentation.mjs`: **PASS**, local links included. Whitespace, staged secrets
  and scope checks are publication gates. No original-file replay, external HTTP or production test.

All default-off implementation acceptance checks are complete; **Merge Candidate** refers to this
bounded code scope, not live-source or deployment approval. No known unresolved P0/P1 in the tested
scope. Actual source permission/HTTP compatibility, derived HTML year acceptance and iPhone UX are
explicit outstanding activation/runtime gates, not claimed PASS. Main's documentation merge and
implementation checkpoint remain separate identities.

Publication scope: 19 files, limited to school-meal API/Web/tests and six existing canonical documents.
Documentation validator **PASS** (274 Markdown / 91 backlog IDs), staged whitespace **PASS**, staged
Gitleaks **PASS**, no leaks. Unrelated untracked work and accepted Finance/BB-158 content are preserved.

## Skollunch v3 — actual-dish presentation and offline verification, 2026-10-25

Status at the implementation checkpoint; the later [merge preparation](#skollunch-v3--merge-preparation-and-owner-reported-permission-2026-10-25) records updated owner evidence.

**REVIEW CHECKPOINT — PARTIAL; LIVE SOURCE ACCESS REMAINS BLOCKED.** Branch
`family/school-lunch-v3`, accepted baseline `485aeda2ec5819ffc7f433b51ac867c55157c19f`.
The owner reports v2 deployed; this implementation task neither inspects production nor independently
verifies that deployment. Previous operational checkpoints are preserved on their separate branch;
none of their backup/deployment scripts is included here. Publication is not merge or activation.

### Bounded implementation and evidence

The existing v2 read-only route, DTOs, fixed-source HTTP client, cache, per-source flags and Family
component already support real dish text. No second school-meal module or new access mechanism is
needed. All three flags remain false by default; no configuration, data store or registration changes.

- `MusikugglanSchoolMeals.cs`: restrict both lunch/snack week captures to ASCII digits, matching the
  verified source format. The previous regex `\d` accepted characters that invariant `int.Parse`
  cannot parse, so malformed upstream headings could throw instead of returning `invalid`.
  A regression using a modified offline heading failed before the correction (1 failed / 8 passed),
  then passed. The original five lunches, whitespace/entities, literal slash-separated wording,
  lunch/snack boundary and explicit `htmlWeekRequestedYear` provenance remain unchanged.
- `SchoolLunch.tsx`: show each lunch's calendar date beside its weekday, without converting a
  date-only value into the phone's local date. Show the existing `sourceUpdatedAtUtc` separately
  from fetch/check times when supplied. This represents the source's HTTP Last-Modified claim,
  not independently verified publication time. No timestamp is invented when it is absent.
  Existing per-school results, stale warning, source links, missing-day text and development-only
  historical preview remain. Theme/layout contracts and other Family sections are unchanged.
- `SchoolMealsTests.cs`, `SchoolMealLiveTests.cs`, `SchoolLunch.test.tsx`: add malformed week,
  both-school/global/individual flag coverage, source-versus-fetch timestamp preservation through
  cache/error, calendar/ISO-year boundary rendering, missing days, duplicate dish order and literal
  rendering of hostile markup. Synthetic boundary/security inputs are tests, not new real menus.

Existing owner-derived offline fixtures are used: Rosenfeldtskolan's five RSS days for 2026-W43,
Musikugglan's five HTML lunches for week44 with no source year. They are already embedded in the
parsers and retain their historical labels. No original file was newly downloaded, no new real-menu
text invented and no fixture is exposed as current production content. RSS GUID dates/ISO year,
entities, multiple and identical alternatives, missing days, XML/DTD rejection, HTML structure
changes and unknown HTML year are exercised by the focused existing tests. Both schools can render
independently; the two historical source weeks are never relabelled as the same current week.

### Access evidence and exact remaining confirmations

No external school-resource, robots, API or terms request was made in this task. Reuse the dated
[source-access findings](#åtkomstvillkor--blockerare-före-liveaktivering); those observations are not
claimed to be freshly verified terms. Technical reachability and a manual download are not permission
for repeated automated access. Only the supplied offline examples are used for this verification.

| Source | Remaining confirmation before any external activation |
| --- | --- |
| Rosenfeldtskolan / Skolmat.info | Official published terms or provider/municipality confirmation that the exact facility RSS endpoint may be fetched automatically for a private self-hosted family display, including how this is compatible with the previously observed `Disallow: /`. Confirm permitted frequency and memory-cache/stale-display terms. `ttl=5`, an RSS link or third-party code alone does not establish this. |
| Musikugglan / Tant Grön | Official terms or source-owner confirmation permitting automated reads of `/matsedel/` and family display, frequency and memory-cache/stale-display terms. The previously recorded `/admin` robots restriction does not itself establish permission. Source HTML supplies week only; owner acceptance of explicitly derived year/date is still required, or the source must provide year evidence. |

The proposed request is one on-demand fetch per enabled source at most every six hours in a stable
API process, including errors (normally at most four attempts per source/day). There is no scheduler.
Restart loses memory/cooldown and may cause another attempt, so this is not a persistent daily quota.
Confirm whether retaining a valid menu for up to 24 hours and showing it explicitly stale after a
failed refresh is allowed. Stricter HTTP cache policy still blocks display rather than increasing
request frequency. Neither provider's ongoing-use permission has been established by this task.

Minimum next action: obtain that source-specific written/public evidence, then independent review
and explicit owner authorization for the appropriate source flags and any required API restart.
Approval for one school does not enable the other. No activation, supplier contact or further
investigation is started automatically. Until then this is **not a completed live-menu delivery**.
After authorized activation, verify real headers/content/current week and iPhone/PWA presentation;
Musikugglan's actual publication year cannot be proved by its current HTML format alone.

### Verification and limits

- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --no-restore --filter
  'FullyQualifiedName~SchoolMeal|FullyQualifiedName~MealPlannerApiTests' -v quiet`: **71 PASS**,
  zero failed/skipped. Includes RSS/HTML, timeout for headers/body, six-hour cache/concurrency,
  stale expiry, conditional requests, wrong week, Stockholm/ISO year rollover, invalid responses,
  disabled-source zero-call assertions and unchanged household meals across school API reads.
  External transport is stubbed; test stores are isolated. No Finance/BB-158 tests run.
- From `src/BigBrain.Web`: `npm test -- --run src/meal-planner/SchoolLunch.test.tsx
  src/meal-planner/MealPlanner.test.tsx src/ux-lab/UXLab.test.tsx`: **22 PASS**. School requests are
  mocked; hostile text stays text rather than executable DOM. Existing Meal Planner and offline
  UX Lab regressions pass. No changes to household writes or Shopping List code/API occur.
- `npm run build`: **PASS** (TypeScript/Vite); advisory CSS-plugin timing message only.
- Targeted Prettier and C# whitespace verification, `node scripts/verify-documentation.mjs`,
  `git diff --check` and changed-file scope/link checks: **PASS**. Targeted diff review and
  secret-pattern checks: **PASS**; no standalone Gitleaks binary is installed, so no local
  Gitleaks run is claimed.

No real-source end-to-end success, current dishes, physical iPhone acceptance, new runtime health,
merge or deployment is claimed. No new database, cache policy, scheduler, dependency or migration.
Finance, BB-158, household planning, Shopping List and production configuration remain untouched.


### Skollunch v3 — merge preparation and owner-reported permission, 2026-10-25

**MERGE CANDIDATE — DEFAULT-OFF CODE; AWAITING EXACT-SHA APPROVAL.** The owner now reports that
both Skolmat.info and Tant Grön permit automated fetching approximately every six hours and
transient caching for up to 24 hours. This is **owner-reported provider permission**, not terms,
correspondence or permission independently inspected by Codex. No supplier was contacted and no
external menu or robots resource was requested. The historical access observations above remain
historical; this update does not claim that robots.txt changed or independently resolve its wording.
The previously missing owner confirmation of frequency/cache permission is now recorded for both
sources. It is not production activation authorization or evidence of a successful live fetch.

Merge preparation verified main `485aeda2ec5819ffc7f433b51ac867c55157c19f` and the reviewed input
checkpoint `1899c55d1ec448c91ce6a6f4eb77c0e6f5c0dc37`, tree
`e73c676b3554286d6b8a943bf5c436f7e7cd4019`. Input ancestry was one commit ahead, zero behind.
Only this existing report changes in the preparation commit; all five code/test file blobs remain
identical to that tested checkpoint. The cumulative candidate still changes exactly these six files:

- `docs/reports/features/family/school-meals-preparation-20261023.md`
- `src/BigBrain.Api/MealPlanner/MusikugglanSchoolMeals.cs`
- `src/BigBrain.Web/src/meal-planner/SchoolLunch.tsx`
- `src/BigBrain.Web/src/meal-planner/SchoolLunch.test.tsx`
- `tests/BigBrain.Api.Tests/SchoolMealLiveTests.cs`
- `tests/BigBrain.Api.Tests/SchoolMealsTests.cs`

Reused unchanged-code evidence: **71 backend tests PASS, 22 frontend tests PASS, frontend build
PASS**, plus targeted formatting. No application test rerun is warranted by this documentation-only
delta. Documentation validator, diff/scope, report-anchor and targeted secret checks **PASS**.
GitHub returned zero Actions runs and zero commit statuses for the input candidate. The configured
workflow runs on PRs and pushes to main, not ordinary branch pushes: candidate CI is **SKIP / NOT
RUN**, not PASS or a running PENDING job. Baseline main run
[40123886908](https://github.com/T-bear/BigBrain/actions/runs/40123886908) is completed/success for
exact main SHA above; it does not test v3. GitHub's main-branch response reports protection disabled
and no required status contexts. Applicable checks must be verified again at the actual merge;
this report does not waive any subsequently applied rules.

No known P0/P1 blocks merging the bounded, offline-tested, default-off code. The new documentation
commit changes the candidate SHA: obtain explicit owner approval of its published remote HEAD,
not the previous input SHA. Final commit/tree identities resolve from this preparation commit's Git
metadata. No merge has occurred. All source flags remain false by default; deployment, restart,
source activation, actual HTTP/cache compatibility and physical iPhone/PWA acceptance are separate.
Musikugglan still provides no source year; explicitly derived dates are not source-verified dates.
Finance/BB-158 remain frozen; unrelated untracked work is preserved.

### Skollunch v3 — bounded HTTP diagnostics and status ordering, 2026-10-26

**REVIEW CHECKPOINT — NO MERGE OR DEPLOYMENT.** Branch
`fix/school-lunch-cache-diagnostics`, accepted baseline
`ff17bf46c7882ff3d046b07a037409ca59de3523`. Exact publication SHA/tree resolve from this
section's commit metadata. Scope is source-response diagnostics and HTTP-error classification;
no source request, runtime inspection/change, cache clearing or supplier activation is performed.

Confirmed in baseline code: the cache-policy check precedes HTTP-status handling. A429 with
no-cache/no-store can therefore return `cachePolicy` before its Retry-After is processed.
This is a reproduced code defect, **not evidence of the actual production response headers**.
Owner-reported permission for six-hour fetching/24-hour transient caching above remains distinct
from HTTP cache instructions; this change introduces no override or new permission assumption.

Changes:

- `src/BigBrain.Api/MealPlanner/SchoolMealSourceClient.cs` emits one structured Information event
  `SchoolMealSourceResponse` (event ID1) per received response, using the existing typed client's
  logger and generated LoggerMessage support. No global logging configuration changes. Fields:
  local school ID, UTC check time, numeric HTTP status, normalized Cache-Control, Age seconds,
  Date/Expires UTC and the selected decision reason. Cache-Control projects only fixed flags
  (no-store, no-cache, must/proxy-revalidate, private/public), numeric max-age/s-maxage and an
  extension-presence boolean. Extension names/values and field-name lists are omitted.
  Typed numbers/dates and fixed vocabulary structurally bound each value below512 characters;
  no raw-header truncation is used. Missing/unparseable scalar headers are `missing`/`invalid`.
  Unknown Cache-Control extensions are represented only by the presence flag; this is intentionally
  not a lossless dump. No URLs, query strings, reason phrases, cookies, keys, bodies or exceptions
  enter this event. Transport failures have no response-header event.
- HTTP429 is `unavailable` with the existing Retry-After parsing; other non200/non304 responses
  are `unavailable`. Error bodies are not read. This prevents an error response's cache headers
  from masking HTTP status or suppressing Retry-After. Previously cached menu data follows the
  existing service's HTTP-error/stale policy, not the error body's cache directives.
- HTTP200/304 retain the same ordered rejection conditions: no-store, no-cache, remaining max-age
  below six hours, or Expires below six hours when max-age is absent. Decision reasons are
  `noStore`, `noCache`, `insufficientFreshness`, `expiresTooSoon`, `rateLimited`, `httpError`,
  `cacheAccepted`. The last denotes only passing this header gate, not successful body validation.
  Six-hour cooldown, 24-hour stale boundary, original menu must-revalidate, flags, fixed URLs,
  redirects/cookies restrictions, five-second deadline and size/encoding checks remain unchanged.
- `tests/BigBrain.Api.Tests/SchoolMealSourceDiagnosticsTests.cs` supplies in-memory HTTP responses
  and a capturing logger. Existing `SchoolMealLiveTests.cs` and `MealPlannerApiTests.cs` constructors
  receive NullLogger; their assertions are preserved. No production or supplier traffic is needed.

Verification and remaining evidence:

- Focused backend build/test command: `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --no-restore --filter 'FullyQualifiedName~SchoolMeal|FullyQualifiedName~MealPlannerApiTests'`.
- Build iterations rejected ordinary/inline logging with CA1848/CA1873 and test array literals with
  CA1861; implementation uses generated logging, an explicit enabled-level guard and static test
  constants. No analyzer suppression was added. The first completed test run was92 PASS /1 FAIL:
  the test incorrectly expected malformed Expires to be ignored. .NET maps it to MinValue/expired;
  the existing cache rejection is preserved and the diagnostic labels this sentinel `invalid`.
  This is offline evidence, not proof that either supplier sent malformed Expires.
- Final focused backend build/tests: **PASS, 93/93, zero skipped**. Tests cover cache reasons/boundaries on200/304,
  missing/malformed headers, HTTP errors,429 Retry-After date/delta/invalid/missing, cooldown without
  extra requests, and bounded structured/rendered logs with synthetic sensitive-value canaries.
- No actual supplier headers have been collected. The live triggering condition remains unknown;
  diagnostics must first be independently reviewed, merged and separately approved for deployment.
  Observe the next ordinary permitted on-demand fetch after deployment; do not clear cache or
  trigger an extra fetch. Deployment/process restart itself resets the existing in-memory cooldown,
  so the operator must preserve the supplier interval when planning that separately authorized step.
- Finance/BB-158 and unrelated untracked work remain untouched. No new endpoint, scheduler,
  database, package, UI change or cache-policy relaxation. UI/full repository tests and production
  checks are outside this focused backend change; candidate Actions runs only on PR/main events.

Final documentation validator and `git diff --check`: **PASS**. Targeted .NET formatting verification:
**PASS**. Five-file scope review and targeted credential-pattern/manual diff review: **PASS**;
this is not a full secrets audit (standalone gitleaks is unavailable locally). No UI code changes;
frontend build/tests **SKIP**, application-wide tests **SKIP**. No live cache-policy fix is claimed:
the bounded diagnostics/status correction is ready for independent review only.

### Skollunch reliability — runtime diagnosis and cache expiry, 2026-10-26

**REVIEW CHECKPOINT — partial improvement, live cachePolicy cause still bounded/unknown.**
Branch `fix/school-lunch-cache-expiry`, accepted main baseline
`b23e00d20a24bdeb459949ab335c8b9cd3599bd3`; publication SHA/tree resolve from this section's
commit metadata. No merge, deployment, provider request, production cache clearing or flag change.
Finance/BB-158 and the blocked API-only deployment contract remain untouched.

#### Implemented versus actually running

Main contains v3 via `ff17bf46c7882ff3d046b07a037409ca59de3523` and the diagnostic/status-ordering
merge `4647cd8119edb5b7cd0872639fd79ba26a38b0d3`. Current read-only Docker inspection on
2026-10-26 verified both containers running/healthy with container **and image** revision labels
`ff17bf46c7882ff3d046b07a037409ca59de3523`:

| Component | Observed immutable image ID | Observed start UTC |
| --- | --- | --- |
| API | `sha256:f9b08f2b58664d6d1fb8e2b5d037f3b0e0fe371ecad83f156b2ee8e9257202e7` | 2026-10-25T18:54:49Z |
| Web | `sha256:d651d216d7aa0e95a4c351d3788c444154cbe20c0dfcc5d7fccc5cafffc27679` | 2026-10-25T18:55:02Z |

The API container environment has `MealPlanner__SchoolMeals__Enabled`, `RosenfeldtEnabled` and
`MusikugglanEnabled` all `true`. This verifies configured environment values, not a new effective
configuration endpoint response. No SchoolMeals GET was made: it may initiate a source request.
The bounded read of the last 30,000 API log lines within 48 hours succeeded and found **zero**
`SchoolMealSourceResponse` formatted events. It is not proof of no past traffic. The observed
runtime revision predates that event's implementation, so missing header diagnostics are expected.
No raw logs, private environment values, credentials, container identifiers or source bodies
are published. No other service was inspected or changed for this task.

#### Evidence-based fault separation

| Condition | Evidence and remaining uncertainty |
| --- | --- |
| Disabled source flags | All three container environment flags are true; default-off code remains unchanged. Earlier owner-observed `cachePolicy` also differs from the `offline` result of disabled sources. |
| HTTP error versus cache rejection | Deployed v3 checks cache headers before HTTP status. Thus a 429/other error with restrictive cache headers can misleadingly become `cachePolicy`. Main already corrects this; actual production status/headers remain unavailable. |
| Intentional cache gate | For successful 200/304, no-store, no-cache, insufficient remaining max-age or early Expires intentionally reject content. No override is introduced. Owner-reported six-hour fetching/24-hour cache permission is not evidence of the actual HTTP headers or an override of them. |
| Parsing and source week | In deployed v3, a `cachePolicy` return happens before parsing. It cannot establish whether that response contained valid XML/HTML or current-week dishes. Existing fixtures verify parser behavior only. Current source week/content and holiday availability are **UNKNOWN**. |
| Freshness/revalidation code defects | The two independent defects below reproduce offline for both schools. They are not asserted to explain the owner's observed production `cachePolicy`. |

The existing structured event on main remains the required evidence at the next ordinary,
permitted source attempt after separately authorized safe deployment. It distinguishes HTTP
status, normalized cache fields and rejection reason without exposing sensitive headers. Until
then, neither provider is blamed and no speculative cache-policy relaxation is made.

#### Bounded correction and preserved contracts

1. `SchoolMealService` previously hid a failed menu after 24 hours but retained its content and
   ETag/Last-Modified indefinitely until replacement. After a long Retry-After it could send those
   expired validators and revive the retained menu with 304. The corrected service evicts the
   menu, validators, validation timestamp and stale permission **on the next read** after the
   existing inclusive 24-hour limit. It never changes `NextAttempt` or `CheckedAt` during eviction.
   A retained entry may physically remain in idle process memory until accessed; no background
   timer, scheduler, persistence or claim of secure memory erasure is introduced.
2. A 304 previously marked an existing menu as freshly validated even if neither ETag nor
   Last-Modified was available/sent. It now requires a cached menu and at least one request
   validator. Otherwise source status is `invalid`; an eligible old menu remains explicitly stale
   within the existing limit, never revalidated by that unsolicited response. An expired menu
   cannot reappear. Ordinary conditional304 still preserves original fetch time and advances
   successful validation time as before.

A small Entry.ClearMenu method reuses the same clearing semantics for expiry, week rollover
and cachePolicy. Both schools keep independent entries/semaphores. No HTTP client/header rules,
parser, endpoint/DTO, Web code, configuration defaults or provider permissions change. Six-hour
attempt spacing, longer Retry-After, must-revalidate, no-store/no-cache and ISO-week guards remain.
There is no household-store/Shopping-List dependency or write path in the changed service.

#### Verification

- Before correction, the four new cases (two regressions × two schools) **FAILED as expected**
  against accepted main code. This demonstrates code defects, not live-source behavior.
- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --no-restore --filter
  'FullyQualifiedName~SchoolMeal|FullyQualifiedName~MealPlannerApiTests' -v quiet`:
  **97 PASS, zero failed/skipped**, including build/analyzers. In-memory HTTP handlers only;
  local API tests use temporary stores and verify household meals unchanged. No Finance tests,
  model runs, live source calls or production database access were performed.
- New tests cover both verified historical source formats, conditional validators, 24-hour boundary,
  48-hour Retry-After, no premature fetch, no expired-content revival and honest stale/invalid state.
  Existing tests cover source isolation, flag combinations, dates/ISO rollover, RSS/HTML parsing,
  headers/status diagnostics, timeouts, cache restrictions, missing menus and fixture gating.
- From `src/BigBrain.Web`, `npm test -- --run src/meal-planner/SchoolLunch.test.tsx
  src/meal-planner/MealPlanner.test.tsx src/ux-lab/UXLab.test.tsx`: **22 PASS** with mocked API;
  `npm run build`: **PASS**. Existing tests verify separate schools, original dish order/duplicates,
  safe external-text rendering, missing/stale states and explicit historical fixtures. No live
  browser or physical iPhone/PWA acceptance is claimed.
- Documentation validator, `git diff --check`, three-file scope and targeted secrets review:
  **PASS**. Full application suites and production/provider verification: **NOT RUN**.

#### Minimum next step toward actual meals

Independent review of this checkpoint; any merge requires exact-SHA approval. Main's already
accepted diagnostics still need a separately authorized, verified deployment path. API-only
remains blocked in the approved wrapper; this task neither enables that contract nor substitutes
the API+Web worker. Establish a safe provider-interval window (restart loses process-local cooldown),
backup/runtime/recovery gates and a reviewed narrow execution path before any later deployment.
Do not clear caches, restart or force a source read merely to collect headers.

Then observe `SchoolMealSourceResponse` at the next ordinary permitted request for **each** school.
If headers allow current policy, verify parser/week results and actual dishes through API/Family
View, followed by owner iPhone acceptance. If headers remain incompatible, seek a provider-backed
compatible feed/cache instruction or a separately reviewed solution; do not bypass restrictions.
Current supplier responses and the production cachePolicy cause remain the concrete evidence gap.
This checkpoint does **not** claim that current meals are now working in production.


### 2026-10-28 — durable source admission and bounded deployment correction

Scope authorized by the owner: finish SchoolLunch using existing Meal Planner storage and the
existing API+Web contract, without a six-hour pause of family use. Baseline
`65ed713649f7d47ec5787efde6bf15745b72c0f0`; branch `fix/school-lunch-durable-gate`.
This section supersedes the older process-memory-only/maintenance assumptions for the proposed
change; historical test and deployment reports above remain historical.

Implementation: two per-school rows in the existing SQLite database, atomic reservation before
HTTP, completion-bound six-hour minimum plus longer Retry-After, and retained pending state on
unseen response or failed persistence. A short separate connection/transaction path never holds
SQLite locks across HTTP or marks the household store unavailable. Existing household tables and
`user_version=2` remain unchanged. Menu content/validators are not persisted. Unknown initial
history performs no fetch; optional verified initial bounds can seed missing rows only. There
are no supplied production bounds, automatic pending-row reset or fabricated timestamps.

HTTP status/cache diagnostics remain sanitized. Retry-After is now retained for received statuses
including 503 and body failures. Invalid Retry-After and missing response are distinguished from
verified absence. Cache prohibitions still clear retained menus even when gate completion fails.
Family View uses the requested simple error text; independent school rendering and stale labeling
remain. Public endpoint/DTO, source URLs, parsers, default OFF flags and household mutation routes
are unchanged. No supplier request was made during development.

The existing worker/gateway contract replaces the long source window with administrator-bound
review evidence for exact durable-guard API code/image. Normal identity, backup, short graceful
stop, health, household-data and non-target checks remain. Before replacement only, legacy API
recovery uses its verified old image/configuration with school flags OFF; ambiguous replacement
is never retried or followed by a DB restore. See the
[updated existing runbook](../../../operations/runbooks/approved-operations.md#supplier-interval-and-maintenance-gate).
No sudoers, installed files, ACLs or production configuration have been changed.

Risk: persistent state/schema is level 2; privileged worker/contract is level 3 and requires
independent exact-candidate review. The database-deployment backup gate remains BLOCKED for
autonomous deployment without separate review: the owner's verified six-database backup is not
proof of a daily backup schedule. Reuse that receipt only while its runtime binding and expiry
remain valid; no new backup was created here.

Read-only production observation during this task: API and Web remain on
`ff17bf46c7882ff3d046b07a037409ca59de3523`, healthy, all three school flags true, 12 containers
running. Installed gateway status: `NO_WRAPPER_RUN`. It still needs the reviewed contract update
and a separately sealed package approval; only the existing four operation commands have
NOPASSWD authorization. No administrative approvals were self-issued.

Remaining real-data limits: current provider headers/menus are not established by offline tests.
Unknown old source history remains blocked per school after first deployment; six elapsed hours
alone cannot prove an unknown longer Retry-After expired. Independently established initial
bounds are required for first source admission. Uncertain pending outcomes and restored databases
need verified recovery; changing bootstrap configuration cannot clear a pending row. iPhone/PWA
acceptance remains the owner's manual test, not a CI result.


Verification of this change (offline, no production mutation):

- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --no-restore --filter
  'FullyQualifiedName~SchoolMeal|FullyQualifiedName~MealPlanner'`: **113 PASS** including RSS/HTML,
  API/household regression, restart, atomic cross-instance reservation, 503/429 Retry-After,
  crash/unknown responses, independent schools, schema errors, failed completion and cache
  prohibition under persistence failure. Synthetic databases/history only.
- `python3 -m unittest discover -s tests/deployment -p 'test_*approved*.py'`: **75 PASS**,
  including exact rollback environment with original flags ON, sources-OFF legacy recovery,
  ambiguous recovery no retry, removed client-pause approval rejection and existing privilege tests.
- Focused SchoolLunch/MealPlanner/UXLab frontend tests: **22 PASS**; frontend build **PASS**.
- Scoped .NET formatting verification, changed frontend formatting, documentation validator
  and `git diff --check`: **PASS**. Required GitHub CI and exact-candidate independent review
  are recorded on the PR/commit, not inferred from local tests.
- Review caught a cache-prohibition/persistence-failure interaction before publication; corrected
  with four regression cases. No live failure recovery, source initialization, root installation,
  deployment or manual acceptance is claimed.

## API image configuration permissions and mixed-runtime recovery — 2026-10-28

Scope: permanent image-only correction after failed run
`33d39b9d84da4f71a34dcd13ef3d0c81`; baseline
`3cf409ea410e1fdbec2cdc9cc451d486b3a245e4`, branch `fix/api-image-config-permissions`.
Risk level 2 (configuration access), HIGH verification with independent candidate review.
No privilege increase, database/schema change, gateway change or production action is included.

Confirmed cause: the immutable API image contains `/app/appsettings.json` as root:root 0600,
unreadable by its actual UID/GID 1654. A disposable container reproduced that failure. The
administrator's current repair changes ownership only in the running container's writable layer;
it is not a permanent image fix. The Dockerfile now sets only published top-level
`appsettings*.json`, `*.runtimeconfig.json` and `*.deps.json` to root:1654 0440.
The application can read, cannot write, and other users receive no access. No recursive ownership
change, runtime chmod, database-volume permission change or new dependency is introduced.

Focused image regression:

- Built the actual API Dockerfile from a clean baseline archive plus this Dockerfile correction,
  with the source `src/BigBrain.Api/appsettings.json` deliberately set to **0600** before build.
  `docker build --file <context>/src/BigBrain.Api/Dockerfile --build-arg BIGBRAIN_REVISION=UNKNOWN
  --iidfile <context>/api.iid <context>`: **PASS**. The UNKNOWN label honestly identifies a local
  uncommitted verification image, not an accepted production release.
- Verification image: `sha256:164a50f27546f8a09127ceecc6357cdda0dd638c39c2b8ea7f564901465f00d0`.
- `python3 -B tests/deployment/verify_api_image.py <immutable-image-id>`: **PASS**. Actual image
  USER 1654, all five matching JSON files readable/root-owned/group-readable 0440, no application
  write permission, normal entrypoint startup and `/health` HTTP 200, zero restarts.
- Test containers have no external network, published ports, host/named-volume mounts or production
  configuration. Data lives only in disposable tmpfs. School sources and Finance scheduling/local
  reasoner remain disabled; no grants or Finance runs. Only newly created test containers are removed.
- The same permission regression rejects the original immutable API image, as expected;
  separate read check confirms root:root 0600 and unreadability. This expected failure is not a
  passing old image. Application source is unchanged, so previously valid application tests are
  reused; mandatory PR/main CI is reported separately on the exact GitHub revisions.
- Python syntax, documentation validator, `git diff --check` and scoped diff/secrets review: **PASS**.

Read-only production observation during this correction:

| Component | Actual revision / image | Observation |
| --- | --- | --- |
| API | `3cf409ea410e1fdbec2cdc9cc451d486b3a245e4` / `sha256:de4973f2720a24b691c64a0c46a71f976e58b01df6c6f6a7054dd3472b67266c` | running/healthy, `/health` 200; repaired file UID/GID 1654 and mode 0600; 168 recorded restarts from the failed start/recovery period. This does not prove the repair survives recreation. |
| Web | `ff17bf46c7882ff3d046b07a037409ca59de3523` / `sha256:d651d216d7aa0e95a4c351d3788c444154cbe20c0dfcc5d7fccc5cafffc27679` | running/healthy, `/health` 200; still the old revision. |

Gateway status remains **MANUAL_REVIEW_REQUIRED**. Selected mount mappings and all six database
inode/device identities match the previous package; other containers retain their images/start
identities and run normally. These identity checks are not a new database integrity test. No
production database contents, Finance data or provider endpoints were read. No container was
restarted, recreated or modified. Protected receipts/claims were not accessible for independent
inspection by this agent; the administrator-reported repair and gateway status do not replace
that required inspection.

### Controlled continuation from the mixed runtime

1. Preserve the failed run, claims, receipts and interlock. Under the
   [existing recovery procedure](../../../operations/runbooks/approved-operations.md#failure-concurrency-and-rollback),
   an administrator must inspect terminal systemd state, worker/recovery receipts and any unfinished
   Docker command before retiring the interlock. Never reuse its consumed ID/approval or rerun the
   old package. No automated interlock reset or database restore is permitted.
2. After reviewed merge/CI, build the final corrected API image from the accepted revision and
   run the same image check. Retain the existing Web artifact only if its accepted revision and
   content remain valid. Capture the **current mixed runtime**, actual configuration/mounts and
   current databases for the next plan; the original all-ff17 preflight is obsolete.
3. Existing backup bytes remain preserved, but their original container/image binding no longer
   matches the replaced API. The current gateway's equality check cannot accept them as current
   merely because volume/inode identities match. Obtain an approved, verified six-database backup
   bound to the current runtime through the existing routine before sealing a new package. This
   task creates no backup or new deployment package. The global daily-backup gate remains BLOCKED.
4. Verify recovery against usable immutable images: the current API image is still defective;
   its writable-layer repair is not captured by its image ID. Do not designate it as a working
   recreation/rollback image. The prior reviewed ff17 image is a possible recovery input only
   after explicit verification against current data/configuration and school-source OFF safeguards;
   it is not approved for blind rollback here. If the existing worker cannot represent the chosen
   safe recovery, stop that deployment rather than weakening its checks.
5. Only after those conditions, seal a **new unique run/package and separate administrator
   approval** under the existing API+Web contract. Deploy API then Web only through the separately
   approved procedure; verify health, accepted identities, preserved data/other services and final
   SchoolLunch status. Unknown source histories and Retry-After remain unknown; no initial bounds
   are fabricated. Actual menus and iPhone/PWA acceptance remain unverified.

This correction is an image fix and recovery handoff, not a claim of completed production delivery.
