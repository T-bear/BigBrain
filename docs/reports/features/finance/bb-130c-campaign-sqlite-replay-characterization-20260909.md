# BB-130C E2 — campaign SQLite persistence, replay and reload

## Metadata

- Date: 2026-09-09. Detta är en sanerad GitHub-version.
- Verified accepted main: `04a7a9c1f5d4d9afb02f313a272b6c4369709f5a`.
- Branch: `bb-130c/campaign-sqlite-replay-characterization`, directly from verified main.
- Scope: one isolated persistence/reconstruction/replay test, reuse of an existing test fixture,
  source characterization and documentation. **No production files changed**.
- Authorities: [BB-129A](finance-bb-129a-multi-dataset-campaign-20260903.md),
  [BB-127](finance-bb-127-owner-research-dataset-xlsx-20260901.md), BB-123/124 contracts,
  [persistence map](bb-130c-finance-persistence-characterization-20260908.md),
  [accepted writer](bb-130c-backtest-persistence-writer-20260908.md),
  [accepted reader/exit assessment](bb-130c-backtest-reader-exit-assessment-20260908.md).

## Status

**OUTCOME A — CURRENT IMPLEMENTATION SATISFIES THE CHARACTERIZED CAMPAIGN PERSISTENCE / REPLAY IDENTITY CONTRACT.**
**REVIEW CANDIDATE ONLY — NOT MERGED TO MAIN.** No reproduced blocker and no production refactor.
E1 remains accepted/merged/CI verified. E2 characterization is locally verified, pending architect
review and owner acceptance. BB-130C remains **NOT READY** until E2 acceptance, merge and green
main CI; BB-130D is **NOT STARTED**. No deployment, runtime or device/owner UX claim.

## Production path map

All API source paths are under `src/BigBrain.Api/` unless noted otherwise.

| Responsibility | Existing path and behavior | Evidence class |
| --- | --- | --- |
| Creation entry | Finance/FinanceDatasetIntake.cs maintenance `finance-research-campaign <immutable-knowledge-time-utc>` parses invariant DateTimeOffset, initializes normal memory/store, calls RunResearchCampaign. No HTTP campaign mutation route. Test calls that same store method, not CLI/configuration startup. | Orchestration |
| Input selection | Finance/FinanceResearchCampaigns.cs RunResearchCampaign calls ResearchCatalog, orders RevisionId with StringComparer.Ordinal, takes at most 8 revisions. It constructs the fixed three variants in two families. | Deterministic selection from stored inputs |
| Definition | Schema/policy/engine/library versions, explicit knowledge time, ordered revision IDs, fixed variants/decimal parameters, limits (2 families, 3 variants/family, 8 instruments, 24 runs, 5 robustness variants, concurrency 1, retries 0, 300 seconds), seed 0, fixed limitations. | Persisted authoritative definition |
| Checksum/ID | FinanceResearchContracts.Fingerprint in BigBrain.Modules/Finance/AutonomousResearch.cs serializes the definition with camelCase property naming, hashes UTF-8 using SHA-256 and lowercase hex with `sha256:`. CampaignId uses `campaign-` plus the first 16 hash hex characters. No extra sort/canonicalizer is invented. | Deterministic derived identity, persisted |
| Dataset lineage | BB-127 research_dataset_revisions contains revision ID, dataset fingerprint, artifact/workbook identity, source/symbol, capabilities and limitations. Definition binds ordered revision IDs; every attempt also retains DatasetFingerprint and Instrument. No canonical or feature revision is synthesized. | Authoritative immutable source evidence and persisted references |
| Attempt identity/order | Dataset ordinal order, then Population order (momentum20, SMA10/40, SMA20/80). Result ID fingerprints `{campaignId, RevisionId, HypothesisId}` with `campaign-result-` and first 16 hex characters. FamilyAttemptOrdinal increments across dataset/variant pairs within each family. | Deterministic derived identity and ordering |
| Outcome creation | Reads TrainValidationHoldout capability and schema: ineligible → DATASET_INELIGIBLE; otherwise non-OHLCV → SCHEMA_INCOMPATIBLE; otherwise INSUFFICIENT_DATA. All current attempts are InconclusiveNotEvaluable and BacktestRunId null. It does not invoke backtest/robustness engines or fabricate absent feature/holdout evidence. | Derived categorical outcome, then authoritative persisted evidence |
| Scorecard | Counts attempts/reasons, zero rejected/survived/robust candidates, fixed categorical ordering explanation and RESEARCH / 0 SEK / NONE. No performance return/score is calculated. | Derived once, persisted authoritative evidence |
| Storage | research_campaigns: campaign_id primary key, checksum UNIQUE, status, created_utc, definition_json, results_json, scorecard_json. Attempts and scorecard are arrays/objects inside this row, not separate child tables. | Authoritative persisted aggregate |
| Initialization | FinanceDatasetIntakeStore.Initialize calls InitializeResearchCampaignStorage; campaign create/list/detail also execute CREATE TABLE IF NOT EXISTS. The constructor also owns existing intake recovery initialization. No schema authority is moved. | Existing DDL/runtime orchestration |
| Connection/transaction | Shared EodhdFinanceOptions.DatabasePath; each method creates/opens/disposes its own SqliteConnection. No campaign connection or campaign object cache on the store. INSERT OR IGNORE is one SQLite statement/implicit transaction for the complete aggregate. Selection/read/insert are not one explicit larger transaction. | Persistence boundary |
| Replay | Rebuilds definition/checksum/ID from the current ordered revision set and SAME explicit time. After initialization, existing ReadCampaign returns immediately: no new results/scorecard or replacement. Initial insert also returns the stored winner through ReadCampaign. | Idempotent lookup for unchanged definition |
| Detail/reload | ResearchCampaign(id) → ReadCampaign: selects six stored fields by requested primary key, parses status and invariant created time, deserializes three JSON fields. It does not recompute IDs, outcomes, ordinals, lineage, reasons or scorecard. Missing ID → null. | Loaded authoritative evidence |
| Catalog | ResearchCampaigns orders created_utc DESC, campaign_id, loads stored campaigns, projects summary fields plus constant RESEARCH/0/NONE envelope. No scientific rerun. Multi-campaign date ordering is source-mapped, not newly regression-tested here. | Projection of stored evidence |
| API reader | FinanceResearchCampaignReader delegates to store; registered singleton reader in Program.cs. Versioned GET catalog/detail routes use existing JsonOptions; null detail becomes 404 Problem Details with finance.research.campaignNotFound. | Presentation/API boundary |

