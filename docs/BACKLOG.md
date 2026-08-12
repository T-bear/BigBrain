# BigBrain Backlog

### BB-083 – Appliance lifecycle and crash recovery baseline

- Modul: Platform / Operations / Finance safety; prioritet P0.
- Status: implementerad och container-runtime-verifierad 2026-08-12; hostinstall,
  Docker-daemon restart/reboot blocked by interactive sudo; physical power-cycle pending.
- Delivered: ADR 0026, clean/unclean journal, recovery/storage/disk/clock gate, explicit job
  policies, interrupted EODHD safety, API/UI, Compose grace/readiness, systemd and scripts.
- Evidence: API PID-1 crash returned healthy/`UNCLEAN`; Finance stayed at 16 requests,
  4,016 raw observations and 16 immutable revisions.
- Report: `docs/reports/features/resilience/bigbrain-bb-083-appliance-resilience-20260812.md`.

Senast uppdaterad: 2026-08-10

Detta dokument samlar verifierade buggar, teknisk skuld och framtida förbättringar som ännu inte är implementerade.

### BB-039 – Media Controller – serie-, säsongs- och avsnittsstatus

- Modul: Media
- Typ: Framtida funktion
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-09

En framtida Media Controller ska via normaliserade Media/Arr-kontrakt visa antal släppta,
lokalt tillgängliga, saknade och kommande avsnitt, per-season breakdown och status på
avsnittsnivå. En framtida uttrycklig ”Sök saknade”-capability kräver eget säkert
mutationskontrakt. Funktionen får inte byggas på Download Controls presentation state.

#### Definition of Done

- Släppta, lokalt tillgängliga, saknade och kommande avsnitt normaliseras från verifierade källor.
- Serie-, säsongs- och avsnittsnivå är konsekventa och testade.
- ”Sök saknade” har separat auktorisering, preview/audit och versionsverifierad Arr-adapter.
- Ingen direkt koppling till Download Control-UI eller rå providerpayload finns.

Prioritet:

- P0 – kritiskt fel eller risk för dataförlust
- P1 – blockerande eller tydligt störande fel
- P2 – viktigt men arbetet kan fortsätta
- P3 – förbättring eller lågprioriterad teknisk skuld

Status:

- Ny
- Bekräftad
- Planerad
- Pågår
- Klar
- Avvisad

### BB-040 – Download Control – snabb navigation vid många färdiga nedladdningar

- Modul: Media / Download Control
- Typ: UX / navigation / responsivitet
- Prioritet: P2
- Status: Pågår
- Upptäckt: 2026-08-10

När historiken innehåller många färdiga nedladdningar blir Download Control mycket lång.
Användaren måste scrolla långt mellan aktiva eller problematiska poster, relevanta
kontroller och färdiga poster; problemet är särskilt tydligt på mobil.

Önskat resultat är snabb och begriplig navigation oavsett historikens längd. Lösningen
ska utredas utifrån beteendet och får inte låsas till en viss komponent. Kollapsbar
sektion, ”Visa fler”, statusfilter, separat historikvy, snabbnavigation eller en
kombination kan utvärderas. Befintlig batchhantering och dess säkerhetsgränser ska
bevaras.

#### Definition of Done

- Användaren behöver inte scrolla genom en lång lista med klara nedladdningar för att nå relevanta kontroller.
- Lösningen fungerar väl på mobil och desktop samt med tangentbord och skärmläsare.
- Aktiva och problematiska nedladdningar prioriteras visuellt.
- Färdiga nedladdningar är fortsatt enkla att hitta och komma åt.
- Urval, filtrering och befintlig batchhantering fungerar fortsatt utan ändrad mutationsgräns.
- Vald lösning har automatiska regressionstester och manuell responsiv verifiering.

#### Sprint 3 implementation 2026-08-10

Standardvyn grupperar i ordningen fel/problem, aktiva, köade/pausade och klara.
Klara är kollapsad som standard när alla statusar visas, med antal och en tillgänglig
Visa/Dölj-kontroll; filtret Klara visar fortsatt hela gruppen direkt. Filter, urvalets
filtrerade scope, batchverktyg och objektsåtgärder ligger kvar ovanför historiken.
Lösningen är implementerad, testad med 30 klara poster, production-byggd och Web-deployad
2026-08-10. Sprint 3-leveransen är stängd efter teknisk runtimeverifiering och omedelbar
produktägaracceptans utan blockerande fynd. BB-040 är ändå inte Klar eftersom dess fulla
DoD kräver längre kvalitativ mobil-, desktop-, tangentbords- och overflowverifiering.
Den uppföljningen sker i BB-041 och blockerar inte den stängda sprinten.

### BB-041 – Download Control – post-release UX-utvärdering efter Sprint 3

- Modul: Media / Download Control
- Typ: UX validation / post-release evaluation
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-10

Sprint 3 är implementerad, automatiskt verifierad, production-byggd, Web-deployad och
tekniskt accepterad utan kända blockerande regressioner. Produktägaren behöver använda
den nya presentationen över tid innan en slutlig kvalitativ bedömning kan göras av
statusgruppering, kompakt historik, vardagsnavigation, skillnaden mellan Nedladdningskö
och Medieflöde samt mobil- och desktopflöde.

Detta är post-release validation, inte en Sprint 3-blockerare, känd defekt eller
misslyckad verifiering. Ingen implementation ingår i kortet om faktisk användning inte
identifierar ett separat verifierat förbättringsbehov.

#### Definition of Done

- Produktägaren har använt den deployade UX:en under en representativ period.
- Statusgruppering, kompakt historik och navigation bedöms på mobil och desktop.
- Skillnaden och den avsiktliga överlappningen mellan Nedladdningskö och Medieflöde är begriplig.
- Eventuella verifierade fynd registreras som separata backloggposter; avsaknad av fynd dokumenteras också.
- BB-040 och BB-033 bedöms mot sina återstående DoD-punkter med daterad evidens.

---

## BigBrain Finance – policy-governed autonomous trading

All Finance items are planning unless explicitly marked complete. No trading mode,
broker connection or live authority exists. Dependencies refer to BB items and the
[Finance master roadmap](architecture/finance/master-roadmap.md).

### BB-042 – Epic: BigBrain Finance – policy-governed autonomous trading

- Modul: Finance
- Typ: Epic
- Prioritet: P2
- Status: Pågår – provider-neutral M2 foundations inklusive immutable revision assembly verifierade
- Upptäckt: 2026-08-10
- Beroenden: BB-043; all promotion gates in the master roadmap.

Deliver reproducible research, backtesting and paper trading before progressively gated
manual, limited and policy-governed automatic trading. Preserve capital, seek positive
net expectancy after costs, control drawdown and permit zero-trade days. Automatic
trading is the eventual destination, never the starting state or a guaranteed return.

#### Definition of Done

- M0–M15 meet their individual DoD and evidence gates.
- Live modes have current legal/operational review and explicit owner approval.
- Risk cannot be bypassed; emergency controls, audit and reconciliation are proven.

### BB-043 – Finance M0 architecture and safety baseline

- Modul: Finance / Architecture
- Typ: Planning / documentation
- Prioritet: P1
- Status: Klar
- Beroenden: none.
- Definition of Done: master roadmap, module/architecture, threat model, ADRs, runbooks,
  test plan and granular backlog are validated and published. Completed 2026-08-10.

### BB-044 – M1 Finance domain skeleton and read-only module

- Modul: Finance
- Typ: Architecture / implementation
- Prioritet: P2
- Status: Klar
- Beroenden: BB-043 and ADR 0017 review.
- Definition of Done: versioned money/instrument/time, signal and portfolio primitives,
  module registration and read-only fakes have unit/property tests; no broker or order path.
- Slutförd: 2026-08-10. Finance registreras read-only som Research; decimal-/UTC-typer,
  provider-neutral observation/strategy, risk/policy, journal, paper-domain records och en
  fixture-only reference pipeline är implementerade och testade. Ingen executor, extern
  provider, broker, credential, Finance-write-endpoint, persistence, UI eller deployment finns.

### BB-045 – M2 historical market-data ingestion and provenance

- Modul: Finance / Market Data
- Typ: Implementation
- Prioritet: P2
- Status: Pågår – synthetic acquisition + manifest/persistence benchmark foundation verifierad
- Syfte: build a reusable local market-data memory whose exact inputs and permitted uses
  remain reproducible and enforceable.
- Scope: entitlement/allowed-use model, provenance envelope, canonical raw OHLCV and
  corporate actions, immutable dataset revisions, quality/correction handling, measured
  self-hosted persistence and a provider adapter only after authorization.
- Beroenden: BB-044, BB-046 and Accepted ADR 0021. Provider-neutral types/fixture tests are
  not blocked; provider activation and persistence of real provider data are blocked by
  BB-071.
- Risk: license laundering through normalized/derived copies, silent correction, bias,
  stale/gapped data, backup over-retention and premature database choice.
- Definition of Done: licensed, versioned datasets normalize reproducibly with source,
  timezone, corporate-action, quality, gap and duplicate tests; policy denies unknown,
  expired or undeclared use; correction/deletion/backup behavior is explicit and tested.
- Verification: synthetic unit/property/integration fixtures prove entitlement,
  provenance, lineage, deterministic replay, correction supersession and deletion scope;
  owner-reviewed BB-071 evidence is required before any external-data acceptance test.
- Evidens 2026-08-10: allowed-use enums, provider/product-scoped policy and stable
  fail-closed results, retention/deletion metadata, immutable dataset revisions,
  provenance quality/classification and derived parent lineage are implemented. Synthetic
  tests cover explicit allowed/denied/unknown, missing/expired/mismatched policy,
  persistence, post-subscription retention and determinism. Full solution: 291/291 tests.
- Evidens 2026-08-11: stable canonical Equity/ETF identity and inclusive effective-date
  provider symbol mappings distinguish MIC venues and preserve identity across renames.
  Synthetic decimal daily OHLCV, raw/adjusted basis, cash dividends, exact rational splits,
  stable findings and duplicate/conflict handling normalize deterministically while
  preserving dataset revision and entitlement policy. Full solution: 305/305 tests.
- Evidens 2026-08-11 (session/replay): fixture-only Trading/Closed/Unknown sessions use an
  explicit timezone and reject invalid/ambiguous DST times. Expected closure, unknown
  session, generic missing observation, explicit provider gap and invalid observation stay
  distinct. Replay binds one immutable revision, orders events deterministically, exposes
  observations only at supplied availability time, resolves historical symbols and emits
  dividend/split events without rewriting raw bars. Full solution: 318/318 tests.
- Evidens 2026-08-11 (revision assembly): immutable snapshots inherit ordered membership
  through an explicit parent chain. Corrections bind original/replacement member IDs,
  reason/evidence and inclusive availability; catalog as-of selection cannot expose a
  future correction. Old revisions remain directly reproducible and linear supersession,
  cycle/reference/scope/future-member invariants fail closed. Corporate-action and
  session/gap evidence retain source revision and policy. Full solution: 336/336 tests.
- Evidens 2026-08-11 (synthetic acquisition): provider-neutral request/batch/observation,
  pagination, completeness, provenance and correction contracts feed the existing canonical
  normalizer and immutable revision assembler. The gate requires explicit analysis,
  backtest, derived-metric, long-term-storage and persistence permission before adapter
  invocation. `SyntheticFixture` cannot masquerade as real authorization. Identical retry/
  overlap is deterministic, conflicts fail, and the immutable journal records policy,
  retention/deletion, counts, findings, failure and resulting revision without secrets.
  Twelve focused tests passed; full solution: 348/348 tests.
- Evidens 2026-08-11 (synthetic persistence): immutable manifests bind dataset/revision,
  coverage/counts, SHA-256 content identity, acquisition/policy/provenance, storage version
  and retention/deletion obligations. The provider-neutral contract and in-memory reference
  prove atomic complete-only append, exact/range/action/gap/lineage reads, idempotency,
  conflict rejection, integrity and policy-scoped auditable deletion. A repeatable JSONL/
  SQLite benchmark measured 2,520, 126,000 and 1,260,000 fixture rows. Large-case JSONL was
  155,992,420 bytes and queried 100 instrument rows in 153.477 ms; SQLite was 356,208,640
  bytes and 0.053 ms, with slower initial write (9,674.985 ms versus 793.331 ms). This
  supports a provisional immutable-file + SQLite catalog/index direction, not activation.
  Twelve focused tests passed; full solution: 360/360 tests.
- Kvar: production-store/backup benchmarks and implementation, richer quality aggregation,
  entitlement evidence for an
  eligible source, authorized provider adapter and external acceptance. Actual
  provider ingestion/persistence remains blocked by BB-071.

### BB-046 – Market-data licensing, retention and provider research

