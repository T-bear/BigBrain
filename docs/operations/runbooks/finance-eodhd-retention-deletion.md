# Finance EODHD retention and deletion

Issue #61 (2026-11-08): EODHD Free personal retention is now `LongTerm/None`
(no automatic deadline) under the [current Terms](https://eodhd.com/financial-apis/terms-conditions)
and [storage FAQ](https://eodhd.com/help/storage). Private data obtained under
an authorized subscription may be retained after cancellation; new API
acquisition and entitlement-gated processing remain blocked without
current authorization. EODHD is still provider-tagged and `Restricted`,
**not** WIKI public-domain backup-eligible. A new copy or restore requires
separate owner authorization; this amendment creates none.
BB-084 source-isolation remains: queries must not delete `NASDAQ-WIKI`,
its public-domain quarantine artifact, `wiki-*` revisions or their exact
derived lineage. Shared symbols/dates do not establish shared retention.

## Scope and safety

This runbook covers only data tagged `EODHD` / `Free` /
`eodhd-free-personal-v2026-08-11`. It does not authorize trading and must not delete another
provider's data. EODHD permits private non-commercial storage/manipulation/analysis and
retention of previously obtained data and derived evidence after termination,
without an automatic deletion deadline, under the official pages reviewed
2026-11-08. The previous one-month rule is preserved as historical policy in
ADR 0028 and issue #61, not as a current deletion instruction.

Loss of network access is an outage, not proof of termination. Set an entitlement end only
from an owner/provider account fact. Once ended, acquisition, replay and other entitlement-gated processing
remain blocked until separately authorized; stored bytes are not
automatically destroyed. Voluntary deletion still requires the two-phase
owner action below.

## Configure the free account

1. Create an EODHD `Free` account without entering payment details.
2. Put the API token in the deployment environment as `FINANCE__EODHD__APITOKEN`; never
   paste it into Git, logs, reports or the Web UI.
3. Set `FINANCE__EODHD__ACCOUNTACTIVE=true` and `FINANCE__EODHD__ENABLED=true`.
4. Leave `FINANCE__EODHD__ENTITLEMENTENDSATUTC` empty while the account is active.
5. Recreate only API and Web through the normal Compose deployment procedure.
6. Verify `/health`, `/api/v1/modules/finance/observation`, EOD labeling, counts, coverage,
   revision and retention `active`. Never print the resolved Compose environment.

The worker makes at most eight one-year daily requests per UTC day, one per watchlist symbol,
skips symbols already acquired successfully that day, and uses one-second spacing. This
stays below the documented 20 calls/day free limit. It uses
no corporate-action, intraday or live endpoint.

## Sanitized runtime evidence

Run the one-shot command with the same Finance volume. It performs no provider request and
prints only journal counts, symbols, coverage, revision IDs and replay checksums:

```bash
docker compose run --rm --no-deps --entrypoint dotnet api BigBrain.Api.dll finance-eodhd-runtime-evidence
```

Use it before and after API/Web recreation. An unchanged request count proves the same-day
skip without spending another free-tier request. Never replace this with an environment dump
or raw SQLite/payload output.

Build or idempotently verify the current feature revision from local memory only:

```bash
docker compose run --rm --no-deps --entrypoint dotnet api BigBrain.Api.dll finance-features-build
```

This command performs no provider request. Retain its sanitized feature revision, value/
warmup/quality counts and checksum. Inspect bounded values through
`GET /api/v1/modules/finance/features`; never dump the database or licensed payloads.

Build or idempotently verify immutable reference backtests from local memory only:

```bash
docker compose run --rm --no-deps --entrypoint dotnet api BigBrain.Api.dll finance-backtests-build
```

This command performs no provider request. Its sanitized output includes exact market/feature revisions, run IDs/checksums, sessions, feature reads, simulated fills, events, elapsed time and idempotency. Deletion preview must include dependent backtest runs, events, fills and equity points; result JSON also contains metrics. Generic strategy definitions may remain.

Build or idempotently verify BB-081 robustness evidence from the same local memory:

```bash
docker compose run --rm --no-deps --entrypoint dotnet api BigBrain.Api.dll finance-robustness-build
```

The command performs no provider request. Its output is sanitized to lineage, evaluation IDs/checksums, bounded run/window/variant counts, elapsed time and idempotency. Preview/confirm deletion additionally covers evaluations, run references, walk-forward windows, parameter sensitivity, cost sensitivity, aggregate results and indexes. Generic evaluation-plan code may remain.

## Record termination or expiry

Set `FINANCE__EODHD__ACCOUNTACTIVE=false` and
`FINANCE__EODHD__ENTITLEMENTENDSATUTC=<verified UTC instant>`, then recreate API. The UI/API
shows expiry without any deletion deadline and blocks acquisition/replay use.
Do not set this merely for
HTTP errors or a missing key.

## Voluntary deletion only (separate owner authorization)

There is no automatic EODHD cancellation deletion. The existing commands
below are retained solely for a separate, explicitly authorized destructive
operation. Do not run them for this policy reconciliation.

### Preview deletion

Run the API image as a one-shot maintenance command with the same Finance volume and
configuration:

```bash
docker compose run --rm api finance-eodhd-deletion-preview
```

Record the sanitized preview ID and counts. Confirm the scope covers raw content-addressed
payloads, normalized observations, market revisions, dependent feature values/revisions and
catalog indexes. Generic feature definitions contain no provider values and may remain. The Finance volume is not
included in any BigBrain backup workflow in BB-077; if an operator has independently copied
it, that copy must be enumerated and deleted separately before attesting completion.

### Execute and verify

After explicit product-owner approval for the displayed preview:

```bash
docker compose run --rm api finance-eodhd-deletion-execute <preview-id>
```

The exact current preview ID is mandatory; changed scope invalidates confirmation. Preserve
only the sanitized receipt ID, timestamp, counts and fingerprint. Restart API, verify zero
EODHD observations/payloads/revisions, retention `deletionComplete`, and confirm unrelated
datasets/volumes remain intact. Never retain prices inside the receipt.

## Rollback

Deletion is intentionally irreversible. There is no data rollback after execution. Before
execution, rollback means cancelling the operation and correcting account-state evidence.
Do not automatically restore deliberately deleted EODHD data, or create
a new copy without source-specific authorization. There is no automatic
cancellation deletion deadline under the currently reviewed EODHD Free
personal terms.

BB-089 risk evaluations reference EODHD market/feature revisions and contain minimized derived rule
evidence; they remain provider-tagged and are removed before source lineage
only during a separately approved voluntary EODHD deletion. Pure risk-halt
transition metadata contains no copied market values and may remain as indefinite audit evidence.
EODHD-derived risk evaluations must not enter a WIKI-only public-domain
backup. Retention permission does not itself grant a new EODHD copy.
