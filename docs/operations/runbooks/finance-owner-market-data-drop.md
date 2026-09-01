# Finance owner market-data drop

## Boundary

This is a local owner-controlled ingress for untrusted research files. It is not public, trusted or
an automatic importer. Finance remains `RESEARCH / 0 SEK / NONE`; inspection never authorizes a
provider, strategy or trade and never promotes data automatically.

## Host setup and use

The default host folder is `./data/finance/market-data-drop` relative to the Compose project. Set
`FINANCE_MARKET_DATA_DROP_PATH` to another owner-controlled absolute host path when required. The
API sees it read-only at `/finance-data/market-data-drop`; quarantine remains inside the separate
`finance-market-data` volume. The directory must be traversable/readable by the container user;
the deployed default uses mode `0755` (owner-only write). Dropped files and sidecars must likewise
be readable by that user, normally mode `0644`, while the Compose mount prevents container writes.

1. Copy one `.csv` or `.zip` file completely into the host folder.
2. Optionally add `<basename>.metadata.json` before marking the dataset ready.
3. Create an empty `<full-filename>.ready` marker, for example `prices.csv.ready`.
4. Wait for the configured scan interval (default 30 seconds).
5. Read `GET /api/v1/modules/finance/datasets` through the normal authorized API.

Never place credentials, cookies, tokens or passwords in filenames, datasets or sidecars. Do not
use this folder to bypass provider restrictions; possession is not entitlement.

## Supported input and limits

Initial input is top-level CSV or ZIP containing one safe CSV. The existing configured defaults
limit the source artifact to 500,000,000 bytes, ZIP entries to 100 and total expanded bytes to
1,000,000,000. Sidecars are limited to 65,536 bytes. Nested archives, path traversal, symlinks,
reparse points, binary/non-CSV content and unsafe names are rejected. The parser reports schema,
UTF-8/comma interpretation, date/row/instrument coverage, invalid OHLCV, duplicate/conflicting
keys, mappings and cross-source classification. Raw/adjusted semantics remain `UNKNOWN` unless
independently proven.

Optional sidecar fields are `sourceProvider`, `originalUrl`, `downloadedOn`,
`licenseOrTermsUrl`, `declaredLicense`, `ownerNotes`, `expectedSymbols`, `expectedMarket`,
`priceBasis`, `downloadedManually` and `permissionReference`. URLs must be absolute HTTP(S).

## Outcomes, restart and retry

Stable bytes are hashed and copied through `.partial` into BB-084 quarantine before inspection.
The same artifact plus sidecar is idempotent; changed bytes or sidecar creates a distinct candidate.
Restart safely rescans markers and resolves the same content identity. A waiting result means the
data file is missing/non-regular or changed during inspection. A rejected or manual-review result
remains quarantined under existing retention rules and has no canonical revision.

Correct the owner-side file or sidecar, ensure copying has finished, and create/retain the marker
to retry. Removing a marker stops future scans but does not erase durable quarantine evidence.
Removing canonical or quarantine evidence is not part of this runbook. Even `APPROVED` means
`READY_FOR_EXPLICIT_PROMOTION_REVIEW`; promotion requires a separate deliberate authorized action.
