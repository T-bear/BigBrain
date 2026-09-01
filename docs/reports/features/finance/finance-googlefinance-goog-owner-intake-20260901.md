# GOOGLEFINANCE GOOG First Owner Intake

Detta är en sanerad GitHub-version. Den innehåller inga hemligheter, privata adresser, råa marknadsrader eller känsliga runtimeuppgifter.

## Metadata

- Date: 2026-09-01
- Baseline: `72d895dbe67e17fa6d9ac93116fc83acd4c54bd5`
- Source claim: Google Finance / GOOGLEFINANCE, manual Google Sheets export
- Requested identity: GOOG / NASDAQ, daily OHLCV
- Finance boundary: `RESEARCH / 0 SEK / NONE`

## Status

**QUARANTINED / INSPECTED / REJECTED FAIL-CLOSED / ZERO CANONICAL ROWS / CI VERIFIED.** The original ZIP remains
unchanged in the owner drop. Codex created only its required external empty ready marker. No market
data was downloaded, no external provider was contacted and no raw payload was committed.
Implementation `057c2f07bd10f2ff21d35d981028b262e0d07cd8` is published.

## Evidence

### Artifact and package

- ZIP SHA-256: `6caa7159833d0dd9ce03ded3fd27f7bbe5a18123987fdeb2cffa4f9efd4025d2`
- Size: 53,488 bytes; four safe top-level entries; one 154,432-byte CSV.
- Candidate: `owner-drop-4891dca5274b57ca303e8d0d`.
- Embedded matching sidecar SHA-256: `278ca4a6342ceac42f7b0615a557bd29afc74c917fa7cacb4ad182b72a335855`.
- Original checksum was identical before and after intake.

### BigBrain inspection

| Evidence | Result |
|---|---|
| Schema | `Ticker, Date, Open, High, Low, Close, Volume`; UTF-8/comma |
| Accepted rows | 3,126 |
| Coverage | 2014-03-27–2026-08-31 |
| Symbols | GOOG only |
| Duplicate/conflicting keys | 0 / 0 |
| Missing values / invalid dates | 0 / 0 |
| Non-positive prices / inconsistent OHLC | 0 / 0 |
| Invalid / zero volume | 0 / 0 |
| Out-of-order rows | 0 |
| Missing expected US sessions | 1 |
| Suspicious ≥20% close discontinuities | 0 |
| Split-like close jumps | 0 |
| Cross-source | `InsufficientOverlap`; no stitching |
| Technical quality | `LIMITED` because one calendar session is absent |

The discontinuity counters are deterministic screening heuristics, not corporate-action evidence.
No split/dividend columns or separate corporate-action artifact exist. Absence of a detected jump
does not independently prove raw semantics or absence of corporate actions.

### Rights, basis and identity

- Owner decision: `ApprovedByOwner` (policy meaning `APPROVED_BY_OWNER`), evidence `OWNER_APPROVED_BY_OWNER_2026-09-01`.
- External rights verification: `Unknown`; declared license text remains an owner claim.
- Owner price-basis declaration: `RAW`; canonical classification remains `Unclear`.
- Expected venue: NASDAQ owner claim. GOOG has no approved historical canonical instrument/provider
  mapping in the current eight-instrument Finance universe.
- Historical identity: unmapped (0 safely mapped, 1 unmapped). Ticker text is insufficient.
- Survivorship: `SurvivorshipUnknown`.

## Changes

Existing BB-084/126 intake is reused. The smallest compatibility change safely reads one matching
embedded JSON sidecar from a one-CSV ZIP and persists structured owner decision, external-rights
state, owner price-basis claim and bounded quality counters in existing validation/manifest JSON.
No schema migration, adapter, alternate importer or promotion path was added.

## Security

Archive traversal, size/file-count and sidecar bounds remain fail-closed. Embedded ready markers
never trigger processing. The drop mount is read-only to the API; content is never executed. No
secret, provider account, raw payload, private path or market row is published.

## Verification

- Focused intake tests: 17/17 passed.
- Full API tests: 587/587 passed.
- Release build: passed, zero warnings/errors.
- API-only image `sha256:acc24f6a39582969c5228076c3c0a5362b074c8459a540bef5424e8fabcff5cd`
  deployed healthy.
- Runtime: three dataset candidates total; GOOGLEFINANCE canonical rows zero; Finance
  `RESEARCH / 0 SEK / NONE`.
- Staged gitleaks: no findings. GitHub Actions run `33528970337` passed all four jobs.

## Remaining work

Before any separately authorized promotion review, provide authoritative effective-dated GOOG
identity/venue evidence and independently resolve external storage/reuse, raw-price and corporate-
action semantics. Owner approval alone does not satisfy these gates.

## Resumption

No automatic continuation is authorized. The exact next owner decision is whether to commission
the evidence work above; do not promote the current rejected candidate as-is.
