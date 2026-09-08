# BB-130C — CSV optional corporate-action column-zero correction

Detta är en sanerad GitHub-version. Only source inspection and isolated synthetic fixtures.

## Metadata

- Work began 2026-09-07; interrupted run resumed and verification completed 2026-09-08.
- Baseline main: `81c80d1c7e361086b5871de512b5ef3855a70f49`.
- Branch: `bb-130c/csv-corporate-action-column-zero-fix`, directly from main.
- Accepted blocker evidence: `6ad73c53e2d2e87df99a8ab2840ab20d73106d5b`, branch
  `bb-130c/intake-csv-parsing-boundary`, **BLOCKER HANDOFF — NOT MERGEABLE**.
- [Original blocker report at its immutable evidence SHA](https://github.com/T-bear/BigBrain/blob/6ad73c53e2d2e87df99a8ab2840ab20d73106d5b/docs/reports/features/finance/bb-130c-intake-csv-parsing-boundary-20260907.md).
  It is preserved unchanged on that branch, not merged/cherry-picked or used as ancestry.
- Accepted contracts: [canonical product/v2 identity](bb-130c-canonical-product-revision-identity-v2-20260907.md),
  [quarantine extraction](bb-130c-intake-safe-artifact-boundary-20260907.md), ADR 0027/0028.

## Status

**ACCEPTED / MERGED TO MAIN / CI VERIFIED**.
Approved candidate `50769e82e3df033066cd307aae78de2856b27092` merged as
`946eb98f139824e65780bf93d41686ea3724c1a3`; [main CI run 34188274546](https://github.com/T-bear/BigBrain/actions/runs/34188274546)
passed backend, frontend, documentation and secrets on 2026-09-08.
Scope is only the authorized optional-column correction. CSV parsing extraction has not resumed.
No deployment or runtime verification is claimed. Finance **RESEARCH / 0 SEK / NONE**.

## Evidence

### Defect and regression first

The parser found `exdividend` and `splitratio` through the existing normalized header lookup,
discarded the lookup booleans, and read values only for indices greater than zero. A valid
first-column value was therefore treated as absent. No new interpretation of headers is needed.

Two fresh regression cases were written on the clean main-derived correction branch, using
the blocker as evidence without its deliberately failing history or assertion of empty bad data.
Each promotes a control with optional columns at nonzero positions and an equivalent reordered
candidate with either ex-dividend or split_ratio at zero. Both must retain dividend `1.25` and
split ratio `2` as candidate-bound evidence, with equal v2 revision IDs and exactly one canonical
observation row. Artifact hashes differ because raw bytes differ; a direct file SHA-256 check
also verifies that the recorded reordered artifact hash is correct.

Three additional cases freeze absent-column behavior: both absent (no action record), only
dividend present, or only split present. Single present columns use nonzero positions as controls;
the missing annotation keeps the existing empty persisted representation.

Before production edits, targeted results were **3 passed, 2 failed, 0 skipped**. Both failures
were the final reordered-evidence assertion: expected one matching action row, actual zero.
All preceding promotion, control evidence, identity and physical-count assertions passed.
After correction, all five cases pass within the **71/71** focused suite, zero failed/skipped.

### Exact correction and compatibility

Only two production lines change in `FinanceDatasetIntakeStore.ParseCsv`:

- Keep `hasExDividend` and `hasSplitRatio` from the existing `TryGetValue` calls.
- Read `f[ex]` / `f[split]` when the corresponding boolean is true; use null when absent.

Nonzero present columns and absent columns therefore keep their existing behavior; index zero
now follows the same present-column path. No generic parser abstraction or header redesign.
Preserved annotation strings flow into existing row comparison/validation and candidate evidence
without modifying those rules or promotion SQL. They are not added to canonical price identity.

`CanonicalDatasetRevisionIdentityV2` remains byte-for-byte unchanged. Existing pinned synthetic
revision `dataset-v2-31c6ed4a006c7514de08409dca1998be6c8e7af9ee2e06a72cb349f69aa15522` and
its checksum tests pass; synthetic legacy ID `wiki-dde8381ccbe9ccc5` and persisted replay evidence
remain unchanged. Equal named canonical price content produces equal v2 identity despite CSV
column reordering; different raw artifact bytes correctly produce different artifact checksums.
The missing/present distinction is confined to optional action annotations. Decimal scale,
date/calendar and symbol normalization, row order, OHLCV and adjusted-close semantics are untouched.

Terminal candidate replay remains unchanged; this correction does not re-inspect old records,
repair historical action annotations or regenerate lineage. Production incidence and historical
impact remain **UNKNOWN**. No production audit, historical repair/backfill, rekey, alias or migration.

### Verification commands and results

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~CorporateActionEvidenceSurvivesCsvHeaderReordering|FullyQualifiedName~CorporateActionAbsentColumnsRemainAbsent' --logger 'console;verbosity=minimal'
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~FinanceDatasetIntakeTests|FullyQualifiedName~FinanceDataProtectionTests|FullyQualifiedName~CanonicalDatasetRevisionIdentityTests|FullyQualifiedName~FinanceResearchDatasetTests' --logger 'console;verbosity=minimal'
dotnet build BigBrain.slnx --configuration Release --no-restore
dotnet test BigBrain.slnx --configuration Release --no-build --logger 'console;verbosity=minimal'
node scripts/verify-documentation.mjs
git diff --check
git diff --cached --check
git diff --cached | docker run --rm -i --network none zricethezav/gitleaks:v8.28.0 stdin --redact --no-banner
```

Pre-fix targeted: 3 pass / 2 expected failures. Post-fix focused: 71/71 pass. Release solution
build: **0 warnings / 0 errors**. These valid results were preserved across interruption; resume
verified unchanged branch/source/test content and ran the first unfinished full-suite gate.
Full API **649/649** and Sentinel **32/32** pass with zero failures/skips. Documentation verifier
passes (229 Markdown files / 90 unique backlog IDs); working-tree diff check passes.
Staged-diff check passed; staged Gitleaks v8.28.0 found no leaks. Final publication staging
contains only the two intended code/test files and eight documentation files.
No Web rerun is required: no consumed API/UI contract change. Compose/runbooks are unchanged.
The subsequent approved main merge passed all four required CI jobs, as linked above.

## Changes

- Production: `src/BigBrain.Api/Finance/FinanceDatasetIntake.cs` — two optional-presence checks.
- Tests: `tests/BigBrain.Api.Tests/FinanceDatasetIntakeTests.cs` — five regression cases.
- Documentation: TESTING, STATUS, BACKLOG, Finance module, BB-130 plan, report catalog,
  canonical recovery note and this correction report. Historical blocker report is untouched.

## Security

Only isolated temporary synthetic files/databases were used. No production Finance data access,
secrets, private addresses, sensitive paths or raw operational logs. No changes to rights/provenance,
promotion gates, candidate lifecycle, quarantine, checksum algorithm, observation storage model,
schema/migrator, provider acquisition, XLSX, scientific calculations, public APIs or UI.
Finance **RESEARCH / 0 SEK / NONE**; no providers, broker/orders, PAPER/LIVE/AUTO or allocation.
No deployment or production repair.

## Remaining work

CSV parser extraction and other intake, persistence/schema/composition and BB-130D
responsibilities remain separate and require a separately authorized checkpoint.
If the owner needs production-incidence evidence, a bounded read-only audit is a possible separately
authorized task; no historical damage or need for repair is inferred here. No new BB ID assigned.

## Resumption

Use [canonical recovery](../../../operations/codex-recovery.md) and
[BB-130 plan](../../../architecture/bb-130-stabilization.md). This correction is accepted and merged;
its temporary recovery state is resolved. Never merge the historical blocker branch.
Do not deploy, start the next checkpoint or resume parser extraction automatically.
