# BB-087 prospective shadow observation foundation

## Metadata

- Date: 2026-08-15
- Baseline: `31156ec8e9780dc6622891cb4ba46050c302a6f6`
- Mode/budget: `RESEARCH`, 0 SEK; no broker, order, PAPER, LIVE or AUTO capability

## Status

Implemented, local regression passed, deployed and bounded-runtime/restart verified. Implementation commit `86e92c431cffc1db37684bcfba5a2a785d7b05ad` passed all four jobs in GitHub Actions run #44 on 2026-08-15.

## Changes

Finance now has a durable SQLite prospective journal for approved EODHD Free `CURRENT EOD / PROSPECTIVE EOD` research. A source-state worker starts after BB-083 recovery, requires clock integrity, scans only canonical EODHD evidence and evaluates the unchanged `buy-and-hold/v1`, `sma-crossover/v1` and `momentum/v1` strategies. It creates no order and has no execution authority.

Prediction identity pins instrument, session, market revision, feature revision, strategy/version, parameter fingerprint, knowledge cutoff and `next-eligible-source-session-close-v1`. Repeated cycles use an idempotent unique insert. Features are selected only when their knowledge time is at or before cutoff. Observations older than the bounded current-session window, or sessions whose next source observation is already knowable, are not backfilled as prospective.

Outcomes are separate one-per-prediction rows. The original ID, cutoff, signal and lineage remain unchanged; only evaluation state advances. EODHD deletion scope and stale-preview fingerprint include the journal, so source-dependent evidence cannot outlive the existing provider policy. It is excluded from WIKI's indefinite public-domain backup class.

Read-only endpoints cover predictions, detail, scorecard and status with bounded filters. The Finance UI separates Shadow research from historical backtests, shows pending versus evaluated, and labels samples below 20 as `BOOTSTRAPPING`.

## Evidence

- Network-free temporal simulation: T0 prediction then T1 outcome.
- Exactly-once retry, clock failure, late-start anti-backfill, malformed ID/filter and immutable prediction assertions.
- Read-only API and UI assertions include no mutation or order controls.
- Runtime: eight EODHD instruments for 2026-08-14, 24 valid pending predictions (three strategy versions), zero evaluated outcomes and exact feature revision `feature-c204a8133abaf8a2` over eight source revisions.
- The first drill exposed 24 rows referencing a WIKI feature revision. They were not deleted or rewritten as successes: the integrity pass classified them `INVALIDATED`, retained the reason, and exact source-revision membership became a hard feature-selection gate.
- Restart retained 48 total audit rows (24 valid pending + 24 invalidated) and created no duplicate valid prediction. Current health, WIKI memory and backup inventory remained available.
- GitHub Actions run #44: documentation, frontend, secrets and backend all `success`.

No claim is made that a future EOD outcome exists. A real prediction is created only if the deployed current observation and feature revision pass all causal gates; otherwise the journal remains safely armed.

## Security

Endpoints are read-only, validate bounded filters and malformed opaque IDs, return no secrets or filesystem paths and expose no mutation/order capability. The clock, entitlement, causality and late-start gates fail closed. No provider request, paid service or external database was added.

## Remaining work

The worker currently performs a lightweight source-state scan after startup; it does not add high-frequency polling. The recommended next Finance slice is prospective outcome-evaluator maturation plus explicit daily current-EOD cadence/recovery evidence, while M5 Hard Risk Engine and the security baseline remain prerequisites before any execution eligibility discussion.

## Resumption

Start with ADR 0029, `FinanceShadowResearch.cs`, the shadow API scorecard and the EODHD lifecycle worker. Verify current-source age and clock integrity before treating any new row as prospective.

## Sanitization

Detta är en sanerad GitHub-version. It contains no provider credential, raw market payload, private runtime identity, sensitive path or raw log.


## Release 1 checkpoint — 2026-11-11

**Implementation checkpoint; RELEASE / ACTIVATION BLOCKED.** The owner's new risk-3
assignment authorizes bounded implementation, review, release and model-free EODHD resumption
only after applicable gates. It supersedes planning-only authority for this scope, not the
reserved BB-132K disposition, provider rights, independent review or deployment boundaries.
Baseline: `9888a2942c52e58469b55dc73355296f6c272378`, tree
`2b6ea7fe9c05482ec743ecc2a77b846d9b00d80d`. Branch:
`checkpoint/finance-release1-20261111`. Exact candidate/tree and CI belong to the PR's Git metadata.
No Qwen, Alpaca activation, new provider, PAPER/LIVE/AUTO, capital or historical rewrite.

### Current runtime and integrity evidence

Read-only inspection on 2026-11-11 around 05:25–05:32 UTC; no provider request,
production POST, database constructor, source copy, restore, configuration edit or restart:

