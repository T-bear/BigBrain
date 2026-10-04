# Finance controlled Alpaca acquisition and build identity

BB-132F1 provides a **review candidate**, not activation permission. Finance remains
**RESEARCH / 0 SEK / NONE**. [Implementation/verification evidence](../../reports/features/finance/bb-132f1-controlled-activation-gate-20261004.md).
The [BB-132F configuration and receipt contract](../../architecture/finance/market-data-memory-and-provenance.md#configuration-and-activation-boundary)
remains authoritative. No new scientific grant, market store, provider semantics or public endpoint.

## Exact build and running identity

After independent review, exact-SHA owner acceptance and merge, fetch and check out the accepted
main with no tracked delta. `scripts/build-bigbrain-api.sh <exact-accepted-main-SHA>` verifies
HEAD/origin-main equality and clean tracked content, builds an immutable `git archive` context,
and supplies that SHA to the existing API Docker build. Ignored/untracked local files and the local
secret file cannot enter that archive. The helper **only builds**; it never starts/recreates services.
Its Compose build explicitly uses `/dev/null` instead of the operator's secret file.

The API embeds `GitRevision` assembly metadata at build time and carries the same
`org.opencontainers.image.revision` OCI label. Builds without supplied provenance report `UNKNOWN`.
The only accepted revision syntax is a nonzero40-character lowercase Git SHA. This is a trusted
build-process assertion, not a cryptographic attestation against a privileged image builder.
No environment variable, HTTP caller, provider response or CLI argument can replace the compiled
process revision. Normal SDK builds intentionally remain UNKNOWN rather than guessing from a checkout.

After a separately authorized deployment, verify the **running container's image ID**, its image
revision label, and its compiled revision against the exact accepted SHA. Do not inspect only a
mutable image tag. Print only the selected label, never full `docker inspect`/resolved environment:

```bash
docker compose exec -T api dotnet BigBrain.Api.dll system-build-identity
```

This command reads assembly metadata and exits before configuration/host initialization. It does
not open Finance storage or start a listener. UNKNOWN/mismatching image/process identity blocks
controlled acquisition. The one-shot itself rejects UNKNOWN compiled identity; the operator must
still verify the reported known SHA is the owner-accepted deployed SHA.

## External configuration, without activation

Compose now forwards the existing external keys below. No values are installed by F1. Credentials
remain in the existing ignored, mode0600 local `.env`/external process environment, never appsettings,
Git, image build arguments/layers, URLs or logs. Enter credentials only through a local private editor
or non-echo terminal input; never paste them into chat or include literal credentials in commands.
Do not render `docker compose config` or full container environment when real keys are present.

| External variable | Meaning / default |
| --- | --- |
| `FINANCE__OBSERVATIONRUNTIME__ENABLED` | **false**; must stay false for one-shot |
| `FINANCE__ALPACADAILYOBSERVATION__ENABLED` | false; separate transport permission |
| `FINANCE__ALPACADAILYOBSERVATION__APIKEY` | Empty by default; local secret only |
| `FINANCE__ALPACADAILYOBSERVATION__APISECRET` | Empty by default; local secret only |
| `FINANCE__ALPACADAILYOBSERVATION__TIMEOUTSECONDS` |15; accepted1–30 |
| `FINANCE__OBSERVATIONRUNTIME__OWNERACCEPTANCEVERSION` | Empty; requires `bb132e-owner-data-use-risk-v1` |
| `FINANCE__OBSERVATIONRUNTIME__POLICYRECORDEDATUTC` | Default missing/sentinel; explicit real UTC policy-recording instant required |
| `FINANCE__OBSERVATIONRUNTIME__AFFECTEDUSEENABLED` | false; explicit owner-scoped use permission |
| `FINANCE__OBSERVATIONRUNTIME__CURRENTDELETION` | Unknown; concrete applicable obligations still block affected use |
| `FINANCE__OBSERVATIONRUNTIME__INSTRUMENTSJSON` | Empty; explicit reviewed1–4 entry JSON array |
| `FINANCE__OBSERVATIONRUNTIME__CADENCEMINUTES` |360; accepted60–1440, unused by one-shot |
| `FINANCE__OBSERVATIONRUNTIME__LOOKBACKDAYS` |3; accepted1–7, unused by one-shot |

`InstrumentsJson` is bounded to16384characters, depth4,1–4objects, exact DTO property names,
no unknown/duplicate properties. It constructs the same `ObservationRuntimeInstrument` objects
and existing canonical instrument/mapping types. It cannot be combined with the existing indexed
`Instruments` configuration; ambiguity or invalid JSON empties the list and fails acquisition closed.
Accepted mapping validation, uniqueness, ranges and policy checks are shared with the runtime.
Credentials alone **never enable** either unattended collection or scientific evaluation.

Each reviewed entry needs `InstrumentId`, `DisplayName`, `ProviderSymbol`, `Mic`, `VenueCode`,
`VenueName`, `ValidFrom`, optional `ValidTo`, and `MappingEvidence`. Source-day validity must be
established; do not invent a historical validity date. The initial one-shot allows only explicitly
configured `US:XNAS:AAPL`/AAPL or `US:XNAS:MSFT`/MSFT, Equity/USD/XNAS. No entries are defaulted.
SPY is excluded; it must never be mislabelled Equity to pass this gate. IEX is a feed, not listing MIC.

Configuration is frozen at process startup. Editing `.env` does not change an existing container's
environment. Passing keys later requires a separately authorized same-revision API recreation with
runtime still false. Do not restart the appliance or other containers as a shortcut. Review existing
legacy EODHD/research-worker enablement before any restart; F1 authorizes none of their work.

## One-shot operator contract

Only after separate owner authorization for **one** real request, verified running revision,
reviewed configuration and local prerequisites:

```text
docker compose exec -T api dotnet BigBrain.Api.dll finance-alpaca-daily-once <configured-canonical-id> <yyyy-MM-dd>
```

The two arguments select one reviewed instrument and one completed source day; no URLs, feed,
credential, timestamp, policy or configuration overrides are accepted. Unknown/extra arguments fail.
Use the already deployed API container/environment/volumes, not a second differently configured host.
The command exits before starting the web host, lifecycle service, model, scheduler or scientific
workers. Runtime Enabled=true refuses even if no background request is currently active.

Before network: shared configuration/mapping/current-policy checks; known compiled revision;
prior New York source day and accepted US session; mapping/instrument date validity; existing
system recovery owner's **read-only** SQLite quick-check of lifecycle and Finance stores, current
clock-sync marker and critical free-disk bound. Missing stores are not created as an operational
workaround. The maintenance check is deliberately stricter than the hosted clock sanity fallback:
it requires the synchronized marker, not merely a plausible year. It does not mark a second
appliance session clean or repair the running service. Unknown running-service readiness remains
an operator gate; these checks do not certify systemd health.

A local same-Finance-path exclusive file handle blocks overlapping one-shot processes without a
queue. Its empty lock pathname remains after exit; do not delete it to defeat a held lock. This is
single-host control, not a distributed lease or protection against a differently configured runtime.
Existing schema/watermark and bounded receipt checksums/lineage are validated before network.
No migration, grant renewal or cutoff sealing is performed by that preflight. Corruption or the
existing1000-receipt bound blocks work rather than silently skipping evidence.

One existing Alpaca daily adapter call → `EodhdMarketMemory.ReobserveDailyAsync` → existing
transactional receipt. Fixed HTTPS/1Day/IEX/raw/no redirects/proxy/retry and bounded response/deadline
remain unchanged. Native daily eligibility remains final authority. SIGINT/SIGTERM/cancellation
propagate; cancellation before commit publishes no partial receipt. New/Duplicate/Revision use
existing identity and immutable predecessor selection. No shadow/science/trading call follows.

Success exits0 with a bounded JSON line: kind, compiled revision, instrument/symbol, source day,
provider/dataset/origin, receipt/checksum, acquisition/ingestion/knowledge times and predecessor.
Failure exits1 with only finite `stage`/`category`; never arbitrary exception/body text, keys, headers
or raw OHLCV/prompt payloads. A failure after a possible commit is uncertain evidence: **STOP**,
inspect existing receipt through Finance ownership; never retry/refund/delete to manufacture success.
Each separate invocation is a new operator action requiring its own authority; F1 is not a durable
one-use authorization-token system.

## Later activation sequence — not executed by F1

1. Accept exact F1 SHA; resolve the previously reported failed appliance lifecycle state separately.
2. Build/deploy that accepted revision under separate authorization; verify running image/process SHA.
3. Enter secrets locally, review the tiny effective mappings/current owner policy, and keep runtime false.
   If configuration was entered after deployment, separately authorize recreation of the same API image.
4. Verify clock, existing Finance schema/evidence and storage; choose one eligible prior New York session.
5. Execute exactly one owner-authorized AAPL acquisition. Any failure means STOP, no retry.
6. Review sanitized receipt, existing Finance reopen and point-in-time evidence: source date is not
   knowledge time; provider publication remains unknown when absent; acquisition may equal ingestion
   within clock resolution, but neither is backdated to the market event. No invented inequality/ticks.
7. Return to owner/architect. Unattended activation remains a separate later decision.

No real provider call, credentials, deployment, restart, live universe, SHADOW execution, Qwen,
research grant change, PAPER/LIVE/AUTO, orders/positions/capital or next sprint is authorized here.
