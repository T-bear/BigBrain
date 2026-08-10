# Finance decision journal and observability

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

M1 provides an in-memory append-oriented model that captures NO TRADE, REJECTED and paper
intent decisions, risk/policy results, reason codes and observation→evaluation→decision
correlation. Persistence, integrity protection, retention and outcome reconciliation are
still deferred; the current journal is test/evidence foundation only.