- Modul: Finance / Governance
- Typ: Research / legal-operational gate
- Prioritet: P2
- Status: Klar
- Beroenden: BB-043.
- Definition of Done: owner-reviewed provider/API usage, licensing, retention and
  redistribution constraints are documented without unsupported legal conclusions.
- Slutförd: 2026-08-10. Eight candidates were compared using public provider sources.
  Twelve Data is the primary Nordic/global EOD candidate, Tiingo the US EOD specialist
  and Massive the US-depth alternative. No activation is authorized because long-term
  local retention and post-cancellation rights remain uncertain; BB-071 is the explicit
  confirmation gate. Daily raw OHLCV plus separate corporate actions is the M2 baseline.

### BB-071 – Confirm market-data storage and retention entitlement

- Modul: Finance / Market Data Governance
- Typ: Licensing confirmation / implementation gate
- Prioritet: P1
- Status: Klar – Twelve Data Personal entitlement verifierat; provider selection/activation separat
- Upptäckt: 2026-08-10
- Beroenden: BB-046 and Accepted ADR 0021.
- Syfte/scope: establish the exact provider/product/market entitlement that may govern
  BB-045 raw, normalized, corporate-action and derived evidence.
- Risk: inferring permission from API access, free pricing or silence would make stored
  evidence unusable and may violate provider/exchange terms.

Obtain written confirmation for the exact intended individual/personal product and
markets before creating an account or ingestion adapter. Confirmation must cover local
storage/caching duration, deterministic backtesting, derived metrics/reports, corporate
actions, retention after cancellation, non-redistribution, Swedish residence and any
exchange-specific terms. Compare Twelve Data with Tiingo/Massive where scope differs.

#### Definition of Done

- Product owner reviews dated provider responses/terms for the selected dataset.
- Permitted instruments, intervals, storage duration, derived use and cancellation
  behavior are explicit; uncertainty blocks ingestion.
- Required subscription/exchange fees and credential class are documented without secrets.
- One provider/product scope is approved for BB-045, or selection is explicitly rejected.
- Verification: dated first-party terms or written provider response is mapped to each
  allowed use, retention/deletion class, market and product; unknown remains fail-closed.

Research update 2026-08-10: public terms confirm personal/internal use in differing
scopes, but they do not grant BigBrain's complete required retention. Twelve Data states
that data is retained only for the subscription-permitted duration and deleted within
30 days after termination. Tiingo requires prompt permanent deletion on termination and
written approval for derived-data creation/retention. Massive requires use to cease and
all market data to be deleted on termination, and its individual terms restrict
non-display/derived use. BB-071 therefore remains open. The ready-to-send questions are
in `docs/architecture/finance/provider-retention-inquiry.md`.

Gate update 2026-08-11: Twelve Data Basic is the conditional technical lead for a small
US-only experiment, but public evidence still leaves durable retention, forward testing,
retained derived artifacts and exchange/termination scope unresolved. Nasdaq Nordic's
delayed files require prior approval for value-added use. BB-071 remains open; every
unchecked item in the published exact-product checklist denies ingestion.

Resolution attempt 2026-08-11: public Twelve Data Basic evidence closes technical US
historical/current access, internal processing/non-display, compliant derived creation and
the 30-day termination deletion rule. It does not close local retention duration,
replay/backtest, forward/shadow evidence, strategy training, provenance or post-termination
derived retention. Status remains **Pågår – väntar på leverantörsbekräftelse**. The narrowed
ready-to-send inquiry is `docs/architecture/finance/provider-retention-inquiry.md`; a free
account is required only for later API access, not for sending the inquiry.

Human evidence update 2026-08-11: Liam at Twelve Data confirmed in writing that the
submitted private, self-hosted, non-commercial and non-redistributed BigBrain use may use a
Personal plan. Local storage/retention, testing/research, post-termination retention,
derived data, audit metadata and use for investment decisions involving only the owner's
own funds are supported for that scope. Basic is evaluation/trial-symbol access and is not
authorized for the intended operation; a paid Personal plan is required. BB-071's Twelve
Data entitlement question is therefore resolved, but no provider is selected or activated.
Commercial/paying-subscriber, redistribution, third-party/customer, unknown-market or
materially changed use remains denied pending renewed review. Cost-first selection now
continues with Alpaca Basic/free IEX entitlement research before any paid decision. Evidence:
`docs/reports/features/finance/finance-twelve-data-human-entitlement-confirmation-20260811.md`.

### BB-072 – FREE HISTORICAL DATA INGESTION preparation and source research

- Modul: Finance / Market Data Governance
- Typ: Research / future ingestion gate
- Prioritet: P2
- Status: Klar – research verifierad; ingen gratis källa auktoriserad
- Upptäckt: 2026-08-11
- Beroenden: BB-045 provider-neutral foundation; BB-071 before any real-data ingestion/storage.
- Syfte/scope: compare zero-cost historical sources without activating one. Evaluate cost,
  license/ToS, local retention, personal backtesting rights, reproducible acquisition,
  rate limits, coverage, survivorship/delisted instruments, corporate actions, raw/adjusted
  prices, symbol history and quality.
- Non-goals: account, API key, adapter, download, provider payload, persistence or purchase.
- Definition of Done: dated first-party evidence and synthetic contract-fit analysis identify
  whether any free source is legally and technically eligible; Unknown remains fail-closed.
  Any recommended activation still requires explicit product-owner review and BB-071-class
  entitlement evidence for the exact source/product/market/use.
- Slutförd 2026-08-11: ten exact source/product paths were compared using dated first-party
  evidence. None verified the complete durable-retention and personal non-display
  backtesting grant. EODHD Free Starter is the best conditional evaluation lead but has
  one year of free history and a post-expiry deletion duty. Twelve Data Basic is the best
  Nordic technical lead but exact retention/derived-use rights remain unresolved.
  Massive prohibits the required default non-display/strategy-derived use, FMP requires
  prior written download/derivative approval and Stooq rights remain unknown. Result:
  **DO NOT INGEST YET**. No account, key, adapter, download, purchase or persistence was
  created. Evidence: `docs/reports/features/finance/free-historical-data-source-research-20260811.md`.

### BB-073 – Live market observation and prospective shadow learning

- Modul: Finance / Market Observer / Strategy Research
- Typ: M2/M3 provider-neutral implementation and entitlement research
- Prioritet: P2
- Status: Pågår – synthetic live-observation/shadow-learning foundation verifierad; external feed blocked
- Upptäckt: 2026-08-11
- Beroenden: BB-045 foundations; BB-071 and explicit product-owner approval before external data.
- Syfte/scope: build a broker-free forward evidence stream with explicit event/provider/
  received/knowledge time, honest freshness, deterministic observation/gap/outage/correction
  handling, immutable versioned shadow predictions, later outcomes and prospective metrics.
- Non-goals: provider adapter/account/key, external payload, broker, order, PAPER/LIVE/AUTO,
  profitability claim, self-modifying strategy/risk or runtime deployment.
- Definition of Done: an authorized exact feed can be normalized and entitlement-aware
  persisted, shadow evaluation cannot look ahead, predictions/outcomes remain immutable and
  version-isolated, prospective metrics include costs/tail risk, and no execution capability
  exists. Provider activation requires separate owner approval.
- Evidens 2026-08-11: `LiveMarketObservation` and deterministic synthetic feed model four
  clocks, delay, session/missing/outage, duplicates and corrections. A fail-closed live gate
  requires analysis/walk-forward/training/derived/storage rights. The explicit non-production
  fixture rule appends immutable predictions and later outcomes and computes version-isolated
  return/excursion/volatility/cost metrics. Sixteen focused tests passed; full suite count is
  recorded in STATUS/report after publication. Free-live research reviewed Twelve Data Basic,
  Alpaca Basic/IEX, EODHD Free and Alpha Vantage Free. Twelve Basic is the best conditional
  technical candidate at that review point, but later human evidence established that Basic
  is insufficient and a paid Personal plan is required.
- Consolidated evidence 2026-08-11: the historical/live scorecard, capacity estimate and
  Twelve Data Basic checklist are published in
  `docs/reports/features/finance/finance-free-market-data-provider-evaluation-20260811.md`.
  It authorizes no provider. FIRST AUTHORIZED MARKET DATA INGESTION requires written
  entitlement evidence and a separate explicit product-owner approval.
- Entitlement update 2026-08-11: Twelve Data is an entitlement-cleared paid fallback for
  the submitted personal use, not a free or active provider. Basic is insufficient. Alpaca
  Basic/free IEX is the next cost-first candidate, with entitlement unresolved and no
  account, key, SDK, adapter or inquiry claimed.
- Prepared, not authorized: an eight-symbol US-only 15-minute REST/EOD experiment with a
  356/800-credit hard daily plan and no WebSocket, provider call, broker or order. Evidence:
  `docs/reports/features/finance/finance-bb071-entitlement-resolution-20260811.md`.
- Kvar: entitlement evidence and owner approval; authorized adapter/local live memory; richer
  calibration/drawdown/regime/instrument/horizon metrics; feature engine and strategy research.

### BB-074 – Early read-only Finance market observation UI

- Modul: Finance / Web / Market Observer
- Typ: M2 read-only implementation
- Prioritet: P2
- Status: Klar – implementerad, automatiskt verifierad, deployad och tekniskt runtime-verifierad 2026-08-11; ej manuellt produktägarverifierad
- Beroenden: BB-044 and provider-neutral BB-045/BB-073 foundations; BB-071 before real data.
- Scope: versioned provider-neutral snapshot, fail-closed production reader, research
  watchlist, Finance view, empty/synthetic/stale/gap/session/quality states, memory panel
  and accessible chart.
- Non-goals: provider account/key/SDK/adapter, real payload, feed, broker, order, PAPER/LIVE/
  AUTO, portfolio/risk/execution controls or deployment.
- Definition of Done: navigable responsive UI, prominent RESEARCH/no-real-money and BB-071
  denial, explicit fixtures, green API/UI/full regressions/build/docs gates.
- Evidence: `docs/reports/features/finance/finance-read-only-market-observation-ui-foundation-20260811.md`.
- Deployment evidence: `docs/reports/features/finance/finance-read-only-market-observation-ui-deployment-20260811.md`.
- Roadmap semantics: early M2 research observation; BB-059/M8 remains planned for trading UI.

### BB-075 – Zero-cost real market-data activation gate

- Modul: Finance / Market Data Governance
- Typ: Research / entitlement / conditional activation
- Prioritet: P1
- Status: Klar – fail-closed research publicerad; ingen 0-SEK-källa auktoriserad
- Upptäckt: 2026-08-11
- Budgetkrav: externa Finance market-data-tjänster = exakt 0 SEK tills nytt explicit
  produktägarbeslut.
- Scope: fresh first-party review of Alpaca Basic/IEX, Stooq, Yahoo/yfinance, Nasdaq Data
  Link, EODHD Free, Alpha Vantage Free, Finnhub Free, FMP Basic and credible direct/open
  alternatives; implement only if the exact automation/storage/research gate passes.
- Definition of Done: provider matrix maps exact product/cost/market/automation/storage/
  retention/termination/replay/derived/audit/own-funds/exchange rights; unknown fails closed;
  adapter/runtime only if every required right is verified.
- Evidence 2026-08-11: no source passed. Alpaca and EODHD require human clarification;
  Stooq, Nasdaq Data Link and Finnhub lack complete exact evidence; Yahoo/yfinance and Alpha
  Vantage are incompatible; FMP requires prior written download/derivative approval. Twelve
  Data Personal remains an entitlement-cleared paid fallback inactive under the zero budget.
- Runtime correction: the no-provider observation projection now reports
  `ZERO-COST ENTITLEMENT GATE`, not superseded `BB-071 / STATE B`; ingestion/storage remain false.
- Report: `docs/reports/features/finance/finance-zero-cost-real-market-data-gate-20260811.md`.
- Kvar: obtain exact written Alpaca answers; then EODHD only if needed. No account/key/adapter
  or provider request before a complete affirmative answer.

### BB-076 – Pragmatic zero-cost personal-research activation

- Modul: Finance / Market Data Governance
- Typ: Policy / entitlement / conditional activation
- Prioritet: P1
- Status: Klar – policy implementerad och verifierad; aktivering fail-closed vid teknisk gate
- Upptäckt: 2026-08-11
- Scope: capability-specifik ägaracceptans av residual osäkerhet för legitima 0-SEK-källor,
  endast privat, read-only, icke-kommersiell personlig research utan identifierat förbud.
- Evidens: ADR 0022 och policytypen skiljer explicit provider grant, owner-accepted personal
  research, human confirmation och denied. Negativa villkor, betalning, förhandsgodkännande
  och tekniska åtkomstkontroller kan inte åsidosättas.
