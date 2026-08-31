# Testa BigBrain

## BB-122 historical security identity evidence pilot

- Documentation/research-only result. Verify the deterministic 30-ticker cohort and its 3 VERIFIED / 17 PARTIAL / 5 AMBIGUOUS / 5 UNRESOLVED classification without invoking intake, acquisition, promotion or autonomous research.
- Confirm `wiki-5713d7dccfa38f56` remains immutable at 3,722 canonical rows/five instruments, no WIKI redownload or Stooq request occurred, and current exchange directories are never treated as historical membership evidence.
- Existing mapping and BB-084 suites remain authoritative for effective dates/MIC ambiguity, all 13 gates, fail-closed `UNKNOWN`, provenance, checksums, immutable revisions, restart/idempotency, OHLCV/conflicts and cleanup safety. No production test changed because behavior did not change.
- Validate documentation structure/links, BB-ID uniqueness, Compose syntax, diff whitespace and secret patterns. No deployment is required.

## BB-121 WIKI forensics and Stooq requalification

- Documentation/research-only result. Read-only runtime/API and aggregate artifact inspection must not invoke intake, acquisition, promotion, feature generation, backtests or autonomous research. No production build/deployment is required without behavior change.
- Verify the retained WIKI SHA-256/size, 2014-01-02–2016-12-19 range, 3,186 ticker strings, 2,155,310 accepted/11,295 rejected rows, and exact five-symbol canonical lineage. AAPL/JNJ/JPM/MSFT each have 748 snapshot rows, XOM 732; SPY/QQQ/IWM have zero.
- Existing BB-084 tests remain authoritative for all 13 gates, `UNKNOWN` fail-closed behavior, checksum revisioning, immutable promotion, restart/idempotency, OHLCV, conflicts, overlap and cleanup safety. No test or gate changed.
- Validate documentation links/BB-ID uniqueness, Compose syntax, diff whitespace and sanitized secret patterns. No Stooq protected data route or external communication is part of verification.

## BB-120 historical evidence source qualification

- Documentation/research-only result: no production source, schema, configuration, provider request, quarantine artifact, promotion or deployment changed. Full code/build regression is therefore outside affected scope.
- Read-only `/api/v1/modules/finance/datasets` inspection must continue to show WIKI promoted as `wiki-5713d7dccfa38f56` with 3,722 canonical rows/five symbols and Zenodo as `ManualReviewRequired` with zero promoted rows. It must not invoke dataset intake POST operations.
- Validate documentation links/BB-ID uniqueness, Compose syntax, whitespace and sanitized secret patterns. Current EODHD path and the 13-gate fail-closed policy remain covered by the existing BB-077/084/085 suites; no test was weakened or duplicated.

## BB-119 Finance readiness and governor reconciliation

- Pinned-clock scheduler tests cover an eligible complete 8/8 universe, incomplete universe, exact/incompatible feature lineage and a 2026-08-30 non-session date. Historical evidence and current-session eligibility are asserted independently; restart/reconciliation tests remain required.
- Operations tests assert the same live readiness projection and explicitly require `NOT_REQUIRED_NON_RESEARCH_DAY` rather than an inferred generic `READY`. Governor tests preserve missing/stale metrics → `DEFER`, healthy metrics → `ALLOW`, critical disk → `BLOCK`, and `BLOCK > DEFER > ALLOW`.
- Sentinel integration starts over a pre-existing stale socket file and must bind successfully. Deployment verification must inspect scheduler, operations, governor and system overview without triggering research or mutating Finance data.

## BB-118 Finance source-of-truth reconciliation

- Production behavior is unchanged. Runtime verification uses only GET endpoints plus repository-native `finance-evidence-counts` and `finance-schema-status`; it must not invoke autonomous-run POST, provider acquisition, backfill, prediction creation, scheduler/config mutation or deletion.
- Validate canonical current-state consistency, report schema/links, unique BB IDs, Compose syntax, diff whitespace and publication secrets. Historical reports remain immutable evidence; stale phrases are acceptable only when explicitly scoped to their original date/slice.
- Current read-only baseline: `RESEARCH / 0 SEK / NONE`; scheduler enabled/not running; 10 opportunities; 0 autonomous runs/experiments; 105 market revisions; 29,890 observations; 16 feature revisions; 797 backtests; 25 robustness evaluations; 288 shadow predictions; 240 outcomes; 240 research-risk evaluations; schema 93. Latest feature revision has 44,520 values; aggregate values are not exposed.

## BB-117 audiobook final polish and Media handoff

- Focused Web coverage locks the 48 px gold crescent target, 23 px vector glyph, lower-right Continue Listening alignment, inactive/active `aria-pressed`, focus entry/return, dialog open/dismiss, 15/30/45/60, local stop time, off, shared detail deadline, expiry pause/sync, independent status row and unchanged mobile two-column Detail Hero.
- Browser QA targets 390×844, 430×932, tablet and 1440×900. Verify compact mobile bottom sheet/desktop popover, no collision or horizontal overflow, active status separated from progress/time and unchanged cover-left/detail-metadata-right hero. Automated QA never grants physical owner approval.
- BB-117 changes Web presentation only. Run focused Web tests, full Web only if required by a regression signal or publication policy, Vite production build, documentation verification, diff/secret gates, Web-only deployment/runtime smoke and GitHub CI. API, Finance, Family and Sentinel suites are outside the affected scope.
- Result 2026-08-31: 34 focused and 160 full Web tests passed; Vite production build and 201-document/89-unique-BB-ID validation passed. Web-only image `sha256:215921b72acbcf27c5f1fcc2bdc9f0e7c512c70e7fb5892e664b18629bed3749` is healthy and `/` plus `/health` returned HTTP 200. Deployed Firefox at all four viewports verified 48×48/23×23 trigger geometry, gold treatment, active state, 320 px mobile/280 px desktop interaction, focus return, shared deadline/off, non-overlap, unchanged hero and no overflow. Firefox BiDi pointer dispatch was inconsistent at narrow viewports even though hit-testing resolved directly to the button; DOM activation completed state QA, so physical iPhone/PWA remains the authoritative touch gate. GitHub Actions runs `33385591024` and `33386467803` passed all jobs.

