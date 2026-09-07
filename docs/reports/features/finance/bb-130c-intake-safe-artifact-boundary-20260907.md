# BB-130C — intake quarantine path and payload lifetime boundary

Detta är en sanerad GitHub-version. Source inspection and isolated synthetic fixtures only.

## Metadata

- Date: 2026-09-07.
- Baseline main: `44dec9ce720a7b0d19d7ef18cc6cb1f3e9acf01e`.
- Branch: `bb-130c/intake-safe-artifact-boundary`, directly from baseline main.
- Scope: one concrete physical quarantine collaborator; no intake subsystem redesign.

## Status

**REVIEW CANDIDATE ONLY — NOT MERGED TO MAIN**. Implemented and locally verified.
The committed branch is published for owner/architect review; no merge approval is inferred.
- No deployment, runtime or owner acceptance. Finance **RESEARCH / 0 SEK / NONE**.

## Evidence

### Characterization and boundary

The interrupted run completed initial source reading only: no production changes, branch,
characterization results or tests existed. Resume verified unchanged main and preserved unrelated
mockups/ADR proposals. Four tests were added and all 66 focused tests passed before extraction.

`FinanceDatasetIntakeStore` previously combined raw quarantine path construction, disk gates,
existence/deletion with candidate lifecycle SQL. `FinanceDatasetQuarantine` now owns those
physical operations. It takes existing dataset options and candidate ID/original filename or
expected byte count; returns the target path/existence/deletion result or throws the same error.
It has no database, HTTP, identity, parser or promotion dependency and no new interface/DI layer.
The repeated payload path rule now has one implementation for download, cleanup and restart.
This is a small responsibility extraction, not a line-count or performance claim.

| Observable contract | Preserved ownership and evidence |
| --- | --- |
| Root and payload paths | Root is created before existing initialization SQL. Payload uses existing candidate ID, `artifact`, and `Path.GetFileName(originalFilename)`; basename whitespace/invalid-character rejection and platform path behavior are unchanged. Download directory is created only after disk/filename gates. |
| Size/free-space gate | Same configured maximum and free-space arithmetic, same IOException/message. New test rejects oversized expected bytes before network/payload creation. The gate still precedes download's catch block: state is Downloading until restart reconciles it to Rejected/interruptedBeforeValidation. This ordering is preserved, not a new retry policy. |
| Invalid filename | New test freezes ArgumentException/message, no payload directory, and existing Downloading lifecycle state. |
| Cleanup eligibility and counts | Store still selects aged Rejected/Retained records in candidate order and owns SQL. It sets CleanupPending, invokes file deletion, accounts stored artifact bytes only when a payload existed, then sets PayloadDeleted. Missing payload test verifies zero released bytes and retained evidence. |
| Cleanup extent | Only the raw payload is removed. New test preserves extracted files, manifest, checksum/bytes, rejection state and cutoff behavior; no recursive directory or partial-file cleanup is introduced. |
| Restart/idempotency | Existing test covers pending deletion with absent payload. New test covers pending deletion with present payload returning to Retained, then successful cleanup and idempotent repeat. |
| Failure propagation | File/path/disk exceptions remain uncaught at the same orchestration positions. In particular deletion failure occurs after CleanupPending and before PayloadDeleted. No new retry, transaction, lock or cleanup-on-failure is added. |
| Artifact evidence | RecordArtifact and EnsureArtifactRecorded remain in the store, including hash algorithms, metadata, SQL ordering and open-stream lifetimes. Existing checksum/replay tests and new cleanup assertions preserve evidence. |
| Acquisition and resource bounds | HTTP attempts, cancellation, byte streaming limits, partial-file moves and archive bounds remain source-identical. No HTTP request is needed by the new tests. |
| Owner/research intake | Owner scanner, symlink/sidecar/copy rules, CSV/ZIP/XLSX parser and eligibility logic remain unchanged; existing intake/research tests cover these paths. |

The collaborator does not establish a new ingress or candidate-identity validation policy.
Archive extraction is deliberately left with its existing bounded CSV/XLSX processing because
it includes format-specific validation and output construction. Owner ingress has separate
stability/sidecar/hash checks and is not folded into a generic file framework.

