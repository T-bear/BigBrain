# BB-086 implementation and runtime evidence

## Metadata

- Date: 2026-08-15
- Baseline: `aee00db93614e9183a2aa56d87fabf53db482a50`
- Outcome: documentation/research-only fail-closed completion
- Finance mode/budget: `RESEARCH`, 0 SEK

## Status

Implemented as a research/documentation-only completion. No runtime change was warranted.

## Evidence

Eight ETF-history candidates were investigated; zero were acquired, quarantined or promoted.
No executable, API, Web, persistence, Compose, provider, strategy, threshold or data-protection
contract changed. Deployment/restart is not applicable because runtime bytes are unchanged.

## Runtime and lineage result

- Existing WIKI scan: zero SPY/QQQ/IWM rows; retained artifact reused, no download.
- External artifact acquisition: 0 bytes; storage growth: 0 bytes.
- New observations/revisions/features/backtests/robustness evaluations/backups: 0.
- Price basis and EODHD overlap: not applicable; no candidate reached validation.
- Existing immutable WIKI revision `wiki-5713d7dccfa38f56`, EODHD revisions, feature
  `feature-3833eb92bb641e51`, backtests, robustness and backup
  `finance-backup-3b093584173d74f6` remain untouched.
- Idempotence/persistence: no promotion operation existed to duplicate; read-only runtime inventory
  remained available. No restart was required or claimed.

The existing feature, backtest and robustness engines were not rerun: without a promoted revision,
doing so would create unrelated duplicate evidence and could not answer BB-086's ETF question.
Existing strategies/parameters/costs/thresholds are unchanged.

## Changes

Roadmap, backlog, status, Finance/security architecture and sanitized reports were synchronized.
No code, test fixture, API, UI, database, Compose or provider configuration changed.

## Security roadmap registration

The product-owner decision is recorded as a planned cross-cutting security milestone without an
invented BB number. It combines continuous automated security checks with controlled black-/
grey-box penetration testing. It covers Web/API/auth/access control and common web attacks,
container/port/volume/host privilege, Sentinel identity/request-proof/replay, malicious Finance
CSV/ZIP/provenance/revision/mode-gate input, crash/recovery/persistence tampering and supply chain.
Potentially destructive cases require isolated environments/test data. Completion is mandatory
before real-money LIVE eligibility and a strong gate before meaningful PAPER/execution authority.
BB-086 implements none of those tests.

## Prospective shadow plan

The repository already has provider-neutral four-clock observation and immutable shadow contracts.
The exact next slice should bind authorized current EODHD observations to causal `core-daily-v1`
features and persist timestamped immutable `RESEARCH` predictions, then append later outcomes—no
orders. Prediction source/revision, known-at time, strategy/version/parameters, signal and optional
confidence never change after outcome knowledge. Historical research, prospective scoring and any
future explicitly versioned parameter-update policy remain separate.

Prospective shadow observation should begin in the next Finance slice because repeated zero-cost
mirror discovery no longer has a credible near-term rights path, while authorized current EOD and
the immutable shadow foundation already exist. It cannot make historical ETF robustness sufficient,
but it starts the independent forward-evidence clock without increasing authority.

## Verification and limitations

Local publication gates passed: 185 focused Finance tests; 406 API and 32 Sentinel regression
tests; Release backend build with zero warnings/errors; 109 frontend tests including the Calendar
regression; frontend production build; documentation verification over 152 Markdown files and 86
unique backlog IDs; Compose validation and `git diff --check`. Gitleaks scanned the staged 30.15 KB
BB-086 diff and found no leaks. GitHub Actions run #42 passed frontend, backend, secrets and
documentation for primary publication commit `594d93304e0b9b5f6746f82fef34b3b2cc95419e`.
No provider call is part of ordinary tests.

Historical ETF cross-asset and major-regime evidence remains missing. Any future source stays
source-specific and must traverse BB-084 intake plus BB-085 rights-aware backup classification; no
implicit stitching is authorized.

## Security

No market artifact, market row, credential, account, private runtime identity/path or raw challenge
body is published. Finance remains `RESEARCH`; no broker, order, PAPER, LIVE, AUTO or self-modifying
strategy capability was added.

## Remaining work

Historical ETF coverage remains a known limitation. Begin prospective current-EOD shadow evidence
next; reconsider history only when a new exact rights-cleared source appears.

## Resumption

Start from the existing BB-073 shadow contracts and authorized EODHD current observations. Preserve
four-clock causality, immutable predictions and the absolute no-order boundary.

## Sanitization

Detta är en sanerad GitHub-version. It contains no secrets, raw data or private runtime details.
