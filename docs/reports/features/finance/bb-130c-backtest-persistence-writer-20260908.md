# BB-130C — Shared immutable backtest persistence writer

Detta är en sanerad GitHub-version. Synthetic temporary SQLite evidence only.

## Metadata

- Date: 2026-09-08.
- Baseline: `4ec675864d76f6da11d747026d917491f085f9e7`; baseline main CI run `34226424850` succeeded.
- Branch: `bb-130c/backtest-persistence-writer`, directly from verified main.
- Scope: one shared immutable writer extraction; ADR 0023/0024/0025 and accepted
  [persistence characterization](bb-130c-finance-persistence-characterization-20260908.md).

## Status

**REVIEW CANDIDATE ONLY — NOT MERGED TO MAIN**. Local verification recorded below.
Branch publication is for architect review; no owner acceptance, main CI claim,
deployment or runtime verification. Finance **RESEARCH / 0 SEK / NONE**.

## Evidence

### Responsibility and exact preserved contract

Before: `EodhdMarketMemory.PersistBacktest` in FinanceBacktestStore.cs, shared by three caller families.
After: concrete internal static `FinanceBacktestPersistence.PersistBacktest`, accepting the same
already-open SqliteConnection and BacktestResult. Four production calls changed: reference benchmark
and strategy writes, robustness underlying runs, and bounded research-dataset backtests.
No DI/interface, options, connection factory, repository or second database was introduced.

The method text is identical to baseline. Two private SQL binding helpers were copied unchanged;
their original counterparts remain needed by other EodhdMarketMemory responsibilities. Both writer
and existing reader retain JsonSerializerDefaults.Web options. No configuration serialization is
invented: configuration remains embedded in result_json, with the existing feature/source columns.
Market revision JSON retains its existing default serializer call. UtcNow creation metadata retains
its existing formatting and is not claimed deterministic across new database writes.

Existing checksum lookup returns false for equal replay and throws InvalidOperationException with
the same message for conflicting evidence. A new write uses the same BeginTransaction, INSERT OR
IGNORE, loser rollback/checksum lookup, and winner insertion order: run, events, fills, equity, commit.
Connection lifetime stays with callers. No isolation, busy-timeout, locking or transaction boundary
change; robustness batches remain separately committed runs followed by evaluation persistence.
DDL initialization, schema authority, readers/catalogs, latest-feature selection, calculations,
provider acquisition and every other persistence family remain where they were.

### Characterization before production move

Two new tests in FinanceBacktestPersistenceTests ran against the original writer, alongside existing
BB-123 concurrent equivalent builders and non-EODHD restart/replay/conflict coverage: **4/4 passed**.
Production diff was empty at this characterization point. A detailed one-test rerun captured the
synthetic golden values below before extraction; they are now pinned assertions.

- Complete write: one run, **7 events / 1 fill / 3 equity points**, exact stored JSON versus input,
  source/feature references, true on first insert, false on replay; full snapshot (including created
  metadata) unchanged after replay/conflict/restart. Conflict message is asserted exactly.
- One rollback case: append a duplicate equity session to a synthetic result. SQLite constraint
  error 19 occurs in the last child family after the earlier children were inserted. Transaction
  disposal leaves **zero runs/events/fills/equity**. The caller connection stays open and a subsequent
  valid write succeeds with all 12 rows. This is fault injection, not a discovered production defect.
- Existing concurrent builders converge to six unique runs. This test protects accepted BB-123
  convergence; it does not force every possible scheduler interleaving or claim global initializer safety.

### Deterministic before/after fixture

- RunId: `backtest-4013c330c3197923`.
- Checksum: `sha256:30d506781dfbbf0be5cd21d3008ebf24c5d889ebc76328080f6222b9cb843120`.
- Stored JSON digest: `12C9479C7F3C3349BF1ECE0532C72C0FF8D7ADB3C0D98CB12ED3EF6A16738459`.
- Digest contract: SHA-256 of UTF-8 LF-joined result_json, event_json ordered by sequence,
  fill_json ordered by fill_id, point_json ordered by session_date; no terminal LF.
- Source `market-1`, feature `feature-1`, buy-and-hold conservative-cost synthetic fixture.

All pinned values, exact JSON comparisons, complete row counts and reader restart checks pass after
extraction. Existing canonical v2/legacy tests are unchanged except two direct writer references in
the accepted non-EODHD characterization. No scientific identity expectation was replaced.

