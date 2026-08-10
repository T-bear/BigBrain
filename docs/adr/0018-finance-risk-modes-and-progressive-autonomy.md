# ADR 0018: Finance hard risk and progressive autonomy

- Status: Proposed
- Date: 2026-08-10

## Context

Research evidence does not authorize financial risk, and autonomous authority must not
expand implicitly.

## Decision

Finance uses RESEARCH, PAPER, MANUAL_APPROVAL, LIMITED_AUTO, AUTO and HALTED. Every
transition toward live or greater autonomy requires explicit product-owner action and
documented gates. Hard Risk policy runs below AI and cannot be overridden. Daily loss,
drawdown, exposure, liquidity, data/broker health and emergency rules can disable trading.
Positions require a validated exit model. Compounding adjusts size using current equity
without increasing percentage risk and shrinks exposure after losses.

## Consequences

PAPER is structurally unable to call live execution. AUTO remains bounded policy authority.
STOP ALL TRADING and circuit breakers are release gates before live operation.