- Aktiveringsresultat: Stooq daily history nådde ägaraccepterad evidensklass, men den
  officiella CSV-ytan returnerade en JavaScript-verifieringskontroll. Ingen bypass, adapter,
  riktig observation, lokal memory, replay eller deployment skapades.
- Kvar: få en normalt stödd Stooq-automationsväg eller välj en separat clearad källa med
  konto/key och implementerbar retention/termination-livscykel.
- Report: `docs/reports/features/finance/finance-bb-076-owner-accepted-zero-cost-policy-20260811.md`.

### BB-077 – EODHD Free real-data activation

- Modul: Finance / Market Data
- Typ: Implementation / entitlement / retention
- Prioritet: P1
- Status: Klar vid credential-gränsen; runtimeuppföljningen är levererad av BB-078
- Upptäckt: 2026-08-11
- Entitlement: current `Free` (€0) permits private non-commercial storage/manipulation/
  analysis while active; all copies must be deleted within one month after expiry.
- Levererat: server-side EOD adapter, eight-symbol mapping, bounded rate/retry, SQLite WAL,
  content-addressed payloads, immutable revisions, acquisition journal, real read model/UI,
  deterministic replay and preview-confirm-delete receipt workflow.
- Säker default: provider är fortsatt disabled utan `FINANCE__EODHD__APITOKEN`, explicit
  active-account state och enable flag. Ingen trading capability skapades.
- Uppföljning: konto/secret, real bootstrap, restart/idempotens, exact-revision replay och UI
  verifierades senare i BB-078; BB-077-rapporten bevaras som historisk credential-bound evidens.
- Reports: `docs/reports/features/finance/finance-eodhd-free-entitlement-revalidation-20260811.md`
  and `docs/reports/features/finance/finance-eodhd-credential-bound-activation-20260811.md`.

### BB-078 – First real Finance market data activation

- Modul: Finance / Market Data
- Typ: Runtime activation / verification
- Prioritet: P1
- Status: Klar; implementerad, deployad och runtime-verifierad 2026-08-11
- Evidens: credential verifierades endast som `present`; åtta av åtta bounded EODHD Free-
  anrop lyckades utan retry för SPY, QQQ, IWM, AAPL, MSFT, JPM, XOM och JNJ.
- Resultat: 2 008 reala daily OHLCV-observationer, åtta content-addressed payloads och åtta
  immutable revisioner täcker 2025-08-11–2026-08-10 i beständig lokal SQLite-memory.
- Verifierat: API/UI REAL EOD, aktiv retention, exakt-revision deterministic replay,
  restartöverlevnad och dagens skip/idempotens utan ytterligare provideranrop.
- Avgränsning: Finance är fortsatt RESEARCH; ingen live feed, broker, order eller trading.
- Report: `docs/reports/features/finance/finance-bb-078-first-real-market-data-activation-20260811.md`.

### BB-079 – First real feature / indicator engine

- Modul: Finance / Research features
- Typ: Implementation / persistence / runtime verification
- Prioritet: P1
- Status: Klar; implementerad, deployad och runtime-verifierad 2026-08-11
- Resultat: provider-neutral `core-daily-v1` beräknade 42 168 immutable feature-värden
  (39 616 available, 2 552 warmup, 0 quality issues) från exakt åtta reala EODHD-
  marknadsrevisioner utan nytt provideranrop.
- Verifierat: kända formelsvar, deterministic checksum/idempotens, causal no-lookahead,
  correction lineage, SQLite reopen/restart, bounded API, responsiv UI och retention/deletion
  scope för beroende features.
- Avgränsning: raw close/OHLC och providerklassificerad volym; inga signaler, strategier,
  portfolio/order/PAPER/LIVE eller brokerfunktioner.
- Report: `docs/reports/features/finance/finance-bb-079-first-real-feature-engine-20260811.md`.

### BB-047 – M3 deterministic backtest engine

- Modul: Finance / Backtesting
- Typ: Implementation
- Prioritet: P2
- Status: Klar genom BB-080; implementerad, automatiskt verifierad, deployad och runtime-verifierad 2026-08-12
- Beroenden: BB-045 och BB-079.
- Definition of Done: versioned datasets/parameters replay deterministically with order
  and portfolio simulation, benchmarks, gross/net metrics and reproducible reports.

### BB-080 – First deterministic real-data backtest engine

- Modul: Finance / Backtesting / Web
- Typ: Implementation / persistence / runtime evidence
- Prioritet: P1
- Status: Klar 2026-08-12
- Resultat: immutable exact-revision runs för buy-and-hold, SMA10/20 och momentum20 med next-session-open, whole-share portfolio, zero/conservative costs, journal, fills, curves, metrics, benchmark, read-only API/UI och EODHD deletion lineage.
- Verifierat: golden/no-lookahead/determinism/cost/edge/retention tests, real BB-078/079 offline run, idempotent restart, build/deploy/runtime. Ingen trading capability.
- Report: `docs/reports/features/finance/finance-bb-080-deterministic-real-data-backtest-20260812.md`.

### BB-081 – Robustness / out-of-sample strategy evaluation foundation

- Modul: Finance / Strategy Lab / Web
- Typ: Implementation / immutable evidence / runtime verification
- Prioritet: P1
- Status: Klar 2026-08-12
- Resultat: versionerad exact-lineage evaluation plan, kronologiska splits och embargo, expanding walk-forward, bounded parameter/cost sensitivity, transparent score, hård insufficiency override, immutable SQLite evidence samt read-only API/UI.
- Verifierat: 70 reala underliggande runs över buy-and-hold/SMA/momentum, no-leakage/determinism/cost/split/retention tests, persistence/restart och runtime. Alla verdict är korrekt `INSUFFICIENT_DATA`; ingen PAPER/LIVE/trading.
- Report: `docs/reports/features/finance/finance-bb-081-robustness-out-of-sample-20260812.md`.

### BB-082 – Longer zero-cost historical market memory

- Modul: Finance / Market Data / Strategy Lab
- Typ: Provider research / entitlement / conditional implementation
- Prioritet: P1
- Status: Blockerad 2026-08-12 – legitimt stoppvillkor; ingen ny data ingested
- Budget: 0 SEK.
- Resultat: aktuell kontroll av Stooq, EODHD Free, Alpha Vantage Free och Nasdaq Data Link
  fann ingen implementerbar nollkostnadskälla som materiellt förlänger den nuvarande
  US-equity/ETF-historiken. Stooqs villkors- och CSV-ytor kräver JavaScript-verifiering;
  kontrollen kringgicks inte. EODHD Free stannar vid cirka ett år, Alpha Vantage full daily
  history är premium och Nasdaq Data Links relevanta US EOD-produkt är premium.
- Bevarat: all BB-078–081-evidens, EODHD daily acquisition och Finance `RESEARCH` är
  oförändrade. Inga feature-revisioner, backtests eller robustness-evaluations skapades.
- Nästa nollkostnadsväg: begär en normalt stödd Stooq bulk/API-väg med uttrycklig
  automations- och lagringsklarhet, eller utvärdera en namngiven offentlig/open-datafil med
  verifierbar redistribution/provenance; ändra inte budget eller kringgå åtkomstkontroll.
- Report: `docs/reports/features/finance/finance-bb-082-zero-cost-history-reassessment-20260812.md`.

### BB-048 – Transaction-cost, slippage and fill simulation

- Modul: Finance / Backtesting
- Typ: Implementation / validation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-047.
- Definition of Done: applicable fees, spread, slippage, FX, delay, partial fill and
  rejection are modeled; a strategy negative after costs fails validation.

### BB-049 – Anti-overfitting and out-of-sample validation

- Modul: Finance / Strategy Lab
- Typ: Statistical validation
- Prioritet: P1
- Status: Grund levererad genom BB-081; längre historik, train/validation/test-selection governance och multiple-hypothesis correction återstår
- Beroenden: BB-047, BB-048.
- Syfte/scope: prevent research selection from being mistaken for durable expectancy by
  governing split design, repeated testing, parameter/regime sensitivity and sequence risk.
- Risk: overfitting, survivorship/look-ahead/data leakage, selection and multiple-testing
  bias, regime change and underestimated costs/slippage.
- Definition of Done: look-ahead/data leakage guards, in/validation/out-of-sample splits,
  sensitivity, regime/cost stress and useful walk-forward/sequence-risk tests are evidenced.
- Verification: seeded negative controls and synthetic biased datasets must be rejected;
  reports disclose trial population, dataset/version, costs and untouched holdout scope.

### BB-050 – M4 versioned deterministic strategy contract and candidates

- Modul: Finance / Strategy Engine
- Typ: Implementation / research
- Prioritet: P2
- Status: Planerad
- Beroenden: BB-047.
- Definition of Done: each momentum, trend, breakout, mean-reversion and volume/liquidity
  candidate is independently testable and produces versioned StrategySignal evidence.

### BB-051 – Market-regime classification

- Modul: Finance / Strategy Engine
- Typ: Research / implementation
- Prioritet: P2
- Status: Planerad
- Beroenden: BB-045, BB-050.
- Definition of Done: versioned regimes and uncertainty can enable, disable or weight
  strategies; abnormal volatility or poor liquidity can result in no trade.

### BB-052 – Multi-strategy candidate decision model

- Modul: Finance / Strategy Engine
- Typ: Implementation
- Prioritet: P2
- Status: Planerad
- Beroenden: BB-050, BB-051.
- Definition of Done: multiple versioned signals create an explainable Candidate Trade;
  tests prove agreement is evidence and cannot authorize execution.

### BB-053 – M5 hard Risk Engine and policy evaluation

- Modul: Finance / Risk
- Typ: Critical safety implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-044, BB-047; ADR 0018 review.
- Definition of Done: all documented capital, exposure, loss, liquidity, spread, hours,
  data/volatility and health limits are server-enforced and bypass/invariant tested.

### BB-054 – Portfolio engine, sizing and bidirectional compounding

- Modul: Finance / Portfolio
- Typ: Implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-048, BB-053.
- Definition of Done: cash, positions, exposure and current-equity sizing reconcile;
  percentage risk never rises through compounding and sizes shrink after losses.

### BB-055 – Exit management, circuit breakers and emergency control

- Modul: Finance / Risk
- Typ: Critical safety implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-053, BB-054.
- Definition of Done: position-opening exit invariant, forced exits, STOP ALL TRADING,
  HALTED behavior, safe-exit policy and resume authorization pass failure drills.

### BB-056 – M6 Strategy Lab, metrics and lifecycle governance

- Modul: Finance / Strategy Lab
- Typ: Implementation / governance
- Prioritet: P2
- Status: Planerad
- Beroenden: BB-049–BB-055.
- Syfte/scope: compare immutable strategy/model/parameter versions and govern transitions
  through EXPERIMENTAL, BACKTESTED, PAPER, APPROVED, ACTIVE, SUSPENDED and RETIRED.
- Risk: recent-performance promotion, incompatible evidence comparison and hidden data
  mining can grant unjustified authority.
- Definition of Done: cost-aware metrics and attribution compare immutable versions;
  lifecycle promotion needs evidence and explicit approval, not recent performance.
- Verification: tests prove new evidence cannot mutate/promote an active version and that
  every transition checks current evidence, Risk policy and owner authorization.

### BB-057 – M7 restart-safe paper trading engine

- Modul: Finance / Paper Trading
- Typ: Implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-045, BB-050, BB-053, BB-056.
- Definition of Done: persistent simulated cash/positions/orders/fills/P&L/costs support
  delay, partial/rejected orders, restart and daily strategy-attributed summaries.

### BB-058 – Paper-trading soak and evidence acceptance

- Modul: Finance / Validation
- Typ: Long-duration validation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-057, BB-070.
- Definition of Done: representative owner-accepted evidence covers regimes, costs,
  drawdown, operations and failures; success is not defined by profitable-day count.

### BB-059 – M8 Finance dashboard and accessible PAPER/LIVE UI

- Modul: Finance / Web
- Typ: UX / implementation
- Prioritet: P2
- Status: Planerad
- Beroenden: BB-053, BB-056, BB-057.
- Definition of Done: portfolio, P&L, mode/risk, positions/orders, signals, journal,
  warnings and emergency state are responsive/accessibility tested; PAPER/LIVE is unmistakable.

### BB-060 – Swedish/EU legal, tax and operational live-trading research

- Modul: Finance / Governance
- Typ: Research / live gate
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-043.
- Definition of Done: qualified sources and owner review cover broker terms, automated-
  trading restrictions, account/tax/reporting, market-data/API and instrument constraints.

### BB-061 – M9 broker evaluation and owner-approved selection

