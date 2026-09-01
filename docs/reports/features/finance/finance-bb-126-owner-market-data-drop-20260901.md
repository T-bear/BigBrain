# BB-126 Owner Market-Data Drop & Quarantine Inspection

Detta är en sanerad GitHub-version. Den innehåller inga hemligheter, privata adresser, råa marknadsrader eller känsliga runtimeuppgifter.

## Metadata

- Date: 2026-09-01
- Baseline/source of truth: `2e6999f86f6863dabb5b7f0225248ea7b5603dfa`
- Finance boundary: `RESEARCH / 0 SEK / NONE`
- Scope: local owner CSV/ZIP detection, quarantine-first inspection and owner-readable evidence

## Status

**IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / RUNTIME VERIFIED / CI VERIFIED.** No external
provider was contacted and no market data was downloaded or promoted. Implementation
`48755ee0ec27fed8f38132ddca050c2d2654e6ca` is published; GitHub Actions run `33492668713` passed.

## Evidence

## Existing architecture reused

BB-084 already supplied `ExternalDatasetCandidate → QuarantineArtifact → ValidationEvidence →
PromotionDecision → CanonicalDatasetRevision`, safe CSV/ZIP parsing, content identity, OHLCV-v1,
effective watchlist mappings, `cross-source-comparison-v1`, 13 promotion gates and restart-safe
SQLite state. BB-126 extends that store with an inspection-only entry point; it creates no second
registry, quarantine, entitlement model, validator or canonical store.

## Owner flow and safety limits

Compose mounts `${FINANCE_MARKET_DATA_DROP_PATH:-./data/finance/market-data-drop}` read-only at
`/finance-data/market-data-drop`. A complete `.csv` or `.zip`, optional same-basename
`.metadata.json`, and explicit `<full-filename>.ready` marker are required. The defaults are
500,000,000 source bytes, 100 archive entries, 1,000,000,000 expanded bytes, 65,536 sidecar bytes
and a 30-second scan interval.

The scanner accepts direct regular files only, rejects symlinks/reparse points and unsafe names,
checks size and modification time around hashing/copying, uses `.partial` quarantine copies and
verifies SHA-256 after copying. Existing ZIP traversal, nested-content, file-count and expansion
limits remain authoritative. Exact artifact-plus-sidecar evidence is idempotent; changed evidence
creates a distinct candidate.

## Inspection and promotion boundary

The existing catalog now exposes filename, checksum/bytes, schema/header interpretation, coverage,
rows/instruments, duplicate/conflict/invalid counts, safe/unmapped identity counts, overlap,
declared license/evidence URL, provenance, limitations and separate technical-quality, rights and
promotion classifications. Owner sidecar statements remain claims. License, provenance, local
retention, price basis and survivorship start unknown and fail closed.

Every stable file is copied into existing quarantine before parsing. Technical validity cannot
override unknown rights or identity. Even an artificial all-pass candidate inspected through the
owner path stops at `APPROVED / READY_FOR_EXPLICIT_PROMOTION_REVIEW`; the method never calls the
canonical promotion operation. Yahoo/yfinance, Tiingo Starter, SimFin, Alpaca and Stooq decisions
from BB-125 remain unchanged by manual possession.

## Changes

- Added the bounded owner-drop scanner/worker and inspection-only store operation.
- Added the read-only Compose ingress mount and environment setting.
- Extended the existing read-only dataset catalog with human-readable inspection evidence.
- Added focused deterministic tests and the owner operations runbook.
- No database schema, provider adapter, UI, strategy, broker, order or execution capability added.

## Security

Dropped content is never executed. Filenames are never interpolated into shell commands. Sidecars
reject unknown properties, non-HTTP(S) URLs, oversized values and common credential patterns; they
must never contain secrets. The mount is local/read-only and has no published port. No provider
restriction, access control or entitlement gate is bypassed.

## Verification

- Focused `FinanceDatasetIntakeTests`: 16/16 passed.
- Full API regression: 586/586 passed.
- `dotnet build BigBrain.slnx -c Release --no-restore`: passed, zero warnings/errors. An initial
  command named the nonexistent legacy `BigBrain.sln`; it changed nothing and was corrected.
- Documentation verifier: 211 Markdown files and 89 unique BB IDs; Compose and diff checks passed.
- API-only image `sha256:e8b7cf7c03baf560740408bc01917162ba928480489490736a94547e94a37311`
  deployed healthy. Docker reports the owner ingress bind as `RW=false`; existing Finance storage
  remains its separate read-write named volume.
- Read-only runtime checks returned the existing two-candidate dataset catalog and
  `RESEARCH / 0 SEK / NONE`. No owner artifact was added for smoke testing.
- The first host directory mode `0750` correctly failed closed but denied the container user.
  Changing only that empty ingress directory to owner-writable/world-traversable `0755` restored
  read access; a full subsequent scan interval emitted no errors. The mount remained `RW=false`.
- Staged gitleaks: no findings.
- GitHub Actions run `33492668713`: backend, frontend, documentation and secrets passed.

## Remaining work

- None within BB-126. Each future owner dataset still requires its own evidence and explicit
  promotion decision; that is expected operation, not unfinished implementation.

## Resumption

No recovery action is required. GitHub holds the implementation and final evidence commits;
unrelated owner working-tree changes remain outside BB-126.
