# BigBrain Status

## BB-128A Alpaca Live Market Data Activation Readiness (2026-09-02)

BB-128A is **IMPLEMENTED / AUTOMATICALLY VERIFIED / CI VERIFIED / ACTIVATION BLOCKED PENDING EXTERNAL ENTITLEMENT CONFIRMATION** from baseline `ed1bae8681fd18797c89f6ff140ecb92de1c8912`. It reuses `LiveMarketObservation`, `LiveObservationEntitlementGate`, `MarketDataEntitlementEvaluator`, deterministic stream identity and the existing prospective shadow boundary. Read-only `/api/v1/modules/finance/providers/alpaca/status` describes Alpaca Basic, real-time IEX single-exchange coverage, US stocks/ETFs, WebSocket as the future transport candidate, the documented 30-symbol limit, absent credentials and the unresolved durable-data lifecycle.

The authoritative existing entitlement evaluator blocks acquisition before any future provider delegate can execute. IEX cannot be represented as SIP or consolidated-US coverage, and stream identity binds provider, plan, feed, instrument, observation type, granularity and policy version. Alpaca documents message field `t` as the market-event timestamp but no separate provider-processing timestamp; that mapping remains `UNKNOWN` and keeps technical status at `contractKnownWithMappingGap`. No account, key, request, observation, persistence, adapter, deployment or trading capability was created. Implementation `646078efe77dfb4c8876afb7c65c4cdcdd421a95` passed GitHub Actions run `33651291772` across backend, frontend, documentation and secrets. Finance remains `RESEARCH / 0 SEK / NONE`.

## BB-127 Owner Research Dataset Capabilities & XLSX Intake (2026-09-01)

BB-127 is **IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / RUNTIME VERIFIED / CI VERIFIED** from baseline `b801712fca7eb0aa7cc3623a2198ac66c3ed8c1b`. The existing BB-084/126 quarantine now accepts a bounded, pre-screened XLSX (direct or one XLSX inside an owner package), reads cached cell values without formula evaluation and persists each manifest-declared historical sheet as an independent immutable Finance research revision. Macro/code, external relationships, embedded objects, traversal and bounded-resource violations fail closed. Support sheets and `CURRENT_METADATA` never become historical datasets.

`owner-research-eligibility-v1` separates purpose-specific research capability from `dataset-promotion-v1`. Owner `APPROVED_BY_OWNER` evidence can make technically adequate noncanonical data eligible-with-limitations for compatible private research while external rights, price basis, corporate actions and historical identity remain `Unknown`. An explicit external denial or incompatible schema remains ineligible. Canonical tables and gates are unchanged; research results carry candidate/artifact/workbook/dataset/policy/limitation lineage. Appliance processing on 2026-09-02 found 18 historical datasets and 154,345 accepted observations after rejecting the independently reproduced one MSFT and three JNJ OHLC inconsistencies. All 18 are research-eligible-with-limitations for at least one purpose; none is universally eligible, and close-only series remain ineligible for OHLCV backtests. Zero workbook canonical rows were added. Deterministic replay of existing `buy-and-hold/v1` consumed GOOG research revision `research-a7f1880044c83ede` as run `backtest-75a9d3296e3f9228` with identical checksum and complete lineage. API image `sha256:08f2db15e83051d18a8f8848e911f4ed7ce27839b39465cc40c1c575d4eed0e0` is healthy. GitHub Actions run `33586792133` passed backend, frontend, documentation and secrets. The result is plumbing evidence, not profitability evidence. Finance remains `RESEARCH / 0 SEK / NONE`.

## First owner intake — GOOGLEFINANCE GOOG (2026-09-01)

The first real BB-126 package is **QUARANTINED / INSPECTED / REJECTED FAIL-CLOSED / ZERO CANONICAL ROWS / CI VERIFIED** from baseline `72d895dbe67e17fa6d9ac93116fc83acd4c54bd5` and implementation `057c2f07bd10f2ff21d35d981028b262e0d07cd8`. BigBrain preserved the original owner ZIP, read its single matching embedded sidecar safely and recorded the `ApprovedByOwner` system value (policy meaning `APPROVED_BY_OWNER`) separately from external rights `Unknown`.

The immutable candidate contains 3,126 accepted GOOG daily OHLCV rows for 2014-03-27–2026-08-31. There are zero invalid OHLCV rows, missing values, invalid dates, non-positive prices, invalid volumes, duplicate/conflicting keys, out-of-order rows or detected 20%/split-like close jumps; one expected US calendar session is absent. Technical quality is `LIMITED`. The package's RAW basis and NASDAQ venue are owner claims, not independently verified semantics/identity. GOOG has no approved historical `CanonicalInstrument / ProviderInstrumentMapping`, cross-source evidence is `InsufficientOverlap`, and corporate-action evidence is absent. Candidate state is `Rejected`, promotion is `BLOCKED`, and no canonical data was added. GitHub Actions run `33528970337` passed all four jobs. Finance remains `RESEARCH / 0 SEK / NONE`.

## BB-126 Owner Market-Data Drop Folder & Quarantine Inspection (2026-09-01)

BB-126 is **IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / RUNTIME VERIFIED / CI VERIFIED** from baseline `2e6999f86f6863dabb5b7f0225248ea7b5603dfa` and implementation `48755ee0ec27fed8f38132ddca050c2d2654e6ca`. The API now watches the owner-controlled, read-only container mount `/finance-data/market-data-drop` for explicit `<file>.ready` markers and copies stable CSV or ZIP-contained CSV evidence into the existing BB-084 quarantine before inspection.

The existing 13-gate policy, OHLCV checks, effective instrument mappings and cross-source comparison remain authoritative. Owner sidecars are retained as unverified claims; possession never establishes entitlement. Unknown provenance/rights remain fail-closed, and even an all-pass inspection stops at `READY_FOR_EXPLICIT_PROMOTION_REVIEW` without canonical insertion. The existing dataset catalog reports schema, coverage, row/identity/conflict/overlap evidence plus separate technical-quality, rights and promotion states. Focused 16/16 and full API 586/586 tests, warning-free Release build, documentation/Compose/diff gates and API-only runtime verification passed. Image `sha256:e8b7cf7c03baf560740408bc01917162ba928480489490736a94547e94a37311` is healthy; GitHub Actions run `33492668713` passed all four jobs. Finance remains `RESEARCH / 0 SEK / NONE`.

## BB-125 Zero-Cost Historical Market Data Qualification (2026-09-01)

BB-125 is **QUALIFIED FAIL-CLOSED / NO STATE A / NO ACQUISITION / NO CODE / NO DEPLOYMENT** from baseline `95490168e113a506bfda200cb2b8603b5ee050d2`. Current first-party material was reviewed for SimFin Free, Alpaca Basic/free, Yahoo Finance/yfinance and Tiingo Starter/free against the existing capability-scoped entitlement policy and BB-084 intake gates.

SimFin Free is `STATE B — HUMAN CONFIRMATION REQUIRED`: it advertises $0/no card, five years, US prices, API/bulk and backtesting, but requires an owner-created account and does not sufficiently resolve exact price provenance/semantics or the raw/normalized/derived lifecycle after cancellation. Alpaca Basic remains `STATE B`: $0 IEX, US stocks/ETFs and historical data since 2016 are technically documented, while persistent storage, backups, accumulation, derived/audit and post-account rights remain unknown. Yahoo Finance is `STATE D` for automated canonical intake because current Yahoo terms prohibit automated collection without prior permission; human interactive reference is a narrower non-canonical case. `yfinance` is separately `STATE D` as an unsupported acquisition mechanism because its Apache license covers code, not Yahoo data. Tiingo Starter is `STATE D` for persistent intake because its 2026 Terms explicitly prohibit any durable Starter storage despite the attractive $0/30+ year EOD scope.

No candidate qualified for a pilot, so no account, key, provider request, data, adapter, candidate, promotion, runtime change or deployment occurred. EODHD Free and immutable WIKI evidence remain authoritative and independent; Stooq remains human-confirmation-required. Finance remains `RESEARCH / 0 SEK / NONE`. Sanitized SimFin, Alpaca, Yahoo and Tiingo inquiry drafts are published but were not sent.

Qualification/documentation commit `16deff048f68da199087de1cccceb8a0111c9a97` is published. GitHub Actions run `33471245304` passed backend, frontend, documentation and full-history secrets jobs.

## BB-124 Anti-Overfitting & Out-of-Sample Governance (2026-09-01)

BB-124 is **IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / BOUNDED RESEARCH VERIFIED** from baseline `386c8766baa0306589e8d5b1fc88c0f81c201c95`. The existing BB-081 robustness engine now produces immutable v2 evidence with chronological `TRAIN / VALIDATION / HOLDOUT`, two 50-session embargoes, a declared parameter trial population, validation-only selection and a single-use holdout lifecycle (`UNTOUCHED / EVALUATED / CONTAMINATED`). A conservative family-breadth gate exposes repeated selection without inventing p-values; DSR and PBO/CSCV remain honestly not evaluable.

Seeded no-signal, future-knowledge leakage, many-noise selection, regime-fragile and positive causal engineering controls are deterministic and explicitly non-market evidence. BB-123's `daily-next-session-open-v2` conservative friction contract is reused. Current short/biased market history remains a separate limitation and may correctly yield `INSUFFICIENT_DATA`; no statistical process repairs survivorship, historical-identity, corporate-action or intraday-liquidity gaps. Autonomous Research integrity v2 now fails closed unless selection governance passes and the holdout was fresh at selection. Finance remains `RESEARCH / 0 SEK / NONE`.

API-only image `sha256:bea523d60e541598e656347ef6663524c20e7420c5fa5f24183fdb55dc281bd0` is healthy. The bounded non-autonomous build was idempotent and returned three v2 evaluations, 45 unique runs, seven candidates, 15 cost variants and zero walk-forward windows. Every strategy reports 99 train / 33 validation / 34 holdout sessions, `INSUFFICIENT_DATA` and holdout `UNTOUCHED`; no autonomous run/experiment was created. Post-deployment aggregate evidence is 113 market revisions, 31,898 observations, 17 feature revisions, 929 backtests and 31 robustness evaluations. Existing enabled cadence continued independently; BB-124 made no provider, entitlement or cadence change and issued no acquisition command.

Media item **BB-158 — Media URL Import & Audio Extraction** is registered as **PLANNED / BACKLOG ONLY**. No Media source, runtime or deployment changed.

Initial CI run `33468854249` passed backend, documentation and secrets but exposed a pre-existing date-dependent Calendar test on the 2026-09-01 month rollover. A separate test-only correction pins that fixture clock; 6/6 focused and 160/160 full Web tests plus the production Web build pass. Calendar/Web production behavior and deployment are unchanged.

The follow-up full-history secrets scan identified one historical false positive in commit `04c9271…`: prose containing “credentials, authorization” in the Finance threat model. A fingerprint-specific `.gitleaksignore` suppresses only that reviewed historical finding; a redacted local scan of 200 commits found no other candidate. Secret detection rules remain otherwise unchanged.

Publication chain: implementation `17192451deac709a3cb2d25ee1aa0ad97919769d`; Calendar test-only rollover remediation `a0a4c9d2ca62ed43587b070f6c06db57a0217708`; fingerprint-specific secrets remediation `91247b81be5b55b1200e7c39233106ef3f2ecd3e`. GitHub Actions run `33469847365` passed backend, frontend, documentation and secrets.

## BB-123 Transaction-Cost, Slippage & Fill Realism (2026-08-31)

BB-123 is **IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / BOUNDED RESEARCH VERIFIED** from baseline `317e882ef90e15562642105ce3f7f5a2621002ad`. The existing BB-080 engine now uses versioned `daily-next-session-open-v2` and `next-session-open-full-fill/v2`: fixed, per-share/minimum and proportional costs; assumed full-spread; separately adverse slippage; whole-share/cash constraints; and explicit filled/rejected attempts. Missing exact-next-session bars, invalid opens, zero quantity, insufficient cash, unavailable positions and end-of-data fail deterministically without future-bar substitution. Old v1 runs remain immutable and readable.

BB-081's existing five-level ladder now includes explicit unobserved-spread assumptions and preserves monotonic friction. A bounded existing-evidence comparison over 265 sessions/eight instruments produced buy-and-hold net returns of 29.65858% zero, 29.59159% base and 29.41012% stress; total simulated friction was 0.00, 66.98 and 248.46. Verdict remains `INSUFFICIENT_DATA`, not strategy approval. Runtime added exactly 81 new immutable runs and three evaluations; repeated builds were idempotent. API-only image `sha256:892ed7e920e29833937c84997e44857775bee4e9bc837bf5af5220e5514e0965` is healthy. Finance remains `RESEARCH / 0 SEK / NONE`; no provider, autonomous run, broker/order, PAPER/LIVE/AUTO or real-money capability changed.

## BB-122 Historical Security Identity Evidence Pilot (2026-08-31)

BB-122 is **RESEARCH VERIFIED / STATE B / NO MAPPING / NO PROMOTION** from baseline `78736af9153ef28c435ca1a70b1971ae38677540`. A deterministic 30-ticker cohort from the 3,181 unmapped WIKI strings included full-snapshot, early-ending and late-starting cases. Bounded public-authority research classified 3 `VERIFIED`, 17 `PARTIAL`, 5 `AMBIGUOUS` and 5 `UNRESOLVED` (10.0% safely resolved). SEC/issuer and exchange filings can establish selected identity/event intervals, but current exchange directories cannot prove 2014–2016 membership and WIKI price coverage cannot prove listing, delisting, ticker change or historical universe membership.

The existing effective-dated instrument/mapping model can express a verified ticker interval, so no parallel security-master architecture was added. It cannot yet durably preserve the complete CIK/source-document/event evidence chain required for a defensible promotion. **STATE B** applies: public evidence works for selected cases but requires substantial/manual reconstruction. No WIKI download, source/schema/runtime change, canonical mutation or deployment occurred; `wiki-5713d7dccfa38f56` remains 3,722 rows/five instruments. Finance remains `RESEARCH / 0 SEK / NONE`; Stooq remains `HUMAN CONFIRMATION REQUIRED / NO ACQUISITION`.

## BB-121 Nasdaq WIKI Historical Recovery & Stooq Requalification (2026-08-31)

BB-121 is **FORENSICALLY VERIFIED / STATE A / NO ACQUISITION / NO PROMOTION** from baseline `98c9bdb040ef5f1f7f22e40dac759dfcd62f42b7`. Finance remains `RESEARCH / 0 SEK / NONE`; no production source, schema, configuration, canonical data or deployment changed.

Read-only inspection verified the persisted 235,562,224-byte WIKI CSV and its published SHA-256. It is a large but partial 2016 snapshot: 2,166,605 raw rows, 2,155,310 accepted rows, 11,295 rejected rows, 3,186 tickers and 2014-01-02–2016-12-19 coverage—not decades and not the final 2018 WIKI release. The existing promotion deliberately filters to the eight canonical EODHD watchlist identities. Only AAPL/JNJ/JPM/MSFT/XOM exist in the artifact, and their full available snapshot coverage is already present in `wiki-5713d7dccfa38f56` (3,722 rows); SPY/QQQ/IWM are absent. The remaining 3,181 tickers lack approved canonical/historical identity and venue mappings. No promotion gate was weakened.