- Modul: Finance / Broker
- Typ: Research / architecture
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-046, BB-060.
- Definition of Done: candidates are compared on Sweden/API/sandbox/instruments/costs,
  data/orders/fractions/limits/security/reliability/terms; no convenience-only selection.

### BB-062 – Broker abstraction and credential-security boundary

- Modul: Finance / Broker
- Typ: Security / implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-061; ADR 0017 review.
- Definition of Done: typed adapter passes fake/sandbox contracts; paper/live secrets use
  least-privilege injection, rotation/revocation and never reach Git, Web, AI or logs.

### BB-063 – Execution verification, idempotency and reconciliation

- Modul: Finance / Execution
- Typ: Critical safety implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-055, BB-062; ADR 0019 review.
- Definition of Done: immutable previews and idempotency prevent duplicates; broker truth
  resolves uncertain/partial/rejected results and material mismatch suspends automation.

### BB-064 – M10 Trading Controller and manual approval mode

- Modul: Finance / Trading Controller
- Typ: Sensitive implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-055, BB-059, BB-060–BB-063, BB-070; explicit owner approval.
- Definition of Done: exact current previews show evidence/risk/cost/size/exit/impact and
  only explicit bound approval can execute once, followed by independent verification.

### BB-065 – M11 limited live automation

- Modul: Finance / Trading
- Typ: High-risk gated implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-058, BB-063, BB-064; explicit owner promotion.
- Definition of Done: tiny exposure/universe/loss limits, mandatory exits, anomaly stop,
  notification and daily reconciliation are verified with accepted bounded evidence.

### BB-066 – M12 long-duration limited-live validation

- Modul: Finance / Validation
- Typ: Long-duration validation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-065.
- Definition of Done: representative net expectancy, drawdown, regime, drift, incident and
  reconciliation evidence is accepted; elapsed time alone is insufficient.

### BB-067 – M13 policy-governed AUTO

- Modul: Finance / Trading
- Typ: High-risk gated implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-066, no critical defects, current BB-060 gate; explicit owner approval.
- Definition of Done: policy-bounded autonomous entry/management/exit and suspension pass
  independent invariant, circuit-breaker, reconciliation and emergency review.

### BB-068 – M14 BigBrain Autonomic Finance capabilities

- Modul: Finance / BigBrain.Brain
- Typ: Future integration
- Prioritet: P2
- Status: Planerad
- Beroenden: stable BB-063/BB-067 capabilities and security review.
- Definition of Done: OBSERVE–DIAGNOSE–DECIDE–POLICY–ACT–VERIFY uses the normal Trading
  Controller; adversarial tests prove AI cannot access credentials or bypass policy.

### BB-069 – M15 continuous strategy governance and drift control

- Modul: Finance / Strategy Governance
- Typ: Operations / governance
- Prioritet: P2
- Status: Planerad
- Beroenden: BB-056 and any active trading mode.
- Syfte/scope: collect outcomes, detect drift and propose reversible version lifecycle
  actions without self-modifying live strategy behavior.
- Risk: feedback loops, regime drift, contaminated labels and automatic retraining or
  promotion based on recent wins.
- Definition of Done: recurring evidence, drift alerts and explicit suspend/retire/promote
  controls keep every active strategy version owned, current and reversible.
- Verification: soak/failure tests prove collection only emits evidence/proposals; live
  changes require the full review/promotion gate and explicit owner action.

### BB-070 – Decision journal, observability and failure injection

- Modul: Finance / Audit and Operations
- Typ: Cross-cutting safety implementation
- Prioritet: P1
- Status: Planerad
- Syfte/scope: preserve the queryable market→signal→risk/policy→decision→execution→outcome
  graph, including NO TRADE, REJECTED, costs, horizons and post-trade evaluation.
- Beroenden: BB-044 and the provider-neutral evidence schema may evolve now; persisted
  market references depend on BB-045, risk evidence on BB-053, and execution/outcomes
  evolve with M7–M13.
- Risk: winner-only evidence, broken correlation, secret/raw-data leakage, tampering and
  journal retention that conflicts with source entitlement.
- Definition of Done: append-oriented decisions reconstruct buy/sell reasons; safe health,
  P&L/risk/execution/reconciliation metrics and outage/restart/corruption tests exist
  without credentials or unnecessary account details.
- Verification: invariant/restart/corruption tests reconstruct complete accepted,
  rejected and no-trade chains and enforce source-policy/redaction rules.

---

## Buggar

### BB-037 – Sprint 1-deployment tappade runtimekonfiguration och kalenderkoppling

- Modul: Deployment / Media / Kalender
- Typ: P0-regression / konfiguration / persistence
- Prioritet: P0
- Status: Klar
- Upptäckt: 2026-08-07

En deployment från en ren käll-export kördes utan repositoryts runtimekonfiguration och från en commit som ännu saknade den deployade kalendermodulen. API-containern fick tomma integrationsvärden och kalenderns named volume monterades inte. Volymen raderades aldrig; databasen verifierades med `integrity=ok`, 39 händelser och 2 importer före återanslutning.

#### Definition of Done

- Deployment använder avsedd runtimekonfiguration utan att secrets publiceras.
- Kalender-, mat-, shopping- och settingsvolymer är explicit monterade och verifierade.
- Kalenderdata och importhistorik kan läsas efter API-omstart.
- Integrationerna klassas åter som konfigurerade.
- API/Web är healthy och externa tjänster behåller identitet och data.
- Produktägaren har manuellt verifierat kalender och integrationer.

Slutstatus 2026-08-07: konfiguration och kalendervolym är återanslutna; kalender-API visar befintliga importer/händelser och integrationskonfigurationen är åter laddad. Produktägaren har manuellt verifierat och godkänt den återställda installationen.

### BB-038 – Delat persistent tema mellan mobil och webb

- Modul: Settings / BigBrain Web
- Typ: P0-regression / UX / persistence
- Prioritet: P0
- Status: Klar
- Upptäckt: 2026-08-07

Temat lagrades endast per webbläsare i `localStorage`; ThemeProvider, SettingsService och Theme API saknades. Mobil och desktop kunde därför ha olika teman.

#### Definition of Done

- Ett versionssatt Theme API läser och skriver ett allowlistat tema.
- ThemeProvider använder serverns persistenta värde som auktoritativ källa.
- Befintligt lokalt tema kan säkert seeda en tidigare okonfigurerad serverinställning.
- Ogiltiga teman avvisas med Problem Details och lokal UI-state återställs vid skrivfel.
- Inställningen överlever API-omstart och delas mellan klienter.
- Frontend-/backendtester, build och produktägarens mobil-/desktopverifiering är godkända.

Slutstatus 2026-08-07: API, dedikerad settingsvolym och ThemeProvider är implementerade, testade, deployade och manuellt godkända av produktägaren på mobil och webb.

### BB-028 – Kalender – Heroma-schemaimport och veckovis arbetsöversikt

- Modul: Kalender
- Typ: Funktion / extern filimport
- Prioritet: P1
- Status: Klar
- Upptäckt: 2026-08-05

#### Definition of Done

- Flera verifierade Heroma-filer kan förhandsgranskas och importeras med partiell framgång.
- Faktiskt `.xlsx`-format, datum, tider, dag/kväll, utbildning, samverkan, semester, okända typer och flera poster per datum parsas säkert.
- Kalenderdata och importhistorik persisterar server-side.
- Hem visar aktuell vecka; förstorat läge visar hela månaden med tider i varje relevant dag och läsbar mobilpresentation.
- Exakt dubblett blockeras; omimport erbjuder Replace, Merge och Cancel; konflikter skrivs inte över.
- Backend/frontend/build/dokumentations- och säkerhetsgrindar är gröna.
- Endast API och Web deployas och är healthy.
- Produktägaren verifierar privat flerfilsimport, reload, dubblett, omimport och mobilvy.
- Sanerad rapport, dokumentation, avsedda commits och push är publicerade; HEAD matchar `origin/main` och CI är grön eller tydligt pågående.

Slutstatus: backend, import, persistence, veckovy och mobil layout är deployade. Produktägaren har verifierat korrekt arbetsvecka, bevarad importerad schemadata och återställd kalender efter remediationen. Automatiska tester, dokumentation, commit och push är publicerade.

### BB-029 – Kalender – externa synkar och personliga kalenderfunktioner

- Modul: Kalender
- Typ: Framtida funktion
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-05

Omfattar framtida Google/Apple/ICS-synk, familje- och manuella händelser, påminnelser, per-user kalender, pushnotiser, AI-planeringsförslag och arbetstidsstatistik. Funktionerna kräver separata ägarskaps-, behörighets-, konflikt- och integritetsbeslut och ingår inte i Heroma-MVP:n.

#### Definition of Done

- En prioriterad del har verifierat användarbehov och separat arkitekturbeslut.
- Ägarskap, per-user-scope, sync-konflikter, auktorisering och retention är dokumenterade och testade.

### BB-030 – Dashboardinställningar under kugghjulsmeny

- Modul: BigBrain Web / Dashboard
- Typ: UX / navigering och inställningar
- Prioritet: P2
- Status: Klar
- Upptäckt: 2026-08-05

#### Beskrivning

Kontrollerna Tema, Redigera och Widgetbibliotek visas permanent högst upp i varje dashboardvy. De tar mycket vertikalt utrymme på mobil och är sekundära inställningsfunktioner snarare än primärt innehåll.

#### Önskat beteende

- Visa en tydlig kugghjulsknapp för dashboardinställningar.
- Tema, Redigera och Widgetbibliotek öppnas därifrån.
- Menyn gäller aktuell dashboardvy.
- Aktivt redigeringsläge framgår fortfarande tydligt.
- Tema förblir globalt om det är det nuvarande kontraktet.
- Funktionerna försvinner inte och blir inte svårare att nå.
- Menyn fungerar med touch, tangentbord, Escape, fokusfälla och fokusåterställning.
- Lösningen fungerar i alla teman och på mobil och desktop.

#### Avgränsning

Ingen ändring av widgetpersistens, moduldata, dashboardprofiler eller backend.

#### Definition of Done

- Tema, Redigera och Widgetbibliotek ligger bakom en tydlig kugghjulskontroll.
- Dashboardens primära innehåll börjar högre upp på mobil.
- Alla tre funktionerna är fullt åtkomliga.
- Menyn har korrekt tillgänglighetssemantik.
- Aktivt redigeringsläge är tydligt.
- Verifierat i Ljust, Mörkt och Obsidian Gold.
- Regressionstester och manuell mobilverifiering finns.

#### Manuell evidens

På iPhone i Obsidian Gold tar Tema-väljaren samt knapparna Redigera och Widgetbibliotek en stor del av den övre dashboardytan.

#### Lösning och verifiering

Tema, redigeringsläge och widgetbibliotek samlas i en tillgänglig inställningspanel bakom ett kugghjul. Escape och klick utanför stänger panelen med fokusåterställning. Implementationen verifierades 2026-08-07 med komponenttester, production build och headless Chromium vid 390×844, 768×1024 och 1440×1000 i samtliga tre teman. Fixen är deployad och manuellt godkänd av produktägaren på mobil och desktop.

### BB-031 – Download Control orsakar horisontell overflow på mobil

- Modul: Media / Download Control
- Typ: Bugg / mobil layout / overflow
- Prioritet: P1
- Status: Klar
- Upptäckt: 2026-08-05

#### Beskrivning

När widgeten Nedladdningar är öppen på mobil blir delar av Download Control bredare än widgeten och viewporten. Informationsrutor, filter, åtgärdsknappar och långa torrentnamn kan fortsätta utanför högerkanten.

#### Nuvarande beteende

- Uppdateringsknappen kapas eller hamnar delvis utanför.
- Filterraden fortsätter utanför widgetens bredd.
- Torrentkort och Hantera-knappar kan bli bredare än tillgängligt utrymme.
- Långa namn pressar layouten horisontellt.
- Innehåll döljs bakom viewportens högra kant.

#### Förväntat beteende

- Ingen sida- eller widgetövergripande horisontell scroll.
- Allt innehåll håller sig inom widgetens inre bredd.
- Långa torrentnamn radbryts eller trunkeras kontrollerat.
- Filter får radbrytas eller ligga i en avsiktlig intern scrollrad utan att hela sidan expanderar.
- Knappar anpassas till mobilbredd.
- Progressindikator och metadata håller sig inom kortet.
- Desktoplayouten försämras inte.

#### Tekniska kontrollpunkter

- `min-width: 0` på grid- och flexbarn.
- `overflow-wrap` och `word-break` för långa release-namn.
- Fasta bredder och `width: max-content`.
- `flex-wrap` för header och filter.
- `max-width: 100%`.
- `box-sizing`.
- Eventuell `overflow-x` på fel container.

#### Definition of Done

