# BB-128A Alpaca Live Market Data Activation Readiness

Detta är en sanerad GitHub-version. It contains no credential, private address, provider payload
or market observation.

## Metadata and result

- Date: 2026-09-02
- Baseline: `ed1bae8681fd18797c89f6ff140ecb92de1c8912`
- Finance: `RESEARCH / 0 SEK / NONE`
- Result: **IMPLEMENTED / AUTOMATICALLY VERIFIED / ACTIVATION BLOCKED PENDING EXTERNAL ENTITLEMENT CONFIRMATION**

## Status

Activation readiness is implemented and automatically verified. Acquisition remains blocked.

## Evidence

Evidence consists of the existing BB-125 entitlement decision, current first-party Alpaca
documentation and deterministic local tests. No provider request was evidence for this sprint.

## Existing architecture reused

BigBrain already has the provider-neutral `LiveMarketObservation`, `LiveStreamId`, immutable
observation/revision/policy/provenance identity, four causal timestamps, freshness/delay rules,
append-only corrections, `LiveObservationEntitlementGate`, synthetic feed and shadow
prediction/outcome journal. The deployed EODHD prospective system is a separate source-specific
use of those principles and was not changed. No second live model, persistence store, shadow
journal, provider framework or backtester was created.

The concrete gap was an owner-visible candidate capability/readiness decision and a tested
activation boundary for a future provider. BB-128A adds `AlpacaBasicIexReadiness`, which describes
the candidate without containing a network client, endpoint or secret, and
`AlpacaLiveActivationGate`, which delegates to the existing `MarketDataEntitlementEvaluator`.

## Technical capability

Current Alpaca first-party documentation reviewed 2026-09-02 says Basic is zero-cost for its
account holders, covers US stocks/ETFs, provides real-time IEX-only data, limits WebSocket
subscriptions to 30 symbols and distinguishes `/v2/iex`, `/v2/sip` and `/v2/delayed_sip`. IEX is a
single exchange; it is never represented as SIP, NBBO or consolidated US coverage.

Trade, quote and bar schemas expose RFC-3339 field `t` as market event time. The documentation does
not expose a separate provider-processing timestamp. BB-128A therefore records that mapping as
`UNKNOWN`; a future adapter must not substitute receive time silently. BigBrain receive and
knowledge timestamps remain locally assigned and causally ordered by the existing constructor.

First-party evidence:

- <https://docs.alpaca.markets/us/docs/about-market-data-api>
- <https://docs.alpaca.markets/us/docs/market-data-faq>
- <https://docs.alpaca.markets/us/docs/real-time-stock-pricing-data>
- <https://docs.alpaca.markets/us/docs/streaming-market-data>

## Entitlement and activation

BB-125 remains authoritative: persistent raw/normalized storage, accumulated observations,
backups, revisions/checksums, derived research evidence and post-account termination retention or
deletion are unresolved. Policy evidence remains `HumanConfirmationRequired`; every applicable
use and lifecycle decision remains `Unknown`. The activation result is consequently:

- technical capability: `contractKnownWithMappingGap`
- entitlement: `humanConfirmationRequired`
- activation: `notActivated`
- acquisition: `blocked`
- reason: `durableDataLifecycleNotConfirmed`

The read-only v1 endpoint is `/api/v1/modules/finance/providers/alpaca/status`. It returns only
sanitized capability and policy metadata. The tested execution gate throws before invoking a
future acquisition delegate while blocked. No adapter skeleton was justified because it would add
no behavior beyond this boundary before written entitlement clearance.

## Changes

Added a narrow provider capability/status contract, an existing-evaluator-backed activation gate,
deterministic IEX stream identity, a read-only API route and focused tests/documentation.

## Stream identity and safety

The deterministic candidate stream ID binds Alpaca, Basic, IEX, instrument, trade/quote/bar type,
granularity and `alpaca-basic-iex-live@bb-128a-v1`. The factory rejects consolidated coverage.
Existing `LiveMarketObservation` validation rejects causal timestamp inversion and a positive-delay
observation labelled real-time. Corrections remain append-only via `CorrectsObservationId`.

No Alpaca account, terms acceptance, credential, HTTP/WebSocket request, market observation,
persistence, historical download, broker/order/PAPER/LIVE/AUTO capability or deployment was
created. No configuration placeholder was needed: future secrets must use the existing deployment
secret boundary only after a separate owner-approved activation.

## Security

The status surface contains no secret field or endpoint URI. Acquisition is denied before the
future provider delegate can execute, and unresolved evidence never becomes allowed.

## Verification and remaining action

Focused tests cover blocked-before-delegate execution, IEX/single-exchange identity, stream
separation, absent credential disclosure and existing temporal fail-closed rules. Full affected
backend tests, Release build, documentation/Compose/diff and staged secret checks are publication
gates.

Local result: Alpaca-focused tests 4/4, combined live/synthetic-shadow/EODHD-shadow regression
24/24 and full API 599/599 passed. Release build passed with zero warnings/errors. Documentation,
Compose, diff and staged secret results are recorded at publication.

Owner action: obtain and provide Alpaca's written confirmation covering the full durable data
lifecycle. A later separately approved sprint must translate that evidence into an entitlement
policy before implementing any adapter or credential configuration. Provider-time semantics also
need explicit adapter treatment if Alpaca still exposes no separate value.

## Remaining work

Written durable-lifecycle confirmation, a reviewed policy update and explicit provider-time mapping
are required before any separately authorized activation implementation.

## Resumption

Resume only from published GitHub evidence plus owner-supplied first-party confirmation. Do not add
credentials, an adapter or acquisition while the authoritative gate remains blocked.
