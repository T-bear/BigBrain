# Finance — formell frysning och återupptagning, 2026-10-16

## A. Läs detta först

**FROZEN — OWNER DECISION REQUIRED TO RESUME.** Ägarbeslut gäller hela Finance från
2026-10-16 på obestämd tid. Denna kanoniska överlämning publiceras på befintlig review-branch;
frysningen är verksam som ägarinstruktion och väntar inte på merge. Publication accepterar inte review-koden.

- Accepterad main: `70b659f772b7b7883a5716786859f1e7833a5634`, tree
  `6433c956ff87bc8434a9a8fff6c5294fe45b258d`. Historisk Qwen och recurring research är mergad kod.
- Bevarad review-baslinje: `545713d2ddbf1c4060ca6421b728541b32ddcd7d`, tree
  `37b20ed4530d2fd32a45055291143fe07eb8cfd9`, branch `review/finance-historical-qwen-deployment`.
- Tekniska grindar kvarstår: **G1 PASS / G2 PASS / G3a BLOCKED / G3b NOT RUN**.
  API-deployment är inte bevis för en säker Qwen-körmiljö. Ingen historisk Qwen-iteration är slutförd.
- Review-only: systemd-preflight, separat diagnostik, förberedande Compose-fragment och
  `MountFlags=private`. Ingen ytterligare maskering är motiverad eller implementerad.
- Senast dokumenterad drift: AAPL/Alpaca observation aktiverad, forskning pausad. Detta uppdrag
  ändrar eller stoppar inte tjänster. Aktuell hälsa, mappningens giltighet och insamlingsutfall är inte omverifierade.
- Ingen felsökning, implementation, nya experiment, merge, deployment eller inferens ska återupptas.
  Vid ett framtida upptiningsbeslut: följ avsnitt I och invänta därefter uttrycklig prioritering.
- Finance förblir **RESEARCH / 0 SEK / NONE**. Äldre planer/kommandon är bevarad historik, inte körorder.