Current Nasdaq first-party documentation still identifies WIKI as free/public-domain and requiring a free API-key account for API access, while its time-series documentation says entire-database bulk download is unavailable for free datasets. The local snapshot therefore remains valuable independent historical evidence but is not evidence of a currently obtainable complete export. **STATE A** applies: reuse the persisted large artifact; do not redownload it. No bounded expansion was scientifically justified because all currently mapped WIKI rows are already canonical and unmapped symbols require separate historical-identity evidence.

Stooq was requalified from current first-party surfaces without probing its protected CSV route. Historical pages remain publicly visible and cite multiple upstream quote suppliers, but no first-party terms/API documentation establishes an official key, automated bounded download, local retention, backtesting, derived-data or post-termination rights. Status remains **HUMAN CONFIRMATION REQUIRED / NO ACQUISITION**. A sanitized inquiry draft is published in the BB-121 report; no communication was sent.

## BB-120 Historical Evidence Expansion & Source Qualification (2026-08-31)

BB-120 is **SOURCE-QUALIFIED / FAIL-CLOSED / DOCUMENTATION VERIFIED** from baseline `940afa78932bf22019f2f45e3ef640afc2c9b5f3`. No production source, schema, provider configuration or Finance data changed; no deployment or new pilot was justified. Finance remains `RESEARCH / 0 SEK / NONE`.

The existing BB-084+ path already admits bounded multi-year CSV and ZIP-contained CSV through quarantine, OHLCV validation, all 13 `dataset-promotion-v1` gates, cross-source comparison, immutable provider-specific revisions and protected cleanup. Runtime still contains the promoted WIKI revision (`wiki-5713d7dccfa38f56`, 3,722 canonical rows across five mapped symbols) and the Zenodo/Yahoo-derived candidate remains `MANUAL_REVIEW_REQUIRED` with zero canonical rows. No duplicate importer, registry, quarantine, provenance or entitlement model was added.

Current first-party evidence confirms EODHD Free at 0 cost provides 20 calls/day and approximately the past year; older/deeper history is paid, and Bulk EOD is listed for paid plans rather than Free. Private non-commercial storage/analysis is permitted only within the active-subscription retention terms; redistribution is prohibited and copies must be deleted within one month after termination/expiry. Free-tier access to delisted securities and separate corporate-action capabilities is **UNKNOWN**, so it remains disabled. SEC EDGAR and Eurostat are legitimate zero-cost first-party evidence sources, but they provide filings/reference or statistical/macro evidence rather than a rights-cleared longer daily-OHLCV artifact for the existing intake contract. Existing FRED/ALFRED, Riksbank and ECB paths were not duplicated.

Because no newly assessed source cleared cost, exact intended-use rights, provenance, format and promotion prerequisites, BB-120 performed zero acquisitions and zero promotions. Canonical data and revision counts are unchanged. The next useful step is source-rights resolution or discovery of a first-party/public-domain daily-OHLCV artifact with historical identity/delisting coverage—not a new ingestion framework.

## BB-119 Finance Readiness & Resource-Governor Reconciliation (2026-08-31)

BB-119 is **IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / RUNTIME VERIFIED / CI VERIFIED** from baseline `5145194ba09ace91f8078c1eba1563bcfd4dbc93`, implementation `a0699d841f118d2eccb247e2a9f58c3e522e1da8` and GitHub Actions run `33393820120`. Finance remains `RESEARCH / 0 SEK / NONE`; no methodology, cadence, readiness gate, provider/data policy or trading capability changed.

Three independent causes were proven. Scheduler status reused the latest opportunity's Sunday `2026-08-30` as though it were a required market session and consequently queried exact-session evidence as 0/8. Operations inferred readiness from the latest journal reason and treated every unrecognized reason—including `nonResearchDay`—as `READY`. Sentinel restarted against a socket file left in its persistent runtime volume and failed with `address already in use`, so the governor correctly failed closed as `DEFER / metricsUnavailable`.

Scheduler and operations now consume one deterministic current readiness projection with separate `historicalEvidenceAvailable`, `currentSessionRequired`, `requiredResearchDate`, current-session readiness and feature-lineage readiness. On a non-research day historical evidence can remain available while current session and lineage are explicitly `NOT_REQUIRED`; universe count is `null`, not misleading `0/8`. On an eligible session the existing exact 8/8 market and feature-lineage gates remain unchanged. Sentinel removes its own stale configured socket before binding; unavailable or stale CPU/memory/configured-disk evidence still produces `DEFER`, and critical disk still wins as `BLOCK`.

Post-deployment read-only evidence at 2026-08-31 13:00 UTC: scheduler enabled/not running, last outcome `Skipped/nonResearchDay`, historical evidence true, current session `NOT_REQUIRED_NON_RESEARCH_DAY`, lineage `NOT_REQUIRED`, instrument count not applicable and 10 unchanged opportunities; operations reports the same readiness with no active run or attention; governor is `ALLOW/resource.ready` from healthy current CPU, memory and one configured disk snapshot. Sentinel, API and Web are healthy, HTTP health is 200 and autonomous research remains 0 runs/0 experiments. No verification run was triggered.

## BB-118 Finance Source-of-Truth & Runtime Baseline (2026-08-31)

BB-118 is a **READ-ONLY CURRENT-STATE RECONCILIATION** from published baseline `5b2b95096c4c5e3dbcb0d567be5551da067f1709`; no Finance production source, configuration, database or behavior changed. Finance is deployed in **RESEARCH**, budget **0 SEK**, execution authority **NONE**. No broker, order path, PAPER, LIVE or AUTO trading exists. Autonomous research, deterministic evidence selection, scheduler/orchestrator, resource governor and operations/recovery are implemented; the appliance scheduler is enabled but was not running at the 2026-08-31 inspection.

Sanitized read-only runtime evidence at 2026-08-31 12:04 UTC: API/system recovery healthy after a clean boot; maintenance pause false; operations `waiting` with no attention, no active run and zero operational failures; EODHD cadence enabled/healthy with last successful acquisition 2026-08-28 and latest canonical session 2026-08-28; provider-retention policy remains active-account/subscription scoped, while the exact entitlement end/deletion deadline is **NOT EXPOSED** by the responsive status endpoints. Persistent evidence contains 105 market revisions, 29,890 observations, 16 feature revisions, 797 backtest runs, 25 robustness evaluations, 288 shadow predictions, 240 shadow outcomes and 240 research-risk evaluations. The latest feature revision contains 44,520 values; the aggregate feature-value count is **UNKNOWN / NOT EXPOSED**. Finance schema is version 93 with migrations 1/90/91/92/93. Autonomous research has 0 runs and 0 experiments. Scheduler history contains 10 opportunities; the latest was skipped as `nonResearchDay`, no research is running and no outstanding active/deferred opportunity was exposed.

One current runtime inconsistency requires remediation before any new Finance capability: scheduler status is fail-closed as `universeIncomplete` with 0/8 instruments and `dataReady=false`, while operations simultaneously reports `dataReadiness=READY`. Governor status independently returns `DEFER / metricsUnavailable`. This reconciliation does not diagnose by mutation, trigger a run or alter thresholds. Implemented capability chain is real EOD market memory → immutable features → deterministic backtests → chronological robustness → macro/vintage evidence → prospective shadow/risk evidence → bounded autonomous-research/scheduler/governor/operations foundations. The BB-089 **research-only risk foundation** is implemented, but BB-053's execution-grade Hard Risk Engine DoD is not complete. Paper executor, portfolio/trading controller, broker adapter, execution/reconciliation, LIVE and AUTO remain unimplemented.

Next decision remains with the owner/system architect: **A)** strengthen scientific evidence first (longer/diversified history, anti-overfitting and multiple-hypothesis governance), recommended because current evidence remains insufficient and scheduler readiness is contradictory; or **B)** begin the still-RESEARCH-only BB-053 Hard Risk Engine completion foundation. Neither direction is implemented by BB-118.

## BB-117 Audiobook Final Polish & Media Handoff (2026-08-31)

BB-117 is **IMPLEMENTED / AUTOMATICALLY AND RUNTIME VERIFIED / DEPLOYED / CI VERIFIED / PHYSICAL OWNER REVIEW APPROVED** from baseline `de1f256ecb1424556f05825f4d6525fd5e5675c9`. On 2026-08-31 the product owner physically reviewed the production result on iPhone/PWA and explicitly stated “I am satisfied.” Continue Listening uses the shared production `SleepTimerControl` as a gold 23 px crescent in a 48 px circular target, deliberately aligned at the card's lower right. The active deadline adds a subtle selected treatment and `aria-pressed`; the authoritative `Stannar HH:MM · N min kvar` remains an independent in-flow row. The compact desktop popover is retained, while mobile uses a centered, viewport-bounded 320 px compact bottom sheet above the dock. Opening moves focus into the dialog, Escape/outside click dismiss it and keyboard dismissal restores the trigger. Presets, local stop time, off, expiry pause/sync and the single AppShell-scoped deadline are unchanged.

**Owner decisions frozen for the Media pause:** the mobile audiobook Detail Hero—back navigation above, cover left, title/author/series at right, compact two-column first viewport—is **OWNER DESIGN APPROVED** and was not redesigned. The primary playback action and full player remain **EXPERIMENTAL / OWNER REVIEW PENDING**. The current vertical production library is functional but not the desired final design; a future cover-forward, likely two-column iPhone collection is deferred. The tiny white clock, broad/form-like disclosure and persistent floating mini-player are **REJECTED**. Storytel may inform public high-level UX study only; its CSS, DOM, code, class names, tokens, assets, artwork, icons, branding and implementation must never be copied.

Full Web passed 160/160 and Vite production build plus documentation verification passed. Deployed Firefox at 390×844, 430×932, 768×1024 and 1440×900 verified 48×48 target, 23×23 glyph, inactive/active semantics, 320 px mobile sheet/280 px anchored popover, focus return, shared deadline, cancellation, independent status row without time/progress intersection, unchanged cover-left Detail Hero and zero overflow. Firefox BiDi pointer dispatch was inconsistent on three narrow runs despite the target being the top hit-test element; DOM activation exercised state transitions and physical iPhone/PWA remains authoritative. Only Web was rebuilt/recreated; image `sha256:215921b72acbcf27c5f1fcc2bdc9f0e7c512c70e7fb5892e664b18629bed3749` is healthy and `/` plus `/health` return HTTP 200. Implementation `601896619b29b2b487be8e840a48d3ea132891ad`, final compact-sheet fix `29cb505804c07b6413c057f134ec0175f08a7d99`; GitHub Actions runs `33385591024` and `33386467803` passed all jobs.

Media is now in **PLANNED PAUSE — FUNCTIONAL WORK STABLE / DEFERRED UX & DISCOVERY BACKLOG PRESERVED**. BB-155, BB-156 and BB-157 remain planned and open. UX/UI Lab is **IMPLEMENTED / DEPLOYED / OWNER REVIEW IN PROGRESS**; BigBrain Design System v1 is **OWNER REVIEW IN PROGRESS / NOT OWNER APPROVED**.

## BB-116A Admin UX/UI Lab Foundation (2026-08-31)

BB-116A is **IMPLEMENTED / AUTOMATICALLY AND RUNTIME VERIFIED / DEPLOYED / CI VERIFIED / PHYSICAL OWNER REVIEW PENDING** from baseline `fc4129cb8c6e13f2154528feb3dbb269593dbfae`. Admin now links to the permanent history-aware `/admin/ux-ui-lab` owner surface. It inventories only current production foundations and patterns under stable visible identities, renders shared `BBButton`, `BBInput`, `BBSelect`, `BBSurface`, `BBEmptyState`, `BBLoadingIndicator`, `StatusBadge`, `BBMediaArtwork` and `AppIcon` primitives, and labels specimens `EXPERIMENTELL`, `OWNER GODKÄND` or `RATAD`; no specimen is self-approved. BigBrain Design System v1 remains **OWNER REVIEW IN PROGRESS**.

The sleep-timer presentation is now one shared production/lab component: a calm gold crescent SVG in a 48 px compact target and a bounded 280 px dialog with 15/30/45/60, local stop time, active state and off. Production retains the existing AppShell timer authority; the lab injects local callbacks and performs no domain/API mutation. The previous tiny white clock and wide form-like disclosure remain recorded as **RATAD**, not resurrected. A lab-only cover-forward audiobook card candidate is included; production library rows are unchanged. A public Storytel reference study observed high-level cover prominence, short title/author hierarchy, grouped discovery and progressive disclosure only; no CSS, DOM, source, tokens, assets, icons or branding were copied.

Current reusable inventory covers action variants/states, icon action, navigation/back patterns, surfaces/cards, text/search/select controls, checkbox/range examples, loading/empty/error/warning/status feedback, artwork/fallback and timer. Checkbox, navigation-row, confirmation/dialog and several domain cards remain local recurring markup and are recorded as consolidation debt rather than refactored speculatively. Search/discovery candidate and BB-157 were deferred. Full Web passes 160/160, Vite production build and documentation verification pass. Implementation `a50c1ffa9b238bc5238b537dcbb7ecc36e476f78` was deployed by rebuilding/recreating Web only; image `sha256:e2360112a7f11e892937f0556a67d134aa14c1b40ff866c9a458ff357db9a3aa`, Web health and `/`, `/health`, `/admin/ux-ui-lab` are healthy/HTTP 200. Deployed Firefox at 390×844, 430×932, 768×1024 and 1440×900 shows responsive containment and dock clearance. GitHub Actions run `33339440666` passed all jobs. Physical iPhone/PWA review remains authoritative and no individual component is owner-approved by BB-116A.

## BB-115R Physical iPhone Sleep-Timer Remediation (2026-08-30)

BB-115R is **IMPLEMENTED / AUTOMATICALLY AND RUNTIME VERIFIED / DEPLOYED / CI VERIFIED / PHYSICAL OWNER VERIFICATION PENDING** from baseline `f8136eccb7feed7bd2f9d23f449aa9f327c51a82`. Physical owner review failed BB-115's timer acceptance: before a live AppShell session, the compact timer was a disabled icon and therefore produced no response on iPhone/PWA; after a detail-configured timer, its absolute-positioned status overlapped the progress/time area.

The compact disclosure is now always operable and uses native `<details>/<summary>` interaction rather than a JavaScript-only button toggle; deployed pre-correction Firefox hit-testing reproduced the non-opening control despite a correct hit target. Without a session it opens a visible explanation and explicit **Starta uppspelningen** action; after session creation the same open panel exposes the existing shared choices. Paused existing sessions remain directly configurable. Active status moved out of the action cluster into its own in-flow full-width grid row, preserving long duration text without overlay or horizontal expansion. Detail and Continue Listening retain one AppShell provider deadline and ordinary expiration pause/sync.

Focused Web tests pass 32/32, full Web passes 157/157 and Vite production build passes. Only Web was rebuilt/recreated and `/` plus `/health` return HTTP 200. Deployed Firefox at 390×844, 430×932 and 1440×900 verified native disclosure, visible no-session guidance/start, 280 px bounded panel, 60-minute status beside a 4:33:57 duration, static full-row layout, zero status/time intersection, shared exact detail status, cancellation and no overflow. Implementation `c6519e059fc87faec92243271e26d0d9bf06d37e`; native-disclosure correction `a4139e46820a46b78c628d1db976a150d26adf98`; GitHub Actions runs `33337614697` and `33337917824` passed. Physical iPhone/PWA verification remains pending. Full player design is still owner-review pending; UX/UI Lab remains next and is not implemented. The permanent interrupted-run rule and canonical sanitized template are present in `AGENTS.md` and `docs/operations/codex-recovery.md`.

