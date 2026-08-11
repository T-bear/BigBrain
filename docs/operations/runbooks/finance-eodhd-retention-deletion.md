# Finance EODHD retention and deletion

## Scope and safety

This runbook covers only data tagged `EODHD` / `Free` /
`eodhd-free-personal-v2026-08-11`. It does not authorize trading and must not delete another
provider's data. EODHD permits private non-commercial storage/manipulation/analysis while
the account/subscription is active and requires all copies deleted within one month after
termination or expiry.

Loss of network access is an outage, not proof of termination. Set an entitlement end only
from an owner/provider account fact. Once ended, acquisition, replay and derived processing
are blocked immediately; deletion still requires the two-phase owner action below.

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

## Record termination or expiry

Set `FINANCE__EODHD__ACCOUNTACTIVE=false` and
`FINANCE__EODHD__ENTITLEMENTENDSATUTC=<verified UTC instant>`, then recreate API. The UI/API
shows the one-month deadline and blocks acquisition/replay use. Do not set this merely for
HTTP errors or a missing key.

## Preview deletion

Run the API image as a one-shot maintenance command with the same Finance volume and
configuration:

```bash
docker compose run --rm api finance-eodhd-deletion-preview
```

Record the sanitized preview ID and counts. Confirm the scope covers raw content-addressed
payloads, normalized observations, revisions and catalog indexes. The Finance volume is not
included in any BigBrain backup workflow in BB-077; if an operator has independently copied
it, that copy must be enumerated and deleted separately before attesting completion.

## Execute and verify

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
Do not restore deleted EODHD data from a backup after the legal deadline.
