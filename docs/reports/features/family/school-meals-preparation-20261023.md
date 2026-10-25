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

## Deployment preflight — stopped before mutation, 2026-10-24

**BLOCKED / NOT DEPLOYED.** The owner authorized API/Web deployment with all school sources
remaining disabled. Accepted target main is `485aeda2ec5819ffc7f433b51ac867c55157c19f`, containing
implementation merge `c8aaba15ac6382b6934591acfa6f615505c95819`; no source/Compose/infrastructure
changes occurred between that implementation merge and target main. [CI 40123886908](https://github.com/T-bear/BigBrain/actions/runs/40123886908)
was verified completed/success for exactly target main: backend, frontend, documentation, secrets.

Read-only Docker inspection found API and Web running and healthy, both labelled revision
`712679fda646aced2a417adf696e59d4a7c0f57b`. API image:
`sha256:8b92e5ea802f215427456e670875df07760e2eff5694bd4ef24d6ad6ed32aa5c`;
Web image: `sha256:195f2bf3792c23a05f44c4ad1b15feea598dfec93490d2fbcf2b8e0e111f0ad6`.
These are pre-existing images, not a new deployment. The running API predates Skollunch v1/v2.
No SchoolMeals settings were found in its container environment, deployed appsettings.json or
private API/Web Compose environment overrides. The accepted v2 defaults for Enabled,
RosenfeldtEnabled and MusikugglanEnabled are all false; no flags were changed and no source called.
This is not a post-deployment endpoint or effective-options verification.

The documented session-independent system-manager execution/recovery prerequisite could not be
established: `sudo -n -v` failed because local password authentication is required. The host's
`bigbrain.service` was also listed failed; its cause and suitability for recovery were not
investigated or asserted. Healthy Docker containers do not prove that this lifecycle unit can
recover a replacement. No old BB-158 deployment package or run ID was executed or modified.
Fresh backup validity, active-import safety and a concrete bounded API/Web recovery execution
remain **UNVERIFIED**; historical copies/tests do not establish them for a new run.

STOP before build, backup mutation, stop, container replacement or configuration change. No
Finance/BB-158 work, database operation, provider request, rollback or application test occurred.
Next operational step: establish locally authorized system-manager execution and verify the
existing procedure's bounded recovery, fresh backups and activity gates for a new API/Web-only
run. Do not request credentials in chat, reuse old launch packages, start the whole stack or
infer automatic database/image rollback permission. Existing deployment authorization remains
limited to the same scope; no new implementation or broader service action is authorized.
After a safe deployment, verify effective false flags/disabled responses and API/Web health;
owner iPhone/PWA acceptance of the unavailable-source presentation remains pending.

### Follow-up preflight — operator authentication boundary, 2026-10-24

Owner reports that the lifecycle-unit failure dates from 2026-09-04 and was caused by
Flaresolverr being unhealthy then. Treat it as a separate historical fault, not evidence that
API/Web deployment requires restarting or repairing that unit. No unit action was performed.
Main remains `485aeda2ec5819ffc7f433b51ac867c55157c19f`; its previously verified four-job CI
success is reused. Read-only reinspection verified all 12 containers running, API/Web healthy
on the exact images/revision recorded above, both old images locally available, no SchoolMeals
environment overrides, and approximately 388.5 GiB available on the workspace filesystem.
This capacity observation does not verify backup size, completeness, integrity or restore safety.

First blocking prerequisite: `sudo -n -l /usr/bin/systemd-run` returned exit 1, password required.
The owner's successful local `sudo -v` does not confer authorization in the agent session.
The exact bounded local operator check is `sudo -l /usr/bin/systemd-run`, with authentication
only in the owner's terminal. It lists permission; it does not launch deployment, grant Codex
passwordless sudo, verify ExecStopPost recovery or satisfy the backup/activity gates. Do not
send passwords, change sudoers, rerun an old launch package or start the full Compose stack.
A future privileged launch must identify the freshly verified API/Web-only procedure and its
inputs; no executable production launch command can honestly be supplied at this stop point.
STOP before build or mutation. Backup usability, current import/database activity and concrete
new-run recovery remain UNVERIFIED, not failed or waived. No school-provider requests, flag
changes, container changes, application tests or Finance/BB-158 actions occurred.

### Read-only deployment gate completion — 2026-10-25

**BLOCKED before mutation.** Main remains `485aeda2ec5819ffc7f433b51ac867c55157c19f`;
reuse its previously verified CI run 40123886908 (Backend, Frontend, Documentation and Secrets
success). No build, deployment, restart, restore, source request or configuration change occurred.
The following observations replace no historical evidence and authorize no operational action.

| Requirement | Result and evidence |
| --- | --- |
| Runtime identity and health | PASS: all 12 production containers running; API/Web healthy, revision `712679fda646aced2a417adf696e59d4a7c0f57b`, exact image IDs recorded above unchanged. Other stopped/created local test containers were not treated as production services or modified. |
| Historical Media backup integrity | PASS for the existing 2026-10-20 17:17 UTC capture only: SHA-256 `88eb418c8905ebb2ea3309f2f05f91ad23b914aefcac09e78e259a87318aa5d3` matches capture and verification manifests. Archive contains one 53,248-byte acquisition SQLite database. Deserialized entirely into an in-memory SQLite connection: integrity_check = ok, foreign_key_check = zero violations. No extraction, production database access or restore. |
| Fresh, sufficient backup and restore coverage | BLOCKED: that historical capture does not prove current data coverage or restore readiness. API also mounts persistent shopping, calendar, settings and lifecycle storage; their host sources are not readable by the current user. Existing backup runbook explicitly records no approved restore drill for these modules. No fresh complete manifest/restore evidence was established. Finance storage was not inspected or copied. |
| Import activity | BLOCKED / actual activity UNKNOWN: read-only acquisition list reports total 40, including queued 2, downloading 5, indexing 1, failed 31 and completed 1. These are persisted labels, not proof of live transfer or restart safety. URL-import list is empty (endpoint limit 200). Media jobs reports degraded, Sonarr and Radarr unavailable, qBittorrent and Jellyfin online, one qBittorrent downloading display entry. No conclusion of idle operation is supported. |
| Other database operations | UNKNOWN: successful list reads do not prove absence of concurrent writers, maintenance or unsafe in-flight operations. No active-database integrity claim is made. |
| School sources | Existing verified configuration evidence reused: no SchoolMeals overrides; accepted defaults Enabled, RosenfeldtEnabled and MusikugglanEnabled are false. No settings changed or school source contacted. Effective v2 disabled response remains a post-deployment check because current images predate v2. |
| Execution and bounded recovery | BLOCKED: owner reports local systemd-run permission check exit 0; this is not evidence of agent privilege or a validated new-run recovery procedure. Old images remain the known recovery candidates, not authorization for downgrade. No new accepted-revision candidate or current-input execution plan was built/validated in this read-only task. |

The acquisition collection endpoint delegates to a database SELECT-only list. Individual job
GET instead reconciles provider status and updates the stored job; it was deliberately not
called. No start, inspect, cancel, preview or confirmation endpoint was called. Provider
unavailability was observed at BigBrain's boundary; its cause was not investigated or asserted.
A persisted nonterminal job may be stale: clearing/reclassifying it or treating it as harmless
would exceed this verification. The existing Media backup covers neither these jobs' latest
state nor all API volumes.

#### Scoped procedure and remaining operator prerequisites

Existing local operational precedent uses pinned service-specific Compose definitions and
systemd-managed bounded execution with durable state and recovery. Inspection only: no old
package, launch command or run ID was executed or modified. In particular the old optional-DOI
package captures only Media storage and contains a POST verification; it is not a verified
SchoolLunch deployment procedure and must not be reused as-is.

The appropriate future sequence remains: verify current identities and idle/safe activity;
verify sufficient current backups and recovery coverage; build immutable API/Web candidates
from the accepted revision while production stays running; validate a fresh, unique-run,
session-independent API/Web-only execution plan; replace API only, verify health, revision,
unchanged mounts/settings and disabled school responses; then replace Web only and verify it.
Use existing Compose service scoping (`--no-deps --no-build --pull never`) with verified pinned
inputs, never a whole-stack start. Preserve all three school flags as false and all unrelated
configuration. This describes required scope, not an executable launch command.

No involvement of `bigbrain.service` is required by service-scoped Compose replacement. Its
historical failed state does not justify restarting or repairing the appliance unit. No active
matching deployment systemd service appeared in the inspected unit list; this is not proof
against every possible unmanaged process.

Recovery must be phase-aware: safe restart of the exact unchanged old container only when
verified state permits it; after possible replacement, preserve state and stop for inspection
rather than automatically downgrade an image or restore data. Do not reuse old run IDs or
alter their results. API's shared process makes unverified in-flight activity relevant even
though source changes are limited to SchoolLunch; no Finance action is authorized.

No production operator launch command is supplied because backup coverage, activity safety
and current-input bounded recovery are not verified. Minimum next action after independent
review: provide current scoped backup/restore evidence and establish provider-backed status
of the eight nonterminal acquisition jobs plus unavailable queue information, using an
explicitly read-only method. Then validate fresh deployment inputs and recovery before a new
operational clearance. Owner authentication occurs locally only; no password or sudoers change
is requested. Existing deployment intent does not waive these gates.

Documentation-only validation: `node scripts/verify-documentation.mjs`, `git diff --check`,
changed-file and sanitized-diff review; application tests and builds intentionally not run.
No runtime or manual iPhone/PWA acceptance is claimed. Unrelated local work remains untouched.

### Targeted import-job and storage assessment — 2026-10-25

Scope: only the eight nonterminal acquisition jobs and API storage protection. Main and
checkpoint baseline remain unchanged. No job reconciliation, backup creation/copy, database
inspection, build, restart, deployment or Finance data access was performed. Existing CI and
historical backup integrity evidence above are reused, not rerun.

#### Eight jobs: stored state versus provider evidence

Identifiers below are the first 12 hexadecimal characters of SHA-256 of the API job ID;
titles, provider hashes and credentials are omitted. Timestamps are last persisted API updates
(UTC), not evidence of a continuously running process.

| Safe identifier | Stored state | Last persisted update | Current provider observation |
| --- | --- | --- | --- |
| a7b1309a759f | queued | 2026-10-23 20:47:51 | Absent from Librarr downloads; actual disposition UNKNOWN. |
| a72206b1f701 | downloading | 2026-10-21 19:56:15 | Matching Librarr download, downloading, 0 B/s at observation; progress not supplied. Active transfer not established; stalled/waiting remains possible. |
| 4956647a6857 | queued | 2026-10-17 15:59:28 | Absent from Librarr downloads; actual disposition UNKNOWN. |
| 5e6a5499b357 | downloading | 2026-10-17 10:38:55 | Absent from Librarr downloads; actual disposition UNKNOWN. |
| 5797d3d8c829 | downloading | 2026-10-17 10:38:55 | Absent from Librarr downloads; actual disposition UNKNOWN. |
| f424fa68ab35 | downloading | 2026-10-17 10:38:55 | Absent from Librarr downloads; actual disposition UNKNOWN. |
| f1f876d62eb3 | indexing | 2026-10-17 10:38:55 | Absent from Librarr downloads; ABS indexing/completion UNKNOWN. |
| 3fbbb9c24feb | downloading | 2026-10-17 10:38:55 | Absent from Librarr downloads; actual disposition UNKNOWN. |

Read-only GETs: API acquisition collection (40 total, all covered), Librarr `/api/downloads`
(one entry) and `/api/activity?limit=500&offset=0` (500 of 27,278 events). No matching event
for these eight IDs occurs in that bounded activity window; absence does not prove completion
or abandonment. No broad pagination was performed. Librarr image remains
`sha256:2431e564d944b048450ab99c94722cdbd10ca95af5e2e24a120cb0ec1bf33b6a`.

Handler review used the available local patched Librarr source and repository adapter/patch
contracts: downloads reads the manager/client queues; activity executes SELECT/count. The
manager suppresses individual torrent-client errors, so an empty/missing entry is not proof
of a complete successful queue read. Local source inspection is not a new binary attestation.
API collection reads stored rows only. Individual acquisition GET would reconcile and UPDATE,
and was not used. No start/cancel/retry/inspection endpoint was called.

Restart impact: code places torrent transfer and import watching in qBittorrent/Librarr, and
indexing in ABS, not in the API acquisition collection. An isolated API/Web replacement does
not stop those processes. API downtime interrupts UI/status requests and could interrupt an
in-flight acquisition submission or status write; a persisted label alone proves neither such
a request nor a restart hazard. These eight labels therefore do not independently justify
requiring every external torrent to finish. No job was declared abandoned or completed. Missing
information is a successful provider-level outcome for the seven absent IDs, ABS outcome for
the indexing entry, and absence of in-flight API mutations at the deployment boundary. A local
operator can inspect these exact jobs in existing provider interfaces without clearing them,
and pause user submissions during the bounded replacement. Do not use the mutating API job GET
as a read-only diagnostic. Unknown provider history need not be repaired merely to deploy
SchoolLunch if independence and no in-flight API mutations are established.

#### Storage: replacement preservation is not corruption recovery

Docker inspect verified existing local named volumes and the private service-specific Compose
files: API-only definition, ten mounts; Web-only definition, no mounts; both image references
match current running images. No Compose invocation or configuration change occurred.

| API destination | Ownership/content | Protection and remaining gap |
| --- | --- | --- |
| /data | Meal Planner SQLite and Media acquisition SQLite | Named volume survives container replacement if the exact mount is retained. Only the historical acquisition capture above is verified; no current Meal Planner backup established. |
| /shopping-data | Shopping List SQLite | Named volume; no verified current backup/restore evidence located. |
| /calendar-data | Calendar SQLite | Named volume; no verified current backup/restore evidence located. |
| /settings-data | Settings SQLite | Named volume; no verified current backup/restore evidence located. |
| /lifecycle-data | Recovery/lifecycle SQLite | Named volume; no verified current backup/restore evidence located. |
| /run/bigbrain | Shared Sentinel runtime socket | Existing local volume, ephemeral runtime state; preserve mount, do not treat as a business-database backup. |
| /run/bigbrain/identity | Read-only client identity bind | Preserve exact binding and access; credentials were not read or published. Recovery availability not established. |
| /host-timesync | Read-only host clock-state bind | Preserve binding; not an application dataset. |
| /finance-data and its read-only drop bind | Frozen Finance storage | Mount metadata only; no contents inspected, copied or verified. Preserve unchanged. This assessment gives no Finance backup or execution authorization. |

Relevant non-Finance database paths were verified against runtime environment. Targeted lookup
of existing deployment-artifact locations found the previously verified Media capture manifests,
not a fresh all-volume backup set. This is a bounded search, not proof that no other operator
backup exists. The backup runbook explicitly records no approved restore drill for household
and lifecycle volumes. No new universal restore-drill requirement is introduced here: the
owner's existing deployment condition requires usable backup/recovery evidence, which is still
missing. Read-only checks of a supplied manifest and integrity report could satisfy evidence
review without a production restore. New backup creation or an isolated restore exercise
requires separate authorization under this read-only task.

Git delta from running revision to accepted main contains SchoolLunch changes, with no changes
to the existing Meal Planner store, Calendar, Settings, Shopping List, Media or shared hosting
sources. No new database migration is introduced by this delta. This reduces schema/downgrade
risk but does not prove that existing data is backed up. Exact retained named mounts protect
against normal container replacement; they do not protect against corruption, wrong-mount
selection or application writes. Web has no persistent mounts. Automatic data restore or image
downgrade remains inappropriate in an uncertain state.

#### Decision and minimum next steps

**BLOCKED**, specifically on backup/recovery evidence required by the owner's deployment gate.
P1 source: the explicit requirement to verify usable backups/recovery before deployment, plus
AGENTS sections 5 and 10 (user-data preservation and safe interruption handling). Consequence:
if replacement exposes corruption or incorrect storage, an old image alone cannot recover
current household/import data. This is an evidence gap, not a detected corrupt database.

1. Operator supplies the existing non-Finance backup manifest/location, capture consistency,
   coverage/date and integrity/restore evidence for the persistent datasets above. If absent,
   owner authorizes a narrowly scoped backup and isolated verification; no production restore.
   Do not copy live WAL databases as standalone files or expand into frozen Finance operations.
2. Establish no in-flight API mutations during a short owner-controlled pause. Inspect provider
   outcome only where needed for restart safety; do not require cleanup of stale job labels,
   alter queues or revive BB-158. Keep qBittorrent, Librarr and ABS running unchanged.
3. At the separately authorized deployment boundary, use fresh accepted-revision API/Web inputs
   and the existing bounded session-independent execution pattern, API before Web, with exact
   mount/config preservation and phase-aware recovery. No old package may be rerun as-is.

No application correction, migration or new backup platform is demonstrated necessary. A fresh
operational plan and usable evidence are necessary; whether an isolated backup check must be
prepared depends on the operator's existing backup artifacts. No additional broad preflight or
provider repair is prescribed. STOP if coverage/integrity is unproven, a mutating request cannot
be safely drained, mounts/config drift, or bounded recovery cannot be established. All school
sources remain off. Documentation validator, diff/scope and sanitized-content checks are the
only checks run for this report; no application test suite or operational mutation.

### Authorized backup attempt — access gate, 2026-10-25

**BLOCKED before copying; backup set NOT CREATED.** Owner authorized a bounded consistent
backup and isolated verification, but required STOP on insufficient permissions. Main remains
`485aeda2ec5819ffc7f433b51ac867c55157c19f`; API remains healthy on the previously recorded image.
All five requested destinations are existing writable named-volume mounts. The agent cannot
read or traverse any host source: enumeration returned PermissionError for /data, /shopping-data,
/calendar-data, /settings-data and /lifecycle-data. `sudo -n -l /usr/bin/python3` requires a local
password. Approximately 388 GiB is free at the proposed workspace destination; source sizes
and exact file inventory could not be established. No Docker privilege workaround was used.

No backup directory, snapshot, copy, manifest, checksum validation or isolated restore was
created/performed. There is no current backup identifier or approved backup location to report;
the earlier historical Media backup does not satisfy this task. Existing containers, databases,
permissions, configuration and flags remain unchanged. Finance contents were not accessed.

Minimum operator action: run the following bounded read-only inventory in the owner's local
terminal with normal sudo authentication. This does not create a backup, change permissions,
grant Codex sudo access or authorize deployment. It reports only expected database presence,
size and entry counts, not rows, credentials or private host paths. Unexpected entries or
symlinks require review before selecting their backup method. Do not send passwords or raw
private data. Existing backup authorization remains valid; no repeated owner policy decision
is needed merely to perform the authorized backup once access and consistency are established.

```sh
sudo /usr/bin/python3 - <<'PY'
import json, os, stat, subprocess
c = json.loads(subprocess.check_output([
    'docker', 'inspect', 'bigbrain-sprint1-api-1']))[0]
assert c['Image'] == 'sha256:8b92e5ea802f215427456e670875df07760e2eff5694bd4ef24d6ad6ed32aa5c'
assert c['State']['Running'] and c['State']['Health']['Status'] == 'healthy'
expected = {
    '/data': ['meal-planner.db', 'audiobook-acquisition.db'],
    '/shopping-data': ['shopping-list.db'],
    '/calendar-data': ['calendar.db'],
    '/settings-data': ['settings.db'],
    '/lifecycle-data': ['lifecycle.db'],
}
for target, names in expected.items():
    matches = [m for m in c['Mounts'] if m['Destination'] == target]
    assert len(matches) == 1 and matches[0]['Type'] == 'volume'
    root = matches[0]['Source']
    with os.scandir(root) as entries:
        entries = list(entries)
    print(target, 'entries', len(entries), 'symlinks', sum(e.is_symlink() for e in entries))
    for name in names:
        path = os.path.join(root, name)
        try:
            s = os.lstat(path)
            print(name, 'regular', stat.S_ISREG(s.st_mode), 'bytes', s.st_size)
        except FileNotFoundError:
            print(name, 'MISSING')
PY
```

After that inventory, the selected SQLite method must use read-only source connections and
SQLite online backup into restricted files outside production volumes, with separate schema,
integrity and checksum verification of the copies. No live main-file-only copying. Consistency
is per database; this must not be represented as an atomic multi-database household snapshot.
Any non-SQLite files require explicit inventory and a safe consistency method before copying.
That work was not attempted after the permission stop. Documentation validator and diff/scope
checks pass; no application tests, backup mutation, deployment or production restore occurred.
