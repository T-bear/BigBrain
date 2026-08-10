# Finance BB-045 – market-data policy/provenance foundation

## Metadata

- Date: 2026-08-10
- Scope: first provider-neutral BB-045 implementation slice
- Related commit: assigned on publication

## Status

The provider-neutral entitlement/provenance slice is implemented and automatically
verified. BB-045 remains in progress; BB-071 remains open and blocks external provider
activation and persistence of real provider data. Nothing was deployed or runtime-enabled.

## Evidence

Host .NET 10 SDK verification:

- `dotnet restore BigBrain.slnx` — PASS
- `dotnet build BigBrain.slnx -c Release --no-restore` — PASS, zero warnings/errors
- `dotnet test BigBrain.slnx -c Release --no-build --no-restore` — PASS
- API tests: 259 passed; Sentinel tests: 32 passed; total: 291 passed, 0 failed/skipped

New deterministic tests use only `ExampleData`, `Synthetic-EOD-Personal`, `TEST-XSTO`
and locally constructed metadata. They contain no real provider response or credential.

## Changes

`MarketDataEntitlements.cs` introduces strongly typed allowed uses, policy/provider/product
and evidence references, UTC validity, retention/deletion classifications, dataset/revision
identity, checksum and status, provenance/quality, and raw/derived parent lineage. The pure
evaluator returns effective allow/deny, source Allowed/Denied/Unknown, stable reason code
and policy reference.

Fail-closed invariants cover missing policy, unknown/unsupported use, exact provider/product
mismatch, explicit denial, expired/not-yet-valid policy, persistence denial/uncertainty and
post-subscription retention denial/uncertainty. Explicit valid permission applies only to
the declared scope. Derived data requires parent revisions and is not automatically free.

## Security

Detta är en sanerad GitHub-version. No account, key, token, legal document body, provider
payload, HTTP client, database, filesystem operation, logger, broker or trading capability
was added. Evidence references are validated opaque identifiers.

## Remaining work

- BB-071 remains `Pågår – väntar på leverantörsbekräftelse`.
- BB-045 still needs canonical time-bounded instrument/provider-symbol identity, OHLCV and
  corporate actions, quality findings, deterministic normalization/replay, a measured
  persistence decision and an authorized adapter.
- No policy serialization, API or persistence contract is introduced by this slice.

## Resumption

Implement canonical instrument identity and time-bounded provider-symbol mappings using
synthetic fixtures only. Then add synthetic OHLCV/corporate-action normalization and
quality/gap handling. Do not add a provider adapter or persistence until BB-071 supplies
owner-reviewed authorization.