| Check | Observation and limit |
| --- | --- |
| API | Healthy; image `sha256:9310e3cc8ea3484d94bcfeed78b005ba1aecb5724e4ca2cfc28fe9deb44ab047`; image label and compiled `system-build-identity` both `e8f3e62cc446cf52a89045d1a3ce7d4c3a2116ff`. |
| API composition | `org.bigbrain.source.composite=sha256:7e67217c811035086102ae1f9998806251dd79a34b682bb7eda06f3913866854`. The accepted Media builder deliberately retains the two older Finance policy blobs from `80fa231773dace31dda60ad0a32c69c1a42b5e50`; compiled SHA alone does not prove all source files match that Git tree. Private composition receipt has not been revalidated for a Finance release. |
| Web | Healthy; revision label `3112868d73c7ef986b4fba5c9a09eb0deb9cd0f9`, image `sha256:78f6a430905088dc7290e414e3082f10c12ad89d4e28c0426c10e4e3aaa2011a`. |
| EODHD | Disabled; configured AccountActive=true is not independent current account proof. Latest canonical session 2026-10-02; latest successful acquisition 2026-10-02T22:17:43.962029Z. |
| Journal | GET status pending=882 versus cadence pending=24/evaluated=858/invalidated=24; last prediction/outcome 2026-10-02T22:48:08.6405254Z. Healthy/clock-integrity true, authority NONE. No new prospective session. |
| Retention | Runtime GET backups still reports EODHD SubscriptionOnly/DeleteAtSubscriptionEnd, Restricted. Accepted main says LongTerm/None. This remains an actual composition/deployment discrepancy. |
| Other acquisition | Existing ObservationRuntime flag is true. It is a separate Alpaca path, not EODHD cadence. No activation, disabling or grant renewal was performed. Any API restart must review all hosted workers and their effective flags. |

