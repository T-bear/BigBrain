# Project Report Catalog

- `BB-110` — [Audiobook Owner UX Consolidation & Native Playback Foundation](features/media/bb-110-audiobook-ux-consolidation-20260828.md)
- `BB-109` — [Audiobook Owner UX Remediation & Native Playback Investigation](features/media/bb-109-audiobook-owner-ux-remediation-20260828.md)

- `BB-108` — [Audiobook Navigation UX Experiment](features/media/bb-108-audiobook-navigation-ux-experiment-20260828.md)

- `BB-107` — [Owner UX Remediation after BB-106](features/design-system/bb-107-owner-ux-remediation-20260827.md)
- `BB-106` — [Consolidated UX / Quality Fix Sprint](features/design-system/bb-106-consolidated-ux-quality-sprint-20260827.md)

- `BB-105` — [AudioBookBay Parser & Literal Author Search Remediation](features/media/bb-105-audiobookbay-parser-remediation-20260824.md)
- `BB-104` — [Universal Metadata-Aware Audiobook Search](features/media/bb-104-universal-metadata-search-gate-20260824.md)
- `BB-103` — [First Usable Audiobook Flow](features/media/bb-103-first-usable-audiobook-flow-20260824.md)
- `BB-102` — [Librarr Provider Security Gate](features/media/bb-102-librarr-provider-security-gate-20260823.md)
- `BB-101` — [Audiobook Acquisition Foundation](features/media/bb-101-audiobook-acquisition-foundation-20260823.md)
- `BB-100` — [Audiobooks Platform Foundation](features/media/bb-100-audiobooks-platform-foundation-20260823.md)

- `BB-096` — [BigBrain UX Sprint v1 / Unified Design System](features/dashboard/bb-096-unified-design-system-20260822.md)
- `BB-096 deployment` — [UX deployment / commissioning](deployments/bb-096-ux-commissioning-20260823.md)

- `BB-092` — [Finance Autonomous Research v1 foundation](features/finance/finance-bb-092-autonomous-research-foundation-20260822.md)
- `BB-093` — [Finance Research Scheduler / Orchestrator v1](features/finance/finance-bb-093-research-scheduler-20260822.md)
- `BB-088` — [prospective daily cadence and Finance UI v1.0](features/finance/finance-bb-088-daily-cadence-ui-v1-20260815.md)
- `BB-085` — [provider-tagged backup, restore and quarantine cleanup](features/finance/finance-bb-085-provider-tagged-backup-restore-cleanup-20260815.md)
- `BB-084` — [dataset research](features/finance/finance-bb-084-dataset-research-20260815.md)
  and [implementation/runtime](features/finance/finance-bb-084-implementation-runtime-20260815.md)

| Area | Evidence | Artifact | Status |
| --- | --- | --- | --- |
| Finance BB-089 Hard Risk Engine | Versioned policy, immutable evaluations, fail-closed rules, durable halt and bypass tests | [BB-089 report](features/finance/finance-bb-089-hard-risk-engine-foundation-20260816.md) | Foundation implemented; runtime/CI status in report |
| BB-083 appliance resilience | Sanitized host/runtime/crash evidence | [Report](features/resilience/bigbrain-bb-083-appliance-resilience-20260812.md) | Compose deployed; host/reboot/physical gates pending |

