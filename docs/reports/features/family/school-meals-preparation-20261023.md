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

### Skollunch v3 — authorized deployment stopped before mutation, 2026-10-25

**BLOCKED — v3 NOT DEPLOYED; both sources OFF.** Owner explicitly authorized deployment and
activation of both schools for main `ff17bf46c7882ff3d046b07a037409ca59de3523`. Fetch verified that
exact remote main. [Main CI run 40199482649](https://github.com/T-bear/BigBrain/actions/runs/40199482649)
finished with Backend, Frontend, Documentation and Secrets all completed/success for that SHA.
Provider permission remains owner-reported as described above; no additional policy approval is
being requested and no supplier HTTP request was made in this run.

Read-only Docker inspection and retained v2 receipts now independently confirm v2 production
revision `485aeda2ec5819ffc7f433b51ac867c55157c19f`, rather than merely the earlier owner report:

| Component | Actual running image | Health |
| --- | --- | --- |
| API | `sha256:b0bb52d525770660d059dd15f97a11d24eeafb667da14360637507b7e1c509aa` | running / healthy, zero restarts |
| Web | `sha256:a77aa1c05a9dc63a402ca9639689a971d26dc3a9a804587ae1f930ffc434416f` | running / healthy, zero restarts |

The existing v2 package has `complete.json: COMPLETED` at that revision, `schoolSources: OFF`,
matching API/Web image receipts and `recovery.json: COMPLETE_NO_ACTION`. All twelve containers
are running; containers without configured health checks are not asserted healthy. API retains
five selected writable named data volumes; Web has no data mounts. No configuration was printed
or altered. Direct API and Web-proxied school endpoints each returned HTTP 200 with `offline`
for both schools, ISO 2026-W43. Both `/health` endpoints returned HTTP 200. The three
`MealPlanner__SchoolMeals__Enabled`, `MealPlanner__SchoolMeals__RosenfeldtEnabled` and
`MealPlanner__SchoolMeals__MusikugglanEnabled` environment overrides are absent; accepted code
defaults them to false and the actual offline responses corroborate disabled fetching.

**Concrete execution blockers:** noninteractive `sudo -n -l /usr/bin/python3` and
`sudo -n -l /usr/bin/systemd-run` both require a password in this agent session. Directory reads
for all five selected Docker-volume host sources fail with PermissionError. Thus a current
six-database online backup cannot be created through the authorized host method, and the existing
system-manager deployment/recovery mechanism cannot be submitted here. Local operator sudo
success does not grant this session access. No Docker privilege workaround was attempted.
Available filesystem capacity was approximately 389 GiB; capacity is not the blocker.

The [existing operational checkpoint](https://github.com/T-bear/BigBrain/tree/2cc6bbe2611a9a67d4dad35b66a898ec2d9c00c8)
contains the previously tested backup and API/Web-only deployment procedures. Its backup script
pins the pre-v2 API image `sha256:8b92e5ea802f215427456e670875df07760e2eff5694bd4ef24d6ad6ed32aa5c`;
executing it unchanged against current v2 would correctly stop with RuntimeDrift. The completed
v2 deployment package likewise binds old identities and an already-used run ID: do not edit or
rerun it. The older operator-verified backup remains historical evidence, not a newly verified
snapshot of current data or current container identity. No existing backup was altered.

**Minimum continuation:** prepare current-runtime-pinned inputs using those same bounded
procedures, locally authenticate the operator for the six-database online backup, and verify
its new manifest. Build exact approved v3 images while production runs; bind a fresh API/Web-only
systemd run to current configuration/mounts, the three explicitly authorized flag changes and
phase-aware recovery. Recheck the maintenance/no-in-flight-write gate before replacement.
The earlier client-pause condition cannot be inferred from healthy containers or old Media job
labels. No executable v3 launch command is claimed ready here: old commands fail their identity
checks and must not be suggested as a working shortcut. Existing owner deployment/activation
authorization remains valid within scope; administrative execution is the missing prerequisite.

No build, new backup, container stop/replacement, activation, rollback, database operation or
Finance/BB-158 action occurred. Actual supplier response/cache compatibility, live dishes,
v3 Family View rendering and physical iPhone acceptance remain NOT RUN. Do not report LIVE_READY.
Only this existing report is updated; no application tests are repeated after the successful
main CI. Documentation validator and diff/scope checks are recorded with the publication result.

### Skollunch v3 — current-runtime backup and built deployment inputs, 2026-10-25

**REVIEW CHECKPOINT — BLOCKED awaiting local backup execution; production remains v2/OFF.**
Continues the preceding permission checkpoint on `ops/school-lunch-v3-deployment` with the same
approved main `ff17bf46c7882ff3d046b07a037409ca59de3523`. Deployment and both-source activation
remain expressly authorized; this is an administrative execution boundary, not a request for
another policy decision. Unrelated untracked files and all older packages/backups are preserved.

[Backup script](../../../../scripts/backup-school-lunch-api.py) and
[offline tests](../../../../tests/deployment/test_school_lunch_backup.py) reuse the reviewed
`2cc6bbe2611a9a67d4dad35b66a898ec2d9c00c8` operational implementation. The image pin is updated
from pre-v2 to the actual v2 API image. A required SHA-256-bound private runtime plan now also
pins the initial container ID, revision and the five selected volume mappings before any copying,
and verifies them again afterward. The manifest records those selected identities. This avoids
accepting a different volume/container merely because it remained stable during one backup.
SQLite online backup, read-only source connections, WAL handling, six-file allowlist, exclusions,
permissions, deadlines, integrity/FK/schema/hash checks and isolated reopen remain unchanged.
A new unique output directory is created for each attempt; failures retain INCOMPLETE artifacts.
Failures before output creation report BLOCKED without claiming any backup. No production backup
has been executed by Codex. Earlier 06:13/06:21 sets are not current backup evidence.

**Offline verification: 28/28 PASS** with
`python3 -B -W error::ResourceWarning tests/deployment/test_school_lunch_backup.py`.
Includes all 24 previous synthetic/WAL cases and four runtime-plan regressions: reordered mounts
accepted, initial container/volume drift rejected, changed revision rejected and plan tampering
rejected. Tests prohibit Docker calls and use synthetic databases only. The private runtime plan
also matches a fresh Docker metadata inspection; this comparison reads no production DB contents.

**Built inputs, not deployed:** a new private directory/run ID
`data/school-lunch-v3-dee40eebf77147579f90a0e1612c7e1d` retains build context, bounded independent
build process/log/exit, image IDs, private before-container metadata, backup plan and prospective
release/rollback Compose files. Directory mode 0700; private inputs 0600. No private configuration,
credentials or source-volume paths are committed. All 714 source files match `git archive` of the
approved revision byte for byte. Both real Docker builds exited 0, with per-image 1,800-second
limits and persisted final status. Main CI already verified above is reused; no full app rerun.

| Role | New v3 candidate image (not running) |
| --- | --- |
| API | `sha256:f9b08f2b58664d6d1fb8e2b5d037f3b0e0fe371ecad83f156b2ee8e9257202e7` |
| Web | `sha256:d651d216d7aa0e95a4c351d3788c444154cbe20c0dfcc5d7fccc5cafffc27679` |

Both image labels identify approved `ff17bf46c7882ff3d046b07a037409ca59de3523`. Current v2 images
listed above remain available as recovery inputs. Prospective Compose rendering PASS: exactly API
and Web, external existing volumes/networks, no build/dependency startup, complete effective
environments preserved except the three authorized API flags set true. Only image/revision labels
and those flags differ from the retained v2 recovery configuration. Entrypoint/user/command/workdir
are compatible with the existing runtime. No Compose up/stop/start was invoked.

An initial ad-hoc unchanged-container comparison rejected mount-list ordering; targeted comparison
confirmed identical mount sets. A separate preparation assertion treated a null Compose entrypoint
as an explicit override; inspecting actual values confirmed both images use the same entrypoint.
Using the established order-independent mount comparison and null/default semantics resolves both
check errors without runtime changes. Final verification: all twelve original containers retain
IDs, images, configurations, mount sets and start times. No actual drift was observed.

**Exact next operator step — backup only:** run this block locally from the server terminal.
It checks the retained script checksum, requests sudo locally and executes the six-database online
backup with the reviewed runtime-plan digest. It does not deploy, restart, activate or restore.

```sh
cd /home/enigma/BigBrain/apps/bigbrain && printf '%s\n' '88ccb8ec063d0bcaad6adabefc2e2e4250bd1f7bf28e7e7c3891143d0807b1da  data/school-lunch-v3-dee40eebf77147579f90a0e1612c7e1d/backup.py' | sha256sum -c - && sudo /usr/bin/python3 -I data/school-lunch-v3-dee40eebf77147579f90a0e1612c7e1d/backup.py --runtime-plan /home/enigma/BigBrain/apps/bigbrain/data/school-lunch-v3-dee40eebf77147579f90a0e1612c7e1d/backup-runtime.json 04115e6031b2df6334690bbe15f44282a5f5fa17f053b4995931662154ca2625
```

Expected: checksum OK, `PASS: 6/6 databases; integrity, foreign keys, schema, SHA-256 and isolated
readback PASS`, a new backup directory/manifest and manifest SHA-256. Return those summary lines;
never paste database contents or the private manifest. On any checksum error, BLOCKED, INCOMPLETE,
interruption or missing PASS: stop, preserve results and do not launch deployment. This command
was syntax/input-hash checked; its privileged production execution is deliberately NOT RUN here.
Backup consistency is per database, not an atomic six-database transaction; isolated reopen is
not a production restore drill. Retained backups can later be reverified with the script's
`--verify` mode without reading production databases.

**Remaining deployment gate:** `preparation.json` explicitly says `INPUTS_PREPARED_NOT_LAUNCHABLE`.
There is no queued deployment or executable v3 launch command. Bind the fresh verified backup
manifest, adapt/test the existing systemd worker's expected v3 environment/live-source checks and
phase-aware recovery to these inputs, then check paused clients/no-in-flight writes immediately
before replacement. Old v2 runner assumptions (single-service original Compose, flags OFF and old
backup identity) must not be reused unchanged. Verify recovery at the relevant phase; no blind
image downgrade, database restore or rerun after ambiguous replacement. The prepared rollback
Compose is configuration evidence, not authorization to execute it without those checks.
A supplier error must remain visible and cannot become fabricated menu data or automatic retries
outside the existing six-hour policy. Live HTTP/cache compatibility, v3 UI and owner iPhone/PWA
acceptance remain NOT RUN. No supplier request, Finance/BB-158 operation or production mutation
occurred. Documentation validator, diff/scope and targeted secret review PASS; no merge.

### Skollunch v3 — sealed operator deployment, 2026-10-25

**BLOCKED on local privileged execution; package PREPARED, production NOT DEPLOYED.** Owner
reports new backup `20261025T182326Z-9479fced116f4ab58ba97219372faaa1`, six databases PASS for
integrity, foreign keys, schema, SHA-256 and isolated readback; manifest SHA-256
`09ccfdd17da297d307f8a76b0af69a2dfafaf16c77fcf56ce1eab167f8aec209`. This is operator evidence.
Codex independently attempted to read the manifest and received PermissionError; noninteractive
sudo for Python/systemd-run also requires authentication. Therefore privileged re-verification
is NOT claimed complete. The launch command below performs it as a mandatory pre-stop gate.
No repeat backup or production restore is needed or performed.

The existing [bounded worker](../../../../scripts/deploy-school-lunch-v3.py) is adapted from
v2's reviewed systemd/Compose mechanism, with a narrow
[backup binding helper](../../../../scripts/verify-school-lunch-v3-backup.py) and
[37 offline fault tests](../../../../tests/deployment/test_school_lunch_v3_deployment.py).
It uses the already built v3 images above; no rebuild or application-code change. Main remains
`ff17bf46c7882ff3d046b07a037409ca59de3523`; successful main CI is reused. Only approved images,
revision labels and the three SchoolMeals flags change. All other API/Web environment, mounts,
HostConfig, user, command and network membership remain bound to the current v2 configuration.
Original private Compose files and old v2 images are preserved.

**Sealed package:** `data/school-lunch-v3-dee40eebf77147579f90a0e1612c7e1d`, SHA-256 of
`package.json`: `866f13c28e2de9e295aa45bdbb955aa1096a9c8f695ed9e4d9068eeb1f5b264c`.
Seven hashed inputs cover worker, plan, release/rollback Compose, existing backup verifier,
initial runtime plan and privileged binding helper. `ready.json: PREPARED_AWAITING_OPERATOR`
supersedes the earlier preparation-only record. No queued/state or deployment completion exists.
Runtime preflight PASS: candidates/revision, original configurations, five volume mappings,
six database file identities (stat only), read-only household response hash, URL-import safety
states and other-service fingerprints. Three socket samples found no inbound API connections;
this is not proof of absence of background transactions or future clients. The operator must
pause users/integrations for the window. Healthy services or old job labels do not satisfy that
assertion. All twelve production IDs/images/configurations/mount sets/start times remain unchanged.

**Local start — existing deployment/activation approval applies.** pause all users/API writers,
then run this command as the normal Docker operator, not a root shell. It requests sudo locally,
never reads/logs a password, and rechecks backup and runtime before creating the unit.

```sh
cd /home/enigma/BigBrain/apps/bigbrain && /usr/bin/python3 -I data/school-lunch-v3-dee40eebf77147579f90a0e1612c7e1d/runner.py launch data/school-lunch-v3-dee40eebf77147579f90a0e1612c7e1d 866f13c28e2de9e295aa45bdbb955aa1096a9c8f695ed9e4d9068eeb1f5b264c --users-paused
```

The privileged helper verifies the exact manifest/hash/method, calls the existing retained-backup
routine (all six integrity/FK/schema/hash/reopen checks), and compares its runtime to both current
Docker and the pinned five-volume/container plan. It reads backup data, not production database
rows. Only sanitized PASS/hash/runtime-match JSON returns to the launcher. Failure prevents unit
submission. The launcher rechecks preflight and quiescence after authentication/backup; the worker
requires the backup receipt and repeats runtime/activity gates immediately before stopping API.

Unit `school-lunch-v3-dee40eebf77147579f90a0e1612c7e1d.service` is system-manager-owned, runs under
the operator UID/GID, has finite start/recovery deadlines (480/180 seconds), KillMode control-group,
Restart=no, UMask 0077 and ExecStopPost. It uses the existing shared deployment lock and requires
matching systemd invocation/cgroup. Write-ahead phase receipts precede every stop/replacement.
API then Web are stopped gracefully (no forced SIGKILL), replaced with scoped
`up -d --no-deps --no-build --pull never`, and checked for health, exact configuration, unchanged
DB identities/household response, real school endpoint status and new Web artifact. No broad stack
operation or database restore occurs. External requests use the accepted API adapter/cache only.
Results distinguish published menus from unavailable/invalid/cachePolicy/notPublished responses;
source failure is recorded rather than fabricated as successful menu delivery. `COMPLETED` with
`schoolSources: ON` means component deployment, not automatically successful dishes or owner UX.

**Results:** `backup-verified.json`, `queued.json`, `state.json`, `api-deployed.json`,
`web-deployed.json`, `complete.json`, `failure.json`, `recovery.json` are retained. `QUEUED` alone
is never success. Inspect the unit read-only:

```sh
systemctl show school-lunch-v3-dee40eebf77147579f90a0e1612c7e1d.service -p ActiveState -p SubState -p Result -p ExecMainStatus
```

On BLOCKED/error/ambiguous queue: stop and preserve results; do not rerun the launch or change
package hashes. On successful deployment, inspect sourceResults for each school through API and
Web and perform the owner's iPhone/PWA check. Actual live responses and effective stale/cache
behavior remain NOT RUN until this procedure executes. Offline tests are not live evidence.

**Recovery:** before possible replacement, automatic recovery restarts only the exact old container
with unchanged image/configuration and clean exit, after resolving an outstanding stop. After
possible replacement, unknown/unhealthy/data-drift conditions require manual review and no restore.
A separate explicit `launch-rollback` action is prepared for known, healthy, fully identifiable
v2/v3 configurations only. It rechecks retained backup integrity without falsely asserting the old
container ID still exists, current DB file identities, household data hash, import/connection gates
and other services. Its distinct `-rollback` unit/receipts then gracefully replaces API followed by
Web with the pinned v2 images and original flags OFF, verifying old Web content and unchanged data.
It is never invoked automatically or blindly after an ambiguous Docker operation. Do not use it
when its preflight fails. Interrupted rollback before replacement may restart only its exact clean
stopped target; after replacement uncertainty it records manual review. No branch of recovery
writes/restores database files. Package/run IDs cannot be requeued after an attempted launch.

**Verification:** `python3 -B -W error::ResourceWarning
 tests/deployment/test_school_lunch_v3_deployment.py`: **37 PASS** (Docker/systemd/HTTP mocked).
Includes original 19 deployment/recovery cases plus flags/config protection, fixture/offline
rejection, source errors, backup tamper/runtime/integrity failure, backup receipt requirement,
post-auth activity recheck, explicit rollback ordering/scoping, separate rollback unit/receipts and
rollback interruption. Initial test adaptation overmatched a mocked sudo command; narrowed matching
restored the failed-submission regression and all tests passed. Prior 28 backup tests remain valid
because backup.py is unchanged. Real read-only package/Compose/runtime preflight PASS; no privileged
helper or actual systemd execution is claimed. Documentation/diff/scope/targeted secret checks PASS.
Only operational scripts/tests and this report changed; no merge, activation or production mutation.