- Ingen horisontell dokument-scroll vid 320, 375 och 390 px.
- Header, filter, torrentkort och knappar ryms.
- Mycket långa torrentnamn förstör inte layouten.
- Download Control är användbart i alla teman.
- Regressionstest använder representativt långt torrentnamn.
- Manuell verifiering genomförs på iPhone.

#### Manuell evidens

På Media-vyn i mobilformat går uppdateringsknappen, filterraden och torrentinformationen utanför widgetens högra kant.

#### Lösning och verifiering

Grundorsaken var intrinsic sizing i gridbarn, header, åtgärdsknapp och långa texter. Berörda containers har nu explicita `min-width: 0`/`max-width: 100%`, radbrytning och enkolumnslayout på mobil utan att dölja informationsinnehåll. Implementationen verifierades 2026-08-07 med komponent-/CSS-regressionstest, production build och headless Chromium vid 390×844, 768×1024 och 1440×1000 utan dokument- eller widgetoverflow. Fixen är deployad och manuellt godkänd av produktägaren på mobil.

### BB-032 – Kalenderns Heroma-importdialog orsakar horisontell scroll

- Modul: Kalender / Heroma-import
- Typ: Bugg / mobil modal / overflow
- Prioritet: P1
- Status: Klar
- Upptäckt: 2026-08-05

#### Beskrivning

När dialogen Importera Heroma-schema öppnas på mobil visas en horisontell scrollbar längst ned. Dialogens innehåll är bredare än den visuella viewporten.

#### Nuvarande beteende

- Importdialogen kan scrollas horisontellt.
- Delar av dialogen sträcker sig utanför skärmen.
- Filväljaren, knappen Förhandsgranska eller dialogens inre panel misstänks skapa en för stor minsta bredd.
- Problemet är särskilt tydligt i mörkt tema på iPhone.

#### Förväntat beteende

- Dialogen är aldrig bredare än mobilens visuella viewport.
- Endast vertikal scroll används när innehållet är långt.
- Filväljare, statusrad, förhandsgranskningsknapp och stängknapp ryms inom dialogen.
- Långa filnamn radbryts eller trunkeras säkert.
- Safe-area-insets respekteras.
- Dialogen fungerar även med flera valda filer och långa filnamn.

#### Tekniska kontrollpunkter

- `width` och `max-width` med hänsyn till viewport och safe area.
- `min-width: 0` på dialogens barn.
- Native `input[type=file]`.
- `box-sizing`.
- Padding plus border.
- `100vw` kontra `100dvw`.
- Långa filnamn och flex-/gridbarn.
- `overflow-x` på dialog, overlay och body.

#### Definition of Done

- Ingen horisontell scrollbar vid 320, 375 och 390 px.
- Dialogen ryms inom viewporten i alla teman.
- Flera filer och långa filnamn förstör inte layouten.
- Endast avsedd vertikal dialogscroll används.
- Bakgrundssidan förblir låst.
- Escape, fokusfälla och fokusåterställning fungerar fortsatt.
- Regressionstest och manuell iPhone-verifiering finns.

#### Manuell evidens

På iPhone visas en tydlig horisontell scrollbar längst ned i dialogen Importera Heroma-schema.

#### Lösning och verifiering

Kalenderdialogen använder ett vertikalt mobilflöde, breddbegränsade barn, säker radbrytning och safe-area-kompensation. Regressionstest och product-owner-verifiering på mobil godkändes 2026-08-07.

### BB-001 – Ingen synlig återkoppling när en dubblettvara stoppas

- Modul: Inköpslista
- Typ: UX / felhantering
- Prioritet: P2
- Status: Bekräftad
- Upptäckt: 2026-08-02

#### Beskrivning

När användaren försöker lägga till en vara som redan finns i inköpslistan skapas ingen dubblett, vilket är korrekt. Däremot visas inget synligt meddelande eller någon dubblettdialog för användaren.

#### Nuvarande beteende

- API eller datalager stoppar dubbletten.
- Ingen extra vara läggs till.
- Användaren får ingen tydlig förklaring.

#### Förväntat beteende

Användaren ska få en tydlig återkoppling, exempelvis:

- `Korv finns redan på listan.`
- möjlighet att öka antal;
- möjlighet att visa den befintliga varan;
- möjlighet att avbryta och fortsätta skriva.

#### Risk

Användaren kan tro att knapptryckningen inte fungerade och försöka flera gånger.

#### Avgränsning

Ingen ändring av dubblettregeln eller datamodellen krävs. Felsök frontendens hantering av API-svaret och dubblettdialogens rendering.

#### Definition of Done

- Ett stoppat dubblettförsök ger synlig återkoppling.
- Ingen dubblett skapas.
- Fokus återgår till ett logiskt ställe.
- Dialogen visas ovanför handlingsläget.
- Fungerar på mobil och desktop.
- Regressionstest finns.

---

### BB-002 – Bakgrundssidan kan scrollas bakom handlingsläget

- Modul: Inköpslista
- Typ: Mobil UX / modalhantering
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-02

#### Beskrivning

När Inköpslistas fullskärmsläge är öppet går det ibland att scrolla innehållet bakom fullskärmsytan.

#### Nuvarande beteende

- Bakgrundsinnehållet kan ibland förflyttas.
- Felet verkar vara intermittent.
- Det är ännu inte fastställt vilka steg som alltid reproducerar det.

#### Förväntat beteende

När handlingsläget är öppet ska endast inköpslistans interna innehåll kunna scrollas. Sidan bakom ska vara helt låst.

#### Misstänkta utlösare

- mobilens tangentbord öppnas eller stängs;
- radmeny öppnas;
- dubblettdialog öppnas;
- handlingsläget öppnas och stängs flera gånger;
- iOS ändrar höjden på den visuella viewporten.

#### Definition of Done

- Bakgrundsscroll är låst under hela handlingsläget.
- Ursprunglig scrollposition återställs när läget stängs.
- Ingen hoppande sida vid öppning eller stängning.
- Verifierat i riktig Chromium och manuellt på iPhone.
- Regressionstest eller reproducerbar browserkontroll finns.

---

### BB-003 – Scrollindikator visas trots att listan verkar rymmas

- Modul: Inköpslista
- Typ: Mobil layout / overflow
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-02

#### Beskrivning

I handlingsläget kan en scrollbar eller scrollindikator ibland visas på höger sida trots att listans innehåll inte ser ut att kräva scrollning.

#### Nuvarande beteende

- Scrollindikatorn visas intermittent.
- Det är oklart om en liten dold overflow faktiskt finns.
- Felet kan vara kopplat till viewporthöjd, tangentbord eller fokus.

#### Förväntat beteende

Ingen intern scrollindikator ska visas när hela innehållet ryms i viewporten.

#### Tekniska kontrollpunkter

- `100vh` kontra `100dvh`;
- marginaler eller padding som skapar några pixels overflow;
- fokus- eller keyboardförändringar;
- dubbla scrollcontainers;
- `min-height`, footer och safe-area-insets;
- portalerade menyer eller dialoger som påverkar dokumenthöjden.

#### Definition of Done

- En kort lista ger ingen onödig scrollbar.
- En lång lista scrollar endast i avsedd intern container.
- Samma beteende vid 320×844 och 390×844.
- Tangentbord, radmeny och dialog orsakar inte falsk overflow.

---

### BB-016 – Ofta köpt visar varor som redan finns i inköpslistan

- Modul: Inköpslista
- Typ: UX / filtrering
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-04

#### Beskrivning

Sektionen ”Ofta köpt” visar varor som redan finns i den aktiva inköpslistan. När en vara läggs till från sektionen kan förslaget dessutom ligga kvar trots att det inte längre är relevant.

#### Nuvarande beteende

- En vara kan visas samtidigt under ”Ofta köpt” och ”Att köpa”.
- Ett förslag kan ligga kvar efter att användaren har lagt till varan.
- Användaren kan få intrycket att samma vara fortfarande behöver läggas till.
- Gränssnittet använder utrymme till förslag som inte längre kan hjälpa användaren.

#### Förväntat beteende

- Varor som redan finns i inköpslistan filtreras bort från ”Ofta köpt”.
- En vara som läggs till från ”Ofta köpt” försvinner direkt efter lyckat tillägg.
- Tillägg genom vanlig textinmatning uppdaterar också förslagen.
- En borttagen vara kan återkomma när den fortfarande kvalificerar sig som ofta köpt.
- Filtreringen använder samma namnnormalisering som dubblettkontrollen.
- Sektionen visas inte som en tom eller irrelevant förslagsyta när inga förslag återstår.

#### Risk

Dubbel information skapar visuell oreda och kan leda till förvirring eller upprepade försök att lägga till en vara som redan finns i listan.

#### Avgränsning

Denna backlogregistrering bestämmer inte om filtreringen slutligen ska ske i frontend, backend eller båda. Vid implementation ska befintlig datamodell, dubblettregel och källa för ”Ofta köpt” först inspekteras. Ingen ändring av statistik eller historik för ofta köpta varor efterfrågas.

#### Definition of Done

- Ingen vara visas samtidigt i ”Ofta köpt” och den aktiva inköpslistan.
- Ett förslag försvinner direkt efter ett lyckat tillägg från ”Ofta köpt”.
- Ett lyckat tillägg genom vanlig textinmatning uppdaterar också förslagen.
- En borttagen vara kan återkomma om den fortfarande kvalificerar sig.
- Versaler, gemener och omgivande blanksteg hanteras konsekvent med dubblettkontrollen.
- Ingen felaktig dubblett skapas.
- Sektionen hanterar noll återstående förslag enligt befintlig UI-standard.
- Beteendet fungerar på mobil och desktop.
- Regressionstester finns för relevant filtreringslogik och användarflöde.

#### Manuell evidens

På mobilvyn observerades att ”Mjölk” visades både under ”Ofta köpt” och i den aktiva listan ”Att köpa”.

---

### BB-017 – Smart Shuffle kan inte starta uppspelning på verifierad Samsung Tizen-TV

- Modul: Media / Smart Shuffle
- Typ: Produktionsbugg / Jellyfin remote playback
- Prioritet: P1
- Status: Klar
- Upptäckt: 2026-08-04

#### Beskrivning

Smart Shuffle kan läsa Jellyfin-biblioteket, visa serier och identifiera en verifierad och fjärrstyrbar Samsung Smart TV via Jellyfin for Tizen. När användaren trycker ”Starta på TV” startas ingen uppspelning och BigBrain visar det sanerade felet ”Jellyfin kunde inte utföra Smart Shuffle-åtgärden.”

#### Reproduktionssteg

1. Öppna Jellyfin-appen på Samsung-TV:n.
2. Kontrollera att vanlig manuell uppspelning fungerar.
3. Öppna BigBrain → Media → Smart Shuffle.
4. Välj minst två serier.
5. Välj Samsung Smart TV.
6. Tryck ”Starta på TV”.
7. Observera att ingen uppspelning startar och att BigBrain visar standardfelet.

#### Förväntat beteende

- BigBrain revaliderar vald TV-session.
- BigBrain väljer korrekt nästa osedda eller påbörjade episod.
- BigBrain skickar ett versionskorrekt PlayNow-kommando.
- Rätt avsnitt startar på exakt vald TV.
- BigBrain verifierar att samma avsnitt blir NowPlayingItem.
- Smart Shuffle-sessionen övergår till aktiv status.

#### Verifierad grundorsak

Det tidigare UI-styrda försöket nådde aldrig PlayNow. En sekventiell episodfråga timeoutade mot Jellyfin efter den generella tresekundersgränsen och `TaskCanceledException` lämnade Smart Shuffle-felmodellen som ett ohanterat 500-svar.

#### Fix och verifiering

Episodkontrollen körs parallellt med en avgränsad Smart Shuffle-timeout. TV-session, användare och fjärrstyrbarhet revalideras precis före ett versionskorrekt PlayNow-anrop. Accepterad uppspelning skiljs från inväntad och bekräftad uppspelning, med en begränsad verifieringsperiod anpassad för Tizen. Säkra felkategorier, startspärr mot dubbelklick och regressionstester har lagts till.

Ett verkligt UI-styrt knapptryck på den slutliga versionen gav exakt ett startkommando, Jellyfin svarade `204`, rätt avsnitt blev `NowPlayingItem` och BigBrain-sessionen blev `active`. Användaren bekräftade att allt fungerade på Samsung-TV:n. Även användarstyrda hopp till nästa avsnitt accepterades och bekräftades utan terminalbaserad uppspelning.

Testbevis: 186 API-tester och 32 Sentinel-tester godkända; 76 frontendtester och production build godkända. Permanent rapport: `/home/enigma/BigBrain/reports/features/smart-shuffle/smart-shuffle-p1-playback-start-fix-20260804-153939.txt`.

#### Definition of Done

