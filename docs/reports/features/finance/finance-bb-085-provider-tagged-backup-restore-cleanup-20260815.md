# BB-085 provider-tagged backup, restore and quarantine cleanup

## Metadata

- Date: 2026-08-15
- Baseline: `69aad9dcc265e153038f5220a2084e27476d58a6`
- Mode/budget: `RESEARCH`, 0 SEK; no broker, order, PAPER, LIVE or AUTO capability

## Status

Implemented, automatically tested, deployed, bounded-runtime-drilled and restart-verified.
Final published CI status is recorded separately in current status when complete. No manual
product approval of UI presentation is claimed.

## Changes

`finance-provider-backup-v1` classifies canonical/derived evidence by provider, product,
policy, rights, provenance, retention/deletion duty and exact revision lineage before export.
Stable open JSON data plus a separate SHA-256 manifest move through staging and become visible
only as `Complete`. Unknown rights are excluded. Restore verifies a copy in isolated staging,
compares revision/observation/lineage identity and removes staging without touching canonical
memory. Low-disk and incomplete-state gates fail closed.

WIKI `PublicDomain/Indefinite` is eligible. EODHD Free is inventoried as
`OwnerAcceptedPersonalResearch/SubscriptionOnly/DeleteAtSubscriptionEnd`, restore-eligible
only inside that governed lifecycle and excluded from WIKI's indefinite backup. Derived rows
are selected only when every exact source revision belongs to the backup.

## Evidence

### Bounded runtime drill

- Source revision: `wiki-5713d7dccfa38f56`
- Coverage/observations: 2014-01-02–2016-12-19; 3,722
- Backup: `finance-backup-3b093584173d74f6`, 495,977,922 bytes
- Artifact SHA-256: `3b093584173d74f63d7061b4a1a8563a39ad0d92d34918d5ccee82a4ffcf67be`
- Lineage: feature `feature-3833eb92bb641e51`, 183 exact-source backtest runs and three
  robustness evaluations
- Verification: complete manifest, artifact size/hash and semantic counts passed
- Restore: one revision and 3,722 observations matched; isolated staging removed
- Corruption: appended-byte staging copy produced checksum mismatch and restore rejection;
  corrupted copy removed; original backup re-verified
- Quarantine cleanup: no retention-aged rejected runtime candidate existed, so zero payloads
  were removed; the unresolved Zenodo candidate remained manual review; one promoted canonical
  revision was reported protected. Fixture drill separately deleted a rejected payload while
  retaining its manifest/hash, preserved manual review and canonical rows, and repeated safely.
- External/provider requests: zero

### Surfaces and verification

`GET /api/v1/modules/finance/backups` exposes sanitized read-only inventory. The Finance Web
view distinguishes `BACKED UP`, provider-restricted, canonical and quarantine state without
backup/delete controls. Dataset catalog adds cleanup eligibility/state and manifest-retained
metadata. Maintenance commands are trusted one-shot operations only.

Network-free tests cover source classification, EODHD deadline semantics, unknown-rights
exclusion, manifest determinism, idempotence, atomic/incomplete recovery, checksums,
corruption rejection, restore identity, source-only derived lineage, disk gates, rejected and
manual-review cleanup, cleanup interruption and canonical protection. Full regression/build,
documentation, secret and CI outcomes are recorded in repository status and the published run.

## Security

No provider call, credential, raw market row, private address or sensitive filesystem path is
published. Backup artifacts remain in the Finance-owned runtime volume and are not committed.
Downloaded content is never executed. Finance remains RESEARCH.

## Remaining work

The first complete lineage backup is intentionally verbose JSON (about 496 MB); no compression
or cloud storage was added. The drill is isolated restore verification, not destructive
replacement of the healthy canonical database. Runtime had no eligible rejected payload to
delete; deletion behavior is proven with sanitized filesystem/SQLite fixtures.

Recommended next slice: **BB-086 — legitimate zero-cost SPY/QQQ/IWM historical coverage**,
using BB-084 quarantine and BB-085 protection gates. It must remain read-only RESEARCH; no
implicit stitching or trading authority.

## Resumption

Start from ADR 0028, the Finance backup/restore runbook and `FinanceDataProtectionStore`.
Re-run the sanitized inventory before any future retention or restore decision.

## Sanitization

Detta är en sanerad GitHub-version. It contains aggregate identities/checksums and no bulk
market data, credentials, private runtime identities or raw logs.
