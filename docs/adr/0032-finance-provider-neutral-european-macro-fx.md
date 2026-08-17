# ADR 0032: Provider-neutral European macro and FX evidence

- Status: Accepted
- Date: 2026-08-17

## Decision

Finance extends the existing Macro Memory pipeline with bounded first-party adapters for Sveriges Riksbank SWEA REST v1 and the ECB Data Portal SDMX 2.1 API. Provider DTOs stop at the adapter/parser boundary. Canonical observations identify provider, source series, region, reference period, UTC knowledge/acquisition times, unit, frequency, evidence class and immutable artifact lineage. FX additionally records base and quote currency; `EUR/SEK` always means SEK per EUR.

The initial Riksbank pack is `SECBREPOEFF`, `SEKEURPMI` and `SEKUSDPMI`. The initial ECB pack is `EXR.D.USD.EUR.SP00.A`, `EXR.D.SEK.EUR.SP00.A` and `FM.D.U2.EUR.4F.KR.MRR_FR.LEV`. Third-party Riksbank catalogue series are deliberately excluded. Current-history API responses have no defensible historical availability/vintage timestamp and are therefore `REVISED_HISTORY_EXPLORATORY`; acquisition UTC is knowledge time. Exact historical point-in-time evidence requires an authoritative publication/history proof and never falls back to revised history.

Riksbank and ECB EUR/SEK remain separate revisions. Deterministic comparison requires matching base/quote and reference date, with absolute differences up to 0.0001 classified consistent, up to 0.02 expected methodology/rounding difference, and larger differences mismatch. Missing or non-comparable days are insufficient. No inverse series is fabricated.

## Rights and operations

Both packs are `LOCAL_RESEARCH` with attribution required. Riksbank permits free automated use/adaptation but requires attribution for unprocessed statistics and states that FX is indicative, not transactional. ECB permits free reuse of public ESCB statistics with source attribution and explicit marking of modifications; third-party data is excluded. Local retention and automation are allowed, redistribution must preserve the stated conditions, there is no identified deletion duty, and terms must be revalidated before changed use.

Acquisition is maintenance-only and bounded by date range, allowlist, HTTPS first-party hosts, timeout and artifact size. Every artifact enters quarantine before parsing and promotion. Provider failure does not affect other providers or API startup. Migration 93 is owned by the existing ordered Finance coordinator. No schedule, strategy change, PAPER, broker, order, LIVE/AUTO or self-learning authority is introduced.