## BB-115 Physical iPhone Artwork Remediation & Continue-Listening Sleep Timer (2026-08-30)

BB-115 is **IMPLEMENTED / AUTOMATICALLY AND RUNTIME VERIFIED / DEPLOYED / CI VERIFIED / PHYSICAL OWNER REVIEW FAILED — REMEDIATED BY BB-115R, RE-VERIFICATION PENDING** from baseline `ad3e39d76139d664c403ea28068d586eaddbada3`. BB-114 browser QA did not reproduce the later physical iPhone/PWA artwork regression. The root cause was an accumulated cascade containing three generations of audiobook-detail layout rules, including obsolete mobile `display: contents` and broad direct-child grid-column overrides. BB-115 removes those legacy detail rules and retains one explicit hero: fetched artwork and BigBrain-B share 2:3 geometry, `min-inline-size: 0`, an explicit first column, 40 vw mobile sizing and a 180 px ceiling.

Continue Listening now exposes the existing sleep timer as a secondary 44 px icon action after playback starts. The disclosure has an accessible name/state, presets, local-clock selection, cancellation and visible stop/remaining status. It reads and writes the same AppShell-scoped provider deadline as detail; expiration, pause and progress synchronization are unchanged. Full player visual design remains **OWNER REVIEW PENDING / NOT DESIGN-SYSTEM APPROVED**. UX/UI Lab remains the next planned design sprint.

Focused Web tests pass 31/31, full Web passes 156/156 and Vite production build passes. Web alone was rebuilt/recreated and is healthy (`/` and `/health` HTTP 200). Deployed Firefox measured Induction at 156×234, 172×258 and 180×270 px for 390×844, 430×932 and 1440×900 with no overflow; the Golden Compass BigBrain-B matched 156/172/180 px and 2:3. Continue Listening started 15 minutes, retained the identical deadline on detail, replaced it with 30 minutes and cancelled it. Implementation `6917ed16b1fedbebaf26b06e36352d4627959d33`; GitHub Actions run `33328144299` passed. Physical iPhone/PWA owner verification remains required. BB-156 is **PLANNED / BACKLOG ONLY**. BB-115 owner UX and BigBrain Design System v1 are not approved.

## BB-114 Audiobook Detail Polish, Sleep Timer & Floating Player Rejection (2026-08-30)

BB-114 är **IMPLEMENTED / AUTOMATICALLY AND RUNTIME VERIFIED / DEPLOYED / CI VERIFIED / OWNER UX REVIEW PENDING** från baseline `587c5d0b41c02440d852d65c494918a72e175bfb`. Detail-artwork använder en audiobook-lokal responsiv 2:3-kolumn med högst 180 px och 40 vw på vanlig mobil; canonical BigBrain-B följer samma geometri. Den redundanta LJUDBOK-raden är borttagen. Healthy native playback visar ingen Audiobookshelf-action; verifierat unavailable-läge behåller en återhämtningslänk utan utvecklarordet “reservväg”.

Runtime-/DTO-auditen verifierade att `X3M 4ever!!!` kommer från Audiobookshelf `media.metadata.description`; den exakta icke-synopsisnoten utelämnas utan källdatamutation. `Pirateaba` kommer från `media.metadata.authorName` och presenteras tydligt som **Av Pirateaba**. Serie, uppläsare, språk, år och användbara beskrivningar bevaras.

En klientlokal sovtimer erbjuder 15/30/45/60 minuter, lokal sluttid och Av. Deadline syns med sluttid/återstående minuter; expiration pausar via det normala audio-pause/sync-flödet och skapar ingen server scheduler eller progressdatabas. Persistent floating player UI är **REJECTED** och borttagen från AppShell. Audioelement, session och sync lever fortsatt i AppShell-scope providern, så playback överlever navigation; kontroller återkommer på aktiv audiobook-detail. Webplattformen kan inte garantera exakt väckning när iOS suspenderar PWA:n.

Verifiering passerar 19 fokuserade API, 31 fokuserade Web, 565 fulla API, 156 fulla Web och 32 Sentinel samt Vite/Release, dokumentation, Compose och diff. Endast API/Web återskapades och är healthy. Deployad Firefox-matris gav artwork 156 px vid 390×844, 172 px vid 430×932 och 180 px vid 1440×900, 2:3-ratio, BigBrain-B med samma mått och noll overflow. Induction fortsatte spela på Home utan overlay och återkom i Pausa-state på detail; sovtimer visade exakt 15 min och kunde avbrytas. Implementation `2607d6f804efd6bbc49bdc2c2f9e721655692a68` och GitHub Actions run `33326376331` passerade backend, frontend, dokumentation och secrets. BB-155 är **PLANNED / BACKLOG ONLY**. Media UX och BigBrain Design System v1 är inte owner-godkända.

## BB-113 Audiobook Player Affordance, Detail Layout & Artwork Remediation (2026-08-29)

BB-113 är **IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / OWNER UX REVIEW PENDING** från baseline `a50a52e33d95e317b0391647a2378a5e347210e5`. Physical iPhone-evidens visade verklig progress utan direkt play/pause, detailtitel i en katastrofalt smal restkolumn, frånvarande native kontroller, dominant tillfällig Audiobookshelf-action och en generisk media-note-bild.

Continue Listening har nu en separat 48 px tillgänglig play/pause-kontroll, medan identitetsytan fortsatt navigerar till detail. Den visar endast tid när Audiobookshelfs befintliga duration och progress finns och härleder aktuell tid ur samma auktoritativa värden. Detail använder ett explicit hero-wrapper-grid med `minmax(0,1fr)`, normal word-break, säkert overflow-wrap och 2:3-artwork; den tidigare kaskaden med mobil `display:contents` över direkta wrappers kan inte längre lämna titeln i en skör intrinsic restkolumn.

Deployad playback-availability verifierades före ändringen som `configuredHealthy`, separat identitet och verklig progress. Aktuell source innehöll redan native Spela men ownerbildens äldre copy “Öppna tillfälligt i Audiobookshelf” finns inte i aktuell bundle-source; orsaken klassificeras som äldre PWA/Web-shell-cache. Web läser nu availability explicit: healthy visar native primär action och en märkt tertiär Audiobookshelf-reservväg; unavailable visar sanningsenlig förklaring och reservväg som fallback. `index.html` får därför `no-cache, no-store` och manifestet `no-cache`; hashade assets förblir oförändrade.

Golden Compass-bilden spårades till ett verkligt Audiobookshelf `200 image/jpeg`, 400×400: den var varken BigBrain-fallback eller broken image. Endast den runtime-verifierade exakta legacy-placeholderhashen klassificeras nu som generisk och ger 404 till Web så befintlig canonical BigBrain-B visas. Annan lyckad ägarartwork bevaras utan motivbedömning. Fokuserat passerar 18 API- och 23 Web-tester; full regression passerar 564 API, 32 Sentinel och 154 Web samt Vite build och Compose. Endast API och Web återskapades och är healthy. Firefox verifierade direkt play, verklig tid, Pausa-state, mini-player och route-survival; 390×844/430×932/1440×900 i alla tre teman gav fullbreddshero, naturlig Golden Compass/lång svensk wrapping, 2:3 BigBrain-B, native primär action, tertiär reservväg och ingen overflow. Implementation `5d829fdcf64541e49b8cf355004bea90d2a372d3` är publicerad och GitHub Actions run `33269194506` passerade. En docs-run exponerade en äldre async testassertion som läste `Filtrera` under `Filtrera pågår`; testfix `09976f0ab43c825f22434177ca7952f5207b746b` väntar nu på readiness och run `33269619672` passerade alla jobb. ADR 0037, mini-player-design, Bibliotek, search/language, Finance och Sentinel är oförändrade.

## BB-112 Native Audiobook Playback + Owner Search/Result UX Remediation (2026-08-29)

BB-112 är **IMPLEMENTED / AUTOMATICALLY AND RUNTIME VERIFIED / DEPLOYED / OWNER UX REVIEW PENDING**. En separat aktiv begränsad Audiobookshelf playback-identitet är server-side konfigurerad, skild från den oförändrade integrationsidentiteten och har verklig progress. BigBrains versionerade same-origin API startar/resumerar item-bundna opaque sessioner, levererar allowlistade tracks med bounded Range/`206`, synkar/stänger sessionen och lämnar durable progress i Audiobookshelf. Ett kontrollerat prov flyttade positionen en sekund och återställde den vid close.

AppShell äger audio/session state. Playern har play/pause, faktisk position/duration, slider seek och ±30; navigation ger en dock-/safe-area-säker mini-player utan att stoppa ljudet. Continue Listening läser playbackidentitetens `items-in-progress`, inte `addedAt`. Overview är **Lyssna/Ljudböcker → Continue Listening → Bibliotek navigation row**; tre användares feedback är hanterad men composition är fortsatt owner review pending och inte globalt antagen.

Sökkortets providerberoende min-content-kollaps är korrigerad med fullbredd, `min-width:0`, normal word-break och compact cover. Svenska/English är rankingpreferenser: explicit språk är verified, starka release-token probable och unknown förblir synligt. Redundant unknown-confidence är borttagen och dedup behåller stabil releaseidentitet. Sex runtimeprov visade provider-variation: Svenska växlade mellan 2 unknown/0 Prowlarr och 4 resultat med 1 svenska/3 unknown/2 Prowlarr; Alla språk gav den senare formen. Hypotesen “Svenska-filter trasigt” är inte etablerad.

Verifiering: 16 fokuserade API-, 21 fokuserade Web-, 562 fulla API-, 32 Sentinel- och 151 fulla Web-tester passerar. Release/Vite-build, Compose och 194-filers dokumentationsgrind passerar. Nio viewport/theme-fall behåller BB-111-regressionerna utan overflow; Firefox-playerprovet gav `206`, playing state, verklig sliderposition och route-survival.

## BB-111 Audiobook Route/Detail UX Fixes & Native Playback Vertical Slice (2026-08-28)

BB-111 UX är **IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / OWNER UX REVIEW PENDING**. Route-entry använder nu modality-aware programmatisk fokus: pointer/touch behåller DOM-/screen-reader-fokus utan falsk gul headingmarkering, medan keyboardklassificerad fokus fortsatt använder den globala synliga focusringen. Ny collection/detail-navigation börjar vid toppen med `preventScroll`-fokus och en egen `scrollY: 0`-post; browser-back återställer den föregående collection-postens sparade scroll. **Bibliotek** är en audiobook-lokal semantisk navigationsrad/länk, inte en upphöjd CTA eller ny knappvariant.

Detaljheron använder ett kompakt tokenbaserat tvåkolumnsgitter där 2:3-omslag och titel/author/primär metadata bildar en sammanhållen komponent. `height:auto`, `aspect-ratio:2/3` och `object-fit:cover` stoppar deformation; mobil detail använder 88 px cover medan collection behåller den redan etablerade kompakta 68 px-storleken. Ingen global artwork-token ändrades. Okänt valfritt språk (`und`/`Språk okänt`) utelämnas i collection/detail utan att meningsfulla provider-/statuslägen döljs.

Native playback är separat **BLOCKED**. Sanerad read-only runtimeverifiering mot den redan konfigurerade server-side nyckeln visar en aktiv begränsad user/service-identitet, inte root/admin, med 0 media-progress och 0 listening sessions. Det är inte ägarens progressbärande playbackidentitet. Nyckeln får därför inte återanvändas för ägarplayback och ingen session, stream-/Range-proxy, player, mini-player eller falsk Continue Listening byggdes. Audiobookshelf stödjer user-owned playbackkontrakt enligt BB-109, så en separat server-side API-nyckel för rätt playbackanvändare krävs innan dess identitet/behörighet kan verifieras och den auktoriserade same-origin-gränsen implementeras. Ingen credential har skapats eller exponerats.

Verifiering 2026-08-28: 28 fokuserade Web-tester, 151 fulla Web-tester, 558 API-tester och 32 Sentinel-tester passerar; Vite och full Release build passerar med 0 warnings/errors. Compose, 192-filers dokumentationsverifiering, scoped gitleaks och diffkontroll passerar. Implementation `686f621937ae0e376bcc55003077769fff8b4351` publicerades och GitHub Actions run `33187236677` passerade alla jobs. Endast Web byggdes/återskapades och är healthy; API förblev healthy. Browsermatrisen 390×844, 430×932 och 1440×900 i Obsidian Gold, Forest Night och Arctic Wind gav semantisk länk, 0 overview-katalograder, ingen falsk pointer-focusring, detail `scrollY=0`, exakt återställd collection-scroll, 2:3-cover (88 px mobil/180 px desktop), `object-fit:cover`, inget okänt språk och ingen overflow.

## BB-110 Audiobook Owner UX Consolidation & Native Playback Foundation (2026-08-28)

BB-110 är **IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / OWNER UX REVIEW PENDING**. Den audiobook-lokala positiva owner-evidensen består: Media förblir bounded, samlingen öppnas separat och scroll-till-top behålls. Media använder nu den kanoniska sekundära **Bibliotek**-knappen utan lokala accent-/typografiöverstyrningar. Samlingen är avskalad och ordnad **Hitta ljudbok → Bibliotek → Hämtningar**; hela den semantiska bokraden öppnar detaljen. Hämtningar använder en mobil vertikal hierarki och terminala fel kan döljas lokalt utan att audit-, jobb- eller providerdata raderas.

Native playback är **BLOCKED** vid en uttrycklig arkitekturgräns. Den nuvarande BigBrain-identitetsmodellen har ingen godkänd mappning från autentiserad BigBrain-användare till personlig Audiobookshelf-playbackidentitet. Serviceidentiteten får därför fortsatt inte användas för ägarens progress/session och ingen token exponeras i Web. Owner/systemarkitekt måste besluta identitetsmappning, credential lifecycle, authorization och same-origin Range/session/progress-sync innan player, mini-player eller Continue Listening kan bli korrekt.

Verifiering 2026-08-28 passerade 25 fokuserade Web-, 147 fulla Web-, 558 API- och 32 Sentinel-tester, Release/Vite-build, Compose och 191-filers dokumentationsverifiering. GitHub Actions run `33179994941` passerade implementation `76b3c9eda56230e6cf570353ea5f1ca4e579a07c`; slutrun `33180923644` passerade efter att två provider-status-tester gjorts deterministiska vid sin separata asynkrona rendergräns. Endast Web återskapades och blev healthy. Browsermatrisen 390×844, 430×932 och 1440×900 i tre teman visade 0 katalograder på overview, 20 bounded rader i collection, korrekt sektionsordning, semantic whole-row, wrapping, ingen overflow, 112 px mobil dock-clearance och synlig scroll-top.

## BB-109 Audiobook Owner UX Remediation & Native Playback Investigation (2026-08-28)