### Verification

Pre-move selection: FinanceBacktestPersistenceTests, ConcurrentEquivalentBacktestBuilds and
NonEodhdEvidenceUsesSharedPersistence: **4/4**. Post-move focused selection: **82/82**, no failures/skips.
Includes writer, FinanceEodhdIntegrationTests, CanonicalDatasetRevisionIdentityTests,
FinanceDeterministicBacktestTests, FinanceRobustnessEvaluationTests, FinanceResearchDatasetTests,
and FinanceResearchCampaignTests. Campaign policy outcomes remain unchanged; no new campaign
SQL replay claim is made. Full gates: full API **661/661**, Sentinel **32/32**, zero failures/skips; Release **0 warnings / 0 errors**.

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~FinanceBacktestPersistenceTests|FullyQualifiedName~ConcurrentEquivalentBacktestBuilds|FullyQualifiedName~NonEodhdEvidenceUsesSharedPersistence' --logger 'console;verbosity=minimal'
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~FinanceBacktestPersistenceTests|FullyQualifiedName~FinanceEodhdIntegrationTests|FullyQualifiedName~CanonicalDatasetRevisionIdentityTests|FullyQualifiedName~FinanceDeterministicBacktestTests|FullyQualifiedName~FinanceRobustnessEvaluationTests|FullyQualifiedName~FinanceResearchDatasetTests|FullyQualifiedName~FinanceResearchCampaignTests' --logger 'console;verbosity=minimal'
dotnet build BigBrain.slnx --configuration Release --no-restore
dotnet test BigBrain.slnx --configuration Release --no-build --logger 'console;verbosity=minimal'
node scripts/verify-documentation.mjs
git diff --check
git diff --cached --check
git diff --cached | docker run --rm -i --network none zricethezav/gitleaks:v8.28.0 stdin --redact --no-banner
```

Publication verification: documentation **232 Markdown files / 90 unique backlog IDs**;
working/staged diff checks passed; staged Gitleaks v8.28.0 found **no leaks**. Only the 14 intended
production/test/documentation files are included; unrelated local files are preserved.

Initial sandbox MSBuild IPC was denied; the authorized unsandboxed run succeeded. The test-output
helper namespace was corrected during test authoring before baseline characterization passed.
No Web/API contract or Compose change; no local Web/Compose rerun required. No performance claim.

## Changes

Production:
- `src/BigBrain.Api/Finance/FinanceBacktestPersistence.cs`: extracted writer and private bindings.
- `src/BigBrain.Api/Finance/FinanceBacktestStore.cs`: remove old writer; two qualified calls.
- `src/BigBrain.Api/Finance/FinanceRobustnessStore.cs`: one qualified call only.
- `src/BigBrain.Api/Finance/FinanceResearchDatasets.cs`: one qualified call only.

Tests: new `tests/BigBrain.Api.Tests/FinanceBacktestPersistenceTests.cs`; two call references in
`tests/BigBrain.Api.Tests/CanonicalDatasetRevisionIdentityTests.cs`. Documentation: this report,
TESTING, STATUS, BACKLOG, Finance module, stabilization plan, report catalog and canonical recovery.

## Security

Finance **RESEARCH / 0 SEK / NONE**. No schema/DDL/migration, historical rewrite, identity change,
production-data access, provider activation, acquisition, broker/orders/PAPER/LIVE/AUTO, capital
allocation or deployment. Sanitized synthetic fixtures only; no credentials or private runtime data.
Rights/provenance, fail-closed, cost/execution, holdout/OOS and NOT EVALUABLE semantics are unchanged.

## Remaining work

Review/owner acceptance and exact-SHA merge are still required. Other provider-neutral storage,
shared path naming/ownership, distributed initialization/schema authority, reader full-JSON cost,
intake and composition debt remain separately scoped; BB-130C is not complete and BB-130D is not begun.
Unverified broader initializer concurrency/campaign SQL replay remains a gap, not a proven defect.
Historical WIKI compatibility and historical CSV column-zero incidence remain UNKNOWN.
Next possible boundary: characterize only backtest catalog/detail reader ownership and serialization
before a separately authorized move; no reader optimization or schema work is proposed here.

## Resumption

Review the exact pushed branch SHA and this evidence. Do not merge, deploy or start another
checkpoint automatically. Full temporary review state belongs only in
[canonical recovery](../../../operations/codex-recovery.md). Main remains accepted source of truth.