### Serialization and time

ResearchJson is JsonSerializerDefaults.Web, camelCase properties, standard numeric enums in the
persisted definition/results/scorecard; database status itself is the string `Completed`.
Fingerprint uses its own existing camelCase options. No serializer options or field representation
changed. ReadCampaign deserializes stored JSON rather than reconstructing an alternate result.

The campaign checksum fingerprints the **definition**, not all result JSON: it is not a general
payload-integrity verifier. This test does not invent a new checksum/security contract. Dictionary
and array order are those of existing deterministic construction and stored JSON, not a new sorter.

KnowledgeTimeUtc is included in the definition fingerprint and becomes CreatedUtc; SQL created_utc
uses invariant `O` format. Changed time is a changed definition. Replay does not sample UtcNow for
campaign evidence. ResearchCatalog.GeneratedAtUtc uses UtcNow, but is a presentation-only envelope
not included in campaign identity. Research dataset created_utc comes from original fixture ingestion
and is retained exactly in the before/after row snapshot. No timestamp is rewritten by the test.

## Evidence

Added `SqliteCampaignReloadAndExactReplayPreserveImmutableEvidence` in
`tests/BigBrain.Api.Tests/FinanceResearchCampaignTests.cs`. The existing BB-127 nested Fixture in
`FinanceResearchDatasetTests.cs` changes only from private to internal so the same workbook/package,
normal scanner and temporary database initialization can be reused. No duplicated ingestion helper,
SQL-seeded scientific metadata, network/provider request, production database or alternate engine.

Fixture scope: one 120-row synthetic OHLCV series and one four-row close-only context series,
owner-approved-with-limitations research claims and absent immutable feature/selection/holdout evidence.
The fixture is ingested once. Its package/workbook bytes are retained through replay; independently
regenerating an XLSX package is not claimed to produce identical archive bytes or golden revision IDs.
The test proves determinism for the same immutable ingested inputs, not package-byte canonicalization.

### Restart and snapshot design

1. Run normal BB-127 fixture ingestion and read two noncanonical research revisions.
2. Run campaign with explicit `2026-09-03T08:30:00+00:00` and capture the complete production-returned
   aggregate serialization plus all seven raw campaign columns and all research revision/observation columns.
3. All method-local production connections have closed. Clear only this temporary database's pool;
   construct a fresh FinanceDatasetIntakeStore and FinanceResearchCampaignReader. The store has no
   IDisposable; no old store is used for reload/replay. This is store/connection reconstruction, not
   an OS-process crash/power-loss test.
4. Load through production reader, read catalog/missing ID, compare full evidence and row snapshots.
5. Replay same time with unchanged dataset set; load again through another newly constructed store/reader.
6. Compare complete raw campaign and dataset snapshots again and verify no canonical/backtest rows.

Snapshot queries open SQLite explicitly read-only with pooling disabled. Rows are ordered by primary
keys and structurally encoded as JSON arrays without parsing/reformatting the stored JSON columns.
This preserves original JSON strings and timestamps for equality. No logical campaign evidence is
excluded. Database page layout, WAL bytes and presentation-only ResearchCatalog.GeneratedAtUtc are
not compared. Read entry points retain their existing IF NOT EXISTS DDL; passing snapshots prove no
row mutation here, not a universal read-only constructor or recovery guarantee.

### Observed results

