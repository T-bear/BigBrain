# BB-124 Anti-Overfitting & Out-of-Sample Governance

Detta är en sanerad GitHub-version. Den innehåller inga hemligheter, privata adresser, råa marknadsrader eller känsliga runtimeuppgifter.

## Metadata

- Date: 2026-09-01
- Baseline: `386c8766baa0306589e8d5b1fc88c0f81c201c95`
- Scope: BB-049 scientific validation completion plus BB-158 backlog-only registration
- Safety: Finance `RESEARCH / 0 SEK / NONE`

## Status

Implementation, automated verification, API-only deployment and bounded runtime validation are complete. Publication and CI evidence are recorded during finalization. BB-049 is complete for the current daily-data research scope; longer/less-biased history remains a data limitation rather than a bypassable gate.

## Evidence

### Existing capability and gap matrix

| Capability | Before BB-124 | Result |
| --- | --- | --- |
| Chronological train/test + embargo | Implemented by BB-081 | Reused |
| Expanding walk-forward | Implemented by BB-081 | Reused on development evidence |
| Parameter/cost sensitivity | Implemented by BB-081/123 | Reused; BB-123 v2 friction retained |
| Feature knowledge-time/no-lookahead | Implemented | Reused and regression-tested |
| Immutable run/evaluation lineage | Implemented | Extended additively; legacy payloads remain readable |
| Explicit train/validation/holdout | Missing | Implemented as deterministic 60/20/20 after two embargoes |
| Holdout freshness lifecycle | Missing | Implemented as `UNTOUCHED / EVALUATED / CONTAMINATED` |
| Parameter trial/selection population | Partial | Every bounded candidate and validation criterion retained |
| Multiple-hypothesis control | Attempts visible; DSR/PBO not evaluable | Conservative family-breadth fail-closed gate; no fabricated p-values |
| Seeded controls | Deferred | Deterministic isolated engineering controls implemented |
| Survivorship/history correction | Blocked by data | Remains explicit; statistics do not repair source bias |

Existing capability could not preserve a fresh validation-only selection and single-use holdout because BB-081 used one test scope for diagnostics and had no durable holdout lifecycle. BB-124 extends that engine and its existing SQLite result JSON; it creates no second backtester, robustness engine, experiment platform or schema.

### Governance semantics

Usable sessions are split chronologically 60/20/20 with a 50-session embargo between train/validation and validation/holdout. Train permits bounded exploration; validation alone chooses from the declared fixed parameter family; holdout remains untouched until selection freezes and is evaluated once. Existing same-plan evidence is returned idempotently. A materially changed later plan over the same exact lineage/date/strategy scope observes prior use and classifies the holdout as contaminated.

`family-breadth-fail-closed-v1` requires at least 75% positive validation excess results and a positive family median before the selected candidate can pass the selection gate. It is deliberately transparent and conservative. It is not a p-value, Bonferroni/Holm/FDR substitute, DSR or PBO. DSR and PBO/CSCV remain `NOT_EVALUABLE` because their required inputs are absent.

Controls are deterministic: seeded no-signal, future-knowledge leakage, selection among many noise candidates, regime fragility and a deliberately causal positive engineering series. Controls never mutate or relabel canonical market evidence. Positive synthetic behavior is engineering evidence only, never a tradable claim.

## Changes

- Extended existing robustness plan/result contracts and deterministic evaluator.
- Added persistent same-lineage holdout reuse detection through existing immutable evaluations.
- Added selection/holdout state to the read-only robustness summary.
- Upgraded autonomous integrity to v2: selection must pass and holdout must be fresh-at-selection then evaluated.
- Added deterministic scientific regression coverage.
- Registered BB-158 Media URL Import & Audio Extraction as backlog-only with rights and SSRF/media-processing boundaries. No Media implementation occurred.

## Security

No provider call, market-data acquisition, autonomous research trigger, broker, order, PAPER, LIVE, AUTO, execution authority, credential or paid capability was added. No user/Finance data was deleted or rewritten. BB-123 costs remain hypothetical simulation inputs.

## Tests

- Focused robustness/autonomous suite: 28/28 passed.
- Full API suite: 578/578 passed.
- Release solution build: passed with 0 warnings and 0 errors.
- Documentation: 208 Markdown files and 89 unique backlog IDs passed; Compose and diff checks passed.
- Initial CI passed backend/documentation/secrets but found two pre-existing Calendar tests coupled to the wall-clock month. A separate test-only fix pins their August 2026 fixture clock; 6/6 focused and 160/160 full Web tests plus Vite production build pass. No Web production source or deployment changed.
- The next full-history secrets job surfaced one reviewed historical prose false positive (`credentials, authorization`) from commit `04c9271…`. A fingerprint-only `.gitleaksignore` entry suppresses exactly that old finding; the redacted 200-commit local scan contains no other result and global rules are unchanged.

## Deployment and bounded validation

Only API was rebuilt/recreated. Image `sha256:bea523d60e541598e656347ef6663524c20e7420c5fa5f24183fdb55dc281bd0` is healthy and `/health` returns `Healthy`. The repository-native non-autonomous robustness build completed in 484 ms and repeated idempotently: three evaluations, 45 unique runs, zero walk-forward windows, seven parameter candidates and 15 cost variants.

All three v2 strategies return `INSUFFICIENT_DATA`; their partitions are 99 train / 33 validation / 34 untouched holdout sessions after the embargoes. This is the intended fail-closed result, not a strategy ranking. Runtime remained `RESEARCH / 0 SEK / NONE` with zero autonomous runs/experiments. Aggregates after deployment are 113 revisions, 31,898 observations, 17 feature revisions, 929 backtests and 31 robustness evaluations. Existing enabled provider cadence operated independently during the deployment window; BB-124 did not issue an acquisition command or change provider/cadence policy.

## Remaining work

- Longer and diversified point-in-time evidence with improved historical identity/survivorship coverage.
- Statistically valid DSR/PBO only if future evidence preserves their required inputs and assumptions.
- Current daily OHLCV cannot prove intraday liquidity/order-book behavior or repair corporate-action gaps.

## Resumption

Continue from the repository source of truth. Do not tune against a consumed holdout, acquire market data, trigger autonomous research or infer execution authority from a research verdict.
