# Finance decision journal and observability

Provider entitlement note 2026-08-11: prospective shadow predictions, later outcomes and
their retained metrics are separate artifact classes in BB-071. Twelve Data's public Basic
material does not explicitly resolve their forward-testing or post-termination retention.
They remain synthetic-only until written evidence and owner approval; an internal-use label
alone cannot authorize the journal.

BB-075 update: no zero-cost source cleared this artifact lifecycle. Twelve Data Personal
clears the submitted scope but is an inactive paid fallback under the 0-SEK budget. Real
acquisition and shadow evidence therefore remain disabled; synthetic evidence is not a
production fallback.

Every financial decision must be reconstructable. The append-oriented journal references
timestamp, safe market snapshot, data/strategy/policy versions, signals, regime, proposed
action, risk result, policy result, preview, execution and verification results, exit
reason and realized net P&L. It must answer why BigBrain bought and why it sold without
logging credentials, authentication material or unnecessary account identity.

Planned health signals include market data, broker, strategy, risk and execution health.
Metrics include candidates, submitted/rejected/filled orders, daily P&L, drawdown,
exposure and reconciliation failures. Labels must avoid sensitive account or instrument
cardinality where unnecessary. Alerts distinguish degraded observation from a mode that
can create exposure; material uncertainty causes suspension.

Journal integrity, retention, access control, redaction, clock source and export/reporting
requirements must be finalized before PAPER persistence. Corrections append a linked
record rather than rewriting history.

The M2 historical replay foundation supplies future journal-compatible stable event
identities for session, observation availability, quality/gap and corporate-action evidence.
Ordering is derived from UTC effective time, explicit event priority, canonical instrument
and stable tie-breakers—not wall clock, locale, hash or storage enumeration. No persistent
journal write was added by this slice.

Future journal/report evidence must record both the selected assembled revision ID and its
knowledge/as-of boundary. Correction ID, original/replacement member IDs, reason/evidence
and superseding revision make later facts explainable without rewriting earlier decisions.
The current assembler is in-memory only and creates no persisted journal.

The M2 acquisition journal is a separate immutable evidence model, not the future trading
decision journal. It records the acquisition request/source/range, policy evidence and
retention/deletion obligations, received/accepted/rejected/duplicate counts, quality
findings, outcome reason and resulting revision. It deliberately excludes credentials,
headers and raw payloads. A future persistent correlation graph may reference its request
and revision IDs but must not copy licensed data into audit records to evade deletion.

Persistence deletion receipts follow the same rule: retain request/scope, policy evidence,
deleted revision IDs and manifest fingerprints, but not licensed observations, actions or
raw payload. This preserves an auditable fact of deletion without laundering provider data
into the journal. A future durable journal must correlate receipt and acquisition IDs.

The BB-073 shadow journal is a separate prospective evidence chain. A prediction freezes
strategy/configuration/feature/risk/build versions, knowledge boundary, observation IDs,
direction, score, horizon, hypothetical entry, regime and reason codes. A later outcome is
appended at/after the horizon with actual path metrics and hypothetical costs; it cannot
replace or edit the prediction. This prevents hindsight-labelled decisions and supports
future calibration/drift evidence. It contains no order, broker or credential reference.

M1 provides an in-memory append-oriented model that captures NO TRADE, REJECTED and paper
intent decisions, risk/policy results, reason codes and observation→evaluation→decision
correlation. Persistence, integrity protection, retention and outcome reconciliation are
still deferred; the current journal is test/evidence foundation only.

## Persistent evidence shape

Future records form an append-only correlation graph from market snapshot and dataset
revision through signals, strategy/parameter version, risk/policy state, decision and
reason codes, rejected alternatives, order/no-trade, execution, costs and slippage to
realized/unrealized outcomes at named horizons and post-trade evaluation. Valuation and
outcome records identify their own dataset versions. NO TRADE and REJECTED remain queryable
to prevent winner-only analysis.

Each reference is subject to the originating dataset's entitlement. The journal may retain
sanitized policy/deletion audit metadata only when allowed; it must not embed raw provider
data to bypass a deletion obligation. See
[market-data memory and provenance](market-data-memory-and-provenance.md).

BB-074's Web snapshot is presentation, not evidence authority. It can display sanitized
freshness/session/quality and revision/provenance summaries, but cannot authorize
entitlement, change mode, write the journal or create orders.
