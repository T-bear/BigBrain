# Testa BigBrain

## Välj verifiering efter scope och risk

[AGENTS](AGENTS.md#3-riskbaserad-arbetsinsats) styr risknivå, verifierbar Definition of Done
och återanvändning av evidens. Börja med berörd yta och beroenden; bredda vid konkret risk
eller bindande krav. Rena dokumentationsändringar behöver inte applikationstester.
En testmetod beskriver hur något kan verifieras, ett daterat resultat vad som faktiskt testats,
och ägarens auktorisering vad som får utföras. Ingen av dessa ersätter de andra.

Verifiera revision, miljö, scope och förutsättningar innan tidigare evidens återanvänds.
Implementerat, testat, CI-verifierat, mergat, deployat och manuellt accepterat är olika tillstånd.
[STATUS](docs/STATUS.md) anger accepterad implementation och verifieringsnivå;
[rapportkatalogen](docs/reports/REPORT-CATALOG.md) innehåller daterad evidens, inte färska PASS.
Merge- och deploymentauktorisering styrs av [AGENTS avsnitt 8](AGENTS.md#8-git-och-review-checkpoints).
Före ett separat uttryckligt ägarbeslut med ikraftträdande revision krävs exakt SHA-godkännande
för merge och separat deploymentauktorisering på alla nivåer. Dokumentationspublicering eller
merge aktiverar inte delegeringen. Efter ikraftträdandet får nivå 1 och 2 autonom teknisk leverans
inom godkänt uppdrag med relevanta kontroller; nivå 2-databasdeployment kräver separat granskning
tills backupgrinden är verifierad. Även nivå 3 får efter ikraftträdandet autonom merge och
deployment inom på förhand godkänt uppdrag och riskram, först efter godkänd faktisk oberoende
granskning av exakt kandidat-SHA/tree mot rätt main-baslinje. Inget nytt manuellt ägargodkännande
av varje SHA eller deployment krävs inom ramen. Utökad ekonomisk risk, nya kostnader, utökning
utanför mandatet, hävning av frysningar och andra reserverade beslut kräver separat ägarbeslut.
Installerade skydd, driftkontrakt, sekretess, ekonomiska gränser och frysningar gäller oförändrat.
Högst två ordinarie reviewvarv och två avgränsade automatiska reparationsförsök ersätter inte
obligatorisk verifiering. Efter ikraftträdandet ska kvarstående fel efter högst två försök leda
till säker rollback enligt driftkontraktet om den kan verifieras utan dataförlust eller brott mot
bindande skydd; annars stoppar Codex, bevarar tillståndet och rapporterar hindret. Före
ikraftträdandet gäller befintliga godkännandekrav och stoppregeln i AGENTS avsnitt 7.
Obligatorisk iPhone/PWA-acceptans
redovisas separat och kan endast godkännas av ägaren.

## Frysningar före testval

Kontrollera [frysningsregistret](docs/operations/freezes.md) innan körning:

- [Finance](docs/operations/finance-freeze-20261016.md) är **FROZEN — RESEARCH / 0 SEK / NONE;
  G3a BLOCKED, G3b NOT RUN**. Äldre testkommandon återaktiverar inte grants, modeller,
  providers, PAPER/LIVE/AUTO, brokerkoppling eller trading.
- [BB-158](docs/operations/bb158-freeze-20261023.md) är **FROZEN i sin helhet**, inklusive
  direktimport, DOI/ISBN, YouTube, Librarr, tester, säkerhetsexperiment och deployment.

Ingen fryst körning eller driftkontroll följer av denna guide. Upptining kräver nytt uttryckligt
ägarbeslut och auktoriserad scope/DoD. Befintlig drift lämnas orörd. Historiska next-, retry-,
benchmark-, outage- eller maintenance-instruktioner är inte aktuella körorder.

## Kommandon och CI

Kommandona är statiskt verifierade mot [CI-workflow](.github/workflows/ci.yml),
[Web package.json](src/BigBrain.Web/package.json) och [lösningen](BigBrain.slnx).
Tabellen beskriver konfiguration, **inte utförda eller godkända tester**.
CI körs vid push till main och pull request; lokal omfattning väljs enligt riskregeln ovan.

| Kontroll | Arbetskatalog | Konfigurerade kommandon / ansvar |
| --- | --- | --- |
| Backend, .NET 10 | Repositoryroten | `dotnet restore BigBrain.slnx`; `dotnet format BigBrain.slnx --verify-no-changes --no-restore`; `dotnet build BigBrain.slnx --configuration Release --no-restore`; `dotnet test BigBrain.slnx --configuration Release --no-build`. |
| Frontend, Node 20 | `src/BigBrain.Web` | `npm ci`; `npm run format:check`; `npm test -- --run`; `npm run build` (TypeScript och Vite). |
| Documentation, Node 20 | Repositoryroten | `node scripts/verify-documentation.mjs`: dokumentstruktur, relativa filvägar och övriga befintliga dokumentationsregler. |
| Secrets | GitHub Actions | `gitleaks/gitleaks-action@v2` med full Git-historik. |
| Lokal diff/länkar | Repositoryroten | `git diff --check`; riktad kontroll av berörda länkar **och rubrikankare**. Dokumentvalidatorn ensam verifierar inte fragment. |

Fokusera exempelvis backend med `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj
--filter FullyQualifiedName~BerördTestklass` från roten (ersätt klassnamnet med faktiskt test).
Frontend kan avgränsas med `npm test -- src/Berörd.test.tsx` från Web-katalogen.
Dessa filter är exempel, inte körorder eller nya testkontrakt. Kontrollera projekt/testnamn och
frysningar; använd inte breda Finance-sviter av rutin. Build/restore behövs när underlaget kräver det.

Formattering i CI är check-only, inte automatisk ändring eller historisk tvåpass-workaround.
`npm audit` förekommer i historisk evidens men är inte ett eget jobb i aktuell CI.
De fyra jobben bevisar inte full penetrationstestning, dependency-/container-/SAST-/supply-chain-
säkerhet, hostisolering, deployment eller fysisk acceptans. Rapportera varje relevant kontroll
som PASS, FAIL, PENDING eller SKIP med revision och avgränsning.

## Kontrakt och kvarstående verifieringsgrindar

Detta är en ingång till befintliga krav, inte ett parallellt testkontrakt. Runbooks behåller
sin egen status och godkännandegräns. Historikflytten stänger inga backlogposter eller grindar.

### Finance: integritet och exekveringsauktoritet

Endast kravbevarande under frysningen: [test- och valideringskontraktet](docs/architecture/finance/testing-and-validation.md),
[hotmodellen](docs/security/finance-threat-model.md), [modulkontraktet](docs/modules/finance.md)
och [local-first reasoner boundary](docs/architecture/finance/local-first-reasoner-boundary.md).
Rättigheter ska verifieras före adapteråtkomst. Immutable receipts, revisioner, checksummor,
replay, concurrency, reopen och avbrott/partial commit ska behålla sin dataintegritet.
Temporalitet, DST/cutoff/no-lookahead, holdout/embargo, negativa kontroller, full attempt population,
kostnadsrealism och ärliga INSUFFICIENT/NOT_EVALUABLE får inte försvagas.

[Retention/deletion](docs/operations/runbooks/finance-eodhd-retention-deletion.md) omfattar även
härledda artefakter och backup; frysning förlänger inte frister och okänd faktisk deadline är inte PASS.
Ingen destruktiv återställning eller radering får tvinga fram ett lyckat test.
Version/grammatik/admission, owner-grants, engångsjournaler, budget, auth/revokering och beständig
audit kvarstår. Förbrukade eller osäkra journaler får inte raderas, återaktiveras eller retryas.
Syntetisk proof-process och CI bevisar inte native hostisolering: timeout/cancel, output,
CPU/minne/processer och nätverk kräver egna verifierade gränser. Historiska FAIL förblir FAIL.

### Sentinel, host, backup och säkerhet

[Sentinel-arkitektur](docs/architecture/sentinel-architecture.md),
[protokoll](docs/architecture/sentinel-protocol.md) och [säkerhetsmodell](docs/security/sentinel-security-model.md)
behåller auth, signerad identitet/proof, replay/expiry/clock, schema-/svarsstorleksgränser,
minsta behörighet och audit. Mocks ersätter inte host-, systemd-, reboot-, power- eller BIOS-prov.
BB-009/BB-083:s kvarstående krav stängs inte här; se [appliance-runbook](docs/operations/runbooks/bigbrain-appliance-lifecycle.md).

[Backup/restore](docs/operations/backup-restore/README.md), [deployment](docs/operations/deployment/README.md)
och [recovery](docs/operations/codex-recovery.md) har separata drift-/dataintegritetsgränser.
Backup-/runbookgodkännandets omfattning är **UNCERTAIN** där implementerade procedurer samtidigt
beskrivs som ej godkända (DOC-2C O7). Kod eller denna guide godkänner inte restore eller driftprov.
Verifiera tillstånd före användning; skydda användardata och använd isolerade testdata.

[Säkerhetskraven](docs/security/finance-threat-model.md) och respektive accepterat kontrakt styr:
pentest är obligatoriskt före real-money LIVE och starkt rekommenderat före PAPER/execution;
striktare accepterade krav gäller oförändrat. Auth-design krävs före broker, Docker, kamera,
high-impact Home Assistant eller bredare nätverk. Bevara negativa IDOR-, injection-, XSS-,
CSRF/CORS-, resurs- och restoreprov. Extern Sentinel-pentest krävs före remote nodes/mutationer
enligt dess kontrakt. De fyra CI-jobben uppfyller inte ensamma hela den planerade säkerhetsbaslinjen.

### Media och Family

[Media-kontraktet](docs/modules/media.md) och [integrationsrunbooken](docs/operations/runbooks/media-integration-verification.md)
styr rättigheter, opaque IDs, edition/preview/bekräftelse, revalidering, idempotens och exakt
rekonsiliering efter timeout, audit/retention och säker filplacering utan överskrivning.
Nedladdning är inte slutförd import; exakt Audiobookshelf-identitet krävs.
Automatiska tester använder mocks/fixturer, inte live torrents/Arr/Prowlarr, playback,
användarprogress eller credentials. Verkliga mutationer kräver uttryckligt auktoriserat scope
samt föreskriven UI-bekräftelse; äldre playbackprov är inte generellt testtillstånd.

[Download Controls](docs/operations/runbooks/download-control-safe-removal.md) och
[ADR 0013](docs/adr/0013-safe-qbittorrent-download-removal-boundary.md)/[ADR 0016](docs/adr/0016-safe-download-control-command-and-partial-batch-boundary.md)
behåller destruktiva, Arr-, Retry- och partial-batch-grindar; BB-019–023:s destruktiva verifiering
är inte godkänd genom denna guide. Mediekontrolls merge bevisar inte full fysisk acceptans.
BB-158:s frysning gäller även när äldre importtester finns i arkivet.

[Family View](docs/architecture/family-view-family-coordination.md) använder källmodulernas
ägarskap; [Meal Planner](docs/knowledge/meal-planner.md) och [Shopping List](docs/knowledge/shopping-list.md)
får inte muteras av skollunchvisning. [Skollunchrapporten](docs/reports/features/family/school-meals-preparation-20261023.md)
bär offline-/HTTP-fixtureevidens: default-off källor, inga verkliga HTTP-anrop i offline-tester,
inget livebevis från testdata. Skollunch v2:s merge/CI innebär inte deployment, extern aktivering
eller fysisk acceptans; åtkomstvillkor och ägarbeslut måste verifieras separat.

Övriga ingångar: [Calendar](docs/modules/calendar.md), [Heroma](docs/operations/runbooks/heroma-schedule-import.md),
[Calendar-verifiering](docs/operations/runbooks/calendar-verification.md) och
[Dashboard-verifiering](docs/operations/runbooks/dashboard-widget-framework-verification.md).

## Manuell, fysisk och operativ acceptans

[Temakontraktet](docs/design-system/theme-contract-v1.md) och [manuell verifiering](docs/design-system/manual-verification.md)
behåller fokus/tangentbord, touchytor (44 px där kontraktet kräver; BB-097:s uttryckliga tertiary
40 px-undantag kvarstår), dock, reduced motion, textförstoring, lång svensk text och responsivitet.
Verifiera obsidian-gold, forest-night och arctic-wind. Manual-guidens äldre temanamn är dokumenterad
instruktionsdrift (O10), inte ett nytt temabeslut eller godkännande.

Normal viewport och nåbar sista handling måste granskas; en sammanfogad helsidesskärmbild eller
DOM-klick bevisar inte fysisk användbarhet. iPhone/PWA, verklig uppspelning, Tizen och driftkontroller
är separata från CI och kräver tillämplig auktorisering. Ratad floating player ska inte återinföras
via gamla testinstruktioner. Begränsat timer-/Finance-UX-godkännande är inte hela modulens acceptans.
Kvarstående fysisk/designmässig acceptans är **UNCERTAIN** (O5) tills avgränsad evidens finns.
Denna konsolidering registrerar ingen ny ägaracceptans eller aktuell runtimeverifiering.

## Historik och spårbarhet

Hela den tidigare guiden finns i [TESTING före DOC-2C](docs/history/testing-before-doc-2c-20261024.md).
[DOC-2C-inventeringen](docs/reports/documentation/doc-2c-testing-index-inventory-20261024.md#fas-b--förlustfri-konsolidering-2026-10-24)
mappar 85 avsnitt och 12 orubricerade områden, inklusive misslyckade tester och kvarstående krav.
Checkpointbundna fullsvits-/fortsättningsinstruktioner är historiska; invariants finns kvar ovan
med kontraktslänkar. Använd [rapportkatalogen](docs/reports/REPORT-CATALOG.md) för daterad evidens
och [dokumentationsindexets auktoritet](docs/indexes/documentation.md#authority-order) vid konflikt.

<a id="bb-130d1-backend-quality-gate-baseline--2026-09-14"></a>

Kompatibilitetsankare för tre äldre rapporter: [BB-130D1:s historiska toolchain-record](docs/history/testing-before-doc-2c-20261024.md#bb-130d1-backend-quality-gate-baseline--2026-09-14).
Detta är daterad baslinje, inte aktuell testorder eller PASS.
