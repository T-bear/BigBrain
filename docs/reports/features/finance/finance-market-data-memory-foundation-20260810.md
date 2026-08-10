# Finance market-data memory, provenance and learning foundation

## Metadata

- Date: 2026-08-10
- Scope: provider-neutral retention, provenance, evidence, learning and storage design
- Related commit: assigned on publication

## Status

Architecture and backlog boundaries are documented. No source implementation, provider
integration, market-data retrieval, persistence, account, credential, runtime change or
deployment was performed. Finance remains RESEARCH; BB-071 remains open and blocks real
provider-data persistence/activation.

## Evidence

Repository architecture, M1 domain/evidence contracts, accepted ADR 0021, proposed ADR
0020, BB-045/049/056/069/070/071, current module patterns and existing modullocal SQLite
persistence were reviewed. The resulting model requires:

- collect once/reuse only under an explicit current entitlement;
- free-first/cost-aware provider selection without weakening license, quality or risk;
- immutable canonical dataset revisions, raw/corporate-action separation and provenance;
- explicit per-use `Allowed`/`Denied`/`Unknown`, with unknown fail-closed;
- raw/derived lineage without assuming derived-data freedom;
- an append-oriented market→decision→outcome graph including rejects and no-trades;
- governed evidence promotion rather than self-modifying live strategies.

No new BB-ID is justified: BB-045 owns memory/provenance implementation, BB-070 owns the
journal, and BB-049/056/069 own validation and lifecycle learning governance. No new ADR
is justified because ADR 0021 owns canonical data/retention enforcement and ADR 0020 owns
evidence/promotion governance.

## Changes

The new canonical design is
`docs/architecture/finance/market-data-memory-and-provenance.md`. Current state, roadmap,
module, strategy, journal, testing, threat model, provider selection and relevant backlog
contracts now reference and enforce it.

Storage direction is deliberately provisional: use Finance-owned self-hosted components,
measure the bounded EOD workload, and prefer the repository's modullocal SQLite pattern
plus immutable files if sufficient. No database/container is added. A different engine
requires measured pressure and architecture review.

## Security

Detta är en sanerad GitHub-version. It contains no provider data, secret, credential,
account identity, private terms correspondence, order capability or runtime identifier.
Entitlement cannot be inferred from access, and backups/derived copies cannot bypass
deletion obligations.

## Remaining work

- Product owner/provider completion of BB-071 remains the external gate.
- BB-045 must implement and test the provider-neutral policy/provenance slice before any
  adapter or real-data persistence.
- Physical storage choice remains deferred until volume, replay, concurrency, backup and
  licensed-deletion requirements are measured.
- ADR 0020 remains Proposed; this task does not accept it or change strategy authority.

## Resumption

Begin with synthetic-only BB-045 value types for entitlement policy/version, allowed use,
retention/deletion, provenance and immutable dataset revision plus a fail-closed evaluator.
Do not add network clients, external payloads or persistence. In parallel, send the BB-071
inquiry; only owner-reviewed entitlement evidence may unblock provider activation.
