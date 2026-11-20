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

## Protected outcome audit design checkpoint — 2026-11-20

**REVIEW CHECKPOINT / risk 3 / synthetic only.** This resumes the
[architect's bounded assignment](https://github.com/T-bear/BigBrain/issues/57#issuecomment-6803404086)
and [WAL qualification](https://github.com/T-bear/BigBrain/issues/57#issuecomment-6804821431),
as confirmed by the owner's resumption and
[current handoff](https://github.com/T-bear/BigBrain/issues/57#issuecomment-7036256540).
Accepted baseline is `1e3d203477cc81e8b1e58780dd4a758926469b7d`.
Branch `checkpoint/finance-outcome-audit-20261120` starts there. Its exact publication
SHA/tree and CI are recorded in the review PR; this is not a merge candidate or operational reader.

The separately reviewed PR #81 remains untouched at
`afcf5d79a62be2831ff002014a9dfe972384f01f`, tree
`82528f28a6abba4168a8e8b2133f7b19d7c6761c`. Its 139 focused tests and independent
42-test causal review are reused for that unchanged candidate only. No correction is lost,
cherry-picked, merged or rebased. PR #82 remains untouched at
`ab76e7f2431804e0e8e7800429ed213eb7ec3081`. Both remain drafts.
The audit assignment explicitly requires this separate checkpoint; it is not a competing
implementation of PR #81. The five audited table contracts and exchange calendar are unchanged
between #81 and this accepted baseline. Current main's ingress/policy changes are retained.
Future integration will need to reconcile the overlapping canonical STATUS/BACKLOG/recovery/report
edits with #81; this checkpoint neither overwrites that branch nor resolves an integration merge.

### Implemented boundary and fixed report

`tools/BigBrain.Finance.OutcomeAudit` is an isolated .NET **library**, with internal audit
entry points and no executable, HTTP route, host integration, configuration/credential loading,
provider, broker, order client, migration, cleanup or snapshot creation. The only shared domain
source is the existing `UsMarketCalendar.cs`, linked at build time. It has no API/Modules
assembly dependency. The eight-symbol EODHD mapping is pinned from the accepted catalog.
The separate test project is included in the existing solution/backend CI; it constructs only
synthetic tables using the actual CREATE TABLE definitions, without constructing a storage owner.

The reader accepts only a pre-existing offline rollback-format SQLite image, supplied SHA-256 and explicit UTC snapshot cutoff. It rejects creation/evaluation
after that cutoff; authenticating the receipt remains the blocked operator responsibility.
It rejects links, WAL-mode headers, any WAL/SHM/journal sidecar, missing files, wrong digests and
images outside 100 bytes–256 MiB **before opening SQLite**. It never creates a missing DB.
It opens with `SqliteOpenMode.ReadOnly`, `immutable=1`, pooling disabled, `query_only=ON`,
`trusted_schema=OFF`, in-memory temporary storage and an explicit deferred read transaction.
`quick_check`, foreign-key checks and five fixed SELECTs run inside that transaction.
Only ordinary tables are admitted. More than 100,000 rows in any audited table stops the whole
report, rather than truncating and presenting a partial success. Image digest and absence of
sidecars are checked again after closing SQLite. No raw SQL/filter capability is exposed.

**These checks do not prove that an arbitrary operator-supplied path is stable or complete.**
The prototype has no approved production entry point. Immutable mode is valid only once an
independently protected, complete offline image has been established. Pre/post hashes and a
cooperative file handle are not an OS-level defense against replacement or concurrent writers;
a digest is not evidence of snapshot lineage, rights or authorization. Production use remains
STOP until the separate operator boundary below is approved and technically verified.

Report contract `outcome-ledger-audit-v1` emits fixed-label integer counts and either
`HISTORICAL_AUDIT_ONLY` or `STOP` with a fixed sanitized reason. It emits no symbols, row IDs,
market prices/returns, timestamps, paths, SQL, exceptions, credentials or source payloads.
Counts cover the complete five-table ledger, declared SQLite foreign-key failures, logical
orphans, duplicate identities, invalidated predictions with outcomes and per-outcome findings:

- Prediction mode/provider/horizon/strategy identity; explicit timestamp zones; source close,
  observation knowledge, cutoff, creation, immediately next US session and strict pre-open.
- Exact source and target observation/revision links, instrument/product/policy/MIC mapping,
  acquisition-to-revision time, checksum shape and checksum-derived EODHD revision identity.
- Earliest target knowledge revision, unknown or tied first revisions, later corrections and
  outcome knowledge strictly after prediction/cutoff and no later than evaluation.
- Feature existence, original source membership, all declared input revisions knowable at
  cutoff, feature creation time, checksum shape and the existing raw-price-basis contract.
- Unique outcome identity/link, reference and target price agreement, exact decimal return
  and direction, including negative and zero returns.

Findings overlap and must not be summed as disjoint failed-row counts. The bounded calendar
reuses `us-equities-ny-v1` (normal 16:00 New York close, conservative on early-close days);
unsupported date range is rejected. There is no claim of a new exchange calendar or adjusted
corporate-action proof. `v2-criteria-satisfied` is a diagnostic comparison, not a prospective
success or a claim that v1 admission used v2. The schema has no admission-version field: every
stored outcome remains **historical/unversioned**. Original raw-payload and complete feature
content hashes are **not recomputed**, explicitly counted as unknown; no payload access is
introduced. These limitations prevent a scientific/rights PASS even with zero reported defects.

### Synthetic verification and observed failures

73 focused tests PASS on 2026-11-20. Exact locally observed provider:
**Microsoft.Data.Sqlite assembly major=10, minor=0, build=10, revision=0; SQLite 3.53.3**. Native package versions are pinned alongside
the existing API dependency versions; this is not a statement about a future operator VFS.
Tests use temporary synthetic fixtures only and remove their own fixtures on disposal.

- ReadOnly + query_only + explicit read transaction over synthetic live WAL **changed existing
  SHM bytes**; the absent-SHM control **created SHM**, with unchanged DB/WAL bytes.
- Immutable access directly to uncheckpointed synthetic WAL missed the WAL-only table.
  The real reader rejects these inputs, including a copied WAL-header file with hidden sidecars,
  without creating SHM or changing bytes. Rejection is not successful live-ledger visibility.
- SQLite backup API is used **only in test fixtures** to make a consistent synthetic snapshot,
  followed by rollback journal mode. The reader sees committed WAL-only rows, excludes another
  connection's uncommitted change, and remains stable after the source writer advances.
- Main/sidecar file-name sets and SHA-256 values remain unchanged through accepted/rejected
  reader calls. UPDATE, DELETE, DDL and temporary-table attempts fail under query_only;
  persistent DML also fails independently with SQLite READONLY when query_only is disabled
  in the synthetic negative control. No test grants production access.
- Tests cover known-before-prediction outcomes, late import/arrival, later/ambiguous revisions,
  missing/null/invalid lineage, orphan/duplicate links, original invalidation evidence, time
  zones, pre-open boundary, US holidays/DST, repeat reads and whole-report resource-limit STOP.
- Initial local builds caught analyzer requirements (cached searches/arrays and test naming).
  These were fixed without suppressing rules. The source's actual price-basis string is checked;
  no invented `RawClose` storage enum is used. No production experiment or failed drift action.

Release solution build PASS (zero warnings/errors); documentation and whitespace PASS.
Required final format/secrets/CI results and exact independent-review status
are attached to the publication PR. Prior #81 review does not review this new library.

### Reused evidence and current gates

| Gate | Current assessment and evidence limit |
| --- | --- |
| Offline design and synthetic no-write/causality tests | **PASS** within the fixtures and version above; not an operator privilege-boundary proof. |
| Actual historical outcomes | **BLOCKED / NOT READ**. Reuse [906-prediction metadata audit](https://github.com/T-bear/BigBrain/issues/57#issuecomment-6796635237): dated 858 evaluated / 24 invalidated / 24 pending, 66 logical revision pairs, no full-revision-key collisions. No new production query; these are not today's remeasured counts. |
| Ingress correction | Reuse [#101 owner acceptance](https://github.com/T-bear/BigBrain/issues/101#issuecomment-7002654042): Nov 19 direct API loopback, missing/wrong credentials denied, authorized Web unlock accepted. The old all-interface observation is historical. No new network probe. |
| Overall isolation / Finance level | **Risk 3**. [Nov 19 audit](https://github.com/T-bear/BigBrain/issues/101#issuecomment-7002793301) remains NO-GO for level 2: broker credential surface, incomplete Media/network/Tailscale/backup isolation and RESEARCH negative guarantees. This library does not repair or certify the whole runtime. |
| Rights / BB-132K | **BLOCKED for protected access/copy**. [Owner attestation](https://github.com/T-bear/BigBrain/pull/82#issuecomment-6795991464) is preserved as owner-reported written permission, not independently inspected. Nov 18 proposed re-review date has passed; no later resolution was found in the inspected handoff/#57/#82. Neither automatic extension nor rights expiry is inferred. No reserved decision is needed for synthetic data. |
| Backup / rollback | Reuse scoped Nov 19 STANDARD backup/restartability receipt evidence in #101; do not infer absence of backups. It does not itself grant a restricted EODHD copy or prove this audit's operator boundary. Current daily/full-Finance recovery coverage remains unverified in this task; no backup/restore/restart attempted. |
| Manifest / cadence | **NOT LOCKED / NO NEW COHORT**. [Fixed evidence-lock proposal](https://github.com/T-bear/BigBrain/issues/57#issuecomment-6803578385) is still proposed, including five-session/22:00 cutoff. No operational policy accepted here; no EODHD calls. Cadence was last documented disabled; not remeasured. |
| Runtime revision | No live inspection in this task. Handoff's dated API `873013d2` / Web `1e3d2034` supersede Nov 11 runtime notes; neither is this audit or PR #81. |
| Independent review / merge / production | Exact new-candidate risk-3 review required; request attached to PR. **No merge, deployment, data-copy, key access, provider/model/order or production audit authority.** |

### Minimum future protected operator route — proposal, not executable approval

Live-WAL reading is rejected. If a complete no-write live reader cannot be independently proven,
seek a **separate, rights-approved, operator-generated consistent offline snapshot**. This is
not permission to copy the real DB. Before any such action, bind an exact reviewed tool SHA/tree
and native SQLite/VFS; a fixed source/storage identity; operator scope; restricted provider/data
classification; explicit copy/read grants including applicable BB-132K decision; source snapshot
transaction and creation timestamp; exact byte count/SHA-256; coverage and completeness receipt;
protected local ownership; agreed retention/inventory/cleanup disposition; and backup/rollback
conditions. Do not reuse WIKI public-domain rights for EODHD or make a mixed-rights export by default.

The generating operator must own source access and establish completeness while writers are
handled through an approved consistent-snapshot mechanism. The audit process must receive only
that sealed image via an independently verified read-only file/directory boundary, without
mutable parent paths, symlinks, credentials, source-volume access, network, Docker socket,
root/sudo or an arbitrary path/query channel. Bind a fixed command and sanitized aggregate-only
result; no public/general SQL endpoint. Qualify absent/present SHM, concurrent writers, actual
image completeness and filesystem no-write in that exact approved environment **before real
execution**. Verify before/after image identity and protected receipts. Cleanup/disposal is a
separately authorized operator action, never an automatic reader side effect. Until these
conditions and independent review pass, **STOP: do not read the real 858 outcome rows**.

Next step is ChatGPT's independent review of this exact published checkpoint and the operator
proposal. No automatic implementation of the operator route, manifest policy or activation follows.
Issues #55/#57 remain open; #54/#59/#60 retain their own scope.

## Owner-authorized integration checkpoint — 2026-11-20

**Verifierad fortsättning efter Codex sandbox-avbrott:** den auktoriserade
Remote Desktop-miljön har slutfört dokumentkonflikterna och lokala merge-commits
(`48fc319` för PR #81, `6917ced` för PR #111). Release solution build
PASS med 0 varningar/fel; 86/86 audit-tester och 84/84 fokuserade Finance/API-tester
PASS; `dotnet format --verify-no-changes` PASS. Dokumentationsmetadata och
recovery uppdateras inom denna checkpoint. Publicering har genomförts som Draft PR #112; GitHub required CI och
oberoende integration-review kvarstår som egna grindar. Den nedanstående
BLOCKED-tabellen avser Codex sandboxens tidigare tillstånd, inte den
senare Remote Desktop-verifieringen. Ingen produktionsåtkomst, merge till
main eller deployment har utförts.

**INTERRUPTED — SAFE TO RESUME / NOT A MERGE CANDIDATE.** The owner resumed the credit-blocked
risk-3 integration on `checkpoint/finance-integration-20261120`, based on verified GitHub main
`1e3d203477cc81e8b1e58780dd4a758926469b7d`. Scope is the two exact reviewed implementations,
canonical documentation, synthetic regression tests, Release build/format, and publication as a
Draft PR for independent risk-3 review if permitted. No real data, snapshots, protected operator
run, provider/model/trading, merge to main or deployment. Maximum two repair attempts; stop on
drift or unresolved conflicts. No implementation repair has been attempted in this resumption.

This section supersedes older continuation directions in the preserved sections above. Those
sections remain complete historical evidence, including their failures and scientific limits.
The owner identifies both heads as independently reviewed; GitHub PR review/CI metadata could
not be re-read in this sandbox. Earlier reviews do not review the new integration candidate.

| Identity | Exact value / disposition |
| --- | --- |
| Accepted main | `1e3d203477cc81e8b1e58780dd4a758926469b7d`, matched GitHub at start; unchanged locally |
| PR #81 head / tree | `afcf5d79a62be2831ff002014a9dfe972384f01f` / `82528f28a6abba4168a8e8b2133f7b19d7c6761c` |
| PR #111 head / tree | `1d332cd8c3fc7b97be79718c1462e64c0146b6da` / `8ae7bbd629b48a778a4031c92c8d9b613e261c58` |
| Saved PR #81 integration merge / tree | `48fc3192540c4323566e96f79fb7ef48f44238c6` / `93064bf3f7bf21802984347fef16697e87adcaeb` |
| Pending merge parent | Exact PR #111 head above; merge remains open because Git index writes were denied |
| Publication / integration CI / independent review | NOT PUBLISHED / NOT RUN / PENDING; no Draft PR created |

PR #81 was merged with history retained. PR #111 then merged without code conflicts; only
`codex-recovery.md` and this report conflicted, as predicted. Both full report additions are
preserved verbatim, in chronological order. Recovery now identifies the new integration scope,
source heads, prior evidence and remaining operational gates. STATUS/BACKLOG distinguish dated
source tests from the unverified integration. Source branches and PR #82 were not changed.
No source code, test, project dependency, schema, authority or isolation rule was modified beyond
the exact reviewed implementations. Both implementations remain separate: API causal admission
and reason preservation, plus the non-executable isolated audit library with fail-closed WAL,
sidecar, digest and malformed-storage checks. No live reader or operator entry point is added.

| Verification on integrated working files | Result |
| --- | --- |
| Main and both source heads vs GitHub at start | PASS, exact matches; checkpoint initially clean |
| 11 PR #81 code/test files vs exact reviewed head | PASS, byte-for-byte |
| 9 PR #111 project/library/test files vs exact reviewed head | PASS, byte-for-byte, including malformed-storage correction |
| Both complete report additions vs their source heads | PASS, retained without losing evidence |
| Whitespace and conflict-marker checks | PASS on resolved working files; Git index still records the two conflicts |
| Documentation verifier | BLOCKED: Node child-process `spawnSync git EPERM`; not PASS |
| Restore prerequisite | BLOCKED: sandbox NuGet access failed with NU1801; bounded single-process attempt stopped after the error |
| Release solution build / format / synthetic regressions | PENDING: restore did not complete; no integrated test count claimed |
| Secrets scan / required CI | PENDING; no local gitleaks executable found, no publication |
| Git staging / final local merge commit | BLOCKED: Git worktree metadata is read-only (`index.lock` creation denied) |
| Remote publication / Draft PR | BLOCKED: GitHub API connection unavailable and final commit unavailable |

The original audit report's 73 tests refer to its first checkpoint. The owner reports 86 tests
for PR #111's corrected final head; the extra malformed-storage tests are present. The 139 PR #81
focused tests and independent 42-test causal review remain source-specific historical evidence.
Required next verification includes the full synthetic Finance/API regression surface (especially
causality, reasons, cadence/quota, source/feature lineage and authorization), all 86 audit tests
including WAL/no-write/malformed input, Release solution build and format, documentation/secrets
checks and applicable CI. Keep real-model opt-ins disabled. No test or check failure was suppressed.

Resume only this checkpoint, retain the pending merge, reverify main and both source heads, stage
the resolved documentation plus STATUS/BACKLOG, complete the PR #111 merge without rewriting
history, run the pending checks, then publish a Draft PR with exact SHA/tree/CI for independent
review. If drift or a blocking check remains, stop under the existing two-repair limit. No security
rule change or privilege workaround was made. [Recovery](../../../operations/codex-recovery.md#finance--integration-återupptagen-2026-11-20)
is the sole interruption entry point. Actual outcome audit, manifest lock and all operational gates
remain outside this assignment.

## Synthetic operator-boundary qualification — 2026-11-20

**Reconciled by the next assignment:** PR #113 is accepted in main
`8b81318ab7ad39ef9c11d19ede4f5573b56a33f9`, verified locally and through GitHub.
The review/publication states below describe that earlier checkpoint. The separate
[OS qualification](#linux-os-isolation-qualification--2026-11-20) does not reimplement it.

**REVIEW CHECKPOINT / risk 3 / SYNTHETIC ONLY. Independent risk-3 review PENDING.**
The owner authorized #57's bounded threat model, synthetic regression tests, validation and
isolated draft publication on `checkpoint/finance-operator-qualification-20261120`, based on
verified local and GitHub main `fcc126478a419d3bb5f5d06aefb3fb196fb12ca1` (merged PR #112).
This supersedes the integration section's pre-merge status, without asserting deployment.
Exact publication SHA/tree belong to Git and the draft PR, not a self-referential document hash.
The [minimum future route](#minimum-future-protected-operator-route--proposal-not-executable-approval)
remains a proposal. No production snapshot generator, seal implementation, executable,
HTTP endpoint, privileged helper, grant issuer or arbitrary-path interface is added.

Authorized DoD: document trusted operator → consistent synthetic SQLite snapshot → sealed offline
image → existing audit library; cover custody, substitution, completeness, grants and failure;
exercise safe bounded synthetic cases; retain explicit real-world UNKNOWN/BLOCKED states;
validate build/format/docs/tests; publish a draft with exact identity and independent review pending.
Maximum two repair attempts. Stop if privilege or live data becomes technically necessary.
No real source/snapshot, protected-directory probe, root/sudo/Docker, network service, key,
model/provider, trading, deployment or merge is authorized. PR #82 remains a separate draft
at `ab76e7f2431804e0e8e7800429ed213eb7ec3081`; its owner attestation is preserved, not re-certified.

### Trust and custody contract — design only

Assets are source integrity, complete historical population, provider-restricted bytes, authority
receipts and sanitized results. Threat actors include a compromised ordinary module/audit process,
an unprivileged local path manipulator, accidental concurrent writer, incorrect operator selection,
and malformed database content. The proposed trusted computing base is the separately authorized
operator, verified OS/filesystem boundary, exact reviewed binaries and pinned native SQLite/VFS.
A malicious privileged operator or compromised OS cannot be excluded by an image digest. Treat
unknown custody as STOP; do not describe it as an acceptable residual scientific uncertainty.

The logical states below are requirements, not an implemented state machine:

1. **AUTHORIZED:** independently verify exact operator principal, fixed source/storage identity,
   tool SHA/tree and dependency/native/VFS identity; purpose, provider/product/policy and allowed
   read/copy/use; finite validity, revocation and invocation budget; protected destination and
   inventory/retention/disposal plan; required backup/recovery evidence and BB-132K disposition.
   Source permission does not imply copy permission; copy does not imply audit or publication.
   The owner authorizes any future operation separately. Missing/stale/conflicting grants stop
   before opening a source. This task issues or consumes no real grant.
2. **CAPTURING:** only the trusted generator may access the fixed source and private staging.
   Use an approved SQLite-consistent snapshot mechanism with a pinned read transaction and an
   established read before racing commits; bind the actual source transaction to its receipt.
   A raw DB/WAL/SHM copy, an arbitrary timestamp, or `immutable=1` on a live source is insufficient.
   A source connection can modify SHM even with SQL read-only; source-side permitted effects
   need separate review and measured evidence. If strict source no-write cannot be established,
   stop rather than silently grant it or checkpoint the source. This task's synthetic writer
   and backup fixture deliberately have write access; they prove no real no-write generator.
3. **COMPLETE:** backup completion and closed destination handles precede publication. Normalize
   only the private synthetic destination to rollback journal mode, never the source. Establish
   source coverage in the SAME pinned transaction: all five audited tables, full populations,
   identities/revisions and a deterministic content commitment, original schema/version, source
   epoch/cutoff and counts. Reconcile destination content, not just matching counts. Include
   missing/invalidated/revised outcomes; no success-only subset or arbitrary row filter. Preserve
   source rights and all required lineage. A mixed-rights whole-database backup is not a default
   permitted export: if all copied tables cannot be authorized, stop for a separately reviewed
   selection/derivation design. Existing audit coverage is five tables, not every Finance store
   or external payload; checksum/feature-content and prospective-science unknowns remain unknown.
4. **SEALED:** close writers, flush/complete publication under a reviewed crash-consistency
   protocol, and bind exact byte length/SHA-256, source commitments, transaction/capture time,
   grants, schema/tool/native identities and completeness receipt in independently protected
   custody. An authenticated protected receipt must come from the trusted operator, not the
   audit caller. Record READY only after data and receipt are durably complete. Partial writes,
   crash, uncertain completion or mismatched receipt never become READY on retry.
5. **AUDITING:** the auditor sees only the fixed sealed image plus fixed receipt, with OS-denied
   writes and no source volume, credential, network, admin, Docker, provider or model access.
   Its command/query/output contract is fixed; it cannot choose a path, SQL, source or grant.
   Verify receipt validity/revocation and stable identity immediately before and after use.
   The existing internal reader still applies its read-only/immutable/query-only/deferred
   transaction, size/row/schema/integrity/sidecar/digest checks. Only aggregate fixed-label
   output is released, bound externally to the receipt and candidate identity. Historical audit
   completion never grants scientific PASS, prospective status, cohort lock or execution.
6. **CLOSED or STOP:** keep a sanitized outcome and inventory reference. An ambiguous invocation
   is not automatically retried or refunded. Retention, revocation and disposal remain operator
   responsibilities under explicit authority; the auditor cannot clean up, restore or repair.
   An abandoned restricted staging image must remain protected and inventoried pending disposition,
   never silently copied to Git, logs or generic backup. Synthetic fixture disposal is unrelated.

### OS protection and substitution threats

A seal means an independently enforced immutable custody interval through the LAST read, not a
filename, `chmod` by the same owner, `FileShare.Read`, a digest, SQL read-only or SQLite's immutable
flag. The future auditor must lack permissions to change ownership/mode, write file contents,
modify any path ancestor, rename/unlink targets, create links/sidecars, remount storage, attach a
debugger to the operator, or replace tools/receipts. Trusted operator access must also prevent
accidental writes during that interval. A read-only view alone is insufficient if another writable
alias or pre-existing write handle can still modify the underlying object. No concrete OS seal
mechanism is selected, installed or certified here; qualification in the exact future environment
and independent review are required before a real image can be accepted.

| Threat / failure scenario | Required future operator control; existing library limit |
| --- | --- |
| WAL-only committed data; missing SHM; writer advances during capture | Approved consistent SQLite backup with established source read transaction and complete receipt. Never strip sidecars or relabel live WAL as immutable. Existing reader rejects WAL headers/sidecars. |
| File or parent symlink; junction/reparse or mount substitution | Pin protected storage/root and resolve every component under OS-enforced custody; reject aliases or unexpected filesystem object types. Library link checks are preflight only and are vulnerable to a race if ancestors remain writable. |
| Hardlink or already-open writable handle | Prevent alternate writable aliases, verify link/custody policy and eliminate pre-existing write capabilities before seal. Library does not check link count. A single-link observation alone cannot rule out an open writer or later link. Hardlink/handle isolation is UNKNOWN here. |
| Rename/path swap after hashing but before SQLite open | The hash stream and SQLite open resolve separately; the stream's post-hash may still refer to the old inode. Protected namespace/object identity throughout use is mandatory. Pre/post hashing is not a TOCTOU defense; no test hook or production race fix is added. |
| Swap A→B→A between observations, same-byte replacement, altered receipt | Prevent substitutions at OS boundary and authenticate the receipt independently. Digest checks cannot prove custody even when bytes match. Receipt supplied by the same untrusted caller adds no authority. |
| Consistent but incomplete or wrong-source image | Compare full content/coverage against trusted same-transaction source commitments. A valid SQLite image, zero findings and matching caller digest cannot prove it includes the original population or correct source. |
| Partial/corrupt/oversized image, row overflow or schema masquerade | Fail the whole report, retain no partial-success counts. No automatic repair/truncation/limit widening. Existing 100-byte–256 MiB image and 100,000-row/table bounds remain. |
| Resource exhaustion or hostile native input | Fixed tool/native dependencies and independently enforced finite CPU, memory, time, file and output limits. Existing size/row limits are not a hard time/memory sandbox; exact OS limits and kill behavior remain unqualified. |
| Interrupted creation, seal or audit; expired/revoked grants | No READY/result acceptance from partial/uncertain state; protected invocation/inventory audit and separately authorized reconciliation. Never regenerate authority or infer permission from an old successful run. |

### Synthetic regression evidence and its limits

`OperatorBoundaryTests` uses only fresh `SyntheticLedger` temporary fixtures. The fixtures have
no user-selectable source path and no connection to a real database. They exercise the existing
internal library; source and tool implementations are unchanged. Their digest receipts are local
test values, **not authenticated operator receipts or OS seals**.

| Case | Evidence sought; scope limit |
| --- | --- |
| Commit during pinned WAL read | A second SQLite connection commits atomic prediction/outcome deletion after the first read and before backup, while the read transaction stays open. The snapshot retains the original five-table counts and clean historical result. Deterministic interleaving, no scheduling sleep or claim of arbitrary race coverage. |
| File/parent rename replacement after receipt | A different valid image at the same path is rejected with `image.digest`. Leaf/parent symlink substitution is rejected with `image.link`. These tests swap before entry; they do not prove prevention of mid-read/ABA races. |
| Same-byte replacement | Accepted historical report deliberately demonstrates that matching digest does not authenticate custody. This documents the pre-existing excluded trust assumption, not an installed defense. |
| Consistent omitted population | A valid snapshot with both prediction and outcome population removed reports zero rows and no findings. The source still has one outcome. This demonstrates why an external completeness receipt is mandatory. |
| Oversized image | A sparse fixture one byte above 256 MiB stops with empty counts/findings and unchanged file names/hashes. Existing corrupt/truncated/schema/row-limit tests remain applicable. |
| WAL/SHM, uncommitted writer, no write | Re-run existing audit suite: absent/present SHM hazards, WAL rejection, consistent backup and uncommitted exclusion, unchanged file sets/hashes, denied DML/DDL/temp writes, malformed/corrupt input and sanitized STOP. File-byte evidence does not certify host access control, metadata/access-time immutability or all VFS behavior. |

Validation results are recorded below after execution; no result in this table is a real-source
qualification. No broad application/model/provider test or network service is required for this
test-only delta. The unchanged library still has no executable entry point or application/broker/
network assembly reference; its existing boundary regression remains in the focused suite.

### Remaining gates

| Gate | Disposition in this checkpoint |
| --- | --- |
| Exact OS seal, principal separation, filesystem/mount/link/handle/race protection | **BLOCKED / UNKNOWN**; no protected-directory probes or host qualification. |
| Real provider/native/VFS behavior and consistent source no-write capture | **BLOCKED / UNKNOWN**; only synthetic SQLite/package behavior can pass here. No real provider transport or credentials. |
| Real read/copy/use grants, source rights and BB-132K disposition | **BLOCKED / UNKNOWN**; existing owner-reported attestation preserved, no independent rights verification, expiry extension or disposition action. PR #82 untouched. |
| Current daily/full-Finance backup, restore and rollback readiness | **BLOCKED / UNKNOWN for this route**; dated scoped evidence is not absence of backups and not new copy/restore authority. |
| Real 858-outcome audit, source population/lineage and completeness receipt | **BLOCKED / NOT READ / UNKNOWN**; 858 is dated metadata, not a remeasured or independently validated population. |
| #57 manifest/cohort, models/providers/cadence/trading | **NOT LOCKED / NO ACTIVATION**; no new observations, policy or research execution. |
| Independent review and real execution | Exact new-candidate risk-3 review **PENDING**. This draft does not authorize real capture/audit, deployment or merge. |

### Local validation — 2026-11-20

| Check | Result |
| --- | --- |
| `dotnet restore BigBrain.slnx --ignore-failed-sources --disable-parallel -p:NuGetAudit=false` | **PASS** using available packages; this deliberately does not claim a vulnerability audit. No dependency/version change. |
| `dotnet build BigBrain.slnx --configuration Release --no-restore -m:1` | **PASS**, zero warnings/errors; SDK major 10, minor 0, patch 302. |
| `dotnet format BigBrain.slnx --verify-no-changes --no-restore` | **PASS**. |
| `dotnet test tests/BigBrain.Finance.OutcomeAudit.Tests/BigBrain.Finance.OutcomeAudit.Tests.csproj --configuration Release --no-build` (TRX logger) | **PASS: 94 / 0 FAIL / 0 SKIP**, including eight new operator-boundary cases and all 86 existing audit tests. |
| `node scripts/verify-documentation.mjs` | Initial document delta **PASS**, 296 Markdown files / 57 unique backlog IDs. Final rerun after adding this results section **BLOCKED** by sandbox `spawnSync git EPERM`, reproduced once without changing the validator or permissions. Final exact-document CI remains PENDING. New cross-document anchors separately **PASS**. |
| Staged whitespace/scope review | **PASS**; one test file and five existing canonical documents, including reconciliation of the completed integration recovery note. No source/library/project/dependency/infrastructure changes. |
| Local secrets scanner / GitHub required CI | Local gitleaks unavailable: **SKIP**, not PASS. Required CI including secrets **PENDING** at publication; consult exact draft-PR metadata. Manual delta review found only synthetic literals and sanitized documentation. |
| Independent candidate review | **PENDING**; self-checks and the earlier integration review do not review this candidate. |

No failed build/test/format check and no implementation repair attempt was needed (0/2 used).
One bounded documentation-verification retry reproduced the sandbox restriction; no bypass or
privilege escalation was attempted. Observed synthetic provider: Microsoft.Data.Sqlite assembly version (major 10, minor 0, build 10, revision 0),
SQLite 3.53.3; the existing plain-read-only WAL control changed SHM bytes in this run.
Tests do not enforce or certify real OS/network isolation. No operational gate above is marked
PASS by these local checks. There is no automatic next implementation, real snapshot/audit,
merge or deployment.

### Review-checkpoint publication — 2026-11-20

The branch was published as [Draft PR #113](https://github.com/T-bear/BigBrain/pull/113)
from verified accepted main fcc126478a419d3bb5f5d06aefb3fb196fb12ca1.
The initial implementation commit was d4ba4a9d7f5102cf167bbe9dceb65c93c0ed9a33,
tree 3d9cb3d8de90a5f61421e258fd7e18300048516c.
During Codex's sandboxed session Git metadata writes and PR creation were
blocked; publication was completed through the authorized Remote Desktop
connection without changing privileges or protections. A final documentation
rerun outside Codex's sandbox caught version numbers that matched the existing
private-address detector. They were rewritten as explicit version components,
without weakening the detector. Final candidate SHA/tree and checks must be
read from the updated Draft PR. Exact-candidate risk-3 independent review and
GitHub required CI remain separate gates until verified. No real-data read,
protected snapshot, operator installation, merge or deployment is authorized.

## Linux OS isolation qualification — 2026-11-20

**REVIEW CHECKPOINT / risk 3 / SYNTHETIC ONLY; independent review PENDING.**
Owner “This is the way” authorizes this bounded next #57 assignment on
`checkpoint/finance-os-isolation-20261120` from accepted main
`8b81318ab7ad39ef9c11d19ede4f5573b56a33f9` (PR #113). The six-part DoD is: precise Linux
principal/custody design; small unprivileged negative proofs; future operator acceptance/STOP
matrix; synthetic regression/build/format/docs and required CI including secrets; canonical
documentation and exact checkpoint identity; no production capability or activation.
Only a Draft REVIEW CHECKPOINT may be published, never merged or deployed under this mandate.
Maximum two repair attempts; no next sprint. Administrative thaw grants no operational authority.

### Proposed Linux boundary — not installed or qualified

This refines the accepted [custody contract](#trust-and-custody-contract--design-only), without
changing architecture, the existing audit library or its five-table coverage. Linux DAC,
separate UIDs, private mount namespaces and a read-only backing filesystem are feasible design
facilities on the project's Linux platform; availability and enforcement on the real host are
**UNVERIFIED/BLOCKED**. No account, mount, namespace, ACL, capability, helper or service is created.
If provisioning requires administration, stop for a separately authorized, independently reviewed
operator prerequisite; do not try root, sudo, Docker or an unprivileged-user-namespace workaround.

| Principal / phase | Exact intended boundary; future evidence required |
| --- | --- |
| Trusted provisioning/custody operator | Separate administrative trust boundary; narrowly authorized preparation only. Owns all protected path ancestors, immutable tool/runtime configuration, receipt trust anchor and release inventory. Ordinary modules and audit cannot invoke it or modify its configuration. It is trusted against malicious action; compromised administrator/kernel/storage is outside this seal's guarantee and forces STOP. No generic privileged helper is proposed. |
| Capture process | Dedicated unprivileged UID, distinct from API/Web/Brain/Media and auditor, with explicit supplementary groups. Reads only the approved fixed source through a separately qualified no-write route; writes only fresh restricted staging. No broker/credential/network or signing-key access. Process and all descendants exit before custody transfer; no pooled connections, inherited writable descriptors, mappings or IPC descriptor transfer survive. A separate UID alone does not protect a still-writable shared inode. |
| Seal custodian | Receives completed staging after capture exit, independently verifies content and handles, and publishes to fresh dedicated storage under exclusive custody. Files and EVERY ancestor belong to a non-auditor principal, with no auditor/ordinary-module write permission or ACL. Seal the backing filesystem read-only, not merely one bind mount. Remove/exclude every writable mount/alias and writable device access for all untrusted principals. No writer is admitted until the last audit read ends. Failure to prove exclusive custody or read-only transition is STOP. |
| Audit process | Different UID/GID, no shared supplementary writer groups, fixed reviewed binary/native SQLite/VFS, fixed image and authenticated receipt only. A private mount namespace exposes read-only image/receipt/tools and no source, staging, private rights, credentials, admin sockets or backing devices. All permitted/effective/inheritable/ambient/bounding capability sets empty; no_new_privs; no setuid/file-capability route, setns/unshare/mount or ptrace/process-memory/descriptor-transfer route to other principals. No network/IPC with writers. A future reviewed launcher must enforce this before parsing hostile SQLite; none is implemented here. |

Filesystem mode bits deny specific new opens/creates under the current credentials. The owner
can change them back, and chmod does not revoke an existing writable file description or mapping.
A private mount namespace isolates names; it does not freeze the underlying inode. A read-only
bind view can coexist with a writable alias in another namespace. The chosen future design
therefore requires BOTH separate principal/namespace confinement AND exclusive, read-only backing
custody. An `st_nlink == 1` observation, `/proc` handle scan, digest, SQLite `immutable=1` or
`FileShare.Read` alone cannot establish that condition. Handle scans are corroboration only;
the positive proof is exclusive staging, bounded process/descriptor ownership, writer termination
and enforced exclusion of new writers through the complete custody interval.

Fixed paths must resolve under custodian-owned ancestors without symlinks, hardlink aliases,
unexpected mounts or replaceable directories. A leaf `O_NOFOLLOW` check does not cover parents.
Descriptor-relative resolution with each parent checked, or a reviewed `openat2` policy rejecting
symlinks and mount crossings, can support admission; neither repairs a writable namespace after
admission. The existing library hashes an open stream and separately opens the path in SQLite;
that path must remain the same protected object until BOTH handles close. No library race fix,
production resolver or new executable is included here.

### Source transaction, sealed image and authenticated receipt

Before source open, bind owner authorization, operator identities, exact source/storage identity,
provider/product/policy, read/copy/use rights, BB-132K disposition, finite invocation/revocation
and backup/recovery evidence. ADR0021/0023/0028/0029 remain authoritative. A whole-database copy
requires permission for every copied table, including tables outside the audit's five; if that
cannot be demonstrated, STOP for a separately reviewed export design. No rights or expiry are
inferred from the administrative thaw, prior attestation or this synthetic run.

Capture must establish the source read transaction by an actual read before backup and before
any competing commit. In THAT SAME transaction, commit to the complete populations of
`shadow_predictions`, `shadow_outcomes`, `revisions`, `observations`, `feature_revisions`:
table/schema identity, row count and deterministic full-row content commitment with explicit
column order, types, null encoding, stable ordering and duplicate multiplicity. Include invalidated,
pending, revised and missing-outcome populations; no success filter. Record original schema,
source epoch and knowledge/capture cutoffs. A wall-clock label or matching counts is insufficient.
Reconcile these commitments against the completed destination before normalization/publication.
PR #113's pinned-WAL regression is retained; it is not a trusted real-source transaction receipt.
Feature checksums/content and broader scientific completeness remain separately unknown.

SQLite read-only can write SHM. A live-WAL `immutable=1` open can omit committed content. The future
generator must demonstrate its exact provider/VFS source-no-write behavior, including sidecars,
on an authorized representative synthetic operator environment first. Denied writes must stop
capture, never trigger chmod, source checkpoint, sidecar removal or automatic retry. If a consistent
strict-no-write source transaction is unavailable, the real route remains BLOCKED; this design
does not authorize quiescing services, copying real files or restoring backups to work around it.

Use a versioned, authenticated receipt from a separate protected signing/custody authority, with
the verifier's trust anchor installed outside auditor control. No key or receipt issuer is added.
Bind invocation ID, source identity/transaction commitments, all five table commitments, image
length/digest/object identity, schema, code SHA/tree, binary/native hashes, principal/namespace
evidence, capture/seal interval, grant identity/expiry/revocation and inventory/retention reference.
The auditor-supplied digest is not authentication. Verify signature, exact expected invocation,
grant and revocation immediately before admission and again before result release. Old valid
receipts, caller-selected keys and mismatched images stop; a signature cannot replace OS custody.

The future durable publication protocol must complete data and receipt, close writers, flush file
and directory metadata, establish the seal, then durably publish READY through the trusted
custodian. A crash at any earlier stage leaves restricted, inventoried NOT READY material.
No partial result or success marker is released on uncertainty. The auditor receives bounded
CPU/memory/time/open-file/output limits enforced outside itself; exact limit values and kill/child
cleanup behavior require future qualification. Audit output uses existing aggregate fixed labels,
bound externally to the receipt and candidate. STOP records contain only code, stage, candidate
identity and opaque evidence references, never rows, raw logs, credentials or sensitive paths.

### Future operator acceptance matrix and STOP evidence

Every row is mandatory before a real capture/audit; **all real-host rows are currently
UNVERIFIED/BLOCKED**. Synthetic PASS below cannot satisfy them. Evidence must identify exact
reviewed candidate SHA/tree, environment, time, observer and protected inventory reference.
Operator evidence is retained privately; Git receives only sanitized status and opaque references.

| Gate | Required acceptance evidence | STOP code / action if absent, denied or inconsistent |
| --- | --- | --- |
| Authority and rights | Current scoped owner decision; read, restricted copy, audit use and output separately covered; all copied tables classified; BB-132K resolved; finite invocation and revocation checked. | `authority.unverified`: do not open source or issue/consume a replacement grant. |
| Backup/recovery | Applicable daily/full-Finance backup coverage and successful isolated restore evidence, provider-tagged copy permission, destination inventory/retention and rollback plan. Dated STANDARD receipt is scoped evidence, not absence of backups or this route's acceptance. | `recovery.unverified`: no real copy/restore or deployment. |
| Identities and confinement | Operator/capture/custodian/auditor UID/GID/groups, UID mappings, namespace identities, all capability sets, no_new_privs, tool/native identity and protected ancestors/ACLs; actual denied mutation/escalation tests under auditor and ordinary-module principals on dedicated synthetic objects. | `isolation.unverified`: stop launch; same UID or mode-only demo never passes. |
| Correct source transaction | Independently bound source/storage identity and established pinned transaction; exact SQLite/VFS and same-transaction five-table content commitments reconciled to output; committed WAL retained and later/uncommitted changes excluded. | `source.unverified`: reject entire image, no best-effort subset or inferred population. |
| Source no-write | Effective read-only source route, sidecar/VFS no-write proof, before/after object inventories and tested denied DML/DDL/file/sidecar operations under the actual future capture principal. | `source.write-risk`: stop, no source repair, checkpoint or permission widening. |
| Seal and no-open-handle | Exclusive staging provenance; capture/descendant termination; no inherited/transferred writable handles or mappings; controlled link/mount/device inventory; read-only backing plus private read-only audit view throughout last read. | `seal.unverified`: keep NOT READY; one link or a handle scan is insufficient. |
| Substitution/race | Separate-principal denials of leaf/parent/ancestor replacement, symlink/hardlink creation and writable aliases; adversarial attempts at hash/open/read/release boundaries; stable object/receipt identity throughout. | `custody.changed`: discard result, retain protected STOP/inventory; no ABA/hash-only acceptance. |
| Receipt and release | Protected trust anchor, authenticated receipt, exact invocation/digest/length/transaction/schema/tool binding, expiry/revocation and replay rejection, durable READY and final recheck. | `receipt.invalid`: no report release or automatic retry/refund. |
| Resource/failure/cleanup | External finite limits, timeout/kill/child cleanup, interrupted capture/seal/audit cases, no success marker on partial state; independently reviewed disposition of restricted staging and revocation of temporary access. | `operation.incomplete`: keep inventory, cease audit, reconcile only under separate operator authority. |

Rollback here means fail closed and retain protected evidence; it never means rewriting source,
resetting a journal, restoring a real database, regenerating authority or silently deleting restricted
staging. Synthetic test cleanup can remove only its own temporary fixtures. No production operation
is started by this assignment, so no real service/data rollback is needed or attempted.

### Bounded synthetic OS evidence and validation

[Linux test harness](../../../../tests/deployment/test_finance_os_isolation.py) is discovered by
the existing CI backend Python step; no workflow or deployment artifact changes. It accepts no
paths and creates unique private temporary directories; all chmod/link/rename/write/delete actions
stay inside them. Root, effective/permitted/ambient capabilities or unsupported cleanup semantics
cause explicit BLOCKED/SKIP before fixtures. It reads only its own process capability metadata.
No child shell, account change, mount, user namespace, network or protected-object probe is used.

| Synthetic case | Observed result and qualification limit |
| --- | --- |
| File and directory mode bits | **PASS demonstration:** new write, create, unlink and rename fail with EACCES where actual permissions deny them. Same owner restores mode and writes, so this is not a principal seal. |
| Leaf/parent symlink | **PASS counterexample:** leaf O_NOFOLLOW fails; parent symlink is followed by a leaf-only check; O_DIRECTORY plus O_NOFOLLOW rejects that parent. Existing audit tests separately check library rejection. |
| Read-only ancestor swap | **PASS counterexample:** writable containing directory allows replacement of a read-only subtree with another synthetic image. |
| Hardlink and open writer | **PASS counterexamples:** aliases share inode/mode; removing the alias restores link count one but an already-open writer still changes bytes after chmod. |
| TOCTOU/ABA and no-write | **PASS counterexample:** deterministic rename interleaving substitutes B during separate path open, restores A, and original stream rehash still matches. Reader writes no bytes; final file set/content is unchanged. This is not an exhaustive race test or integrated library exploit. |
| Interruption and rollback | **PASS:** injected exception restores only fixture-directory permissions; fd-based cleanup removes partial fixture without following a symlink into a separate test-created witness; witness unchanged and no READY. |
| Separate principal / mount namespace | **2 explicit SKIP = UNVERIFIED/BLOCKED**, never PASS. No attempt to obtain privileges or establish a real seal. |

Local run: **8 PASS / 0 FAIL / 2 SKIP**, 2026-11-20. This qualifies ordinary Linux semantics
only. Release build, existing 94-case synthetic audit regression, formatting and documentation
results are recorded after execution below. Required GitHub CI including secrets and independent
exact-candidate risk-3 review remain PENDING until separately evidenced. Publication SHA/tree
are obtained from Git metadata and the Draft PR, avoiding a self-referential hash in this report.

Real Finance data/volume/backup/private rights/credentials and the dated 858 outcomes are **NOT
READ**; real OS enforcement, source-no-write capture and authenticated custody are **BLOCKED**.
Manifest/cohort stays unlocked; provider/model/broker/orders, ingress, production entrypoints,
service changes, activation, merge and deployment are outside scope. No inference of grants.

### Interruption and independently checked local validation

Codex authored the isolated synthetic Python harness and the threat/acceptance
matrix, but execution stopped at a ChatGPT/Codex usage limit before it could
publish the checkpoint. No authorization or runtime capability changed.
ChatGPT then independently inspected the small synthetic-only harness and
executed the existing local checks through the already-authorized non-root
Remote Desktop connection:

- Python deployment discovery: 275 tests run, 0 failures, 3 skips.
  This module alone: 8 pass, 0 fail, 2 explicit unverified skips.
- Solution Release build: PASS, 0 warnings and 0 errors.
- dotnet format verify-no-changes: PASS.
- Focused Finance offline audit: 94 pass, 0 failures, 0 skips.
- Documentation validator: PASS, 296 Markdown files and 57 backlog IDs.
- Required GitHub CI, including secrets: PENDING until exact draft commit runs.
- Independent level-3 exact-candidate review: PENDING.
- Actual privileged/separate-UID/mount/custody qualification: BLOCKED.
- No deployment, merge, source access, real data copy or real audit.

This is a review checkpoint, not a merge candidate or real operator acceptance.
