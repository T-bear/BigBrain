# DOC-2C fas A — inventering av testdokumentation och index

- Datum: 2026-10-24.
- Status: **REVIEW CHECKPOINT — inventering, ingen konsolidering eller fas B**.
- Accepterad main: `875c03330d2e5363ac587e5fbca975b8caa56073`. Branch: `docs/doc-2c-testing-index-inventory`.
- Publiceringsidentifierare: `docs: inventory testing guidance and indexes for DOC-2C phase A`.
  Exakt SHA/tree hämtas från commitmetadata på denna branch; inga självrefererande SHA i rapporten.
- Ändringar: endast denna rapport och [befintlig rapportkatalog](../REPORT-CATALOG.md).
- Implementerat: ingen funktion eller teständring. Testat: dokumentationskontroller nedan.
  CI för applikationen: inte körd/granskad i detta uppdrag. Mergat/deployat/manuellt accepterat: inget nytt.

## Baslinje, scope och auktoritet

Fetch verifierade exakt förväntad main. DOC-2B:s kandidat `d781ad270a96f164e7dd0a51e52c385135ebb848`
ingår i main; main-trädet är identiskt med kandidaten. DOC-2A:s kompakta STATUS/recovery och
DOC-2B:s aktiva Backlog/Roadmap med fullständiga historikarkiv är accepterad dokumentstruktur.
Orelaterat ospårat arbete lämnas orört, inte läst som accepterad arkitektur eller inkluderat i publiceringen.

