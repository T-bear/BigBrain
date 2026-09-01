# BB-127 Owner Research Dataset Capabilities & XLSX Intake

Detta är en sanerad GitHub-version. Den innehåller inga hemligheter, privata sökvägar, råa
workbooks, marknadsrader eller känsliga runtimeuppgifter.

## Metadata

- Date: 2026-09-01
- Baseline: `b801712fca7eb0aa7cc3623a2198ac66c3ed8c1b`
- Package SHA-256: `ef16c8763d6f4eea6b0bb03d9986349e86c0dba992472ef2d1c51cd29d3b6383`
- Workbook SHA-256: `ca59dc3440ea148359f83a84d080fdec502041913f07b8dad97790284f9b8b73`
- Safety: Finance `RESEARCH / 0 SEK / NONE`

## Status

Implementation, automated tests, isolated real-workbook inspection and bounded research plumbing
verification are complete. Appliance deployment/runtime and GitHub CI evidence are recorded during
publication; canonical data remains unchanged.

## Architecture and decisions

Existing capability could not express “purpose X may consume this noncanonical immutable
revision with limitations” because the BB-084 catalog had only canonical promotion state and the
backtest reader selected canonical observations. BB-127 adds two tables inside the existing
Finance SQLite store for immutable research revisions/observations, a purpose-specific
`owner-research-eligibility-v1` policy, and optional research lineage on the existing backtest
configuration. No alternate quarantine, canonical store, engine, provider adapter or promotion
path exists.

Canonical and research decisions are orthogonal. Owner policy evidence
`OWNER-GOOGLEFINANCE-WORKBOOK-2026-09-01-V1` approves maximum technically/semantically compatible
private research. It leaves external rights, independently verified RAW semantics, corporate
actions and canonical historical identity `Unknown`. Explicit external denial or technical
failure is ineligible. Close-only context/FX cannot enter an OHLCV backtest; unresolved
price/actions cannot claim accurate adjusted total return. `CURRENT_METADATA` remains a current
snapshot and is excluded from all historical partitions.

## Safe XLSX intake

The owner ZIP and inner XLSX remain immutable acquisition evidence. OpenXML is pre-screened for
entry/expansion/XML/sheet/row/column/cell bounds, traversal, macro/binary content, embeddings and
external relationships. Existing pinned ClosedXML reads only `CachedValue`; formulas and exported
`__xludf.DUMMYFUNCTION(...)` are never evaluated. `EXPORT_MANIFEST` claims are cross-checked
against actual data sheets. Support sheets are excluded.

## Changes

- Extended the existing intake store with immutable research revisions/observations.
- Added a purpose-specific eligibility policy and read-only research dataset catalog endpoint.
- Added bounded XLSX package handling and an explicit maintenance backtest selector.
- Extended existing backtest lineage additively; legacy JSON remains readable.

## Evidence

| Class | Datasets | Source rows | Accepted |
| --- | ---: | ---: | ---: |
| Equity/ETF OHLCV | 9 | 93,079 | 93,075 |
| Index/FX close-only | 9 | 61,270 | 61,270 |
| Total | 18 | 154,349 | 154,345 |

Independent parsing reproduced MSFT `1995-08-24` and JNJ `2006-02-01`, `2006-10-19`,
`2006-10-24` as inconsistent OHLC. These four rows remain in the immutable workbook, are reported
and are not repaired or normalized into research observations. All 18 datasets retain external
rights/historical-identity limitations; OHLCV additionally retains owner-only price-basis and
unresolved corporate-action limitations. Zero canonical datasets/rows were produced.

An existing conservative `buy-and-hold/v1` run consumed GOOG revision
`research-a7f1880044c83ede` for 2014-03-27–2026-08-31 using
`daily-next-session-open-v2`. Run `backtest-34d7f56c997c2236` retained candidate, package/workbook,
dataset fingerprint, source/sheet, owner evidence, external-rights state, purpose and limitations.
The numerical return is intentionally not published or interpreted: this is deterministic plumbing
evidence over owner-claimed RAW/unresolved corporate-action data, not profitability evidence.

## Verification

- Focused tests: 8/8 passed.
- Full API tests: 595/595 passed.
- Release solution build: passed with zero warnings/errors.
- Deployment/runtime and GitHub CI: pending publication evidence.

## Security

No provider was contacted, no autonomous research was triggered, no strategy was added/tuned and
no broker/order/PAPER/LIVE/AUTO capability changed.

## Remaining work

- External rights, authoritative identity, price basis and corporate actions remain unresolved.
- Current metadata has no PIT history; daily OHLCV does not establish intraday liquidity.
- Close-only context is not an OHLCV strategy input.

## Resumption

Continue only from the published GitHub and runtime evidence. Do not promote these revisions or
reuse them for a purpose whose capability is ineligible without a separate evidenced decision.