Primära underlag: [AGENTS](../../AGENTS.md), [dokumentauktoritet](../indexes/documentation.md),
[forskningskontrakt](../architecture/finance/research-learning-contract.md),
[G3-grindar](https://github.com/T-bear/BigBrain/blob/378d43e9a33b217fef30de5d4f74ef58a0a01926/docs/operations/finance-historical-qwen-g3-checklist.md),
[daterad operationsrapport](https://github.com/T-bear/BigBrain/blob/378d43e9a33b217fef30de5d4f74ef58a0a01926/docs/reports/features/finance/historical-qwen-operation-20261016.md),
[hosting-runbook](https://github.com/T-bear/BigBrain/blob/378d43e9a33b217fef30de5d4f74ef58a0a01926/docs/operations/runbooks/finance-historical-hosting.md). Detta dokument ändrar inga tekniska kontrakt.

## B. Ägarbeslut och auktoritet

Produktägaren har uttryckligen beslutat om omedelbar frysning av **hela Finance**, inte enbart
historisk Qwen. Det äldre villkoret att självständig forskning först måste vara verifierat igång
är uttryckligen ersatt. Endast ett nytt uttryckligt ägarbeslut kan häva frysningen eller auktorisera
en avgränsad åtgärd under den. Ett allmänt återhämtningsmeddelande eller en gammal runbook gör inte det.

Utan nytt beslut är nya funktioner/sprintar/arkitekturändringar, fortsatt felsökning/implementation,
nya Finance-tester/experiment utöver bevarandeverifiering, modellkörning, forskningsaktivering/sessioner,
deployment, migration, driftändring, merge av utestående Finance-kod och automatisk återupptagning
förbjudna. Även frivilliga optimeringar och automatiskt startade felsökningskedjor omfattas.

Detta uppdrag tillåter dokumentationsinventering, sanerad frysningsöverlämning, dokumentkontroller
och commit/push. Under frysningen får dokumentationen bevaras och korrigeras för att korrekt återge
beslut och status; det är inte tillstånd till nya tekniska utredningar eller runtimeåtgärder.
Nya observationer från ägare/arkitekt ska tillskrivas källan. Säkerhets-, rättighets-, retention-,
integritets- och sekretesskrav gäller fortsatt; frysningen pausar inga externa skyldigheter.

Detta är ingen instruktion att stoppa/starta/konfigurera befintliga tjänster. Om fortsatt aktiv
insamling eller en konkret rättighets-/säkerhetshändelse kräver åtgärd ska ägaren fatta ett separat,
avgränsat driftbeslut. Varken akutåtgärd, automatisk radering eller grantförnyelse följer av dokumentet.

## C. Systemarkitektens överlämningsrapport — ChatGPT

Följande är den av produktägaren tillhandahållna arkitektöverlämningen, tillskriven ChatGPT.
Den är inte Codex egen hostverifiering. Saklig innebörd är oförändrad.

### Arkitektens nulägesbedömning, 2026-10-16

Finance har en omfattande befintlig implementation för marknadsobservationer, dataproveniens, tidsmässig integritet, vetenskaplig utvärdering, begränsade forskningssessioner, lokal modellintegration och skyddad exekveringsauktoritet.

Arkitekturen bygger på tydligt separerade ansvar, begränsade exekveringsrättigheter, reproducerbar evidens och att RESEARCH inte innebär handelsauktoritet.

Den accepterade historiska Qwen-implementationen har mergats till main och Finance API har driftsatts och hälsokontrollerats. Detta innebär inte att historisk Qwen-exekvering är driftsklar.

Den aktuella releaseprocessen befinner sig vid G3a, där faktisk host-isolering och skyddad exekveringsauktoritet ännu inte är verifierade. G3b har inte körts.

En begränsad MountFlags=private-korrigering finns endast på review-branchen. Den befintliga monteringspolicyn är fortfarande fail-closed.

Produktägaren har genomfört en lokal, skrivskyddad granskning av den befintliga privata diagnostikfilen. Resultatet bekräftade 65 monteringar och 16 policyavvikelser: 12 nsfs, 2 tmpfs, 1 binfmt_misc och 1 hugetlbfs. Samtliga 16 hade en förälder i snapshoten. Tolv nsfs-poster delade förälder, och binfmt_misc-avvikelsen hade ett monteringsmål som även förekom i en annan post.

Detta visar monteringsrelationer men bevisar inte effektiv skrivåtkomst, monteringsursprung, säker maskeringspunkt eller godkänd host-isolering.

Produktägaren har uttryckt att de 16 avvikelserna är acceptabla ur dennes perspektiv. Det är en uttalad riskacceptans, inte teknisk PASS eller tillåtelse att generellt försvaga säkerhetskontroller.

Finance ska inte fortsätta genom G3a nu. Det är ett medvetet produktägarbeslut om frysning, inte ett påstående att tekniken har blivit färdig.

Vid återupptagning bör första aktiviteten vara att validera source of truth och presentera ett begränsat antal beslutspunkter för ägaren, inte automatiskt återuppta den tidigare felsökningskedjan.

Finance förblir RESEARCH / 0 SEK / NONE.

Slut på ChatGPT:s överlämningsrapport.

## D. Codex självständiga tekniska överlämning

Bedömningen är en riskbaserad kod-/dokumentinventering, inte en ny säkerhetscertifiering eller en
fullständig line-by-line-review. **V** = verifierat i Git/kod i detta uppdrag;
**D** = dokumenterat eller ägarrapporterat, inte självständigt omverifierat i drift;
**O** = okänt. Inga antaganden används för att fylla ett beviskrav. Inga fristående externa Finance-granskningar används.
Källfilerna nedan avser main-baslinjen om de inte uttryckligen anges som review-only.

| Område | Självständig slutsats, evidens och begränsning |
| --- | --- |
| Arkitektur/modulgränser | **V:** [ADR0038](../adr/0038-finance-research-learning-authority-contract.md) ger Finance vetenskaplig auktoritet, [ADR0039](../adr/0039-local-first-research-reasoner-boundary.md) begränsar modellen till förslag och [ADR0040](../adr/0040-local-reasoner-owned-worker-lifecycle.md) begränsar processkontroll till ägd worker. [LocalRecurringResearchComposition](../../src/BigBrain.Finance.Hosting/LocalRecurringResearchComposition.cs) kopplar befintlig Brain-runtime till Finance via särskilt hosting-projekt; ingen andra forskningsdatabas behövs. Detta är gränser i kod, inte bevis att hosten uppfyller dem. |
| Funktioner/datamodell | **V:** [FinanceSchemaMigrations](../../src/BigBrain.Api/Finance/FinanceSchemaMigrations.cs) innehåller95 finite session,96 observation receipts,97 daily shadow,98 mapping manifests/bindings och99 recurring program/exclusion. [FinanceLearningLedger](../../src/BigBrain.Api/Finance/FinanceLearningLedger.cs), [FinanceBacktestStore](../../src/BigBrain.Api/Finance/FinanceBacktestStore.cs) och [FinanceRobustnessStore](../../src/BigBrain.Api/Finance/FinanceRobustnessStore.cs) äger beständig evidens. Schema99 i koden bevisar inte aktuell produktionsschema; inga migrerande konstruktörer kördes. |
| Vetenskap/temporalitet | **V:** [FinanceMarketObservations](../../src/BigBrain.Api/Finance/FinanceMarketObservations.cs) tar acquisition/ingestion från Finance-klockan och kontrollerar rättigheter/lineage i transaktion; `MarketKnowledgeAt` förseglar cutoff och är inte read-only-diagnostik. [DailyFeatureEngine](../../src/BigBrain.Modules/Finance/DailyFeatureEngine.cs), [DeterministicBacktesting](../../src/BigBrain.Modules/Finance/DeterministicBacktesting.cs) och [RobustnessEvaluation](../../src/BigBrain.Modules/Finance/RobustnessEvaluation.cs) är befintlig beräkningsväg. [ADR0024](../adr/0024-finance-deterministic-research-backtest-evidence.md)/[0025](../adr/0025-finance-immutable-chronological-evaluation-evidence.md) skiljer kronologisk, embargo-/holdoutstyrd forskning från lönsamhetsbevis; DSR/PBO är inte styrkta. |
| Datakällor/rättigheter | **V:** [ADR0021](../adr/0021-finance-provider-neutral-market-data-and-retention-gate.md), [0022](../adr/0022-finance-owner-accepted-zero-cost-personal-research.md), [0027](../adr/0027-finance-external-dataset-quarantine-and-promotion.md), [0028](../adr/0028-finance-provider-tagged-backup-restore-and-cleanup.md) kräver källa, capability, provenance och ärvd retention. [Marknadskontraktet](../architecture/finance/market-data-memory-and-provenance.md) skiljer Alpaca IEX/raw från listing MIC och från historisk import. Ägarens Alpaca-riskacceptans är ingen garanti om leverantörens retention. **O:** nu gällande kontostatus/deletion notices; ingen extern kontroll eller ny dataläsning utfördes. |
| Mapping/revisioner | **V:** [FinanceObservationMappings](../../src/BigBrain.Api/Finance/FinanceObservationMappings.cs) separerar `RequireHistorical` och `RequireCurrent`, binder oförändrade snapshots och högst sju dagars assertion. Legacyhistorik får inte förlängas retroaktivt. Fyra instrument/fyra versioner per instrument och bevarandekrav på tidigare snapshots är avsiktliga begränsningar, inte automatisk corporate-action-hantering. SHADOW v1 accepterar inte blandad mappingproveniens; se [H-rapporten](../reports/features/finance/bb-132h-versioned-observation-mapping-20261010.md). |
| Scheduler/governor | **V:** [FinanceObservationRuntime](../../src/BigBrain.Api/Finance/FinanceObservationRuntime.cs) har recovery-gate, single-flight och full cadence före första cykeln. [FinanceResearchScheduler](../../src/BigBrain.Api/Finance/FinanceResearchScheduler.cs) och [governor](../../src/BigBrain.Api/Finance/FinanceResearchResourceGovernor.cs) har separata science-, maintenance- och resursgrindar. Historiska program ingår inte i prospective scheduler. Aktiverad datainsamling innebär inte AI-forskning. |
| Auktoritet/återstart | **V:** [RecurringResearchAuthority](../../src/BigBrain.Api/Finance/RecurringResearchAuthority.cs) kontrollerar hashkedja, grant, monoton konsumtion och exakt journal-/SQLite-match; append och fsync sker före DB-commit. [FinanceRecurringResearch](../../src/BigBrain.Api/Finance/FinanceRecurringResearch.cs) reserverar arbete och stoppar osäker exekvering utan refund/retry. Vanlig restart är villkorat säker inom dokumenterad custody-/restoremodell, inte vid rollback av både DB och journal. Administratörsskydd/restore-exkludering är ett faktiskt driftberoende; saknad journal får aldrig ersättas med tom fil. |
| Historisk Qwen | **V:** [FinanceHistoricalResearch](../../src/BigBrain.Api/Finance/FinanceHistoricalResearch.cs) binder exakt WIKI-källa/748 sessioner och faktisk acquisition; första500 utgör förbestämd forskningsperiod. [Forskningskontraktet](../architecture/finance/research-learning-contract.md#historical-qwen-admission--implementation-checkpoint-2026-10-16) skiljer historisk admission från prospective data och beskriver development-only modellprojektion. Exponering av historisk holdout är Unknown, inte garanterat orörd. En ändlig iteration är implementerad; kontinuerlig historisk forskning eller träning av modellvikter är inte levererad genom detta. |
| Lokal runtime/isolation | **V:** [LocalReasonerRuntime](../../src/BigBrain.Brain/LocalReasonerRuntime.cs) återanvänder bounded BRF1, cancellation och ägd worker cleanup; [containment.h](../../src/BigBrain.LocalReasoner.Worker/containment.h) kräver cgroup och begränsar filåtkomst/syscalls. **D/O:** binärer/loader/provisionering har daterad evidens men ingen komplett slutlig host-PASS. Review-only systemd-test är inte modellmiljö eller authority-check. |
| Test/CI | **D:** [historisk integrationsrapport](../reports/features/finance/historical-qwen-integration-20261016.md#evidence) anger Release build PASS,1215 API PASS/8 explicita modell-SKIP,231 tidigare fokuserade PASS och ett ytterligare modellbytes-test PASS. [Operationsrapporten](https://github.com/T-bear/BigBrain/blob/378d43e9a33b217fef30de5d4f74ef58a0a01926/docs/reports/features/finance/historical-qwen-operation-20261016.md) anger main-CI38863227447 SUCCESS och26 fokuserade mount-tester PASS. **V:** testkällor finns, bl.a. [historiska tester](../../tests/BigBrain.Api.Tests/FinanceHistoricalResearchTests.cs), [recurring](../../tests/BigBrain.Api.Tests/FinanceRecurringResearchTests.cs), [mapping](../../tests/BigBrain.Api.Tests/FinanceObservationMappingTests.cs). Inga tester/CI-körningar återutfördes här; modellfria fixture-resultat är inte produktionsinferens. |
| Kodkvalitet/teknisk skuld | **V:** explicit versions-/checksum-/fail-closed-modell och återanvända evaluatorer minskar implicit omtolkning. Finance persistence använder en omfattande partial `EodhdMarketMemory` och SQL/hosting har flera tätt kopplade invariants; ändringar kräver riktad concurrency/reopen-verifiering. Detta är underhållsrisk, inte påvisat nytt fel eller refaktoruppdrag. **D:** [BB-130C identity-fix](../reports/features/finance/bb-130c-robustness-selected-result-identity-fix-20260909.md) och [testing map](../../TESTING.md) bevarar kända regressionsgränser. Stale statusformuleringar är dokumentationsskuld, hanterad här genom tydlig aktuell överordnad hänvisning utan historikomskrivning. |
| Utestående leverans | **V:** historisk/recurring applikationskod är ancestors till main. Den aktiva review-deltan består av driftdokumentation, tre förberedande infrastrukturartefakter, två Python-kontroller och deras tester; ingen ny Finance-evaluator. Andra reviewbrancher är bevarad besluts-/driftevidens, inte implicit mergekö. Exakta identiteter finns i E. |

### Auktoritet och dokumentmotsägelser

Ägarbeslutet styr om arbete får ske, utan att sänka accepterade ADR-/säkerhetskrav. Tekniska kontrakt
har fortsatt sin befintliga auktoritetsordning. Äldre rubriker med “review implementation”, “NOT STARTED”,
en tänkt första Twelve Data-källa eller “enda aktiva EODHD-källa” är daterade milstolpar. De får inte
ersätta Git-ancestry, senare daterad Alpaca-driftsrapport eller detta frysningsbeslut.
Det äldre villkorade frysningsmålet i STATUS ersätts uttryckligen av B, inte av en påstådd teknisk framgång.
Historiska recovery-instruktioner om “nästa exakta åtgärd” är vilande. Ingen Finance-plan i ROADMAP,
BACKLOG eller ett kontrakt är under frysningen en aktivitetsauktorisation.

## E. Exakt Git- och kodinventering

Inventering utförd efter fetch under överlämningen2026-10-16–17; ägarbeslutets datum är2026-10-16.
Huvudidentiteterna i A matchade lokal Git och GitHub. Alla lokala relevanta Finance-branchheads
matchade motsvarande hämtade origin-ref; inga opublicerade Finance-commits identifierades.
`gh` saknas lokalt; read-only GitHub-connector `search_prs`, repository T-bear/BigBrain, state=open,
query=is:open, topn100 returnerade inga öppna PR:er. Detta är en daterad inventering, inte garanti
mot senare PR-/branchändringar. Ingen PR skapades eller ändrades.

Följande tabell är fryst **före dokumentationscommiten**. “I main” är verifierad Git-ancestry;
review utanför main är inte accepterad kod. Den aktiva branchens nya freeze-SHA/tree hämtas ur
publiceringscommiten med ämne `docs(finance): freeze Finance by owner decision` och slutrapporten,
inte genom en självrefererande hash i dokumentet.

| Branch | HEAD före frysning | Tree | Status mot main |
| --- | --- | --- | --- |
| `bb-130c/finance-persistence-characterization` | `888ee57fa0c25ee5d3a733c9e2bbff77294fb3aa` | `36aaeb0e9c3f80c95094e9611fcfd1b223365f47` | I main; 0 ahead / 105 behind |
| `bb-131a/research-learning-architecture` | `1eb50979a7923f115b4b182fbee15de735ea1f7e` | `bef025f159e8fe478f1dc191cd6e122e362b728d` | I main; 0 ahead / 59 behind |
| `bb-131b/synthetic-contract-replay-proof` | `56ad7fd0290471bec926b2008384951a9bb8286a` | `e7988a8f2b88a64fcb403142d9da1c0214e604d5` | I main; 0 ahead / 56 behind |
| `bb-131c/persistent-learning-ledger` | `55a05bf967044f16aeb8d5482f0c158c09b2883f` | `c0942e47946c9572015e1d1320a25be0c6de6406` | I main; 0 ahead / 53 behind |
| `bb-131d/local-first-reasoner-boundary` | `8fbf1f497b7dc3b2cdceca7a02b752a8d7ce58e7` | `0781add2bdb3756941a6ce5c824f247d713247d9` | I main; 0 ahead / 50 behind |
| `bb-131e/local-reasoner-contract` | `b7e3d226644576e84b59135f285bccdc36626c52` | `bbe3f43e4da91fb36654c615274f48f4dbae1631` | I main; 0 ahead / 47 behind |
| `bb-131f/local-reasoner-runtime-isolation` | `1e003705c08000f33b97f3d0d5dc7f35546e4d1e` | `a129504ad121f8ad695ce18cc74aecc66dbac415` | I main; 0 ahead / 44 behind |
| `bb-132a/first-local-language-model` | `960a12ddb04349fd54b5b90a550b9e1e4be9419c` | `9cc93f14f4b80cc9dafcd027d7c80833626059bc` | I main; 0 ahead / 37 behind |
| `bb-132b/persistent-research-loop` | `8df4541fdcd300b0d334d519c221e4189dd32084` | `ca278b2a34095abd72feccaaf02e73474355bece` | I main; 0 ahead / 33 behind |
| `bb-132c/local-model-research-sequence` | `78517da41e8d4e7642029daebde544a951da092d` | `d2c0222122e9dcaa5f4f216e5983e85ddc3798dc` | I main; 0 ahead / 29 behind |
| `bb-132d/real-market-observation-foundation` | `85163a9a648a65afded90a72f9433f2bc5389c58` | `bbf65c00792b74db54524bd8f27d6a83d78e029a` | I main; 0 ahead / 26 behind |
| `bb-132e/prospective-shadow-evaluation` | `5e755c0c7e38c1e8843257b7222e3fe81b3c4f43` | `ddfdcfe5ea8f59504f9bf99371b50172d7e29d95` | I main; 0 ahead / 21 behind |
| `bb-132f/live-market-observation-runtime` | `a3ef3cd8a8ac5b1199a1cabc5001dc8d5ffff08c` | `1d121bb96f9fad8a097cf32655669d202c44af2a` | I main; 0 ahead / 18 behind |
| `bb-132f1/controlled-activation-gate` | `999bc684076761b769f02d0b15a9b8cfa051210a` | `0206eb593c3fcd1493c71583b37316b4705b75c5` | I main; 0 ahead / 15 behind |
| `bb-132h/versioned-observation-mapping` | `bd271824bec656d302a289b3f3f053b73013bf57` | `72649fe9c5dd9fb7c2bb7fe0ed0fa0dd9ac06547` | I main; 0 ahead / 11 behind |
| `finance/historical-qwen-research` | `87b08d3d074beb02a4d1d41e467c4e0c28a057bf` | `6433c956ff87bc8434a9a8fff6c5294fe45b258d` | I main; 0 ahead / 1 behind |
| `finance/recurring-research` | `ac34a7eb80fca23b2c3804d5ce7c2e29978178a4` | `84b751733b63eb54c926bb380fe59f1686f4230b` | I main; 0 ahead / 3 behind |
| `review/finance-aapl-mapping-compatibility` | `a31544e708e3b059fd2ea8b62b74438085091193` | `675edd2d8802a6ae3fad07017019145e373ffd5c` | Ej ancestor; bevarad review; 1 ahead / 13 behind |
| `review/finance-automation-release` | `340aaf3df13ea2b1f00c928eab68936150f01baa` | `0a4520c238fc7579f89760b9f22c61d124f45039` | Ej ancestor; bevarad review; 1 ahead / 9 behind |
| `review/finance-bb132i-operational-readiness` | `4eaf9cef6388af29d9509c71bad93ab351b4f474` | `48298f49262197297e28d3b38d2d341636515d02` | Ej ancestor; bevarad review; 1 ahead / 10 behind |
| `review/finance-bb132j-evidence-recovery-preflight` | `ca05b5b1ec50fca377208dd36fc18c7ef390550f` | `e5427884a4e7b85a48c0dce7e1666a7a722a10f3` | Ej ancestor; bevarad review; 1 ahead / 10 behind |
| `review/finance-bb132k-consolidated-readiness` | `763ea59ea18240ee1264095e97f23f806303baea` | `60c288ac11fc54879dd0f499337aceb6d8bc6409` | Ej ancestor; bevarad review; 1 ahead / 10 behind |
| `review/finance-bb132k-protected-recovery-execution` | `0e798221debeffdb7f3286114bd1b01383e1af39` | `a37dd7f68d6d13ea3fda727ceb96bcbd8e8bb48a` | Ej ancestor; bevarad review; 9 ahead / 10 behind |
| `review/finance-bb132k-recovery-execution-preflight` | `dcf2c5cc81d602a007173aba9a34a7789a190e51` | `d90ffa619f05cfda7590a1b22072e0ff4071d8cb` | Ej ancestor; bevarad review; 1 ahead / 10 behind |
| `review/finance-historical-qwen-deployment` | `545713d2ddbf1c4060ca6421b728541b32ddcd7d` | `37b20ed4530d2fd32a45055291143fe07eb8cfd9` | Ej ancestor; bevarad review; 7 ahead / 0 behind |
| `review/finance-live-observation-activation` | `2df263a8cc11bd5837ea205244ffacefa184cec9` | `d85c7d6adb4ab773001c7afad20dad44ee4e77c6` | Ej ancestor; bevarad review; 1 ahead / 13 behind |
| `review/finance-recurring-research-contract` | `ca2b0fddaf58b050bde3f43d36db12cfff47ab66` | `e1413c2367129e33acad43efe62dc91556a7346c` | Ej ancestor; bevarad review; 1 ahead / 9 behind |
| `review/finance-recurring-research-deployment` | `7faba26a391c2246ede7b73b8329b66b3cd7d290` | `26ba39c3357ddcabd0198f651c02442af1f34c91` | Ej ancestor; bevarad review; 5 ahead / 2 behind |

Aktiv review-delta mot main före frysningen: sju dokument, följande infrastruktur samt två tester:
`bigbrain-historical-maintenance.service`, `bigbrain-historical-mount-diagnostic.service`,
`compose.historical-preparation.yaml`, `historical-mount-diagnostic.py`, `historical-preflight.py`
under `infrastructure/finance/`; `test_historical_preflight.py` och
`test_historical_mount_diagnostic.py` under `tests/deployment/`. Ingen av dessa blir accepterad
eller installerad genom frysningen. Main och review har identisk `src`-tree.

Spårat arbetsläge var rent. Orelaterat untracked arbete bevaras: `deisgnMockups/` och fyra
Sentinel-ADR-utkast0006–0009. Sju vanliga untracked filer har en intern kontrollsummesnapshot för
bevarandejämförelse; innehåll publiceras inte. Inga ignored hemligheter, lokala operatörshjälpare,
modellfiler eller `.env`-värden öppnas/stagas. Deras privata fullständighet/integritet inventeras
inte på nytt; tidigare runbooks/rapporter är endast daterade hänvisningar. Detta är en uttalad
inventeringsgräns, inte ett påstående att Git omfattar alla privata driftartefakter.

Bevarade kodträd från review-baslinjen, att jämföra efter dokumentationscommiten:

| Katalog | Tree |
| --- | --- |
| src | `e6e0238ec3ec6c9a8533b6325e565b94df1cb28f` |
| tests | `e4bde920123a2fa23692163317f4d9577a8f87ec` |
| infrastructure | `065ea64b3f615549556d979b5809ce02d4c093d3` |
| scripts | `10671e7cf63c505a5e350c47da4729d5bdcc795d` |

## F. Driftinventering — daterad evidens, ingen driftmutation

**Ingen ny host-, databas-, journal- eller skyddad diagnostikinspektion görs i frysningsuppdraget.**
Befintlig evidens räcker för att frysa arbetet, inte för att certifiera dagens drift. Därmed kan
Git-kodintegritet verifieras utan att påstå runtime-integritet. Ingen sudo eller annan eskalering.

| Objekt | Senast dokumenterade uppgift och aktuell verifieringsgräns |
| --- | --- |
| API/G2 | [Operationsrapportens deployment](https://github.com/T-bear/BigBrain/blob/378d43e9a33b217fef30de5d4f74ef58a0a01926/docs/reports/features/finance/historical-qwen-operation-20261016.md#build-and-deployment) anger start2026-10-16T06:52:20.069029818Z, hälsa HTTP200, OCI/compiled70b659f, image `sha256:26a2b150f449c2c7adce3b591b3ed10f3f3d35c5ba91042ff21150cd535b80fc`, frisk recovery/klocka. **D**, inte ny hälsokontroll. |
| AAPL/Alpaca | Samma rapport anger ObservationRuntime=true, transport=true, endast befintlig AAPL-mapping, cadence360min/lookback3dagar; ingen manuell cykel eller verifierad postdeployment-acquisition i den rapporten. **D**. Aktuell nästa körning, giltig assertion och eventuella senare kvitton är **O**. |
| Forskning/legacy | Senast dokumenterat EODHD=false, FRED=false, ResearchScheduler=false, MaintenancePaused=true; ingen historisk modellprofil/journalbinding eller utfärdad session i denna operation. **D**; ändras inte här. |
| Historiskt dataset | Read-only `finance-historical-inspect` dokumenterat före/efter G2 med oförändrad WIKI-källa och748 sessioner. Ingen ny databasläsning här. |
| Systemd/preflight | Ägare rapporterade löst217/USER, sedan UnexpectedWritableMount; temporär diagnostik avinstallerad, originalpreflight oförändrad. Ny ägarrapporterad relationsgranskning finns i C. Senaste Codex-open av privat resultat gav EACCES vid545713d; den läsningen återförsöks inte. |
| Artefakter/authority | Daterad worker-build/loader-evidens finns i hostingrapporten; slutliga root-skyddade kopior, dependency closure, enrollment, append-only-journal, restore-exkludering och final-context authority-match saknar samlad PASS. Aktuell privat filstatus är **O**. |
| Övrig drift | API/Web och andra containrar lämnas orörda. Det äldre failed `bigbrain.service` är dokumenterat men repareras/ominspekteras inte. Senaste dokumenterade Compose-deployment är inte bevis att enheten nu fungerar. |

**Separat ägarbeslutspunkt:** Om ägaren avser att även redan aktiverad AAPL-insamling ska upphöra
behövs ett uttryckligt driftuppdrag. Frysningen i sig stoppar den inte, förlänger inte mappingens
giltighet och medger inte förnyelse av utgången verifiering. Rapporterad aktivering kan därför vara
förenlig med frysningen samtidigt som framtida acquisition avvisas av befintliga grindar.

Rättigheter och retention kan kräva beslut även under frysningen: ADR0028 och befintlig
[EODHD-runbook](runbooks/finance-eodhd-retention-deletion.md) gäller fortsatt. BB-132K:s krypterade
kopia och privata disposal-inventering får inte glömmas; kontroll av faktisk deadline kräver separat
auktoriserad privat hantering. Den tidigare ramen var sju dagar efter verifierad deploymentacceptans
eller högst30dagar från skapandet, det som inträffar först, förkortat av providerkrav; exakt privat
förfallodatum/disposal-status är inte verifierat här. Frysningen förlänger inte fristen. Ingen ny
backup, restore, deletion eller BB-132K-utredning följer här.

Källklasser får inte blandas: WIKI:s dokumenterade public-domain-status skiljer sig från EODHD:s
aktiva-konto/deletionsvillkor och Alpacas uttryckliga ägarriskacceptans med kvarstående osäkerhet.
GOOGLEFINANCE/Zenodo/Yahoo och äldre candidate/databaskopior har den begränsade ägaracceptans som
bevaras i K-checkpointen; den är ingen generell tredje parts kopieringsgaranti. Quarantine, macro/intake
samt härledda resultat ärver respektive källas begränsningar. Inga rättigheter omprövas eller utökas här.

## G. Bevarad teknisk stoppunkt och risker

G1/G2 PASS är daterad merge-/deploymentevidens. G3a är fortsatt BLOCKED; G3b NOT RUN.
Administrativ frysning varken ersätter eller häver grindarna i [FIN-OPS-G3-20261016](https://github.com/T-bear/BigBrain/blob/378d43e9a33b217fef30de5d4f74ef58a0a01926/docs/operations/finance-historical-qwen-g3-checklist.md).

- Modellen är Qwen3-1.7B-Q4_K_M, SHA256
  `b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897`.
  Dataset: NASDAQ-WIKI AAPL `wiki-5713d7dccfa38f56`,748 sessioner2014-01-02–2016-12-19;
  fast första500:2014-01-02–2015-12-24. Exakta source/artifact commitments finns i
  [operationsrapporten](https://github.com/T-bear/BigBrain/blob/378d43e9a33b217fef30de5d4f74ef58a0a01926/docs/reports/features/finance/historical-qwen-operation-20261016.md#historical-source-read-only).
  Inga skyddade data eller modellresultat publiceras här.
- Tidigare beslutad engångsram I1/E1/T3/C64/W33s, högst30s inferens,250ms grace/2s reap,
  en worker, CPU2/RAM4GiB/swap0/pids32. Denna dokumenterade ram är **inte körbar auktorisation
  under frysningen**. Inga gamla A/B/C-grants får återanvändas. Ny giltighet får inte fabriceras.
- Tidigare50.458s i BB-132A överstiger30s; det är en verklig timeout-risk under annan tidigare
  auktorisation, inte stöd för en utökning. Misslyckat/uncertain försök kan förbruka budget.
- Review-only `MountFlags=private` hindrar senare propagation men avlägsnar inte redan ärvda
  monteringar. Ingen bredare allowlist eller individuellt verifierad `InaccessiblePaths` infördes.
  Alla16 har enligt ägarens nya granskning förälder; delad förälder/duplicerat target bevisar inte
  effektiv åtkomst eller säker mask. Riskacceptansen i C är inte teknisk PASS.

| Risk/status | Bevarad disposition |
| --- | --- |
| P0 | Inget nytt verifierat P0-brott konstaterat i denna dokumentinventering. Detta är inte en full säkerhetsrevision eller ett påstående att P0 är omöjligt. |
| P1 — slutlig hostisolering | ÖPPEN: ingen framgångsrik faktisk preflight i slutligt sammanhang; mountåtkomst/ursprung/säker maskering inte styrkta. Ägaracceptansen häver inte detta. |
| P1 — exekveringsauktoritet/artefakter | ÖPPEN: final artifact custody/loader/isolation, korrekt enrollment, oberoende append-only-journal, restore-exkludering och RecurringAuthorityMatched saknar samlad hostevidens. Ingen tillåtelse att provisionera under frysningen. |
| BB-132K skyddsavvikelse | Dokumenterad, kvarstående reviewfråga: temporär plaintext kvar över natt före godkänd cleanup. Tekniska återställningskontroller passerade, men inget secure-erase- eller no-exposure-bevis. Ingen ny P0/P1/P2-klass fabriceras; tidigare avvikelse är inte bortgodkänd. Se H. |
| Kända funktionsbegränsningar, inte nya säkerhetsundantag | Historisk väg tillåter en iteration, inte kontinuerlig forskning; rå prisbasis/corporate actions/survivorship och okänd tidigare exponering begränsar vetenskapliga slutsatser. Mappingövergång ger ingen mixed-provenance SHADOW-behörighet. Journal64poster/128KiB, receipts1000 och mappingversioner4 är fail-closed-gränser. |
| P2/dokumentationsskuld | Äldre aktuellt utseende statusrubriker är bevarade som daterad historik och överordnas denna ingång. Ingen ny teknisk P2-defekt konstateras genom freezeinventeringen. Framtida idéer i BACKLOG är parkerade, inte nästa arbetskö. |

Detta är den kända risklistan för bevarad releaseväg, inte en inventerad garanti för frånvaro av
fel i hela Finance eller andra moduler. Inga tidigare blockerare kallas lösta genom frysningen.

## H. Källhänvisad historik och beslut

| Tid/milstolpe | Beslut och bevarad evidens |
| --- | --- |
| Aug–sep2026, BB-077–089/123–124 | EODHD/WIKI, versionerade features, deterministiska backtester, chronological robustness och Hard Risk införs. [ADR0023](../adr/0023-finance-immutable-feature-revisions-and-causal-lineage.md), ADR0024/0025/0030 och [BB-124](../reports/features/finance/finance-bb-124-anti-overfitting-oos-governance-20260901.md). Simulerade fills/kapital är forskningsartefakter, inte handelsauktoritet. |
| Sep2026, BB-130/131A–F | Persistence/identity-karakterisering följt av research authority, fake/replay, ledger, local-first port och ägd runtime. Accepterade commits finns i E. [ADR0038–0040](../indexes/adr.md), [BB-131C](../reports/features/finance/bb-131c-persistent-learning-ledger-20260922.md), [BB-131F](../reports/features/finance/bb-131f-local-reasoner-runtime-isolation-20260929.md). |
| Sep29–Oct2, BB-132A/B/C | Kontrollerade lokala modeller och ändlig sekvens. [A](../reports/features/finance/bb-132a-local-model-preflight-20260929.md), [B](../reports/features/finance/bb-132b-research-loop-boundary-20261001.md), [C](../reports/features/finance/bb-132c-local-sequence-compatibility-20261001.md): tidigare riktiga inferenser är separat, förbrukad evidens; C:s N+1 stoppades BudgetExceeded. Detta är inte den nya historiska G3b-körningen. |
| Oct2–4, BB-132D/E/F/F1 | Immutable receipts → explicita daily eligibility/shadow → bounded observation worker → one-shot/build gate. [D](../reports/features/finance/bb-132d-market-observation-foundation-20261002.md), [E](../reports/features/finance/bb-132e-prospective-shadow-compatibility-20261002.md), [F](../reports/features/finance/bb-132f-live-market-observation-runtime-20261003.md), [F1](../reports/features/finance/bb-132f1-controlled-activation-gate-20261004.md). Minuteaggregation valdes bort för daily science; Twelve Data plan gav inte liveauktorisation. |
| Oct2026, G/H och I/J | H:s versionerade mapping och expiry/historisk-assertion-fix accepterades; [H](../reports/features/finance/bb-132h-versioned-observation-mapping-20261010.md). Review G/I/J bevarar analys/driftbevis på separata brancher, inte parallell implementation. |
| Oct10–12, BB-132K | [Exakt K-checkpoint](https://github.com/T-bear/BigBrain/blob/0e798221debeffdb7f3286114bd1b01383e1af39/docs/reports/features/finance/bb-132k-protected-recovery-execution-20261010.md): krypterad kopia, isolerad drill och integritet tekniskt verifierade; API/Web återstartade. Saknad underkänd ECB-artefakt är accepterad befintlig begränsning, inget innehåll återställdes. Plaintext-över-natt-avvikelsen kvarstår dokumenterad; BB-132K återöppnas inte här. |
| Oct14–15, recurring research | `ac34a7e` accepterades via merge `be7634bd82f52b3cb536142ae3e03dd0e2656aa2`. [Implementation](../reports/features/finance/recurring-research-implementation-20261014.md) bevarar tidigare BLOCKED-spår och senare journalbaserad korrigering. Full rollback av DB+journal löstes inte genom en flagga; oberoende custody/restore-gräns är driftförutsättning. |
| Oct16, historisk Qwen | `87b08d3` → accepterad merge70b659f, G1/G2 PASS. [Integrationsrapport](../reports/features/finance/historical-qwen-integration-20261016.md) och [operationsrapport](https://github.com/T-bear/BigBrain/blob/378d43e9a33b217fef30de5d4f74ef58a0a01926/docs/reports/features/finance/historical-qwen-operation-20261016.md). Dockerplacering räckte inte för worker-cgroup; systemd-underhållspreflight förbereddes på reviewbranch. Ingen G3b-inferens. |
| Oct16, G3a-review | `bfa99fb` förberedelse, `31711a1` analys, `351f25f` privat propagation, `545713d` EACCES. Senare ägarrapporterad privat läsning/relationsanalys och riskacceptans finns i C. Ingen av dessa är host-PASS. |
| Oct16, omedelbar frysning | Ägaren ersätter tidigare villkorad frysning med obegränsad administrativ paus för hela Finance. Inga tidigare tekniska accepteranden tas tillbaka; inget kvarvarande arbete startas automatiskt. |

## I. Återupptagningsprotokoll — inte körinstruktion nu

När ägaren uttryckligen beslutar att Finance får återupptas:

1. Läs detta frysdokument och AGENTS/START-HERE först. Tolka inte ett äldre “fortsätt” eller review-trigger som upptining.
2. Fetch/verifiera GitHub main, relevanta reviewbrancher, SHA/tree, PR:er och working tree. Bevara alla nya lokala/ignored ändringar; ingen reset/rebase/automatisk merge.
3. Läs aktuella kanoniska kontrakt, STATUS/BACKLOG och berörda runbooks riskbaserat. Kontrollera om arkitektur- eller ägarbeslut har ändrats.
4. Jämför nuläget med A/E och kodträdens identiteter. Skilj main, review, opublicerat och installerat; branchlikhet bevisar inte hostlikhet.
5. Identifiera förändringar under frysningen, inklusive möjlig fortsatt AAPL-insamling, utgångna mappingassertions, rättigheter/retention, klocka, journal/artefakter och deployment. Privilegierad åtkomst eller runtimeåtgärd kräver separat tillstånd.
6. Bedöm tidigare test-/CI-/driftevidens mot faktisk ändring enligt J. Återanvänd giltigt underlag; inga rutinmässiga fullsviter eller nya recoveryövningar.
7. Presentera kort lägesrapport med högst ett fåtal konkreta beslut: fortsatt frysning/avgränsat mål, hantering av befintlig drift och eventuell specifik G3a-evidensgranskning. G3a är inte automatiskt första uppdraget.
8. Invänta ägarens uttryckliga prioritering och omfattningsgodkännande. Upptining i sig tillåter inte felsökning, merge, deployment, migration, grant/journaländring eller inferens. Exakt SHA och operativ ram måste fortsatt godkännas där kontrakten kräver det.

## J. Bevarandelista och evidensens livslängd

| Bevara/återanvänd | När ny verifiering behövs |
| --- | --- |
| Gitobjekt, checkpoint-historik, datakontrakt och accepterade ADR:er; denna freezecommit | Main/review/ägarramar ändras eller working tree avviker. Historien får inte skrivas om. |
| Historisk integration:1215 API PASS/8 modell-SKIP, build och fokuserade kontroller med exakt scope i integrationsrapporten | Relevant kod, testantaganden, dependencies eller vetenskapliga kontrakt ändras. Gamla skip är inte inferensbevis. |
| Review351f25f:15 preflight +11 diagnostiktester och systemd257.13 static verify | Unit/probe/diagnostik/kernel/systemd/topologi/privilegier ändras. Statik bevisar aldrig verklig hostisolering. |
| G1:s commit/tree och dokumenterad CI; G2:s deploymentidentiteter | Ny release, image/container/config-/mountändring eller faktisk återstart. Daterad hälsa är inte aktuell hälsa. |
| Befintliga immutable receipts, raw JSON/checksum, lineage, knowledge-cutoffs och mappingmanifests | Relevant storage-/restore-/proveniensändring; inga receiptomskrivningar eller nya cutoffs för verifiering av historisk identitet. |
| BB-132K tekniska drillresultat, retentioninventering och skyddsavvikelse | Konkret nytt recoverybehov/ändrad scope med separat tillstånd; genomför inte om K för att återuppta Finance. |
| Ägarens65/16-diagnostik och relationsrapport, Codex EACCES, oförändrad policy | Saknade åtkomst-/ursprungsfakta måste styrkas före eventuellt framtida specifikt korrigeringsbeslut; omtest får inte beställas automatiskt. |
| Förbrukad grant-/exponeringshistorik och journalmodell | All verklig körning kräver aktuell tids-/rättighets-/profil-/journalmatch. Förlust/osäkerhet ger stopp, aldrig förnyelse. |
| Privata artefakter, originalrapporter, backup/disposal- och credentialcustody | Ska bevaras i befintlig skyddad mekanism. Deras innehåll kopieras inte till Git; utgångna externa skyldigheter kräver separat beslut. |

## Publicering och bevarandeverifiering

Detta är en dokumentationsbaserad REVIEW CHECKPOINT, inte merge-/driftauktorisation. Den nya
primära ingången länkas från status, backlog, roadmap, Finance-modul/arkitektur, operations/index,
G3/runbook och rapportkatalog. Äldre daterade poster bevaras; inga nya ADR:er eller backlog-ID:n skapas.
Ingen extern fristående Finance-granskningsrapport används som utvecklingsunderlag.

Bevarandekontroller genomförda för dokumentationsdelta:

| Kontroll | Resultat |
| --- | --- |
| `git fetch origin`, exakta HEAD/main/tree och branch-ancestry | PASS mot A/E; ingen ny kod accepterad |
| `node scripts/verify-documentation.mjs` | PASS:272 Markdown-filer,91 unika backlog-ID:n; relativlänkvalidering |
| Separat kontroll av frysdokumentets63 lokala länkar/fragment | PASS:0 saknade filer/ankare |
| `git diff --cached --check` och scopejämförelse | PASS:exakt18 avsedda Markdown-filer, ingen ospårad arbetsfil stagad |
| Index-tree mot545713d för src/tests/infrastructure/scripts | PASS:exakta tree-ID:n i E oförändrade; inga migrations-, unit-, runtime- eller säkerhetspolicyändringar |
| Sju befintliga orelaterade untracked filer, före/efter SHA256 | PASS:identiska; inga innehåll publicerade |
| Gitleaks8.28.0 `git --staged --redact --no-banner .` | PASS:inga läckor i staged delta |
| Nya Finance-tester, CI-körning, runtime-/produktionskontroller | NOT RUN:bevarandefrysning; tidigare evidens återanvänds endast med ursprungligt scope |

Slutligt staged dokumentations-/sekretessunderlag kontrolleras efter denna resultatnot. Efter push
verifieras remote HEAD=lokal HEAD, oförändrad main och bevarade kodträd. Exakta publiceringsidentiteter
finns i Gitmetadata/slutrapport. Fullständig privata hostartefakt-/driftsintegritet är fortsatt okänd,
inte ett krav som detta dokumentationsuppdrag påstår sig ha uppfyllt.
Ingen ny Finance-testkörning, modell-/providertrafik, schema-/journal-/konfigurationsändring,
serviceoperation, deployment eller merge ingår. Slutlig SHA/tree och remote-match redovisas från Git.

**FINANCE — FROZEN / AWAITING EXPLICIT OWNER RESUME**
