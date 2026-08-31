# BigBrain Roadmap

## BB-089 – M5 Hard Risk Engine foundation (implemented 2026-08-16)

Finance now has a mandatory deterministic server-side risk authority for new RESEARCH/shadow
proposals. Policy `research-eod-v1`, immutable evaluations, fail-closed EOD freshness/lineage/
health/volatility/liquidity/exposure rules and durable audited halts are implemented. Daily-loss,
rolling-drawdown and consecutive-loss breakers are proven with deterministic simulation. Spread,
sector and portfolio aggregation remain explicitly not evaluable. Next gate: Security/Penetration
Testing Baseline while prospective evidence accumulates; not PAPER.

## BB-088 – Prospective daily cadence and Finance UI v1.0 hierarchy (implemented 2026-08-15)

Finance now resumes a bounded current-EOD cycle automatically after recovery: internal checks run
every 30 minutes, provider access is limited to eligible weekdays after 22:00 UTC and existing
adapter retries remain capped with exponential backoff. Genuine predictions mature exactly once;
missing historical decisions are never invented. Finance UI v1.0 now follows the approved
`Market today → BigBrain now → Prospective result → Details & research` hierarchy and durable UX
principle `SUMMARY → EXPLANATION → RAW EVIDENCE`. Multi-real-session proof must accumulate over
future sessions. Next priority is continued prospective evidence observation, followed by M5 Hard
Risk Engine when enough operational evidence exists; security remains a gate before execution.

## BB-087 – Prospective shadow observation foundation (implemented 2026-08-15)

Current canonical EODHD observations can produce deterministic, exactly-once `RESEARCH` predictions after recovery and clock gates. Predictions pin their knowledge cutoff, market/feature/strategy/parameter lineage and next-source-session horizon; outcomes append later. Historical backfill is barred from prospective evidence and UI/API scorecards state sample limitations. Next: mature daily current-EOD cadence and outcome evaluation evidence. Hard Risk Engine and the approved security/pentest baseline remain gates before any PAPER eligibility discussion; LIVE additionally requires separate owner authorization.

## BB-086 – Legitimate zero-cost ETF history (fail-closed 2026-08-15)

Eight bounded candidates were evaluated for SPY/QQQ/IWM. WIKI contains none; Stooq still returns
a browser proof-of-work control; CC0/CC BY/MIT mirrors do not establish rights to their underlying
Yahoo, PiTrading or otherwise undocumented exchange data. No candidate was acquired or promoted,
so BB-084/085 canonical evidence is unchanged. Historical bootstrap remains incomplete for ETF
cross-asset evidence; the exact next Finance slice is prospective read-only shadow observation,
while legitimate ETF-source discovery becomes opportunistic rather than the critical path.

## Security and penetration-testing gate – PLANNED

BigBrain requires continuous automated security checks plus controlled black-/grey-box penetration
testing across Web, API, auth/access control, containers/Sentinel, Finance untrusted-data and
mode-gate surfaces. Potentially destructive tests use isolated environments/test data. This
baseline is mandatory before real-money LIVE eligibility and a strong gate before meaningful
PAPER/execution authority. BB-086 records the approved milestone only; it does not implement it.

## BB-085 – Provider-tagged Finance data protection (verified 2026-08-15)

WIKI public-domain memory now has deterministic atomic backup manifests, SHA-256 verification,
isolated restore/corruption drills and exact derived lineage. EODHD is excluded from indefinite
backup and retains subscription deletion semantics. Rejected quarantine payload cleanup keeps
audit manifests and cannot address canonical data. The next recommended slice is BB-086:
legitimate zero-cost historical coverage for SPY/QQQ/IWM, still read-only RESEARCH.

## BB-084 – Historical dataset intake (deployed 2026-08-15)

Quarantine, immutable manifests, deterministic validation/promotion, WIKI bounded promotion,
Zenodo manual-review classification and source-specific feature/backtest/robustness lineage are
implemented. BB-085 subsequently completed provider-tagged backup/restore and cleanup drills.

**BB-083 APPLIANCE RESILIENCE BASELINE** pauses the next Finance slice. Lifecycle/recovery,
clean/unclean journal, storage/clock/disk gates, crash-safe Finance requests, recovery API/UI,
Compose readiness/grace and systemd artifacts are implemented. Container crash passed without
request/data loss. Host reboot and physical power-cycle remain gates; not PAPER.