BB-109 är **IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / OWNER UX REVIEW PENDING**. BB-108:s positiva owner-evidens bevaras: Media-overview är bounded och hela katalogen öppnas först via den separata samlingen. Den dominerande räknarlänken ersätts av standardknappen **Bibliotek**; samlingsrouten skiljer tydligt ny discovery från lokal filtrering, har lugnare rubrikhierarki, bounded 24-poster, dock-safe scroll-till-top och uppdelade aktiva jobb, åtgärdsbehov och presentation-only historikrensning.

Continue Listening-felet är verifierat som en identitetsgräns i Audiobookshelf 2.36.0: den commissioning-begränsade BigBrain-serviceanvändaren har inga progressrader eller sessioner, medan en annan Audiobookshelf-användare har tre aktiva progressrader/sessioner. Progress är användarspecifik och saknas därför redan i server-side adapterresponsen; Web filtrerar inte bort den. Installerad runtime verifierar start/session-track/sync/close-kontrakten, men säker native playback är **PARTIAL/BLOCKED** tills owner/systemarkitekt väljer vilken Audiobookshelf-identitet som ska äga playback och progress. BigBrain exponerar inte token och behåller den externa owner-länken som tydligt tillfällig fallback.

Stale `Hämtas` hade separat rotorsak: när både Librarr/qBittorrent och durable import evidence saknade jobbet returnerade providern `null`, vilket BigBrain tidigare tolkade som oförändrat aktivt för alltid. Efter fem minuters registreringsgrace övergår sådan avsaknad fail-closed till `failed/Kräver åtgärd`; nya jobb skyddas mot registreringsrace. Ingen acquisition har avbrutits, startats eller raderats manuellt.

Verifiering 2026-08-28 passerade 17 fokuserade Web-, 13 fokuserade API-, 144 fulla Web-, 558 fulla API- och 32 Sentinel-tester, Vite/Release-build utan varningar, Compose och dokumentationsverifiering. GitHub Actions run `33161107258` passerade backend, frontend, documentation och secrets för publicerad implementation `5e2d2b4c2ad97536cbd6007947dcb07d6aadeb39`. Endast API och Web återskapades och är healthy. Read-only reconciliation flyttade 15 evidenslösa stale aktiva poster till `failed`; totalsanningen är nu 1 completed och 18 failed. Browsermatrisen 390×844, 430×932 och 1440×900 i alla teman gav ingen overflow, 0 katalogposter på overview, **Bibliotek** utan count, separata sökintentioner, scroll-top och 112 px mobil dock-clearance. Continue Listening är fortsatt sanningsenligt otillgänglig för den anslutna serviceprofilen.

## BB-108 Audiobook Navigation UX Experiment (2026-08-28)

BB-108 är **IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / OWNER REVIEW: POSITIVE EVIDENCE + NEEDS ITERATION**. Ägaren accepterade positivt att Media-overview hålls bounded och att lång scroll börjar först efter explicit öppning av biblioteket. Navigationen är fortfarande inte globalt accepterad. BB-109 hanterar iterationen kring affordance, progresskorrekthet, samlingshierarki, discoveryplacering och historik.

Owner-bilden med film/musiknoten är verifierad som en faktisk coverfil som Audiobookshelf returnerar för biblioteksobjektet, inte BigBrains fallback eller en browserglyph. Flera berörda objekt returnerar samma upstream-bild; BigBrain ersätter inte legitimt bibliotek-artwork. Saknad eller misslyckad cover använder fortsatt den gemensamma BigBrain-B-fallbacken. Navigationsexperimentet och dockens lämplighet inväntar fysisk owner review och är inte globalt accepterade.

Automatisk verifiering 2026-08-28 passerade 142 Web-, 556 API- och 32 Sentinel-tester, Release/Vite-build, dokumentationsverifiering och Compose. Deployen återskapade endast Web. Browsermatrisen 390×844, 430×932 och 1440×900 i samtliga tre teman verifierade separata collection/detail-routes, browser-back, återställd query/sort, 0 overview-katalogkort, ingen overflow och 112 px dock-clearance på mobil. Runtime hade ingen påbörjad progresspost och visade därför sanningsenligt ingen Continue Listening-bok.

## BB-107 Owner UX Remediation after BB-106 (2026-08-27)

BB-107 är **IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / OWNER UX REVIEW PENDING**. Home-radernas faktiska grid-root cause är korrigerad i den gemensamma BBButton-kompositionen. Media-add behåller en stabil idempotensnyckel och API:t avstämmer timeout/5xx efter en påbörjad Arr-skrivning mot registrerad foreign ID; en lyckad mutation visas därför som lyckad utan en andra POST. Audiobook-starten visar nu en kompakt overview med Continue Listening och högst fyra nyligen tillagda böcker, medan hela sök-/sorterings-/pagineringsytan öppnas som **Alla ljudböcker**.

Automatisk validering passerade 137 Web-, 556 API- och 32 Sentinel-tester samt Vite/Release-build, Compose och diffkontroll. Deployen återskapade endast API och Web. Browsermatrisen 390×844, 430×932 och 1440×900 i Obsidian Gold, Forest Night och Arctic Wind visade ingen horisontell overflow; Home-texten hade 259/299 px användbar bredd på mobil, textfält var synliga och sista verkliga Media-kontroll nåddes ovanför dockan. BB-106:s owner review dokumenteras som remediation required; varken BB-106 eller BB-107 är owner UX approved. Finance, scheduler, governor och Sentinel ändrades inte.

## BB-106 Consolidated UX / Quality Fix Sprint (2026-08-27)

BB-106 är **IMPLEMENTED / AUTOMATICALLY VERIFIED / WEB DEPLOYED / OWNER UX REVIEW FOUND REMEDIATION REQUIRED**. Ägargranskningen fann Home-layout, Media first-click, sökknapp och audiobook-informationsarkitektur som kräver BB-107. Den gemensamma shell-cascaden reserverar nu den faktiska mobildockans höjd plus safe-area efter `.bb-page`-shorthanden; formulärfält, loading, media-placeholder, status och radpadding har gemensamma kontrakt. Lokal verifiering passerade 135 Web-, 555 API- och 32 Sentinel-tester, Release/Vite-build, Compose och diffkontroll. BB-099 förblir **TECHNICALLY COMPLETE / OWNER UX REVIEW PENDING**.

Ljudboksbiblioteket är paginerat och sök-/sorterbart, Continue Listening är fortsatt främst och hämtningar delas i pågående, kräver åtgärd och infälld historik. BigBrain äger bibliotek, omslag, metadata, progress, detalj och uppspelningsstart; pause/resume, seek och chapters är **PARTIAL** eftersom nuvarande granskade adapter endast har server-side read/cover och owner-deep-link, inte ett verifierat Audiobookshelf playback-session/streamingkontrakt. Inga mediajobb eller filer raderades.

## BB-105 AudioBookBay Parser & Literal Author Search Remediation (2026-08-24)

BB-105 är **TECHNICALLY COMPLETE / DEPLOYED / RUNTIME VERIFIED / CI VERIFIED**. GitHub Actions run `32769320392` passerade backend, frontend, documentation och secrets. Den pinnade Librarr-revisionen är oförändrad; image `bigbrain-librarr:1208254-bb6` lägger endast till en sanerad fixture-testad DOM-textgräns för AudioBookBay. Den verkliga sidan innehöll 9 poster och 9 titellänkar men sammanfogade `English` med nästa nästlade etikett till `EnglishKeywords:`. Den tidigare språkgrinden avvisade därför samtliga. bb6 separerar DOM-textnoder och loggar endast sanerade parser-räkningar (`posts`, `rows_with_titles`, `rejected_language`, `retained`) på debugnivå.

BigBrains sökplan reserverar nu ägarens literalinput för author-only-sökningar innan högst en metadatahärledd work-seed läggs till. `pirateaba` når därför den verkliga discovery-gränsen. Vald språkpreferens används vid slutrankning: svenska respektive engelska prioriteras, `und` behålls och Alla språk lägger ingen språkpreferens.

BB-105-uppföljningen är **IMPLEMENTED / DEPLOYED / RUNTIME VERIFIED / CI VERIFIED** genom GitHub Actions run `32777190573`. Ett case-beroende AudioBookBay-svar gjorde att `Pirateaba` gav 9 parserade men 0 Librarr-behållna rader medan `pirateaba` gav 9. `bigbrain-librarr:1208254-bb7` lowercasar därför endast AudioBookBays utgående sökparameter; originalinput, relevans/scoring, Prowlarr och övriga källor är oförändrade. Read-only runtime gav 9 kandidater för vardera `Pirateaba`, `pirateaba`, `PIRATEABA`, `The Wandering Inn` och `the wandering inn`. Jobbantalet förblev 12 och Ghostsong förblev `indexing`; ingen acquisition skapades.

Read-only runtime: direkt source-HTML gav 9 poster; AudioBookBay-parsern returnerade 9, Librarr behöll 9 och BigBrain visade 9 kandidater för `pirateaba`, samtliga med sanerad AudioBookBay-proveniens och sanningsenligt `und/unknown`. `The Wandering Inn`, `Wandering Inn`, titel+författare och den naturligt upptäckta volymtiteln `Fae and Fare` gav fortsatt 0. 390×844 passerade Obsidian Gold, Forest Night och Arctic Wind med nio verkliga resultat, ingen overflow, fri dockyta och stängd confirmation. Jobbantalet var 10 före/efter; ingen acquisition skapades. Librarr, provider och Audiobookshelf är `configuredHealthy`, och ett befintligt biblioteksobjekt är intakt. BB-099 kvarstår **TECHNICALLY COMPLETE / OWNER UX REVIEW PENDING**.

## BB-104 Universal Metadata-Aware Audiobook Search (2026-08-24)

BB-104 är **TECHNICALLY COMPLETE / DEPLOYED / RUNTIME VERIFIED**. Ägaren godkände Open Library som en lågvolyms, server-side metadata-provider. BigBrain äger nu metadata- och sökplankontrakten; Open Library används endast för kanonisk bokmetadata och omslag, medan all release-discovery och acquisition fortsatt går genom den pinnade Librarr-gränsen.

En universell sökruta klassificerar validerad ISBN-10/13, sannolik ASIN och fri text. Metadata kan ge kanonisk titel, alternativa titlar, författare, serie, år, språk, ISBN och omslag. Högst två deduplicerade provider-sökningar planeras (högst sex befintliga Librarr-källfrågor). Svenska är en rankningspreferens, inte ett destruktivt filter. Open Library saknar tillförlitlig uppläsardata och ASIN-crosswalk; dessa visas aldrig som verifierade. BB-102/103:s explicita utgåveval, confirmation, opaque candidate-ID, no-overwrite, import och completion-reconciliation är oförändrade.

Runtime 2026-08-24: `The Wandering Inn` och `pirateaba` gav användbar kanonisk Open Library-metadata men noll releasekandidater från nuvarande discovery-täckning. ISBN `9780261103573` klassificerades som ISBN-13, löstes till *The Fellowship of the Ring* och gav fyra separata Prowlarr-kandidater. Jobbantalet var oförändrat 9 före/efter all QA. Mobil 390×844 och 430×932 samt desktop 1440×900 passerade i alla tre teman utan overflow; sista releaseknappen kunde scrollas helt ovanför dockan.

## BB-103 First Usable Audiobook Flow (2026-08-24)

BB-103 är **TECHNICALLY COMPLETE / FIRST REAL ACQUISITION VERIFIED**. Den ägarvalda Narnia-releasen passerade verklig request, qBittorrent-download, fail-closed Librarr-import, Audiobookshelf-indexering, BigBrain-bibliotek och fungerande owner-deeplink. BigBrain visar exakt en biblioteksrad och jobbet som `completed/Klar`.

`bigbrain-librarr:1208254-bb4` behåller import- och source-säkerheten och kompletterar bb3 med bounded author-aware discovery. Titel-only är oförändrad; titel plus författare ger högst tre normaliserade unika varianter som når både Prowlarr och tillåten native source, medan författaren fortsatt används i post-result scoring. Redundanta `audiobook`-suffix blockeras och partial source success bevaras. Runtime-prov 2026-08-24 med `The Wandering Inn`, `pirateaba`, `en` verifierade de exakta varianterna men gav 0 råa Prowlarr-träffar, 0 Librarr-resultat och 0 BigBrain-kandidater på 24,26 s. Det kvarvarande discovery-hindret är därmed nuvarande audiobook-indexertäckning, inte BigBrain-filtrering. Inget acquisition-anrop gjordes.

Ägaren har därefter ersatt den individuella native-allowlisten med revision-bunden tillit för alla audiobook-källor som faktiskt ingår i samma pinnade upstream-commit. `bigbrain-librarr:1208254-bb5` aktiverar AudioBookBay, LibriVox och The Pirate Bay audiobook tillsammans med Prowlarr; BookTracker audiobook är del av den betrodda revisionen men förblir inaktiv utan separat konfiguration. Okänd/injicerad/dubblerad source, source-set drift eller annan revision stoppar uppstarten. Read-only-provet `The Wandering Inn` + `pirateaba` + engelska gav 0 råa träffar från var och en av de fyra aktiva källorna och 0 BigBrain-kandidater; native-källorna förbättrade alltså inte täckningen för titeln. Den kombinerade multi-source-latensen krävde den befintliga provider-specifika hårda maxgränsen 45 sekunder; cancellation består. Provider och Audiobookshelf är fortsatt `configuredHealthy`, ett befintligt biblioteksobjekt är intakt och ingen acquisition startades.

Automatisk validering passerade 540 API-, 130 Web- och 32 Sentinel-tester, upstreams organizer/search/download-paket inklusive konflikt- och discovery-regressionerna, Release- och Vite-build. Deployad QA passerade 390 × 844 och 430 × 932 i alla tre teman med verkliga kandidater, explicit bekräftelse, ingen overflow och fri dockyta. Audiobookshelf och Librarr-provider rapporterar `configuredHealthy`; befintliga fem Media-integrationer är online. Finance är oförändrat `RESEARCH / 0 SEK / NONE`. Scheduler är enabled; dagens 02:00 UTC-opportunity skippades som non-research day, inget research/training-run startade och nästa due är 2026-08-25 02:00 UTC. Den kända separata Sentinel socket-restart-loopen kvarstår. BB-099 förblir **TECHNICALLY COMPLETE / OWNER UX REVIEW PENDING**.

Runtime-regressionen efter första acquisitionen hade två separata orsaker. Ett transient provider-statusfel/fördröjt svar visades felaktigt som `NotConfigured`; Web använder nu korrekt `ConfiguredUnavailable` och visar inget providerbesked innan status har laddats. Narnia-jobbet fastnade dessutom i `indexing` eftersom Librarrs release-titel och `Unknown`-författare inte var identiska med Audiobookshelfs normaliserade titel/författare. Reconciliation kräver fortsatt exakt import-hash och exakt en unik Audiobookshelf-match, men accepterar nu deterministiskt en normaliserad kanonisk titel och behandlar `Unknown` som frånvarande metadata. Regressionstesterna passerade 540 API- och 130 Web-tester; endast API och Web återskapades.

## BB-102 Librarr provider continuation (2026-08-24)