| Contract | Before / reconstructed reload / exact replay / final reload |
| --- | --- |
| Campaign rows | Exactly **1**, all seven columns unchanged. |
| Campaign ID and checksum | Identical; checksum equals definition fingerprint; CampaignId has existing derived prefix. Changed explicit time changes definition fingerprint. |
| Definition and population | Complete JSON equal, including versions, limits, seed, timestamps, decimal parameters and variant order. |
| Dataset lineage | Two exact ordered revision IDs; each attempt keeps expected dataset fingerprint and instrument. All **126** dataset rows (2 revisions + 124 observations) unchanged. |
| Attempt identity and ordering | **6 unique result IDs**, matching dataset/variant pairs and existing identity formula; per-family ordinals and serialized result ordering unchanged. |
| Scorecard and dispositions | Six INCONCLUSIVE / NOT EVALUABLE, three DATASET_INELIGIBLE and three INSUFFICIENT_DATA; zero rejected/survived/robust candidates. Entire scorecard/result JSON preserved. |
| BacktestRunId / scientific numbers | All **null**; no OOS/holdout/return evidence fabricated. Existing integer counts/parameters are preserved. Zero backtest_runs and zero canonical observations. |
| Catalog / missing ID | One matching immutable summary, RESEARCH / 0 SEK / NONE; missing synthetic ID returns **null** with unchanged campaign rows. API 404 behavior is source-mapped, not a new HTTP test. |

## Verification

Commands run against isolated fixtures on 2026-09-09:

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~FinanceResearchCampaignTests --logger 'console;verbosity=minimal'
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~FinanceResearchCampaignTests|FullyQualifiedName~FinanceResearchDatasetTests|FullyQualifiedName~FinanceBacktestPersistenceTests|FullyQualifiedName~CanonicalDatasetRevisionIdentityTests|FullyQualifiedName~FinanceDeterministicBacktestTests|FullyQualifiedName~FinanceEodhdIntegrationTests|FullyQualifiedName~FinanceRobustnessEvaluationTests|FullyQualifiedName~FinanceDatasetIntakeTests|FullyQualifiedName~FinanceDataProtectionTests' --logger 'console;verbosity=minimal'
dotnet build BigBrain.slnx --configuration Release --no-restore
dotnet test BigBrain.slnx --configuration Release --no-build --logger 'console;verbosity=minimal'
node scripts/verify-documentation.mjs
git diff --check
git diff --cached --check
git diff --cached | docker run --rm -i --network none zricethezav/gitleaks:v8.28.0 stdin --redact --no-banner
```

- Campaign suite: **10/10** (one new SQLite case and nine existing policy cases).
- Related Finance suites: **129/129**, zero failures/skips.
- Release build: **0 warnings / 0 errors**.
- Full API: **664/664**; Sentinel/architecture: **32/32**, zero failures/skips.
- Documentation verifier: **passed**, 235 Markdown files / 90 unique backlog IDs.
- Working and staged diff checks: **passed**. Staged Gitleaks v8.28.0: **no leaks found**.
- Local results are not GitHub CI or main acceptance; this is branch publication for review.
- Web not rerun: no Web or shared/frontend/API contract changed. Compose unchanged.

## Changes

Only the two named test files and relevant TESTING, STATUS, BACKLOG, Finance module,
BB-130 plan, report catalog, this report and canonical recovery. Production source remains
byte-for-byte unchanged. No extraction, testability-only production change, schema change or migration.

## Security

Finance **RESEARCH / 0 SEK / NONE**. No scientific model, strategy/variant, eligibility,
BB-123 execution/cost, BB-124 OOS/holdout, BB-127 lineage/canonical v2/legacy identity,
BB-129A disposition or immutable writer change. No production data, acquisition, provider,
Alpaca integration or new support evidence, broker/order/PAPER/LIVE/AUTO/capital, historical
rewrite, deployment or infrastructure. All data is isolated synthetic evidence; published
report contains no raw datasets, secrets, private identities or sensitive paths.

## Remaining work

Architect review and owner acceptance/merge/main CI are still required. E1 remains accepted;
E2 now has passing bounded evidence, not accepted completion. No new blocker was reproduced.
After E2 acceptance there are no further currently-known C blocking checkpoints in the accepted
short exit plan; owner/architect must confirm exit before separately authorizing BB-130D.
C is not marked complete here. Existing post-BB-130 debt remains explicitly deferred.

Limits: sequential replay/reconstruction over unchanged inputs; no corruption injection, concurrent
campaign creators, power failure or transitional candidate recovery matrix. It does not establish
behavior after changing dataset inputs/policy versions or hostile stored JSON. These are unverified
scenarios, not reproduced defects; no new blocking checkpoint is invented. No runtime audit or
performance claim. Scientific feature/holdout absence remains a legitimate limitation.

## Resumption

Use [canonical recovery](../../../operations/codex-recovery.md), current STATUS and the published
branch SHA. Review this bounded evidence; merge only after exact-SHA owner approval. No E2 production
fix, BB-130D, deployment or new provider work is authorized automatically by this publication.