## BB-115R physical iPhone sleep-timer remediation

- `Audiobooks.test.tsx` verifies that the pre-session timer is not a dead disabled icon: native details/summary activation opens visible guidance and an explicit playback action; the still-open panel becomes configurable from Continue Listening after session creation. It retains presets, custom local clock, replace/cancel, shared detail status, ordinary expiration pause/sync and unchanged Play/Pause.
- `UXPolishStyles.test.js` requires active timer status to occupy its own in-flow `grid-column:1/-1` row and forbids the former absolute status positioning. The compact panel is viewport-bounded.
- Local result 2026-08-30: 32 focused Web tests and Vite production build passed. Deployed 390×844, 430×932 and 1440×900 QA must cover no-session guidance, start→15 min, hour-boundary/long-duration status, shared detail state, change/cancel, containment and no overflow. Physical iPhone/PWA verification remains authoritative and pending.
- Final result 2026-08-31: 32 focused and 157 full Web tests plus Vite build passed. Deployed Firefox at all three viewports opened the native summary by pointer, showed no-session guidance/start in a 280 px panel, then rendered `60 min kvar` with a 4:33:57 audiobook in a static `1 / -1` row with zero geometric intersection or overflow. Exact status survived detail navigation and cancelled there. Web `/` and `/health` returned HTTP 200; GitHub Actions runs `33337614697` and `33337917824` passed. Physical iPhone/PWA remains pending.

## BB-115 physical iPhone artwork and Continue Listening timer

- `UXPolishStyles.test.js` protects the root cascade correction: the obsolete mobile `display:contents` and broad detail-child grid override are absent, one canonical detail container remains, and both fetched artwork and BigBrain-B receive explicit 2:3, first-column, intrinsic-safe, 40 vw / 180 px constraints.
- `Audiobooks.test.tsx` verifies that the timer affordance is present on Continue Listening, disabled before an active session, enabled after direct playback, keyboard/button semantics through `aria-expanded`/`aria-controls`, presets, custom local clock, cancellation, replacement, ordinary expiration pause and the same deadline/status after navigating to detail. Existing direct Play/Pause behavior remains covered.
- Local result 2026-08-30: 31 focused Web and 156 full Web passed; Vite production build passed. API was not touched and BB-115 does not require an API rerun.
- Browser QA must measure Induction and BigBrain-B at 390×844, 430×932 and 1440×900, verify no overflow, and exercise Continue Listening timer start/change/cancel plus shared detail/navigation state. Browser QA cannot replace physical iPhone/PWA owner verification because BB-114's browser matrix failed to expose the reported regression.
- Deployed result 2026-08-30: Induction measured 156×234, 172×258 and 180×270 px at the three viewports, all 2:3 without overflow; Golden Compass BigBrain-B matched the widths/ratio. Continue Listening exposed the accessible timer, started 15 minutes, retained the same status on detail, changed to 30 minutes and cancelled. Web-only deployment and `/health` returned HTTP 200. GitHub Actions run `33328144299` passed.

## BB-114 audiobook detail polish, sleep timer and floating-player rejection

- `Audiobooks.test.tsx` covers semantic detail metadata, absent redundant label/raw fallback copy, healthy-native suppression of the Audiobookshelf link, truthful unavailable recovery, route-surviving session state, in-detail controls, sleep presets/custom local clock/cancel/replace/expiration and ordinary pause sync.
- `AudiobookTests` traces Audiobookshelf `authorName`, `seriesName`, `narratorName`, `language`, `publishedYear` and `description`; it locks the verified `X3M 4ever!!!` non-synopsis omission while preserving useful Ghostsong metadata. `UXPolishStyles.test.js` locks 2:3 compact responsive detail artwork, shared fallback geometry, no fixed player styling and no AppShell player render.
- Browser QA: 390×844, 430×932 and 1440×900. Verify Narnia, Ghostsong and Golden Compass fallback; no LJUDBOK/raw X3M/reservväg; timer presets/custom/cancel; Home/Family/Finance navigation with continuing audio and no overlay; return-state, dock clearance and zero horizontal overflow. Exact sleep expiry is best-effort while iOS suspends a PWA.
- Result 2026-08-30: 19 focused API, 31 focused Web, 565 full API, 156 full Web and 32 Sentinel passed; Vite/Release, 196-document/89-unique-BB-ID, Compose and diff gates passed. Only API/Web were recreated. Deployed Firefox verified Narnia/Ghostsong/Golden Compass at 390×844, 430×932 and 1440×900 with 156/172/180 px 2:3 artwork, same-size BigBrain-B, no raw/fallback copy and no overflow. Induction playback survived Home without overlay, returned as Pausa, and timer activated at 15 min then cancelled. GitHub Actions run `33326376331` passed all four jobs for `2607d6f804efd6bbc49bdc2c2f9e721655692a68`.

## BB-113 audiobook owner-review remediation

- `Audiobooks.test.tsx` covers separate direct play versus detail navigation, accessible play/pause state, native start/session path, authoritative duration/progress time, healthy native-primary/fallback-secondary hierarchy and truthful unavailable fallback.
- `AudiobookTests` preserves arbitrary valid owner artwork and matches only the exact runtime-verified Audiobookshelf generic media-note placeholder hash. `UXPolishStyles.test.js` locks the hero's `minmax(0,1fr)`, natural word wrapping, 2:3 artwork and narrow stacked fallback.
- Gates: focused audiobook Web/API, full Web/API/Sentinel, Vite/Release build, Compose, documentation, diff and scoped secret scan. Deployed QA: 390×844, 430×932 and 1440×900; overview/direct playback/time/detail/Golden Compass plus a long Swedish title/native player/fallback/mini-player route survival/overflow/dock-safe-area. Theme expansion is required only where the existing automation is cheap.
- Result 2026-08-29: 18 focused API, 23 focused Web, 564 full API, 32 Sentinel and 154 full Web passed; Vite, Compose, 195-document verification, diff and staged gitleaks passed. Deployed Firefox direct-play/mini-player/route-survival passed, as did the 3×3 viewport/theme matrix. GitHub Actions run `33269194506` passed frontend, backend, documentation and secrets for `5d829fdcf64541e49b8cf355004bea90d2a372d3`.
- Publication follow-up: docs-run `33269347381` exposed one asynchronous test race where the collection button still had busy-name `Filtrera pågår`; the assertion now awaits `Filtrera`. Three focused repetitions, full Web and Vite passed locally; GitHub Actions run `33269619672` passed all jobs for testfix `09976f0ab43c825f22434177ca7952f5207b746b`.