Den tidigare Prowlarr-only-regeln är uttryckligen ersatt av ägarbeslutet **Prowlarr preferred + explicit allowlist för Librarr-native sources**. Upstream är fortsatt pinnad till commit `1208254c20b31fbf217558c0fb987f779fed1cf8`; `bigbrain-librarr:1208254-bb2` behåller den isolerade no-overwrite-patchen och lägger till en fail-closed source-policy. Endast `prowlarr_audiobooks` och den granskade native-källan `audiobookbay` laddas. Saknad/tom/duplicerad/okänd allowlist stoppar processen, och framtida upstream-källor kan inte aktiveras implicit.

Commissioning 2026-08-24 verifierade intern-only Librarr, autentiserad downstream-health för Prowlarr/qBittorrent/Audiobookshelf och BigBrain-provider `configuredHealthy`. Ett verkligt read-only-sökprov gav 11 Prowlarr-kandidater; AudioBookBay deltog utan fel men gav noll träffar för de två provfrågorna. Resultaten hade utgåveinformation men saknade auktoritativ språk- och uppläsarmetadata, så språk förblev sanningsenligt `und/unknown`. Sök-timeout är nu provider-specifikt 30 sekunder efter tidigare observerad >10-sekunders latens; caller cancellation propagerar. Ingen acquisition eller qBittorrent-hämtning har startats. BB-099 kvarstår **TECHNICALLY COMPLETE / OWNER UX REVIEW PENDING**.

## BB-101 audiobook acquisition foundation (2026-08-23)

BB-101:s provider-neutrala vertikala slice är implementerad, publicerad, CI-verifierad och deployad ovanpå den commissionade BB-100-baslinjen i `1ef411a99a6b57e057099597366aaa64004bc801`. BigBrain äger nu sökresultat/utgåvor, providerstatus och beständiga acquisition-jobb med stabila BigBrain-ID:n samt bounded API-kontrakt för search, request, list/detail och cancel. Media/Ljudböcker visar svensk språkpreferens, valfri författare, tydligt separerade utgåvor/uppläsare, detaljer och aktivitet i den verkliga registry-komponerade Media-vyn. Den deployade providern förblir sanningsenligt `None / NotConfigured`; därför skapas inget jobb och ingen falsk progress. Importpolicyn tillåter bara relativa destinationer under serverstyrd rot och vägrar både traversal och överskrivning.

Lokal regression är grön med 125 Web-, 515 API- och 32 Sentinel-tester samt Vite production- och full Release-build utan varningar. GitHub Actions `32655261525` passerade backend, frontend, documentation och secrets. Endast API och Web återskapades; båda är healthy. Runtime visar Audiobookshelf `configuredHealthy`, provider `none / notConfigured`, noll jobb före och efter en kontrollerat nekad request samt fortsatt tomt bibliotek som lyckat tillstånd. Alla tre teman passerade 390 × 844 och 430 × 932 utan overflow eller dock-ocklusion och Obsidian Gold återställdes. Befintliga fem Media-integrationer är online. Finance är oförändrat `RESEARCH / 0 SEK / NONE`; scheduler är enabled/idle och governor är oförändrad. Sentinel har fortsatt den separata kända socket-relaterade restart-loopen och ändrades inte. BB-099 kvarstår **TECHNICALLY COMPLETE / OWNER UX REVIEW PENDING**. En faktisk anskaffningsprovider kräver separat ägarval och säkerhetsgranskning; ingen tredjepart har installerats eller aktiverats.

## BB-100 audiobook platform foundation (2026-08-23)

BB-100 is implemented, published, CI-verified and commissioned from baseline `c2efe5385f714de10490b2dee54db76055f66a70`. Media owns a bounded Audiobookshelf adapter and BigBrain read-only audiobook API, while `IAudiobookAcquisitionProvider` remains truthfully `NotConfigured`. Official Readarr is retired and is not a dependency. Library, progress, continue-listening, secure cover proxy, local search, language filtering and detail UX are implemented without exposing the ABS API key to Web. Audiobookshelf 2.36.0 is initialized with library `Ljudböcker` rooted at `/audiobooks`; a dedicated non-admin `bigbrain` API identity is restricted to that library. The key remains only in the ignored appliance environment, and the library ID was discovered through authenticated server-side API access. BigBrain now reports `configuredHealthy`; overview, empty library, empty search and controlled detail/cover 404 behavior were runtime-verified. All three themes passed 390 × 844 and 430 × 932 normal-viewport review without horizontal overflow. The independent Sentinel `/run/bigbrain/sentinel.sock` restart issue remains and was not modified. Finance remains `RESEARCH / 0 SEK / NONE`; scheduler is enabled, idle and unchanged. BB-099 remains **TECHNICALLY COMPLETE / OWNER UX REVIEW PENDING**.

Audiobookshelf owner access was commissioned on 2026-08-23 using a host-port bind to the appliance's runtime-verified Tailscale IPv4 address only. The Tailscale URL returned HTTP 200, while loopback and the LAN address were not bound; Docker-internal API communication remained HTTP 200 and all persistent mounts were preserved. First-run and the restricted server identity are complete. The private tailnet address is intentionally retained only in appliance environment, not repository documentation.

## BB-099 whole-app UX audit (2026-08-23)

BB-099 is technically complete from baseline `f6ffd92645f4d0d0b96dafa52a7336991c32abb6`; owner UX review is pending. The seven registered views plus embedded Settings and important interaction states were audited against the owner-approved Family reference. Home now provides independently resolving real Family, Media and Finance signals instead of duplicating dock navigation. Media receives shared dock/safe-area clearance, a continuous Smart Shuffle composition and collapsed technical integrations. Finance's default overview is one ordered narrative while every advanced research surface remains available. AI's repetitive planned placeholders are consolidated truthfully. Local validation passed 121 frontend, 495 API and 32 Sentinel tests, both builds, documentation (177 files / 89 BB IDs), Compose and diff checks. GitHub Actions runs 32643858026 and 32644787645 passed all jobs. Only Web was recreated; final image `sha256:ae1d52afd99e5dfd3b680dcae19c69e15fa5b76a0d7d0dd27d8d7b52a325c14c` is healthy. API and Sentinel remained healthy and were not recreated. Deployed Home, Media and Finance content checks plus the full route/theme matrix found no horizontal overflow; Smart Shuffle's 44 px select was fully above the dock. Finance remains `RESEARCH / 0 SEK / NONE`; scheduler is enabled, not running and unchanged. **OWNER UX REVIEW PENDING.**

## BB-098 global premium UI rollout (2026-08-23)

BB-098 is complete from baseline `0e60ffe970e231c9af6c594c8e23b8e1e15f2600`. The owner approved BB-097 Family as BigBrain's golden design direction. The migration replaced the central non-Family DashboardWidget presentation with route-aware material sections, introduced semantic React control/surface primitives, normalized every current button and native form control through the shared material/focus/motion contract, aligned mobile and desktop navigation, and gave Arctic Wind its full cold/light material identity. Home, Media, Finance, More/Settings, AI and Admin were migrated; Family behavior and all Finance backend semantics remained frozen. Local validation passed 119 frontend, 495 API and 32 Sentinel tests, both production/Release builds, documentation (175 files / 89 BB IDs), Compose and diff checks. GitHub Actions run 32632208758 passed backend, frontend, documentation and secrets. Only Web was rebuilt/recreated; image `sha256:5410d787fadfd368587c1113675c2801e558c41c43aced8512bd94e984d3aa8e`, API and Sentinel are healthy. Opened deployed Home viewports passed at 390 × 844, 430 × 932 and 1440 × 900 after correcting the mobile settings control. Theme switching/persistence passed for all three themes and was restored to Obsidian Gold. Finance remains `RESEARCH / 0 SEK / NONE`; scheduler remains enabled, idle and unchanged. Migrated modules remain pending owner review.

BB-089 Hard Risk Engine foundation is implemented, deployed and restart-verified on 2026-08-16 from
baseline `7aa202bc88142399cd9a70085cec5bf4f23db16f`. Policy `research-eod-v1` centrally enforces
fail-closed RESEARCH-only data/time/lineage/health, universe/price/move, rolling-volatility,
volume-liquidity, hypothetical sizing/exposure and simulated loss/drawdown/loss-streak rules.
Evaluations are immutable/idempotent; durable system halt and explicit recovery transitions are
audited across reopen. Four risk endpoints and compact/detailed UI are read-only. Sunday runtime
preserved 24 valid pending, zero outcomes, 24 invalidated and zero retroactive risk evaluations;
cadence logged `no-provider-check`. Full local regression passed (423 API, 32 Sentinel, 111 Web),
both builds, docs, Compose and whitespace. Implementation commit `f8f2e1f871e5532ff906092545e8706d642b941b`
passed GitHub Actions run #48 (documentation, frontend, secrets and backend). Spread, sector and aggregate
portfolio remain explicitly not evaluable. Budget is 0 SEK; no PAPER, broker, order, LIVE/AUTO or
self-learning exists.

BB-088 is implemented, deployed and runtime/restart-verified on 2026-08-15 from baseline
`0ab8c7470a1b16e399728ea564cfeb34962d3069`. Implementation commit
`8653c44fd05fc76460e460ec5c0fef8d4cc9e7ed` passed GitHub Actions run #46: documentation,
frontend, secrets and backend all succeeded. The
Saturday drill correctly made no provider request and preserved latest session 2026-08-14, 24
valid pending predictions, zero outcomes and 24 invalidated audit rows across two restarts. Cadence
health/clock were healthy; no duplicate prediction/outcome appeared. Actual overview breadth was
3 up/5 down across eight tracked instruments, `BOOTSTRAPPING`, with no graph. One WIKI-eligible
backup and EODHD `SubscriptionOnly/restricted` classification survived. Local verification passed
412 API tests, 32 Sentinel tests, 111 frontend tests, both builds, docs and Compose. Finance remains
`RESEARCH`; no PAPER, broker, order, LIVE/AUTO, self-learning or paid service was added.

BB-087 is implemented, deployed and bounded-runtime/restart verified on 2026-08-15 from baseline `31156ec8e9780dc6622891cb4ba46050c302a6f6`. Eight current EODHD instruments at session 2026-08-14 produced 24 causally source-pinned pending predictions across three unchanged strategies; no outcome is yet knowable. The drill detected 24 earlier rows pinned to a WIKI feature revision, preserved them as `INVALIDATED`, then enforced exact source-revision membership and created correct evidence on `feature-c204a8133abaf8a2`. Restart retained 48 audit rows and did not duplicate the 24 valid predictions. Published implementation commit `86e92c431cffc1db37684bcfba5a2a785d7b05ad` passed GitHub Actions run #44 on 2026-08-15: documentation, frontend, secrets and backend all succeeded. Finance remains `RESEARCH`; there is no PAPER, broker, order, LIVE/AUTO or self-learning authority and no paid service.

BB-086 completed as a legitimate fail-closed research result on 2026-08-15 from baseline
`aee00db`. The existing WIKI artifact has zero SPY/QQQ/IWM rows. Eight bounded candidates were
reviewed; no source passed both underlying-rights/provenance and supported-access gates. Stooq's
single normal SPY request returned verification HTML, not CSV, and no challenge was solved.
No artifact, observation, revision, feature, backtest, robustness or backup was created; existing
WIKI/EODHD evidence is untouched. The approved future security/pentest baseline is now recorded
as a mandatory pre-LIVE roadmap gate. GitHub Actions run #42 passed frontend, backend, secrets and
documentation for the BB-086 publication. Finance remains `RESEARCH` at 0 SEK.

BB-085 is implemented, deployed, bounded-runtime-verified and restart-verified 2026-08-15
from baseline `69aad9d`.
`finance-provider-backup-v1` exports only provider-eligible revisions and exact derived
lineage through atomic staging plus deterministic SHA-256 manifests. WIKI revision
`wiki-5713d7dccfa38f56` restored identically in isolated staging; corruption was rejected.
EODHD remains `SubscriptionOnly/DeleteAtSubscriptionEnd` and outside indefinite backup.
Rejected cleanup retains manifest/hash evidence, manual review is conservative and canonical
promoted observations are not cleanup targets. API/UI are read-only; Finance remains RESEARCH.

BB-084 is implemented, deployed and runtime-verified 2026-08-15 from baseline `d9eadbb3`.
Finance now has a fail-closed quarantine/manifest/validation/promotion pipeline. WIKI candidate
`dd5127…a43e` passed and promoted 3,722 AAPL/MSFT/JPM/XOM/JNJ rows as
`wiki-5713d7dccfa38f56`; Zenodo DOI 10.5281/zenodo.20192822 remains manual review with zero
canonical rows. Feature `feature-3833eb92bb641e51` and three robustness v3 evaluations are
deployed; 523/175 train/test sessions resolve the old insufficiency verdict to MIXED/MIXED/
FRAGILE without creating trading authority. EODHD memory and retention remain independent.

BB-083 resilience baseline 2026-08-12 is deployed at Compose level. Durable lifecycle,
clean/unclean state, storage/clock/disk gates, recovery API/UI, Sentinel readiness and stop
grace are active. API PID-1 crash recovered as `UNCLEAN`; Finance requests remained 16 with
no duplicate evidence. Host unit install, Docker restart and reboot are blocked by interactive
sudo; physical power test is pending. Finance remains `RESEARCH`; no shutdown/trading API.

- Senast uppdaterad: 2026-08-12 (Europe/Stockholm)
- Verifierad mot commit: `eaa3a0446356316dc21b2fee3e0e0a2b30c5211c` (BB-083 resilience
  documentation baseline on `origin/main`).
- Runtime senast verifierad: 2026-08-12 (BB-083 container-runtime: Compose deployment and
  API PID-1 crash recovery verified; host install, Docker-daemon restart/reboot and physical
  power-cycle are not verified)

Status skiljer uttryckligen mellan implementerat, automatiskt verifierat, deployat och manuellt verifierat. Detaljerad evidens finns i [rapportkatalogen](reports/REPORT-CATALOG.md).

## Sprint 1 – slutförd

- Status: Slutförd 2026-08-07. Sprint 1-fixarna och remediationen efter deploymentregressionen är implementerade, automatiskt verifierade, deployade och manuellt godkända av produktägaren.
- Root cause: en ren deployexport saknade root-runtimekonfigurationen och den ännu opublicerade kalendermodulen. Integrationsvärden blev tomma och kalenderns befintliga volume monterades inte. Separat lagrades tema endast per klients `localStorage`.
- Data: kalenderdatabasen raderades inte och verifierades med `integrity=ok`; 39 händelser och 2 importer fanns kvar före återanslutning. Kalender-API läser åter data.
- Remediation: API/Web använder åter avsedd konfiguration och persistent storage. Ett globalt allowlistat Theme API, en persistent settingsvolym och ThemeProvider synkroniserar tema mellan klienter.
- Automatiskt verifierat: 99 frontendtester, production build, 207 API-tester, 32 Sentinel-tester, healthy API/Web, kalender- och integrationskontrakt samt dokumentationsgrindar.
- Manuell verifiering: godkänd av produktägaren för Dashboardinställningar, Download Control, Shopping List, Ofta köpt, kalenderåterställning, integrationer och delat persistent tema. BB-028, BB-030–BB-032, BB-034–BB-035 och BB-037–BB-038 är klara.
- Incidentrapport: [Sprint 1 deployment regression](reports/incidents/sprint-1-deployment-regression-20260807.md).

## Dashboard Views och Widget Framework Phase 1

