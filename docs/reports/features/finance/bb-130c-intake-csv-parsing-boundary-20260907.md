# BB-130C — CSV parsing characterization: corporate-action column-zero blocker

Detta är en sanerad GitHub-version. Synthetic fixtures and source-code evidence only.

## Metadata

- Date: 2026-09-07.
- Baseline main: `81c80d1c7e361086b5871de512b5ef3855a70f49`.
- Branch: `bb-130c/intake-csv-parsing-boundary`, directly from verified main.
- Original scope: characterize and, only if safe, extract one syntactic CSV responsibility.
- Accepted prerequisites: [quarantine extraction](bb-130c-intake-safe-artifact-boundary-20260907.md)
  and [canonical product/v2 identity](bb-130c-canonical-product-revision-identity-v2-20260907.md).

## Status

**BLOCKED — CSV CORPORATE-ACTION EVIDENCE LOSS AT COLUMN ZERO**.
**BLOCKER HANDOFF — NOT MERGEABLE**.

Characterization reproduced a pre-existing correctness defect before any production edit.
Extraction is stopped. The branch contains only a deliberately failing characterization
test and documentation. It is not a green implementation, accepted correction or merge candidate.
No deployment, runtime verification or production-data inspection occurred.

## Evidence

### Reproduced on unchanged production

One theory has two isolated cases: move `ex-dividend`, or move `split_ratio`, to the first
column while preserving every named cell value. Each case first promotes a control CSV with
both optional columns after the required columns, then a separately identified reordered
candidate with the same source/product and canonical content. Both are synthetic one-row
fixtures with explicit all-pass rights metadata; no provider is contacted.

| Assertion | Result in both cases |
| --- | --- |
| Control and reordered candidates | Both Promoted |
| Canonical revision identity | Equal; accepted v2 content contract unchanged |
| Physical canonical rows / reported promoted rows | One / one |
| Artifact hashes | Different, as expected for reordered bytes |
| Control action evidence | Dividend `1.25`, split ratio `2` retained |
| Reordered evidence | The first-column value is stored as an empty string |
| Both original action values retained after reordering | FAIL: expected one matching row, actual zero |

`ParseCsv` finds both indices using `TryGetValue`, but discards the success booleans.
It constructs `ImportBar` using `ex > 0 ? f[ex] : null` and
`split > 0 ? f[split] : null`. Zero is a valid first-column index as well as the default
value when lookup fails. The parser therefore silently drops a present first-column value.
Promotion writes a null annotation as an empty string when the other annotation is present.
These two paths are independently reproduced; the defect is not a culture test failure.

The all-pass promotion gate does not detect this loss. The accepted v2 serializer deliberately
excludes corporate-action annotation strings: they are candidate-bound evidence, not canonical
price identity. Equal v2 IDs are expected here and must not be changed to hide this parser defect.
Source inspection also shows this evidence is included by FinanceDataProtection backup selection.
No production incidence, historical damage or downstream scientific-result change is established.

### Pre-extraction responsibility map — source inspection, not a complete passing characterization

| Area | Current behavior / owner |
| --- | --- |
| Reader/tokenizer | ParseCsv uses a read-shared FileStream and BOM-detecting StreamReader with strict UTF-8 fallback; reads physical lines. Csv splits commas with quote/escaped-quote handling; unterminated quotes throw. No cancellation token in this synchronous path. |
| Headers | Trimmed stored headers; lookup strips non-alphanumerics and lowercases invariantly. Required ticker (preferred) or symbol, date, OHLC and volume. Duplicate normalized names throw during dictionary creation. Optional adjclose, exdividend, splitratio. The latter two have the reproduced zero-index defect. |
| Numbers/dates | DateOnly.TryParseExact yyyy-MM-dd with InvariantCulture; decimal.TryParse NumberStyles.Float with InvariantCulture. Volume is decimal. Numeric scale is not normalized. Missing/blank adjusted close is null; nonblank must parse positive. |
| Symbols and bad rows | Symbols trim/uppercase invariantly, reject empty/over-32/formula-leading values. Field-count errors throw; missing required values or invalid dates/numbers increment counters and skip rows. |
| Validation mixed into ParseCsv | Nonpositive prices, negative volume, OHLC consistency, duplicate/conflicting keys, source order, missing market sessions and price-jump heuristics. These are not all syntactic parser ownership. |
| Ordering/duplicates | Source order retained for accepted first occurrences; duplicates compare ImportBar values ignoring source row number. Key interpolation currently uses process culture; no new culture-compatibility claim is made by this stopped checkpoint. V2 independently orders and serializes invariantly. |
| Bounds | Header 64,000 chars; each data line 1,000,000 chars, checked after ReadLine. Existing archive file/expanded-size bounds precede parsing. No new total-row or cancellation guarantee. |
| Store/data dependencies | ParseCsv invokes CompareExisting (SQL/EODHD comparison) and builds candidate-derived limitations. BuildGates, canonical product/v2 identity, promotion SQL, manifests and lifecycle remain store responsibilities. |
| Lifecycle | Artifact recording/checksum binding and terminal replay precede parsing. PrepareCsv runs in Inspecting; ParseCsv runs in Validating. Direct failures propagate; owner scanner catches supported inspection errors and records rejection. |
| Paths | Generic and fixed WIKI canonical CSV paths use this parser. Owner CSV uses review-only inspection. ZIP preparation selects/combines CSV before it; XLSX has separate research handling. No owner promotion or XLSX generalization is authorized. |