## BB-112 native audiobook playback and owner search remediation

- `AudiobookPlaybackTests` covers separate identity/progress verification, credential-free opaque DTOs, item/track binding, invalid progress, single bounded Range and absence of arbitrary upstream URLs.
- Audiobook acquisition/provider tests retain deterministic ranking, unknown-language retention, stable release dedup and partial-provider behavior. `Audiobooks.test.tsx` retains overview/collection/detail, result and confidence presentation.
- Runtime mutation is bounded: move an existing in-progress item one second, sync, restore the original position on close, then require the closed stream to return 404. Firefox verifies `206` audio, playing state, real slider position, route survival and no overflow.

## BB-111 route/detail UX and playback credential gate

- `npm test -- --run src/audiobooks/Audiobooks.test.tsx src/routeFocus.test.ts src/UXPolishStyles.test.js`: 28/28. Täcker semantic Library-link i stället för CTA, pointer-/keyboardklassificerad route focus, forward detail till top, back-restoration, okänt språk som utelämnas, detail artwork-klass/ratio-kontrakt och befintliga bounded audiobookflöden.
- `npm test -- --run`: 151/151 Web.
- `npm run build`: TypeScript + Vite production build passerar.
- `dotnet test BigBrain.slnx -c Release --no-restore`: 558 API + 32 Sentinel passerar. Det första felskrivna försöket mot `BigBrain.sln` kunde inte starta eftersom repositoryt använder `BigBrain.slnx`; det körde inga tester och följdes av korrekt kommando.
- Runtime credential discovery är read-only och sanerad: endast identity/privilege-klass, aktiveringsstatus och aggregerade progress/session-counts får lämna proben. Token, användarnamn, user ID, privata URL:er och råa payloads får aldrig skrivas ut. Resultatet 2026-08-28 är restricted/non-root, 0 progress och 0 sessions; inga playback-write-/sessionanrop kördes mot fel identitet.
- `docker compose config --quiet`, 192-filers documentation verifier, `git diff --check` och scoped staged gitleaks (0 fynd) passerar.
- Deployad Firefox-matris 390×844/430×932/1440×900 × Obsidian Gold/Forest Night/Arctic Wind passerar. Samtliga nio fall har link-semantik, 0 overview-katalograder, pointerfokuserad heading med DOM-fokus men ingen ring, detail på `scrollY=0`, exakt collection-scrollrestoration, 1.5 höjd/bredd (2:3), `object-fit:cover`, inget `Språk okänt` och ingen overflow. Web/API health är HTTP 200; GitHub Actions run `33187236677` passerade implementation `686f621937ae0e376bcc55003077769fff8b4351`.

## BB-110 audiobook owner UX consolidation

- `Audiobooks.test.tsx` verifierar kanonisk secondary-variant för **Bibliotek**, reducerad synlig copy med bevarade accessible names, discovery → library → downloads, semantic whole-row detail navigation, bounded collection och lokal persistent attention-dismiss utan POST eller audit/provider-mutation.
- Full Web-regression, API/Sentinel-regression och Release/Vite-build körs trots att BB-110 endast ändrar Web. Browsermatrisen är 390×844, 430×932 och 1440×900 i Obsidian Gold, Forest Night och Arctic Wind; kontrollera långa titlar, vertikala jobb, dock/safe-area och overflow.
- Native playback får inte testas eller markeras implementerad innan BigBrain-user→Audiobookshelf-playback-identitet och same-origin Range/session/progress-sync har ett godkänt arkitekturkontrakt. Ingen ägartoken får förekomma i Web eller browserlagring.
- Resultat 2026-08-28: 25 fokuserade Web, 147 fulla Web, 558 API och 32 Sentinel; Release build 0 warnings/errors, Vite, Compose och 191 Markdown passerade. Deployad 3×3 browsermatris passerade utan overflow med 20 bounded rows, semantic whole-row, korrekt sektionsordning, jobbwrapping och 112 px mobil dock-clearance. Två separata asynkrona provider-status-assertions stabiliserades efter en CI-race; GitHub Actions run `33180923644` var green för `87d8528791a32a0e4d0e66de400d5ca4d5c392c9`.

## BB-109 audiobook owner UX remediation

- `Audiobooks.test.tsx` verifierar standardiserad **Bibliotek**-affordance utan dominant count, progressbaserad Continue Listening utan falsk tomstatus, separata inputs för ny discovery/lokal filtrering, bounded collection, reducerad-motion scroll-till-top, aktiva/attention/history-sektioner och presentation-only historikrensning. Befintlig edition-confirmation/idempotens körs fortsatt.
- `AudiobookAcquisitionTests` verifierar att provider-absence efter fem minuters grace fail-closed blir `failed`, medan ett nyregistrerat jobb behåller aktiv state under gracen.
- Runtimekontroll ska sanerat verifiera serviceprofilens respektive övrig profils progressantal, provider/download/import-evidens och att inga acquisitioner cancel/start/delete görs. Native playback får inte markeras complete utan beslutad identitet och säker same-origin session/stream-gräns.
- Browser-QA: 390×844, 430×932 och 1440×900 i alla tre teman. Kontrollera overview, Bibliotek, separat discovery/library-search, rubrikhierarki, scroll-till-top/dock/safe-area, aktivitet/history, overflow och lång svensk text.

