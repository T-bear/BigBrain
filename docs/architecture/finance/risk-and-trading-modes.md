# Finance risk, exits, compounding and trading modes

## Hard Risk Engine

The Risk Engine is authoritative below AI and strategies. Versioned policy must cover
maximum capital and percentage risk per trade, total/instrument/sector exposure,
concurrent positions, daily loss, rolling drawdown, consecutive losses, liquidity,
spread, expected reward/risk, allowed instruments/markets, trading hours, data age,
abnormal volatility, broker/API health, suspension and emergency stop.

If `dailyLoss >= configuredDailyLossLimit`, the conceptual state becomes
`TRADING_DISABLED` until policy-defined recovery and explicit authorization conditions
are satisfied. Denial is safe and zero-trade days are valid.

## Exit invariant

Selling is as important as buying. A position cannot open without a defined, validated
exit model unless an explicitly reviewed strategy and policy safely permit otherwise.
Exit mechanisms may include stop loss, profit target, trailing stop, signal invalidation,
time/volatility exit, Risk Engine forced exit and emergency liquidation policy.

## Modes and transitions

```text
RESEARCH → PAPER → MANUAL_APPROVAL → LIMITED_AUTO → AUTO
    └────────────── any operational mode ──────────────→ HALTED
```

Transitions toward greater authority require satisfied evidence gates and explicit
product-owner action. PAPER can never call a live-order endpoint and can never silently
become live. HALTED prevents new exposure and strategy-originated orders, cancels only
eligible pending orders according to policy, may preserve safe exits, and requires
explicit owner action to resume.

## Compounding

`capital_next = capital_current + realized_net_profit_loss` is the conceptual equity
update. Position sizing may use current risk-adjusted equity: sizes can grow gradually
when equity rises and must shrink when equity falls. Compounding never justifies a
higher percentage risk. All sizing uses net results and current hard limits.
