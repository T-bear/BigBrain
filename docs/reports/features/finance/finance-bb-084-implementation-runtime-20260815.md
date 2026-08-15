# BB-084 dataset intake implementation and runtime

## Metadata

- Date: 2026-08-15
- Baseline: `d9eadbb3f3533e70e57cd12ad6890b06e36e4772`
- Scope: implementation, deployment and read-only Finance runtime

## Status

Implemented, automatically tested, deployed and restart-verified on 2026-08-15 from baseline
`d9eadbb3f3533e70e57cd12ad6890b06e36e4772`. Finance remains `RESEARCH`.

The pipeline persists candidate state, immutable manifest, artifact/file hashes, license and
provenance evidence, schema/coverage/quality, promotion policy/result and canonical revision.
HTTP download has bounded retry/size/disk gates. CSV/ZIP handling rejects traversal, excessive
file count/expanded bytes, huge lines, malformed encoding/fields, binary masquerade and unsafe
ticker prefixes. Archive scripts are never extracted or executed.

## Evidence

## Runtime result

- WIKI: 235,562,224 bytes; SHA-256 `dd5127…a43e`; 2,155,310 accepted rows,
  3,186 instruments, 2014-01-02–2016-12-19; 11,295 rejected anomalies; public domain;
  promoted AAPL/MSFT/JPM/XOM/JNJ only. Canonical revision `wiki-5713d7dccfa38f56` contains
  3,722 rows. QQQ/SPY/IWM were unavailable in this snapshot. EODHD overlap was insufficient.
- Zenodo: 4,084,638 bytes; SHA-256 `214fa7…1180`; 207,158 valid rows, 21 series,
  1982-01-04–2025-03-28; CC BY curatorial license but underlying Yahoo provenance unknown;
  manual review, zero promoted rows.
- Quarantine currently occupies 268,824,339 bytes. Finance SQLite is currently 1,344,012,288
  bytes after derived evidence; no pre-slice database-size baseline was captured, so exact
  storage growth is not claimed.

WIKI produced feature revision `feature-3833eb92bb641e51`: 78,162 values, 76,567 available,
1,595 warmup, zero quality issues, checksum `sha256:3833eb92…6c132`, 730 ms. Six unchanged
BB-080 strategy/cost runs covered 748 sessions/five instruments, 1,172 fills and 27,992 events
in 5,645 ms. Run IDs: `backtest-f9da712aacbe9d80`, `backtest-01c5c0c7c63971db`,
`backtest-8a8b9dbcbe468652`, `backtest-ddbc1348b3779bc5`, `backtest-7590336784fae4e0`,
`backtest-98fc189d4e9a4bc1`.

Robustness v3 keeps the 64-run per-evaluation bound and deterministically caps long-history
walk-forward windows. It built 177 unique referenced runs, 44 windows, seven parameter variants
and 15 cost variants in 48,883 ms. Buy-and-hold is `MIXED` (39.89; 523/175 train/test; 22
windows), SMA is `MIXED` (44.09; 11 windows), and momentum is `FRAGILE` (28.13; 11 windows).
Evaluation IDs are `evaluation-afb220dd7d33f6af`, `evaluation-b9d75845712147eb` and
`evaluation-2eaba97c172da520`. Longer history therefore resolves `INSUFFICIENT_DATA`; it does
not validate profitability or authorize trading.

Read-only `GET /api/v1/modules/finance/datasets` and Finance Web distinguish REAL CURRENT EOD,
HISTORICAL ARCHIVE DATA and QUARANTINED/NOT APPROVED. A real interrupted validation recovered
to downloaded state and retried without partial publication. Container recreation retained
both manifests, the WIKI revision and derived evidence. EODHD revisions were not mutated;
feature building now defaults to EODHD-only or an explicit exact revision set, preventing
implicit stitching.

No paid service, broker, order, PAPER, LIVE or AUTO capability was added. Remaining safe work:
BB-085 should add provider-tagged backup/restore and quarantine-cleanup drills, including
independent WIKI retention and explicit manual-review expiry, without expanding the universe.

## Changes

Domain validation, API persistence/maintenance/read surfaces, Finance UI, Compose volume path,
robustness budget semantics, tests, ADR, architecture, operations and reports changed together.

## Security

Quarantine input is non-executing and bounded. No credential, raw market row, private address or
sensitive host path is published. Finance remains RESEARCH.

## Remaining work

BB-085 provider-tagged backup/restore and quarantine-cleanup drills remain; no backup job was
silently activated by BB-084.

## Resumption

Use the immutable candidate/revision IDs above and the dataset-intake architecture document.

## Sanitization

Detta är en sanerad GitHub-version. Runtime facts are aggregate and contain no raw dataset rows.