- Exakt grundorsak identifierad.
- Jellyfin 10.11.11-kontraktet verifierat.
- Session, användare och episod revalideras precis före start.
- Rätt session-ID och item-ID används internt.
- Query-parametrar och requestformat är versionskorrekta.
- Upstream-status och säker felkategori loggas utan hemligheter.
- Användarens UI-klick startar rätt avsnitt på Samsung-TV:n.
- NowPlayingItem verifieras efter start.
- Dubbelklick kan inte orsaka dubbla starter.
- Automatiska tester täcker grundorsaken och relevanta fel.
- Permanent verifieringsrapport skapad.
- Buggen markeras som löst först efter verklig UI-styrd TV-verifiering.

---

### BB-018 – Smart Shuffle – Jellyfins ”Nästa avsnitt” visas missvisande mellan serier

- Modul: Media / Smart Shuffle
- Typ: UX / Jellyfin-klientintegration
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-04

#### Beskrivning

När ett avsnitt avslutas under Smart Shuffle visar Jellyfin for Tizen den vanliga ”Nästa avsnitt”-rutan för nästa avsnitt i samma serie. Smart Shuffle väljer däremot nästa serie enligt sin rättvisa shufflealgoritm. Funktionen fortsätter att fungera, men Jellyfins förslag blir missvisande och visuellt störande.

#### Förväntat beteende

Smart Shuffle och Jellyfins klientgränssnitt ska ge en begriplig och konsekvent övergång mellan serier. Jellyfins normala nästa-avsnitt-funktion ska fortsatt fungera vid vanlig Jellyfin-uppspelning utanför Smart Shuffle.

#### Utredningspunkter

- Verifiera om Jellyfin 10.11.11 eller Jellyfin for Tizen dokumenterar ett sessionsavgränsat sätt att stänga av eller undvika klientens vanliga ”Nästa avsnitt”-ruta.
- Utred om ett annat dokumenterat playbackflöde kan göra Smart Shuffle-övergången tydligare utan att störa vanlig Jellyfin-användning.
- Om klientbeteendet inte säkert kan påverkas, utred dokumentation av begränsningen eller en tydlig förklaring i Smart Shuffle-gränssnittet.
- Inför inte odokumenterade Jellyfin-endpoints eller klienthack.

#### Avgränsning

Ingen global Jellyfin-inställning får försämra eller stänga av den normala nästa-avsnitt-funktionen utanför Smart Shuffle. Implementationen ska föregås av verifiering av installerad serverversion, Tizen-klientens beteende och dokumenterade kontrakt. Ingen kod-, runtime-, Compose-, Jellyfin- eller Tizen-konfigurationsändring ingår i denna backlogregistrering.

#### Definition of Done

- Installerad Jellyfin-version och Jellyfin for Tizen-beteendet verifieras.
- Det dokumenterade Jellyfin-kontraktet för completion, autoplay, PlayNext och nästa-avsnitt-UI undersöks.
- Det fastställs om klientens ruta kan påverkas per Smart Shuffle-session.
- Ingen global Jellyfin-inställning ändras utan separat uttryckligt beslut.
- Vanlig manuell Jellyfin-uppspelning påverkas inte.
- Smart Shuffles automatiska seriebyte fortsätter att fungera.
- Ingen odokumenterad eller versionsosäker endpoint används.
- Automatisk övergång, skip och stop regressionstestas.
- Lösningen verifieras manuellt på Samsung Smart TV.
- Relevant dokumentation och permanent verifieringsrapport uppdateras.

---

### BB-019 – BigBrain saknar säker borttagning av oönskad nedladdning

- Modul: Media / Download Control
- Typ: Funktion / säker extern mutation
- Prioritet: P2
- Status: Pågår
- Upptäckt: 2026-08-04

#### Beskrivning

BigBrain saknade ett objektspecifikt och bekräftat sätt att avbryta ett oönskat qBittorrent-jobb utan att exponera rå torrentidentitet eller riskera andra jobb och media.

#### Implementerad MVP

Listning, opaka kortlivade ID:n, live-revalidering, filbevarande standardborttagning, separat riskgrindad destruktiv borttagning, Arr-varning, säkra fel och automatiska fake-baserade tester är implementerade och deployade. Användaren har genom BigBrains UI bekräftat att minst ett fastnat jobb togs bort från qBittorrent med filerna bevarade. Full manuell verifiering av `deleteFiles=true`, samtliga destruktiva riskscenarier och konsekvenser för importerad media/Arr återstår; status förblir därför Pågår.

#### Manuell status och kvarvarande verifiering

- Filbevarande borttagning genom BigBrain UI: verifierad av användaren.
- Borttagning från qBittorrent: verifierad av användaren.
- Destruktiv dataradering: inte fullständigt produktionsverifierad.
- Retry, pausa/återuppta, Arr Recovery, diagnostik, masshantering och retention: separata backlogposter BB-020–BB-026.

#### Definition of Done

- Exakt ett liveverifierat jobb påverkas per request.
- Normal borttagning använder `deleteFiles=false` och bevarar data.
- Destruktiv borttagning är separat, explicit och blockeras vid osäker risk.
- Rå hash, credentials, paths och upstreamfel exponeras inte.
- Sonarr/Radarr-ägarskap varnas för utan Arr-mutation.
- Backend-/frontendtester och production build är gröna.
- Verklig UI-styrd testborttagning verifieras på uttryckligt testjobb.
- Permanent rapport publiceras och indexeras.

---

### BB-020 – Download Control – säker masshantering

- Modul: Media / Download Control
- Typ: Funktion / UX / säker extern mutation
- Prioritet: P1
- Status: Pågår
- Upptäckt: 2026-08-04

#### Bakgrund och verifierat nuläge

Download Control listar qBittorrents aktuella torrentjobb men kan endast öppna och
hantera ett jobb åt gången. Det blir ineffektivt när flera jobb har fel, har fastnat,
är pausade, behöver startas om eller tas bort, eller är färdiga och kan rensas.

Detta är den befintliga backlogposten för samma behov; den konkretiserades och höjdes
från P3 till P1 den 2026-08-07 efter verifierat användarbehov. Ingen ny dubblettpost
skapades. Objektspecifika Retry och Pausa/Återuppta i BB-023 respektive BB-024 är
förutsättningar eller relaterade capabilities. Koordinerad Sonarr/Radarr-recovery
förblir separat i BB-021; den ersatta dubbletten BB-025 ändrar inte denna gräns.
Säker rensning av avslutade jobb och retention definieras fortsatt i BB-022, och
diagnostik i BB-026.

Sprint 2-slutstatus 2026-08-10: urval, ”markera alla” i filtrerad vy, vald-räknare,
avmarkering och begränsad partiell batch för pause, resume och retry är implementerade,
automatiskt verifierade, deployade och manuellt godkända av produktägaren. Destruktiv
batch, rensning och retention ingår inte i den godkända delmängden; posten förblir
Pågår eftersom hela Definition of Done inte är uppfylld.

#### Önskad UX

- Checkbox per nedladdning.
- ”Markera alla” gäller endast den aktuella filtrerade vyn.
- Antalet markerade poster visas tydligt och alla kan avmarkeras med en handling.
- Ett batchåtgärdsfält visas eller aktiveras först när minst ett objekt har markerats.
- Utred Starta/återuppta markerade, Pausa markerade, Försök igen markerade fel,
  Ta bort markerade och Rensa markerade färdiga.
- Utred separata snabbåtgärder: Försök igen alla fel, Pausa alla aktiva,
  Återuppta alla pausade och Rensa färdiga.
- Urval, bekräftelse, resultat och fel ska vara begripliga och användbara på mobil
  och desktop samt med tangentbord och skärmläsare.

#### Säkerhets- och kontraktskrav

- En framtida implementation ska föregås av ett separat arkitekturbeslut och får
  varken använda qBittorrents `hashes=all` eller bli en generell proxy.
- Browsern skickar endast opaka BigBrain-identiteter. Torrenthashar, paths,
  credentials och råa upstreamsvar exponeras aldrig.
- Servern skapar ett explicit målmanifest och preview visar exakt vilka jobb och
  åtgärder som kommer att påverkas innan bekräftelse.
- Varje objekt och dess identitet revalideras server-side omedelbart före mutation;
  browser-supplied raw hashes eller ett implicit aktuellt filter är aldrig auktoritet.
- Varje objekt får en individuell riskbedömning. Ett osäkert objekt får inte utsättas
  för destruktiv mutation och får inte göra övriga objekts riskbedömning mindre strikt.
- Kontraktet ska uttryckligen besluta atomärt kontra partiellt resultat och redovisa
  success/failure per objekt, inklusive säkra Problem Details-fel.
- Åtgärder ska vara idempotenta där det är möjligt och skyddas mot dubbelklick,
  återanvänd bekräftelse och samtidiga submits.
- Destruktiv dataradering har en separat aktiv bekräftelse, tillämpar minst samma
  konservativa path-/importgrindar som objektspecifik borttagning och får aldrig
  påverka ett objekt vars datascope är osäkert.
- Arr-ägarskap är inte tillstånd för dold mutation. Koordinerad blocklist,
  köborttagning eller ny sökning följer BB-021:s separata preview- och
  bekräftelsekontrakt.

#### Definition of Done

- Val, ”Markera alla” för filtrerad vy, antal valda, avmarkering och villkorat
  batchåtgärdsfält är implementerade och tillgänglighetsverifierade.
- Beslutade batch- och snabbåtgärder har tydliga tillståndsregler och påverkar endast
  det visade, bekräftade målmanifestet.
- Separat arkitekturbeslut dokumenterar capability-gräns, risk per objekt,
  atomärt eller partiellt resultat, idempotens och concurrency.
- Ingen `all`-parameter, rå hash, path eller generell qBittorrent-proxy används.
- Preview, separat destruktiv bekräftelse, server-side revalidering, per-item-resultat,
  audit och säkra fel är automatiskt testade.
- Mobil, desktop, tangentbord och skärmläsarnamn/aria-labels är verifierade.
- Ofarlig manuell batchverifiering visar att exakt avsedda jobb påverkades och att
  övriga qBittorrent-, Sonarr-, Radarr- och medieobjekt var oförändrade.
- Relevant modul-, status-, ADR-, test- och runbookdokumentation är uppdaterad.

---

### BB-033 – Media – tydliggör skillnaden mellan Nedladdningar och Mediajobb

- Modul: Media / MediaDashboard
- Typ: UX / informationsarkitektur
- Prioritet: P2
- Status: Pågår
- Upptäckt: 2026-08-07

#### Bakgrund och verifierad ansvarsskillnad

Media-vyn visar både ”Nedladdningar” och den kollapsbara modulen ”Pågående” med
undertiteln ”Pågående aktivitet” (Media Jobs). Gränssnittet förklarar inte varför båda
finns, och Media Jobs kan dessutom sammanfatta flera aktiva poster som ”pågående
nedladdningar”.

- **Nedladdningar / Download Control** representerar qBittorrents aktuella torrentkö.
  Den läser den fulla kön via Download Control-kontraktet, normaliserar torrentstatus,
  hastighet, storlek, köposition, kategori och Arr-ägarskapsvarning och är den
  avsiktligt smala mutationsytan för objektspecifik, preview-/bekräftelsestyrd
  borttagning. Livscykeln är begränsad till torrentjobbet: aktiv, köad, pausad, fel,
  klar eller okänd.
- **Pågående / Media Jobs** är en separat read-only livscykelvy för ett medieönskemål
  genom flera system. Den aggregerar Sonarrs och Radarrs registrerade bibliotek och
  köer, en begränsad qBittorrent-lista och Jellyfins bibliotekskatalog. Poster
  normaliseras och kan grupperas från beställd/sökning/kö/nedladdning via
  stannad/fel och färdig nedladdning/import till tillgänglig i Jellyfin.
- Funktionerna överlappar därför avsiktligt under nedladdningsfasen: samma
  qBittorrentjobb kan synas i båda. De har inte samma ansvar. Download Control visar
  och hanterar torrentjobbet; Media Jobs förklarar mediets end-to-end-status och är
  inte en mutationsyta.

Det är främst presentationen och de nuvarande namnen, inte de verifierade
ansvarsgränserna, som är otydliga för en användare utan kunskap om qBittorrent,
Sonarr eller Radarr.

#### Önskat resultat

- Användaren förstår direkt att den ena modulen hanterar själva nedladdningskön och
  att den andra följer mediets väg till biblioteket.
- Utred bättre användarnamn, exempelvis ”Nedladdningskö” för Download Control och
  ”Biblioteksjobb” eller ”Bearbetning” för Media Jobs, utan att föregripa beslutet.
- Utred korta beskrivande undertitlar, särskiljande ikonografi och begripliga
  statusbeskrivningar utan krav på teknisk tjänstekunskap.