- Status: Implementerat, automatiskt verifierat, deployat och manuellt visuellt godkänt av produktägaren.
- Implementerat: Hem, Media, AI och Admin; registerbaserade widgets; widgetbibliotek; redigeringsläge; synlighet, ordning, drag-and-drop, knappbaserad flytt och kollapsning; versionerad lokal persistens med säker fallback.
- Deployat: BigBrain Web. Ingen backend-, Compose- eller runtimekonfigurationsändring krävdes.
- Manuellt verifierat: mobilnavigationen, de fyra vyerna och layouten godkändes 2026-08-04.
- Kända begränsningar: profilsynkronisering, delade dashboards, mallar, roller, serverpersistens och fria storlekar är framtida arbete i BB-027.
- Sprint 1-buggfix 2026-08-07: Dashboardinställningar samlar Tema, redigeringsläge och widgetbibliotek bakom ett tillgängligt kugghjul. Fixen är automatiskt testad, production-byggd, headless-verifierad, deployad och manuellt godkänd på mobil och desktop.
- Dokument: [arkitektur](architecture/dashboard-widget-framework.md), [ADR 0014](adr/0014-dashboard-views-and-widget-framework.md), [runbook](operations/runbooks/dashboard-widget-framework-verification.md), [rapporter](reports/features/dashboard/).

## Kalender och Heroma-import

- Status: Implementerat, automatiskt verifierat, deployat och manuellt godkänt. Kalenderdata och importhistorik bevarades genom deploymentincidenten och är åter verifierade efter remediation.
- Implementerat: server-side `.xlsx`-parser för verifierad svensk Heroma-månadskalender, flera filer med partiell framgång, kortlivad preview, transaktionell confirm, exakt dubblettskydd, Replace/Merge/Cancel, konfliktstopp, SQLite-persistens, importhistorik, veckovy på Hem och responsiv månadsvy med direkt synliga tider.
- Formatverifiering: den privata lokala samplefilens signatur, sheet/dimensioner, calendar-grid, tidsmönster, specialetiketter, merge och dokumentegenskaper analyserades utan publicering av råvärden. Originalet importerades inte och finns inte i Git.
- Automatiskt verifierat: syntetisk workbook-parser, dag/kväll, specialtyper, flera intervall, okänd/ledig, overnight, ogiltig struktur, persistence, duplicate, replace/merge conflict samt frontendens vecka, månad, navigation, flerfils-preview, Escape och fokusåterställning.
- Deployat: BigBrain API och Web är healthy. Kalenderns read-only week/import-history-endpoints svarar HTTP 200 och läser befintlig persistent data.
- Manuellt verifierat: Produktägaren har godkänt aktuell arbetsvecka, importerad schemadata, importhistorik och mobil layout efter remediationen.
- Dokument: [Kalender](modules/calendar.md), [Heroma-kunskap](knowledge/heroma-schedule-import.md), [ADR 0015](adr/0015-calendar-heroma-import-boundary.md) och [verifieringsrunbook](operations/runbooks/calendar-verification.md).

## Matlista och Inköpslista

- Status: Implementerade, deployade och manuellt runtimeverifierade.
- Implementerat: familjefokuserade mobilflöden för veckoplanering och inköp.
- Sprint 1-buggfix 2026-08-07: konservativ skrivvariantskontroll och uttryckligt `Lägg till ändå` är implementerade för nya varor; ”Ofta köpt” använder läsbara semantiska färger i samtliga dokumenterade states. Fixarna är automatiskt testade, production-byggda, headless-verifierade, deployade och manuellt godkända.
- Kända begränsningar: BB-001–BB-003 och BB-016 återstår separat. Gemensam realtidssynk är endast planerad i BB-036.
- Dokument: [Matlista](knowledge/meal-planner.md) och [Inköpslista](knowledge/shopping-list.md).

## Media

- Status: Media Search, Media Jobs och dashboardöversikt är implementerade och deployade.
- Implementerat: normaliserade läsflöden och smala, uttryckligt bekräftade mutationsgränser via adapters; inga generella externa proxyer.
- Kända begränsningar: externa leverantörers tillgänglighet och versionskontrakt verifieras separat.
- Dokument: [modulkontrakt](modules/media.md), [media-stack](knowledge/media-stack.md), [verifieringsrunbook](operations/runbooks/media-integration-verification.md).

### Smart Shuffle

- Status: Implementerad, publicerad och deployad.
- Automatiskt verifierat: backend- och frontendkontrakt, säker sessionsmappning och playbackstatus.
- Manuellt verifierat: ett uttryckligt knapptryck i BigBrain startade rätt avsnitt på vald Samsung Tizen-TV och korrekt `NowPlayingItem` bekräftades 2026-08-04.
- Kända begränsningar: naturlig completion/långtidstestning och Jellyfins missvisande nästa-avsnitt-UI återstår i BB-014 och BB-018.
- Dokument: [Media](modules/media.md), [ADR 0011](adr/0011-smart-shuffle-jellyfin-remote-playback-boundary.md), [rapportkatalog](reports/REPORT-CATALOG.md).

### Download Control

- Status: MVP implementerad och deployad; fortsatt säkerhetshärdning pågår.
- Automatiskt verifierat: opaka ID:n, live-revalidering, preview/bekräftelse, exakt ett mål, filbevarande respektive separat destruktivt kontrakt, samtidighetsskydd och sanerade fel.
- Manuellt verifierat: minst ett användarstyrt filbevarande borttagande från qBittorrent via BigBrain. Destruktiv `deleteFiles=true` är inte fullständigt produktionsverifierad.
- Kända begränsningar: destruktiv borttagningsverifiering i BB-019, återstående destruktiv masshantering i BB-020, BB-021–BB-022 samt manuell Retry-verifiering i BB-023. BB-040/BB-033:s återstående kvalitativa UX-evidens följs efter release i BB-041.
- Sprint 2-status 2026-08-10: avslutad och deploymenten godkänd av produktägaren. Dashboard, gemensamt tema/temasynk, kalender och bevarat importerat arbetsschema, Download Control, pause/resume, batchhantering, diagnostik, mobilvy och övriga integrationer är manuellt verifierade. BB-024 och BB-026 är klara; den levererade icke-destruktiva delen av BB-020 är godkänd. Retry i BB-023 är implementerad, automatiskt verifierad och deployad men kunde inte manuellt verifieras eftersom ingen naturligt felande eller problematisk nedladdning fanns. Detta är varken blockerare eller konstaterad defekt. Batch-delete är uppskjuten. BB-033 har endast fått begränsat namn-/undertitelförtydligande.
- Sprint 1-buggfix 2026-08-07: informationspaneler, kort, header, progress och långa namn är breddbegränsade utan horisontell widgetscroll. Fixen är automatiskt testad, production-byggd, headless-verifierad, deployad och manuellt godkänd på mobil.
- Sprint 3 closure 2026-08-10: standardvyn prioriterar fel/problem, aktiva och köade/pausade före klara poster. Klara är kompakt och kollapsad i Alla-vyn men direkt åtkomlig via kontroll eller Klara-filter. Förklaringar skiljer själva Nedladdningskön från Medieflödets väg till biblioteket. 103 frontendtester och production build är gröna. Endast Web deployades; Web är healthy, API och externa container-ID:n samt API-volymer är oförändrade och Calendar/settings/Download Control läser fortsatt korrekt. Produktägaren accepterar den omedelbara smoke-nivån utan blockerande fynd och Sprint 3 är stängd. Den kvalitativa långtidsutvärderingen är uppskjuten till BB-041 och är inte ett känt fel eller en sprintblockerare. BB-040/BB-033 förblir Pågår tills deras fulla DoD har separat evidens.
- Dokument: [Media](modules/media.md), [ADR 0013](adr/0013-safe-qbittorrent-download-removal-boundary.md), [runbook](operations/runbooks/download-control-safe-removal.md), [qBittorrent](knowledge/qbittorrent.md).

## Designsystem och teman

- Status: designsystem v1 och Obsidian Gold är implementerade, automatiskt verifierade och deployade i BigBrain Web.
- Implementerat: tokenbaserade teman, persistens och säker fallback. Standardtemat ändrades inte.
- Jellyfin: separata CSS-adaptrar finns. Den befintliga BigBrain-adaptern och Obsidian Gold-underlaget ska inte sammanblandas; Obsidian Gold är inte installerat som Jellyfin Custom CSS och är inte Tizen-verifierat.
- Jellyfin följer inte BigBrains aktuella runtime-tema: serverinstallerad Custom CSS körs i en separat Jellyfin-klient och kan inte läsa BigBrains lokala `data-theme` eller localStorage. Automatisk koppling skulle bryta ADR 0012:s fristående, manuellt publicerade adaptergräns och implementeras därför inte i Sprint 1.
- Kända begränsningar: manuell tvärmodul- och Tizen-verifiering följs i BB-015.
- Dokument: [temakontrakt](design-system/theme-contract-v1.md), [manuell verifiering](design-system/manual-verification.md), [ADR 0012](adr/0012-design-system-theme-contract-and-jellyfin-adapter.md), [Jellyfin-adapter](../themes/jellyfin/README.md).

## Future product planning and Family coordination

- Status: planned only; no feature, redesign, authentication flow, external integration or runtime
  change was implemented by the 2026-08-17 capture.
- Captured: a candidate BigBrain-wide Obsidian Gold/less-is-more design direction; passwordless,
  passkey, trusted-device and step-up investigation; external school meals and school-aware
  household menu generation; and Home Calendar past-day plus mobile swipe UX investigations.
- Family epic consolidation 2026-08-18: planned-only Family View & Family Coordination now links the
  existing school-meal/menu and Calendar/Home work with BB-029, BB-036 and BB-027 dependencies, and
  captures school schedules, holidays, study days, attendance-aware meals, member context,
  daily/weekly aggregation, generic conflict semantics, provenance/freshness and privacy. No feature,
  sprint, provider research, deployment or runtime change occurred.
- Priority: the active Finance direction remains the smallest deterministic foundation toward
  `FINANCE AUTONOMOUS RESEARCH v1`. These entries await normal prioritization and do not move the
  existing consolidation/security or penetration-testing gates.
