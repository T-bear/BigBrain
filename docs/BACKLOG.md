# BigBrain Backlog

Avstämd 2026-10-24 i DOC-2B mot accepterad main `ab6a84b3bde807fe5ec38bfcba35ae9b85c130c5`.
Detta är ingången till kvarstående arbete, DoD, beroenden och acceptansgrindar. Produktinriktning
finns i [Roadmap](../ROADMAP.md), verifierat tillstånd i [STATUS](STATUS.md), avbrott i
[recovery](operations/codex-recovery.md). Ingen prioritet ändras och ingen post stängs genom konsolideringen.

Statusord Ny/Bekräftad/Planerad/Pågår behåller sitt befintliga scope; ACTIVE betyder kvarstående
arbete, inte en startorder. BLOCKED anger en konkret grind, UNCERTAIN otillräcklig evidens,
FROZEN ett kanoniskt ägarbeslut. Uppskjutet arbete kräver separat prioritering. Klar/Avvisad är
historiska dispositioner i [fulltextarkivet](history/backlog-before-doc-2b-20261024.md), inte nya beslut.
P0/P1/P2/P3 behåller befintlig innebörd och prioritet. Tester/CI, merge, deployment och manuell
acceptans är olika saker. Källhänvisningarnas B-nummer pekar på DOC-2B:s ursprungliga avsnitt.

## Frysningar och kvarstående operativa grindar

### BB-158 — Media URL Import & Audio Extraction

**FROZEN / OWNER DECISION REQUIRED TO RESUME sedan 2026-10-23**, hela BB-158:
direkt MP3/M4A, YouTube, extraktion/konvertering, DOI/ISBN, Librarr-import, API/Web, tester och deployment.
[Kanoniskt beslut och återupptagning](operations/bb158-freeze-20261023.md) styr, inte gamla launchpaket
eller fortsättningsinstruktioner. Accepterad direktimport/valfri DOI/Sonarr-fix är inte bevis för
aktuell drift: images, aktivering, importer, backup, biblioteksintegritet och recoveryutfall är UNKNOWN / NOT REVERIFIED.
YouTube/ISBN/konvertering är inte implementerade; kontrollerad egress och verktygs-/resurs-/avbrottsisolering
är ofullständigt verifierade. Ingen ny säkerhetsutredning följer av denna not.

