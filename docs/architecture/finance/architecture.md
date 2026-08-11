# Finance target architecture and capability boundary

## Conceptual components

```text
BigBrain Finance
├── Market Data
├── Market Observer
├── Strategy Engine
├── Market Regime Classification
├── Risk Engine
├── Portfolio Engine
├── Trading Controller
├── Broker Adapter
├── Execution Verification
├── Audit / Decision Journal
└── Finance UI
```

Data and strategies may propose a Candidate Trade. Only the Risk Engine and policy can
authorize it for the current mode. Only the Trading Controller can invoke a broker
adapter, and independent verification must resolve the resulting state.

## Trust boundaries

1. Market providers and datasets are untrusted external inputs; validate provenance,
   timestamp, schema, freshness, corporate actions and completeness.
2. Strategies and AI are untrusted proposers. They have no execution authority.
3. Risk/policy is a server-side hard boundary. No proposer can override its denial.
4. Trading Controller validates authorization, mode, immutable preview, idempotency and
   current policy before execution.
5. Broker Adapter alone receives injected credentials. Credentials never enter Web,
   Brain, prompts, domain events, logs or journal.
6. Broker state is authoritative for orders, fills, positions and cash after execution.
7. Finance storage is module-owned; other modules use versioned capabilities, not tables.

Historical bootstrap and live/near-live observations converge only after their separate
provider envelopes pass entitlement and canonical normalization. They share canonical
identity but retain source, product, event/provider/received/knowledge time, raw/adjusted
basis and immutable revision lineage. Overlap, disagreement and late corrections are
explicit evidence; no provider silently overwrites another. The free-first source gate is
documented in [provider selection](market-data-provider-selection.md).

The first authorized adapter, if later approved, is constrained to the existing acquisition
boundary and four-clock observation model. Its quota controller must fail before a configured
daily/minute ceiling, represent 429/temporary failure as provider outage, recover bounded
missing ranges idempotently and never map no response to an unchanged price. These are
adapter responsibilities, not Twelve Data concepts in the canonical domain.

## Autonomic integration

```text
BigBrain Autonomic
  OBSERVE market + portfolio
  DIAGNOSE strategy + regime
  DECIDE candidate action
  POLICY Risk Engine
  ACT Trading Controller
  VERIFY broker reconciliation
```

Brain discovers structured Finance capabilities and uses the normal authorized API.
It never communicates directly with a broker. AUTO means autonomy within an owner-
approved policy, not unrestricted financial authority.

## Consistency and failure

Order intent needs a stable decision/preview identity and an idempotency boundary.
Timeout or transport success does not establish execution. The controller records an
uncertain outcome, blocks conflicting action and reconciles before any retry. Material
cash, position, order or fill mismatch suspends automated trading.

## M1 implementation boundary

M1 implements domain contracts and a deterministic in-memory evidence slice in
`BigBrain.Modules`. Strategy evaluation, risk, policy and decision are distinct types.
The module registry exposes only `finance.research.read`; there is no Finance-specific
HTTP mutation, broker port, network client, executor, persistence or Web component.

## Planned M2 market-data boundary

Provider adapters normalize into immutable canonical datasets before strategy access.
Canonical identity uses an internal ID plus MIC/currency and time-bounded provider symbol
mappings. Dataset provenance, raw/adjusted state, corporate actions, corrections, market
calendar and ingestion metadata are mandatory. See
[provider selection](market-data-provider-selection.md). Accepted ADR 0021 governs this
boundary. BB-071 now contains positive human Twelve Data Personal-plan evidence, but Basic
is insufficient and no provider has been selected or activated; architecture or entitlement
evidence alone is not activation approval. No adapter is implemented yet.

BB-075 adds a hard current product constraint of 0 SEK for external market data. Its fresh
source sweep cleared no exact free product, so the planned adapter/persistence boundary
remains inactive. The fail-closed read projection names the zero-cost entitlement gate and
never substitutes synthetic observations in production.