- Documents: [Family epic](architecture/family-view-family-coordination.md),
  [backlog](BACKLOG.md#future-family-view--family-coordination-epic) and historical
  [planning record](reports/documentation/product-ux-auth-school-meals-backlog-capture-20260817.md).

## Sentinel och systemstatus

- Status: grundläggande systemstatus är deployad; den bredare Sentinel-arkitekturen är föreslagen och inte godkänd som generell mutationsplattform.
- Kända begränsningar: lokala, ännu inte publicerade Sentinel-förslag är inte del av denna baseline.
- Dokument: [arkitektur](architecture/sentinel-architecture.md), [kunskap](knowledge/sentinel.md), [ADR-index](indexes/adr.md).

## Finance

- **Current state is the BB-118 baseline at the top of this document.** Finance spans several delivered bounded research slices rather than “early M2 only”: real EOD data/persistence, deterministic features/backtests/robustness, macro evidence, prospective shadow/risk evidence and autonomous-research operations are implemented and deployed. Trading-related M4/M5/M7+ gates remain incomplete.
- The chronological bullets below preserve dated historical evidence. Statements such as “no provider”, “no real data”, “not deployed” or “scheduler disabled/default-off” describe their original slice and are superseded as current-state claims by BB-078 activation, BB-095 commissioning and BB-118 runtime evidence.
- Historical foundation ledger begins here: M0 planning plus decimal-based money/price/quantity/risk primitives,
  UTC market observations, provider-neutral market-data and strategy contracts, explicit
  risk/policy decision boundary, in-memory append-oriented decision journal, future
  paper-domain records and deterministic reference pipeline.
- Completed research: BB-046 compared eight provider candidates. Twelve Data is the
  primary Nordic/global EOD candidate; Tiingo and Massive are specialized US alternatives.
  Daily raw OHLCV plus separate corporate actions is the recommended M2 scope.
- Completed research: BB-072 compared ten free/free-adjacent source products with dated
  first-party evidence. None passed the complete retention/non-display/backtesting gate.
  EODHD Free Starter is the best conditional evaluation lead and Twelve Data Basic the
  Nordic technical lead; neither is selected or authorized. Decision: DO NOT INGEST YET.
- Architecture baseline: collect once/reuse when permitted, explicit provenance and
  fail-closed entitlement checks govern local market-data memory. Free/legal sources are
  evaluated first, but free access is not retention permission. Decision/outcome evidence
  and controlled learning are versioned and cannot mutate active/live strategies.
- Implemented BB-045 slice: typed market-data uses; Allowed/Denied/Unknown policy;
  provider/product/evidence/validity/retention/deletion metadata; immutable dataset
  revision and provenance models; raw/derived parent lineage; stable reason codes and a
  deterministic evaluator where missing, mismatched, expired or uncertain scope denies.
- Implemented canonical slice: stable Equity/ETF identity independent of ticker; lifecycle,
  currency, venue and MIC; inclusive effective-date symbol mappings; decimal daily OHLCV;
  raw/adjusted basis; cash dividends; exact rational splits; quality findings and
  deterministic duplicate/conflict handling with immutable revision/policy provenance.
- Implemented session/replay slice: explicit Trading/Closed/Unknown sessions, IANA timezone
  conversion with DST ambiguity rejection, closure/missing/provider-gap/invalid/unknown
  classifications and immutable-revision replay with availability timestamps, historical
  provider references, corporate-action events and stable same-time ordering.
- Implemented revision slice: immutable member snapshots, parent ancestry, explicit
  original→replacement corrections, linear supersession, inclusive availability-as-of
  selection and deterministic ordering. Old revision IDs remain exactly reproducible;
  invalid references, cycles, branches, future members and scope changes fail explicitly.
- Implemented acquisition slice: provider-neutral requests and immutable batches carry
  exact source/product, canonical/provider identity, range, timezone, pagination,
  provenance, completeness and destination revision. A fail-closed gate requires all
  storage/backtest/derived uses before adapter execution. The `SyntheticFixture` adapter,
  deterministic retries/overlap, acquisition journal and orchestration reuse existing
  normalization, gap/replay and revision assembly without IO or persistence.
- Implemented persistence-foundation slice: immutable dataset manifests bind revision,
  coverage, counts, checksum, acquisition/policy/provenance, storage format and deletion
  obligations. A provider-neutral contract plus in-memory correctness reference supports
  immutable append, exact/range/action/gap queries, lineage, integrity, scoped enumeration
  and auditable payload deletion without retaining licensed facts in deletion receipts.
  JSONL and SQLite fixture benchmarks measured up to 1,260,000 rows; the evidence supports
  a provisional immutable-file + transactional SQLite catalog/index direction, not an
  activated production store or final ADR.
- Implemented BB-073 synthetic live-learning slice: provider-neutral current observations
  distinguish event/provider/received/knowledge time and honest real-time/delayed/EOD
  freshness. A deterministic fixture feed represents out-of-order delivery, duplicate,
  correction, session, missing observation and outage. The broker-free fixture shadow rule
  appends immutable strategy/config/feature/risk/build-bound predictions and later outcomes,
  then calculates version-isolated prospective return/excursion/volatility/cost metrics.
- Automated verification 2026-08-11: .NET 10 restore/build passed; 344 API tests and 32
  Sentinel tests passed (376 total). This includes 16 live observation/entitlement/shadow/
  outcome/metric/no-order tests plus all prior persistence and no-lookahead invariants.
- Implemented BB-074 early read-only Finance observation UI: Finance-owned typed snapshot
  and `GET /api/v1/modules/finance/observation` expose RESEARCH safety, provider/entitlement,
  configured watchlist, freshness/session/quality/history and sanitized memory/revision/
  provenance. Production defaults to no provider, no observations and denied real ingestion/
  storage. The responsive view labels fixtures and chart gaps and has no trade/order control.
  API and Web are deployed and technically runtime-verified; not manually product-owner
  verified and M8 has not started.
  Verification 2026-08-11: solution build passed with zero warnings/errors; 351 API and 32
  Sentinel tests passed; 106 Web tests and the Web production build passed; documentation,
  Compose and whitespace gates passed. Runtime API/Web health, fail-closed Finance response,
  405 mutation denial, mobile/desktop headless layout and persisted Calendar/theme/Media
  read smoke passed. No external browser request or market feed was observed.
- BB-071 entitlement update 2026-08-11: a human Twelve Data representative confirmed the
  submitted private/self-hosted personal scope is supported on a qualifying Personal plan,
  including local storage/retention, research/testing, post-termination retention, derived
  data, audit metadata and owner-personal-funds investment use. Basic is evaluation/trial
  only and insufficient. Twelve Data is an entitlement-cleared **paid fallback / qualified
  candidate**, not selected or active.
- Historical free-live research ranked Twelve Data Basic conditionally, but that rank is
  superseded by human evidence that Basic is evaluation/trial only. Alpaca Basic/free IEX
  is now the next cost-first candidate; its entitlement is unresolved. EODHD Free remains
  limited/delayed and Alpha Vantage realtime/delayed US data premium-only. No provider is authorized.
- Combined gate: Twelve Data Personal is legally qualified for the submitted scope but paid;
  free operational suitability is **NO** for Basic. Nasdaq Nordic delayed files still require
  prior approval and are not a canonical OHLCV/corporate-action product. Free Nordic
  eligibility remains unknown. Decision remains **DO NOT INGEST** pending selection.
- The earlier public-evidence state **HUMAN CONFIRMATION REQUIRED** is superseded for a
  qualifying Twelve Data Personal plan by direct provider correspondence. It is not cleared
  for Basic, commercial/paying-subscriber, redistribution, customer/third-party, unknown
  market or materially different use. No plan, account, key, adapter or real data exists.
- Cost-first provider selection remains open. Next safe task: resolve Alpaca Basic/free IEX
  entitlement for the same private scope, without creating an account, key, SDK or adapter;
  then compare any authorized adequate zero-cost option with the Twelve Data paid fallback
  and request explicit product-owner selection before first authorized ingestion.
- Documentation verification 2026-08-11: documentation gate passed for 127 Markdown files
  and 74 unique BB IDs; `git diff --check` and Compose configuration validation passed.
  Source build/test suites were not run because this slice changes no production source.
- BB-075 zero-cost gate 2026-08-11: external Finance market-data budget is exactly 0 SEK.
  Fresh first-party review of Alpaca, Stooq, Yahoo/yfinance, Nasdaq Data Link, EODHD, Alpha
  Vantage, Finnhub, FMP and direct/open alternatives found no exact source with complete
  automation, local retention, replay/backtest and artifact-lifecycle rights. Result:
  **FAIL CLOSED / NO PROVIDER ACTIVATED**. No account, key, adapter, payload or real data exists.
- Current candidates: Alpaca Basic/free IEX is HUMAN CONFIRMATION REQUIRED; EODHD Free
  Starter is the second clarification track. Twelve Data Personal is entitlement-cleared
  but inactive because it is paid. Production remains no-provider/no-real-data.
- Implemented status correction: the safe Finance observation response now exposes
  `ZERO-COST ENTITLEMENT GATE` and `zero-cost-provider-unresolved` instead of superseded
  BB-071 State B wording. Safety flags remain RESEARCH with ingestion, real storage, PAPER,
  LIVE and broker false. Automatically verified: 7 focused tests, 351 API tests, 32 Sentinel
  tests and 106 Web tests plus backend/Web production builds passed. Not deployed: no
  provider passed and no runtime activation was warranted.
- Repository gates: documentation verification passed for 128 Markdown files and 75 unique
  BB IDs; Compose configuration and `git diff --check` passed. The documentation command
  required an unsandboxed rerun after a sandbox-only `spawnSync git EPERM`.
- BB-076 policy update 2026-08-11: Finance now distinguishes explicit provider grants,
  owner-accepted personal research, human confirmation and denial per capability. Owner
  acceptance is restricted to legitimate 0-SEK private read-only research and cannot
  override an explicit prohibition, payment/permission requirement or access control.
- Stooq daily historical download reached owner-accepted residual entitlement evidence for
  the bounded personal use, but the official CSV smoke returned a JavaScript proof-of-work
  verification page rather than data. No bypass was attempted, so activation remains
  fail-closed. No provider/account/key/adapter/real memory/replay/deployment changed.
- EODHD remains conditionally explicit while subscribed with its deletion duty and absent
  account/key/lifecycle. Alpaca retained IEX use still requires human clarification.
- BB-077 2026-08-11 revalidated the renamed current **EODHD Free** tier: €0, 20 calls/day,
  past-year EOD and private non-commercial storage/manipulation/analysis while active. All
  copies must be deleted within one month after expiry. This capability is authorized under
  ADR 0022; post-expiry use/retention and redistribution are denied.
- Implemented and automatically verified: direct server adapter, bounded retries/rate,
  eight-symbol mapping, SQLite/content-addressed durable memory, acquisition journal,
  revision-aware read projection, deterministic replay, compact API/UI retention status and
  preview-confirm-delete receipts. Corporate actions, intraday and live remain disabled.
- BB-078 runtime evidence 2026-08-11: credential presence was verified as a boolean only;
  EODHD Free is enabled with active account and unset entitlement end. Exactly eight external
  EOD requests succeeded without retry for the full watchlist. The durable store contains
  2 008 real observations, eight payloads and eight revisions covering 2025-08-11 through
  2026-08-10. No symbol failed or was rejected.
- Deployed verification: API/Web healthy; the API reports REAL, EODHD Free, delayed/closed,
  durable persistence and active retention. All eight exact-revision replay checks repeated
  with identical checksums. API/Web recreation preserved counts/revisions/payloads and the
  worker skipped today's completed symbols, leaving request count at eight. Headless mobile
  and desktop checks rendered all instruments and chart without console errors or overflow.
- BB-079 runtime evidence 2026-08-11: `core-daily-v1` built locally from the same eight
  immutable market revisions without another EODHD request. Revision
  `feature-5d0397a53d094a2f` contains 42 168 values (39 616 available, 2 552 warmup, zero
  reported quality issues) over 2025-08-11–2026-08-10. Rebuild checksum/idempotency,
  no-lookahead tests, SQLite persistence/restart, feature API/UI and retention inventory
  were verified. Dependent EODHD feature artifacts are in deletion scope.
- BB-080 runtime evidence 2026-08-12: the first offline deterministic M3 engine binds exact market/feature revisions, strategy/version/parameters, next-open simulation, whole-share sizing, cost model and seed to immutable run IDs/checksums. Six BB-078/079 runs for buy-and-hold, SMA10/20 and momentum20 under zero and conservative costs are persisted, API/UI-visible and idempotent across restart. Golden/no-lookahead/cost/retention tests pass. Deployment-day scheduled ingestion ran independently; the backtest engine made no provider request and old exact runs remain immutable.
- BB-081 runtime evidence 2026-08-12: `chronological-oos-walk-forward/v1` and `transparent-robustness-score-v2` bind 16 exact market revisions plus `feature-a04bcf61e20a79ec`. The 70/30 plan with 50-session embargo yields 176/26 sessions and three expanding walk-forward windows. Seventy unique runs cover three strategies, seven bounded parameter variants and fifteen cost points. All verdicts are correctly `INSUFFICIENT_DATA`; SMA and momentum underperform buy-and-hold in the primary test. Immutable API/UI evidence, deletion lineage, no-leakage and restart determinism are deployed/runtime-verified. The evaluator made no provider request; Finance remains RESEARCH.
- BB-082 provider reassessment 2026-08-12: Stooq remains owner-accepted only at the
  entitlement layer, but both its public terms route and daily CSV route returned an active
  JavaScript verification control. No challenge solution, browser automation or alternate
  endpoint was attempted. EODHD Free still exposes about one year; Alpha Vantage full daily
  history and Nasdaq Data Link US EOD are paid. The legitimate blocked path therefore
  applies: zero historical downloads, no adapter/runtime/deployment change and no new
  features/backtests/evaluations. Existing EODHD memory and BB-078–081 evidence remain intact;
  all three robustness verdicts remain `INSUFFICIENT_DATA` and Finance remains `RESEARCH`.
- Known limitations: no live/near-live feed, corporate-action ingestion, robust out-of-sample strategy validation,
  full Risk Engine, paper executor, broker adapter or trading runtime. The deployed Finance
  API/UI is read-only observation only.
- Blockers: no zero-cost live/near-live source is selected; strategy and trading gates remain. ADR 0021 was accepted after explicit
  product-owner architecture review on 2026-08-10; its acceptance does not activate a
  provider. All documented gates block real money.
- Owner approval required: every promotion toward live or greater autonomy.
- Live trading enabled: **NO**.
- Current trading mode: **RESEARCH** (domain/default and module status only; not deployed).
- Dokument: [master roadmap](architecture/finance/master-roadmap.md),
  [module](modules/finance.md), [threat model](security/finance-threat-model.md),
  [market-data memory](architecture/finance/market-data-memory-and-provenance.md).

## Säkerhets- och publiceringsnotering

## Sprint 3 – Download Control-navigation och begriplighet

- Status 2026-08-10: **Stängd**. Implementation complete; automated verification, production build, Web-deployment och teknisk runtimeverifiering pass. Omedelbar produktägar-smoke accepterad utan blockerande fynd. Extended manual UX evaluation är deferred till BB-041 och blockerar inte sprintstängningen.
- Sprintmål: göra Download Control snabbt att överblicka och navigera med lång historik samt tydliggöra skillnaden mellan nedladdningskön och Medieflöde, utan att ändra mutationsgränser.
- Levererat scope: BB-040 och återstående UX-del av BB-033; långtidsvalidering följs i BB-041.
- Varför: BB-040 är en verifierad responsiv användbarhetsfriktion efter Sprint 2; BB-033 är närliggande informationsarkitektur och kan lösas i samma vy utan nya provider- eller datakontrakt.
- Beroenden: befintlig Download Control-listning, filter/urval/batch-state och Media Jobs-livscykel; inga nya externa API:n krävs.
- Risk: låg till medel. Huvudrisken är regression i urval/filter/batch och att färdiga poster blir svårare att hitta.
- Sprintnivåns acceptance criteria: relevanta kontroller och aktiva/problematiska poster nås utan lång scroll; färdiga poster är enkla att hitta; mobil, desktop, tangentbord och skärmläsare fungerar; batchurval och säkra mutationer är oförändrade; användaren förstår skillnaden mellan Nedladdningskö och Medieflöde; automatiska regressionstester och manuell responsiv verifiering är godkända.
- Ingår uttryckligen inte: ny Retry-logik, framtvingat feltest, destruktiv batch/delete, retention/rensning, Arr-recovery, nya providerintegrationer, realtidssynk eller implementation av BB-039.
- Faktisk omfattning: frontend/UX, regressionstester och tillhörande dokumentation; inga backend- eller delade kontrakt ändrades.

Retry-verifieringen i BB-023 utförs separat när ett naturligt säkert testobjekt finns och är inte ett villkor för att starta eller avsluta Sprint 3.

Inga mediafiler eller externa tjänsters konfiguration ändrades av Dashboard Phase 1 eller dokumentationskonsolideringen. Aktuellt kvarvarande arbete finns i [BACKLOG](BACKLOG.md); arbets- och completion-regler finns i [AGENTS](../AGENTS.md).
# BB-090 complete (2026-08-16)

Repository history proves that the implementation commit is `6f6cc743b91b9d0c2cb52fb297d0215fd7125b50`, followed by publication commit `fb55537aa77dd54c7a5929e3f29084bb5f86252e`. The previously documented `6f6cc746ce5d8bea24d19ad48c3596cb9ed0de28` did not exist and is superseded as incorrect provenance.

Closure runtime evidence dated 2026-08-16: the stale one-off API container was identified as an old-image maintenance invocation that had instead run the web host for five days; it was gracefully stopped and removed after proving the named Finance volume was shared and persistent. A hash-verified full SQLite backup was created before migration. An isolated copy migrated to ordered versions `1,90,91,92` with identical before/after counts, followed by production migration, adjusted-history audit, API/Web deployment and repeated restart/idempotence checks. API, Web and Sentinel are healthy. The immutable 25 market revisions, 9,746 observations, 6 feature revisions, 473 backtests, 13 robustness evaluations, 48 shadow predictions, 0 outcomes and 0 risk evaluations survived unchanged.

The adjusted audit classified 24 EODHD revisions `RAW_AND_ADJUSTED_VALID` and `wiki-5713d7dccfa38f56` `ADJUSTED_SEMANTICS_INVALID`; the WIKI revision was not rewritten and adjusted research is denied while raw research remains valid. Macro promotion now goes through quarantine, hashes, rights/provenance, validation and promotion evidence. Production holds `fred-a98118273a99adc9`, 58,196 observations, one candidate and one revision as `REVISED_HISTORY_EXPLORATORY`; its latest deterministic explanatory context is Rates UNKNOWN, YieldCurve NORMAL, Inflation HIGH, Labor IMPROVING. This is not a point-in-time or trading claim.

The runtime now reports `fredApiKeyConfigured=true` without disclosing secret material. Official first-party `fred/series/observations` requests with `output_type=2` promoted CPIAUCSL and UNRATE for 2020-01-01–2020-03-01 over real-time period 2020-02-01–2021-02-01 as separate immutable revisions `fred-9300bb944c8c319d` and `fred-5800c2de8f2ef185`, 36 observations each, `POINT_IN_TIME_CAUSAL`. The original `fred-a98118273a99adc9` and all 58,196 `REVISED_HISTORY_EXPLORATORY` observations remain unchanged. UNRATE January 2020 proves a real revision from 3.6 (real-time start 2020-02-07) to 3.5 (2021-01-08). Knowledge time is conservatively the following day at 00:00 UTC; as-of selection and missing-vintage behavior fail closed without revised-history fallback.

Local finalization regression is 440 API, 32 Sentinel and 113 frontend tests, with Release/frontend/container builds, documentation verification and Compose validation green. API, Web and Sentinel passed two restart/recreate checks. Production retains 25 market revisions, 9,746 observations, 6 feature revisions, 473 backtests, 13 robustness evaluations, 48 shadow predictions (24 PENDING and 24 INVALIDATED), 0 outcomes and 0 risk evaluations; Sunday macro acquisition created no market session, prediction or outcome. Finalization commit `0f2ba9c3e7d5e7b3bf088c9094bdd79b3a35db1e` passed GitHub Actions run 55: backend, frontend, documentation and secrets all succeeded. Every BB-090 closure gate is satisfied; **BB-090 is COMPLETE**. Finance stays RESEARCH.

# BB-091 complete (2026-08-17)

Provider-neutral Riksbank/ECB Macro and FX implementation, migration 93, verified backup/isolated drill, 20-year bounded production acquisition, cross-provider comparison, exact retry idempotence, preservation and restart are green. Riksbank holds 15,065 and ECB 16,804 `REVISED_HISTORY_EXPLORATORY` observations; all 58,268 BB-090 FRED observations remain unchanged. Final local verification passed 449 API, 32 Sentinel and 113 frontend tests plus Release/frontend/API image/docs/Compose/diff/secret gates. Implementation commit `dc42d47f8712023984d653b0c34977145476b2e3` passed GitHub Actions run 31997421033. **BB-091 is COMPLETE.** Finance remains RESEARCH / 0 SEK with no PAPER, broker, order, LIVE/AUTO or self-learning.

# BB-092 complete (2026-08-22)

The bounded Autonomous Research v1 foundation is implemented, locally verified and published in `8f8358c990b871c986bf0ee30ce434cb68a2394e`. It reuses immutable daily features, deterministic backtests and genuine chronological OOS/walk-forward robustness; stores all experiment outcomes with pinned lineage, family attempt counts and deterministic identities; and exposes read-only API/UI evidence. Local verification passed 455 API, 32 Sentinel and 114 frontend tests plus Release/frontend/API-image/docs/Compose/diff/secret gates. GitHub Actions run 32587285183 passed backend, frontend, documentation and secrets. No deployed production runtime verification was performed. Finance remains RESEARCH / 0 SEK / execution authority NONE; Hard Risk Engine is unchanged. **BB-092 is COMPLETE.**

## BB-092 remediation/finalization (2026-08-22)

Correctness remediation is implemented, verified and published in `4f5e2b3272c92b3ad072bc13849b374824f50299`: complete failed-run reconstruction after interruption, immutable same-key failed-result semantics, explicit run/experiment lineage, durable global single-flight, actual attempt sums, consistent next-session/horizon-1 hypotheses, separate scientific verdict totals and bounded historical run/experiment reads. Current regression passed 461 API, 32 Sentinel and 114 frontend tests, Release/frontend/API-image builds, documentation, Compose, diff and sanitized secret-pattern gates. GitHub Actions run 32589143795 passed backend, frontend, documentation and secrets. The work remains BB-092 and does not begin the Scheduler/Orchestrator slice. **BB-092 is remediated/finalized and ready for separately approved operationalization planning.**

## BB-092 final evidence-selection remediation (2026-08-22)

A final narrow correction published in `6796953533fb72cb12e5546964033e2939cf74c0` selects only the exact latest robustness generation returned by the repository-native builder and requires coherent feature, market, checksum, relational and strategy-version evidence for both enabled families. Historical lexical ordering and stale cross-revision fallback are removed. Missing or conflicting current evidence fails closed before experiment creation. Local verification passed 465 API, 32 Sentinel and 114 frontend tests plus Release/frontend/API-image/documentation/Compose/diff/secret-pattern gates. GitHub Actions run 32590079318 passed backend, frontend, documentation and secrets. **BB-092 final remediation is complete.**

# BB-093 implementation (2026-08-22)

The disabled-by-default Finance Research Scheduler/Orchestrator v1 is implemented, verified and published in `0f952f88e3ec630b0a000e3d7565dd6274f63c09` over BB-092. It uses a 60-minute check, one 02:00 UTC current opportunity, deterministic session-date identity, durable opportunity journal, restart reconciliation, bounded deferral and read-only API/UI status. It neither polls providers nor changes prospective shadow, research methodology, Hard Risk or execution authority. Local verification passed 475 API, 32 Sentinel and 115 frontend tests plus Release/frontend/API-image/documentation/Compose/diff/secret-pattern gates. GitHub Actions run 32591633284 passed backend, frontend, documentation and secrets. **BB-093 is COMPLETE.** Continuous unattended research is not yet approved.

Readiness remediation on 2026-08-22 is implemented, verified and published in `adf930a6aacbfab2e314f558a0307b7643edb7e0`. It removes the unsafe `MAX(session_date)` readiness shortcut: all eight approved watchlist instruments must be current for the required market session, and a same-session feature revision must exactly match the canonical market-revision lineage and include every instrument. Temporary gaps defer with specific reasons; an older deferred opportunity is explicitly superseded at the next date. Local regression passed 479 API, 32 Sentinel and 115 frontend tests plus all applicable build/configuration/documentation gates. GitHub Actions run 32592856198 passed all four jobs. No provider polling, BB-092 scoring, Hard Risk or execution authority changed. **BB-093 is remediated and ready for Resource & Safety Governor review; that slice has not started.**

# BB-094 implementation (2026-08-22)

The Finance Resource & Safety Governor v1 is implemented, verified and published in `8f6e83904aa3b0a9e6995c22c699a7344777739d`. Scheduled research now evaluates trusted Sentinel-backed CPU, memory and configured-disk snapshots after recovery/readiness and before opportunity claim. Unavailable/stale/failed critical metrics defer fail-closed; critical disk blocks; all applicable reasons and compact evidence persist on the opportunity. Temperature and service activity are explicitly unsupported rather than inferred. Local verification passed 486 API, 32 Sentinel and 115 frontend tests plus all applicable build/configuration/documentation gates. GitHub Actions run 32595755361 passed backend, frontend, documentation and secrets. Scheduler default-off, manual BB-092, Research Integrity, Hard Risk and `RESEARCH / 0 SEK / NONE` authority remain unchanged. **BB-094 is COMPLETE; continuous unattended research still awaits a separately reviewed operations/recovery-hardening slice.**

# BB-095 implementation (2026-08-22)

Finance autonomous research operations/recovery v1 is implemented, verified and published in `31c11865b430a137b7eb5be77681158d1d6adb1f`. The read model derives from existing cadence, scheduler, governor, research and recovery truth; startup reconciliation preserves immutable experiment evidence. True operational failures are uniquely journaled and become attention-required after three consecutive opportunities, while scientific rejection and temporary deferral remain normal. Scheduler stale/persistent waits and explicit maintenance pause are visible through read-only API/UI. Local verification passed 495 API, 32 Sentinel and 115 frontend tests plus all applicable build/configuration/documentation gates. GitHub Actions run 32597319341 passed backend, frontend, documentation and secrets. Scheduler remains disabled by default and authority remains `RESEARCH / 0 SEK / NONE`. **BB-095 is complete and the foundation is eligible for a separate explicit deployment decision enabling continuous unattended RESEARCH; this publication did not activate it.**

# BB-096 implementation (2026-08-22)

BigBrain UX Sprint v1 is complete and published in `1293d36a04013bb65469ca2191f2f1683d52a50d`. The application now uses one mobile-first AppShell, one semantic component/token contract and the selectable Obsidian Gold, Arctic Wind and Forest Night themes. Hem is a calm launcher; existing meal, shopping and calendar tools moved intact to Familj; Media and Finance retain their complete functional surfaces; Settings/theme selection, AI and Admin are discoverable through Mer. Obsidian Gold is the default, legacy stored theme IDs migrate safely, PWA theme color follows the active theme and navigation reserves iOS safe areas. Local verification passed 495 API, 32 Sentinel and 115 frontend tests plus Release, frontend, API/Web image, documentation, Compose, diff and sanitized changed-file gates. A temporary cross-theme mobile/desktop view matrix was manually inspected. GitHub Actions run 32600293268 passed backend, frontend, documentation and secrets. No Finance research, scheduler, governor, Hard Risk or execution behavior changed.

## BB-096 appliance commissioning (2026-08-23)

Repository HEAD `27b67450cee61f0283a8eaaf7748a0d57274ea1c` was built and deployed through Compose without removing volumes. The served Web contains the BB-096 AppShell and three themes; real-browser checks passed theme switching/persistence, the five-destination mobile dock at 390 x 844, desktop rail at 1440 x 900, AI/Admin secondary navigation and representative Family, Media, Finance and Admin surfaces. All relevant read-only API smoke requests returned HTTP 200. Recovery was healthy after the controlled API/Web recreation. Finance stayed `RESEARCH / 0 SEK / NONE`; scheduler state, provider cadence and evidence counts were unchanged, with one deferred opportunity and zero runs/experiments. No source or runtime defaults changed. Product-owner manual UX review remains pending; see the sanitized [commissioning report](reports/deployments/bb-096-ux-commissioning-20260823.md).

# BB-097 implementation (2026-08-23)

Family now has a dedicated `FamilyExperience` composition and no longer renders through `DashboardWidget` in its normal mode. Meal and shopping modules expose an explicit Family presentation that removes the legacy collapsible shell while retaining their existing standalone contract and API behavior. The compact settings icon preserves theme, ordering and visibility controls; meals, shopping, calendar and truthful planned reminders form one continuous mobile-first flow. Obsidian Gold surfaces, borders, accent, typography and the bottom dock were refined against all three supplied local mockups.

Local verification passed 115 frontend tests, the frontend production build, 495 API tests, 32 Sentinel tests, Release build, documentation, Compose and diff gates. GitHub Actions runs 32618788138 and 32619102449 passed. Final commit `cb9c9caa0efc66119616767bc3d9507d9a83562b` was built as Web image `sha256:5f37b814b1ccb03dcd25353721a4ddd42e94a0f5bcea257f2b3389e59775f312` and deployed by recreating only Web without volume removal. The served PWA was opened at 390 × 844, 430 × 932 and 1440 × 900 with real Family data and Obsidian Gold; all four sections and meal tabs were present, no alerts or horizontal overflow occurred, and the dock was mobile-only. Recovery remained healthy/clean. Finance remained `RESEARCH / 0 SEK / NONE`, with zero experiments and unchanged scheduler operation. BB-097 is technically complete; owner visual approval is separately and explicitly pending. See the [implementation report](reports/features/dashboard/bb-097-family-obsidian-gold-reference-20260823.md).

Owner review retained the BB-097 Family architecture but rejected the remaining generic dark-web appearance. A 2026-08-23 premium art-direction refinement now introduces theme-owned canvas, three-level surface, edge/highlight, coherent shadow, accent-tonal and three-level text tokens. Obsidian Gold uses warm charcoal/stone materials and a restrained multi-tone metal accent; Forest Night uses deeper pine/smoked-green materials and bounded organic CSS illumination, rather than an Obsidian recolor. Family typography, tabs, Today marker, settings control and the shared floating dock use the same semantic material contract. The fixed page background and unbounded diagonal pseudo-light were replaced after investigating full-page capture artifacts. Implementation commit `4da8de726963b59dfa467c1f73d1aaf876b9d9d3` passed GitHub Actions run 32624929631. Web image `sha256:bc92125c7b5613dcc9db9b9661bd97f04e01db5119feeb2394463b37f7beff52` is deployed and healthy; normal deployed 390 × 844 and 430 × 932 viewport frames were opened for both dark themes, and Arctic Wind remained readable and overflow-free at 390 × 844. API and Sentinel remained healthy, recovery stayed healthy/clean and Finance remained `RESEARCH / 0 SEK / NONE`. Owner visual approval remains pending.

Real-iPhone owner review on 2026-08-23 remains authoritative over the earlier headless full-page diagnostic: the bright rounded/geometric artifact is still reproducible in iOS Safari full-page screenshots for both dark themes, although it is not visible during normal interaction. BB-097 therefore records `FULL-PAGE IOS SAFARI SCREENSHOT ARTIFACT: STILL REPRODUCIBLE`; no fix is claimed without equivalent iOS verification. The owner-directed polish retains the complete Family architecture and behavior while tuning only material relationships, gold tonal response, Forest atmosphere, metadata hierarchy, meal density, Shopping primary-action weight and dock details. Exactly two normal-viewport iterations per dark theme were opened at 390 × 844, with 430 × 932 and normal scroll checks. Implementation commit `cdad8fcca5ed5742d97aadbb07b54b4628b0b867` passed GitHub Actions run 32626737721 in backend, frontend, documentation and secrets. Only Web was rebuilt and recreated; image `sha256:a4c89f10d3e54c97d4d4fe91118f5235e96d858a49c32ff10311f2c145ed0fa4`, Web, API and Sentinel are healthy. Deployed Obsidian Gold and Forest Night top and normal-scroll viewports were opened at 390 × 844, both themes passed 430 × 932 without horizontal overflow, and Arctic Wind remained readable at 390 × 844. Recovery is healthy/clean and Finance remains `RESEARCH / 0 SEK / NONE` with its enabled scheduler unchanged. The pass is technically complete; owner visual approval remains pending.

The final planned Family craftsmanship pass on 2026-08-23 keeps the owner-accepted architecture, layout, hierarchy and dark-theme direction. It replaces the generic Byt form block with a compact meal-anchored material action panel while preserving automatic replacement, manual choice and cancel behavior. The panel names the selected meal, sends focus to the primary action, closes on Escape, uses 44 px primary/secondary targets and retains a 40 px tertiary dismiss target. Semantic 100 ms press and 180 ms component-motion tokens align active feedback across Family tabs, arrows, settings, Shopping, Calendar and the dock; reduced-motion CSS removes all new nonessential transitions and the panel animation. One primary visual pass and one defect-only correction pass were opened for Obsidian Gold and Forest Night at 390 × 844 and 430 × 932, including the required Söndag/Idag/Smashed Burgers/Byt-open state. No overflow or long-meal-name collision was found. Commit `d9091574fad84d156d22d719d2286a0db473f465` passed GitHub Actions run 32629227326 across all four jobs. Only Web was rebuilt/recreated; image `sha256:3538f738ef44690aacc61d70e6752c008a7ce4ee60403403be65477db18f2ad1`, Web, API and Sentinel are healthy. Both dark themes passed opened deployed 390 × 844 and 430 × 932 Byt-open and lower-section frames; Arctic Wind passed a deployed 390 × 844 smoke render. Finance remains `RESEARCH / 0 SEK / NONE`; owner visual approval remains pending.

## Finance unattended-research commissioning (2026-08-22)

The actual appliance was updated to the accepted BB-092–095 chain and explicitly commissioned with the deployment-only scheduler override at 20:55 UTC. Source remains default-off; deployed cadence is 60-minute checks, 02:00 UTC and maximum two experiments. Recovery was healthy/clean, the real Sentinel-backed governor returned `ALLOW`, maintenance pause was false, the eight-instrument EODHD cadence remained unchanged and Finance stayed `RESEARCH / 0 SEK / NONE`. The first natural deterministic opportunity deferred on `featureLineageIncomplete`, producing zero runs and zero experiments. One controlled API restart preserved exactly one opportunity, zero runs and zero experiments with no incident or attention state. EODHD backup eligibility remains restricted/subscription-only. Narrow correction `5cc35f415c5314f702c719f0114979ac3f8a3821` maps that defer reason truthfully in aggregate operations status, passed GitHub Actions run 32598337042 and is deployed as API image `sha256:8f916403e8a672a7fe1aa6dc76e14cbcb86c9312bbb1e1fb35070af847153a2a`; it does not change the authoritative readiness gate. Scheduler rollback is the deployment override `FINANCE__RESEARCHSCHEDULER__ENABLED=false` plus API recreate, without deleting history.