- Utred om modulerna bör visualisera en gemensam men sanningsenlig pipeline, exempelvis
  `Hittad → Nedladdning → Import/bearbetning → Klar i Jellyfin`, inklusive hur
  ”beställd”, ”söker”, fel, paus, kö och okänd status representeras.
- Behåll tekniska providerdetaljer som valfri fördjupning och förklara överlappningen
  när samma nedladdningsfas visas i båda modulerna.
- Ingen funktionalitet eller befintlig säkerhetsgräns får försvinna.

#### Definition of Done

- Den faktiska ansvarsskillnaden och datakällorna ovan återspeglas i namn,
  beskrivningar och statusetiketter.
- Slutanvändaren behöver inte känna till qBittorrent, Sonarr eller Radarr för att
  förstå vilken modul som visar livscykel respektive hanterar nedladdningskön.
- Namn, undertitlar, ikonografi och eventuell pipeline är konsekventa med verklig
  funktion och vilseleder inte om import- eller Jellyfin-status.
- Avsiktlig överlappning under nedladdningsfasen förklaras utan att dubblera eller
  ta bort funktionalitet.
- Mobil och desktop är visuellt och funktionellt verifierade.
- Tillgängliga namn, rubrikhierarki, aria-labels, statusmeddelanden och
  skärmläsarkontext är verifierade.
- Relevanta tester samt modul-, status- och UX-dokument uppdateras när lösningen
  implementeras.

#### Sprint 3 implementation 2026-08-10

Nedladdningskö beskriver nu vardagligt att den hanterar själva nedladdningen och
hänvisar till Medieflöde för vägen till biblioteket. Medieflöde beskriver sökning,
nedladdning, bearbetning och bibliotek samt varför samma titel kan synas i båda vyerna.
Rubriker, undertitlar och skärmläsarregioner är automatiskt verifierade utan nya
providerbegrepp i förklaringen. Web är deployad och Sprint 3 är stängd efter teknisk
acceptans utan blockerande fynd. Posten förblir Pågår tills den längre kvalitativa
mobil-/desktopverifieringen i BB-041 uppfyller hela Definition of Done.

---

### BB-021 – Download Control – koordinerad Sonarr/Radarr-recovery

- Modul: Media / Download Control
- Typ: Framtida funktion / Arr-orkestrering
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-04
- Beskrivning: Utred ett separat uttryckligt flöde för blocklist, köborttagning och ny sökning när Arr äger jobbet. MVP:n muterar endast qBittorrent.
- Definition of Done: Versionsverifierade Arr-kontrakt, separat preview/bekräftelse, idempotens, ingen dold sökning och end-to-end-test med ofarligt testjobb.

---

### BB-022 – Download Control – säker rensning och retention för avslutade jobb

- Modul: Media / Download Control
- Typ: Framtida funktion / retention
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-04
- Beskrivning: Definiera en separat policy för avslutade jobb, importerad media och eventuell datarensning. MVP:n blockerar destruktiv borttagning av färdiga/importosäkra jobb.
- Definition of Done: Beslutad retentionpolicy, verifierat import- och hårdlänkskontrakt, säkra undantag, audit, rollbackstrategi och manuell verifiering.

---

### BB-023 – Download Control – Försök igen (Retry)

- Modul: Media / Download Control
- Typ: Funktion
- Prioritet: P2
- Status: Pågår
- Upptäckt: 2026-08-04

#### Beskrivning

Användaren ska kunna försöka återuppliva en nedladdning som fastnat utan att behöva öppna qBittorrent.

Exempel på åtgärder:

- reannounce mot trackers;
- Force Resume om torrenten är pausad;
- uppdatera status efter utförd åtgärd;
- visa säkra felmeddelanden;
- aldrig exponera hash eller råa API-svar.

#### Definition of Done

- Exakt ett torrentjobb påverkas.
- Säkra felkoder används.
- Ingen påverkan på andra torrents.
- Fullständig backend- och frontendtestning finns.
- Dokumentationen är uppdaterad.

Verifieringsstatus 2026-08-10: implementation och automatiska backend-/frontendtester
är klara och capabilityn är deployad. Manuell verifiering väntar tills en naturligt
felande eller problematisk nedladdning finns. Avsaknaden av ett säkert realistiskt
testobjekt är inte en konstaterad defekt och blockerar inte Sprint 2-stängningen.

---

### BB-024 – Download Control – Pausa / Återuppta

- Modul: Media / Download Control
- Typ: Funktion
- Prioritet: P2
- Status: Klar
- Upptäckt: 2026-08-04

#### Beskrivning

BigBrain ska kunna pausa och återuppta en enskild nedladdning.

#### Definition of Done

- Pausa fungerar.
- Återuppta fungerar.
- Status uppdateras direkt.
- Ingen massoperation används.
- Endast ett jobb påverkas.
- Tester och dokumentation är uppdaterade.

Slutstatus 2026-08-10: implementerad, automatiskt verifierad, deployad och manuellt
godkänd av produktägaren, både objektspecifikt och inom Sprint 2:s säkra batchgräns.

---

### BB-025 – Download Control – Arr Recovery

- Modul: Media / Download Control
- Typ: Funktion
- Prioritet: P3
- Status: Avvisad
- Upptäckt: 2026-08-04

#### Beskrivning

Skapa ett separat återställningsflöde för nedladdningar som ägs av Sonarr eller Radarr.

Exempel på åtgärder:

- ta bort torrent;
- valfri blocklist;
- starta ny sökning;
- visa tydligt vad som kommer att hända innan något utförs.

Detta ska vara ett eget arbetsflöde och inte blandas ihop med vanlig borttagning.

Posten är en dubblett av BB-021 och ersätts av den mer preciserade posten där. Ingen
implementation eller historik har tagits bort.

#### Definition of Done

- Preview finns.
- Bekräftelse krävs.
- Säker rollback vid fel finns.
- End-to-end-test finns.
- Dokumentationen är uppdaterad.

---

### BB-026 – Download Control – Diagnostik (”Varför laddar den inte ner?”)

- Modul: Media / Download Control
- Typ: UX / funktion
- Prioritet: P2
- Status: Klar
- Upptäckt: 2026-08-04

#### Beskrivning

BigBrain ska analysera varför en nedladdning inte gör framsteg och ge användaren en begriplig förklaring i stället för enbart rå status.

Exempel på diagnoser:

- inga seeders;
- tracker svarar inte;
- torrent pausad;
- väntar på metadata;
- disk full;
- Sonarr/Radarr väntar;
- fel autentisering;
- timeout;
- nätverksproblem.

För varje diagnos ska BigBrain även föreslå en lämplig åtgärd.

#### Definition of Done

- Diagnoser visas med mänskligt språk.
- Felsökningen bygger på verifierad data.
- Ingen rå intern information exponeras.
- Tester finns.
- Dokumentationen är uppdaterad.

Slutstatus 2026-08-10: deterministisk, sanerad diagnostik är implementerad,
automatiskt verifierad, deployad och manuellt godkänd av produktägaren.

---

## Dokumentationsstyrning

### BB-004 – Arkivera ARR-incidentens mellanrapporter

- Modul: Dokumentation/Operations
- Typ: Dokumentationsskuld
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Arkivera mellanrapporter additivt med manifest och checksummor.
- Motiv: Skilja slutrapport och aktiv diagnostik från historisk evidens.
- Avgränsning: Ingen radering; flytt kräver separat review och godkännande.
- Risk: Brutna referenser eller förlorad spårbarhet.
- Definition of Done: Godkänt manifest, checksummor, uppdaterade index och verifierade länkar.
- Relaterade dokument: `docs/indexes/documentation.md`, ADR 0010, extern `arr-incident-index.txt`.

### BB-005 – Revidera README mot aktuell implementation

- Modul: Projektdokumentation
- Typ: Dokumentationsskuld
- Prioritet: P2
- Status: Klar
- Upptäckt: 2026-08-03
- Beskrivning: Uppdatera funktioner, läsordning och länkar efter faktagranskning.
- Motiv: README speglar inte hela verifierade implementationen.
- Avgränsning: Ingen arkitekturändring eller ny funktion.
- Risk: Felaktig onboarding.
- Definition of Done: Kort, kodverifierad README med auktoritativa länkar.
- Relaterade dokument: `README.md`, `docs/indexes/documentation.md`, `docs/history/early-sprints.md`.
- Slutförd: 2026-08-04; långlivad produktöversikt verifierad och relevant tidig historik bevarad separat.

### BB-006 – Dela upp och korta STATUS.md

- Modul: Projektdokumentation
- Typ: Informationsarkitektur
- Prioritet: P2
- Status: Klar
- Upptäckt: 2026-08-03
- Beskrivning: Begränsa STATUS till aktuellt läge och placera historisk verifiering efter review.
- Motiv: Filen blandar sprintlogg, runtimeevidens, problem och produktstatus.
- Avgränsning: Bevara historik; inga flyttar utan review.
- Risk: Förlust av evidens eller dubbla sanningar.
- Definition of Done: Definierat ansvar, indexerad historik och verifierade länkar.
- Relaterade dokument: `docs/STATUS.md`, `docs/reports/REPORT-CATALOG.md`, ADR 0010.
- Slutförd: 2026-08-04; kompakt modulstatus skiljer implementation, test, deployment och manuell verifiering.

### BB-007 – Besluta om en enda produktroadmap

- Modul: Produktstyrning
- Typ: Governance
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Fastställ ROADMAP.md:s ansvar och relation till backlog och stabiliseringsplan.
- Motiv: Parallella planeringsytor skapar otydlig prioritet.
- Avgränsning: Ingen reprioritering före produktägarreview.
- Risk: Konkurrerande planer styr arbetet.
- Definition of Done: En normativ roadmap med ägare, scope och länkar.
- Relaterade dokument: `ROADMAP.md`, `docs/BACKLOG.md`, `STABILIZATION_PLAN.md`.

### BB-008 – Aktivera eller avveckla CHANGELOG-policy

- Modul: Releasehantering
- Typ: Governance
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Besluta om CHANGELOG ska underhållas och definiera trigger och format.
- Motiv: En tom eller oägd changelog ger falska förväntningar.
- Avgränsning: Ingen retroaktiv historik utan verifierbar källa.
- Risk: Releaseförändringar blir svåra att följa.
- Definition of Done: Dokumenterad policy och första tillämpning, eller tydlig avveckling.
- Relaterade dokument: `CHANGELOG.md`, `ROADMAP.md`.

### BB-009 – Klassificera Proposed ADR 0002 och 0006–0009

- Modul: Arkitektur
- Typ: ADR-review
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Granska om förslagen ska accepteras, ersättas eller förbli Proposed.
- Motiv: Långvarigt Proposed-läge gör auktoriteten oklar.
- Avgränsning: Status ändras endast genom explicit arkitekturreview.
- Risk: Implementation och föreslaget beslut divergerar.
- Definition of Done: Varje ADR har dokumenterat reviewbeslut och evidens.
- Relaterade dokument: `docs/adr/0002-sentinel-exclusive-system-access.md`, `docs/adr/0006-sentinel-local-transport-identity-and-request-proof.md`, `docs/adr/0007-sentinel-v1-system-metrics-schemas-and-compatibility.md`, `docs/adr/0008-sentinel-system-metrics-policy-classification-and-audit.md`, `docs/adr/0009-sentinel-system-metrics-packaging-privilege-and-supply-chain.md`.

### BB-010 – Flytta TESTING.md till rätt runbookstruktur

- Modul: Kvalitet
- Typ: Dokumentationsstruktur
- Prioritet: P3
- Status: Klar
- Upptäckt: 2026-08-03
- Beskrivning: Separera testpolicy från körinstruktioner och senare placera dem rätt.
- Motiv: Normativ policy och procedur bör vara tydligt åtskilda.
- Avgränsning: Ingen flytt i denna fas; länkar granskas först.
- Risk: Brutna länkar eller otydlig Definition of Done.
- Definition of Done: Godkänd målstruktur, bevarad historik och gröna länkar.
- Relaterade dokument: `TESTING.md`, `docs/operations/runbooks/dashboard-widget-framework-verification.md`.
- Slutförd: 2026-08-04; rotfilen är en testkarta och Dashboard-proceduren har en verifierad runbook.

### BB-011 – Konsolidera STABILIZATION_PLAN

- Modul: Produktstyrning
- Typ: Dokumentationsskuld
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Separera roadmap, teknisk skuld och verifierade buggar.
- Motiv: En plan ska inte fungera som osorterad felinkorg.
- Avgränsning: Inget stängs eller flyttas utan evidens och review.
- Risk: Prioriteringar och problemstatus misstolkas.
- Definition of Done: Varje punkt har rätt hemvist, ägare och status.
- Relaterade dokument: `STABILIZATION_PLAN.md`, `ROADMAP.md`, `docs/BACKLOG.md`.

