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

## Addendum — EODHD Free personal retention, 2026-11-08 (issue #61)

**Later owner-scoped decision supersedes only EODHD's former automatic
`SubscriptionOnly/DeleteAtSubscriptionEnd` policy.** The official
[Terms and Conditions](https://eodhd.com/financial-apis/terms-conditions)
and [storage/cancellation FAQ](https://eodhd.com/help/storage), directly
checked 2026-11-08, permit a non-professional user to store and retain
downloaded market data and private derived research after cancellation,
without a post-cancellation deletion deadline. An older web-cached Terms
version still displayed the former one-month requirement; preserve this
date-sensitive discrepancy in issue #61 rather than calling it nonexistent.

The current EODHD Free retention classification is therefore
`LongTerm/None`: no automatic deletion deadline. This is a
provider/product-specific retention amendment, **not** public-domain
classification, unlimited access, permission to redistribute, or new
provider/grant authorization. WIKI-only automatic backups remain WIKI-only;
EODHD copies remain `Restricted`, explicitly provider-tagged, and require
their own owner-approved copy/restore scope. Post-termination API acquisition
and entitlement-gated processing remain blocked; existing records need no
migration or retroactive rewriting. Manual deletion remains a separately
confirmed, irreversible owner action. All other providers and the separate
BB-132K disposition stay unchanged. Finance remains RESEARCH / 0 SEK / NONE.
