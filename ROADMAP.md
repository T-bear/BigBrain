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

The next M2 milestone is **FREE HISTORICAL DATA INGESTION preparation/research** (BB-072):
compare zero-cost sources on license, local retention, backtesting rights, coverage,
corporate actions, symbol history and quality. Research does not authorize activation;
BB-071 evidence remains mandatory before any real-data ingestion or storage.
