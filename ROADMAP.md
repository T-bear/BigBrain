# BigBrain Roadmap

Avstämd 2026-10-24 i DOC-2B mot accepterad main `ab6a84b3bde807fe5ec38bfcba35ae9b85c130c5`.
Produktägaren äger riktning och prioritering. Detta dokument beskriver produktmål och större steg;
[Backlog](docs/BACKLOG.md) äger kvarstående arbete/DoD och [STATUS](docs/STATUS.md) daterad evidens.
[Dokumentauktoriteten](docs/indexes/documentation.md#authority-order) och [AGENTS](AGENTS.md) gäller.
Ingen ny prioritering, epic eller sprint startas genom konsolideringen. STABILIZATION_PLAN.md
är inte en parallell roadmap och ska inte återskapas; BB-011 kvarstår UNCERTAIN.

## Aktuell beslutad riktning och produktprinciper

- KISS och befintliga modulgränser gäller. Family View är ett koordinationslager över källägande
  moduler, inte en konkurrerande datakälla eller ännu en fristående widgetdashboard. Calendar,
  Meal Planner, Shopping List, skolkällor och hushålls-/medlemskontext behåller ägarskap.
- Skollunch är ett avgränsat steg i Family-inriktningen, inte acceptans av hela Family-epiken.
  V1, v2-plan och default-off v2-implementation är mergade; v2-CI är verifierad enligt
  [STATUS](docs/STATUS.md#family--skollunch). Deployment, källors automatiserings-/cachevillkor,
  extern aktivering och manuell live-/iPhone/PWA-acceptans är separata kvarstående beslut.
  Historiska offline-menyer är inte aktuella matsedlar. Ingen extern hämtning aktiveras här.
- Bevara less-is-more och `SUMMARY → EXPLANATION → RAW EVIDENCE`: en rubrik per informationsnivå,
  data först, korta kontextetiketter och sekundär teknisk evidens vid behov. Obsidian Gold är en
  ägarpreferens och Family en accepterad designriktning, inte generell acceptans av alla komponenter.
  Befintliga avvisade spelar-/timeruttryck och osäkra visuella acceptanspunkter finns i Backlog.
- Äldre BB-130-före-Finance-prioritet är historik: BB-130 är COMPLETE / EXIT APPROVED.
  Uppskjuten skuld, säkerhetsgrindar och manuell acceptans avslutas inte av den stängningen.
  BB-119 korrigerade det dåtida BB-118-readinessfelet; ingen färsk driftverifiering görs här.

Källor: [R015:s dokumentansvar och skyddsgränser](docs/history/roadmap-before-doc-2b-20261024.md#authority-and-current-direction--bb-130a-2026-09-05),
[Family-produktmål](docs/architecture/family-view-family-coordination.md),
[ägarens UX-principer](docs/history/backlog-before-doc-2b-20261024.md#bigbrain-wide-design-system-and-less-is-more-hierarchy),
[SUMMARY-principen](docs/history/roadmap-before-doc-2b-20261024.md#bb-088--prospective-daily-cadence-and-finance-ui-v10-hierarchy-implemented-2026-08-15).

## Planerat men ännu inte auktoriserat arbete

Tabellen är bevarad produktinriktning, inte en ny prioriteringsordning eller genomförandeauktorisering.
Konkreta beroenden och kvarstående DoD ligger i [Backlog](docs/BACKLOG.md).

| Riktning | Omfattning och beroendegräns |
| --- | --- |
| Family Coordination | Member association, skolschema/kalender/lov/studiedagar, attendance, Family Context, hard/soft/information evaluation, Day/Week, proveniens/färskhet, manuellt override, privacy/security och sparsamma notiser. Beroenden: BB-029, BB-036, BB-027, auth-hardening samt Home Calendar/swipe. Skollunchens merge är inte leverans av dessa delar. |
| Skolmedveten middagsplanering | Samordna källornas luncher med hushållets preferenser, historik, variation, rester, budget och ingredienser. Maträttslikhet är en mjuk begränsning, starkast samma dag; manuella val vinner. Skolmåltider skapar inte inköpsingredienser. Ingen jämförelsemotor eller AI auktoriseras här. |
| Delad familjedata och Dashboard | BB-036:s versionssatta realtidskontrakt med Inköpslista först; ADR, auth, konflikter, idempotens, reconnect och stale-state-resync före implementation. BB-027:s identitet/ägarskap, profilsynk/roller och layouter kräver separata kontrakt. Ingen ny databas/synkplattform följer av planeringen. |
| Home/Calendar och gemensam UX | BB-029 extern synk/personliga kalenderfunktioner, tillgänglig presentation av förflutna dagar och mobil swipe som samexisterar med back-gestures, tangentbord och synliga kontroller. Ingen godkänd specifik interaktion antas. Designsystem/UX Lab kräver avgränsad ägaracceptans. |
| Media | BB-039 Controller, Download Controls kvarstående säkra mutations-/retention-/Arr-scope, BB-033/040/041 kvalitativ acceptans, BB-014/018 Smart Shuffle/Tizen. BB-155 metadata, BB-156 latens och BB-157 availability är uppskjutna; verklig providersemantik och rättigheter före implementation. BB-158 är separat fryst. |
| Identitet och säkerhetskonsolidering | Standardsbaserade passkeys/WebAuthn, trusted devices, bootstrap/recovery/revocation och step-up enligt faktisk användarmodell. MAC är inte autentisering; biometrin stannar på enheten och inget eget biometriprotokoll införs. Privat nätverksidentitet ersätter inte applikationsidentitet/auktorisering. |

[Ursprungligt Family-beslut](docs/history/roadmap-before-doc-2b-20261024.md#future-family-view--family-coordination-epic-planned-only)
och [Backlogs produkt-, UX- och auth-krav](docs/history/backlog-before-doc-2b-20261024.md#future-product-ux-authentication-and-meal-planner-capture)
bevaras. Äldre meningar om aktiv Finance-prioritet överordnas av senare frysning; det skapar inte
ett nytt generellt Family-uppdrag.

## Säkerhets- och beroendegrindar

- Ingen broker/trading, Docker-/kamerakontroll, high-impact Home Assistant eller bredare
  nätverksexponering utan accepterad authentication/authorization-design.
- Säkerhets-/pentestbaseline är **PLANNED**, inte implementerad: kontinuerliga secrets/dependency/
  SAST/container/API/header/supply-chain/configuration-kontroller samt kontrollerade black-/grey-box-prov
  av Web/API/auth, IDOR/injection/XSS/CSRF/CORS, felaktig indata/traversal/resurser, host/container/volymer,
  Sentinel transport/proof/replay, Finance CSV/ZIP/proveniens/revision/mode-grindar och recovery/persistens.
  Potentiellt destruktiva prov kräver isolerade miljöer/data. Baseline är obligatorisk före real-money
  LIVE och stark grind före meningsfull PAPER/execution; striktare accepterade domänkrav gäller fortsatt.
- BB-009:s Sentinel-konformitet/ADR-disposition, BB-083:s host-/reboot-/power-evidens och Media-
  mutationernas preview/explicit confirmation/revalidering/audit/retention förblir öppna där evidens saknas.
  Sprintacceptans av icke-destruktiva flöden godkänner inte destruktiva operationer.
- Tester/CI och merge är inte deployment, driftsintegritet, manuell UX-acceptans eller nya rättigheter.
  Ingen blockerare får stängas genom dokumentationsflytt.

[Original säkerhetsmilsten](docs/history/roadmap-before-doc-2b-20261024.md#security-and-penetration-testing-gate--planned),
[kvarstående konkreta krav](docs/BACKLOG.md#säkerhet-drift-och-arkitekturgrindar).

## Fryst arbete

[Registret](docs/operations/freezes.md) länkar kanoniska ägarbeslut; det är inte en separat auktoritet.
Befintlig drift lämnas orörd. Upptining kräver nytt uttryckligt ägarbeslut och auktoriserad scope/DoD,
inte gamla next/current-instruktioner. Det ger inte automatiskt merge/deployment eller körningsrätt.

| Område | Bevarad riktning och gräns |
| --- | --- |
| Finance — FROZEN sedan 2026-10-16 | [Kanoniskt beslut](docs/operations/finance-freeze-20261016.md). RESEARCH / 0 SEK / NONE; G3a BLOCKED, G3b NOT RUN. Rättigheter, retention/disposal, integritet, hostisolering, journal/enrollment/authority, science/holdout och kvarstående driftkrav gäller fortsatt. Ingen ny källa, grant, modellkörning, PAPER/LIVE/AUTO eller brokerkoppling. |
| Hela BB-158 — FROZEN sedan 2026-10-23 | [Kanoniskt beslut](docs/operations/bb158-freeze-20261023.md). Accepterad direktimport/DOI bevaras; YouTube/ISBN/konvertering och review-isolering är inte färdig produkt. Aktuell deployment/aktivering/import/recovery är inte omverifierad; ingen automatisk felsökning, retry, merge eller deployment. |

Finance behåller långsiktigt ägarbeslut **“Finance may be creative with hypotheses, but conservative
with its own construction.”** Befintlig Finance validerar hypoteser och bevisar deterministiskt;
modellen får inte generera kod/plugins/exekveringsmekanismer/risk-/sciencepolicy eller egen auktoritet.
Local-first och 0-SEK-gränsen kvarstår; extern AI är valfri efter separat rights/export/auth/cost-review,
inte ett krav på kvalificerad lokal användning eller automatisk fallback.

Den frysta långsiktiga riktningen är lokal forskare → beständig loop/historik → prospektiv validering →
framtida långsiktig och aktiv/högrisk PAPER-forskning → obevakad översikt/drift. Detta är mål, inte
påstående om levererad kontinuerlig forskning. Aktiv forskning kan avse intradag till flera dagar,
meningsfull första bedömning kring en månad och prospektiv utvärdering upp till ungefär ett år.
Realistisk spread, avgifter, slippage och förluster krävs; compounding är inget avkastningslöfte och
**zero trades is valid**. Fryst [Finance master roadmap](docs/architecture/finance/master-roadmap.md)
bär domänsekvensen; [Backlog](docs/BACKLOG.md#finance--fryst-arbete-och-kvarstående-krav) behåller öppna DoD.
[Ägarbeslutets original](docs/history/roadmap-before-doc-2b-20261024.md#finance-product-direction--owner-instruction-2026-09-29).

## Historiska beslut och tidigare prioriteringar

Alla 25 originalavsnitt finns i [Roadmap-arkivet](docs/history/roadmap-before-doc-2b-20261024.md).
BB-130:s stabiliseringssekvens och BB-131/132:s bounded next-step-ledger är historik; genomförd
implementation eller gammal commissioning återauktoriserar inget fryst arbete.
[DOC-2B:s fullständiga mappning](docs/reports/documentation/doc-2b-backlog-roadmap-inventory-20261024.md#fas-b--förlustfri-konsolidering-2026-10-24)
visar original och destinationer. Ägarprinciper och öppna skyddsgrindar ovan förblir synliga utan att
ändra kanonisk dokumentauktoritet, prioriteringar eller acceptera en ny produktfunktion.
