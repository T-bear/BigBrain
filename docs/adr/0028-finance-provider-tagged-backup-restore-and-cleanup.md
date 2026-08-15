# ADR 0028: Finance provider-tagged backup, restore and quarantine cleanup

- Status: Accepted through product-owner authorization of BB-085
- Date: 2026-08-15
- Related: ADR 0021, 0023–0027

## Context

Finance market memory shares physical storage but not rights. WIKI is verified public domain;
EODHD is subscription-only with a deletion obligation; quarantined candidates are untrusted;
derived evidence inherits exact source lineage. A whole-volume backup would erase these legal
and lifecycle distinctions.

## Decision

Every Finance backup selection is classified by provider, product, policy, rights,
provenance, retention/deletion duty, canonical/quarantine state and derivation lineage.
Unknown classification fails closed. Backups use deterministic open JSON plus SHA-256 and an
immutable manifest. Staging must complete write, hash and verification before status COMPLETE;
only COMPLETE is restorable. Restore verifies in isolated staging and never blindly replaces
healthy canonical memory. Providers are never implicitly merged.

Quarantine cleanup is a separate lifecycle. It may delete an aged rejected raw payload while
retaining manifest, checksum, provenance, validation and rejection evidence. Manual review is
conservative. Cleanup cannot address canonical market, feature, backtest or robustness tables.

## Consequences

WIKI may use ordinary local indefinite backup. EODHD remains visible as restricted and is not
included in that class; any permitted EODHD copy must preserve its expiry/deletion inventory.
Derived rows are included only when every exact source revision is included. Operations are
maintenance-only; API/Web expose sanitized read-only inventory. Finance remains RESEARCH and
this decision creates no broker, order, PAPER, LIVE or AUTO capability.