| Area | Local evidence classification | Published artifact | Status |
| --- | --- | --- | --- |
| Dashboard Phase 1 implementation | Sanitized full report | [Implementation](features/dashboard/dashboard-views-widget-framework-phase1-20260804.md) | Published |
| Dashboard Phase 1 deployment | Sanitized full report | [Deployment](features/dashboard/dashboard-views-widget-framework-phase1-deployment-20260804.md) | Published |
| Sprint 1 UX bugfix deployment | Sanitized deployment evidence | [Deployment](features/sprint-1/sprint-1-bugfix-deployment-20260807.md) | Completed and manually verified |
| Sprint 1 deployment regression | Sanitized root cause and remediation evidence | [Incident](incidents/sprint-1-deployment-regression-20260807.md) | Resolved and manually verified |
| Calendar and Heroma import MVP | Sanitized implementation evidence | [Calendar MVP](features/calendar/calendar-heroma-import-mvp-20260805-162245.md) | Implemented, deployed and manually verified |
| Smart Shuffle MVP and Tizen playback | Catalog metadata only; full reports local | [STATUS](../STATUS.md#smart-shuffle) and [Media](../modules/media.md#smart-shuffle-mvp) | Local full evidence retained |
| Download Control MVP | Catalog metadata only; full reports local | [STATUS](../STATUS.md#download-control) and [runbook](../operations/runbooks/download-control-safe-removal.md) | Local full evidence retained |
| Download Control Sprint 2 | Sanitized implementation and closure report | [Sprint 2 closure](features/download-control/sprint-2-download-operations-20260809.md) | Closed and deployment accepted; Retry manual verification pending, no known defect |
| Download Control Sprint 3 | Sanitized implementation, deployment and closure report | [Sprint 3 navigation](features/download-control/sprint-3-download-navigation-20260810.md) | Closed; technical verification passed, extended UX evaluation deferred to BB-041 |
| Obsidian Gold | Catalog metadata only; full reports local | [STATUS](../STATUS.md#designsystem-och-teman) and [theme contract](../design-system/theme-contract-v1.md) | Local full evidence retained |
| Early ARR recovery and diagnostics | Local only or superseded | [History](../history/early-sprints.md) | Not published as raw reports |
| Repository consolidation | Sanitized full report | [Publication report](documentation/repository-consolidation-and-documentation-governance-20260804-214358.md) | Published |
| Product/UX/auth/school-meals backlog capture | Sanitized planning record | [Planning record](documentation/product-ux-auth-school-meals-backlog-capture-20260817.md) | Planned only; no implementation or priority change |
| Finance policy-governed trading epic | Sanitized planning baseline | [Finance epic planning](features/finance/finance-epic-planning-20260810.md) | M0 complete; runtime and trading not implemented |
| Finance Sprint 1 foundation | Sanitized implementation evidence | [Sprint 1 foundation](features/finance/finance-sprint-1-foundation-20260810.md) | M1 domain/evidence foundation automatically verified; not deployed |
| Finance Sprint 2 market-data research | Sanitized provider/licensing research | [Sprint 2 research](features/finance/finance-sprint-2-market-data-research-20260810.md) | BB-046 complete; provider activation blocked by BB-071 |
| Finance BB-071 retention gate review | Sanitized public-terms and owner-review evidence | [BB-071 review](features/finance/finance-bb-071-retention-gate-review-20260810.md) | Historical waiting state; provider path later resolved by BB-077/078 |
| Finance market-data memory foundation | Sanitized retention, provenance and learning architecture | [Memory foundation](features/finance/finance-market-data-memory-foundation-20260810.md) | Historical architecture-only state; provider/runtime later delivered by BB-077/078 |
| Finance BB-045 policy/provenance foundation | Sanitized implementation and automated-test evidence | [Policy/provenance foundation](features/finance/finance-market-data-policy-provenance-foundation-20260810.md) | Provider-neutral slice verified; former BB-071 block resolved by BB-077/078 |
| Finance BB-045 instrument/normalization foundation | Sanitized implementation and automated-test evidence | [Instrument/normalization foundation](features/finance/finance-instrument-identity-normalization-foundation-20260810.md) | Canonical synthetic slice verified; superseded next-step note resolved by later reports |
| Finance BB-045 market-session/replay foundation | Sanitized implementation and automated-test evidence | [Session/replay foundation](features/finance/finance-market-session-replay-foundation-20260811.md) | Timezone/gap/replay synthetic slice verified; correction foundation now separately verified |
| Finance BB-045 immutable revision assembly | Sanitized implementation and automated-test evidence | [Revision assembly](features/finance/finance-immutable-dataset-revision-assembly-20260811.md) | Historical synthetic slice verified; former provider/persistence gap resolved by BB-077/078 |
| Finance BB-072 free historical source research | Sanitized dated first-party source/rights comparison | [Free historical source research](features/finance/free-historical-data-source-research-20260811.md) | Historical do-not-ingest gate; superseded for EODHD Free by BB-077/078 |
| Finance BB-045 synthetic acquisition foundation | Sanitized implementation and automated-test evidence | [Synthetic acquisition foundation](features/finance/finance-synthetic-acquisition-foundation-20260811.md) | Historical fixture-only foundation; former real-data block resolved by BB-077/078 |
| Finance BB-045 synthetic persistence benchmark | Sanitized manifest, contract and measured JSONL/SQLite evidence | [Synthetic persistence benchmark](features/finance/finance-synthetic-persistence-benchmark-20260811.md) | Historical fixture-only benchmark; production persistence later delivered by BB-077/078 |
| Finance BB-073 free live/current source research | Dated first-party product, freshness and entitlement comparison | [Free live research](features/finance/free-live-market-data-source-research-20260811.md) | Historical Basic ranking and no-provider state superseded by BB-077/078 |
| Finance BB-073 live observation/shadow learning | Sanitized implementation and automated-test evidence | [Live observation foundation](features/finance/finance-live-observation-shadow-learning-20260811.md) | Fixture-only four-clock feed and immutable prospective evidence; no broker/order/runtime |
| Finance free-first historical + live provider gate | Dated combined provider, entitlement, capacity and architecture evidence | [Provider evaluation](features/finance/finance-free-market-data-provider-evaluation-20260811.md) | Historical gate; superseded for EODHD Free by BB-077/078 |
| Finance BB-071 Twelve Data resolution | Exact-use entitlement matrix, zero-cost experiment and support inquiry | [BB-071 resolution](features/finance/finance-bb071-entitlement-resolution-20260811.md) | Historical State B superseded by human confirmation; no account, adapter or data |
| Finance Twelve Data human entitlement confirmation | Sanitized direct-provider entitlement evidence | [Human confirmation](features/finance/finance-twelve-data-human-entitlement-confirmation-20260811.md) | Personal-plan entitlement cleared; Basic insufficient; paid fallback only; no activation |
| Finance BB-075 zero-cost real market-data gate | Fresh first-party source and exact-rights matrix | [Zero-cost gate](features/finance/finance-zero-cost-real-market-data-gate-20260811.md) | Historical fail-closed result; superseded for EODHD Free by BB-077/078 |
| Finance BB-076 owner-accepted zero-cost policy | Sanitized policy, reassessment and Stooq technical-gate evidence | [BB-076 policy](features/finance/finance-bb-076-owner-accepted-zero-cost-policy-20260811.md) | Policy implemented/tested; historical no-provider state superseded by BB-077/078 |
| Finance BB-077 EODHD entitlement | Current first-party Free product and deletion-duty evidence | [Entitlement revalidation](features/finance/finance-eodhd-free-entitlement-revalidation-20260811.md) | EOD personal research cleared while active; one-month deletion duty |
| Finance BB-077 EODHD implementation | Sanitized credential-bound adapter/memory/lifecycle evidence | [Credential-bound activation](features/finance/finance-eodhd-credential-bound-activation-20260811.md) | Historical credential-bound status; key activation and runtime smoke completed by BB-078 |
| Finance BB-078 first real market data | Sanitized acquisition, memory, replay, restart and UI runtime evidence | [First real activation](features/finance/finance-bb-078-first-real-market-data-activation-20260811.md) | 8/8 requests; 2,008 real EOD observations; durable and deployed |
| Finance BB-079 first real feature engine | Sanitized formulas, causal lineage, persistence, API/UI and runtime evidence | [First real features](features/finance/finance-bb-079-first-real-feature-engine-20260811.md) | `core-daily-v1`; 42,168 values; deterministic, durable and deployed |
| Finance BB-080 first deterministic backtest engine | Sanitized exact-lineage simulation, cost, no-lookahead, persistence and runtime evidence | [First real backtests](features/finance/finance-bb-080-deterministic-real-data-backtest-20260812.md) | Six BB-078/079 runs plus immutable restart evidence; read-only RESEARCH deployed |
| Finance BB-081 robustness/out-of-sample foundation | Sanitized split, embargo, walk-forward, sensitivity, insufficiency and immutable runtime evidence | [Robustness evaluation](features/finance/finance-bb-081-robustness-out-of-sample-20260812.md) | 70 exact local runs; all three strategies correctly insufficient; read-only RESEARCH deployed |
| Finance BB-082 longer zero-cost history | Sanitized current provider/entitlement and technical-access reassessment | [BB-082 reassessment](features/finance/finance-bb-082-zero-cost-history-reassessment-20260812.md) | Legitimately blocked; no download, adapter, runtime or evidence mutation |
| Finance BB-086 ETF dataset research | Sanitized eight-candidate rights, provenance, coverage and access assessment | [BB-086 research](features/finance/finance-bb-086-etf-dataset-research-20260815.md) | Fail-closed; WIKI has no target ETF and no external candidate qualified |
| Finance BB-086 implementation/runtime | Sanitized no-ingest result, security gate and prospective-shadow recommendation | [BB-086 runtime](features/finance/finance-bb-086-implementation-runtime-20260815.md) | Documentation/research only; zero runtime/storage mutation |
| Finance BB-074 read-only observation UI | Sanitized read-contract, UI and safety evidence | [Observation UI foundation](features/finance/finance-read-only-market-observation-ui-foundation-20260811.md) | Early M2 read-only source verified and now deployed; M8 remains planned |
| Finance BB-074 observation UI deployment | Sanitized deployment, runtime and persistence-smoke evidence | [Observation UI deployment](features/finance/finance-read-only-market-observation-ui-deployment-20260811.md) | API/Web deployed and technically verified; no manual owner approval; no provider selected |

Local-only classification prevents accidental publication of machine identities,
operational logs and sensitive service data; it does not reduce the evidentiary value
of the retained local originals.
