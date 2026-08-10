# Finance mode promotion runbook

Status: design-only for promotion. RESEARCH is represented as the safe domain default and
read-only module status, but Finance has not been deployed and no promotion operation exists.

Promotion is an owner-authorized policy change, never an elapsed-time automation. Before
promotion, record current mode, target, evidence set, policy/risk version, outstanding
defects, broker/legal gate currency, rollback mode and explicit approval.

- RESEARCH → PAPER: deterministic backtesting, hard Risk Engine and versioned strategies.
- PAPER → MANUAL_APPROVAL: accepted paper evidence, tested broker adapter and credentials,
  reconciliation and emergency stop, plus owner approval.
- MANUAL_APPROVAL → LIMITED_AUTO: verified failure handling, limits and audit plus accepted
  evidence and owner approval.
- LIMITED_AUTO → AUTO: accepted long-duration evidence, no critical defect, proven
  reconciliation/circuit breakers and explicit owner approval.

Promotion verification must prove PAPER/LIVE separation and effective limits without
placing an unapproved order. Any mismatch enters HALTED. Rollback lowers authority,
preserves evidence and reconciles broker truth.
