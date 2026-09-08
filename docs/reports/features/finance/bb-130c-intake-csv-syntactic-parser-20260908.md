# BB-130C — intake CSV syntactic tokenizer boundary

Detta är en sanerad GitHub-version. Source inspection and isolated synthetic fixtures only.

## Metadata

- Date: 2026-09-08.
- Baseline main: `39af684e8aa8a84b40caff4525b430794838c6eb`.
- Branch: `bb-130c/intake-csv-syntactic-parser`, directly from verified main.
- Scope: one concrete physical-line field tokenizer; not the whole ParseCsv method.
- Historical [blocker evidence](https://github.com/T-bear/BigBrain/blob/6ad73c53e2d2e87df99a8ab2840ab20d73106d5b/docs/reports/features/finance/bb-130c-intake-csv-parsing-boundary-20260907.md)
  remains **BLOCKER HANDOFF — NOT MERGEABLE**, excluded from ancestry.
- Accepted [column-zero correction](bb-130c-csv-corporate-action-column-zero-fix-20260907.md),
  [quarantine boundary](bb-130c-intake-safe-artifact-boundary-20260907.md) and
  [canonical identity v2](bb-130c-canonical-product-revision-identity-v2-20260907.md) are preserved.

## Status

**REVIEW CANDIDATE ONLY — NOT MERGED TO MAIN**. Implemented and locally verified;
this committed branch is published for exact-SHA architect review and owner merge approval.
No CI, main acceptance, deployment or runtime verification is claimed for this candidate.
Finance **RESEARCH / 0 SEK / NONE**. BB-130C overall remains partial; BB-130D is separate.

## Evidence

### Characterization and ownership

Before extraction, the store owned a self-contained Csv helper used at five call sites:
normal headers/rows, archive headers/rows and content detection. It takes a string and returns
a list of raw fields, with no candidate, file, database or identity dependency. This is the
smallest demonstrably independent syntax responsibility. The whole ParseCsv method is not moved.

`FinanceDatasetCsvTokenizer.Tokenize` now owns that same algorithm. There is no forwarding
wrapper, interface, DI registration, alternate parser pipeline or public contract. Existing
call sites invoke it explicitly; all surrounding statements and ordering remain unchanged.

| Contract | Preserved behavior and evidence |
| --- | --- |
| Raw fields | Comma separates fields only outside quotes; empty input produces one empty field, consecutive/trailing commas retain empty fields. Whitespace and Unicode content are not normalized. New five-case tokenizer theory freezes these outputs. |
| Quotes | Quote toggles quoted state; doubled quote while quoted appends a literal quote and advances one character. Commas inside quotes remain field content. Existing unterminated-quote test plus new escaped-quote cases pass before/after. This is the existing algorithm, not a new RFC-compliance claim. |
| Failure | Open quoted state at line end throws InvalidDataException with the same message. No multiline-record support is added. Three new structural tests retain missing-header, field-count and quote errors, Validating state, artifact checksum and zero canonical rows. |
| Physical reading (store retained) | FileStream remains read-only/read-shared; StreamReader retains strict UTF-8 fallback, BOM detection and 65,536 buffer. BOM detection is not a UTF-8-only guarantee. Lines are read synchronously; no cancellation token. New integration fixture includes a UTF-8 BOM. Other decoding semantics are source-identical, not newly broadened. |
| Bounds (store retained) | Header limit 64,000 and data-line limit 1,000,000 characters, checked after ReadLine. Archive count/expanded-byte/path protections and detection's 4,096-byte sample stay unchanged. No new allocation/cancellation guarantees. |
| Headers (store retained) | Trim stored names; lookup removes non-alphanumerics and lowercases invariantly, with ordinal dictionary and existing duplicate-name exception. Required ticker (preferred) or symbol, date, OHLC, volume; optional adjclose/exdividend/splitratio. New normalized/quoted-header integration test passes. |
| Optional action presence (store retained) | Existing TryGetValue booleans retain column zero for both action fields; absent stays absent. All five accepted correction cases pass before/after, including nonzero controls, candidate-bound annotations and physical row counts. |
| Lexical values (store retained) | Invariant yyyy-MM-dd DateOnly parsing and NumberStyles.Float decimal parsing; no scale normalization. Empty/blank adjusted close remains null; positive-value validation stays where it was. New integration fixture covers exponent syntax and empty/blank adjusted values. Existing culture/calendar/scale identity suite passes unchanged. |
| Domain decisions (store retained) | Missing/invalid row counts, symbol normalization, OHLC consistency, duplicate/conflict policy, source ordering, missing sessions, jumps, CompareExisting SQL/EODHD comparison and candidate limitations remain source-identical. Process-culture key interpolation is not cleaned up or newly certified by this extraction. |
| Orchestration (store retained) | Acquisition, quarantine usage, checksum binding, archive preparation, lifecycle, rights, eligibility, gates, canonical v2/legacy identities, observations and manifests/SQL stay in the store. Owner ingress and XLSX research handling are unchanged. |

## Deterministic before/after evidence

Nine new test cases were added before touching production: five tokenizer cases, three
structural/lifecycle failures and one end-to-end normalized/quoted-header comparison.
The same 80 focused cases passed against unchanged production and after extraction; only
the direct tokenizer test target was renamed. No assertions or expected identities were relaxed.

The new integration fixture compares ordinary CSV with BOM/quoted headers, exponent-form open,
blank adjusted fields and ignored quoted annotation text. Both promote to the pinned synthetic
`dataset-v2-31c6ed4a006c7514de08409dca1998be6c8e7af9ee2e06a72cb349f69aa15522`,
with two physical canonical rows, zero invalid OHLCV and zero duplicates. Distinct raw bytes
have distinct artifact hashes; each is checked against the unchanged SHA-256 helper.
Existing exact v2 bytes/digest, scale/culture/calendar cases, terminal replay and historical
`wiki-dde8381ccbe9ccc5` snapshot assertions pass before/after without migration or rewrite.
Corporate-action preservation and candidate state/checksum assertions also pass unchanged.
This is deterministic fixture evidence, not a performance or production-incidence claim.

## Changes

- `src/BigBrain.Api/Finance/FinanceDatasetCsvTokenizer.cs`: existing tokenizer extracted into a concrete internal type.
- `src/BigBrain.Api/Finance/FinanceDatasetIntake.cs`: five explicit calls and removal of the old helper only.
- `tests/BigBrain.Api.Tests/FinanceDatasetIntakeTests.cs`: nine characterization cases and direct tokenizer target updates.
- TESTING, STATUS, BACKLOG, Finance module, BB-130 plan, catalog, recovery and this report.

## Verification

Before and after extraction: **80/80 focused tests passed**, zero failures/skips.

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~FinanceDatasetIntakeTests|FullyQualifiedName~FinanceDataProtectionTests|FullyQualifiedName~CanonicalDatasetRevisionIdentityTests|FullyQualifiedName~FinanceResearchDatasetTests' --logger 'console;verbosity=minimal'
dotnet build BigBrain.slnx --configuration Release --no-restore
dotnet test BigBrain.slnx --configuration Release --no-build --logger 'console;verbosity=minimal'
node scripts/verify-documentation.mjs
git diff --check
git diff --cached --check
git diff --cached | docker run --rm -i --network none zricethezav/gitleaks:v8.28.0 stdin --redact --no-banner
```

Release solution build: **0 warnings / 0 errors**. Full API **658/658**, Sentinel **32/32**,
zero failures/skips. Documentation verification passed: **230 Markdown files / 90 unique backlog IDs**.
Working/staged diff checks passed; staged Gitleaks v8.28.0 found no leaks. The first documentation
check identified two required report headings; these were corrected and verification rerun successfully.
No Web-consumed contract changed: no Web rerun is required. No Compose/runbook change.
Branch push alone does not trigger this repository's main/pull-request CI; no CI claim.

## Security

Schema, canonical identity v2, legacy identities, rights/provenance, provider and scientific
semantics are untouched. No production data access, acquisition, migration, rekey, aliases,
provider activation, deployment, broker/order/PAPER/LIVE/AUTO or capital allocation.
Only temporary synthetic files/databases were used; no raw private evidence is published.
Finance **RESEARCH / 0 SEK / NONE**. Historical column-zero production incidence remains UNKNOWN.

## Remaining work

The store still owns physical CSV reading, header/value interpretation, archive preparation,
validation/comparison, candidate policy, acquisition, artifact recording, promotion and SQL
lifecycle/catalog persistence. Further boundaries require separate characterization/approval;
none is started. Finance persistence/schema/composition, remaining frontend responsibilities
and BB-130D stay separate. No new defect or performance improvement is claimed.

## Resumption

Review the exact remote branch SHA, then obtain explicit owner merge approval; do not merge
historical blocker branches. Use [canonical recovery](../../../operations/codex-recovery.md)
and the [BB-130 plan](../../../architecture/bb-130-stabilization.md). Stop after review publication.
