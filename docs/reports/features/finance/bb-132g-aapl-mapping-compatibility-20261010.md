# BB-132G — AAPL mapping compatibility review

Detta är en sanerad GitHub-version. Read-only architecture/code review, 2026-10-10.
No operational activation, implementation, test changes or production-store inspection.

## Metadata

- Accepted baseline: `a2373f172600c7b45ce378c56aab1d7da2b9b242`.
- Baseline tree: `fb41dea807053d7ef2c2219170f7a907e48ed146`.
- Branch: `review/finance-aapl-mapping-compatibility`, created from that exact fetched main.
- Publication subject: `review: document BB-132G AAPL mapping compatibility`.
  Resolve this commit and its tree directly from Git metadata; publication is not acceptance.
- Independently read operational review: `2df263a8cc11bd5837ea205244ffacefa184cec9`, tree
  `d85c7d6adb4ab773001c7afad20dad44ee4e77c6`, on
  `review/finance-live-observation-activation`. That branch is preserved, not merged here.
- Goal: assess ongoing AAPL mapping against immutable receipt, temporal, revision and shadow
  contracts, without configuring or collecting anything.

## Status

**REVIEW CHECKPOINT / ANALYSIS COMPLETE / NOT A MERGE CANDIDATE.**

**Verdict: VERSIONED MAPPING REQUIRED for the complete compatibility requirement.**
The existing runtime cannot express two effective mapping versions for one instrument, and
accepted shadow v1 requires identical mapping provenance across its selected daily history.
A blanket `ValidTo=null` configuration replacement is therefore **not** an end-to-end compatible
extension. This is an explicit capability boundary, not evidence that existing receipts are corrupt.
No production correction or automatic migration is proposed as an operational workaround.

A narrower conclusion also matters: configuration replacement alone does not mutate stored
receipts. New, previously unobserved source days can be acquired with different mapping metadata,
while old projections remain reproducible. This supports a possible separately reviewed
**collection-only, non-overlapping transition**, not unrestricted compatibility: old-day
reacquisition conflicts and mixed-history shadow v1 remains blocked. This report does not authorize
that tradeoff or infer that later observations have actually been collected.

## Evidence

### Authoritative context and dated operational evidence

