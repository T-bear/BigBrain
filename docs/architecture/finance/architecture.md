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
boundary; BB-071 still blocks provider activation because architecture approval is not a
data license. No adapter is implemented yet.
