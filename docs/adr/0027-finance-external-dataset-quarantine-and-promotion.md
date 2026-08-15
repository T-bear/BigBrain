# ADR 0027: Finance external dataset quarantine and deterministic promotion

- Status: Accepted through product-owner authorization of BB-084
- Date: 2026-08-15
- Related: ADR 0021–0026

## Context

Public downloadability does not establish rights, provenance, trustworthy schema or canonical
identity. External archives are untrusted input and must not be read directly by strategy,
feature or backtest code.

## Decision

Every external historical dataset enters a Finance-owned quarantine as an immutable artifact.
Its candidate record binds source/final URL, retrieval time, acquisition method/request count,
size, SHA-256, type/compression, license evidence, underlying provenance and state. Inspection
is non-executing, bounded and fail-closed. ZIP extraction rejects rooted/traversal paths,
excess file count and excessive expanded size; parsers bound line length and reject binary or
formula-like ticker input.

`dataset-promotion-v1` requires exactly one deterministic result for integrity, rights,
provenance, schema, semantics, dates, OHLCV, duplicates, symbol identity, survivorship,
corporate actions, source overlap and retention. Any failure rejects; any unknown requires
manual review; only all-pass evidence automatically promotes. Promotion is a single bounded
transaction from parsed quarantine rows to a new immutable source-specific market revision.
The artifact is never mutated. Existing EODHD revisions remain untouched.

Sources never merge implicitly. Feature construction must select one provider or explicit
exact revision set. WIKI and EODHD remain separate historical evidence. Attribution,
survivorship, price basis, unsupported corporate-action evidence and source-specific retention
continue through the manifest and derived lineage.

## Consequences

Maintenance may download/inspect candidates, but the public API and Web remain read-only and
expose no arbitrary URL control. Public-domain evidence may be retained and backed up
independently; EODHD deletion remains scoped to EODHD-derived artifacts. Unknown underlying
rights cannot be cured by a mirror or repository license. Finance remains `RESEARCH`; this ADR
creates no broker, order, PAPER, LIVE or AUTO capability.