[AGENTS](../../../AGENTS.md), [START-HERE](../../START-HERE.md) och
[dokumentauktoriteten](../../indexes/documentation.md#authority-order) gäller oförändrat.
Accepterade kontrakt begränsar lösningar; koden visar testkommandon, daterad evidens visar tidigare
resultat. Runbookens egen status och scope måste läsas innan proceduren används. Rapporten är
inventering, inte nytt normativt testkontrakt eller bevis på full testtäckning.

[Finance-frysningen](../../operations/finance-freeze-20261016.md) och
[BB-158-frysningen](../../operations/bb158-freeze-20261023.md) överordnas äldre fortsättningsord.
Finance **RESEARCH / 0 SEK / NONE, G3a BLOCKED, G3b NOT RUN**. Inga frysta tester,
modeller, externa källor, driftkontroller eller produktionstjänster har körts/inspekterats.

Full läsning av TESTING.md; riktad läsning/sökning av alla sex spårade dokumentationsindex,
CI/workflow/package/solution och relevanta kontrakt/runbooks. Historiska rapporter lästes inte
rutinmässigt i fulltext. Ingen nätverkskontroll av externa rapport-/CI-/leverantörslänkar.

## Fullständig avsnittsinventering

TESTING.md: **1286 rader, 85 rubriker** (inklusive dokumenttitel och underavsnitt).
Git-blob `478bfd31db93dd35d65d9b21a5a9f8d1b67ee931`;
SHA-256 `fec4ad655dcabda05ef685d21f63b277c412481c776bb829a85d2dafb82be827`.
Varje intervall går från rubriken till raden före nästa rubrik, oavsett nivå; intervallen
partitionerar hela originalfilen. Det gör även orubricerade krav mekaniskt spårbara.
Alla originaltexter ligger kvar; destinationer nedan är endast förslag inför fas B.

- **CURRENT:** aktuell metod/ingång, inte färskt PASS.
- **HISTORICAL:** daterat resultat eller ersatt instruktion, ska bevaras ordagrant i föreslaget arkiv.
- **REQUIRED GATE:** bindande invariant eller acceptanskrav; kvar synligt med kontraktslänk.
- **BLOCKED/FROZEN:** inget körningstillstånd; frysning ändrar inte kravets existens.
- **UNCERTAIN:** tillämpning/acceptans behöver avgränsas med evidens före omklassificering.

G0–G8 hänvisar till bevarandegrindarna nedan, inte nya regler. Hela originalet föreslås senare
arkiveras; CURRENT/GATE och osäkra krav ska samtidigt ha en synlig aktiv hänvisning. Ingen post
eller DoD stängs genom klassificering. Blandfall är uttryckligen flermärkta.

| ID / källposition | Ursprunglig rubrik | Klassificering | Bevara / framtida disposition |
| --- | --- | --- | --- |
| [T001: 1–2](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1-L2) | Testa BigBrain | CURRENT | Titel; blandat dokument, inte bevis för färsk testning. G0. |
| [T002: 3–13](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L3-L13) | BB-132E model-free prospective verification — 2026-10-03 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Daterad E-matris; DST/cutoff, immutable receipts, concurrency, rättighetsrevokering. Full API-krav gäller denna persistensdelta, inte varje dokumentuppgift. G1/G2. |
| [T003: 14–30](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L14-L30) | BB-132D model-free observation checks — 2026-10-02 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | D:s receipt/reopen/migration/corruption och bounded transport; focused MarketObservation-kommando historiskt scoped. Ingen live-provider/model som testberoende. G1/G2. |
| [T004: 31–50](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L31-L50) | BB-132C real finite research sequence — 2026-10-02 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | C accepterad real sekvens/CI för angivna SHA; förbrukad allowance, NO further inference. Äldre D NOT AUTHORIZED är tidsbundet. G1/G3. |
| [T005: 51–71](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L51-L71) | BB-132C — local-session compatibility review, 2026-10-01 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Äldre 30s/180s/prompt-kompatibilitetsstopp ersatt inom C; varken ny grant eller omtestorder. G1/G3. |
| [T006: 72–94](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L72-L94) | BB-132B — finite model-free research session, 2026-10-01 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Finite session: explicit grant, budget, SQLite reopen/race/corruption, holdout och disabled opt-in; syntetiskt är inte OS-/lönsamhetsbevis. G1/G3. |
| [T007: 95–115](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L95-L115) | BB-132A final version-bound acceptance — 2026-10-01 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Version-bound one-shot acceptans; förbrukad journal får aldrig återköras. CI skippar modeller; ingen implicit deployment. G1/G3. |
| [T008: 116–130](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L116-L130) | BB-132A RC04 model-free characterization — 2026-10-01 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | RC04 modellfri grammatik/parser/admission-evidens; dåtida unused allowance ersatt av senare slutresultat. G3. |
| [T009: 131–144](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L131-L144) | BB-132A RC03 verification — 2026-10-01 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | RC03 InvalidReply är bevarat FAIL, inte grön acceptans; ingen retry/journalåterställning. G3. |
| [T010: 145–156](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L145-L156) | BB-132A RC02 diagnostic verification — 2026-09-30 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | RC02 slutna rejectors och ofullständig acceptance; gamla svar kan inte rekonstrueras som evidens. G3. |
| [T011: 157–171](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L157-L171) | BB-132A RC01 verification state — 2026-09-30 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | RC01 misslyckade modeller/ofullständig matris; ordinary CI inget modeldownload; förbrukade opt-in-journaler bevaras. G3. |
| [T012: 172–193](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L172-L193) | BB-131F local runtime control — 2026-09-29 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Owned worker BRF1/pidfd/cancellation; Linux/glibc och hård OS-/resurs-/auditgrind separat från proof child. G3. |
| [T013: 194–215](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L194-L215) | BB-131E local reasoner contract — 2026-09-28 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Reasoner wire/projection/late reply; kontraktstest bevisar inte auth/watchdog/native sandbox. G3. |
| [T014: 216–225](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L216-L225) | BB-131D architecture verification — 2026-09-28 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | D är designreview, abuse-matrisens framtida negativa tester är inte körda tester. G3. |
| [T015: 226–239](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L226-L239) | BB-131C ledger verification — 2026-09-22 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Ledger CAS/concurrency/reopen/lost response; isolerad interruption är inte hostrollback/backupbevis. G2/G3. |
| [T016: 240–256](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L240-L256) | BB-131B synthetic contract/replay verification — 2026-09-22 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Syntetisk replay/admission/holdout; senare CI skild från lokal evidens och vetenskaplig kvalitet. G1/G2. |
| [T017: 257–273](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L257-L273) | BB-131A architecture/recovery verification — 2026-09-21 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | A design/recoveryreview; B då föreslaget, senare implementerat. Gamla NOT IMPLEMENTED är datumscopade. G1/G3. |
| [T018: 274–294](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L274-L294) | BB-130 final exit assessment — 2026-09-21 | HISTORICAL + REQUIRED GATE | BB-130 EXIT APPROVED bevaras; latency, schema/readers, auth/Sentinel och device debt inte avslutade. G0/G4/G5. |
| [T019: 295–323](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L295-L323) | BB-130D2 frontend formatter CI gate — 2026-09-21 | HISTORICAL + CURRENT + REQUIRED GATE | Frontend format:check finns i aktuell CI; gamla D IN PROGRESS/next är historik, inte ny order. G0/G6. |
| [T020: 324–378](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L324-L378) | BB-130D2 frontend formatter cleanup — 2026-09-20 | HISTORICAL + REQUIRED GATE | 80/83-filers cleanup och ägaraccepterad tvåpassavvikelse är historik; inget tvåpass/auto-fix i normal CI. G6. |
| [T021: 379–406](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L379-L406) | Accepted publication — 2026-09-21 | HISTORICAL + REQUIRED GATE | Underavsnittets merge/CI och NOT ENABLED gällde cleanupens datum; senare gate finns. G6. |
| [T022: 407–431](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L407-L431) | BB-130D2 formatter tooling — 2026-09-17 | HISTORICAL + REQUIRED GATE | Toolingens 80 formatfel/expected exit1 före cleanup är inte aktuell CI-baslinje. Bevara pinned/check-only princip. G6. |
| [T023: 432–453](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L432-L453) | BB-130D2 dependency maintenance — 2026-09-16 | HISTORICAL + REQUIRED GATE | Daterad dependencymaintenance/audit0; inget bevis för dagens sårbarhetsläge. G6/G7. |
| [T024: 454–483](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L454-L483) | BB-130D2 dependency triage — 2026-09-15 | HISTORICAL + REQUIRED GATE | Daterad triage fem fynd, no fix då; senare maintenance ersätter det tillståndet. Ingen ny audit körd. G7. |
| [T025: 484–511](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L484-L511) | BB-130D2 frontend characterization — 2026-09-14 | HISTORICAL + REQUIRED GATE | Daterade 199 tester/80 formatfel/fem findings; ny full karakterisering är inte automatiskt nästa steg. G0/G6. |
| [T026: 512–532](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L512-L532) | BB-130D backend format CI gate — 2026-09-14 | HISTORICAL + CURRENT + REQUIRED GATE | Backend formatcheck finns i aktuell CI. Historisk D2 NOT STARTED är ersatt; inga write-mode-steg. G6. |
| [T027: 533–559](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L533-L559) | BB-130D backend whitespace cleanup — 2026-09-14 | HISTORICAL + REQUIRED GATE | Whitespacecleanup: token/semantic-identitet; historiskt gate NOT ENABLED motsvarar inte nuvarande workflow. G6. |
| [T028: 560–589](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L560-L589) | BB-130D1 backend quality-gate baseline — 2026-09-14 | HISTORICAL + REQUIRED GATE | D1 22573 whitespace-fynd och uppmaning att reproducera är historik; senare cleanup/gate gäller. G0/G6. |
| [T029: 590–604](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L590-L604) | BB-130C exit approved — 2026-09-09 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | C-exit inte slut på deferred persistence debt; D NOT STARTED historiskt. G1/G2. |
| [T030: 605–624](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L605-L624) | BB-130C E2 campaign SQLite replay/reload — accepted, merged and CI verified, 2026-09-09 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | SQLite campaign replay/count/checksum, NOT EVALUABLE utan fallback; runtime inte verifierad. G2. |
| [T031: 625–643](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L625-L643) | BB-130C E1 robustness identity correction — accepted, merged and CI verified, 2026-09-09 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | E1 exact selected identity/stale response; blockerbranch NOT MERGEABLE, inget nytt drift-/UX-PASS. G2/G5. |
| [T032: 644–664](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L644-L664) | BB-130C backtest readers and exit assessment — accepted, merged and CI verified, 2026-09-08 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | DO NOT EXTRACT-beslut och deferred schema/composition; historiska E1/E2-planer numera accepterade. G2. |
| [T033: 665–680](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L665-L680) | BB-130C shared immutable backtest writer — accepted, merged and CI verified, 2026-09-08 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Shared immutable writer/fyra tabeller och no schema-change; dated build/CI, ingen runtime. G2. |
| [T034: 681–691](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L681-L691) | BB-130C Finance persistence characterization — accepted, merged and CI verified, 2026-09-08 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Non-EODHD reload/idempotency/checksumconflict och kvarstående coveragegräns. G2. |
| [T035: 692–701](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L692-L701) | BB-130C CSV syntactic tokenizer — accepted, merged and CI verified, 2026-09-08 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | CSV tokenizer: canonical/legacy identity och artifactchecksums oförändrade. G2. |
| [T036: 702–713](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L702-L713) | BB-130C CSV corporate-action column-zero correction — 2026-09-08 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Column-zero reproducerad FAIL→fix; historisk regression, inga verkliga data. G2. |
| [T037: 714–722](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L714-L722) | BB-130C intake quarantine boundary — 2026-09-07 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Storlek/filnamn/cleanup/restart-retention i isolerade fixtures. G2. |
| [T038: 723–731](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L723-L731) | BB-130C canonical product/revision identity v2 — 2026-09-07 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | v2 identity och legacyreplay, isolated CLI; inga nya vetenskapliga ändringar. G2. |
| [T039: 732–747](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L732-L747) | BB-130C revision identity audit — 2026-09-07 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | ReadOnly audit och ofullständig recomputation; blockerbranch får inte misstolkas som accepted invariant. G2. |
| [T040: 748–756](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L748-L756) | BB-130C backtest identity — merged and CI verified, 2026-09-07 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Selected backtest identity, cancellation/stale completion; robustness utanför dåtida scope. G2/G5. |
| [T041: 757–768](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L757-L768) | BB-130C research-detail characterization — 2026-09-06 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Reproducerad mismatch var defekt, inte framtida testinvariant; senare korrigering behöver behållas i historikkedjan. G2. |
| [T042: 769–778](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L769-L778) | BB-130C observation lifecycle characterization — 2026-09-06 | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Visibility/online dedup, abort/cache/retry och accessibility; ingen ny deviceacceptans. G1/G5. |
| [T043: 779–791](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L779-L791) | BB-130B loading verification — 2026-09-06 | HISTORICAL + REQUIRED GATE | Cold loads, polling och cancellation; browserpreview inte deployment/iPhoneacceptans. G5. |
| [T044: 792–803](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L792-L803) | BB-130A continuity verification — 2026-09-05 | HISTORICAL + REQUIRED GATE | Daterad Sentinel/format/docs-slice; gammal later-phases-instruktion ersatt av exit och AGENTS. G0/G4. |
| [T045: 804–810](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L804-L810) | BB-128C Finance async/degraded design-system conformance | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Async/degraded/state semantics och localeoberoende assertion; ägaracceptans begränsad till den microfixen. G1/G5. |
| [T046: 811–827](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L811-L827) | BB-128B Finance last-known-good resilience | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Cache/retry/säker projection och fysisk outageacceptans daterad. Sexstegs outageprocedur är inte tillstånd att stoppa API nu. G1/G5. |
| [T047: 828–842](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L828-L842) | BB-128A Alpaca activation readiness | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Entitlement före delegate, IEX inte consolidated, tidsordning; network-free bevis inte provideraktivering. G1/G2. |
| [T048: 843–862](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L843-L862) | BB-127 owner research capabilities and XLSX | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | XLSX bounds/owner scope/no canonical sideeffect; datamängder och deployed image är daterade. G2. |
| [T049: 863–876](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L863-L876) | GOOGLEFINANCE GOOG first owner intake | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | GOOG Rejected/BLOCKED identity/rights/raw; inga grants/promotion genom inventering. G2. |
| [T050: 877–895](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L877-L895) | BB-126 owner market-data drop | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Ready-marker/ZIP/restart/no promotion; historisk deploymentcheck ingen aktuell order. G2. |
| [T051: 896–910](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L896-L910) | BB-125 zero-cost historical market-data qualification | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Qualification docs-only, STATE A saknas; ingen adapter/no providerrequest, gammal gitleaksfrånvaro daterad. G1/G2. |
| [T052: 911–917](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L911-L917) | BB-124 anti-overfitting and OOS governance | REQUIRED GATE + BLOCKED/FROZEN + HISTORICAL | Embargo/holdout/trial accounting/leakage, DSR/PBO NOT_EVALUABLE. Maintenance/deployment i checkpointscope, aldrig nu. G2. |
| [T053: 918–925](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L918-L925) | BB-123 transaction-cost, slippage and fill realism | REQUIRED GATE + BLOCKED/FROZEN | Kostnader/fills/legacy JSON, exact revision och concurrent immutability; bounded comparisons är frysta driftåtgärder. G2. |
| [T054: 926–932](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L926-L932) | BB-122 historical security identity evidence pilot | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Ticker/cohort identity och 13 fail-closed gates; nutida directory är inte historiskt medlemskap. G2. |
| [T055: 933–939](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L933-L939) | BB-121 WIKI forensics and Stooq requalification | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | WIKI checksum/cohort/row counts är daterad evidens; ingen ny protected source eller runtimeinventering. G2. |
| [T056: 940–945](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L940-L945) | BB-120 historical evidence source qualification | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Dataset runtime IDs/states avser dåtida scope; GET/inspection är inte auktoriserat här. G2. |
| [T057: 946–951](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L946-L951) | BB-119 Finance readiness and governor reconciliation | REQUIRED GATE + BLOCKED/FROZEN | Pinned-clock readiness/DEFER och Sentinel stale socket; aktuell hoststatus ej verifierad. G1/G2/G4. |
| [T058: 952–957](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L952-L957) | BB-118 Finance source-of-truth reconciliation | HISTORICAL + REQUIRED GATE + BLOCKED/FROZEN | Daterade counts/schema/readiness; även read-only maintenance kräver scope, ingen återkörning. G1/G2. |
| [T059: 958–964](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L958-L964) | BB-117 audiobook final polish and Media handoff | HISTORICAL + REQUIRED GATE + UNCERTAIN | Timer 48px/23px och hero, separat Firefox kontra fysisk PWA; BB-117:s smala ägarbesked enligt STATUS är inte full modulacceptans. G5/G8. |
| [T060: 965–971](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L965-L971) | BB-115R physical iPhone sleep-timer remediation | HISTORICAL + REQUIRED GATE + UNCERTAIN | BB-115R no-session/expiry/layout; äldre fysisk pending får inte generellt stängas av BB-117. G5/G8. |
| [T061: 972–979](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L972-L979) | BB-115 physical iPhone artwork and Continue Listening timer | HISTORICAL + REQUIRED GATE + UNCERTAIN | Artwork ratio/timer; BB-114 browserPASS fångade inte fysisk regression. Behåll fysisk grind. G5/G8. |
| [T062: 980–986](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L980-L986) | BB-114 audiobook detail polish, sleep timer and floating-player rejection | HISTORICAL + REQUIRED GATE + UNCERTAIN | Floating player rejection, 2:3, route/session, best-effort iOS expiry; automation ej full ägaracceptans. G5/G8. |
| [T063: 987–994](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L987-L994) | BB-113 audiobook owner-review remediation | HISTORICAL + REQUIRED GATE + UNCERTAIN | Narrow UI/player tests och fixed race; äldre mini-player QA ska inte återinföra ratad design. G5/G8. |
| [T064: 995–1000](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L995-L1000) | BB-112 native audiobook playback and owner search remediation | REQUIRED GATE + HISTORICAL | Opaque progress/session/Range/gräns; historiskt one-second runtimeprov muterar användarprogress och är inte generell testmetod. G8. |
| [T065: 1001–1010](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1001-L1010) | BB-111 route/detail UX and playback credential gate | HISTORICAL + REQUIRED GATE | slnx-korrigering, sanerad credential-discovery, route/focus/scroll. Inga tokens eller privat userdata i evidens. G5/G8. |
| [T066: 1011–1017](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1011-L1017) | BB-110 audiobook owner UX consolidation | HISTORICAL + REQUIRED GATE + UNCERTAIN | Full API/Sentinel trots Web-only var checkpointval, inte generell regel. Identitet/session/stream-kontrakt är skydd; äldre blanket not implemented är historik. G0/G8. |
| [T067: 1018–1024](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1018-L1024) | BB-109 audiobook owner UX remediation | REQUIRED GATE + HISTORICAL + UNCERTAIN | Grace fail-closed/provider/job-bevis, ingen mutation, playback-identitet; fysisk acceptance fortsatt scopebunden. G8. |
| [T068: 1025–1031](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1025-L1031) | BB-108 audiobook navigation experiment | HISTORICAL + REQUIRED GATE + UNCERTAIN | Experimentell route/catalogue/history/artwork QA; inte nuvarande normal Media-ingång. Inget nytt ownerapproved UI. G5/G8. |
| [T069: 1032–1037](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1032-L1037) | BB-107 owner UX remediation | REQUIRED GATE + HISTORICAL | Arr POST timeout→exact foreign-ID reconciliation och samma idempotensnyckel; endast providerfixture. G8. |
| [T070: 1038–1043](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1038-L1043) | BB-106 consolidated UX quality | HISTORICAL + REQUIRED GATE | Busy/placeholder/date semantics och viewportdock; historiska suitecounts ingen aktuell fullsuiteorder. G5/G6. |
| [T071: 1044–1050](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1044-L1050) | BB-105 AudioBookBay parser and literal author search | HISTORICAL + REQUIRED GATE | Gamla pinned Librarr-imagebyggen/currentmarkup och externa search-QA är daterade; source- och nätverksbehörighet först. G8. |
| [T072: 1051–1056](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1051-L1056) | BB-104 universal metadata-aware audiobook search | REQUIRED GATE + HISTORICAL | ISBN-10/13/OpenLibrary fixtures, bounded seeds/unknown language, ingen acquisition vid sök. Inte BB-158-återupptagande. G8. |
| [T073: 1057–1067](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1057-L1067) | BB-103 usable audiobook lifecycle | HISTORICAL + REQUIRED GATE | bb4/bb5 current-image-ord är historik. No overwrite, durable failure, exact ABS identity; ambiguous kvar indexing. G8. |
| [T074: 1068–1074](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1068-L1074) | BB-102 patched Librarr provider | HISTORICAL + REQUIRED GATE | bb4 Docker och källa/sourcepolicy daterade; auth/bounds/real-state/fail-closed skydd behålls. G8. |
| [T075: 1075–1082](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1075-L1082) | BB-101 audiobook acquisition foundation | HISTORICAL + REQUIRED GATE | Foundation Provider None/empty runtime är inte dagens förväntan; no-overwrite/confirmation/inert search kvar. G8. |
| [T076: 1083–1090](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1083-L1090) | BB-100 audiobook platform foundation | HISTORICAL + REQUIRED GATE | Foundation configured library/provisioning och gates är scoped; secrets aldrig fixtures/Web. G8. |
| [T077: 1091–1094](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1091-L1094) | BB-099 whole-app UX audit | REQUIRED GATE + HISTORICAL + UNCERTAIN | Full UX-audit: reachable dock/sista handling/normalviewport, ingen generell acceptance. G5. |
| [T078: 1095–1098](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1095-L1098) | BB-098 global UI migration | REQUIRED GATE + HISTORICAL + UNCERTAIN | Family canary/tre teman/a11y, iOS fullpageartefakt inget fixbevis. G5. |
| [T079: 1099–1152](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1099-L1152) | BB-097 Family reference validation | REQUIRED GATE + HISTORICAL + BLOCKED/FROZEN + UNCERTAIN | BLANDFALL: Family-kvalitet OCH orubricerade BB-092–096/089/088/087/086/085/084/083. Se separat delmappning; bevara freeze/retention/recovery. G1–G5. |
| [T080: 1153–1179](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1153-L1179) | Automatiska tester | CURRENT + REQUIRED GATE + UNCERTAIN | Giltiga slnx/npm-kommandon men formatchecks saknas i kortlistan; Compose är inte varje dokumentuppgiftskrav. G0/G6. |
| [T081: 1180–1267](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1180-L1267) | Verifieringskarta | CURRENT + REQUIRED GATE + HISTORICAL + BLOCKED/FROZEN | BLANDFALL: runbooklänkar + Finance-metoder och gamla runtime/rerun-instruktioner; ingen mutation/benchmark auktoriseras. G1/G2/G8. |
| [T082: 1268–1272](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1268-L1272) | Live-säkerhetsregel | REQUIRED GATE | Live write-endpoints/credentials förbjudna i automation; verkliga mutationer endast explicit UI/scope. Media har smala writes. G8. |
| [T083: 1273–1278](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1273-L1278) | BB-090 test additions | REQUIRED GATE + HISTORICAL + BLOCKED/FROZEN | Macro vintages/knowledge/DST/SQLite migration och counts; production migrationdrill fryst, ingen ny integrityPASS. G2. |
| [T084: 1279–1281](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1279-L1281) | BB-091 test additions | REQUIRED GATE + HISTORICAL + BLOCKED/FROZEN | Riksbank/ECB fixtures, as-of/rights; maintenance inteCI, revised bootstrap inte prospective. G2. |
| [T085: 1282–1286](https://github.com/T-bear/BigBrain/blob/875c03330d2e5363ac587e5fbca975b8caa56073/TESTING.md#L1282-L1286) | BB-129A campaign verification | REQUIRED GATE + HISTORICAL + BLOCKED/FROZEN | Campaign budget/fail-closed/holdout; counts och runtime 2026-09-03 återanvänds endast datumscopat. G2. |

### Krav utan egen rubrik — särskilt flyttskydd

T079 ser ut som en Family-post men innehåller tolv ytterligare BB-områden. De får inte flyttas
som enbart Family-historik. Följande radpositioner avser samma pinnade TESTING-original ovan:

| Källrader | Innehåll och bevarande |
| --- | --- |
| 1107–1108 | BB-092: allowlisted features, OOS/cost/lineage, negative history, partial-run retention, single-flight, korrekt current evidence; inga provider/PAPER/order-mutationer. G1/G2. |
| 1109–1110 | BB-093: injicerad tid, restart/idempotens, no catch-up storm, readiness/deferral/cancel; ingen real schedulerstart. G1/G2. |
| 1111–1112 | BB-094: injicerade metrics, BLOCK > DEFER > ALLOW, stale/throwing/restartaudit, ingen fabricerad temperatur. G1/G2/G4. |
| 1113–1114 | BB-095: isolerad SQLite, incidents/recovery/partial experiments, metadata-backup, inga real sleeps/providers. G1/G2. |
| 1115–1116 | BB-096: tema-ID/migrering/persistens/nav, visuella mockups inte testfixtures eller driftbevis. G5. |
| 1117–1122 | BB-089: auth/riskinvariants, forged verdicts, halt/recovery/idempotens, inga verkliga order/credentials. G1/G2. |
| 1123–1128 | BB-088: weekday/provider-window, verklig breadth/causality och ingen fabricerad helgsession. G1/G2. |
| 1129–1130 | BB-087: knowledge cutoff/no-lookahead, append-only outcomes/late-start anti-backfill, ingen mutation. Historiskt fullsuitekrav inom dåvarande scope. G1/G2. |
| 1131–1135 | BB-086: dokument-only men dåtida fullsvitsorder; ersätt inte AGENTS riskbaserade regel med historisk rutin. G0. |
| 1136–1140 | BB-085: providerklassning, atomic manifests, SHA/recovery/retention/canonical protection. G1/G2. |
| 1141–1146 | BB-084: CSV/ZIP/proveniens/promotion/rights/no partial publish och budgetgränser; one-time acquisition inte vanlig testdependency. G1/G2. |
| 1147–1150 | BB-083: clean/unclean/restart/missed-run, separat systemd/reboot/physical hostgate, sanerad output. G4. |

T081 innehåller både modulöversikten (1182–1189), normativa Finance-invariants och
testmetoder (1190–1266), gamla maintenance-kommandon samt BB-071:s daterade rerun-text.
Behåll alla krav och fulltext; ingen benchmark, providerhämtning eller volume-/restartkontroll
auktoriseras av att en metod beskrivs. T083–085 är extra toppnivårubriker efter live-regeln,
inte delar som kan tappas bort vid en enkel H2-extraktion.

## Testkommandon och CI — statiskt verifierat nuläge

Följande är läst i accepterad kod, **inte exekverat i DOC-2C**. Proportionell lokal verifiering
väljs enligt AGENTS, inte utifrån gamla fasta svitantal eller allomfattande checkpointlistor.

| Yta | Faktiskt kontrakt / kommando | Verifieringsgräns |
| --- | --- | --- |
| Backend | Från roten: `dotnet restore BigBrain.slnx`; `dotnet format BigBrain.slnx --verify-no-changes --no-restore`; `dotnet build BigBrain.slnx --configuration Release --no-restore`; `dotnet test BigBrain.slnx --configuration Release --no-build`. | CI använder .NET 10.0.x. Lösningen innehåller API- och Sentinel-testprojekt, Brain proof worker och Finance-benchmarkprojekt; build av benchmark är inte dess körning. Full CI-svit är inte order att lokalt köra frysta tester nu. |
| Fokuserad backend | `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --filter '<berörd testklass>'`; Sentinel har separat `tests/BigBrain.Sentinel.Tests/BigBrain.Sentinel.Tests.csproj`. | Filter är scopeberoende, restore/build-flaggor kräver motsvarande artefakter. Inget nytt fast testantal. |
| Frontend | I `src/BigBrain.Web`: `npm ci`, `npm run format:check`, `npm test -- --run`, `npm run build`. | package.json anger Vitest run, tsc/Vite och check-only Prettier. `npm run format` skriver filer och är inte verifieringskommando. Ingen generell lintscript finns där. |
| Dokumentation | `node scripts/verify-documentation.mjs`, `git diff --check`. | Validator kontrollerar relativa filvägar men tar bort fragmentdelen; ankare behöver riktad kontroll. Den kontrollerar också styrregler, statusmetadata, backlog-ID, rapport-/publiceringshygien. |
| Secrets | `gitleaks/gitleaks-action@v2`, checkout `fetch-depth: 0` i CI. | Workflowkonfiguration bevisar inte senaste run eller fullständig sårbarhets-/pentesttäckning. Scope-/sekretessreview behövs fortfarande. |
| Compose / Docker | T080 anger `docker compose config --quiet`; gamla Librarrposter anger byggen med historiska taggar. | Inte generell dokumentationsgrind eller säker driftprocedur. Välj enbart vid relevant auktoriserad config/build-delta; inga sådana kommandon körda här. |

Källor: [CI-workflow](../../../.github/workflows/ci.yml), [solution](../../../BigBrain.slnx),
[Web scripts](../../../src/BigBrain.Web/package.json). Workflow triggas av push till main och PR;
fyra jobb: backend, frontend, documentation, secrets. Inga separata deploy-, fysisk device-,
pentest-, modeller/inferens- eller provideracceptansjobb finns i denna workflow.
Det är en statisk läsning, inte nytt GitHub Actions-PASS eller bevis på att alla möjliga opt-in-tester körs.

## Övriga relevanta verifieringskällor

Riktad källkarta; inte total inventering av varje historisk rapports rubriker eller av all testkod.
Bindande krav bevaras även om ett dokument också innehåller daterade operationsinstruktioner.

| Källa | Typ, ansvar och viktigt kvarstående skydd |
| --- | --- |
| [Finance teststrategi](../../architecture/finance/testing-and-validation.md) | Normativ domänstrategi med historiska tillägg: invariants, unit/integration/simulation/paper/sandbox/failure-injection/reconciliation/security/a11y/performance/soak. Rights/retention/lineage och structurally no-live test doubles. BLOCKED/FROZEN; inget körningstillstånd. |
| [Finance threat model](../../security/finance-threat-model.md) | Risk/auth/credential/holdout/inputgränser; planerad säkerhets-/pentestmilsten. REQUIRED GATE + historik, inte certifiering. |
| [Finance reasonergräns](../../architecture/finance/local-first-reasoner-boundary.md) | Abuse-matris, noninterference, closed outputs, auth/revocation, resource/cancel/audit; designkrav inte körda prov. |
| [Finance modul](../../modules/finance.md) | Domän-/API- och capabilitykontrakt; accepterad implementation skild från drift/rights/auktoritet. Frysningen styr. |
| [Finance mode promotion](../../operations/runbooks/finance-mode-promotion.md), [retention/deletion](../../operations/runbooks/finance-eodhd-retention-deletion.md), [controlled acquisition](../../operations/runbooks/finance-controlled-acquisition.md) | Godkännande-, scope-, rättighets-, deletion-/restore- och driftgrindar får inte flyttas bort som bara gamla resultat. Inga procedurer körda. |
| [Finance freeze](../../operations/finance-freeze-20261016.md) | Kanonisk ingång, inkluderar permanenta commitlänkar till historisk G3-checklista/hosting-/operationsunderlag som inte ligger som aktiva filer på main. G3a BLOCKED, G3b NOT RUN. |
| [BB-158 freeze](../../operations/bb158-freeze-20261023.md), [BB-158-rapport](../features/media/bb-158-url-import-20261017.md) | Hela direktimport/DOI/YouTube/ISBN/konverteringsområdet fryst. Daterad parser-/atomic placement-/scanner-/SSRF-/recoveryevidens, inte färsk drift eller ny testsprint. |
| [Media modul](../../modules/media.md), [Media verifiering](../../operations/runbooks/media-integration-verification.md) | Versionssatta read- och smala writegränser, sanerade adapterfel, separata identity/preview/confirmation. Runbooken är Draft/read-only; ingen självständig produktionsauktoritet. |
| [Safe removal](../../operations/runbooks/download-control-safe-removal.md), [ADR0013](../../adr/0013-safe-qbittorrent-download-removal-boundary.md), [ADR0016](../../adr/0016-safe-download-control-command-and-partial-batch-boundary.md) | Preview→explicit confirmation→revalidation/audit, inga all-hashes/unsafe path/överskrivningar. Destruktiv fysisk verifiering och Retry återstår enligt STATUS, BB-019–023. |
| [Sentinel arkitektur](../../architecture/sentinel-architecture.md), [säkerhetsmodell](../../security/sentinel-security-model.md), [protokoll](../../architecture/sentinel-protocol.md) | Threat review, auth/negative/replay/expiry/clock/idempotency, schemafuzz/parser bounds, symlinkrace, supplychain/SBOM/signering och extern pentest före remote nodes/mutationer. BB-009 inte full konformitet; lokala ADR-utkast används inte som accepterade krav. |
| [Appliance lifecycle](../../operations/runbooks/bigbrain-appliance-lifecycle.md) | Process-/clean-marker/recovery och explicit fysisk power/rebootgrind. CI behöver inte ha systemd PID1; software bevisar inte BIOS. |
| [Backup/restore](../../operations/backup-restore/README.md), [deployment](../../operations/deployment/README.md), [aktiv recovery](../../operations/codex-recovery.md) | Scope/versioner/isolated staging/ingen destruktiv restore, providerretention och sessionsåterhämtning. Flera daterade Finance-kommandon; familjemodulers restore-drills saknar godkännande i dokumentet. Se O7. |
| [Kalender](../../modules/calendar.md), [Heroma](../../operations/runbooks/heroma-schedule-import.md), [kalenderverifiering](../../operations/runbooks/calendar-verification.md) | Syntetiska XLSX, preview/confirm, conflict/duplicate/idempotens, persistence/retention. Riktiga Heroma-filer får inte bli automatiska fixtures. |
| [Dashboard-verifiering](../../operations/runbooks/dashboard-widget-framework-verification.md) | Registry/layout/persistens, responsivitet och separat deploy/rollback/manual acceptans; ingen ny testkörning. |
| [Family kontrakt](../../architecture/family-view-family-coordination.md), [Meal Planner](../../knowledge/meal-planner.md), [Shopping List](../../knowledge/shopping-list.md) | Moduler äger sina data; Family koordinerar, skolmat skriver inte middagsplan/inköp. Bredare epic inte levererad genom skollunch. |
| [Skollunchrapport](../features/family/school-meals-preparation-20261023.md#skollunch-v2--default-off-implementation-2026-10-23) | Faktisk senare testyta som TESTING saknar: SchoolMealsTests/SchoolMealLiveTests/MealPlannerApiTests, SchoolLunch/App. Offline transportfixtures, datum/år, default-off, sex timmars cache/errors/no household mutation. Daterat 63 backend/39 frontend/build; inte omkört. Mergestatus enligt STATUS, ingen liveacceptans. |
| [Design manuell verifiering](../../design-system/manual-verification.md), [theme contract](../../design-system/theme-contract-v1.md) | Teman/a11y/200% text/dock/touch/focus/reduced motion, separat Jellyfin/Tizen. Normalviewport och fysisk iPhone/PWA ersätts inte av headless screenshots. |
| [STATUS](../../STATUS.md), [Backlog](../../BACKLOG.md), [DOC-2A](doc-2a-recovery-status-inventory-20261024.md), [DOC-2B](doc-2b-backlog-roadmap-inventory-20261024.md) | Aktuell evidensnivå, kvarstående acceptans/säkerhetskrav och förlustfria källmappningar. Arkivens gamla fortsättningsinstruktioner är inte auktorisering. |

## Överlappningar, motsägelser och osäkerheter

| ID | Konkret observation | Disposition inför fas B |
| --- | --- | --- |
| O1 | T019/026 anger aktiva formatteringsgrindar; T020–028 har older NOT ENABLED/currently fails/next. CI har båda check-only-stegen. | Historik, inte reellt motsägande aktuell CI. Behåll exakta datum/SHA/negativprov i arkiv och en kort aktuell kommandotabell. |
| O2 | T066 säger full API/Sentinel trots Web-only; BB-086 i T079 kräver full backend/frontend för docs-only. AGENTS föreskriver riskbaserad insats. | Bevara dåtida utfört/krävt scope historiskt, inte obligatorisk generell rutin. Riktiga säkerhets-/persistensdelta får inte nedklassas. |
| O3 | T004/017/018 m.fl. har gamla next/not authorized/D NOT STARTED och återkommande reconciliation-CI-order; BB-130 är stängt och Finance fryst. | Gamla next blir inte nya uppdrag. Frysningar och dagens separata SHA-/deploymentbeslut styr. |
| O4 | TESTING har ingen samlad Finance-/BB-158-frysningsingång; T046 innehåller API-outageprov, T064 progressmutation, T081 benchmark/provider/volymoperationer. | REQUIRED GATE kan bevaras utan körorder. Lägg i framtida guide tydlig scope/freezegrind före operationslänkar; behåll journal/no-retry/retentionkrav. |
| O5 | T059–078 blandar smala UX-godkännanden, browserPASS, äldre mini-player/navigation och fysisk pending. Senare accepterad Media döljer separata audiobook-UI. | UNCERTAIN per kvarstående fysisk/designyta. Bevara ratade alternativ, kvalitativ DoD, exakt smalt godkännande; inget nytt generellt modul-PASS eller återinförande av UI. |
| O6 | T079 innehåller många orubricerade Finance- och hostkrav; T081 är både länkkarta och lång historik. | Full originalkopia plus delmappning ovan krävs; arkivera inte enbart efter rubrikens modulnamn. |
| O7 | Backup/restore-README beskriver implementerade BB-085/090-procedurer men avslutar med ”Ingen procedur är godkänd ännu”. Runbookindex säger Finance emergency stop design-only ”until Finance exists”. | UNCERTAIN godkännandescope / uppenbart daterad etikett. Implementerad kod är inte approvalbevis. Fas B får länka med reservation, inte godkänna eller köra procedurerna. Ändring av runbooks kräver separat scope. |
| O8 | TESTING saknar Skollunch v1/v2 och en aktuell Mediekontroll-ingång; katalogen beskriver Skollunch v2 som in review och Mediekontroll som review only. STATUS visar merge/CI och separat deploymentuppgift. | Katalogtexter är historiska checkpoints, inte nuvarande produktstatus. Ingen ny live-/fysisk acceptans här. Framtida guide bör länka STATUS för status och rapport för daterad evidens. |
| O9 | Baseline-index har externa 2026-08-03 ”current inom scope”; docsindex lyfter BB-130 ordered stabilization som ingång. | Datumscopad historik, inte färsk server-/säkerhetssanning eller aktiv sprint. Externa baselineartefakter inte lästa/verifierade här. |
| O10 | Manual-verification använder gamla Tema-val `Ljust` och `Obsidian Gold`; accepterat temakontrakt har senare tre teman. | Instruktionsdrift behöver granskas vid separat dokumentändring. Bevara geometriska/a11y/Tizen-krav; ingen ny visuell acceptans. |
| O11 | `Automatiska tester` saknar formatchecks som faktisk CI kör; `npm audit` förekommer i historik men inte som eget aktuellt CI-steg. | Lyft faktiska checks, skilj kodkonfiguration från utfört resultat och planerad security/pentestbaseline. Ingen validator- eller workflowändring behövs i fas A. |

## Länk- och indexgranskning

Riktad lokal kontroll omfattade **13 filer, 383 relativa Markdown-länkar**, inklusive fragment:
TESTING.md, samtliga sex spårade `docs/indexes/*.md`, reports README/katalog samt operations
README/runbookindex/backup/deployment. **PASS: inga brutna relativa vägar eller ankare hittades
inom denna kontroll.** Rubriksluggar, dubblettsuffix och explicita HTML-ankare kontrollerades.
Backtickreferenser/externa GitHub- och leverantörs-URL:er är inte verifierade som levande resurser.
Dokumentvalidatorns PASS ensam skulle inte bevisa fragmenten, eftersom den stryker `#...` vid kontroll.

| Index | Fynd och minsta förslag, utan ändring nu |
| --- | --- |
| [documentation](../../indexes/documentation.md) | Auktoritetsordningen korrekt bevarad. TESTING finns, men Finance teststrategi/Sentinel security/manual a11y bara indirekt upptäckbara. Föreslå länkar via kort testguide, inte ett nytt testregister. BB-130-etiketten bör datumscopas vid senare godkänd revidering. |
| [operations](../../indexes/operations.md) | Bra proceduringång, men runbooklänk betyder inte Approved; Media-runbook är Draft. Finance-frysning nås via huvudindex, BB-158 är direkt länkat. Framtida tydlig freeze-registerhänvisning räcker; ingen duplicerad grindlista. |
| [reports](../../indexes/reports.md) | Handplockad äldre lista överlappar huvudkatalogen och är inte komplett/current. Behåll historiska länkar, använd katalogen för discovery och STATUS för aktuell evidensnivå. |
| [baselines](../../indexes/baselines.md) | Historisk datumscoping anges, men ordet current kan missläsas. Externa artefakters tillgänglighet/innehåll UNKNOWN, ingen aktuell driftkontroll. |
| [knowledge](../../indexes/knowledge.md) | Säger uttryckligen att knowledge inte ersätter kontrakt. Relevanta Meal/Shopping/auth/Sentinel/temalänkar finns; ingen ny auktoritet behövs. |
| [ADR](../../indexes/adr.md) | Beslutens status måste läsas per ADR; förekomst i index innebär inte automation/PASS. Ingen bruten lokal länk funnen, inga lokala föreslagna ADR accepterade här. |
| [rapportbibliotek](../README.md) / [katalog](../REPORT-CATALOG.md) | Tydligt sanerad evidens och ej automatisk acceptance. Katalogens äldre reviewbeskrivningar behöver historisk scope, inte omskrivning av rapportresultat. Endast ny katalogpost auktoriserad nu. |
| [operations overview](../../operations/README.md) / [runbookindex](../../operations/runbooks/README.md) | Överlapp med operationsindex har olika nivåer; kan förenklas med befintliga länkar vid fas B utan att slå ihop status, guide, runbook och evidens. O7 måste förbli synlig. |

## Kvarstående bindande verifierings- och acceptansgrindar

- **G0 — auktoritet och scope:** AGENTS risknivå/DoD, exakt SHA/merge och separat deployment.
  Ingen historisk next/rerun/cleanup-order är automatisk fortsättning. Kod/test/CI/merge/deploy/manual
  är skilda tillstånd; daterat PASS gäller bara sin revision, miljö och tidpunkt.
- **G1 — frysningar:** hela Finance och BB-158 förblir frysta. RESEARCH / 0 SEK / NONE,
  G3a BLOCKED / G3b NOT RUN. Ingen inferens, grantförnyelse, provider, PAPER/LIVE/AUTO/broker,
  BB-158-isoleringsexperiment, verklig import eller driftprov. Befintlig drift lämnas orörd.
- **G2 — integritet, science och retention:** immutable revision/checksum/replay, concurrency och
  avbrott/reopen, rättigheter före adapter, inherited derived/backup retention/deletion, temporalitet/
  DST/cutoff/no-lookahead, holdout/embargo/negative controls/full attempt population, kostnader och
  ärliga INSUFFICIENT/NOT_EVALUABLE. Frysningen förlänger inga retentionfrister; okänd faktisk deadline
  blir inte PASS. Destruktiv restore/deletion får inte användas för att tvinga ett testresultat.
- **G3 — modeller och authority:** syntetisk proof-process eller CI bevisar inte native hostisolering.
  Version/grammatik/admission, owner-grants/one-shot journals/budget, bounded output/cancel/timeout,
  auth/revocation/durable audit och kontrollerad OS-/resurs-/nätverksgräns kvarstår. Consumed/uncertain
  journaler får inte raderas, återaktiveras eller retryas; gamla failures förblir failures.
- **G4 — Sentinel/host/recovery:** signed/authenticated identity/proof/replay/schema/responselimits,
  least privilege och audit, systemd/reboot/power/BIOS separata från mocks. BB-009 och BB-083:s
  kvarstående scope inte stängt. Inga hostkommandon, stop/restart eller restore i inventeringen.
- **G5 — manuell kvalitet:** normalviewport/touch/fokus/44px där kontrakt kräver, dock/reduced-motion,
  tre teman, lång svensk text, responsiv iPhone/PWA och separat Tizen. BB-097:s uttryckliga tertiary
  40px-undantag får inte osynligt skrivas om. Ratad floating player och begränsad timer-/Finance-
  acceptans bevaras; browserPASS/DOM-klick ersätter inte fysisk ägaracceptans eller liveuppspelning.
- **G6 — engineering:** faktiska formatterings-, build-/test-, docs-/diff-/secretkontroller; inga
  historiska tvåpassworkarounds/auto-fix i CI. Testmatriser väljs riskbaserat, inte utifrån gamla counts.
  Kodgranskning/fixtures är inte bevis för att varje test nu passerar.
- **G7 — säkerhet/pentest/auth:** planerad kontinuerlig secret/dependency/SAST/container/API/header/
  supplychain/configuration-baseline är inte implementerad i sin helhet av fyra CI-jobb. Mandatory
  före real-money LIVE och stark före PAPER/execution; striktare accepterade kontrakt försvagas inte.
  Auth-design före broker/Docker/kamera/high-impact HA/bredare nätverk; negativa IDOR/injection/
  XSS/CSRF/CORS/resource-/restoreprov med isolerade testdata. Extern Sentinel-pentest före remote
  nodes/mutationer enligt dess kontrakt. Ingen securitywaiver genom historikflytt.
- **G8 — Media och Family:** mocks får inte mutera live torrents/Arr/Prowlarr, starta playback eller
  röra användarprogress. Opaque IDs, explicit edition/preview/confirmation, idempotent timeout-
  reconciliation, rättigheter/retention/audit/no-overwrite och exakt ABS-indexering består. Download
  Controls destruktiva/Arr/Retry- och Mediekontrolls fysiska grindar förblir separata. Family använder
  källmoduler; skollunchens offline-fixturer och merge/CI är inte livekällor eller ägaracceptans.

## Förslag till fas B — inte genomfört

En enda kort aktiv TESTING-guide kan innehålla:

1. Scope/risk/DoD och freeze-register först; tydlig skillnad mellan metod, utfört resultat och körningstillstånd.
2. Aktuella lokala/CI-kommandon med arbetskatalog, check-only och när fokuserat/fullt är motiverat.
3. Modul-/säkerhetskarta till befintliga kontrakt/runbooks, inklusive Skollunch och Sentinel.
4. Separat manuell/fysisk/operativ acceptans, inga generella livekommandon.
5. Länk till full historik och rapportkatalog; datum/SHA-resultat hör hemma i befintlig evidens.

Föreslaget enda arkiv: `docs/history/testing-before-doc-2c-20261024.md` (inte skapat;
arkivdatum fastställs vid faktisk flytt). Bevara hela originaltexten, rubriker/BB-ID/datum/SHA,
felresultat, ägarbeslut, commands och orubricerade krav. Endast arkivförklaring och reversibla
relativa länkjusteringar. Inventeringens 85 intervall plus T079/T081:s blandfall ska få faktiska
arkivdestinationer. Fulltextintegritet ska verifieras mekaniskt mot denna baslinje.
Aktiva G0–G8 och UNCERTAIN-krav behålls synliga med exakta källor, inte gömda i ett stort arkiv.

Index behöver inte bli ett nytt register: documentation → TESTING för metod; operations för
procedur med egen status; rapportkatalog för dated evidence; STATUS för verifieringsnivå.
Historiska rapporter och indexlänkar behålls. Auktoritetsordningen ändras inte.

### Beslutspunkter inför fas B

- **D1:** godkänn denna klassificeringsgräns och en fulltextarkivering med aktiv metod-/grindkarta;
  ingen automatisk konsolidering efter fas A.
- **D2:** godkänn att O1–O3:s checkpointbundna svit-/next-order markeras historiskt, medan de
  underliggande invariants/säkerhetskraven bevaras. Inget nytt generellt testtak eller waived gate.
- **D3:** behåll O5:s fysiska/designacceptans och O7:s backup-/runbookgodkännanden som UNCERTAIN.
  Ingen ny runtimeutredning behövs för att bevara osäkerheten. Eventuell saklig korrigering utanför
  TESTING/index kräver separat scope, inte tyst omskrivning av kontrakt eller runbook.
- **D4:** välj minsta indexjusteringar (O8–O11) inom framtida auktorisering: korrekt statuslänk,
  formatteringskommandon och discovery av befintliga testkontrakt, inga nya processlager.

## Verifiering, begränsningar och återupptagning

- **PASS:** `node scripts/verify-documentation.mjs`: 283 Markdown-filer, 53 unika aktiva backlog-ID.
- **PASS:** `git diff --check`; riktad täckningskontroll 85/85 rubriker, exakta rubriktexter och
  källintervall som partitionerar samtliga 1286 rader. T079:s tolv orubricerade områden särmappade.
- **PASS:** slutlig länkkontroll av samma 13 ingångar plus ny rapport: 14 filer och 439 relativa
  länkar/ankare (inklusive katalogens nya länk), inga fel. Externa länkar inte livekontrollerade.
- **PASS:** tvåfilsscope och riktad sekretesskontroll. TESTING, index, kod/tester, workflows,
  runbooks, STATUS/Backlog/Roadmap och frysningsbeslut oförändrade; orelaterat arbete bevarat.
- **SKIP:** applikationstester/build och produktions-/externa kontroller; inte motiverade eller
  auktoriserade för denna rena inventering. Tidigare PASS citeras med sitt ursprungliga scope.

Inga applikationstester,
fulla historiska CI-omgranskningar, live HTTP, modell-/Finance-/BB-158-körningar eller driftprov.
Ingen coverage-/säkerhetscertifiering eller ny produktacceptans påstås. Inga rapporterade hemligheter,
credentials, privata adresser, råloggar eller privata medie-/serveridentiteter inkluderas.

Nästa steg är oberoende GitHub-granskning av exakt publicerad SHA; ägaren måste separat auktorisera
fas B och eventuell senare merge. Följ AGENTS, verifiera då main/review och working tree igen,
återanvänd oförändrad evidens. Inget aktivt implementationsavbrott skapas av denna inventering.