Kvarstående skydd: rätt att hämta, inga DRM-/auth-/betalväggsgenvägar, SSRF/DNS/redirect-/privat-IP-skydd,
format-/storleks-/tids-/resursgränser, scannerisolering, rättigheter till filer, atomär reservation utan
överskrivning, import-ID/SHA/ABS-avstämning, separat bekräftelse och bevarad DOI/proveniens.
Inga secrets eller godtyckliga shellkommandon från indata. Osäkra importer får inte automatiskt återställas
eller köras om; backup/rollback, aktivitet och sessionsoberoende recovery måste senare verifieras.
Befintlig drift och media lämnas orörda. [Ursprunglig post](history/backlog-before-doc-2b-20261024.md#bb-158--media-url-import--audio-extraction).

<a id="bigbrain-finance--policy-governed-autonomous-trading"></a>

## Finance — fryst arbete och kvarstående krav

**FROZEN sedan 2026-10-16. RESEARCH / 0 SEK / NONE.**
[Finance-frysningen](operations/finance-freeze-20261016.md) är kanonisk. G1/G2 är dokumenterat PASS,
**G3a BLOCKED / G3b NOT RUN**. Ingen ny källa, grant, inferens, forskningssession, PAPER/LIVE/AUTO,
brokerkoppling, deployment eller upptining. Befintlig drift lämnas orörd utan separat beslut.

### Rättigheter, integritet och vetenskap

- Rättigheter är källa/produkt/marknad/capability-specifika; unknown/expired/mismatched scope avvisas.
  Twelve Data Personal-besked gäller inte Basic eller andra användningar. Alpaca-riskacceptans är ingen
  providergaranti; de sex besvarade frågorna ska inte skickas igen, men villkor/feed/lifecycle och policy/mapping
  måste vara giltiga. SimFin/Stooq och Avanza har kvarvarande rättighetsgrindar; Yahoo/yfinance och beständig
  Tiingo Starter-intake är inte generellt tillåtna. Ingen skyddad nedladdningskontroll får kringgås.
- Retention/deletion/backup ärvs av raw, härledd evidens och audit. EODHD får inte behandlas som WIKI:s
  public-domain-minne. BB-132K:s faktiska privata disposal-frist är okänd; frysningen förlänger inga frister.
  Dokumenterad plaintext-avvikelse är inte ett secure-erase/no-exposure-bevis. Se [F/G i frysdokumentet](operations/finance-freeze-20261016.md#f-driftinventering--daterad-evidens-ingen-driftmutation).
- Quarantine/drop/promotion ska behålla symlink-/traversal-/ZIP-/storleks-/schemagrindar och explicit
  promotionsreview. BB-122:s identitets-/venueintervall och ticker-reuse är inte lösta av nutida symbolregister;
  BB-127:s GOOG-identity/rights/price-basis/corporate-actions kräver evidens. Ingen generell security master,
  tickerbaserad promotion eller provider stitching. WIKI-snapshotens 2014–2016-scope är inte decennier/final 2018.
- BB-045/073 har historiskt levererade syntetiska och senare avgränsade EODHD-grunder, inte stängd full DoD.
  Behåll licensed versioned datasets, immutable revision/correction/deletion, timezone/DST/session/gap,
  canonical identity, corporate actions, quality/duplicates, measured store/backup och exakt replay/proveniens.
  Alla provider-/received-/knowledge-tider, prospective prediction identity och senare utfall ska vara ärliga;
  inget historiskt backfill som prospektiv evidens.
- BB-048/049 är klara endast för daglig research via BB-123/124. Partial fills/volymtak och intraday/spread/
  sector/portfolio-exponering är inte fullständigt utvärderbara. Kort/biased historik, survivorship och okänd
  tidigare exponering begränsar slutsatser; BB-129A:s 24 INCONCLUSIVE/NOT EVALUABLE är inte lönsamhetsbevis.
  DSR/PBO är NOT_EVALUABLE. Orörd holdout, embargo, trial-population/all-attempt accounting, native evaluator,
  negativkontroller och avvisning av leakage/noise/regimfragilitet kvarstår. Ingen modellstyrd risk-/sciencepolicy.
- BB-131/132:s delade ledger, adaptiv testhistorik, versionsbundet modellkontrakt, framtida auth/audit/export
  och lokalt ägd worker är bindande gränser. Nya hypoteser får inte bli kod/plugins eller egen exekveringsauktoritet.
  Genuine multi-real-session/multistrategy-evidens återstår; datakvalitet kan inte ersättas av mer modellkörning.
- BB-130B:s uppskjutna Finance-latensfråga kvarstår: intermittenta reads bortom 45-sekundersprovet,
  nio samtidiga explicit öppnade detaljreads och stora payloads. Ingen B owner/mobile approval är styrkt;
  BB-130:s avslut ger inte rätt till nytt endpoint-/sciencearbete. BB-119 har korrigerat BB-118:s dåtida readinessfel,
  men dagens runtime verifieras inte här.

Källor: [B045–058](history/backlog-before-doc-2b-20261024.md#finance-source-evidence-update--2026-09-06),
[B112](history/backlog-before-doc-2b-20261024.md#bb-045--m2-historical-market-data-ingestion-and-provenance),
[B117](history/backlog-before-doc-2b-20261024.md#bb-073--live-market-observation-and-prospective-shadow-learning),
[B128–129](history/backlog-before-doc-2b-20261024.md#bb-048--transaction-cost-slippage-and-fill-simulation),
[BB-131](history/backlog-before-doc-2b-20261024.md#bb-131--finance-research-learning-foundation),
[BB-090–095](history/backlog-before-doc-2b-20261024.md#bb-090--fred-macro-and-finance-correctness-stabilization).

### BB-045 / BB-073 — öppna fullständiga data- och forward-evidenskrav

**FROZEN / full DoD inte stängd.** BB-045 prioritet P2, beroenden BB-044/046 och accepterad ADR0021;
BB-073 prioritet P2, beroende BB-045 samt BB-071 och separat ägarbeslut för externa data.
De gamla globala providerblocken är delvis ersatta av senare avgränsad EODHD-aktivering, inte av
generellt tillstånd eller bevis för dagens drift. Bevara följande DoD utöver gränserna ovan:

- BB-045: licensed, versionsbundna dataset med source/timezone/corporate-action/quality/gap/duplicate-prov;
  okänd/utgången/odeklarerad användning avvisas; correction/deletion/backup är explicit och testat.
  Kvarstående production-store/backup-benchmarks, rikare kvalitetsaggregation, källrättigheter och extern
  acceptans ska disponeras med exakt senare evidens, inte antas färdiga från syntetiska benchmarktal.
- BB-073: exakt auktoriserat feed normaliseras och lagras entitlement-aware; immutable version-isolerade
  prediction/outcome och no-lookahead; prospektiva mått omfattar costs/tail risk utan execution capability.
  Rikare calibration/drawdown/regime/instrument/horizon-mått och resterande strategi-/feature-scope
  måste bedömas mot senare leveranser innan stängning. Förberett experiment är ingen körorder.
- BB-131: multi-protocol/adaptive admission, oberoende source/holdout, retained negative history,
  authorized projection/use/export, scoped principal/audit, verifierad hostkapacitet och model/runtime-
  provenance, limits/cancellation och adversarial-prov kvarstår. Extern credentials/cost-review får inte
  blockera kvalificerad lokal-only användning; svagt modellutfall medger ingen säkerhetslättnad.
- M5 policy maturation: tillförlitlig sector/spread-evidens, hypotetisk portfolioaggregation och
  exchange-calendar-förfining först när beroenden finns; inga fabricerade värden.
- Prospektiv multi-real-session-evidens är en framtida observationsgrind, inte ett dagligt manuellt
  uppdrag under frysningen. Genuine future EOD-sessioner före kvalitetsanspråk, ingen source stitching.
- BB-082, P1: längre nollkostnadshistorik kräver namngiven verifierbar källa eller normalt stödd
  bulk/API-väg med automation/lagring/proveniens klarlagd; budget och åtkomstkontroller består.

Källor: [BB-045](history/backlog-before-doc-2b-20261024.md#bb-045--m2-historical-market-data-ingestion-and-provenance),
[BB-073](history/backlog-before-doc-2b-20261024.md#bb-073--live-market-observation-and-prospective-shadow-learning),
[BB-131](history/backlog-before-doc-2b-20261024.md#bb-131--finance-research-learning-foundation),
[BB-082](history/backlog-before-doc-2b-20261024.md#bb-082--longer-zero-cost-historical-market-memory).

### Historisk Qwen, recurring research och observation

Kod är mergad, men faktisk slutlig hostisolering, artefakt-/loader-custody, root-skydd, enrollment,
oberoende append-only-journal/restore-exkludering och RecurringAuthorityMatched saknar samlad PASS.
Korrekt källdata/mapping/cohort, ändlig grant och pinnad modellprofil/resursram måste granskas separat.
I1/E1/T3/C64/W33s och 30s inferens är historisk ram, inte körbar auktorisation. Failure/uncertain kan
förbruka reservation; inget retry/refund eller automatisk grantförnyelse. Gamla A/B/C-grants får inte återanvändas.
Review-only MountFlags=private löser inte ärvda mounts; bred allowlist eller gissad maskering är inte tillåten.

BB-132D/E/F/F1/H behåller separata grindar för live-readiness, verkliga credentials/mapping, trusted freeze,
framtida förseglade utfall och korrekt revisionslineage. Current mapping-verifiering, stängning av adopted
open-ended snapshot/material identity change och cross-version SHADOW är egna beslut. Receipt1000,
journal64/128KiB och mappingversioner4 är fail-closed-gränser, inte godkänd kapacitetsutökning.
Cross-host coordination, lång-outage-backfill och automatisk vetenskaplig utvärdering är inte fria fortsättningar.
Whole-host rollback inklusive authority, witness loss och automatisk repair ingår inte i releasebeviset;
gamla grants får inte återaktiveras utan betrodd aktuell historik. [B003–009](history/backlog-before-doc-2b-20261024.md#historical-qwen-operational-gate).

### Fryst forsknings- och exekveringsplan

Följande öppna BB-ID:n behåller sina ursprungliga prioriteringar, DoD och beroenden. Alla omfattas
av FROZEN oavsett äldre Planerad/Pågår. De är inga nya sprintuppdrag. BB-042:s epic kvarstår,
BB-082:s längre rättighetsklarerade historik är inte löst för alla instrument, och BB-053 är partiell.
[Domänroadmap](architecture/finance/master-roadmap.md) styr sekvensen; rättigheter och skydd ovan gäller före varje steg.

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

[Fulltext B108](history/backlog-before-doc-2b-20261024.md#bb-042--epic-bigbrain-finance--policy-governed-autonomous-trading).

#### Definition of Done

- M0–M15 meet their individual DoD and evidence gates.
- Live modes have current legal/operational review and explicit owner approval.
- Risk cannot be bypassed; emergency controls, audit and reconciliation are proven.

[Fulltext B109](history/backlog-before-doc-2b-20261024.md#definition-of-done-3).

### BB-050 – M4 versioned deterministic strategy contract and candidates

- Modul: Finance / Strategy Engine
- Typ: Implementation / research
- Prioritet: P2
- Status: Planerad
- Beroenden: BB-047.
- Definition of Done: each momentum, trend, breakout, mean-reversion and volume/liquidity
  candidate is independently testable and produces versioned StrategySignal evidence.

[Fulltext B130](history/backlog-before-doc-2b-20261024.md#bb-050--m4-versioned-deterministic-strategy-contract-and-candidates).

### BB-051 – Market-regime classification

- Modul: Finance / Strategy Engine
- Typ: Research / implementation
- Prioritet: P2
- Status: Planerad
- Beroenden: BB-045, BB-050.
- Definition of Done: versioned regimes and uncertainty can enable, disable or weight
  strategies; abnormal volatility or poor liquidity can result in no trade.

[Fulltext B131](history/backlog-before-doc-2b-20261024.md#bb-051--market-regime-classification).

### BB-052 – Multi-strategy candidate decision model

- Modul: Finance / Strategy Engine
- Typ: Implementation
- Prioritet: P2
- Status: Planerad
- Beroenden: BB-050, BB-051.
- Definition of Done: multiple versioned signals create an explainable Candidate Trade;
  tests prove agreement is evidence and cannot authorize execution.

[Fulltext B132](history/backlog-before-doc-2b-20261024.md#bb-052--multi-strategy-candidate-decision-model).

### BB-053 – M5 hard Risk Engine and policy evaluation

- Modul: Finance / Risk
- Typ: Critical safety implementation
- Prioritet: P1
- Status: **PARTIAL / RESEARCH-ONLY FOUNDATION DELIVERED BY BB-089; EXECUTION-GRADE DoD INCOMPLETE**
- Beroenden: BB-044, BB-047; ADR 0018 review.
- Definition of Done: all documented capital, exposure, loss, liquidity, spread, hours,
  data/volatility and health limits are server-enforced and bypass/invariant tested.

[Fulltext B133](history/backlog-before-doc-2b-20261024.md#bb-053--m5-hard-risk-engine-and-policy-evaluation).

### BB-054 – Portfolio engine, sizing and bidirectional compounding

- Modul: Finance / Portfolio
- Typ: Implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-048, BB-053.
- Definition of Done: cash, positions, exposure and current-equity sizing reconcile;
  percentage risk never rises through compounding and sizes shrink after losses.

[Fulltext B134](history/backlog-before-doc-2b-20261024.md#bb-054--portfolio-engine-sizing-and-bidirectional-compounding).

### BB-055 – Exit management, circuit breakers and emergency control

- Modul: Finance / Risk
- Typ: Critical safety implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-053, BB-054.
- Definition of Done: position-opening exit invariant, forced exits, STOP ALL TRADING,
  HALTED behavior, safe-exit policy and resume authorization pass failure drills.

[Fulltext B135](history/backlog-before-doc-2b-20261024.md#bb-055--exit-management-circuit-breakers-and-emergency-control).

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

[Fulltext B136](history/backlog-before-doc-2b-20261024.md#bb-056--m6-strategy-lab-metrics-and-lifecycle-governance).

### BB-057 – M7 restart-safe paper trading engine

- Modul: Finance / Paper Trading
- Typ: Implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-045, BB-050, BB-053, BB-056.
- Definition of Done: persistent simulated cash/positions/orders/fills/P&L/costs support
  delay, partial/rejected orders, restart and daily strategy-attributed summaries.

[Fulltext B137](history/backlog-before-doc-2b-20261024.md#bb-057--m7-restart-safe-paper-trading-engine).

### BB-058 – Paper-trading soak and evidence acceptance

- Modul: Finance / Validation
- Typ: Long-duration validation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-057, BB-070.
- Definition of Done: representative owner-accepted evidence covers regimes, costs,
  drawdown, operations and failures; success is not defined by profitable-day count.

[Fulltext B138](history/backlog-before-doc-2b-20261024.md#bb-058--paper-trading-soak-and-evidence-acceptance).

### BB-059 – M8 Finance dashboard and accessible PAPER/LIVE UI

- Modul: Finance / Web
- Typ: UX / implementation
- Prioritet: P2
- Status: Planerad
- Beroenden: BB-053, BB-056, BB-057.
- Definition of Done: portfolio, P&L, mode/risk, positions/orders, signals, journal,
  warnings and emergency state are responsive/accessibility tested; PAPER/LIVE is unmistakable.

[Fulltext B139](history/backlog-before-doc-2b-20261024.md#bb-059--m8-finance-dashboard-and-accessible-paperlive-ui).

### BB-060 – Swedish/EU legal, tax and operational live-trading research

- Modul: Finance / Governance
- Typ: Research / live gate
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-043.
- Definition of Done: qualified sources and owner review cover broker terms, automated-
  trading restrictions, account/tax/reporting, market-data/API and instrument constraints.

[Fulltext B140](history/backlog-before-doc-2b-20261024.md#bb-060--swedisheu-legal-tax-and-operational-live-trading-research).

### BB-061 – M9 broker evaluation and owner-approved selection

- Modul: Finance / Broker
- Typ: Research / architecture
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-046, BB-060.
- Definition of Done: candidates are compared on Sweden/API/sandbox/instruments/costs,
  data/orders/fractions/limits/security/reliability/terms; no convenience-only selection.

[Fulltext B141](history/backlog-before-doc-2b-20261024.md#bb-061--m9-broker-evaluation-and-owner-approved-selection).

### BB-062 – Broker abstraction and credential-security boundary

- Modul: Finance / Broker
- Typ: Security / implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-061; ADR 0017 review.
- Definition of Done: typed adapter passes fake/sandbox contracts; paper/live secrets use
  least-privilege injection, rotation/revocation and never reach Git, Web, AI or logs.

[Fulltext B142](history/backlog-before-doc-2b-20261024.md#bb-062--broker-abstraction-and-credential-security-boundary).

### BB-063 – Execution verification, idempotency and reconciliation

- Modul: Finance / Execution
- Typ: Critical safety implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-055, BB-062; ADR 0019 review.
- Definition of Done: immutable previews and idempotency prevent duplicates; broker truth
  resolves uncertain/partial/rejected results and material mismatch suspends automation.

[Fulltext B143](history/backlog-before-doc-2b-20261024.md#bb-063--execution-verification-idempotency-and-reconciliation).

### BB-064 – M10 Trading Controller and manual approval mode

- Modul: Finance / Trading Controller
- Typ: Sensitive implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-055, BB-059, BB-060–BB-063, BB-070; explicit owner approval.
- Definition of Done: exact current previews show evidence/risk/cost/size/exit/impact and
  only explicit bound approval can execute once, followed by independent verification.

[Fulltext B144](history/backlog-before-doc-2b-20261024.md#bb-064--m10-trading-controller-and-manual-approval-mode).

### BB-065 – M11 limited live automation

- Modul: Finance / Trading
- Typ: High-risk gated implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-058, BB-063, BB-064; explicit owner promotion.
- Definition of Done: tiny exposure/universe/loss limits, mandatory exits, anomaly stop,
  notification and daily reconciliation are verified with accepted bounded evidence.

[Fulltext B145](history/backlog-before-doc-2b-20261024.md#bb-065--m11-limited-live-automation).

### BB-066 – M12 long-duration limited-live validation

- Modul: Finance / Validation
- Typ: Long-duration validation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-065.
- Definition of Done: representative net expectancy, drawdown, regime, drift, incident and
  reconciliation evidence is accepted; elapsed time alone is insufficient.

[Fulltext B146](history/backlog-before-doc-2b-20261024.md#bb-066--m12-long-duration-limited-live-validation).

### BB-067 – M13 policy-governed AUTO

- Modul: Finance / Trading
- Typ: High-risk gated implementation
- Prioritet: P1
- Status: Planerad
- Beroenden: BB-066, no critical defects, current BB-060 gate; explicit owner approval.
- Definition of Done: policy-bounded autonomous entry/management/exit and suspension pass
  independent invariant, circuit-breaker, reconciliation and emergency review.

[Fulltext B147](history/backlog-before-doc-2b-20261024.md#bb-067--m13-policy-governed-auto).

### BB-068 – M14 BigBrain Autonomic Finance capabilities

- Modul: Finance / BigBrain.Brain
- Typ: Future integration
- Prioritet: P2
- Status: Planerad
- Beroenden: stable BB-063/BB-067 capabilities and security review.
- Definition of Done: OBSERVE–DIAGNOSE–DECIDE–POLICY–ACT–VERIFY uses the normal Trading
  Controller; adversarial tests prove AI cannot access credentials or bypass policy.

[Fulltext B148](history/backlog-before-doc-2b-20261024.md#bb-068--m14-bigbrain-autonomic-finance-capabilities).

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

[Fulltext B149](history/backlog-before-doc-2b-20261024.md#bb-069--m15-continuous-strategy-governance-and-drift-control).

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

[Fulltext B150](history/backlog-before-doc-2b-20261024.md#bb-070--decision-journal-observability-and-failure-injection).

Övrig vilande produktavsikt: Investopedia endast utbildnings-/hypoteskontext efter källrättighets- och
proveniensgranskning genom befintliga deterministiska grindar; ingen scraper/Knowledge Layer.
Avanza/MCP är RIGHTS_UNKNOWN / MANUAL_REVIEW; macro-/FX-/råvaru-/fundamental-/point-in-time-datapaket
är separata framtida frågor. Ingen ny prioritet. [B045](history/backlog-before-doc-2b-20261024.md#finance-source-evidence-update--2026-09-06),
[B083–084](history/backlog-before-doc-2b-20261024.md#finance--m5-policy-maturation),
[BB-090](history/backlog-before-doc-2b-20261024.md#bb-090--fred-macro-and-finance-correctness-stabilization).

## Säkerhet, drift och arkitekturgrindar

### BigBrain security and penetration-testing baseline

- Modul: Cross-cutting Security; status: Planerad; identifier awaits normal product prioritization.
- Beslut: implement continuous CI-oriented secret/dependency/SAST/container/API/header/
  supply-chain/configuration checks and controlled black-/grey-box testing of Web, API,
  authentication/authorization, IDOR, injection, XSS, CSRF, CORS, malformed input, traversal,
  resource exhaustion, containers/ports/volumes/host privilege, Sentinel transport/request proof/
  replay, Finance CSV/ZIP/provenance/revision/mode gates, recovery/persistence tampering and supply
  chain. Potentially destructive tests must use isolated test environments/data.
- Gate: mandatory before real-money LIVE eligibility and a strong prerequisite before meaningful
  PAPER/execution authority. This item is planning only; BB-086 implements none of it.

[Fulltext B091](history/backlog-before-doc-2b-20261024.md#bigbrain-security-and-penetration-testing-baseline).

R015:s kvarstående auth-design-gate gäller broker/trading, Docker-/kamerakontroll,
high-impact Home Assistant och bredare nätverksexponering. Kontrollerad destruktiv testning sker
isolerat. Striktare PAPER-krav i accepterade domänkontrakt får inte försvagas av ordet stark;
aktuell generell pentestbaseline är fortfarande planerad, inte genomförd.

### BB-083 – Appliance lifecycle and crash recovery baseline

- Modul: Platform / Operations / Finance safety; prioritet P0.
- Status: implementerad och container-runtime-verifierad 2026-08-12; hostinstall,
  Docker-daemon restart/reboot blocked by interactive sudo; physical power-cycle pending.
- Delivered: ADR 0026, clean/unclean journal, recovery/storage/disk/clock gate, explicit job
  policies, interrupted EODHD safety, API/UI, Compose grace/readiness, systemd and scripts.
- Evidence: API PID-1 crash returned healthy/`UNCLEAN`; Finance stayed at 16 requests,
  4,016 raw observations and 16 immutable revisions.
- Report: `docs/reports/features/resilience/bigbrain-bb-083-appliance-resilience-20260812.md`.

[Fulltext B099](history/backlog-before-doc-2b-20261024.md#bb-083--appliance-lifecycle-and-crash-recovery-baseline).

### BB-009 – Klassificera Proposed ADR 0002 och 0006–0009

- BB-130A review 2026-09-05 extends this existing follow-up to ADR 0005 conformance:
  fixed allowlist/proof checks do not establish signed-overlay lifecycle; certificate
  validity/EKU and lifecycle tests, complete response validation/bounds, truthful
  identity/policy/timing/completion audit, and container/in-process privilege/host-semantics
  evidence remain incomplete. Status: **OPEN / SECURITY REVIEW REQUIRED**, no waiver.
- DoD extension: owner/architect-reviewed disposition of each gap, isolated negative/
  compatibility/security tests and truthful runtime scope before full acceptance.
  New privilege or security semantics require a separate Proposed ADR; no hardening
  implementation is smuggled into BB-130A. Local ADR 0006–0009 stay unpublished.

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

[Fulltext B258](history/backlog-before-doc-2b-20261024.md#bb-009--klassificera-proposed-adr-0002-och-00060009).

## Family, hushåll och uppskjuten samordning

<a id="future-family-view--family-coordination-epic"></a>

### Family View och Family Coordination

Framtida umbrella-epik, inte generellt prioriterad eller auktoriserad genom Skollunch v2.
Family är ett koordinationslager, inte en ny datakälla eller konkurrerande widgetdashboard.
Calendar, skolor, Meal Planner, Shopping List, aktiviteter och hushåll/member context äger sina data.
Versionssatta capabilities får normalisera referenser, inte klona/ändra källposter.
Kvar: member association, skolschema/kalender/lov/studiedagar, attendance, Family Context,
hard/soft/information evaluation, Day/Week, provenance/freshness, manuellt override, privacy/security
samt sparsamma notiser. Beroenden BB-029, BB-036, BB-027, auth-hardening och Home Calendar/swipe.
[Normativ planeringsgräns](architecture/family-view-family-coordination.md),
[ursprungligt produktmål](history/backlog-before-doc-2b-20261024.md#future-family-view--family-coordination-epic).
Äldre Finance-före-Family-prioritet är historisk; detta skapar ingen ny prioritering.

### Skollunch — kvarstående livegrindar

**BLOCKED för extern aktivering/liveacceptans.** V1 och v2-plan är mergade; v2-implementation är
mergad via `c8aaba15ac6382b6934591acfa6f615505c95819` och CI40004794277 SUCCESS enligt
[STATUS](STATUS.md#family--skollunch). Offline-original/fixtures, RSS/HTML-parser, read-only API,
hämtning vid behov och sex timmars minnescache är verifierade för den revisionen.
Det bevisar inte deployment, produktionsinställning, källrättigheter eller fysisk iPhone/PWA-acceptans.

Kvarstående DoD: verifierade rättigheter/villkor för automatisering och cache för båda faktiska
leverantörerna, separat aktiverings-/deploymentbeslut, verkliga veckomenyer och ägaracceptans.
Källidentitet, originaltext/alternativ, datum/ISO-vecka, härlett kontra verifierat år, freshness,
timeout/HTTP/ogiltig/saknad/stale meny ska vara sanningsenliga. Ingen fabricerad lovstatus eller
fixture som live; ingen godtycklig URL, scheduler, databas eller påverkan på hushållsmiddagar/inköp.
Endast källstödda veckor. Hushållets planerade helglunch och modulägarskap bevaras.
[Fulltext B096](history/backlog-before-doc-2b-20261024.md#existing-family-epic-member-external-school-meals-in-meal-planner),
[käll-/implementationsrapport](reports/features/family/school-meals-preparation-20261023.md).

### Existing Family-epic member: School-aware household menu generation

- Module: Meal Planner / Shopping List; status: future Meal Planner intelligence.
- Intent: household weekly generation considers all relevant school sources alongside household
  preferences, meal history, variation, leftovers, budget, supported available ingredients and
  shopping-list integration. Avoid materially similar home food most strongly on the same day,
  strongly around adjacent days and more softly farther away; exact weights remain future design.
- Similarity: reason over protein, carbohydrate/base, dish type, preparation, sauce/style and broad
  flavor/cuisine family rather than dish-name strings alone. This task authorizes no classifier.
- Control: similarity is a soft optimization constraint across every relevant external source;
  manual household choice wins. Generated household meals may create shopping requirements, but
  school meals never add their own ingredients to the household shopping list.

[Fulltext B097](history/backlog-before-doc-2b-20261024.md#existing-family-epic-member-school-aware-household-menu-generation).

### Referenced Home/Calendar work: past-day presentation and mobile swipe investigation

- Module: BigBrain Web / Home; status: future UX enhancement/investigation.
- Calendar intent: Calendar is a module in Home, not a standalone view. Past days should visually
  recede so current/upcoming days are immediately understandable. Exact styling remains open and
  must not rely on color or opacity alone; reduced emphasis or appropriate strike-through may be
  evaluated with accessible state semantics.
- Swipe intent: evaluate mobile-first touch navigation only between appropriate BigBrain views or
  contexts. It must coexist with browser/system back gestures and horizontal controls, retain
  visible and keyboard/mouse navigation, preserve accessibility and avoid accidental destructive
  actions. This is not an approved interaction specification.

[Fulltext B098](history/backlog-before-doc-2b-20261024.md#referenced-homecalendar-work-past-day-presentation-and-mobile-swipe-investigation).

### BB-029 – Kalender – externa synkar och personliga kalenderfunktioner

- Modul: Kalender
- Typ: Framtida funktion
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-05

Omfattar framtida Google/Apple/ICS-synk, familje- och manuella händelser, påminnelser, per-user kalender, pushnotiser, AI-planeringsförslag och arbetstidsstatistik. Funktionerna kräver separata ägarskaps-, behörighets-, konflikt- och integritetsbeslut och ingår inte i Heroma-MVP:n.

[Fulltext B158](history/backlog-before-doc-2b-20261024.md#bb-029--kalender--externa-synkar-och-personliga-kalenderfunktioner).

#### Definition of Done

- En prioriterad del har verifierat användarbehov och separat arkitekturbeslut.
- Ägarskap, per-user-scope, sync-konflikter, auktorisering och retention är dokumenterade och testade.

[Fulltext B159](history/backlog-before-doc-2b-20261024.md#definition-of-done-8).

### BB-001 – Ingen synlig återkoppling när en dubblettvara stoppas

- Prioritet: P2
- Status: Bekräftad

Öppen återkopplingsbugg för en redan stoppad exakt dubblett. Ändra inte dubblettregel eller datamodell; BB-034:s likhetskontroll stänger inte denna post.

#### Förväntat beteende

Användaren ska få en tydlig återkoppling, exempelvis:

- `Korv finns redan på listan.`
- möjlighet att öka antal;
- möjlighet att visa den befintliga varan;
- möjlighet att avbryta och fortsätta skriva.

#### Avgränsning

Ingen ändring av dubblettregeln eller datamodellen krävs. Felsök frontendens hantering av API-svaret och dubblettdialogens rendering.

#### Definition of Done

- Ett stoppat dubblettförsök ger synlig återkoppling.
- Ingen dubblett skapas.
- Fokus återgår till ett logiskt ställe.
- Dialogen visas ovanför handlingsläget.
- Fungerar på mobil och desktop.
- Regressionstest finns.

[Fulltext B183–B189](history/backlog-before-doc-2b-20261024.md#bb-001--ingen-synlig-återkoppling-när-en-dubblettvara-stoppas).

### BB-002 – Bakgrundssidan kan scrollas bakom handlingsläget

- Prioritet: P2
- Status: Ny

Intermittent bakgrundsscroll i handlingsläget, reproduktionssekvens ännu osäker. Endast listans interna innehåll ska scrolla.

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

[Fulltext B190–B195](history/backlog-before-doc-2b-20261024.md#bb-002--bakgrundssidan-kan-scrollas-bakom-handlingsläget).

### BB-003 – Scrollindikator visas trots att listan verkar rymmas

- Prioritet: P3
- Status: Ny

Intermittent scrollindikator/falsk overflow; faktisk orsak ännu okänd. Ingen indikator när innehållet ryms.

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

[Fulltext B196–B201](history/backlog-before-doc-2b-20261024.md#bb-003--scrollindikator-visas-trots-att-listan-verkar-rymmas).

### BB-016 – Ofta köpt visar varor som redan finns i inköpslistan

- Prioritet: P2
- Status: Ny

Ofta köpt-filtrering kvarstår; BB-035:s kontrastfix stänger inte denna post.

#### Förväntat beteende

- Varor som redan finns i inköpslistan filtreras bort från ”Ofta köpt”.
- En vara som läggs till från ”Ofta köpt” försvinner direkt efter lyckat tillägg.
- Tillägg genom vanlig textinmatning uppdaterar också förslagen.
- En borttagen vara kan återkomma när den fortfarande kvalificerar sig som ofta köpt.
- Filtreringen använder samma namnnormalisering som dubblettkontrollen.
- Sektionen visas inte som en tom eller irrelevant förslagsyta när inga förslag återstår.

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

[Fulltext B202–B208](history/backlog-before-doc-2b-20261024.md#bb-016--ofta-köpt-visar-varor-som-redan-finns-i-inköpslistan).

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

[Fulltext B265](history/backlog-before-doc-2b-20261024.md#bb-027--dashboardprofiler-synkronisering-och-avancerade-widgetlayouter).

### BB-036 – BigBrain – realtidssynkronisering av delad familjedata

- Prioritet: P1
- Status: Ny

Delad familjedata uppdateras inte automatiskt i redan öppen klient. Inköpslista är första konsument; ingen separat synkplattform beslutad.

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

[Fulltext B276–B279](history/backlog-before-doc-2b-20261024.md#bb-036--bigbrain--realtidssynkronisering-av-delad-familjedata).

## Media — öppet arbete och separat acceptans

### Mediekontroll

Merge `3375e83c871e5ce333238e4447de7e3edc143435` är accepterad. Daterad deployment finns enligt
[STATUS](STATUS.md#media), men separat fysisk acceptans av kort, undersidor, fokus, history/back/forward,
legacybokmärken och iPhone/PWA är inte styrkt. Den 17 oktober godkända integrerade sökningen är en
annan leverans. Kataloggränser 50 senaste biblioteksposter/25 discoveryposter består; whole-catalog
search och BB-155/156/157 är inte levererade genom navigationen.
[Fulltext B002](history/backlog-before-doc-2b-20261024.md#media--mediekontroll-review-gate-2026-10-17).

### BB-039 – Media Controller – serie-, säsongs- och avsnittsstatus

- Prioritet: P3
- Status: Ny

Framtida Media Controller ska använda normaliserade Media/Arr-kontrakt, inte Download Controls presentation state.

#### Definition of Done

- Släppta, lokalt tillgängliga, saknade och kommande avsnitt normaliseras från verifierade källor.
- Serie-, säsongs- och avsnittsnivå är konsekventa och testade.
- ”Sök saknade” har separat auktorisering, preview/audit och versionsverifierad Arr-adapter.
- Ingen direkt koppling till Download Control-UI eller rå providerpayload finns.

[Fulltext B100–B101](history/backlog-before-doc-2b-20261024.md#bb-039--media-controller--serie--säsongs--och-avsnittsstatus).

### BB-040 – Download Control – snabb navigation vid många färdiga nedladdningar

- Prioritet: P2
- Status: Pågår

Pågår: Sprint 3 levererade grupperad/kollapsad historik. Full kvalitativ mobil-, desktop-, tangentbords- och overflowverifiering kvarstår via BB-041; ingen stängning av full DoD.

#### Definition of Done

- Användaren behöver inte scrolla genom en lång lista med klara nedladdningar för att nå relevanta kontroller.
- Lösningen fungerar väl på mobil och desktop samt med tangentbord och skärmläsare.
- Aktiva och problematiska nedladdningar prioriteras visuellt.
- Färdiga nedladdningar är fortsatt enkla att hitta och komma åt.
- Urval, filtrering och befintlig batchhantering fungerar fortsatt utan ändrad mutationsgräns.
- Vald lösning har automatiska regressionstester och manuell responsiv verifiering.

[Fulltext B102–B103](history/backlog-before-doc-2b-20261024.md#bb-040--download-control--snabb-navigation-vid-många-färdiga-nedladdningar).

### BB-041 – Download Control – post-release UX-utvärdering efter Sprint 3

- Prioritet: P3
- Status: Ny

Kvalitativ post-release-uppföljning, inte en verifierad defekt eller ny implementation. Nytt förbättringsarbete kräver separat fynd/scope.

#### Definition of Done

- Produktägaren har använt den deployade UX:en under en representativ period.
- Statusgruppering, kompakt historik och navigation bedöms på mobil och desktop.
- Skillnaden och den avsiktliga överlappningen mellan Nedladdningskö och Medieflöde är begriplig.
- Eventuella verifierade fynd registreras som separata backloggposter; avsaknad av fynd dokumenteras också.
- BB-040 och BB-033 bedöms mot sina återstående DoD-punkter med daterad evidens.

[Fulltext B105–B106](history/backlog-before-doc-2b-20261024.md#bb-041--download-control--post-release-ux-utvärdering-efter-sprint-3).

### BB-033 – Media – tydliggör skillnaden mellan Nedladdningar och Mediajobb

- Prioritet: P2
- Status: Pågår

Pågår: Sprint 3 gav namnen Nedladdningskö/Medieflöde men full DoD kvarstår via BB-041. Download Control hanterar torrentlivscykeln och är den smala mutationsytan; Media Jobs är read-only och förklarar vägen från önskemål till bibliotek. Avsiktlig överlappning under nedladdning får inte förväxlas med dubbla ansvar.

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

[Fulltext B233–B236](history/backlog-before-doc-2b-20261024.md#bb-033--media--tydliggör-skillnaden-mellan-nedladdningar-och-mediajobb).

### BB-019 – BigBrain saknar säker borttagning av oönskad nedladdning

- Prioritet: P2
- Status: Pågår

BLOCKED för full destruktiv acceptans. Filbevarande borttagning har ägarverifiering; deleteFiles=true och riskerna för importerad media/Arr saknar full manuell verifiering.

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

[Fulltext B223–B227](history/backlog-before-doc-2b-20261024.md#bb-019--bigbrain-saknar-säker-borttagning-av-oönskad-nedladdning).

### BB-020 – Download Control – säker masshantering

- Prioritet: P1
- Status: Pågår

Pågår: Sprint 2:s pause/resume/retry-batch är separat accepterad delmängd. Destruktiv batch, rensning och retention är inte godkända genom den. Beroenden/relaterat: BB-023/024, BB-021, BB-022, BB-026; BB-025 är avvisad dubblett av BB-021.

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

[Fulltext B228–B232](history/backlog-before-doc-2b-20261024.md#bb-020--download-control--säker-masshantering).

### BB-021 – Download Control – koordinerad Sonarr/Radarr-recovery

- Modul: Media / Download Control
- Typ: Framtida funktion / Arr-orkestrering
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-04
- Beskrivning: Utred ett separat uttryckligt flöde för blocklist, köborttagning och ny sökning när Arr äger jobbet. MVP:n muterar endast qBittorrent.
- Definition of Done: Versionsverifierade Arr-kontrakt, separat preview/bekräftelse, idempotens, ingen dold sökning och end-to-end-test med ofarligt testjobb.

---

[Fulltext B238](history/backlog-before-doc-2b-20261024.md#bb-021--download-control--koordinerad-sonarrradarr-recovery).

### BB-022 – Download Control – säker rensning och retention för avslutade jobb

- Modul: Media / Download Control
- Typ: Framtida funktion / retention
- Prioritet: P3
- Status: Ny
- Upptäckt: 2026-08-04
- Beskrivning: Definiera en separat policy för avslutade jobb, importerad media och eventuell datarensning. MVP:n blockerar destruktiv borttagning av färdiga/importosäkra jobb.
- Definition of Done: Beslutad retentionpolicy, verifierat import- och hårdlänkskontrakt, säkra undantag, audit, rollbackstrategi och manuell verifiering.

---

[Fulltext B239](history/backlog-before-doc-2b-20261024.md#bb-022--download-control--säker-rensning-och-retention-för-avslutade-jobb).

### BB-023 – Download Control – Försök igen (Retry)

- Prioritet: P2
- Status: Pågår

BLOCKED för manuell slutverifiering i väntan på ett naturligt felande, säkert testobjekt. Implementation/test/deployment är daterad evidens; ingen ny defekt antas.

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

[Fulltext B240–B242](history/backlog-before-doc-2b-20261024.md#bb-023--download-control--försök-igen-retry).

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

[Fulltext B262](history/backlog-before-doc-2b-20261024.md#bb-013--persist-smart-shuffle-sessions).

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

[Fulltext B263](history/backlog-before-doc-2b-20261024.md#bb-014--smart-shuffle--manuell-tv-verifiering-och-mvp-härdning).

### BB-018 – Smart Shuffle – Jellyfins ”Nästa avsnitt” visas missvisande mellan serier

- Prioritet: P3
- Status: Ny

Jellyfins vanliga nästa-avsnitt-ruta kan vara missvisande när Smart Shuffle väljer annan serie. Bevara normal Jellyfin-uppspelning; inget odokumenterat klienthack.

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

[Fulltext B217–B222](history/backlog-before-doc-2b-20261024.md#bb-018--smart-shuffle--jellyfins-nästa-avsnitt-visas-missvisande-mellan-serier).

## UX, metadata och osäker manuell acceptans

**U2: UNCERTAIN där ägarbeskedets omfattning inte kan styrkas.** Teknisk completion/deployment
är inte fysisk acceptans. BB-096–099:s migrerade moduler och senare BB-107–114 har överlappande
acceptanspunkter. Ingen generell stängning. Fullspelare/primär playback, Library/sökning, artwork,
long-title-kort, routes/fokus/scroll och sammanlagd tillgänglighet kräver respektive scope-evidens.
BB-097 Family är accepterad riktning, inte automatiskt varje senare detalj/remediation.
BB-117:s specifika timer/Detail Hero och BB-128B/C:s resilience/korrigerade Finance-visualisering
är godkända bara inom sina egna gränser. Döljandet av inbyggd ljudboks-UI godkänner inte dess design.

**Avvisat:** persistent floating mini-player, tiny white clock trigger och broad/form-like timer.
Navigation-surviving AppShell playback-livslängd behålls separat; inget globalt ersättningsoverlay.
Bevara kompakt tvåkolumns mobil-Detail Hero med back-navigation ovanför, cover vänster och titel/
författare/serie höger. BB-116A UX/UI Lab och Design System v1 saknar samlad owner approval;
komponenter får inte märkas OWNER GODKÄND utan explicit besked. BB-112:s framtida fleranvändar-
playbackidentitet måste bindas till autentiserad användare; serviceidentitet är inte owner progress.
Inga tokens i browser, ingen tyst dataradering när presentation-only historik döljs.

Äldre osäkra krav finns samlat i följande exakta källor, behålls öppna och får inte tolkas som
stängda vid en framtida produktbedömning:

| Scope | Kvarstående verifiering / original |
| --- | --- |
| BB-114 — Audiobook Detail Polish, Sleep Timer & Floating Player Rejection | UNCERTAIN: fysisk/visuell scopeacceptans; [Fulltext B067](history/backlog-before-doc-2b-20261024.md#bb-114--audiobook-detail-polish-sleep-timer--floating-player-rejection). |
| BB-113 — Audiobook Player Affordance, Detail Layout & Artwork Remediation | UNCERTAIN: fysisk/visuell scopeacceptans; [Fulltext B068](history/backlog-before-doc-2b-20261024.md#bb-113--audiobook-player-affordance-detail-layout--artwork-remediation). |
| BB-112 — Native Audiobook Playback + Owner Search/Result UX Remediation | UNCERTAIN: fysisk/visuell scopeacceptans; [Fulltext B069](history/backlog-before-doc-2b-20261024.md#bb-112--native-audiobook-playback--owner-searchresult-ux-remediation). |
| BB-111 — Audiobook Route/Detail UX Fixes & Native Playback Vertical Slice | UNCERTAIN: fysisk/visuell scopeacceptans; [Fulltext B070](history/backlog-before-doc-2b-20261024.md#bb-111--audiobook-routedetail-ux-fixes--native-playback-vertical-slice). |
| BB-110 — Audiobook Owner UX Consolidation & Native Playback Foundation | UNCERTAIN: fysisk/visuell scopeacceptans; [Fulltext B071](history/backlog-before-doc-2b-20261024.md#bb-110--audiobook-owner-ux-consolidation--native-playback-foundation). |
| BB-109 — Audiobook Owner UX Remediation & Native Playback Investigation | UNCERTAIN: fysisk/visuell scopeacceptans; [Fulltext B072](history/backlog-before-doc-2b-20261024.md#bb-109--audiobook-owner-ux-remediation--native-playback-investigation). |
| BB-108 — Audiobook Navigation UX Experiment | UNCERTAIN: fysisk/visuell scopeacceptans; [Fulltext B073](history/backlog-before-doc-2b-20261024.md#bb-108--audiobook-navigation-ux-experiment). |
| BB-107 — Owner UX Remediation after BB-106 | UNCERTAIN: fysisk/visuell scopeacceptans; [Fulltext B074](history/backlog-before-doc-2b-20261024.md#bb-107--owner-ux-remediation-after-bb-106). |
| BB-096 — BigBrain UX Sprint v1 / Unified Design System | UNCERTAIN: fysisk/visuell scopeacceptans; [Fulltext B286](history/backlog-before-doc-2b-20261024.md#bb-096--bigbrain-ux-sprint-v1--unified-design-system). |
| BB-097 — Premium UX Reference: Family / Obsidian Gold | UNCERTAIN: fysisk/visuell scopeacceptans; [Fulltext B287](history/backlog-before-doc-2b-20261024.md#bb-097--premium-ux-reference-family--obsidian-gold). |
| BB-098 — Global Premium UI Rollout | UNCERTAIN: fysisk/visuell scopeacceptans; [Fulltext B288](history/backlog-before-doc-2b-20261024.md#bb-098--global-premium-ui-rollout). |
| BB-099 — Whole-App UX Audit & Premium Composition Pass | UNCERTAIN: fysisk/visuell scopeacceptans; [Fulltext B289](history/backlog-before-doc-2b-20261024.md#bb-099--whole-app-ux-audit--premium-composition-pass). |

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

[Fulltext B264](history/backlog-before-doc-2b-20261024.md#bb-015--design-system-v1--manuell-visuell-och-tizen-verifiering).

### BB-116A — Admin UX/UI Lab Foundation & Production Component Inventory

- Status 2026-08-31: **IMPLEMENTED / AUTOMATICALLY AND RUNTIME VERIFIED / DEPLOYED / CI VERIFIED / PHYSICAL OWNER REVIEW PENDING**.
- Definition of Done: permanent Admin route, stable real-component inventory, safe specimens, review vocabulary, shared sleep-timer candidate, focused/full Web tests, production build, viewport QA, canonical report, deployment and CI evidence.
- BigBrain Design System v1 remains **OWNER REVIEW IN PROGRESS**. No component may become `OWNER GODKÄND` without explicit owner feedback.
- Deferred: production audiobook grid migration; search/discovery candidate; BB-157; broad consolidation of local checkbox, navigation, dialog and domain-card markup.

[Fulltext B060](history/backlog-before-doc-2b-20261024.md#bb-116a--admin-uxui-lab-foundation--production-component-inventory).

### BB-157 — Media Release Availability Search

- Status: **PLANNED / BACKLOG ONLY**. Preserved by BB-117; not implemented.
- When indexers actually provide it, expose truthful seeder count and allow sorting by seeders; never invent missing counts. Future filters may combine language and genre/category, for example Swedish children's audiobooks ordered by seeders.
- Keep release/indexer availability distinct from BB-155 canonical book metadata. Measure provider capability and semantics before designing or implementing filters.

[Fulltext B061](history/backlog-before-doc-2b-20261024.md#bb-157--media-release-availability-search).

### BB-156 — Module Runtime Latency & Reliability

- Status: **PLANNED / BACKLOG ONLY**. Preserved by BB-117; not implemented.
- Instrument and diagnose end-to-end module load latency and reliability, separating Web, API, module, database and external-service delays. Measure before optimization instead of blindly increasing timeouts.
- Investigate slow endpoints, blocking dependencies, sequential calls, container behavior, dashboard aggregation, timeout classification and partial/widget-level loading so one slow dependency does not unnecessarily block an entire module.

[Fulltext B064](history/backlog-before-doc-2b-20261024.md#bb-156--module-runtime-latency--reliability).

### BB-155 — Canonical Book Metadata Resolver

- Status: **PLANNED / BACKLOG ONLY**. Preserved by BB-117; not implemented and no provider selected.
- Start with Audiobooks only. Audiobookshelf remains authoritative for local media, playback, sessions, duration and progress. BigBrain may later build a cached canonical normalized audiobook metadata record, with ISBN/ISBN-13 as the strongest external identity when available.
- Candidate enrichment fields: title, subtitle, author, series/number, language, publication year, publisher, description, ISBN, subjects/genres and high-quality cover artwork. Books without ISBN may use cautious title + author + series + language matching with explicit confidence thresholds.
- Uncertain matches must never silently overwrite trustworthy data. Retain field-level provenance where appropriate and cache resolved data rather than fetching on every page view.
- Before implementation, investigate provider/API terms, rate limits, artwork/image rights, licensing and cache conditions. Do not prematurely create a generic all-Media metadata platform.

[Fulltext B066](history/backlog-before-doc-2b-20261024.md#bb-155--canonical-book-metadata-resolver).

### BigBrain-wide design system and less-is-more hierarchy

- Module: BigBrain Web; status: future UX/consolidation candidate, not approved for implementation.
- Intent: evolve the existing token/theme foundation into consistent typography, text hierarchy,
  controls, surfaces, spacing, radii, icons, status and interaction states, responsive behavior and
  accessible focus states. `Obsidian Gold` is the product owner's preferred candidate direction,
  with a dark foundation and restrained gold for focus/action/emphasis rather than decoration; it
  is not an approved final visual specification.
- Product principle: the interface should not explain what is already self-evident. Prefer one
  heading per real information level, data/content first, short contextual labels and progressive
  disclosure of secondary or technical evidence. Apply this BigBrain-wide; do not redesign Finance
  in this item.
- Definition of Done: an owner-approved cross-module specification, accessible component/token
  contracts and responsive/manual verification criteria exist before any rollout.

[Fulltext B094](history/backlog-before-doc-2b-20261024.md#bigbrain-wide-design-system-and-less-is-more-hierarchy).

### Passwordless authentication, trusted devices and step-up investigation

- Module: BigBrain-wide Identity/Security; status: future consolidation/security investigation.
- Intent: evaluate standards-based WebAuthn/passkeys, passwordless and trusted-device enrollment,
  naming, management, revocation, recovery/bootstrap and lost-device handling, platform
  authenticators and cross-device/phone-assisted QR or proximity authentication.
- Security boundary: MAC addresses are not authentication identities. BigBrain never receives or
  stores Face ID, Touch ID or equivalent biometric data; local biometrics may only unlock a device's
  cryptographic credential. No custom biometric protocol is permitted.
- Authorization: trusted low-risk reads should minimize friction, while sensitive administration,
  device enrollment/removal and any separately approved future high-risk Finance operation may
  require fresh step-up authentication. A dashboard session grants no Finance execution authority.
- Scope: investigate device/use roles such as owner or adult personal device, shared family tablet,
  kitchen/kiosk display and service device without hardcoding family members. Tailscale/device
  identity or an equivalent private-network layer may complement, but never replace, application
  user identity and authorization.
- Roadmap: place this in the future BigBrain consolidation/security and authentication hardening
  phase according to the current roadmap; do not change the evidenced pentest or Finance ordering.

[Fulltext B095](history/backlog-before-doc-2b-20261024.md#passwordless-authentication-trusted-devices-and-step-up-investigation).

## Dokumentationsskuld och osäker disposition

Ingen av följande poster stängs av DOC-2B. BB-004:s separata incidentarkiv är inte detta uppdrag;
BB-012:s förslag ger inte rätt att införa rutinmässiga granskningsloopar eller ny automation.

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

[Fulltext B253](history/backlog-before-doc-2b-20261024.md#bb-004--arkivera-arr-incidentens-mellanrapporter).

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

[Fulltext B257](history/backlog-before-doc-2b-20261024.md#bb-008--aktivera-eller-avveckla-changelog-policy).

### BB-011 – Konsolidera STABILIZATION_PLAN

- Modul: Produktstyrning
- Typ: Dokumentationsskuld
- Prioritet: P2
- Status: UNCERTAIN (U1); tidigare Ny, inte stängd
- Upptäckt: 2026-08-03
- Beskrivning: Separera roadmap, teknisk skuld och verifierade buggar.
- Motiv: En plan ska inte fungera som osorterad felinkorg.
- Avgränsning: Inget stängs eller flyttas utan evidens och review.
- Risk: Prioriteringar och problemstatus misstolkas.
- Definition of Done: Varje punkt har rätt hemvist, ägare och status.
- Relaterade dokument: `STABILIZATION_PLAN.md`, `ROADMAP.md`, `docs/BACKLOG.md`.

[Fulltext B260](history/backlog-before-doc-2b-20261024.md#bb-011--konsolidera-stabilization_plan).

STABILIZATION_PLAN.md saknas på main och får inte återskapas som parallell roadmap.
Separat dispositionsbevis för samtliga kvarstående krav behövs innan stängning.

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

[Fulltext B261](history/backlog-before-doc-2b-20261024.md#bb-012--införa-återkommande-baseline-review).

## Historik och bevarande

Alla 290 ursprungsavsnitt, även tidigare Klar/Avvisad, finns i
[Backlog-arkivet](history/backlog-before-doc-2b-20261024.md). Inga BB-ID:n eller DoD raderas ur historiken.
[DOC-2B:s destinationsmappning](reports/documentation/doc-2b-backlog-roadmap-inventory-20261024.md#fas-b--förlustfri-konsolidering-2026-10-24)
visar källpositioner, arkivdestinationer, U1–U3 och synliga K1–K7-krav. Kanoniska kontrakt och
frysningsbeslut har oförändrad auktoritet; arkivets current/next är inte aktuella körorder.