### BB-012 – Införa återkommande baseline-review

- Modul: Operations/Dokumentation
- Typ: Governance
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Definiera intervall, scopeägare och current-post per baselinetyp.
- Motiv: Daterad evidens får inte bli odaterad sanning.
- Avgränsning: Ingen automation innan retention och secret-scan godkänts.
- Risk: Föråldrade baselines styr felsökning och beslut.
- Definition of Done: Godkänd kalender, ägare, statusmodell och indexuppdatering per scope.
- Relaterade dokument: `docs/indexes/baselines.md`, ADR 0010.

### BB-013 – Persist Smart Shuffle sessions

- Modul: Media
- Typ: MVP-begränsning
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-03
- Beskrivning: Ersätt processlokal Smart Shuffle-state först när restartåterställning eller flera API-repliker krävs.
- Motiv: MVP:n förlorar automationstillstånd vid API-restart och stoppar inte redan startad TV-uppspelning.
- Avgränsning: Ingen databas eller distribuerad låsning införs utan verifierat behov och nytt arkitekturbeslut.
- Definition of Done: Godkänd persistensmodell, säkert återupptagande, idempotenta övergångar och multi-replika-test.
- Relaterade dokument: `docs/modules/media.md`, ADR 0011.

### BB-014 – Smart Shuffle – manuell TV-verifiering och MVP-härdning

- Modul: Media / Smart Shuffle
- Typ: Verifiering / härdning
- Prioritet: P2
- Status: Pågår
- Upptäckt: 2026-08-03
- Beskrivning: Smart Shuffle MVP är implementerad, publicerad och automatiskt testad. UI-styrd start, rätt `NowPlayingItem` och användarstyrt skip är verifierade på den verkliga Samsung-TV:n. Naturlig avsnittsövergång, stopplivscykel och API-restartens processlokala beteende återstår för fullständig end-to-end-verifiering.
- Avgränsning: Verifieringen ska utlösas genom användarens BigBrain-gränssnitt; ingen terminal eller automatiskt test får starta verklig uppspelning.
- Definition of Done:
  - TV:n visas som valbar enhet i BigBrain utan rått UserId eller session-ID.
  - Användarens knapptryck startar exakt seriens valda nästa osedda avsnitt.
  - Nästa serie väljs automatiskt när avsnittet slutar och samma serie undviks direkt när alternativ finns.
  - Skip fungerar mot den verkliga TV-sessionen.
  - Stoppa Smart Shuffle förhindrar nya automatiska byten utan att störa vanlig manuell Jellyfin-användning.
  - Telefonens BigBrain-sida behöver inte hållas öppen.
  - API-restartens processlokala MVP-beteende verifieras och dokumenteras.
  - En slutlig, sekretessgranskad verifieringsrapport skapas.
- Relaterade dokument: `docs/modules/media.md`, `docs/STATUS.md`, ADR 0011.

### BB-015 – Design system v1 – manuell visuell och Tizen-verifiering

- Modul: BigBrain Web / Media
- Typ: Verifiering
- Prioritet: P2
- Status: Ny
- Upptäckt: 2026-08-04
- Beskrivning: Kör den dokumenterade visuella kontrollen av samtliga BigBrain-teman och verifiera separat, efter uttryckligt installationsgodkännande för aktuell adaptervariant, om serverbaserad Jellyfin Custom CSS påverkar den verkliga Samsung Tizen-klienten. Obsidian Gold är deployat i BigBrain Web men ännu inte mänskligt visuellt godkänt; dess separata Jellyfin-adapter är inte installerad.
- Avgränsning: Ingen automatisk Jellyfin-publicering, klientfork eller TV-patch. Custom CSS säkerhetskopieras och installeras endast manuellt efter separat godkännande.
- Definition of Done: BigBrains mörka, ljusa och Obsidian Gold-teman är manuellt verifierade vid 320 px, mobil, desktop, tangentbord och 200 % text; aktuell Jellyfin-variant är separat installerad efter backup och Jellyfin Web desktop/mobile är visuellt verifierat; verklig Tizen-effekt och fungerande selectors är dokumenterade eller uttryckligen klassade som ej stödda.
- Relaterade dokument: `docs/design-system/manual-verification.md`, `themes/jellyfin/compatibility.md`, ADR 0012.
- Sprint 1-bedömning 2026-08-07: automatisk synk med BigBrains aktuella tema implementeras inte. Jellyfin laddar manuellt installerad server-CSS i en separat origin och klientlivscykel och kan inte läsa BigBrains `data-theme` eller localStorage. Dynamisk koppling skulle bryta ADR 0012:s fristående adaptergräns. Posten förblir öppen för den redan definierade manuella variantinstallationen och Tizen-verifieringen.

### BB-027 – Dashboardprofiler, synkronisering och avancerade widgetlayouter

- Modul: BigBrain Web / Dashboard
- Typ: Framtida arkitektur och funktion
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-04
- Beskrivning: Utred nästa dashboardfas med per-user och delade dashboards, mallar, profiler, rollbaserade layouter, användarvalda widgetstorlekar, verkställda widgetbehörigheter och serversynkronisering. Phase 1 är avsiktligt lokal och enhetsbunden.
- Avgränsning: Ingen backendpersistens, identitetsmodell, synkkonfliktlösning eller behörighetsmotor införs innan separata kontrakt och faktisk användarmodell finns.
- Definition of Done: Godkänd ägarskaps- och identitetsmodell, versionssatt synkkontrakt, konflikt- och migreringsstrategi, behörighetstester, tillgänglig storleksredigering, offlinebeteende, säkerhetsgranskning och manuell fleranvändarverifiering.
- Relaterade dokument: `docs/architecture/dashboard-widget-framework.md`, ADR 0014.

### BB-034 – Shopping List – upptäck snarlika varor innan tillägg

- Modul: Inköpslista
- Typ: Bugg / UX / datakvalitet
- Prioritet: P1
- Status: Klar
- Upptäckt: 2026-08-07

#### Problem och verifierat nuläge

Den befintliga kontrollen använder ett normaliserat namn och stoppar exakta träffar, men fångar inte tillräckligt väl mindre skrivvariationer. I verklig användning kan exempelvis `Lördags godis` och `Lördagsgodis` förekomma samtidigt trots att de sannolikt avser samma vara. BB-001 gäller återkopplingen för en redan stoppad exakt dubblett och täcker därför inte detta fynd; en separat post behövs.

#### Framtida lösningsriktning

- Utred och verifiera normalisering för skiftläge, inledande och avslutande whitespace, flera mellanslag, sammanskrivning kontra mellanslag, bindestreck och relevant diakritik.
- Komplettera vid behov med enkel fuzzy similarity och en dokumenterad säkerhetsmodell.
- Vid en sannolik men osäker dubblett ska UI visa exempelvis `Liknande vara finns redan: Lördagsgodis` och erbjuda använd befintlig, lägg till ändå och avbryt.
- Osäker likhet får inte automatiskt blockera ett tillägg eller skriva över ett befintligt listobjekt.
- Undvik aggressiv matchning som utan tydlig säkerhetsmodell slår ihop skilda produkter, exempelvis `mjölk` och `havremjölk`.

#### Definition of Done

- En verifierad normaliserings- och säkerhetsstrategi är dokumenterad.
- Testfall täcker vanliga skrivvariationer, inklusive `Lördags godis`/`Lördagsgodis`, samt negativa närliggande fall.
- Sannolika dubbletter upptäcks innan tillägg.
- Användaren kan välja använd befintlig, lägg till ändå eller avbryt när matchningen är osäker.
- Inga befintliga listobjekt skrivs över.
- Feedbacken är tillgänglig och fungerar på mobil och desktop.
- Relevanta frontend- och backendtester finns.
- Dokumentationen är uppdaterad.

#### Lösning och verifiering

En separat konservativ jämförelsenyckel tar bort whitespace, Unicode-bindestreck och diakritiska markörer. Ingen edit distance, substringmatchning eller annan fuzzy-algoritm används. En sannolik träff visar befintlig vara och kräver uttryckligt val mellan använd befintlig, lägg till ändå och avbryt. `mjölk` och `havremjölk` förblir olika. Frontend-, API- och storetester samt production build verifierar beteendet. API-/Web-fixen är deployad och manuellt godkänd av produktägaren.

### BB-035 – Shopping List – förbättra läsbarheten för Ofta köpt

- Modul: Inköpslista
- Typ: Bugg / UX / tillgänglighet
- Prioritet: P2
- Status: Klar
- Upptäckt: 2026-08-07

#### Problem och verifierat nuläge

Kapselknapparna under ”Ofta köpt” använder en textfärg som kan ge otillräcklig kontrast mot knappytan, så flera knappar ser nästan tomma ut. BB-016 behandlar vilka förslag som visas och täcker inte läsbarhet eller tillståndsstyling; en separat post behövs.

#### Omfattning

Kontrollera Ljust, Mörkt och Obsidian Gold samt default, hover, focus, active, disabled och selected state. Läsbarhet, fokusmarkering och mobil layout ska verifieras utan att försämra knappens träffyta eller begriplighet.

#### Definition of Done

- Texten är tydligt läsbar i alla stödda teman och tillstånd.
- WCAG-relevant kontrast är mätt, dokumenterad och uppfylld för text och nödvändiga visuella indikatorer.
- Knapptext försvinner inte i default, hover, focus, active, disabled eller selected state.
- Synlig fokusmarkering fungerar med tangentbord.
- Mobil layout, radbrytning och touchyta fungerar.
- Snapshot-, komponent- eller motsvarande regressionstest skyddar samtliga relevanta tillstånd och teman.
- Dokumentationen är uppdaterad.

#### Lösning och verifiering

Knapparna använder nu semantiska surface/text-, primary/contrast-, focus- och disabled-tokens i stället för inverse-text på en vanlig surface. Default, hover, focus, active/selected och disabled skyddas av regressionstest. Production build och headless Chromium i Ljust, Mörkt och Obsidian Gold vid mobilbredd godkändes 2026-08-07. Web-fixen är deployad och manuellt godkänd av produktägaren i relevanta teman och states.

### BB-036 – BigBrain – realtidssynkronisering av delad familjedata

- Modul: BigBrain-plattformen / första konsument Inköpslista
- Typ: Arkitektur / funktion / realtid
- Prioritet: P1
- Status: Ny
- Upptäckt: 2026-08-07

#### Problem och användningsfall

En redan öppen klient uppdateras inte automatiskt när en familjemedlem ändrar inköpslistan från en annan klient. Användaren måste ladda om eller navigera om. Första konkreta användningsfallet är Inköpslista, men behovet ska utredas som ett gemensamt realtidskontrakt för BigBrain och inte som ett modulspecifikt specialfall. BB-027 gäller dashboardprofiler och serversynkroniserad layout och täcker inte server push för delad familjedata.

#### Arkitekturriktning och omfattning

- Utred i första hand ASP.NET Core SignalR/WebSockets i linje med `ARCHITECTURE.md`; aggressiv polling ska inte bli standard utan dokumenterat skäl.
- Ett separat ADR krävs före implementation för transport, kontrakt, modulägarskap, eventversionering, återhämtning, konfliktmodell, authorization, säkerhet och fallback.
- Första integrationen ska synka skapad, uppdaterad, borttagen och avbockad/återställd vara i Inköpslista.
- Kontraktet ska hantera automatisk reconnect, återhämtning efter avbrott, stale-client-resync, flera samtidiga klienter, ordning/versionering, idempotens och definierade konflikter.
- Framtida möjliga konsumenter är Matlista, Kalender, Påminnelser, Mediajobb, Download Control samt AI-jobb/notiser och andra delade familjemoduler.
- Framtida authorization ska följa faktisk användarmodell. Secrets och interna identifierare får inte exponeras i onödan.
- Normal målsättning på lokalt nät är synlig uppdatering inom cirka 1–2 sekunder utan reload, navigation eller manuell refresh.
- UI kan diskret visa live/ansluten, återansluter och offline.

#### Definition of Done

- Ett gemensamt, versionssatt realtidskontrakt och tillhörande ADR är godkända och dokumenterade.
- Inköpslista är första integrerade konsument och create/update/delete/check-state synkas.
- Automatisk reconnect och stale-state-resync fungerar.
- Konflikt-, ordnings-, versions- och idempotensmodeller är definierade och testade.
- Flera samtidiga klienter är integrationstestade.
- Fallback- och offlinebeteende är dokumenterat; lösningen använder ingen onödig polling.
- Authorization, informationsminimering och övrig säkerhet är granskade.
- Integrationstester, relevant dokumentation och runbook finns.
