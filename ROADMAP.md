# BigBrain Roadmap

Product work is tracked in [BACKLOG](docs/BACKLOG.md); current verified reality is in
[STATUS](docs/STATUS.md). This file points to dedicated canonical roadmaps rather than
duplicating their detailed gates.

## Finance epic

The canonical Finance delivery sequence is the
[Finance master roadmap](docs/architecture/finance/master-roadmap.md). Finance is in
RESEARCH with M0 and M1 complete. BB-046 provider research and ADR 0021 owner review are
complete, but M2 ingestion is blocked by BB-071's written retention entitlement.
External data, PAPER execution, live connectivity and real-money authority are not implemented.
Provider-neutral policy/provenance, canonical identity, normalization, market-session/gap
semantics and deterministic synthetic replay may be built before BB-071. Those BB-045
foundations, inklusive immutable correction/supersession assembly, are implemented and verified;
actual provider
ingestion or durable provider data remains blocked.

**FREE HISTORICAL DATA INGESTION preparation/research** (BB-072) is complete. Ten
free/free-adjacent source products were compared; none passed the complete durable
retention and personal non-display backtesting gate. EODHD Free Starter is the best
conditional evaluation lead and Twelve Data Basic the Nordic technical lead, but neither
is authorized. The synthetic acquisition contract, entitlement gate, journal and fixture
pipeline are implemented and verified without external IO. The next synthetic slice has
now added an immutable manifest/persistence contract and measured JSONL versus SQLite at up
to 1,260,000 fixture rows. The provisional direction is immutable payload files plus a
transactional SQLite catalog/index; it is evidence, not a production storage selection or
activation. Next is exact written entitlement evidence or, while blocked, a bounded
synthetic local-memory prototype/backup-and-restore validation. BB-071 remains mandatory
before real ingestion or provider-data storage.
