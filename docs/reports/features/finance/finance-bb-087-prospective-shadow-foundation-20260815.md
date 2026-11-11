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


Review continuation 2026-11-11: PR [#81](https://github.com/T-bear/BigBrain/pull/81),
initial candidate `aa375d1894fa43b730a4195dba43481636b07215`, tree
`22c0cdaf357bd0a85df540e025d370b499e89217`, passed all four CI jobs in
[run 42967208569](https://github.com/T-bear/BigBrain/actions/runs/42967208569).
A separate reviewer verified identities and inspected HTTP/state handling, then reported a P2:
the new quota regression used a fixed start date while failure completion uses the real clock.
The test now uses the actual persisted completion day, including midnight rollover. This changes
only the isolated test, not production timestamps. That review stopped on a tool usage limit;
it is **INCOMPLETE, not an independent level-3 approval**. Final-candidate review/CI remain required.


## Bounded causal correction — 2026-11-11

**Current checkpoint scope: implementation, isolated tests, strictly read-only evidence and
release preparation only. NO MERGE / DEPLOYMENT / ACTIVATION.** This later owner assignment
separately authorizes correction of the reproduced #57 defects. Reverified main remains
`e63aaa4dc30718f2620f2880a6f2b847e6091d89`; PR #81 began at
`1690325f2735457b7f8e72ae75fd7e427e2b551d`, tree `ab9c719d083c86fae8994bbf3c4233233486c45b`.
It is an implementation review checkpoint, not a characterization-only blocker branch.
It already descends from that main; continue it under AGENTS §8 and checkpoint-workflow §9,
without a parallel branch or history rewrite. Exact final SHA/tree, CI and review are in PR #81.
PR #82 remains a separate draft at `ab76e7f2431804e0e8e7800429ed213eb7ec3081`, untouched.
Both starting PR revisions had four successful CI jobs, rechecked for this assignment.

PR #82 records the owner's **conditional BB-132K retention choice A**, with internal review
by 2026-11-18 14:36 Europe/Stockholm. This supersedes this report's earlier statement that no
later owner choice had been found. It is not source-by-source rights PASS, archive-integrity
PASS, new copy/restore authority or retroactive extension of the November 10 deadline.
The owner's statement is preserved by reference to [PR #82](https://github.com/T-bear/BigBrain/pull/82);
its documentation is not imported, modified or merged here.

### Corrected admission and preserved evidence

[Exact synthetic counterexample](https://github.com/T-bear/BigBrain/issues/57#issuecomment-6796335408)
reproduces the stored-prediction defect independently of the generation guard. New
`shadow-outcome-causality-v2` admission in `FinanceShadowOutcomes.cs`:

- Validates EODHD/Free/policy, instrument/symbol/provider-symbol/MIC, RESEARCH and the existing
  horizon. Source session must be an exchange session. Source acquisition must follow its
  conservative calendar close and equal the pinned prediction knowledge time; knowledge cutoff
  must lie between source knowledge and prediction creation, and creation cannot be in the future.
- Requires the **immediately next exchange session**, and prediction creation strictly before
  its open. No substitution of a later available session when the target is missing. This is a
  conservative tightening of v1 admission, not a rewrite of historical horizon identifiers.
  Requiring pre-open prevents a post-event prediction from being rescued by a late import.
- Independently requires outcome knowledge strictly after prediction creation and cutoff,
  after the target's conservative close, and no later than evaluation. A genuinely future
  target received late may still be evaluated; latency is retained, not counted as another session.
- Selects the earliest stored target knowledge instant, before checking admissibility. An
  invalid earlier revision is never skipped to select a convenient later revision; ties across
  revision identities fail closed. Offset-bearing timestamps are parsed and compared as instants,
  not ordered as SQLite text. Missing time zone, source revision or inconsistent revision time,
  checksum-derived revision identity, mapping, price or feature lineage fails closed.
- Resolves the reference close through the exact pinned source revision; requires feature
  revision creation no later than cutoff, explicit membership of that source revision, and all
  declared feature input revisions present and known by the cutoff.
  Reads and outcome append share one SQLite transaction. Existing UNIQUE prediction linkage
  preserves first outcome on repeated evaluation or later source corrections.
- `InvalidateBrokenLineage` changes only evaluation state for newly broken lineage. It never
  updates stored reasons, including already-invalidated rows. Original beliefs/causes remain
  byte-for-byte evidence; no synthetic replacement cause is written into an old prediction.
  The code predicate explains new invalidation; stored original reasons are not a new incident log.

There is **no schema migration, historical repair or production invocation**. The version is
bound to candidate code and the future manifest, not retroactively stamped onto old outcomes.
Normal prediction generation and all pre-existing outcomes remain separate from this admission.

The first focused run found an analyzer requirement (cached SearchValues) and an older
clock-dependent future-outcome fixture that could forecast after target open. The implementation
uses cached hash-character validation; the fixture now explicitly forecasts before a future
session. Neither correction changes production records. Then **139 focused tests PASS, 0 FAIL,
0 SKIP**, covering the exact reproduction, late imports, revisions, UTC offsets, open/close
boundaries, holidays/DST, missing targets, invalid lineage, retained reasons and repeated reads/
evaluation. Full solution Release build: PASS, zero warnings/errors. Targeted formatting,
documentation (292 Markdown / 53 backlog IDs) and whitespace: PASS. Final CI and independent
exact-candidate review are tracked at the checkpoint; no earlier CI result is attributed to later code.

### Historical outcome audit and access — bounded results

The [complete metadata audit](https://github.com/T-bear/BigBrain/issues/57#issuecomment-6796635237)
is reused, not repeated: 906 predictions, 858 evaluated / 24 invalidated / 24 pending,
66 logical revision pairs (42 evaluated/evaluated plus 24 evaluated/invalidated), zero collisions
when both source and feature revisions participate. These remain historical material only.
New read-only cadence GET again confirms 24/858/24, disabled and latest session 2026-10-02.

**Actual contents of 858 outcome relations: BLOCKED, NOT READ.** The deployed GET contract
exposes outcome existence, not each outcome's knowledge/revision/reference linkage. Host access
to the Finance volume is denied; the already-running API image has no sqlite/sqlite3/python3
reader. The existing historical inspection command is WIKI-specific and cannot audit these
relations. No new binary, container, general endpoint, executable upload, database constructor,
copy or privilege workaround was used. Needed next: an explicitly approved protected reader
that opens the existing database with SQLite `Mode=ReadOnly`, one consistent deferred read
transaction, no initialization/migration/cleanup, and returns only counts/commitments.
It must reconcile all 858 links, exact source/target revision and mapping, parsed source/feature/
creation/cutoff/outcome/evaluation times, next-session calendar, orphan/tied revisions, recomputed
returns and immutable outcome IDs. Count all failure classes separately; do not mutate or
reclassify historical evidence merely to make the audit pass. The synthetic tests prove corrected
selection, **not** the historical ledger's actual temporal integrity.

**Off-host ingress: BLOCKED.** No approved external LAN/Tailscale client or execution route was
identified; a specific route was requested. No peer was accessed or third-party probe used.
Existing same-host tests cannot establish isolation. Reinspection confirms both IPv4/IPv6 API
publication on port 18080 and Web publication on 13000. Both source-visible Web proxy and direct
API path need the external test matrix: approved LAN client, approved Tailnet client, a client
that should be denied, IPv6 where routed, and each declared reverse-proxy ingress. Use health
GET only (no provider data/write trigger); record allow/deny expectations, observed HTTP/TCP
result and authenticated-client class without publishing addresses. Read approved ACL/NAT rules
alongside these results. An absent route is UNKNOWN, not proof of protection.

### Backup evidence and exact release/rollback preparation

Current API compiled revision and OCI image, Web image/revision and healthy state were rechecked
and match the runtime table above. Existing WIKI backup artifacts were read **in place** through
the running container's existing identity using `stat` and `sha256sum`; nothing was copied.

| Backup | Current byte/hash verification | Scope and limitation |
| --- | --- | --- |
| `finance-backup-3b093584173d74f6`, 2026-08-15 | PASS, 495977922 bytes; SHA-256 equals artifact and content fingerprints | PublicDomain NASDAQ-WIKI, 1 revision / 3722 observations / 1 feature revision / 183 backtests / 3 robustness evaluations. |
| `finance-backup-8b549915b4e4129c`, 2026-08-16 | PASS, 495977949 bytes; SHA-256 equals artifact and content fingerprints | Same WIKI-only coverage. Neither contains the EODHD shadow journal or current whole-Finance state. |

The existing maintenance `finance-backup-verify` CLI was **not invoked**: source review shows it
first constructs writable EodhdMarketMemory and a protection store that cleans staging folders.
Its name does not make it a strictly read-only path. Current byte/hash checks are not a new
restore drill. Reuse the dated BB-085 WIKI drill only within that scope. Protected operator backup
and receipt roots exist but are unreadable under current identity; six-database routine and
historical owner receipts do not establish current daily coverage. **Unknown other backup sets
are not reported as nonexistent.** No decryption/integrity check of BB-132K, restore or new backup.

Read-only Compose resolution (secrets retained only in process memory) established:

| Binding | Verified identity / required plan action |
| --- | --- |
| Current API rollback image | `sha256:9310e3cc8ea3484d94bcfeed78b005ba1aecb5724e4ca2cfc28fe9deb44ab047`; compiled `e8f3e62cc446cf52a89045d1a3ce7d4c3a2116ff`, existing Media composite receipt remains a separate check. |
| Current Web, retained for release and rollback | `sha256:78f6a430905088dc7290e414e3082f10c12ad89d4e28c0426c10e4e3aaa2011a`; revision `3112868d73c7ef986b4fba5c9a09eb0deb9cd0f9`. No Web change is required by this correction. |
| Actual API Compose input SHA-256 | `e78259683d3c85ba93493b82959baea8a6e69b27e1171453552e2b59954bc798`; contains API+Web, 91 configured API environment values match runtime, 10 mounts, 2 networks, unless-stopped. |
| Actual Web Compose input SHA-256 | `5167af60392d0c95bac51145cba4253f52bf71f7619b0ddb62db1faf3bdb9e1a`; Web-only, zero mounts, 1 network, unless-stopped. |
| Runtime Compose config hashes | API `dac16f78fe96915408e0a53672b4c90a187c1845fdb6a58bf41a69067dfc0597`; Web `ded747dd2206f0ef4af491580b7a6cc58bfd8a1ce34c015a8b3c0d9e873a123c`. Private file paths remain in existing Compose labels, not Git. |
| Observed Finance flags | Eodhd.Enabled=false; AccountActive=true (configuration only); entitlement end blank; ObservationRuntime.Enabled=true; AlpacaDailyObservation.Enabled=true; ResearchScheduler.Enabled=false; ResearchOperations.MaintenancePaused=true; Fred.Enabled=false. |
| Proposed candidate API | Exact final accepted SHA/tree + immutable image digest and composition receipt **UNBOUND / NOT BUILT**. Do not invent an image ID or reuse a Media-only exception. |

Prepared sequence for a **separately authorized** release: (1) revalidate these identities and
current source/backup/ingress evidence; (2) bind accepted code, independent review, required CI,
immutable built API digest and exact composite-source diff, including the previously pinned
Finance policy blobs; (3) review startup behavior and a private resolved service configuration
preserving all mounts, networks, secrets and unrelated services; (4) validate schema compatibility
and protected rollback image against an authorized isolated backup before any restart;
(5) only then use a specifically approved existing execution mechanism for the API service;
(6) verify compiled identity, health, policy, counters and inactive workers. This task executes
none of these deployment steps and does not produce an executable approval package.

Rollback is **application-image-only** to the exact API digest above and unchanged Web identity,
with identical protected data mounts/configuration. Never restore/drop databases, reset journals,
regrant authority, prune images/volumes or restart other services. If schema/startup reconciliation
would make the old image incompatible, stop for a reviewed recovery decision rather than restore.
STANDARD explicitly excludes Finance; its Media exception cannot be reused.

A restart is not inert: ObservationRuntime/Alpaca flags are already true; owner-drop scanning,
constructor migrations/stale-run recovery and operations reconciliation can write independently
of EODHD cadence. Enabling EODHD also enables existing startup feature/backtest/robustness workers.
These must be reconciled with the future strictly model-free scope before any operational plan
is executable. No flag was changed here. **Release/rollback readiness remains BLOCKED** on this
startup/privilege contract and current Finance/dependent-store recovery evidence.

### Issue 57 operational manifest draft v2 — NOT LOCKED

This is the requested **reviewable proposed specification**, not approved research policy,
manifest approval or cohort activation. It fills the earlier estimator/cost/calendar gaps.
Before operational lock, bind a dated immutable UTF-8 manifest, SHA-256 of its exact bytes,
approval reference, final accepted code/tree and release digest. All remain pending until rights,
access, actual outcome audit, mapping and recovery gates pass. No first session/date is selected now.

- Source: EODHD Free only; existing policy `eodhd-free-personal-v2026-08-11`, accepted retention
  revision 2026-11-08. Current account attestation and source-specific rights references are
  required fields, not inferred from AccountActive. No new source, grant or data request.
- Universe/mapping: the eight instrument/provider-symbol pairs in the readiness table above,
  unchanged. Verify their existing venue/identity evidence before lock. No replacement of a
  delisted/unavailable symbol after seeing outcomes; record unavailable/invalid instrument-days.
- Strategies: buy-and-hold/v1 `{}`; momentum/v1 `{period:20}`;
  sma-crossover/v1 `{fastPeriod:10,slowPeriod:20}`. Parameter fingerprints respectively
  `sha256:44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a`,
  `sha256:50b7571dd68c97ae1dbd78ab570ec6669ba2dd5a11296c5c0ce90e7d54814465`,
  `sha256:63e3c6043aa162377b9e5e1f3bcfcd8822193d7f3105307c4c4633ed77775329`.
- Features: `core-daily-v1`, `daily-feature-engine-v1`, all 21 definitions and 12-decimal
  AwayFromZero rounding from DailyFeatureEngine.cs. Set fingerprint
  `sha256:50b70854a70ee7dd9ab67cae83defe6c8b7183388e9c5a487cc879cd0d521a5a`.
  Momentum uses raw `close[t]-close[t-20]`; SMA uses raw mean10/mean20; ties select TargetFlat.
  Snapshot exact blobs for engine, strategies, EODHD adapter/policy, shadow admission and calendar
  in the final manifest; source and feature revisions/knowledge cutoffs are pinned per prediction.
- Calendar: `us-equities-ny-v1`, America/New_York opens 09:30; retain its **conservative 16:00
  close bound**, including early-close days, and the 22:00 UTC provider window. Freeze the complete
  ordered upcoming session list for a maximum of 240 exchange sessions at lock, validated against
  [NYSE's published 2026–2028 calendar](https://ir.theice.com/press/news-details/2025/NYSE-Group-Announces-2026-2027-and-2028-Holiday-and-Early-Closings-Calendar/).
  That official calendar confirms early closes on 2026-11-27 and 2026-12-24; the code does not
  model 13:00 closes, so its later admission bound is deliberately conservative. Unexpected closure
  or calendar divergence pauses admission and requires a prospective amendment, not silent date
  replacement. This draft does not certify arbitrary historical/future calendar accuracy.
- Start/horizon: first source EOD session whose forecast can be created after lock and before
  the immediately following session's open. The first target must still be genuinely future.
  Prediction knowledge <= cutoff <= creation < target open < target conservative close <=
  target knowledge <= evaluation. Admission version `shadow-outcome-causality-v2`; existing
  serialized horizon remains `next-eligible-source-session-close-v1`.
- Selection: for each target-session/instrument/strategy/parameters/horizon, select the earliest
  valid prediction by creation time using only then-known evidence. Keep all source/feature
  revisions as audit records; conflicting same-instant candidates invalidate that key. Do not
  substitute a later prediction when the selected one becomes invalid. Freeze original target
  revision at its earliest unambiguous knowledge instant; later corrections are separate sensitivity
  evidence and never replace primary rows. All 906 pre-lock records are excluded by cohort identity.
- Accrual: a session qualifies for primary paired analysis only when all eight instruments and
  all three strategies have valid pre-open predictions and valid causal outcomes. Missing,
  insufficient, invalidated, corporate-action-unresolved and interrupted sessions remain in the
  ordered ledger with reasons. Accrue the first 120 qualifying sessions within the predeclared
  maximum 240; stop on the 120th irrespective of sign. If fewer qualify by 240, report insufficient
  evidence; no automatic extension or restarting to remove losses. Record each session once,
  not 24 independent experiments. Serial dependence is not assumed absent.
- Primary estimators: let `r[i,t]=rawClose[i,target]/rawClose[i,source]-1`,
  `y[i,t]=1(r[i,t]>0)` and `p[s,i,t]=1(TargetLong)`, otherwise 0 for TargetFlat.
  `A[s,t]=(1/8)*sum_i 1(p[s,i,t]=y[i,t])`; zero return counts as nonpositive, matching the journal.
  Always-long passive baseline is `A[L,t]=(1/8)*sum_i y[i,t]`; always-flat is also reported.
  Report `mean_t A[s,t]` and paired `mean_t D[s,t]`, where `D=A[s,t]-A[L,t]`, on the identical
  120 complete sessions. Buy-and-hold is a baseline sanity check, not an independent discovery.
  Publish each instrument/strategy's coverage and directional counts, with losses and zero returns.
- Uncertainty: for each reported session-mean series X, `e[t]=X[t]-mean(X)` on valid sessions,
  zero elsewhere in the fixed calendar ledger; `n` is the count of valid sessions. Use Bartlett
  HAC lag 5: `SE²=(sum_t e[t]²+2*sum_{l=1..5}(1-l/6)*sum_t e[t]*e[t-l])/n²`.
  Report approximate `mean ± 1.96*SE`, n and coverage; preserve calendar gaps when forming lags.
  These are descriptive approximate intervals, not independent-binomial guarantees or a trading
  acceptance test. Both active strategies are always reported; no best-strategy selection,
  significance promotion or multiplicity-adjusted success claim. DSR/PBO/calibration stay
  NOT_EVALUABLE; no probability forecast exists.
- Cost sensitivity: predeclare one-way combined cost `c` in `{0,5,10,25,50}` basis points.
  Report `q[s,i,t,c]=p[s,i,t]*r[i,t]-2*(c/10000)*p[s,i,t]`, equally averaged over the same eight
  instruments and sessions; compare to identical-cost always-long and zero-position baselines.
  This is a daily round-trip **close-based signal sensitivity proxy**, not an executable strategy
  return: the source close precedes the forecast. No fills, spread model, dividends, leverage,
  borrow, portfolio P&L, annualized Sharpe or profitability is inferred. Raw split/action-ambiguous
  intervals are invalid for primary analysis until independently resolved; no retroactive data fix.
- Reporting/interruptions: preserve missingness, acquisition/evaluation latency, negative and
  invalid outcomes, all attempts/revisions and gate failures. No performance-driven parameter
  changes, backfill, instrument replacement or window reset. Any strategy/source/feature/policy
  change requires a separately timestamped future cohort/amendment; preserve the original cohort.
  120 sessions permits evidence-adequacy review only. Tail/regime/drawdown claims unsupported by
  this directional journal remain NOT_EVALUABLE, not synthetic assurances.

### Gate disposition and next exact step

| Gate | Result for this checkpoint |
| --- | --- |
| Current identities, unchanged unrelated work/PR #82 | PASS, reverified; no merge/deployment. |
| Synthetic causal/revision/invalidation regression | PASS, 139 focused tests; no historical data edited. |
| Full 858-outcome audit | BLOCKED: missing approved protected reader; metadata-only evidence reused. |
| External ingress | BLOCKED: missing approved off-host client/route and verified effective ACLs. |
| WIKI backup artifact integrity | PASS for the two exact August artifacts, source-limited. |
| Daily current Finance/dependent-store backup and isolated recovery | BLOCKED: protected operator evidence inaccessible; WIKI scope insufficient; no new restore authorized. |
| Exact executable Finance release/rollback | BLOCKED: candidate image/approved mechanism and startup/compatibility/recovery binding outstanding. |
| Operational manifest | DRAFT v2, specified above; NOT LOCKED / NOT APPROVED / NOT STARTED. |
| EODHD/research/production changes in this task | NOT RUN, explicitly outside authority. |

Complete independent review of final PR #81 SHA/tree and its CI. Then end this bounded assignment.
Next technical work, if separately authorized: approve a narrowly protected read-only outcome
reader and supply existing operator backup/restore evidence plus approved off-host test vantage;
use those results to resolve the concrete operational gates before any manifest lock or activation.