Reviewed [market memory/provenance architecture](../../../architecture/finance/market-data-memory-and-provenance.md),
[controlled-acquisition runbook](../../../operations/runbooks/finance-controlled-acquisition.md),
accepted D/E/F/F1 reports linked in the catalog, and their actual implementation/tests below.
The historical operational evidence is available at the
[exact published handoff](https://github.com/T-bear/BigBrain/blob/2df263a8cc11bd5837ea205244ffacefa184cec9/docs/operations/codex-recovery.md).
It records the 2026-10-02 AAPL source-day receipt, New / AcquiredProviderDaily, acquisition at
2026-10-04T17:56:19.98109+00:00 and ingestion at 2026-10-04T17:56:20.0069441+00:00;
receipt/checksum, no predecessor, two reopen checks and before/at-ingestion visibility are there.
Those are inherited dated observations, not newly executed verification in BB-132G.

The same handoff records runtime activation on 2026-10-04, AAPL only, 360-minute cadence,
3-calendar-day lookback, legacy workers disabled, full startup wait and no activation-time request.
Both instrument and mapping were bounded to 2026-01-29 through 2026-10-02 by the shared DTO.
Later source days are rejected before network. Enabled is not proof of ongoing collection.
This review neither reads the live database nor asserts its present receipt count or runtime health.

### Contract distinctions and code anchors

All implementation references below refer to the accepted baseline, unchanged on this branch.

| Responsibility | Actual contract / source |
| --- | --- |
| Canonical identity | `CanonicalInstrument` in [CanonicalMarketData.cs](../../../../src/BigBrain.Modules/Finance/CanonicalMarketData.cs), lines 54–86: ID, type, currency, venue/MIC, lifecycle and validity. `US:XNAS:AAPL` is not a provider feed or ticker-only identity. Full record equality also includes display name and dates. |
| Effective symbol mapping | Same file, lines 89–123: provider, dataset, symbol, instrument ID, venue/MIC, inclusive `ValidFrom`/optional `ValidTo`, evidence token. `IsValidOn(d)` means `d >= ValidFrom && (ValidTo == null || d <= ValidTo)`. |
| Existing multi-period primitive | `InstrumentMappingCatalog`, lines 126–174, already resolves by source date and rejects overlapping intervals; a null end prevents a subsequent overlapping interval. It is an in-memory catalog, not a persisted knowledge-time registry. The F runtime does not use it for multi-version selection. |
| Deployment projection | [FinanceObservationRuntime.cs](../../../../src/BigBrain.Api/Finance/FinanceObservationRuntime.cs), lines 10–72: bounded `InstrumentsJson`; one DTO's dates construct **both** canonical instrument and mapping. Clearing `ValidTo` changes both records, not only provider metadata. |
| Runtime validation | Same file, `ValidateConfiguration` lines 132–162: 1–4 entries, unique canonical IDs and unique symbols, Equity/USD, allowed listing MIC, fixed Alpaca daily dataset. Two non-overlapping AAPL entries still fail uniqueness. |
| Source/feed | [AlpacaDailyMarketObservations.cs](../../../../src/BigBrain.Api/Finance/AlpacaDailyMarketObservations.cs): fixed IEX/1Day/raw, `asof=-`, exact symbol response. IEX is not XNAS. MIC/currency are validated against trusted configuration; the adapter is not a current listing/rename discovery service. |
| Time | [DailyMarketEvidence.cs](../../../../src/BigBrain.Modules/Finance/DailyMarketEvidence.cs): completed prior New York date, explicit source contract. Runtime/F1 separately check supported US sessions. Provider publication is unknown/null; [MarketObservationEvidence.cs](../../../../src/BigBrain.Modules/Finance/MarketObservationEvidence.cs) defines knowledge as trusted ingestion, never session date. |
| Evidence verification time | `MappingEvidence` is a bounded `EvidenceReference` token. Neither mapping nor canonical instrument has a mapping-observed/verified-at field or a review-expiry gate. `PolicyRecordedAtUtc` records owner data-use policy, **not** mapping verification. Receipt ingestion proves when Finance retained the supplied mapping assertion, not when an issuer was consulted. |

Null end is represented as `OPEN` in receipt checksums. It is computationally an unbounded
upper date predicate; code does not prove future permanence. A truthful operator meaning could
be “no known termination as of the recorded verification,” but the present contract lacks a
structured verification instant/currentness policy to distinguish that from stale indefinite
permission. Null is supported syntax (also used by existing fixtures), not self-validating evidence.
Do not silently redefine the old 2026-10-02 bound as a delisting fact: it was the reviewed scope,
not evidence that Apple ceased trading then. Likewise do not invent a new historical start date.

### Identity, duplicate, revision and replay analysis

[MarketObservationIntegrity](../../../../src/BigBrain.Modules/Finance/MarketObservationEvidence.cs),
lines 73–106, and [FinanceMarketObservations.cs](../../../../src/BigBrain.Api/Finance/FinanceMarketObservations.cs),
`AcquireObservationCoreAsync` / `ReadObservationReceipts`, establish:

1. Logical identity hashes receipt contract, canonical ID/MIC/currency, provider/dataset, origin,
   granularity/raw basis, interval and event timestamp. Mapping dates/evidence are **not** in it.
2. Content checksum hashes provider observation values, publication value/unknown, payload
   commitment and daily semantics. Mapping dates/evidence are **not** in it either.
3. Receipt ID hashes logical identity, content checksum and predecessor (or NONE).
   Thus a metadata-only change does **not** create a new independent receipt identity for the
   same content/predecessor. It must not be used to evade a conflict.
4. Full receipt checksum **does** bind instrument dates and mapping dates/evidence, plus original
   times and provenance. Rewriting those fields on an old receipt invalidates that commitment.
5. `ReobserveDailyAsync` reads the last receipt for the logical identity under the existing
   immediate transaction. Full instrument/mapping equality, adapter, policy and policy-evidence
   equality are required **before** duplicate/revision selection. A changed `ValidTo` (including
   clearing it), `ValidFrom`, evidence token or display/venue metadata rejects even if prices
   match; changed prices do not rescue it. The adapter request has already occurred before
   this persistence provenance check. A conflict stops the cycle, not just that slot.
6. With unchanged provenance, identical current content returns the original receipt and times;
   changed content extends the chain with a later trusted knowledge time. Reverting content is
   still a new revision. Mapping migration is not a provider-value revision.
7. A different source day has a different logical identity; absent existing receipts for that
   day, the new metadata can persist normally. This does not silently update the older record.
8. Reopen validates each receipt using **its embedded** instrument/mapping, checksum and lineage;
   it does not reinterpret old JSON with the current configuration. `MarketKnowledgeAt(E)`
   filters by persisted ingestion <= E and hashes ordered receipt checksums. The watermark
   prohibits new knowledge entering an earlier sealed boundary. Later records cannot change E.

Scope of the equality finding: the older internal `AcquireMarketObservationAsync` explicit-
predecessor path does not apply the runtime's `last.Mapping` equality guard to a new-content
revision; it still enforces duplicate provenance and exact predecessor lineage. That is not an
available runtime/configuration mapping-transition mechanism: runtime and F1 always call
`ReobserveDailyAsync`. Do not bypass their guard by routing operational work through the older
entry point. Shadow compatibility would remain unresolved in either case.

Consequently changing configuration neither changes the old 2026-10-02 receipt ID/checksum nor
retroactively repairs/relabels it. Its acquisition/ingestion times and lineage must remain exact.
But the current configuration can no longer reacquire that day as Duplicate/Revision if provenance
has changed. Waiting until the rolling lookback no longer contains that date avoids that *request*,
not the missing compatibility. Without a current store inspection, no other day may be assumed
unobserved; BB-132G deliberately performs none.

### Daily shadow compatibility is stricter than collection

[FinanceProspectiveDailyShadow.cs](../../../../src/BigBrain.Api/Finance/FinanceProspectiveDailyShadow.cs):

- `FreezeDailyShadow` lines 35–83 selects all known daily receipts for canonical ID/provider/dataset.
  It rejects `rows.Any(x => x.Mapping != mapping)` before feature construction. Two mapping epochs
  therefore block a new freeze even when source-day intervals do not overlap and identity is stable.
- Candidate identity excludes mapping version; candidate checksum binds the full mapping hash.
  An existing freeze cannot be refreshed under a new mapping (`Frozen mapping conflict`).
- `EvaluateDailyShadow` lines 86–132 checks every known row's origin/full mapping hash before
  selecting prospective outcomes. A new mapping known by E fails `Prospective source provenance
  mismatch`, even when it would not become the chosen outcome. No automatic continuity is inferred.
- A previously sealed E before the new receipt remains replayable because `DailyRows` filters
  ingestion <= E. New provenance may block a later E; it does not mutate the old result.

Do not bypass this by deleting the October receipt, changing canonical ID/feed, discarding
unfavorable history, refreshing a frozen candidate, or weakening equality. Safe refusal is an
accepted invariant, but it is not successful ongoing shadow compatibility.

### Options and decision

| Option | What works | What fails / required preconditions | Review finding |
| --- | --- | --- | --- |
| A: same configured entry, null end, current evidence | Existing syntax can collect previously unseen later days; original embedded receipts and earlier cutoffs survive. | Same-day provenance conflict after request; both instrument/mapping snapshots change; mixed shadow history rejects. Must retain supported start, establish current provider/issuer identity, define no-known-termination semantics and ongoing review, prove no overlap with retained days, explicitly accept loss of old-day operational reacquisition and mixed-history shadow use. | Not CONFIG-ONLY SAFE for the full requirement. A narrower collection-only exception needs a separate architect/owner decision; not enacted. |
| B: new effective mapping alongside immutable old | Domain catalog already supports non-overlapping source-date periods with stable canonical ID; receipt snapshots are immutable. | Runtime JSON uniqueness blocks two AAPL entries; dates conflate instrument/mapping; no verification-time registry; shadow v1 requires one mapping. Putting two entries in current config is not a solution. | Preferred direction: VERSIONED MAPPING REQUIRED, reusing existing owners/types, not a second symbol master. |
| C: minimum additive integration if full compatibility is required | Can connect bounded mapping versions to source-date selection, preserve exact old snapshots and explicit knowledge. | Needs separately approved contract and tests; must not simply remove provenance checks or reinterpret v1 receipts/candidates. Cross-version science compatibility is a separate explicit decision, not assumed from unchanged ticker. | Propose bounded follow-up only; no code in BB-132G. |

Minimum proposed follow-up contract (not accepted or implemented here):

- Keep canonical identity stable; distinguish instrument identity/lifecycle facts from reviewed
  provider-mapping coverage. Preserve the exact old instrument **and** mapping snapshots for
  reacquisition of old logical identities; selecting the old mapping with a newly changed
  instrument record would still conflict.
- Add a bounded, immutable mapping-assertion version/commitment with source evidence reference,
  trusted recording/verification time and explicit effective source-date range. Define null as
  no known termination at verification, not guaranteed future validity, and define when revalidation
  or known rename/delisting must block new collection. No fabricated backdated verification.
- Reuse date-resolution/overlap rules; resolve one authorized version per instrument/source day
  and acquisition knowledge context. Reject gaps/ambiguity before network. Preserve version
  evidence across reopen through existing Finance ownership; choose the minimal additive storage
  or durable manifest binding during follow-up design, not an unreviewed second database.
- Preserve all v1 receipt hashes/readers. Existing logical identities use the exact recorded
  provenance for duplicate/provider-value revisions; a material identity conflict stays refused.
  New source days use their authorized version and new ingestion time. Do not reopen grants.
- Leave shadow v1 fail-closed until a separately reviewed additive projection/compatibility
  contract can bind a verified ordered mapping lineage at freeze and cutoff. It must distinguish
  metadata continuity from real identity/corporate-action changes, retain all required evidence,
  and never mutate existing candidates/results. If that is outside the bounded follow-up,
  explicitly declare cross-version shadow unavailable rather than relaxing its checks.

This is a scoped recommendation, not a requirement to build a generic security master or a new
scientific engine. Existing single-mapping operation remains valid within its reviewed scope.

### Public identity evidence and limits

Public pages were accessed on **2026-10-10**, during this review (UTC review time recorded at
04:10:44Z). No authenticated Alpaca request, data endpoint or account query was made.

| Source | Observed claim and dating | What it does not establish |
| --- | --- | --- |
| [Apple investor FAQ](https://investor.apple.com/investor-relations/faq/default.aspx), “What exchange does Apple stock trade on?” | Issuer states AAPL trades on Nasdaq. The answer has no publication/effective timestamp shown; access time is this review's observation time. | No future permanence, complete symbol history, exact new historical ValidFrom, provider entitlement, or machine-enforced mapping expiry. Does not by itself prove the entire interval between two checks. |
| [Alpaca historical stock bars reference](https://docs.alpaca.markets/us/reference/stockbars) | Documents 1Day, raw, IEX, USD default and `asof=-` disabling symbol-name mapping; AAPL appears as an example. Page reports “Updated 5 months ago,” not an exact durable publication date. This is the public multi-symbol reference, not a live response from the accepted single-symbol adapter. | An example is not current account-level symbol validation. Symbol-only querying cannot certify continuity through renames/reuse or prove perpetual listing. No feed change or live entitlement inferred. |

Current issuer confirmation can support an explicitly recorded current identity assertion.
It cannot alone authorize indefinite unmonitored continuation or retroactively prove every source
day. Existing October operational evidence remains dated evidence; no new effective date is invented.
Corporate actions can change raw-return interpretation even without symbol change; accepted raw/IEX
observations are not corporate-action-neutral returns or whole-market evidence.

### Existing tests inspected; missing characterization specified

No test files were changed and no tests/builds were run. The following assertions were inspected,
not presented as newly passing execution:

- `FinanceMarketDataNormalizationTests.CanonicalIdentitySurvivesSymbolChangeAndBoundaryIsInclusive`
  and `OverlappingMappingsAreRejected`: domain date resolution and overlap protection.
- `FinanceObservationRuntimeTests.RealAdapterOwnerPathReopensDuplicatesRevisesAndPreservesSealedHistory`:
  stable open-ended fixture mapping, native receipt reuse/revision, restart and earlier-cutoff identity.
- `FinanceObservationRuntimeTests.MappingProvenanceCannotChangeThroughReacquisition`:
  changed evidence token plus changed response degrades with only the original receipt retained.
- `FinanceMarketObservationTests.SameValuesCannotSilentlyReplaceMappingProvenance`:
  changed mapping evidence with same values throws, original receipt remains.
- `ProspectiveDailyShadowTests.FreezeFutureObservationSealedResultReopensAndCannotBeContaminated`:
  freeze/reopen and later-revision noninterference under a single unchanged mapping.
- F1 `FinanceObservationMaintenanceTests`: shared bounded configuration/one-shot guards; no
  operational helper is executed by this review.

Exact missing assertions for a separately authorized model-free checkpoint:

1. Seed finite instrument/mapping receipt; independently change only mapping end, only instrument
   end, only evidence, and both ends via DTO. Assert logical/content/receipt ID inputs stay equal
   for same payload/predecessor, full checksum differs, reobserve rejects, original JSON/times and
   cutoff projection remain byte-identical. Assert request count is one, no retry/no second receipt.
2. Same mutations with changed content still reject revisions; unchanged old snapshots still
   permit Duplicate and a proper later Revision after reopen. A new instrument snapshot paired
   with the old mapping is not sufficient.
3. Persist a *different* source day using new provenance: receipt succeeds with new knowledge;
   earlier E remains byte-identical; old-day reobserve still rejects under new provenance.
4. Configure two non-overlapping AAPL entries in today's JSON: assert Misconfigured and zero
   transport calls. Separately prove existing catalog resolves those periods but rejects overlaps.
5. Seed enough accepted daily/research fixtures to freeze with old mapping; add new-day/new-mapping
   evidence. Assert replay at old E succeeds, later E rejects provenance, and a new research-run
   freeze rejects mixed mappings. No candidate refresh or scientific grant renewal.
6. For a future versioned solution: prove bounded before-network selection, immutable version
   verification time, no-known-termination/currentness policy, rename/reuse conflict rejection,
   concurrent/restart determinism, exact prior receipt/projection retention and explicit shadow
   compatibility/refusal. Add migration tests only if additive persistence is actually introduced.

Static findings above follow the real branch predicates and canonical hash inputs. These proposed
cross-boundary tests have **not** been implemented or executed; existing tests do not prove a live
mapping extension is safe.

## Changes

Documentation only: this focused report, its catalog entry, and a brief recovery pointer distinguishing
this review from accepted implementation and the separate operational evidence branch.
No ADR acceptance or production architectural contract was changed.

Potential follow-up surfaces (not changed): `CanonicalMarketData.cs`, `FinanceObservationRuntime.cs`,
`FinanceObservationMaintenanceCommand.cs`, `FinanceMarketObservations.cs`, `MarketObservationEvidence.cs`,
and, only under explicit scientific compatibility approval, `FinanceProspectiveDailyShadow.cs`.
Relevant tests are named above. Reuse `InstrumentMappingCatalog` and the Finance SQLite owner;
no arbitrary provider/plugin/scheduler path is justified. External configuration would change only
in a later explicitly authorized operation after review, never in this checkpoint.

## Security

No source/test implementation, database/configuration/credential access or changes, service restart,
deployment, provider-data request, manual one-shot, cycle trigger or mapping extension. Public
issuer/provider documentation reads are not authenticated market-data calls. No raw payload/OHLCV,
credentials or private paths are published. Unrelated local/untracked work is preserved/excluded.
No Qwen, research session, grant reset/refund/renewal, automatic SHADOW, PAPER/LIVE/AUTO,
broker/Avanza, orders, positions or capital. Finance **RESEARCH / 0 SEK / NONE**.

## Remaining work

Architect/owner decision on the versioned compatibility boundary versus a knowingly restricted
collection-only option. Actual mapping adoption/currentness evidence and any operational change
remain unapproved. The existing operating state is intentionally untouched, not newly certified.
No claim of later receipts, future listing facts or scientific edge is made.

Publication verification on 2026-10-10: `node scripts/verify-documentation.mjs` PASS
(266 Markdown files / 91 unique backlog IDs); `git diff --check` and staged diff/scope PASS;
Gitleaks 8.28.0 `git --pre-commit --staged --redact --no-banner` PASS, no leaks.
Exactly three Markdown files are included: this report, `docs/reports/REPORT-CATALOG.md` and
`docs/operations/codex-recovery.md`. Source/test/configuration delta is empty. Baseline verified
after fetch; unrelated mockups and local ADR files remain excluded. No full code test suite is
justified for this documentation-only review. Production assertions require the future tests
above before use.

## Resumption

Read this report against the exact baseline, then the separately published dated operational handoff.
Resolve publication SHA/tree via the unique subject above. **STOP after push for independent
ChatGPT/architect review.** Do not merge, extend the mapping, activate anything or implement the
recommendation without a new bounded decision. The owner need only say “Codex är klar”.
