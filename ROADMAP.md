# BigBrain Roadmap

**BB-083 APPLIANCE RESILIENCE BASELINE** pauses the next Finance slice. Lifecycle/recovery,
clean/unclean journal, storage/clock/disk gates, crash-safe Finance requests, recovery API/UI,
Compose readiness/grace and systemd artifacts are implemented. Container crash passed without
request/data loss. Host reboot and physical power-cycle remain gates; not PAPER.

Product work is tracked in [BACKLOG](docs/BACKLOG.md); current verified reality is in
[STATUS](docs/STATUS.md). This file points to dedicated canonical roadmaps rather than
duplicating their detailed gates.

## Finance epic

The canonical Finance delivery sequence is the
[Finance master roadmap](docs/architecture/finance/master-roadmap.md). Finance is in
RESEARCH with M0 and M1 complete. BB-046 provider research and ADR 0021 owner review are
complete. BB-077/078 passed the EODHD Free authorization and activation gates for M2 daily
EOD ingestion; persistent real provider data, replay and read-only API/UI are deployed.
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