BB-076 adds ADR 0022's capability-scoped owner-accepted personal-research evidence class.
It applies only to legitimate 0-SEK private read-only research and cannot override explicit
negative terms or technical controls. Stooq failed the technical activation gate because
its official CSV request returned a JavaScript verification challenge.

The canonical memory and entitlement model is specified in
[market-data memory and provenance](market-data-memory-and-provenance.md). Provider-neutral
policy/provenance types may be implemented against fixtures before BB-071; real provider
payloads may not be persisted or reused until their exact policy returns `Allowed`.

BB-045 now implements this provider-neutral contract in `BigBrain.Modules.Finance` with no
adapter or IO. Canonical IDs survive ticker changes; provider/product/symbol/MIC mappings
are effective-dated, and synthetic daily OHLCV/corporate actions normalize with immutable
revision and entitlement references. Evaluation additionally matches provider/product exactly;
an otherwise valid policy for another product fails closed with a stable diagnostic code.

The fixture-only session/replay foundation represents Trading, Closed and Unknown calendar
knowledge with explicit timezone conversion, classifies absence without inferring provider
fault and replays a single immutable revision by supplied availability timestamps. Stable
priority/tie-break ordering, effective-date symbols and explicit dividend/split events
prevent host-timezone dependence, lookahead and silent raw-price rewriting. It is not an
exchange-calendar database, persistence layer or backtest engine.

The immutable revision assembler now defines the correction boundary in memory. A child
revision inherits its parent's immutable membership; a correction removes no historical
record and instead relates active original and newly introduced replacement member IDs at
an explicit UTC availability time. A linear acyclic supersession catalog makes old revision
IDs reproducible and chooses current knowledge through inclusive `available <= as-of`.
Dataset/provider/product scope cannot change inside the chain. Persistence remains deferred.

The fixture-only acquisition preparation now defines the adapter handoff without adding a
provider transport. A request fixes source/product, provider and canonical identity, range,
daily/raw-adjusted scope, UTC acquisition, source timezone, cursor, policy and destination
revision. Immutable batches add response identity, pagination, completeness, provenance,
raw observations/actions, gaps and correction declarations. Before adapter invocation, the
gate requires exact permission for historical analysis, backtesting, derived metrics,
long-term storage and persistence. The pipeline then reuses canonical normalization and
revision assembly; an acquisition journal records reproducibility/audit counts and deletion
metadata without payloads or credentials. Only `SyntheticFixture` evidence is implemented.

The fixture persistence boundary now adds an immutable manifest, deterministic content
fingerprint and provider-neutral storage contract around those revisions. Complete-only
append, idempotent identity, explicit conflicts, range/action/gap reads, lineage, integrity,
policy-scoped enumeration and deletion are proven by an in-memory reference. This is a
domain/benchmark boundary, not a production repository, API endpoint or runtime database.

## Live observation and shadow evidence boundary

Historical replay and forward observation share canonical identity, entitlement,
provenance and immutable revision references but remain distinct evidence streams. Current
observations record market event time, provider production time, BigBrain receive time and
first-usable knowledge time. A strategy evaluation may consume only evidence whose knowledge
time is at or before its supplied boundary. Delayed or end-of-day evidence is labelled as
such; technical accessibility never upgrades freshness or rights.

The fixture-only live feed and shadow pipeline implement `OBSERVE → RECORD → MEASURE →
COMPARE → SCORE → BUILD EVIDENCE`. They do not implement the Trading Controller, broker
adapter, order intent, portfolio, PAPER executor or self-modifying strategy. Predictions
bind strategy/configuration/feature/risk/build and data/policy versions and remain immutable;
outcomes append later. No result can promote a version or alter risk without a future
separate governed workflow.

## Read-only observation projection

BB-074 adds the last, read-only portion of the future flow:
`authorized adapter (future) → journal → normalization/quality/session/gaps → immutable
Finance memory → IFinanceObservationReader → versioned API → Finance Web`. Today only the
projection exists and its production reader fails closed with no provider or observations.
Synthetic snapshots are test-only. This is not a parallel data model and has no write/order
capability.