No parser was extracted. Pure field tokenization may be a later small boundary; moving the whole
ParseCsv method would absorb validation, candidate policy and SQL and violate the requested scope.
Further characterization is deferred until the correctness blocker has an architect decision.

### Verification

Targeted reproduction: two failing theory cases, both at the final preservation assertion;
all preceding lifecycle, control-evidence, canonical-row and explicit-empty-field checks passed.

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~CorporateActionEvidenceMustSurviveCsvHeaderReordering' --logger 'console;verbosity=normal'
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~FinanceDatasetIntakeTests|FullyQualifiedName~FinanceDataProtectionTests|FullyQualifiedName~CanonicalDatasetRevisionIdentityTests|FullyQualifiedName~FinanceResearchDatasetTests' --logger 'console;verbosity=minimal'
node scripts/verify-documentation.mjs
git diff --check
git diff --cached --check
git diff --cached | docker run --rm -i --network none zricethezav/gitleaks:v8.28.0 stdin --redact --no-banner
```

Final focused suite: **66 passed, 2 deliberately failed, 0 skipped (68 total)**. Existing v2
vectors/legacy replay and quarantine/research tests pass unchanged. Production diff is empty.
Documentation verification passed (229 Markdown files / 90 unique backlog IDs); working/staged
diff checks passed; staged Gitleaks v8.28.0 found no leaks. No full-suite,
Sentinel, solution Release or Web rerun is claimed: implementation stopped, production is unchanged,
and AGENTS permits a clearly labelled failing characterization-only blocker handoff. No CI success
claim. Branch push alone does not trigger the main/pull-request workflow.

## Changes

- `tests/BigBrain.Api.Tests/FinanceDatasetIntakeTests.cs`: one two-case header-order regression.
- This report and catalog; TESTING, STATUS, BACKLOG, Finance module, BB-130 plan and recovery.
- Production files changed: **none**. No extracted type or production fix.

## Security

Only isolated temporary synthetic files/databases are used. No production Finance evidence,
credentials, private identifiers/paths, market-data payloads or raw operational logs are published.
Schema, quarantine, artifact algorithms, canonical product/v2/legacy identities, rights/provenance,
eligibility, provider and scientific behavior remain unchanged. Finance **RESEARCH / 0 SEK / NONE**.
No acquisition, deployment, rekeying, aliases, migration, broker/order/PAPER/LIVE/AUTO work.

## Remaining work

Architect review must decide the bounded future correction for optional-column presence and
evidence preservation. Do not rewrite historical evidence or alter canonical identity as part
of this handoff. Production incidence and historical impact remain UNKNOWN. Retain old blocker
branches as NOT MERGEABLE; do not merge this branch either. CSV extraction #2 is not complete;
remaining intake, persistence/schema/composition and BB-130D work remain separately scoped.

## Resumption

Review the exact published branch SHA and this test. A correction needs separate owner/architect
authorization and a fresh verified-main checkpoint; do not import the deliberately failing test
unchanged into mergeable history. Continue using [canonical recovery](../../../operations/codex-recovery.md)
and [BB-130 plan](../../../architecture/bb-130-stabilization.md). No next checkpoint starts here.