The [November 9 metadata audit](https://github.com/T-bear/BigBrain/issues/57#issuecomment-6733301277)
remains dated evidence: 906 rows, 35 source dates, 858 evaluated rows over 34 dates,
816 distinct evaluated session/instrument/strategy/parameter keys, 42 additional revisions.
This checkpoint rechecks live aggregate counters, not all original rows. It does not call
858 rows independently verified scientific outcomes. Existing GET exposes no individual
outcome close/time/checksum audit; direct Finance volume read access was unavailable.
No new container, copy or privilege workaround was used to obtain that audit.

### Bounded implementation

- `FinanceShadowResearch`: one effective-state expression and grouped counts. Invalidated
  lineage takes precedence over an existing immutable outcome (matching the existing overview's
  exclusion rule); otherwise an outcome means Evaluated, otherwise original state. Catalog,
  filters, status, cadence and overview agree. Catalog adds `invalidated` without removing fields.
  Insufficient/Missed are no longer called Pending by the overview. Revisions stay separate audit
  rows; duplicate outcomes do not inflate counts. This changes reads only, with no migration or
  prediction/outcome rewrite. Historical outcome bytes stay intact even when excluded.
- `FinanceEndpoints`: the sole Finance HTTP research POST now always returns 403 Problem Details,
  code `finance.research.httpExecutionDisabled`, before parsing input or resolving storage.
  This deliberately removes caller-triggered autonomous execution from Release 1; it introduces
  no password system, loopback trust, proxy-header trust or enable flag. GET history, model-free
  cadence and other modules retain their interfaces. Restoring HTTP execution requires a separately
  reviewed authenticated authority boundary. No Web caller of this POST was found.
- `FinanceProspectiveCadence`: reuse `us-equities-ny-v1`, require an exchange session and completed
  close as well as the configured UTC window. Default remains 22:00 UTC, conservatively after early
  closes. Holidays, weekend and pre-close attempts are rejected. Current entitlement now gates
  feature/shadow processing too, not just provider transport.
- A failed acquisition now consumes that symbol's daily attempt. Cadence disables adapter retries:
  eight eligible symbols imply at most eight requests in the single deployed cadence worker per
  UTC day, including failures/restart. The former combination of up to three transport attempts
  and repeating failed symbols could exceed the Free quota. Other uses of the generic adapter
  retain their explicit retry options; no account-wide quota claim is made for external users.

### Security boundary and remaining stop

The running API is published on all IPv4/IPv6 interfaces at port 18080. Unauthenticated health GET
succeeded against both the host LAN address and its Tailscale address **from this same host**.
This is not a remote-client or Internet exposure test. Tailscale is running; nftables/iptables
inspection was denied without privileges. No general application authentication/authorization
was found on Finance routes, and Web nginx proxies `/api/` without its own authentication.
The existing research POST directly calls the persistent evaluator; no live POST was used to
prove the source-visible defect. Regression tests exercise the denial on isolated test storage.

The candidate closes the identified HTTP write path after deployment, but does not certify
private Finance read access, remote ingress ACLs or the wider BigBrain authorization baseline.
**Activation remains blocked** until legitimate access and the effective network/ingress boundary
are independently verified. Do not bind only the API to loopback and assume Web's proxy is secured,
or change shared ingress/firewall rules without an exact reviewed access-preservation contract.

### DSR/PBO assessment — no research-policy change

[ADR 0033](../../../adr/0033-finance-bounded-autonomous-research-integrity.md) explicitly defines
`research-integrity-v2` Pass as the bounded Challenger screen with DSR/PBO NOT_EVALUABLE.
`FinanceResearchContracts.Assess` matches that accepted contract. Downstream
`RunAutonomousResearch` creates only Challenger; snapshot reports NEEDS_MORE_EVIDENCE and NONE
execution authority. No downstream DSR/PBO-success inference was found in that path.
Tests cover a positive complete screen, missing selection, previously evaluated holdout,
negative OOS, incomplete lineage/costs and excessive complexity; DSR/PBO remain NotEvaluable.
Changing aggregate Pass to NotEvaluable would change accepted policy and is not silently done.
No historical verdict or serialized contract is rewritten, and no statistical guarantee is added.

### Historical AAPL audit

The existing `finance-historical-inspect` command was inspected before execution: it opens SQLite
in ReadOnly mode, uses a deferred transaction, validates rights/promotion/raw capability, all
748 rows, exact session sequence, identity, OHLCV and acquisition chronology, then emits commitments.
No research, feature build, enrollment or model execution is invoked.

Actual PASS: NASDAQ-WIKI / WIKI/PRICES, candidate `wiki-eod-mirror-kmfranz-v1`, revision
`wiki-5713d7dccfa38f56`, 748 AAPL sessions from 2014-01-02 to 2016-12-19; development boundary
first 500 through 2015-12-24. Artifact
`sha256:dd5127aae478d270150904fcbad6e96a42e461e13c3d48a1587edb9b89cea43e`, current source commitment
`sha256:ad5db63d5996463ea4ce23a9c28fe53e14aadcd137616dce6267f9bf997c405e`.
Required limitations remain historical.notProspective, raw.notTotalReturn,
corporateActions.unresolved, survivorship.unknown and historical.priorExposureNotProven.

Apple's [official split history](https://investor.apple.com/faq/) confirms the seven-for-one
split on 2014-06-09, inside both the pinned interval and development subset. A raw close ratio
across that boundary cannot by itself establish investment return; unchanged share quantities
would generate a spurious split loss. This command does **not** inspect adjacent split prices,
validate adjustment factors or prove the historical simulator applied corporate actions.
Consequently split-corrected performance remains NOT VERIFIED. The old WIKI adjusted-close
capability remains invalid under closed/not-planned #56; no repair, new dataset or retrospective
backtest was attempted. This historical limitation does not supply or count as new EODHD evidence.

### Rights, backup and release gates

Public [Terms](https://eodhd.com/financial-apis/terms-conditions),
[storage FAQ](https://eodhd.com/help/storage) and [Free plan](https://eodhd.com/pricing)
were checked again 2026-11-11. Personal retained data/derived evidence has no automatic
cancellation deletion deadline; Free advertises one year, 20 calls/day and 20 requests/minute.
This confirms the public basis for #61, not current private account status or permission to share.
Current account confirmation is requested; credentials and account APIs were not used.

BB-132K's separate review deadline, 2026-11-10 14:36:45 Europe/Stockholm, has passed.
No later disposition decision was found in main or the requested issues/comments; bounded local
inventory discovery established the private helper area exists but supplied no disposition proof.
The specific owner question remains pending. Neither automatic expiry of data rights nor an
extension/deletion/copy/restore is inferred. This blocks affected archive/recovery actions.

The [backup register](../../../operations/backup-restore/README.md) still lacks verified daily
important-database backup and the required current restore coverage. Reuse the two inventoried
August WIKI backups and dated BB-085 isolated restore only within their original source scope.
They are not current whole-Finance recovery evidence. BB-132K's historical protected drill is
not a fresh restore authorization or proof of daily backup. No copy, restore or retention change.

[STANDARD](../../../policy/lanes.md) expressly excludes Finance and rejects an unknown composite
baseline. Its Media exceptions are exact-release-specific. Thus no valid existing executable
Finance release path was established for this runtime. No allowlist widening or ad hoc Compose
replacement is performed. A reviewed release must first bind current composition/Compose,
all hosted-worker flags, Finance and other affected-store recovery evidence, immutable candidate
image and the exact preserved rollback image above. Rollback must replace application images
only under that verified contract; never restore databases/journal/secrets or reset consumed
research authority. Preserve Web/other modules and data. **Rollback readiness: PENDING.**

### Issue 57 manifest readiness — NOT LOCKED / NOT STARTED

The owner-selected [120-session design](https://github.com/T-bear/BigBrain/issues/57#issuecomment-6733301277)
remains the preregistered design. This checkpoint cannot honestly produce an approved final
operational manifest while current rights, access, release/recovery and outcome audit are blocked.
No manifest hash/approval/start date is fabricated. Preparation pins the existing code facts:

| Field | Prepared identity; final gate |
| --- | --- |
| Provider | EODHD Free; policy `eodhd-free-personal-v2026-08-11`, accepted policy revision `2026-11-08`; current account and deployed policy pending. |
| Existing universe | SPY.US/US:ARCX:SPY, QQQ.US/US:XNAS:QQQ, IWM.US/US:ARCX:IWM, AAPL.US/US:XNAS:AAPL, MSFT.US/US:XNAS:MSFT, JPM.US/US:XNYS:JPM, XOM.US/US:XNYS:XOM, JNJ.US/US:XNYS:JNJ. Code mapping is not fresh source identity verification. |
| Strategies | buy-and-hold/v1 `{}`, momentum/v1 `{period:20}`, sma-crossover/v1 `{fastPeriod:10,slowPeriod:20}`. No retuning or model. |
| Features | Existing `CoreDailyFeatureSet`, `daily-feature-engine-v1`; raw-close momentum20 and SMA10/20. Freeze definitions/fingerprint and exact code blobs; each future derived revision must separately pin actual source revisions and knowledge cutoff. |
| Horizon | `next-eligible-source-session-close-v1`; original prediction before the future outcome, immutable versioned linkage. Full stored-outcome audit pending. |
| Cohort | First not-yet-observed eligible session after all gates and manifest approval; at least 120 distinct future US exchange sessions, each counted once. All 906 old records excluded from the primary cohort. |
| Accounting | Predeclare ordered sessions; retain missing/invalid/insufficient/interrupted days and revisions; earliest eligible prediction per frozen key, never best outcome. No backfill or restart of the window to remove losses. |
| Analysis | Same-session passive/always-long comparison, directional fractions and coverage by strategy/instrument/revision; uncertainty at session level. Exact estimator/cost ladder and calendar/date sequence still need final preregistration and review. Missing probability, regime, fill or portfolio evidence remains NOT_EVALUABLE. |
| Acceptance | 120 sessions permits adequacy assessment, not profitability or promotion. Source/strategy changes require a separate preregistered cohort; old journal and negative results stay intact. |

Before freeze, inspect the actual outcome rows and current source mapping through a reviewed
read-only path. In particular, the existing legacy evaluator selects the next source-date row
without an explicit predicate that its acquisition/market close is later than the prediction;
metadata counts alone cannot certify that case or the new cohort. Its existing lineage invalidation
also updates stored prediction state/reasons. No invocation against production was made, and no
new scientific readiness is claimed. These require focused integrity verification/correction
before any resumed cycle, independent of the safe read-model fixes here.

### Verification and next action

Focused test/build results and independent exact-candidate review are recorded below and in the PR.
The initial HTTP regression incorrectly compared changing response generation timestamps; the
assertion was corrected to compare persisted run count/content. No production defect was hidden.
No local check or CI replaces deployment, remote-ingress proof, owner account/disposition facts,
full outcome audit or actual future sessions. Issues #54/#55/#57/#59/#60 remain OPEN;
#56/#58 remain closed NOT_PLANNED and #61 closed completed. None is closed by this checkpoint.

Local verification 2026-11-11: **97 PASS / 0 FAIL / 0 SKIP** for
`FinanceShadowResearchTests`, `FinanceAutonomousResearchTests`, `FinanceEodhdIntegrationTests`
and classes matching `ApiTests`, Release configuration. This includes no-write HTTP rejection,
effective-state reconciliation with invalidated outcomes/duplicate retries/revised observations,
unchanged stored evidence, holiday/close handling and durable failed-attempt suppression.
Documentation validation: **PASS**, 292 Markdown files / 53 unique backlog IDs.
Independent candidate review, complete CI and release remain separate; see exact PR checks.

Before publication, main advanced to `e63aaa4dc30718f2620f2880a6f2b847e6091d89` through
PR #80 (three Audiobookshelf wrapper files only). The checkpoint fast-forwarded to that accepted
baseline; no Finance/API/build input changed, so the 97-test evidence is reused. Solution Release
build: **PASS**, zero warnings/errors. Targeted .NET formatting: **PASS**. Documentation initially
rejected renamed STATUS metadata keys; required keys were restored and validation passed.