Product work is tracked in [BACKLOG](docs/BACKLOG.md); current verified reality is in
[STATUS](docs/STATUS.md). This file points to dedicated canonical roadmaps rather than
duplicating their detailed gates.

## Future Family View & Family Coordination epic (planned only)

The [Family epic](docs/architecture/family-view-family-coordination.md) consolidates existing
school-meal, school-aware Meal Planner and Home/Calendar planning with future member, school
schedule/calendar, daily/weekly context and generic conflict semantics. It is not an active sprint
and creates no implementation order. BB-091 remains complete and the current Finance direction
toward `FINANCE AUTONOMOUS RESEARCH v1` retains priority; Family planning does not start Finance or
Family implementation.

## Finance epic

**Current BB-118 baseline (2026-08-31):** Finance is deployed `RESEARCH / 0 SEK / NONE` with real EODHD memory, immutable features, deterministic backtests/robustness, macro context, prospective shadow/research-risk evidence and commissioned scheduler/governor/operations foundations. It has no broker, orders, PAPER, LIVE or AUTO. Scheduler is enabled but currently not running; a readiness contradiction (`universeIncomplete` 0/8 versus operations `READY`) and unavailable governor metrics must be reconciled before the next implementation decision. Scientific evidence strengthening is the recommended candidate; continuing execution-grade BB-053 risk foundation is the alternative. Neither is started here.

The canonical Finance delivery sequence is the
[Finance master roadmap](docs/architecture/finance/master-roadmap.md). Finance remains
RESEARCH; M0/M1 and the bounded BB-077–095 research chain are delivered. BB-046 provider
research and ADR 0021 owner review are complete. BB-077/078 passed the EODHD Free
authorization and activation gates for M2 daily EOD ingestion; persistent real provider
data, replay and read-only API/UI are deployed.
PAPER execution, live connectivity and real-money authority are not implemented.
Provider-neutral policy/provenance, canonical identity, normalization, market-session/gap
semantics and deterministic synthetic replay may be built before BB-071. Those BB-045
foundations, inklusive immutable correction/supersession assembly, are implemented and verified;
their former real-ingestion block was superseded by BB-077/078 for EODHD Free.

**FREE HISTORICAL DATA INGESTION preparation/research** (BB-072) is a completed historical gate. Ten
free/free-adjacent source products were compared; none passed the complete durable
retention and personal non-display backtesting gate. EODHD Free Starter is the best
conditional evaluation lead and Twelve Data Basic the Nordic technical lead, but neither
is authorized. The synthetic acquisition contract, entitlement gate, journal and fixture
pipeline are implemented and verified without external IO. The next synthetic slice has
now added an immutable manifest/persistence contract and measured JSONL versus SQLite at up
to 1,260,000 fixture rows. The provisional direction is immutable payload files plus a
transactional SQLite catalog/index; it is evidence, not a production storage selection or
activation. Its pre-ingestion block was later passed for EODHD Free by BB-077/078; the
synthetic benchmark remains historical evidence rather than the current storage state.

**LIVE MARKET OBSERVATION / SHADOW LEARNING preparation** (BB-073) now has a verified
synthetic foundation: explicit event/provider/received/knowledge time, honest freshness,
deterministic outage/gap/correction feed, immutable versioned predictions, later outcomes
and prospective metrics with no broker/order path. The dated free-provider research is now
superseded for Twelve Data by human evidence requiring a paid Personal plan. Its former
provider-selection block was later superseded for EODHD Free by BB-077/078.
The earlier **STATE B — HUMAN CONFIRMATION REQUIRED** is resolved for Twelve Data Personal:
direct human evidence supports the submitted personal storage/research/retention scope,
including post-termination derived and audit retention. Basic/free is expressly insufficient,
so Twelve Data is a paid fallback rather than the free lead. The recorded next action was an
Alpaca Basic/free IEX entitlement inquiry and zero-cost comparison, not an adapter. That
historical gate was subsequently passed by the explicitly authorized EODHD Free activation.