## Changes

Production:
- `src/BigBrain.Api/Finance/FinanceDatasetQuarantine.cs` — concrete physical quarantine boundary.
- `src/BigBrain.Api/Finance/FinanceDatasetIntake.cs` — explicit delegation at existing call sites.

Characterization:
- `tests/BigBrain.Api.Tests/FinanceDatasetIntakeTests.cs` — two pre-network gate tests.
- `tests/BigBrain.Api.Tests/FinanceDataProtectionTests.cs` — two cleanup/restart/evidence tests.

Documentation: this report, TESTING.md, STATUS, BACKLOG, Finance module, BB-130 plan,
report catalog and canonical recovery note. Historical reports are not rewritten.

## Security

### Determinism and safety

Unchanged `CanonicalDatasetRevisionIdentityTests` passes before/after. The pinned synthetic ID is
`dataset-v2-31c6ed4a006c7514de08409dca1998be6c8e7af9ee2e06a72cb349f69aa15522`, with the
same SHA-256 digest and `sha256:` checksum prefix. Culture/calendar/decimal-scale, separate-candidate
row-count/replay and source/product tests remain intact. Synthetic legacy ID
`wiki-dde8381ccbe9ccc5` and its persisted evidence remain unchanged. Cleanup assertions retain the
original artifact checksum and byte count. No expectation was relaxed or replaced.

CanonicalDatasetRevisionIdentityV2, promotion SQL, source/product metadata and storage keys are
untouched. No DDL change, schema migration, legacy rewrite, alias or scientific retuning.
Production Finance data was not accessed. No provider acquisition/activation, rights/provenance
change, broker/order/PAPER/LIVE/AUTO or capital allocation. Existing fail-closed gates remain.
Exact production legacy WIKI compatibility remains UNKNOWN; this checkpoint does not revisit it.

## Verification

Before and after production extraction (same four new tests included), **66/66** passed,
zero failed/skipped:

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~FinanceDatasetIntakeTests|FullyQualifiedName~FinanceDataProtectionTests|FullyQualifiedName~CanonicalDatasetRevisionIdentityTests|FullyQualifiedName~FinanceResearchDatasetTests' --logger 'console;verbosity=minimal'
```

```sh
dotnet build BigBrain.slnx --configuration Release --no-restore
dotnet test BigBrain.slnx --configuration Release --no-build --logger 'console;verbosity=minimal'
node scripts/verify-documentation.mjs
git diff --check
git diff --cached --check
```

Release build: **zero warnings/errors**. Full API **644/644**, Sentinel **32/32**, zero failures/skips.
Documentation verification passed (228 Markdown files / 90 unique backlog IDs); working/staged
diff checks passed. Staged Gitleaks v8.28.0 found no leaks. Final intended-file staging is rechecked
before publication. Scanner command:

```sh
git diff --cached | docker run --rm -i --network none zricethezav/gitleaks:v8.28.0 stdin --redact --no-banner
```
The initial sandboxed test invocation was blocked by MSBuild named-pipe permissions before tests;
the authorized external-sandbox rerun passed. All tests use temporary fixtures. No Web-consumed
contract changed, so no local Web rerun; no Compose/runbook change requiring Compose validation.
Branch push alone does not trigger this repository's main/pull-request CI; no candidate CI claim.

## Remaining work

FinanceDatasetIntakeStore still owns acquisition/retry, artifact recording/checksum binding,
CSV and workbook preparation/parsing, validation, promotion, lifecycle/catalog persistence,
cleanup eligibility/state reconciliation and structural initialization. Those responsibilities
were not extracted together. Later persistence/schema/composition work and BB-130D remain separate.

## Resumption

Next action: owner/architect review of this exact branch tip, then explicit approval before any
merge. A possible next intake checkpoint is bounded CSV parsing characterization, separating
format parsing from existing comparison/persistence only if a clean boundary is demonstrated.
Do not start it automatically. Use [canonical recovery](../../../operations/codex-recovery.md)
and [BB-130 plan](../../../architecture/bb-130-stabilization.md) for continuation.