## BB-108 audiobook navigation experiment

- `Audiobooks.test.tsx` verifierar att Media-overview inte renderar katalog/nyligen tillagt, att Continue Listening kräver verklig progress, collection-routen är bounded, detail har egen deep-link, browser/in-app-back använder History API och query/sort överlever detail-retur. Befintlig sök-, edition-confirmation-, jobb- och completion-regression körs oförändrad.
- `App.test.tsx` verifierar att en direkt `/media/audiobooks`-länk aktiverar Media och renderar endast den dedikerade audiobook-routen, inte hela Media-dashboarden.
- `components.test.tsx` förblir kontraktstest för BigBrain-B vid saknad/misslyckad bild. Runtime artwork-verifiering ska separat skilja HTTP-success från 404: en verklig Audiobookshelf-cover får inte ersättas baserat på motivet.
- Browser-QA: 390×844, 430×932 och 1440×900 i Obsidian Gold, Forest Night och Arctic Wind. Kontrollera overview-density, progress, collection-affordance, bounded catalogue, detail/deep-link/back, state restoration, dock/safe-area, overflow, lång text och `prefers-reduced-motion`. QA får inte skapa acquisition eller ändra media.

## BB-107 owner UX remediation

- `MediaLookupRequestTests` simulerar den verkliga first-click-klassen: Arr registrerar posten men POST-svaret timeoutar. API:t avstämmer exakt registrerad foreign ID, returnerar `created` och gör totalt en POST. Web-testet bevisar att retry av samma dialog använder samma idempotensnyckel och att busy-knappen blockerar dubbeltryck.
- `Audiobooks.test.tsx` bevisar overview → collection: Media-starten visar högst fyra senaste böcker och döljer full katalog/sökfält tills **Visa alla** väljs. Befintliga explicit-utgåve-, aktivitet-, completion- och placeholdertester består.
- Browser-QA kör 390×844, 430×932 och 1440×900 i alla teman. Kontrollera Home-textcellens användbara bredd, horizontal overflow, verklig sista kontroll över dockan, kompakt audiobook overview, gemensam B-placeholder och dimensionsstabil sök-busy state. Den muterande first-click-regressionen körs med säker providerfixture, inte mot användarens bibliotek.

## BB-106 consolidated UX quality

- `components.test.tsx` verifierar dimensionsstabil busy-knapp, tillgänglig status och gemensam media-placeholder. Audiobook-regressionen verifierar explicit utgåvebekräftelse, kompakt historik och pagineringskontrakt; Calendar provar lokal past/today/future-klassificering och Home provar datum+titel+tid.
- Full verifiering: 135 Web-, 555 API- och 32 Sentinel-tester, Vite- och Release-build, Compose, dokumentationsverifierare och diffkontroll.
- Deployad browserkontroll ska vänta tills modulernas read-only data har stabiliserats, scrolla den rumsligt sista kontrollen och prova 390×844 samt 430×932 i alla tre teman. Kräv ingen horisontell overflow, synliga textfält och full dock-clearance. Kontrollera även 1440×900 och reduced-motion-kontraktet.

## BB-105 AudioBookBay parser and literal author search

- Build `bigbrain-librarr:1208254-bb6`. The image applies the sanitized current-markup fixture, proves English rows survive adjacent nested metadata labels, rejects a non-English row, and reruns complete organizer/search/download plus the focused API regression.
- `AudiobookMetadataTests` prove literal author-only input is the first bounded provider seed and only one resolved work supplements it. `AudiobookAcquisitionTests` prove English preference, Swedish preference, All Languages and retained unknown candidates without creating requests.
- Runtime verification is read-only: record source HTML post count, AudioBookBay parsed count, Librarr retained count and BigBrain candidate count; compare acquisition-job totals before/after and never open the final confirmation.
- BB-105 follow-up image `bigbrain-librarr:1208254-bb7` adds a network-free table regression proving AudioBookBay alone sends lowercase queries for `Pirateaba`, `pirateaba`, `PIRATEABA`, `The Wandering Inn` and `the wandering inn`. Runtime QA repeats those five GET-only searches and verifies acquisition-job totals and Ghostsong state are unchanged.

## BB-104 universal metadata-aware audiobook search

- `AudiobookMetadataTests` use network-free Open Library fixtures for ISBN-10/13 classification, malformed identifiers, deterministic metadata parsing, missing metadata, timeout, series/alternate/author planning, narrator capability and the bounded The Wandering Inn fixture.
- `AudiobookAcquisitionTests` prove that at most two metadata seeds are merged, duplicate provider editions collapse, English/unknown remain visible under Swedish preference, partial source failure retains successful results and search creates no acquisition request.
- `Audiobooks.test.tsx` proves the single universal input, canonical book context and unchanged explicit edition-confirmation gate. Runtime acceptance is read-only: compare job count before/after title, author and ISBN searches and never press **Lägg till vald utgåva**.

## BB-103 usable audiobook lifecycle

- Build `bigbrain-librarr:1208254-bb5`; its revision-policy regressions prove the exact pinned native audiobook source set is accepted in Prowlarr-first order while a revision mismatch, missing source, duplicate registration, malformed ID or injected/future source fails closed. The same image reruns the complete organizer/search/download packages, preserving every no-overwrite and lifecycle regression.
- BigBrain provider tests cover all exact pinned source IDs, safe URL/hash/path normalization, unknown-source rejection and rejection-before-request for direct native candidates that cannot enter the hash-backed acquisition lifecycle. Runtime diagnostics use the same bounded title/author variants and expose sanitized per-source counts only.

