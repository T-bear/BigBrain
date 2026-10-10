# BB-132H — Versioned observation mapping

Detta är en sanerad GitHub-version. Model-free implementation evidence, 2026-10-10.
No production mapping, credentials, provider payloads or host-private state.

## Metadata

- Accepted baseline: `a2373f172600c7b45ce378c56aab1d7da2b9b242`.
- Baseline tree: `fb41dea807053d7ef2c2219170f7a907e48ed146`.
- Branch: `bb-132h/versioned-observation-mapping`, created directly from verified accepted main.
- Unique publication subject: `review: implement BB-132H versioned observation mapping`.
  Resolve that publication's exact SHA/tree from Git metadata; no self-referential commit identity.
- Architect-reviewed input: [BB-132G analysis at a31544e](https://github.com/T-bear/BigBrain/blob/a31544e708e3b059fd2ea8b62b74438085091193/docs/reports/features/finance/bb-132g-aapl-mapping-compatibility-20261010.md),
  tree `675edd2d8802a6ae3fad07017019145e373ffd5c`. Analysis is not accepted implementation.
- Historical operational handoff: [2df263a](https://github.com/T-bear/BigBrain/commit/2df263a8cc11bd5837ea205244ffacefa184cec9).
  Neither review branch is merged, rewritten or treated as current deployment proof.

## Status

**MERGE CANDIDATE / IMPLEMENTED / AUTOMATICALLY VERIFIED / REVIEW ONLY.**
Publication is not acceptance, deployment or permission to extend the live AAPL mapping.
Finance **RESEARCH / 0 SEK / NONE**. No operational acquisition or production-store access.

## Changes

The previous configuration held one complete instrument/mapping snapshot per canonical ID.
Changing its validity or evidence would conflict with historical receipt provenance. This candidate
adds bounded versions selected by source day while retaining each historical snapshot exactly.
[Canonical contract and limits](../../../architecture/finance/market-data-memory-and-provenance.md#bb-132h-versioned-observation-mapping--review-implementation-2026-10-10)
and [configuration/adoption gate](../../../operations/runbooks/finance-controlled-acquisition.md#versioned-mapping-configuration--bb-132h-review-candidate)
carry the durable developer/operator rules.

The existing canonical types, `InstrumentMappingCatalog`, Alpaca daily adapter, runtime/F1 path,
Finance SQLite owner and immutable receipt/projection machinery are reused. No generic security
master, second market store, provider framework, scheduler, endpoint or science engine is added.

- One configured instrument has up to four non-overlapping complete snapshots. Up to four
  instruments remain independently bounded. Source-date gaps/overlaps/ambiguity reject before
  network. Versions do not multiply requests for a single instrument/day.
- `finance-observation-mapping-v1` separates effective dates, operator-attested verification time,
  trusted Finance first-recorded time and actual market acquisition/knowledge. An explicit
  revalidation deadline is exclusive and at most seven days after verification. Evidence is a
  bounded reference, not an automatically verified issuer assertion. Null effective end means
  no known termination at verification; it never overrides the currentness deadline.
- Stable ID, type/currency, listing MIC/venue, display name and provider symbol must agree across
  snapshots. Unreviewed material changes fail closed. IEX is still the dataset/feed, not listing venue.
- Additive migration98 stores immutable assertion manifests and receipt bindings in the existing
  owner. Persisting assertions is necessary to preserve verification/recording evidence across
  configuration replacement and restart without changing old receipt commitments. Immediate
  transactions protect adoption and receipt/binding commit. No old receipt payload is migrated.
- Every adopted snapshot must remain in later manifests exactly. Retained receipts must resolve
  to their original full instrument/mapping before provider access. Versioned mode cannot silently
  downgrade to legacy configuration. A concurrent adoption during a legacy request prevents commit.
- New/revision receipts bind their authorizing manifest atomically. Duplicate returns original
  receipt/knowledge/binding. Legacy duplicate does not retrofit a binding. Reverification creates a
  separate assertion while old receipt and sealed-cutoff identities remain unchanged.
- Currentness is rechecked after runtime spacing, before request and at commit. Expiry during a
  request publishes no observation. A failed/cancelled request can retain a mapping assertion;
  this is not market evidence and does not advance the market watermark.
- Manifest identity commits canonical sorted snapshots/assertions; checksum also commits trusted
  recording time. Reopen checks receipt binding against authorization at original ingestion,
  not today's expiry. Market watermark/manifest clock regression and corrupted commitments reject.
- Legacy single-entry input retains accepted semantics without invented verification fields.
  Historical metadata conflicts in runtime/F1 now fail before network. Existing reobserve and
  SHADOW equality checks are not relaxed. Ledger readers only recognize additive schema98.

### Changed-file responsibility

Production (seven files):

- `src/BigBrain.Api/Finance/FinanceObservationMappings.cs`: bounded plan/currentness; durable manifest and binding validation.
- `src/BigBrain.Api/Finance/FinanceObservationRuntime.cs`: bounded nested config, shared snapshot validation, per-day selection/adoption.
- `src/BigBrain.Api/Finance/FinanceObservationMaintenanceCommand.cs`: same version selection/adoption for F1; original one-shot restrictions.
- `src/BigBrain.Api/Finance/FinanceMarketObservations.cs`: authorization checks and atomic receipt binding; original provenance equality retained.
- `src/BigBrain.Api/Finance/FinanceSchemaMigrations.cs`: append migration98.
- `src/BigBrain.Api/Finance/FinanceLearningLedger.cs`: schema98 compatibility allowlist only.
- `src/BigBrain.Api/Finance/FinanceFiniteResearchSession.cs`: schema98 compatibility allowlist only.

Tests (three files):

- `tests/BigBrain.Api.Tests/FinanceObservationMappingTests.cs`: version/currentness, receipt/replay, migration, race/cancellation and mixed-SHADOW proofs.
- `tests/BigBrain.Api.Tests/FinanceObservationMaintenanceTests.cs`: nested one-shot fixture and earlier conflict rejection request count.
- `tests/BigBrain.Api.Tests/ProspectiveDailyShadowTests.cs`: latest schema assertions only; science behavior unchanged.

Documentation (seven files): this report, `docs/reports/REPORT-CATALOG.md`, `docs/STATUS.md`,
`docs/BACKLOG.md`, `docs/operations/codex-recovery.md`,
`docs/architecture/finance/market-data-memory-and-provenance.md`, and
`docs/operations/runbooks/finance-controlled-acquisition.md`.

## Evidence

New tests use synthetic identities/dates and temporary stores, not production AAPL evidence.
Transport doubles exercise the real adapter/Finance owner without a network call. New mapping tests
never invoke a model or scientific backtest engine. The SHADOW refusal test constructs explicitly
synthetic retained evidence through existing persistence, checks old-cutoff replay/refusal using
the real shadow boundary, and does not perform an operational SHADOW run. Existing regression
science fixtures retain their own accepted model-free evaluator coverage.

| Requirement | Deterministic evidence |
| --- | --- |
| Unique effective version, null end/currentness, bounded JSON | SourceDateResolutionAndNestedConfiguration; InvalidManifest theory; NestedJson theory |
| Overlap/gap/stale/ambiguous/material change before network | InvalidManifestNeverReachesTransport; RuntimeSelectsOneVersionPerDayAndDoesNotRetryGapOrOverlap |
| Legacy compatibility, exact old bytes/times/lineage | LegacyReceiptSurvivesTransitionDuplicateRevisionAndReopenWithSealedHistory |
| Real revision versus metadata replacement | Same test plus ConfigurationCannotReinterpretHistoricalProvenanceEvenBeforeNetwork and existing provenance regressions |
| New-day mapping/later knowledge, sealed noninterference | LegacyReceiptSurvivesTransitionDuplicateRevisionAndReopenWithSealedHistory compares serialized earlier projection |
| Expiry during request, fresh assertion without rewriting | CurrentnessExpiresDuringRequestAndReverificationDoesNotRewriteOriginalAuthorization |
| Persisted authority/corruption/clock/downgrade | DurableManifestAndReceiptBindingsFailClosed theory |
| Reopen/concurrency/cancellation | ConcurrentReopenAndCancellationKeepAtomicReceiptAndBinding; AdoptionDuringLegacyRequestCannotCommitAnUnauthorizedReceipt |
| Additive migration/crash/old grants | MigrationIsAdditiveRollbackSafeAndDoesNotRenewSpentAuthority; UpgradeFrom97PreservesExactLegacyReceiptBytes |
| Mixed mapping SHADOW remains refused | ShadowV1StillRejectsMixedMappingsAndOldCutoffReplaysWithoutScienceExecution |
| F1 uses exactly one selected version/request | FinanceObservationMaintenanceTests versioned fixture; existing guards remain in focused suite |

The real 2026-10-02 AAPL receipt was **not** opened, migrated or modified during development.
Preserving it follows from no production operation and the additive implementation; fixture byte
identity is tested. Live reopen/adoption verification remains a separately authorized operational gate.

### Verification commands and results

Focused command (Release, model-free):

```bash
dotnet test tests/BigBrain.Api.Tests -c Release --no-restore --filter 'FullyQualifiedName~FinanceObservationMappingTests|FullyQualifiedName~FinanceObservationRuntimeTests|FullyQualifiedName~FinanceObservationMaintenanceTests|FullyQualifiedName~FinanceMarketObservationTests|FullyQualifiedName~ProspectiveDailyShadowTests' --logger 'console;verbosity=normal'
```

Final focused result: **193 passed, 0 failed, 0 skipped**.
The solution formatter then applied formatting only to the ten changed C# files; subsequent checks:

```bash
dotnet restore BigBrain.slnx
dotnet format BigBrain.slnx --verify-no-changes --no-restore
dotnet build BigBrain.slnx -c Release --no-restore
dotnet test tests/BigBrain.Api.Tests -c Release --no-build --no-restore --logger 'console;verbosity=normal'
node scripts/verify-documentation.mjs
git diff --check
gitleaks git --pre-commit --staged --redact --no-banner
```

Restore PASS (up to date); formatter PASS; Release build PASS, zero warnings/errors.
Full API: **1144 passed, 0 failed, 8 skipped** (1152 total; 1.53 minutes). The eight
explicit real-model acceptance tests were not invoked. Documentation verifier PASS:
266 Markdown files / 91 unique backlog IDs. Diff/scope check PASS; staged Gitleaks8.28.0
PASS, no leaks. Exactly17 intended files; no live configuration or unrelated files staged.

Intermediate failures are retained as engineering evidence, not final passes: initial existing
regressions156 pass/3 fail exposed two schema-reader97 allowlists and the now-earlier metadata
rejection request count. A seven-test diagnostic subset repeated those three failures. Both
allowlists and the assertion were corrected within scope. Compilation checks caught xUnit1051
(cancellation-token fixture overload) and CA1869 (serializer option reuse), both corrected.
A later192-test run had190 pass/2 fail: fixture clock did not advance for separate writes, and
foreign-key enforcement correctly prevented a corrupt-binding injection. The controllable fixture
clock and explicitly corrupt temporary-database setup were corrected, without relaxing production
clock/foreign-key checks. Final focused193 pass includes the added F1 versioned test.

Full API verification is justified by shared Finance persistence/schema and compatibility readers.
Sentinel/frontend suites are not rerun: their implementation/contracts are unchanged. Solution
build still compiles the solution. Real-model acceptance tests remain explicitly skipped. No
network/provider/model integration test is required or authorized. GitHub workflow triggers only
main pushes or pull requests; a checkpoint branch push alone is not evidence of green GitHub CI.

## Security

No secrets, raw provider payloads, OHLCV, private paths or production mapping values are published.
No `.env`, live storage, credentials, runtime state or deployed image is touched. No deployment,
restart, provider request, manual one-shot, cycle trigger, Qwen/research invocation or grant renewal.
Existing bounded transport, default-disabled runtime, separate transport gate, single-flight,
serial spacing, request bounds, recovery/clock and sanitized failure categories remain authoritative.
No PAPER/LIVE/AUTO, Avanza/broker, orders, positions or capital. RESEARCH / 0 SEK / NONE.
Checksums establish deterministic integrity, not protection against a privileged database rewriter;
legacy receipts have no retroactively invented assertion binding.

## Remaining work

Independent exact-SHA review and owner acceptance. Operational adoption is a **separate** gate:
verify original receipt snapshots/current mapping evidence, review actual effective dates and expiry,
then separately authorize deployment/migration/configuration. No live AAPL values are supplied here.

Known deliberate limits: four versions/instrument, four instruments, seven-day maximum assertion
window, 1000 retained manifests and existing bounded receipt projections. Refreshing assertions must
retain all previous snapshots; there is no automatic verification, pruning or capacity expansion.
An already adopted open-ended interval cannot be shortened to introduce a successor. A newly known
termination/material change must stop affected collection pending a separately reviewed transition.
Versioned collection does not establish cross-version scientific compatibility: SHADOW v1 remains
blocked on mixed mapping provenance. No architecture/ADR is marked Accepted by this implementation.

## Resumption

Resolve this report's unique publication subject against the stated baseline; inspect the17-file
diff and verification. Unrelated mockups and local ADR0006–0009 are excluded and preserved.
No review history has been rewritten. STOP after publication for independent architect review.
The owner only needs to say “Codex är klar”. Do not merge or adopt live mappings from this branch.
