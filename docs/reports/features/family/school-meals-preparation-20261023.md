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