- Build `bigbrain-librarr:1208254-bb4`; the image gate runs complete upstream organizer/search/download tests. Regressions prove an existing audiobook destination creates durable failure evidence, never deletes the qBittorrent job/source and never changes existing content. Discovery tests cover unchanged title-only behavior, bounded title-plus-author variants reaching sources, normalized deduplication, audiobook-suffix suppression and retained partial-source results; existing scoring/language tests remain in the complete search package. This bb4 evidence is retained historically; bb5 is the current runtime image.
- `LibrarrAudiobookAcquisitionProviderTests` cover single-use opaque candidates, bounded real-state mapping, disappearance without completion, durable import failure, exact local import identity, Audiobookshelf indexing and final completion. Missing evidence remains importing/indexing rather than becoming complete.
- `Audiobooks.test.tsx` proves the actual registered Media view requires explicit release confirmation, submits exactly once only after that confirmation, localizes truthful job states, refreshes the library on completion and never fabricates percentage progress.
- Runtime QA performs read-only real search, confirms zero jobs before the owner gate, and checks 390 × 844 plus 430 × 932 in all themes. Never click **Lägg till vald utgåva** without explicit owner selection and approval.
- First-acquisition regression coverage proves that an exact imported provider hash may reconcile with exactly one canonical Audiobookshelf title even when release tags differ and Librarr reports author `Unknown`; two possible canonical matches remain `indexing`. Web proves transient provider-status failure renders `configuredUnavailable`, not a false not-configured state. Final totals: 540 API and 130 Web tests.

## BB-102 patched Librarr provider

- `docker build -t bigbrain-librarr:1208254-bb4 -f infrastructure/librarr/Dockerfile .` applies all pinned patches, runs the complete upstream organizer/search/download packages and the focused native-source and query regressions. Source-policy tests prove exact allowlisting, Prowlarr-first order and fail-closed empty/duplicate/unknown configuration.
- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --filter 'FullyQualifiedName~LibrarrAudiobookAcquisitionProviderTests|FullyQualifiedName~AudiobookAcquisitionTests' --no-restore` covers authenticated health, dependency degradation, bounded/opaque candidates, request cache, language hints, state mapping and safe cancellation semantics without a network.
- `npm test -- --run src/audiobooks/Audiobooks.test.tsx` covers the real registry-composed Media view and bounded polling of real job states without fabricated percentage progress.
- Runtime search and downstream health are commissioning checks. Aggregate candidate counts/provenance may be recorded, but raw URLs and releases stay private. These checks must not initiate the first download; secrets remain outside Git, test output and chat.

## BB-101 audiobook acquisition foundation

- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --filter FullyQualifiedName~Audiobook --no-restore` covers provider None, bounded search, Swedish/English/unknown ranking, narrator/edition distinction, stable job IDs, state mapping, controlled provider failure, missing-job 404, cancellation and safe import paths including no-overwrite.
- The complete API suite proves the new store and provider boundary do not alter existing modules. Provider fixtures are network-free and never download media.
- `npm test -- --run src/audiobooks/Audiobooks.test.tsx` covers actual Media registry placement, Swedish-default search, explicit provider-unavailable UI, absence of fake progress, edition detail and disabled request controls.
- Runtime verification must preserve BB-100 `configuredHealthy`, show provider `NotConfigured`, return an empty job list, reject a fabricated request without persisting a job and inspect Media/Ljudböcker at 390 × 844 and 430 × 932 in all three themes.
- No real provider is part of ordinary validation. Installing one requires an owner decision plus a separate maintenance/API/security review.

## BB-100 audiobook platform foundation

- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --no-restore` covers network-free ABS mapping/degradation, auth failures, malformed payloads, provider None, language normalization/ranking and edition distinction.
- `npm test -- --run` covers configured and not-configured audiobook UI plus language-filtered local search without a real ABS dependency.
- `npm run build`, Release build, Sentinel tests, Compose validation, documentation verification and `git diff --check` remain publication gates.
- Runtime verification checks the audiobook overview, Media/Ljudböcker in all themes, dock clearance, existing Media flows and Finance `RESEARCH / 0 / NONE`.
- A configured library test requires an owner-created ABS service identity/API key and library ID; secrets never enter fixtures, documentation or frontend.

## BB-099 whole-app UX audit

Render every registered view plus embedded Settings at 390 × 844, scroll long views from top to bottom and exercise their primary controls. Use normal viewport captures, never stitched full-page iOS captures, as reachability evidence. Verify that the last actionable element can scroll entirely above the persistent dock through the shared `--bb-mobile-dock-clearance`, and specifically exercise Media search/request, downloads, Smart Shuffle, its lower select and collapsed technical integrations. Home tests must prove that existing read-only API data appears before contextual navigation. Finance must keep its default narrative and advanced disclosure without API or safety changes. Repeat representative checks at 430 × 932, 768 × 1024 and 1440 × 900 and across all three themes. Family remains the regression canary. Finance must remain `RESEARCH / 0 SEK / NONE` with unchanged scheduler and governor.

## BB-098 global UI migration

Treat BB-097 Family as the visual canary. Frontend regression must cover semantic button variants including busy/disabled state, native label/value behavior for inputs and selects, theme switching and existing route actions. Normal viewport review—not stitched full-page capture—covers Home, Family, Media, Finance, More/Settings, AI and Admin in Obsidian Gold, Forest Night and Arctic Wind at 390 × 844, plus 430 × 932, 768 × 1024 and 1440 × 900 responsive checks. Finance advanced disclosure remains accessible and Finance must stay `RESEARCH / 0 SEK / NONE`. The owner-confirmed iOS Safari full-page screenshot artifact remains a known limitation and is not a BB-098 acceptance surface.

## BB-097 Family reference validation

Family behavioral regression verifies the dedicated reference composition, absence of a normal-mode `DashboardWidget` wrapper, semantic page heading, settings access, meal tabs, shopping mode and calendar access. Existing Meal Planner, Shopping List, Calendar, AppShell, theme and navigation suites remain authoritative for detailed actions and accessibility. Pixel snapshots are deliberately not used. Separate manual browser evidence must render both Obsidian Gold and Forest Night repeatedly at 390 × 844 and then at 430 × 932, using normal viewport captures and additional scroll positions where needed. Compare atmosphere, materiality, tonal and typographic hierarchy, accent restraint, border/radius treatment, lighting, dock placement and theme identity against the original local mockups. Full-page capture is diagnostic only because fixed/composited backgrounds may produce stitching artifacts; it never replaces normal viewport review. Fixture-only visual runs must be identified as such and never represented as deployed evidence.

Real-iPhone owner evidence is authoritative for iOS Safari capture behavior. As of the 2026-08-23 owner-directed pass, the large bright rounded/geometric artifact remains reproducible in stitched full-page iOS Safari screenshots for Obsidian Gold and Forest Night while absent in normal interaction. Desktop/headless non-reproduction must never be reported as an iOS fix. Normal 390 × 844 top and scrolled viewport frames remain the art-direction acceptance surface; 430 × 932 is the responsive smoke surface.

The final BB-097 craftsmanship pass additionally requires the normal mobile Söndag/Idag/Smashed Burgers/Byt-open state in both dark themes. Verify automatic, manual and dismiss actions through component tests; viewport review must not mutate real meal data. The primary action receives focus, Escape dismisses the contextual panel, action targets remain at least 44 px except the intentionally tertiary 40 px dismiss target, long Swedish meal names wrap without colliding with Byt, and `prefers-reduced-motion` removes the new panel/control motion.

BB-092 adds network-free tests for the allowlisted research feature registry, invalid IDs and bounds, deterministic hypothesis fingerprints, explainable complexity, OOS/cost/lineage fail-closed integrity, family attempt visibility, DSR/PBO `NOT_EVALUABLE`, and conservative read-only UI language. Remediation coverage also exercises partial-run evidence retention, persisted restart recovery, cross-key global single-flight, same-key failed-result idempotency, actual 3/5/2 attempt accumulation, bounded/filterable history, count reconciliation and target/horizon consistency. Final evidence-selection cases prove that a second feature/robustness generation wins over lexically earlier stale history, incomplete current families do not fall back, exact market lineage and approved strategy versions are mandatory, and repeated unchanged selection stays deterministic. It reuses BB-081's train/test leakage, cost monotonicity and real expanding-window tests. Research tests never call providers or create PAPER, broker, order, portfolio, LIVE/AUTO or risk-policy state.

BB-093 scheduler tests use injected times/direct orchestrator calls rather than real sleeps. They cover default-disabled startup, due completion, repeated ticks, completed and pre-run restart recovery, no catch-up storm, recovery/data deferral, manual-run busy deferral, current-evidence failure, option bounds, cancellation, bounded APIs, read-only UI wording and unchanged `RESEARCH / 0 SEK / NONE` authority. Readiness remediation additionally covers one-current/rest-stale rejection, full-universe recovery on the same opportunity, stale feature deferral, exact source-lineage mismatch, deterministic readiness, zero experiment evidence on partial acquisition and explicit cross-date supersession of deferred work.

BB-094 resource-governor tests inject deterministic `ISystemMetricsProvider` snapshots; they never depend on workstation load. Coverage includes healthy allow, independent and combined CPU/memory/disk pressure, critical-disk precedence, unavailable/stale/throwing metrics, option bounds, no-run deferral followed by same-opportunity completion, restart-readable compact audit, read-only API/UI state and unchanged `RESEARCH / 0 SEK / NONE` authority. Temperature remains explicitly unsupported and is not faked.

BB-095 operations tests use isolated SQLite stores and injected timestamps. They cover disabled/maintenance semantics, stale enabled scheduling, persistent readiness/resource waits, operational-versus-scientific failure classification, deduplicated incident streaks, success recovery, pre-run interruption, partial experiment preservation, post-run scheduler reconciliation, repeated reconciliation, bounded read APIs, compact metadata backup/restore and unchanged `RESEARCH / 0 SEK / NONE` authority. Hosted-service tests never wait for real scheduled time or contact providers.

BB-096 frontend regression covers the three stable theme IDs, default/fallback and migration aliases, local/server persistence, immediate switching, shared token completeness, five-item primary navigation, secondary AI/Admin access, Family relocation without functional removal, dashboard editing and the existing module interaction suites. Visual review uses temporary, uncommitted browser captures at mobile and desktop sizes; the local design mockup binaries are references and are not test fixtures.

BB-089 adds network-free policy, invariant and adversarial tests for deterministic identity,
version/config validation, ALLOW/REDUCE/DENY/HALT/INSUFFICIENT_DATA, EOD weekend freshness,
clock/lineage/instrument/price/health/volatility/liquidity/exposure failures, client-forged verdicts,
simulated daily loss/drawdown/consecutive losses, immutable idempotence and durable audited halt
recovery. Tests never create an order, broker connection, real portfolio or provider call.

BB-088 adds network-free tests for weekday/provider-window scheduling, healthy weekend/no-provider
cycles, cadence timestamps, bounded read-only status/overview endpoints, repeat outcome evaluation,
actual market breadth, transparent POSITIVE/NEUTRAL/NEGATIVE aggregation, pending/sample honesty,
historical/prospective separation and absence of fake index, portfolio, real-time or order claims.
Runtime provider verification is separate and must not manufacture a weekend session.

BB-087 tests are network-free. They cover deterministic prediction identity, retry idempotence, knowledge cutoff/no-lookahead selection, strategy/parameter/source lineage, explicit horizon, pending-to-evaluated temporal progression, append-only outcomes, clock fail-closed, late-start anti-backfill, bounded/malformed read API, UI pending/sample honesty and absence of mutation/order controls. Full API, Sentinel and frontend regression plus Release/build/documentation/secrets/Compose checks remain required before publication.

BB-086 changes research/planning documentation only because all eight ETF-history candidates
failed closed before acquisition. No runtime contract or fixture changed. Publication verification
therefore runs the complete existing backend/frontend suites and builds, documentation verifier,
secret scan, Compose validation and `git diff --check`; ordinary tests remain network-free.

BB-085 tests are network-free and cover WIKI/EODHD/unknown source classification, deterministic
manifests, atomic/incomplete-state handling, SHA-256 corruption rejection, isolated restore
identity, derived lineage, disk gates, rejected/manual-review cleanup, idempotence and canonical
protection. Runtime drills use only existing local Finance evidence and make no provider calls.

BB-084 tests are network-free and use sanitized CSV/rights fixtures. They cover candidate
transitions, content/schema hashes, promotion PASS/FAIL/UNKNOWN, CSV quoting, ZIP traversal,
OHLCV/duplicate policy, symbol-bounded promotion, cross-source classification and idempotent
re-import. Live WIKI/Zenodo acquisition is a one-time maintenance verification, never an
ordinary test dependency. Long-history robustness also verifies the explicit run-budget cap.

BB-083 tests clean/unclean markers, idempotent recovery, missed-run policies and conservative
interrupted EODHD acquisition without live calls. systemd/reboot remain separate host tests;
CI need not run systemd as PID 1. The verifier prints sanitized states/counts only.

Detta dokument är en kort karta. Auktoritativa procedurer ligger i respektive runbook och modulkontrakt.

## Automatiska tester

Frontend:

```bash
cd src/BigBrain.Web
npm ci
npm test -- --run
npm run build
```

Backend och Sentinel med den lokala .NET 10 SDK:n, från repositoryroten och utan sudo:

```bash
dotnet restore BigBrain.slnx
dotnet build BigBrain.slnx --configuration Release --no-restore
dotnet test BigBrain.slnx --configuration Release --no-build --no-restore
```

Dokumentation och repositoryhygien:

```bash
node scripts/verify-documentation.mjs
git diff --check
docker compose config --quiet
```

## Verifieringskarta

- Dashboard/widgetramverk, persistence, responsiv kontroll, Web-only deployment och rollback: [dashboardrunbook](docs/operations/runbooks/dashboard-widget-framework-verification.md).
- Kalender/Heroma: [modulkontrakt](docs/modules/calendar.md), [import-runbook](docs/operations/runbooks/heroma-schedule-import.md) och [verifieringsrunbook](docs/operations/runbooks/calendar-verification.md). Verkliga Heroma-filer får aldrig användas i automatiska tester; workbooks genereras syntetiskt.
- Media API och read-only providerkontroll: [Media integration verification](docs/operations/runbooks/media-integration-verification.md).
- Smart Shuffle: [Mediamodulen](docs/modules/media.md), [ADR 0011](docs/adr/0011-smart-shuffle-jellyfin-remote-playback-boundary.md) och samma media-runbook.
- Download Control: [säker borttagningsrunbook](docs/operations/runbooks/download-control-safe-removal.md), [ADR 0013](docs/adr/0013-safe-qbittorrent-download-removal-boundary.md) och [ADR 0016](docs/adr/0016-safe-download-control-command-and-partial-batch-boundary.md). Automatiska tester får aldrig mutera riktiga torrents.
- Designsystem och teman: [manuell verifieringsplan](docs/design-system/manual-verification.md), [theme contract](docs/design-system/theme-contract-v1.md) och [Jellyfin-runbook](docs/operations/runbooks/jellyfin-bigbrain-theme.md).
- qBittorrentdiagnostik: [queue/peer-runbook](docs/operations/runbooks/qbittorrent-queue-and-peer-diagnosis.md).
- Aktuell verifieringsstatus: [STATUS](docs/STATUS.md).
- Finance: [testing and validation strategy](docs/architecture/finance/testing-and-validation.md),
  including invariant, simulation, paper, sandbox, failure-injection, reconciliation,
  security, UI/accessibility, performance and soak layers. No Finance test may access a
  live broker or real credentials.
  Market-data tests must prove fail-closed entitlement, immutable provenance, derived
  lineage, correction supersession and retention/deletion scope with synthetic fixtures
  until an exact provider/product is entitlement-cleared, selected and explicitly approved
  for activation; BB-071 evidence alone does not activate a provider.
  BB-075 fail-closed tests additionally assert that the runtime reports the current
  zero-cost entitlement gate rather than superseded State B wording, while every ingestion,
  storage, broker, PAPER and LIVE flag remains false. BB-076 entitlement tests cover
  zero-cost/versioned owner acceptance, capability-specific denial precedence, paid-source
  rejection and fail-closed confirmation/denied evidence.
  BB-077 EODHD tests use documented-shape sanitized JSON fixtures and cover parsing,
  impossible data, 429 retry bounds, symbol mapping, durable SQLite restart/idempotency,
  content-addressed payloads, deterministic exact-revision replay, expiry blocking,
  deletion preview/confirmation/receipt, unrelated-file protection and sanitized API/UI.
  BB-078 adds a network-free runtime-evidence projection test and runs the command against
  the deployed volume before/after restart. It exposes only request/catalog counts, symbols,
  coverage, revision IDs, payload-reference integrity, causal knowledge-time status and replay checksums; never token or
  raw payload content. The single bounded provider acquisition is runtime evidence, not an
  ordinary automated-test dependency.
  BB-079 adds hand-verifiable formula tests for returns, SMA/EMA, momentum, population
  volatility, Wilder RSI/ATR and volume features; edge, warmup/gap, deterministic checksum,
  correction lineage and explicit future-horizon/no-lookahead tests; SQLite reopen/
  idempotency, retention deletion scope, bounded feature API and responsive feature UI.
  Runtime feature builds consume only the existing local memory and must not trigger an
  EODHD request.
  BB-080 adds hand-verifiable next-open/cash/position/whole-share/fee/slippage/exit/final-equity tests; explicit future-bar/feature and same-close no-lookahead proofs; repeated-run identity/checksum/journal/curve determinism; insufficient-cash, warmup, repeated-signal, missing-next-session and retention inventory coverage. Real runs are offline maintenance commands and must not call a provider.
  BB-081 adds chronological 60/40, 70/30 and 80/20 split tests, configurable embargo/no-overlap checks, bounded-grid/isolated-peak tests, higher-cost monotonicity, explicit insufficient train/test/walk-forward evidence, future-feature invisibility, test-mutation isolation, earlier walk-forward stability, evaluation ID/checksum determinism, SQLite retention and read-only UI language coverage. Evaluation commands read local memory only.
  BB-045 policy/provenance tests use only `ExampleData` synthetic fixtures and cover
  exact provider/product scope, missing/unknown/denied/expired policy, persistence,
  post-subscription retention, immutable revision state and raw/derived lineage.
  Canonical-normalization tests additionally cover historical symbol boundaries, MIC venue
  distinction, overlap/unknown mapping rejection, decimal daily OHLCV invariants, raw and
  adjusted classification, dividends, exact split ratios, immutable revision/policy
  references, duplicates/conflicts and repeatable output. No calendar is guessed: future
  expected no-trading days, unknown missing observations and provider gaps remain distinct.
  Session/replay tests use an explicit `Europe/Stockholm` fixture calendar and verify UTC/DST,
  invalid/ambiguous local times, closure/unknown/missing/provider-gap distinctions,
  invalid-observation quarantine, historical ticker resolution, explicit dividends/splits,
  immutable revision binding, no-lookahead, range bounds and deterministic event order.
  Revision-assembly tests verify original/corrected as-of views, inclusive availability,
  immutable old revisions, explicit linear supersession, correction references/cycles,
  deterministic multi-correction order, policy/provenance, corporate-action time,
  inherited session/gap evidence and rejection of future/unavailable membership.
  Acquisition tests require exact multi-use entitlement before adapter invocation and cover
  deterministic requests/batches, synthetic-only identity, unauthorized provider/retention,
  repeated batches, overlapping pagination, correction supersession, journal evidence,
  canonical normalization, explicit provider gaps, immutable revision assembly, repeated
  replay/no-lookahead and absence of secret-bearing contract fields.
  Persistence-foundation tests cover deterministic manifests/checksums, immutable exact
  revision roundtrip, idempotent duplicate append, explicit conflicts, correction lineage,
  gap/action queries, policy-scoped enumeration/deletion receipts, partial-write rejection,
  replay compatibility and no-lookahead. Run the reproducible fixture benchmark with
  `dotnet run --project tools/BigBrain.Finance.PersistenceBenchmarks -c Release --no-build -- --full`;
  it writes only process-scoped temporary files and compares JSONL/SQLite without external IO.
  Live-observation tests use only an injected synthetic feed and explicit UTC evidence.
  They cover event/provider/received/knowledge causality, honest delay classification,
  deterministic and out-of-order delivery, duplicate/correction preservation, missing/
  outage/session events, fail-closed entitlement, immutable versioned prediction/outcome,
  cost-aware prospective metrics, no-lookahead and absence of broker/order/secret surfaces.
  The 2026-08-11 combined historical/live provider gate is documentation-only. It changes
  no domain/runtime code and adds no .NET test delta; source links, scorecard evidence and
  fail-closed language are covered by documentation verification and `git diff --check`.
  The BB-071 resolution is likewise documentation-only: the existing 376-test synthetic
  baseline is rerun, while no provider/network acceptance test is authorized.
  BB-082 follows the legitimate blocked path and changes no executable contract. Provider
  research is verified through dated primary-source links, bounded request accounting,
  documentation link/BB-ID validation and `git diff --check`; no live market-data test,
  fixture parser test or runtime deployment is applicable because no adapter/data exists.
  BB-074 tests prove fail-closed RESEARCH/no-provider/no-order API state and deterministic
  synthetic mapping. Web tests cover navigation, no-real-money and entitlement warnings,
  empty/synthetic/stale/gap/memory/chart states, native keyboard controls and no trade UI.
  Sprint 1 testar decimalprecision, invariants, UTC, provider-neutral fixture-data,
  strategy-/orderseparation, fail-closed risk/policy, NO TRADE/REJECTED-journal,
  korrelationskedja och att endast PAPER kan skapa ett lokalt paper-intent.

## Live-säkerhetsregel

Automatiska tester använder fakes/mocks och får aldrig anropa live write-endpoints. De får inte starta Jellyfin-uppspelning, ta bort eller ändra torrents, mutera Sonarr/Radarr/Prowlarr, ändra media, starta om externa tjänster eller använda riktiga credentials. Verkliga mutationer får endast ske genom dokumenterat UI-flöde efter uttrycklig användaråtgärd och separat scope.

Media har både read- och smala write-kontrakt. Påståendet att Media saknar POST/write-endpoints är historiskt och gäller inte dagens implementation. Läs [Mediamodulen](docs/modules/media.md) för aktuella gränser.
# BB-090 test additions

Network-free fixtures and tests cover macro release/knowledge cutoffs, vintage selection, forward-fill only after knowledge time, migration restart/idempotence, New York DST, regular holidays, weekends and bounded exceptional closures. Dataset/risk regression covers adjusted semantics, provider-aware promotion, typed insufficient/warmup categories and exact prediction-risk lineage. Live FRED acquisition is a bounded maintenance drill and never an automated-test dependency.

BB-090 closure adds empty/legacy/interrupted/concurrent migration coverage, rejected Macro quarantine candidates, strict evidence-class selection, Juneteenth and exact DST transition dates, immutable invalid WIKI adjusted evidence, configurable provider-neutral risk policies and deterministic multi-verdict frontend aggregation. Finalization additionally verifies the official FRED JSON `output_type=2` column schema and rejects non-vintage response shapes. The 2026-08-16 finalization regression passed 440 API, 32 Sentinel and 113 frontend tests. Production migration drills must compare `finance-evidence-counts` before/after; secrets are never test output.

# BB-091 test additions

Network-free sanitized Riksbank JSON and ECB SDMX CSV fixtures cover selected-series identity, policy/FX values, explicit base/quote semantics, malformed artifacts, rights denial, quarantine rejection, exact-artifact idempotence, cross-provider EUR/SEK tolerances and region/evidence-class as-of isolation. Live official acquisition is maintenance evidence only. Current-history bootstrap remains revised-history exploratory.