**ZERO-COST REAL MARKET DATA ACTIVATION** (BB-075) records a historical fail-closed gate. The external
market-data budget is exactly 0 SEK. A fresh first-party sweep found no exact free product
with complete automation, local-retention, replay/backtest and artifact-lifecycle rights.
No adapter, account, key, payload, real memory or deployment activation was created in that
slice. This status was superseded by BB-077/078 for EODHD Free.

**BB-076 PRAGMATIC ZERO-COST PERSONAL-RESEARCH ACTIVATION** ändrar evidenssemantiken men
inte säkerhetsgränsen. Ägaraccepterad personlig forskning kan beslutas per capability för
legitima 0-SEK-källor utan identifierat förbud. Stooq nådde denna evidensklass för avgränsad
daily history, men den offentliga CSV-ytan svarade med en JavaScript-verifieringskontroll.
BigBrain kringgår inte kontrollen; i den slicen aktiverades ingen adapter, data eller deployment.

**BB-077 EODHD FREE ACTIVATION** är implementerad till credential-gränsen. Nuvarande tier
heter `Free`: €0, 20 anrop/dag och ett års EOD. Adapter, SQLite/content-addressed memory,
revision/replay, API/UI och preview-confirm-delete-livscykel finns och är disabled by default.
**BB-078 FIRST REAL MARKET DATA** passerade credential-gränsen 2026-08-11. Åtta av åtta
watchlist-anrop lyckades utan retry och gav 2 008 EOD-observationer, åtta payloads/revisioner
och ett beständigt lokalt minne för 2025-08-11–2026-08-10. Exakt-revision replay, restart,
daglig skip/idempotens, API och responsiv UI är runtime-verifierade. Nästa säkra slice är
indikator-/feature-grunden över den frysta reala revisionen; live/near-live förblir separat.
**BB-079 FIRST REAL FEATURE / INDICATOR ENGINE** levererar nu `core-daily-v1`: 42 168
immutable, revisionsbundna feature-värden över de åtta reala marknadsrevisionerna. Formel-
korrekthet, idempotent checksumma, explicit warmup, causal no-lookahead, SQLite-restart,
retention lineage samt API/UI är verifierade och deployade. Nästa säkra gate är en minimal
M3 research-backtest som binder exakt market revision + feature revision; ingen PAPER/trading.
**BB-080 FIRST DETERMINISTIC REAL-DATA BACKTEST ENGINE** levererar nu M3:s första offline researchmotor med exact revision pinning, buy-and-hold/SMA10-20/momentum20, next-open fills, whole-share portfolio, explicita zero/conservative costs, immutable journal/result/checksum, metrics/curves och read-only API/UI. BB-078/079-runs är deterministiskt restart-verifierade. Nästa säkra gate är BB-081 robustness/out-of-sample; inte PAPER.
**BB-081 ROBUSTNESS / OUT-OF-SAMPLE FOUNDATION** levererar immutable 70/30-evidens med 50-sessioners embargo, fixed expanding walk-forward, bounded parameter- och cost-sensitivity samt transparent versionerad score. Den korta reala testdelen ger korrekt `INSUFFICIENT_DATA` för alla tre referensstrategier. Nästa säkra steg är längre/andra rättighetsklarerade historiska minnen eller corporate-action-adjusterad lineage; inte PAPER.
**BB-082 LONGER ZERO-COST HISTORICAL MARKET MEMORY** genomförde 2026-08-12 den aktuella
provider-/entitlementgrinden men stoppade legitimt före ingestion. Stooqs offentliga
download kräver en JavaScript-verifiering som inte får kringgås; EODHD Free ger inte längre
historik och de relevanta Alpha Vantage/Nasdaq Data Link-produkterna är betalda. Nästa säkra
Finance-slice är en normalt stödd Stooq bulk/API-väg med klar retention, eller en namngiven
verifierbart öppen historisk filkälla. `INSUFFICIENT_DATA` kvarstår; inte PAPER.

**READ-ONLY MARKET OBSERVATION UI FOUNDATION** (BB-074) is implemented as an early M2
research surface: provider-neutral fail-closed snapshot, research watchlist, honest empty/
synthetic/stale/gap states, memory summary and accessible lightweight chart. This does not
start M8; portfolio, positions, orders, risk, broker and execution workflows remain planned.
