# BB-131A — Finance Research Learning contract


## Historical Qwen admission — implementation checkpoint, 2026-10-16

This additive path uses the existing recurring ledger, protected authority journal, local BRF1
composition, daily feature engine and deterministic evaluator. It does **not** convert imported
history into prospective Alpaca receipts or reuse synthetic/A/B/C authority. The reviewed operational
stop at `7faba26a391c2246ede7b73b8329b66b3cd7d290` is input evidence, not merged implementation.
[Checkpoint evidence and limitations](../../reports/features/finance/historical-qwen-integration-20261016.md).

`RecurringResearchAuthorization.Historical` is optional and absent fields retain previous
serialization/identities. Its version `wiki-historical-research-v1` admits only NASDAQ-WIKI AAPL,
revision `wiki-5713d7dccfa38f56`, candidate `wiki-eod-mirror-kmfranz-v1`, the pinned WIKI artifact
and existing Qwen3-1.7B-Q4_K_M artifact. Finance validates exactly748 complete source sessions,
2014-01-02–2016-12-19, stored rights/promotion/raw capability, OHLCV and acquisition chronology.
A commitment binds all748 rows, actual acquired times, candidate manifest/validation, revision
identity and price-capability audit. It must match the explicitly approved grant at admission,
execution and reopen. Missing or changed evidence fails closed. There is no new table/migration.

The single precommitted experiment uses the **first500 sessions, 2014-01-02–2015-12-24**;
the remaining248 rows are committed but not searched or evaluated. This retains the existing
64-call envelope and unchanged partition/embargo, fixed momentum20 proposal and5/10/20 sensitivity
trials. Raw-price/corporate-action/survivorship limitations are mandatory. Historical exposure is
**Unknown**, not a claim of untouched prospective holdout. Existing overlapping native evaluation
veto remains. No total-return, executable-profitability or future-validation claim is permitted.

Retrospective features/backtests use the existing simulated end-of-source-day clock. Actual 2026
acquisition/admission time remains distinct and bound to source provenance; it is never backdated.
`historical-development-v1` sends only development statistics and eight recent development samples
(raw close/momentum20) plus development-only hashes. Full source/model commitments, holdout rows,
selected outcomes and full feature identities remain Finance-owned. The model can propose only the
existing closed momentum hypothesis or decline. Its question/rationale are untrusted, persisted
separately from actual native evaluation. No model weights are trained.

One explicit I1/E1/T3/C64/W33s grant is the intended initial profile; it is **not issued by this
implementation**. Existing journal append/fsync-before-SQLite, reservations, uncertain-stop, replay,
expiry and no-refund/no-renewal rules are unchanged. No new program can replace the singleton.
A completed result remains readable; this version grants no subsequent inference. A future authorized
iteration must preserve exposure/history and cannot use a replacement database/program to renew quota.
The prospective scheduler ignores historical programs, including its next-due status.

### Explicit historical operator path (separate operational approval required)

These are bounded maintenance commands in `BigBrain.Finance.Hosting.dll`, never public endpoints.
Do not execute them against production merely because code is published.

1. Review/merge/deploy the exact candidate separately. Preserve AAPL collection/configuration.
2. `finance-historical-inspect` opens the existing Finance DB read-only and prints source commitments,
   coverage, product, fixed evaluation range and limitations; no grant, migration or model invocation.
3. Approve the finite historical grant (existing exported JSON schema plus Historical binding), its
   exact source checksum, owner rights/limitations, due/expiry UTC, pinned model and reasoner-profile
   identity. Prepare the profile using existing `finance-recurring-profile-identity`; no worker runs.
4. With MaintenancePaused=true and ResearchScheduler/EODHD/FRED/local-reasoner enable gates false,
   `finance-historical-enroll <private-grant-json> <sha256:raw-file-hash>` persists the explicit grant
   and emits the existing journal enrollment proposal. ObservationRuntime/Alpaca may remain active.
   Enrollment does not create the protected journal. It uses the existing Finance owner/startup schema
   behavior; do not treat it as read-only or as approval to apply unreviewed migrations.
5. Provision the existing protected journal under the independent root-custody/restore-exclusion
   contract below, only with separate owner approval. No auto-provisioning or old-grant reuse.
6. For the controlled maintenance process only, supply the approved external local profile and
   `Finance:RecurringLocalReasoner:Enabled=true`, preserving scheduler=false and maintenance=true.
   The trusted composition hashes the actual native worker model argument and matches the grant.
   Use the accepted CPU2,4GiB memory,swap0,pids32 containment, one concurrent invocation,30s maximum
   inference,250ms grace and2s reap (reserve33s). No old180s exception, cloud or download. The input
   remains subject to native3072-token/context4096/output512 bounds. Completion in30s is unproven.
7. `finance-historical-run` requires recovery/clock/disk, current authority and existing Sentinel
   resource governor; runs opportunity1 exactly once; persists proposal, runtime audit, input identity,
   budget and native result. SIGINT/SIGTERM cancel through the owned runtime. No retry or scheduler.
   Failure/timeout/decline can consume authority without an evaluation; it is not a successful analysis.
8. `finance-historical-result` opens read-only, reconstructs/validates source and native result
   commitments, and prints bounded hypothesis/result identities and reference validation outcome.
   It never reruns inference. Do not publish raw prompts/provider data/private configuration.

Grant issuance, journal custody/provisioning, exact model profile activation and deployment remain
separate owner decisions. No production grant, journal, profile, AAPL mapping or runtime is changed
by this checkpoint. Finance remains RESEARCH / 0 SEK / NONE.

## Recurring real-data implementation checkpoint — 2026-10-14

**REVIEW CHECKPOINT / model-free implementation / NOT DEPLOYED OR ACTIVATED.**
Implementation authority is the owner's approval of the
[reviewed contract ca2b0fd](https://github.com/T-bear/BigBrain/blob/ca2b0fddaf58b050bde3f43d36db12cfff47ab66/docs/architecture/finance/research-learning-contract.md).
That review branch was not merged. Baseline is `0851ee48a768348a001f1525f0b30c29584fd821`;
[implementation evidence and exact remaining release gates](../../reports/features/finance/recurring-research-implementation-20261014.md).
Older accepted synthetic contracts below retain their own semantics and spent authority.

### Explicit real admission and cumulative authority

`RecurringResearchAuthorization` freezes one owner decision, eligibility evidence, exact canonical
instrument/mapping, Alpaca historical1Day/IEX/raw source, reasoner identity, expiry, ordered disjoint
source-date cohorts and I/E/T/C/W budgets. There are no operational defaults or seeded grants.
The first implementation permits at most **two cohorts/invocations/evaluations**, one instrument,
300–500 sessions per cohort subject to the unchanged native partition minima, and the existing
1,000-receipt projection bound. Warmup is inside each cohort; no shared development/holdout windows.
All required `ResearchDatasetEligibilityPolicyV1` decisions must be Eligible, with Pass evidence for
rights, integrity, historical identity, price basis and corporate actions. Owner data-use risk acceptance
alone does not establish these scientific facts. No production authorization is created by tests.

Finance selects persisted daily receipts as of trusted cutoff K, verifies immutable lineage, selects
revisions known by K and requires exact instrument/mapping snapshots and complete calendar sessions.
Provider event/source date never becomes knowledge time. Every receipt must have been known **before
its following session's open**; late historical acquisition fails closed. Existing daily features,
momentum/v1 period20, fixed 5/10/20 trials, partition/falsification rules and native evaluator are reused.
Protected holdout begins after program registration. Earlier native evaluations with overlapping source
coverage veto another inference/evaluation. Missing sessions wait without spending; invalid provenance stops.

Migration99 adds empty program/exclusion tables to the existing Finance SQLite owner. One program
per database is supported; neither replacement IDs nor expiry/completion creates a fresh grant.
Original A/B/C records are not reset, converted or refunded. Atomic transactions reserve I/W and the
full input before inference, then E/three trials/64-call envelope before science. Unused reserved work
is not refunded; elapsed reasoner time is separate metadata. No retries. Exclusive program authority
prevents competing legacy/manual/campaign science; existing synchronous legacy builders retain their
own idempotence/concurrency semantics, with tracked in-flight leases blocking program registration.
Unclean legacy leases are not automatically erased. Cancellation never abandons an executing evaluator.

Native results remain in existing backtest/robustness stores. Program attempts bind input, receipts,
full plan, bounded untrusted proposal, response checksum and native result identities. Reopen verifies
commitments and native result/run checksums. Exact replay does not invoke a reasoner or evaluator.
Reserved/uncertain work stops, including a crash after native persistence; automatic uncertain-result
finalization is not provided. Completed positive/negative evaluations advance by the frozen schedule,
never by holdout success. Decline/rejection/failure stops and remains retained. No catch-up burst.

### Model boundary and existing scheduler

`Finance:ResearchScheduler:RecurringProgramMode` is default false and mutually exclusive with the
legacy EODHD readiness path. It creates no program. Existing recovery, maintenance, governor,
opportunity journal and worker remain the hosting path. Internal trusted composition supplies
`IResearchReasoner.ReasonAsync` and the actual bounded runtime-plus-cleanup duration; API still has
no assembly reference to Brain. Absent composition defers without spending. No production fake,
public mutation API, new scheduler or automatic grant provisioning is registered.

The model receives development-only hashes/session counts and all preceding sanitized outcomes with
precommitted history-release times and **fixed reference validation** excess return. Actual evaluator
completion time remains private: prior history is released at the next cohort opportunity, and
completion after that boundary fails closed instead of exposing data-dependent timing. Full receipt/result IDs, selection,
robustness and holdout values/classifications stay private. Full evidence identity is separate from
visible identity. Recurring history has its own versioned optional field; existing finite/synthetic
serialization stays unchanged when it is absent. The native prompt now describes the supplied
recurring history without asserting synthetic/empty history. Grammar, vocabulary and isolation are unchanged.
Test fixtures have a durable fixture origin and explicit fixture composition; they cannot pass real-origin
admission or become operational evidence. This is inference integration, not model-weight training.

### Independent recurring authority — final release implementation, 2026-10-15

The former unconditional production stop is replaced by a **required protected journal match**.
Missing protection or disagreement still returns `finance.research.recurring.restoreReconciliationRequired`;
operations reports AttentionRequired. Default startup creates neither journal nor grant. The existing
Finance SQLite owner remains the only research/result/history store. The journal contains only version,
program/grant/state commitments, cumulative I/E/T/C/W counters, sequence and previous/checksum links.
It is bounded to64 records/128KiB and is not exposed to the reasoner.

Before each authority mutation, Finance holds its existing SQLite immediate transaction, obtains a
nonblocking exclusive journal flock, checks the exact previous state and appends/fsyncs the next state
**before** SQLite commit. Audits and terminal outcomes follow the same rule. Matching state permits
ordinary restart; durable reservations still prevent replay of interrupted work. Journal-ahead/DB-ahead,
missing, malformed, partial or contradictory evidence stops. A crash after journal flush but before
SQLite commit may burn work that never ran: no rollback, truncation, repair, refund or automatic retry.
The journal is checked before both actual inference and scientific evaluation. Read/completed replay
can return stored evidence without creating authority. Native result persistence and holdout isolation
remain unchanged. No extra database migration is needed beyond the branch's migration99.

**Protection model:** Linux64/local ext4, root-owned regular single-link journal with the kernel
append-only attribute set; root-owned non-group/world-writable ancestors, no symlink traversal, API
nonroot with zero effective/permitted/ambient capabilities and NoNewPrivs=1. Descriptor-relative
openat/statx and fstatfs enforce these local properties on every access; writing uses kernel O_APPEND.
An ordinary writable file, unsupported filesystem or merely asserted protection fails closed.
The path must be outside configured Finance/lifecycle database directories. This alone is not proof of
backup independence: trusted host administrators must dedicate this authority location and exclude it
from **all** Finance/lifecycle/config/volume restore sets. The API cannot truncate, unlink, replace,
chmod or clear the append-only flag. Administrators must not restore a prior authority prefix, create
alternate copies for the same grant, or grant API raw-device/mount/immutable privileges.

The reviewed operating model covers Finance/lifecycle rollback with the protected witness retained,
normal restart and process/power loss with filesystem fsync guarantees. It does **not** protect against
malicious privileged host administration, full-host/disk rollback including the witness, storage that
lies about flush durability, or loss of the witness. Those events require an external operational stop
and independent manual reconciliation; old grants remain unusable if consumption/exposure cannot be
proved. Same-disk placement is not disaster recovery. No silent fallback accepts an uncertain witness.
Actual host provisioning/protection must be verified before activation; this checkpoint performs none.

No reconciliation/clear/reset endpoint exists. Reconciliation can restore an independently verified
current Finance state matching the retained journal, preserving all spending/reservations/exposure.
It cannot bless an older balance or rewrite the journal to match a backup. A mismatched or lost witness
cannot be recreated by startup or enrollment. If trustworthy history is unavailable, retire the old
authority; a separately reviewed new grant must also avoid previously exposed/unknown holdouts.

`BigBrain.Finance.Hosting` is a first-party composition entry for the **same API process**, not another
service, scheduler or process runner. It references API and Brain; neither references the other or the
composition root. It calls the existing API startup and registers only the narrow reasoner delegate.
The API Docker build publishes this entry point and its dependencies; no image was deployed. Existing
API maintenance commands and compiled build identity remain available. No reflection/plugin loading.

External trusted startup configuration is `Finance:RecurringLocalReasoner:Enabled` (default false)
and `Profile`. No actual profile or approved resource value is shipped. Profile requires bounded
OwnerDecision and ArtifactAndIsolationEvidence references, UTC ApprovedAtUtc/ExpiresAtUtc, and Runtime:
WorkerExecutable, WorkerArguments, CoordinationDirectory, InvocationTimeout, GracePeriod, ReapTimeout.
These are private first-party deployment inputs, never model/request data. Runtime values are explicit;
missing values fail closed. Existing ordinary LocalReasonerRuntime bounds apply, including at most30s
invocation. The old180s acceptance exception is inaccessible from this host. A different limit needs a
separate explicit profile/policy decision; this correction grants no new real-model allowance.

The full profile checksum is the binding's reasoner identity and must match the explicitly frozen grant.
Changing a profile does not reinterpret an old grant. Artifact and OS-isolation evidence must establish
the pinned native worker/libllama/Qwen, read-only artifacts and pre-provisioned cgroup before any future
activation; the reference itself is an owner attestation, not an automatic proof of those host facts.
No downloads, model choice, arbitrary command endpoint, grant creator or default production fake.

Composition owns runtime disposal through API shutdown/failure. Single-flight, pidfd ownership,
cancellation, kill/reap, bounded BRF1 I/O and inherited-environment clearing stay in LocalReasonerRuntime.
Profile expiry and clock regression fail closed. Started/response/completed fixed audit metadata is
bounded to four records in the already reserved Finance attempt, checksummed and reopened with it;
no new audit database or raw prompt/reply logging. Audit failure uses existing runtime failure/disable
behavior. This metadata remains private and never enters the model-visible history.

### Initial release configuration and operator sequence

Only the explicitly approved initial enrollment is manual. Subsequent eligible cohorts are scheduled
by the existing worker without a per-experiment operator action. The following are future operational
steps, **not authorization to execute them during implementation**:

1. Approve exact release SHA, scientifically eligible receipt source/mapping/rights/price-basis and
   corporate-action evidence, ordered disjoint cohort dates, grant expiry and owner decision. Each
   cohort needs300–500 sessions and protected holdout beginning after enrollment. No existing small
   AAPL observation set is claimed sufficient; late imported data cannot become timely evidence.
2. Approve the existing pinned native worker/libllama/Qwen profile and OS/cgroup resource isolation.
   Suggested bounded starting envelope (not a grant): **I2/E2/T6/C128, W66s, CompletionSeconds33**,
   invocation30s + grace250ms + reap2s =32.25s; concurrency1, retries0/refunds0. Scheduler polling60min,
   with cohort-specific immutable DueAtUtc, no catch-up. One cohort may instead use I1/E1/T3/C64/W33s.
   Ordinary runtime still caps inference at30s; no claim the real model will finish within that bound.
   A longer profile requires separate policy approval; old180s experimental authorization is not reused.
3. With API research/observation/provider workers stopped/disabled and MaintenancePaused=true, prepare
   the existing trusted external profile. `dotnet BigBrain.Finance.Hosting.dll finance-recurring-profile-identity`
   validates it and prints only the identity; it starts no worker. Bind that exact identity into the
   private authorization JSON. The JSON uses the existing Finance observation string-ID converters
   (`ExportRecurringAuthorization`/`ParseRecurringAuthorization`), not a parallel grant schema.
4. Explicitly run `dotnet BigBrain.Finance.Hosting.dll finance-recurring-enroll <private-grant-json> <sha256:raw-file-hash>`.
   It checks exact bounded JSON, maintenance/disabled gates, recovery/clock/store and existing ledger,
   then persists one program and prints one sanitized enrollment record. Save that output privately.
   It does not create/repair a witness or call a provider/model. Existing spent/uncertain/replacement
   programs refuse enrollment. Identical pristine enrollment is idempotent. This command can apply
   accepted startup migrations; it is not a read-only diagnostic or an automated startup command.
5. Trusted host root provisions **once** a dedicated authority directory outside all recovery sets:
   ancestors root-owned without group/other write, directory root:API-group0750, journal root:API-group0660.
   Seed with the exact enrollment record including its final newline, verify its `checksum`, flush file
   and directory metadata (`sync -f <journal>` on local ext4), set `chattr +a <journal>` and sync again.
   No runtime/root helper performs this automatically. Retain authority and initial enrollment commitments;
   do not rotate, replace, truncate, restore, clone as active, or clean this journal with Finance backups.
6. Through the existing private Compose override/.env mechanism, bind only that protected directory
   into the API; no Docker socket, privileged mode, raw devices or generic process authority. Use
   `cap_drop: [ALL]`, `security_opt: [no-new-privileges:true]`, existing nonroot API user and appropriate
   journal group. Set `Finance__RecurringAuthority__JournalPath`, `__EnrollmentChecksum` (sha256: prefix)
   and `__RestoreBoundaryDecision` under the same prefix. The decision attests the actual root custody
   and restore exclusions, not just a path. Configuration alone cannot establish those facts.
   The private override also passes the existing `Finance__RecurringLocalReasoner__Profile__...`
   fields/WorkerArguments indexes and scheduler options; no values/universe/grants are shipped as defaults.
7. Under the exact deployed API UID/capabilities/mounts, with all gates still disabled and maintenance
   paused, run `dotnet BigBrain.Finance.Hosting.dll finance-recurring-authority-check`. It opens existing
   SQLite read-only without constructors/migration and verifies native history plus protected journal.
   Require `RecurringAuthorityMatched`; no bypass if unavailable. This is the required real-host
   protection preflight, separate from the model-free journal protocol tests.
8. Only after separate deployment/inference/data approvals enable `Finance:RecurringLocalReasoner:Enabled`,
   `Finance:ResearchScheduler:Enabled` and `Finance:ResearchScheduler:RecurringProgramMode`; release
   MaintenancePaused after recovery is ready. Keep competing legacy research disabled. Market acquisition
   remains separately authorized under the existing observation runtime. Missing eligible data waits;
   expired/exhausted/uncertain authority stops. No automatic scientific grant creation or renewal.

Before **any** restore keep scheduler/model disabled and maintenance paused in the independent deployment
configuration. Never restore the witness/config custody together with Finance. After restore require the
same read-only authority check and manual consumption/exposure reconciliation before enabling anything.
A clean lifecycle flag cannot clear a mismatch. Full-host recovery outside this model must not automatically
restart research; absent independent current evidence, old authority stays retired.

Finance remains **RESEARCH / 0 SEK / NONE**. Collection, SHADOW, model provisioning and research grants
remain independently controlled; no trading, automatic promotion or capital side effect is introduced.

## BB-132C accepted integration — 2026-10-02

**ACCEPTED / MERGED / CI VERIFIED.** [Exact approval, merge and CI evidence](../../reports/features/finance/bb-132c-local-sequence-compatibility-20261001.md#accepted-publication--2026-10-02).
The accepted local runtime now composes its bounded completion deadline with the finite session;
the native prompt describes exactly the existing Finance-owned sanitized history. Scientific
projection/admission/grants/engine/isolation remain unchanged. Real N evaluated once, native evidence
persisted/reopened, real history-bearing N+1 rejected BudgetExceeded;2/2 inference allowance consumed.
No additional inference during publication. ADR0038/0039/0040 remain Accepted. No new architecture
decision, deployment or runtime endpoint. Finance RESEARCH / 0 SEK / NONE; BB-132D NOT STARTED /
NOT AUTHORIZED. Earlier B-era C-not-started statements below describe their historical date.

## BB-132B — finite model-free research session, 2026-10-01

**ACCEPTED / MERGED / CI VERIFIED.**
Baseline `2caf2eb898acaafa759c6287d02442a90bb666db`; same branch `bb-132b/persistent-research-loop`.
The owner/architect resolved the first review's intentional v1 boundary: a separate explicitly
created finite grant permits two reasoner invocations and at most one evaluation (N only),
three trials/64 underlying calls, concurrency one and no retries/refunds.
[Ownership, exact publication identity, projection policy and evidence](../../reports/features/finance/bb-132b-research-loop-boundary-20261001.md).

Finance migration95 adds an empty session table in the existing database; it does not seed a grant
or convert old v1 state. Explicit creation requires an uninitialized v1 scientific slot. Existing
enrolled/spent v1 rows cannot acquire this grant. First invocation reservations commit together;
existing admission/evaluator/result stores and scientific ledger remain authoritative.
N+1 receives only a versioned, bounded reference-validation outcome and sanitized operational
history. Protected holdout/selection/classifications/IDs are excluded. A second proposal fails
BudgetExceeded; NoUsefulProposal is normal. Failure/uncertainty never refunds or permits retry.
Focused model-free B/C/E/F/B-session suite: **227 PASS**. Full API906 PASS/7 model skips;
Sentinel32 PASS; restore/Release build/format/docs/scope/secrets PASS. No model was invoked.

This is an internal synthetic, finite API: no runtime registration/public endpoint, daemon or
real-model integration. General adaptive history/overlap, real-market eligibility and runtime
validation remain future work. No Qwen, tuning/download/cloud/GPU/provider/trading/deployment/BB-132C.
Finance **RESEARCH / 0 SEK / NONE**. Earlier A-era B-not-authorized statements are dated history;
this authorization is model-free only and does not revive consumed inference allowances.

### Finite session projection and replay policy

The new `finance-finite-research-session-v1` is an explicit trusted Finance grant, not a version
upgrade of existing v1 rows. It requires an unused/uninitialized v1 scientific slot; enrollment,
failed, completed and uncertain old programs are ineligible. The singleton scope cannot be
replaced or renamed into a fresh session. Two invocations are a ceiling; evaluation exists only
at N, and is never transferred to N+1 even if N declines. C's one-invocation/evaluation semantics
and native result ownership remain unchanged. Schema95 creates an empty additive session table.

First invocation and original scientific invocation reservation commit atomically before reasoning.
Scientific admission still reserves trials/exposure before the unchanged engine. A session's
Reserved/Failed/Indeterminate state is never replay permission or a refund. Pending/failed work
blocks progression. Parsed negative outcomes are retained; completed N may supply one bounded
history item to N+1. Replaying an exact iteration returns its recorded outcome without invoking
a reasoner or engine. Unknown or mutated history/source references fail closed.

`finance-finite-history-v1` is a fixed-size Finance projection. It exposes only previous iteration1,
its bounded admission/decline/rejection category, UTC availability and as-of cutoff, the native
fixed-reference momentum20 validation excess return (fraction, nullable), and remaining evaluation
count0. No full robustness object/classification, selected metric, score, run/result/proposal/full-plan
identity, raw rows/features or holdout proxy is authorized. The reference metric is preselection;
it is not permission to expose the best selected variant or any final verdict.

Knowledge must be available by trusted creation/reservation time. N retains the accepted initial
development cutoff; N+1 additionally requires prior completion availability <= research cutoff <=
reservation time. Do not conflate development-window time with later operational knowledge time.
The entire permitted prior outcome is included regardless of sign. Native IDs/checksums remain
private lineage, and projection eligibility is explicit, never inferred from arbitrary SQLite rows.
Canonical history and input hashes bind the exact frozen projection; reopen rederives it against
validated immutable native evidence. Noninterference holds for protected content changes under
fixed cohort/plan and trusted operational timestamps, as demonstrated by the actual evaluator test.
General overlap/adaptive history and timing side channels are not claimed solved.

Model-free callers bind IResearchReasoner.ReasonAsync to Finance's narrow internal delegate. No
public endpoint/DI/runtime/native prompt change exists. Real-model invocation of this new history
requires a separate authorization and compatibility review; no A allowance is renewed.

Owner explicitly approved independently reviewed candidate `8df4541fdcd300b0d334d519c221e4189dd32084`.
Merged unchanged as `b2da9e7ee46b2722fc9f696fbdfe9097afa0caab`; candidate/merge tree identical.
[Accepted identities and merge CI36918069527](../../reports/features/finance/bb-132b-research-loop-boundary-20261001.md#accepted-publication--2026-10-01):
backend restore/format/Release build/tests, frontend install/format/tests/build, documentation and
Gitleaks jobs/actual steps SUCCESS. Final docs reconciliation is verified by its own exact-SHA CI;
its identity is resolved in the report. No implementation or model invocation during publication.
**BB-132C NOT STARTED / NOT AUTHORIZED.** Next: STOP for independent post-merge review/planning.

## BB-131E local reasoner contract — 2026-09-28

**ACCEPTED / MERGED / CI VERIFIED.**
Owner-authorized from `e2f9cd761d0c8ed7569939b9ada908e6de37cf3f` on
`bb-131e/local-reasoner-contract`. [Implementation and evidence](../../reports/features/finance/bb-131e-local-reasoner-contract-20260928.md).
A minimal Brain contract library references Finance's existing development input and closed reply.
Finance owns the shared B parser and unchanged scientific admission; C still reserves invocation,
trials/exposure and execution. Only test code implements the reasoner and orchestration.
No API registration/reference to Brain, model, inference, external SDK/provider/export or deployment.
Finance **RESEARCH / 0 SEK / NONE**. ADR 0038/0039 remain Accepted; BB-130 closed; A/B/C/D accepted.
E is accepted and merged as a contract foundation, not runtime-authorized. Auth/audit/transport/resource/kill-switch isolation,
model provenance/capacity and first-invocation review remain prerequisites. No paid AI dependency
or cloud fallback. No subsequent checkpoint is authorized. Older D authorization statements below
are dated history and do not override this separately authorized E scope.


## BB-131D local-first design addendum — 2026-09-28

**ACCEPTED / MERGED / CI VERIFIED.**
[Local-first reasoner and security contract](local-first-reasoner-boundary.md) separates on-host use from optional external export.
Finance owns projections/science/admission/ledger/risk; Brain is orchestration only, models untrusted.
No paid cloud prerequisite or automatic fallback. B/C's synthetic protocol and accepted ADR 0038
remain unchanged; generalized history projection and real-model/auth/resource controls remain future.
Only D design is authorized. A/B/C are accepted; their earlier next-step wording below is historical.
No implementation, inference, model/provider installation or deployment. BB-131E NOT STARTED / NOT AUTHORIZED.
Finance RESEARCH / 0 SEK / NONE.

## BB-131C persistent governance addendum — 2026-09-22

**ACCEPTED / MERGED / CI VERIFIED.** Separately owner-authorized from `cb520994f3ae4222bab1ba31d8cc683abbc28991`.
[Implementation, ownership and limitations](../../reports/features/finance/bb-131c-persistent-learning-ledger-20260922.md). Existing EodhdMarketMemory and FinanceSchemaMigrator
own additive migration 94 and one bounded synthetic ledger; no second database or scientific engine.
The fixed B protocol reserves invocation, complete plan, three trials and exposure durably before
computation. Immediate SQLite transactions protect concurrent requests; failures retain spent authority.
Existing result stores remain scientific truth; ledger binds checked IDs/checksums and native verdicts.
No production reasoner, endpoint, new provider, autonomous loop, statistical or execution claim.
Future real-model access still needs auth/scoped authorization, audit, resource/egress limits,
export/retention rights and security review. Unknown real-data cohort relationships fail closed.
ADR 0038 stays Accepted. A/B evidence below is historical; no accepted decision is rewritten.
Finance **RESEARCH / 0 SEK / NONE**; no deployment. BB-131D NOT STARTED / NOT AUTHORIZED.


- Date: 2026-09-21
- Status: **ACCEPTED / MERGED / CI VERIFIED — architecture only**
- Baseline: `7747b8f715204dc3ccf753f170238b40238d1b11`
- Branch: `bb-131a/research-learning-architecture`
- Decision: [ADR 0038 — Accepted](../../adr/0038-finance-research-learning-authority-contract.md)
- Assessment/recovery/verification: [BB-131A report](../../reports/features/finance/bb-131a-research-learning-architecture-20260921.md)

BB-130 remains COMPLETE / EXIT APPROVED. BB-131A authorizes analysis and documentation,
not implementation. Owner/architect accepted the exact reviewed architecture candidate;
ADR 0038 is now Accepted. The original proposed contract/slice wording below describes future
implementation, not shipped capability. Existing accepted ADRs remain authoritative.
Finance remains **RESEARCH / 0 SEK / NONE**.

## BB-131B implementation addendum — 2026-09-22

Separately owner-authorized B is **ACCEPTED / MERGED / CI VERIFIED**.
[Implementation evidence](../../reports/features/finance/bb-131b-synthetic-contract-replay-proof-20260922.md) maps the minimal typed
boundary to this accepted decision. Two additive module files provide immutable synthetic scope,
closed JSON admission, full new identities and existing result references. Reasoner/orchestration,
cohort ledger, commitment/result manifests and SQLite replay are test-only. No accepted ADR decision
or existing engine changes. The initial projection supports only empty pre-invocation history;
no general adaptive projection or production ledger is claimed. Preflight conservatively counts
all engine calls (including duplicates), rejecting excessive plans without shrinking them.
Replay is reproduction of consumed engineering evidence, not fresh statistical confirmation.
Future real-data/auth/rights/retention/cross-cohort requirements below remain binding.
Historical A wording describes architecture acceptance, not a prohibition on the separately authorized B.
Finance RESEARCH / 0 SEK / NONE; no deployment or BB-131C authority.

## Objective and Definition of Done

Define the smallest extension through which a research reasoner can suggest a bounded
experiment, and Finance can independently reject it or produce reproducible research evidence.
**AI MAY PROPOSE. DATA MUST PROVE. RISK MAY VETO.**

A is complete for review when existing boundaries, structured contracts, adaptive-testing
controls, holdout protection, lineage, security prerequisites and a fixture-first B slice are
specified with source evidence and limitations. No provider, production capability, schema,
experiment, changed historical result or deployment is part of A. BB-131 as a whole is not
complete, and this proposal does not authorize B or a provider phase.

## Current capability map

Paths below are relative to the repository. REUSE AS-IS means reuse the named responsibility
and accepted semantics, not that its present API already accepts untrusted proposals. EXTEND
identifies a future integration gap; no extension is implemented in A.

| Capability | Exact existing boundary | Classification and limit |
| --- | --- | --- |
| Canonical revision identity | `src/BigBrain.Modules/Finance/CanonicalMarketData.cs`, `DatasetRevisionAssembly.cs`; `src/BigBrain.Api/Finance/CanonicalDatasetRevisionIdentityV2.cs` | REUSE AS-IS: exact source/product/revision/checksum, causal knowledge time. V2 applies to future promotions; never recompute legacy IDs. |
| Noncanonical owner research | `FinanceResearchDatasets.cs`: `FinanceResearchDatasetItem`, `ResearchCatalog`, `RunBoundedResearchBacktest`; module `ResearchDatasetLineage` | REUSE AS-IS for purpose-specific limited evidence; distinct from canonical eligibility. Current maintenance runner is buy-and-hold with `research-no-features-v1`, not an arbitrary feature/strategy executor. |
| Eligibility and entitlement | `ResearchDatasetEligibilityPolicyV1`, `ResearchDatasetFacts`, `ResearchCapabilityDecision`; `MarketDataEntitlements.cs`; ADR 0021/0022/0027 | REUSE AS-IS. EXTEND only the admission projection that binds the exact purpose and rights decision. EligibleWithLimitations is not globally eligible; private research permission is not external AI export permission. |
| Causal features | `DailyFeatureEngine.cs`, immutable feature revisions, ADR 0023; `FinanceResearchFeatureLibrary` | REUSE AS-IS. Registered context features are not necessarily executable strategy inputs. Unsupported feature/data combinations stop. |
| Strategy and parameter identity | `IResearchBacktestStrategy`, `StrategyIdentity`; `BuyAndHoldResearchStrategy`, `MomentumResearchStrategy`, `SmaCrossoverResearchStrategy` | REUSE AS-IS compiled strategies only. EXTEND a typed admission allowlist; no code/DSL/plugins. Actual parameter keys are `period`, `fastPeriod`, `slowPeriod`. |
| Backtest and cost/fill/sizing | `DeterministicBacktestEngine`, `BacktestRunConfiguration`, `BacktestCostModel`, `BacktestFillModel` | REUSE AS-IS BB-123 v2. Pin exact configuration and checksum, not merely a model label. Simulation capital is hypothetical, never spendable capital. |
| OOS/holdout | `AntiOverfittingPolicy`, `ResearchDataPartition`, `SelectionGovernanceEvidence`, `DeterministicRobustnessEvaluator`; `FinanceRobustnessStore.cs` | REUSE AS-IS numerical/selection rules; EXTEND admission, cross-proposal exposure accounting and commitment before execution. Existing prior-use lookup covers exact strategy/revision/feature/date scope, not every overlapping future experiment. |
| Robustness | `EvaluationPlan`, `RobustnessEvaluationResult`, parameter grid/cost ladder/walk-forward | REUSE AS-IS. Existing evaluator does both selection and holdout in one call; no development-only switch. Do not expose its full result to an adaptive reasoner on an active holdout. |
| Hypotheses and integrity | `AutonomousResearch.cs`: `ResearchHypothesis`, `ResearchComplexity`, `ResearchIntegrityVerdict`, `FinanceResearchContracts.Assess` | REUSE vocabulary; EXTEND proposal/falsification/provenance envelope. Existing hypothesis IDs are generated by fixed internal logic, not accepted from AI. DSR/PBO remain NotEvaluable. |
| Autonomous orchestration | `FinanceAutonomousResearch.cs`: `RunAutonomousResearch`, `SelectCurrentResearchEvaluations`, `research_run_experiments` | REUSE idempotency, single-flight and negative-history principles. EXTEND only after separate authorization: runner currently builds/selects fixed current evidence and cannot execute arbitrary pinned proposals. Do not route a proposal into it and claim it was honored. |
| Campaigns | `FinanceResearchCampaigns.cs`: `ResearchCampaignDefinition`, `ResearchCampaignVariant`, limits, scorecard, disposition | REUSE population/lineage/bounded-ledger concepts; EXTEND for proposal admission. Actual BB-129A emits categorical ineligible/insufficient results with no backtest reference; it does not execute OOS research. Campaign `lookback`/`fast`/`slow` labels are not engine parameter keys; no implicit alias mapping. |
| Risk | `FinanceRiskEngine.cs`: `FinanceRiskProposal`, `FinanceRiskEvaluation`, server-owned policy and durable halts; ADR 0030 | REUSE AS-IS for applicable prospective RESEARCH proposals. It needs current EOD/cadence/health/exposure evidence, not just historical returns. No fabricated risk ALLOW for a historical learning iteration. |
| Result vocabulary | `RobustnessVerdict`, `SelectionGovernanceOutcome`, `ResearchExperimentVerdict`, `ResearchCampaignDisposition`, `FinanceRiskVerdict` | REUSE AS-IS as typed source results. No new universal profitability/robustness score. Scientific result and operational outcome stay separate. |
| Immutable storage/replay | `FinanceBacktestPersistence.PersistBacktest`, `FinanceBacktestStore.cs`, `FinanceRobustnessStore.cs`; existing Finance SQLite | REUSE AS-IS result writers/readers. EXTEND future learning envelope/admission journal only after storage design approval. Readers deserialize historical JSON; they are not universal integrity validators. |
| API and Web | `FinanceEndpoints.cs`, reader interfaces; `src/BigBrain.Web/src/finance/FinanceObservation.tsx`, `useFinanceObservation.ts`, `useFinanceBacktestDetails.ts`, `financeSnapshotCache.ts` | EXTEND later with a narrow authorized evidence projection. Current human-facing responses include holdout/selection details; do not give a reasoner unrestricted API access. Browser stale cache is display-only, never admission evidence. Preserve BB-128B/C behavior. |
| Scheduler/resource governor/operations | `FinanceResearchScheduler.cs`, `FinanceResearchResourceGovernor.cs`, `FinanceResearchOperations.cs`; ADR 0034–0036 | NOT NEEDED FOR BB-131A or fixture B. Existing bounded scheduling/recovery may be reused later. Operational Allow is not scientific/risk authority; manual research is outside the scheduled governor. |
| Model adapters/educational inputs | Brain's logical orchestration responsibility in ARCHITECTURE; module's planned educational-source role | FUTURE. No new Brain project/service/provider/SDK/source-ingestion subsystem is needed now. |
| Execution authority | Proposed ADR 0017–0020 and master-roadmap promotion gates | FUTURE and outside BB-131. No reachable broker/order/PAPER/LIVE/AUTO/capital path. |

### Findings that constrain the design

BB-123 preserves exact next-session fills, hypothetical spread/slippage/fees and versioned
identities; daily bars do not prove intraday liquidity or correct corporate actions.
BB-124 uses 60/20/20 with 50-session embargoes, minimum 126/40/40 sessions and validation-only
selection. Its 75% positive validation-excess/family-median control is an explicit heuristic,
not a multiple-testing significance guarantee. BB-092's family-attempt visibility and integrity
check do not impose a cumulative adaptive-search budget; DSR/PBO checks remain NotEvaluable.

BB-129A's accepted 24 attempts (15 ineligible, nine insufficient, zero robust candidates)
are not numerical OOS results. Its eight-dataset/three-variant/24-attempt limits are different
units from a robustness evaluation's underlying backtest-run budget. BB-127 research revisions
lack the accepted immutable feature/selection chain required by that campaign. Do not promote
or synthesize this missing chain in BB-131A/B.

`BuildRobustnessEvaluations` returns same-version evidence and detects prior evaluated holdout
for exact strategy/feature/market/date scope. A new model, family name, overlapping revision,
universe or date choice must not become a way for a future learner to claim unseen evidence.
This is an extension prerequisite, not evidence that today's fixed runner was exploited.

## Ownership and authority

```text
Finance-owned authorized evidence projection (no protected holdout)
  -> reasoner port: one bounded structured suggestion, or NoUsefulProposal
  -> Finance admission: schema + rights + lineage + policies + budget + duplicate/holdout checks
  -> committed, frozen exact execution plan
  -> existing deterministic backtest / robustness / applicable risk evaluation
  -> immutable source results + learning lineage references
  -> permitted summary for a later, separately admitted research question
```

Reasoner may identify unanswered questions, missing evidence, bounded variants and falsification
criteria, or decline to propose. Its explanations are untrusted commentary. It cannot create
eligibility, set a verdict, invent an evidence reference, rewrite history, change policy,
choose hidden data after outcomes, tune calculations, unhalt risk or authorize an external action.
No textual instruction, tool-call request or confidence score confers authority.

Finance owns contracts, admission, calculation and persistence. Future Brain code owns only
model-neutral reasoning orchestration and model adapters, and calls the normal authorized
Finance application/API boundary. It receives no database, file-path, shell, generic HTTP,
provider, scheduler-control or execution capability. Web continues to render server truth.
No parallel engine, database, service, general experiment platform or generic plugin layer.

## Proposed versioned contracts

Proposed contract identifier: `finance-research-learning-v1`. This is a documentation contract,
not a shipped type or accepted wire schema. Public HTTP, if ever authorized, remains `/api/v1`
and Problem Details; A/B adds no route. Unknown version, property, enum or capability rejects.

| Concept | Minimal structured content and ownership |
| --- | --- |
| Research question | Embedded bounded question text (max 1,024 characters), target/horizon capability ID and optional parent hypothesis reference. No separate question table or engine. Text cannot alter the structured target. |
| Existing ResearchHypothesis | Reuse existing feature IDs, target/horizon, universe, exact market/feature references and family/fingerprint concepts. Finance constructs the trusted record only after validation; do not deserialize a model response directly into it. Existing v1 IDs/results remain unchanged. |
| ReasonerInput | Contract/projection version, immutable input checksum, opaque authorized scope handle, knowledge cutoff, permitted capability choices, bounded evidence summaries and complete family-attempt/exposure/budget summary. Generated by Finance, not the model. |
| ProposalDraft | Discriminated `Proposal` or `NoUsefulProposal`; input checksum, scope handle, structured target ID, bounded rationale, parent reference, 1–3 declared variants and 1–8 falsification criteria. No mode, risk verdict, execution command, new policy or source URL field. |
| ExperimentVariant | Supported StrategyIdentity plus complete exact numeric parameter map and an offered dataset/feature binding handle. No expressions, code, arbitrary ranges, implicit defaults or model-selected cost/holdout/risk thresholds. All declared variants execute or acquire an explicit non-executed outcome. |
| EvidenceReference | Tagged kind (dataset revision, feature revision, backtest, robustness, research experiment, campaign, risk evaluation), immutable ID, source contract/version and stored checksum where available. Finance verifies scope, existence, lineage and rights. Never arbitrary path/URL/SQL. If no native checksum exists, use a versioned projection checksum; do not pretend one is a native identity. |
| FalsificationCriterion | Allowlisted metric/gate ID, development/validation phase, comparison enum, decimal threshold, unit and required sample-policy reference. Missing measurement gives NotEvaluable, never zero. Conditions are conjunctive and can only strengthen immutable mandatory gates; no user-defined expression/JSONPath. Selection criterion and tie-break remain server-owned. |
| CommittedProposal | Finance-generated immutable proposal ID/hash, exact normalized draft, exact input/evidence references, approved protocol/family/holdout-cohort IDs, complete resolved execution plan and budget reservation. Includes policy versions AND immutable policy snapshots/checksums where no canonical policy entity exists. |
| LearningResult | Operational state/reasons, proposal/iteration/input references, actual attempted variant/run/evaluation IDs and checksums, exact original typed scientific classifications and limitations, optional matching risk evidence, missing-evidence reasons. Optional AI explanation is separately labelled and never overwrites a source verdict. |

Proposed wire limits: input/output each at most 64 KiB UTF-8, JSON depth at most eight,
maximum 32 evidence references, 16 bounded summaries and 2,048 characters per rationale;
parameter count at most two for the first allowlist. Reject duplicates in object keys/arrays
where set semantics apply; reject nonfinite numbers, null required fields and silent truncation.
If complete governance history cannot fit, provide a deterministic aggregate with counts and a
history digest, plus bounded ordered examples; never present a selected-success-only history.
The trusted validator reads the complete ledger, not this condensed reasoner view.

The draft union has two closed shapes. `Proposal` requires version, discriminator,
inputChecksum, scopeHandle, targetId, question, rationale, parentHypothesisRef (explicitly null
for a root), variants and falsificationCriteria. `NoUsefulProposal` permits only version,
discriminator, inputChecksum, scopeHandle, reasonCode (`InsufficientEvidence` or
`NoSupportedQuestion`) and bounded explanation; variant/execution fields are forbidden.
References and IDs are opaque bounded strings resolved from the offered scope; arbitrary new IDs
are not capabilities. Strategy parameters are decimal values, not numeric expressions or strings.
Criterion comparisons are `LessThanOrEqual`, `GreaterThanOrEqual` or `Equal`, constrained by the
chosen metric's offered policy; units and phase must match that metric exactly. Initial policy
accepts only the specified validation-excess criterion. This contract must become explicit
closed types/schema and negative tests in B, not permissive dynamic dictionaries at the boundary.

### Exact execution binding

A committed plan references the existing `BacktestRunConfiguration` and `EvaluationPlan`
semantics: exact dataset kind/revision/checksum, instrument/universe identity, feature revision
and source membership, strategy/version/complete parameters, simulation/fill/sizing versions,
all cost values/versions, calendar, dates, seed, embargo, thresholds, grid, selection rule,
robustness model, applicable risk-policy snapshot and entitlement decision evidence.
A dataset ID alone or `latest` is never sufficient. Canonical and noncanonical identities do
not become aliases. No caller can replace knowledge cutoff with wall-clock completion time.

Initial executable proposal allowlist is **momentum/v1 with period=20 only**. Its existing
robustness diagnostic/selection grid remains period 5/10/20: those three effective trials must
be disclosed and counted even though the reasoner offers one reference variant. All three corresponding causal feature periods must be
available in the pinned feature revision; a momentum20 feature alone is insufficient. Benchmark
buy-and-hold is an engine-owned control, not a selectable new hypothesis. SMA may be supported
later only through an explicitly reviewed mapping to actual feature periods and exact engine
keys; BB-129A's labels/40/80 periods are not silently adopted.

Falsification v1 for the fixture uses `validation.excessReturn <= 0` (fractional return,
not percent) as failure, plus the unchanged engine sample/selection/robustness gates. It is
not a profitability declaration or a newly calibrated scientific rule. Holdout failure remains
an engine-owned terminal test; the reasoner cannot choose its metric or threshold after seeing it.

### Identity and normalization

Reject ambiguous payloads before normalization. Canonicalize validated structured values using
a versioned explicit field order, UTF-8, ordinal ID/map ordering, UTC timestamps, ISO dates and
invariant finite decimal values without equivalent spelling/scale ambiguity. Preserve ordered
variant/trial sequences where order is meaningful; normalize declared sets only. Reject unknown
keys and incompatible units rather than guessing. Bind resolved exact references and policy
snapshots, not model-authored checksum claims. B must pin test vectors for culture/order/decimal
normalization. Do not change any existing Finance identity algorithm.

Use full SHA-256 for the new committed proposal and retain the full digest even if a display ID
is shortened. Separate an exact sanitized response/input digest from the **execution fingerprint**:
changing rationale wording, JSON key order, provider, request ID or timestamp must not create a
fresh executable experiment or budget. A new rationale can be linked as duplicate evidence.
Existing `FinanceResearchContracts.Fingerprint` serializes an object; it is not by itself a
canonicalizer for arbitrary untrusted JSON. Existing hypothesis IDs remain referenced as-is.

## Multiple-hypothesis governance

This proposal extends BB-124/092/129 accounting around the same engines; it does not introduce
an alternative statistics engine or claim a p-value/FDR guarantee.

### Family and protocol identity

Finance, not the reasoner, registers a research protocol before any reasoner call: objective,
target/horizon, evidence universe, allowed strategy families/parameter cells, development and
protected cohorts, selection/falsification rules, complete effective trial population and budgets.
Family membership is assigned from this registry and parent lineage, not generated prose.
Renaming a hypothesis, switching models, changing an idempotency key, shifting a threshold or
using a new overlapping revision cannot reset family/program history. Novel or ambiguous family
membership rejects pending human review; semantic-near-duplicate detection by an LLM is not trusted.
Within the first finite allowlist, exact parameter cells and parent/protocol membership make
near-duplicate accounting deterministic. Even distinct allowed cells consume the same budget.

### Proposed conservative first protocol limits

| Unit | Proposed initial ceiling | Accounting |
| --- | --- | --- |
| Reasoner invocations / submitted proposals | 1 / 1 per frozen protocol | No repair prompt, automatic retry or second suggestion. Malformed/no-useful output terminates this protocol invocation. |
| Families / caller variants / instruments | 1 / 1 / 1 | Reference momentum20; three engine-owned period trials counted separately, not hidden. |
| Robustness evaluations | 1 | Exact existing plan; five cost scenarios, benchmark and walk-forward work are predeclared diagnostics, not five new independent hypotheses. |
| Underlying backtest runs | At most 64 | Existing EvaluationPlan.MaximumRuns plus a preflight plan/budget check; charge actual unique runs, preserve reused-run links. Not BB-129A's 24 dataset/variant attempts. |
| Concurrency / automatic retries | 1 / 0 | Existing single-flight/idempotency principles. Timeout/restart does not refund unknown work or renew the protocol. |
| Reasoner deadline / whole-iteration cap | 30 / 300 seconds | Proposed engineering bounds; never accept partial scientific output or silently omit trials to meet deadline. |
| Fresh holdout release | At most one frozen selection per cohort | Benchmark and selected strategy form the declared comparison; no second adaptive candidate gets a fresh claim over overlapping data. |

These numbers are proposed engineering limits, not experimentally established statistical power.
For B they constrain only the synthetic fixture. A later multi-iteration production protocol
requires separate review of cumulative family/program limits and dependence between cohorts;
there is no authorization to loop indefinitely under new protocol IDs. A proposal for more than
one caller variant rejects in the initial policy even though the general schema can represent three.

Count every admitted hypothesis/parameter trial, including failed, rejected, timed-out, duplicate
and reused experiments through linked history. Keep submission count, admitted scientific trials,
actual engine executions and reused results as distinct counters (a duplicate must not create
new computation or be presented as an independent observation). Never erase losses or only retain
survivors. Excluded/ineligible variants retain reason codes. No stopping on an attractive interim
result, no post-hoc budget enlargement, no choosing which failed trial to hide.

A future production extension must atomically persist proposal commitment, budget reservation and
exclusive holdout-cohort reservation BEFORE computation. Separate processes/keys cannot each take
the last slot. Existing SQLite transaction/single-flight patterns are reusable; current research
rows do not already implement this cross-proposal ledger. Uncertain completion stays reserved;
recovery reconciles exact run IDs, never reruns with a fresh key or restores an unseen claim.

### Holdout protection and adaptive learning

Do not give the reasoner protected rows, derived features, returns, charts, distributions,
selected-holdout run IDs, holdout-linked risk metrics, full robustness payloads or classifications
that indirectly reveal active holdout performance. Suppress outcome-derived rejection details
until the cohort is closed. An opaque capability handle may identify an approved split without
permitting arbitrary data reads. Redaction includes aggregate/proxy/instrument-overlap paths.

Commit all variants, selection rule and allowed internal engine grid before calling the current
combined evaluator. The reasoner never participates between validation selection and holdout.
After unblinding, mark the cohort consumed across the entire research program, descendants and
reasons/models, including aborted or rejected attempts that revealed a statistic. Exposure through
a human/operator or another channel counts too; a new prompt cannot make previously seen data unseen.
Overlapping instruments/sessions, adjusted/reissued copies or revised features remain tainted
unless independent review proves a genuinely new eligible cohort. Unknown exposure fails closed.
A hash is an identity aid, not proof that datasets are independent or holdout is secret.

Existing already-published evaluations may inform exploratory questions with their consumed label.
They cannot be relabelled as fresh confirmation. A future learner can see a closed cohort's negative
and positive outcomes, but its descendants need independently qualified untouched evidence before
confirmation. No protected real-market holdout is claimed in B: synthetic data are engineering
fixtures, and possible model pretraining exposure is another reason not to claim historical data
are unknown to an external model. Fresh prospective evidence may ultimately be needed.

Stop on budget exhaustion, no useful proposal, repeated/ambiguous proposal, data/rights/lineage
gap, unknown cohort exposure, policy drift, persistence ambiguity or operational failure.
Advancement requires unchanged eligibility/causality/cost/OOS/holdout/robustness controls and
complete trial history; missing evidence stays missing. An engineering pass, `MoreRobust` or
`Challenger` never promotes strategy lifecycle, mode, shadow deployment or capital.
DSR/PBO/CSCV remain NotEvaluable; no new significance method is implemented or promised.

Adaptive reuse needs additional statistical assumptions beyond a fixed held-out test, as explained
by [Dwork et al., Preserving Statistical Validity in Adaptive Data Analysis](https://arxiv.org/abs/1411.2664).
Our inference for this repository is to restrict the first protocol to a frozen synthetic proof,
not implement reusable-holdout algorithms or advertise adaptive statistical validity.

## Reasoner input and validation

Allowed summaries are server-created from permitted development evidence: source-typed prior
classifications, metric ID/value/unit/sample coverage, robustness limitations, causal context,
purpose-specific dataset capabilities, missing-evidence codes, applicable risk outcomes and
complete negative-history accounting. Each summary includes exact provenance and whether it is
engineering-only, exploratory or from a consumed cohort. No arbitrary raw data, full database,
private paths, credentials, browser cache or unbounded text. Input selection/order/redaction has
its own version and digest. Missing units/unknown rights do not become zero values or grants.

Validate in this order before any engine call:

1. Bounded parse; known schema/version/discriminator; reject extra/duplicate keys, invalid values,
   free-form tools/code, impossible references and contradictory NoUsefulProposal content.
2. Resolve input digest, authorized scope and current rights/provenance from Finance-owned state.
   Prevent arbitrary reference enumeration; recheck changed entitlement/halt/policy at commitment.
3. Resolve strategy/version and exact parameter allowlist; verify feature definitions/periods,
   dataset purpose/schema, price/currency/calendar compatibility, causal cutoffs and policy snapshots.
   No implied aliases, `latest`, global eligibility or model-created feature revision.
4. Normalize and recompute identity; compare exact and family/parameter duplicates, parent graph
   (no cycles), program history and exposure/overlap. Unknown relationship rejects.
5. Expand all engine-owned grid/cost/benchmark/window work and reserve the complete budget and
   holdout scope atomically. Reject rather than silently shrinking an excessive proposal.
6. Persist commitment and validation evidence before invoking the exact compiled engine with the
   frozen resolved configuration. Never call a different `latest`-based runner as a substitute.

UNKNOWN => FAIL CLOSED applies to authorization, required scientific inputs and unsupported
capabilities. It does not retroactively revoke ADR 0022's explicit owner-accepted *limited private
research* decision: preserve that limitation, refuse unsupported conclusions, and require a
separate explicit decision for model-provider disclosure. No inferred redistribution permission.

## Results, risk and reproducibility

Keep exact existing typed results: robustness `InsufficientData/Fragile/Mixed/MoreRobust`,
selection `Pass/Fail/InsufficientData/Contaminated`, autonomous
`Rejected/Inconclusive/Promising/Challenger/NotEvaluable`, campaign
`Rejected/InconclusiveNotEvaluable/SurvivedInitialScreen/RobustCandidate` and risk verdicts.
These are not interchangeable. Do not map `MoreRobust` directly to campaign `RobustCandidate`,
map missing evaluations to zero, or turn an admission rejection into a measured scientific loss.
LearningResult references the evaluator's actual output plus missing-evidence/admission reasons;
no second scientific classifier. UI labels may explain the source vocabulary without rewriting it.

Risk is a separate deterministic authority. Historical learning may reference an existing matching
risk evaluation or state that current prospective risk evidence was not evaluated/applicable.
Never synthesize current provider/cadence health from historical bars or use old ALLOW for a new
proposal. DENY/HALT/INSUFFICIENT_DATA cannot be overridden; even ALLOW means hypothetical RESEARCH
only. The current risk engine's declared spread/sector limitations remain visible. B can exercise
a synthetic deny/missing-evidence fixture, but cannot claim production risk approval.

Retain contract/projection/normalization/protocol/engine versions; sanitized future adapter/model
identity and generation settings when applicable; exact permitted input projection plus references
and digest; bounded sanitized response/draft; normalized proposal and execution fingerprint;
family/parent/cohort/selection/budget events; every validation failure and admitted/skipped attempt;
exact run/evaluation/result checksums, classifications, limitations and optional commentary.
Distinguish data event/session time, source knowledge time, research knowledge cutoff, proposal
received/committed time, execution start/completion and persistence time. Audit wall-clock times
are not an input to deterministic experiment identity. Reject impossible ordering, never backdate.

Model generation need not be repeatable. Deterministic replay starts from the frozen proposal,
resolved evidence and exact engine/policy versions without calling the reasoner. Reopened results
must have identical underlying run IDs/checksums and relevant structured outcomes. Revalidate
stored identities/checksums and relationships at the new trust boundary; an existing JSON reader
is not such a validator. Hash equality alone is not authenticity or license permission.

A production learning journal would belong to the same Finance persistence boundary, referencing
existing result rows rather than copying scientific truth into a parallel store. Its schema,
transaction/restart/retention integration needs a later explicit implementation decision. Do not
assume current result_json can be arbitrarily extended to store the complete admission ledger.

Retention follows source rights, including derived inputs, summaries, prompts and model output.
Preserve negative results for the permitted lifecycle, with safe deletion/tombstone evidence where
rights require removal. Replay then honestly becomes unavailable; never retain prohibited derived
data to satisfy a reproducibility slogan. Provider keys, credentials and unrestricted raw model
payloads are not audit data. A malformed response can leave reason/size/hash evidence without
storing unsafe text; hashes/references alone are not a promise of content replay.

## Failure and degradation

| Condition | Required response |
| --- | --- |
| Unavailable reasoner, timeout or NoUsefulProposal | Record bounded operational outcome; no engine call, no auto retry, existing Finance remains independent. |
| Malformed/unsupported/excessive/unauthorized draft | Record sanitized validation rejection and stable reason; no execution or policy relaxation. |
| Duplicate | Link original immutable outcome; no extra experiment, no new independent success, no renewed holdout. |
| Insufficient data, ineligible purpose, stale lineage or contamination | Preserve native NotEvaluable/InsufficientData/admission reason as applicable; never try another dataset/threshold to obtain a pass. |
| Risk veto or policy/rights change | Deny proposed advancement/admission as applicable, preserve deterministic evidence, do not unhalt or rewrite policy. |
| Persistence unavailable/conflict | No execution without durable commitment; no success without durable result. Quarantine conflict for human review. |
| Interrupted committed iteration | Reconcile exact reservation and result IDs on restart; completed evidence reused, unknown work remains consumed/reserved, incomplete state retained. No automatic fresh-key retry. |
| Result cannot be replayed under current retention rights | Expose unavailable evidence and provenance limitation, not a replacement calculation or fabricated old result. |

Future operational status can reuse Pending/Running/Completed/Failed plus validation reason codes;
NoUsefulProposal is a reasoning outcome, not a strategy verdict. Do not alter today's scheduled
operations/circuit breakers or make them depend on reasoner availability.

## Security and external knowledge

General application auth is explicitly incomplete in ARCHITECTURE and the accepted BB-130 exit.
Existing research POST/read routes and home-network placement are not proof of a secure AI
capability boundary. Source review here is not a penetration test or a newly reproduced exploit.
Docs A and a strictly local synthetic test fixture B can proceed before that broader sprint because
they expose no service, real dataset, credentials or external actions. **RESEARCH-only is not a
waiver for real model access or remote mutation.**

Before any production reasoner integration: require reviewed authenticated principal/scope,
per-resource Finance read/propose permission, deny-by-default capabilities, identity-bound audit,
rate/resource/token limits, egress allowlist, data export/retention rights and prompt-injection/
output-handling tests. No arbitrary URL fetch, shell, SQL, dynamic code or tool recursion. Render
commentary as untrusted text. Schema conformance alone cannot establish authorization or prevent
indirect injection; enforce independent controls outside the model. This follows the separation
and least-privilege guidance in the [OWASP prompt-injection prevention guidance](https://cheatsheetseries.owasp.org/cheatsheets/LLM_Prompt_Injection_Prevention_Cheat_Sheet.html).
No SDK, model or provider is selected in A.

Broker, orders, PAPER, LIVE, AUTO, capital allocation, provider activation and other high-authority
external actions remain blocked by the existing separate owner/security/entitlement gates and
are not capabilities of BB-131. Research output cannot self-promote into those later stages.

Future educational material attaches as a separately tagged **idea source**, with rights/provenance
and retrieval/version metadata. It is not an EvidenceReference proving a Finance outcome.
Investopedia remains planned-only: concept -> proposal -> deterministic evidence -> robustness/risk.
No scraping, ingestion or external educational integration. Even benign source text is untrusted
and may neither expose protected holdout nor change policy.

## Proposed BB-131B — fixture contract and replay proof

Smallest useful next slice, **NOT STARTED / NOT AUTHORIZED by A**:

1. Add only the minimal typed Finance proposal/input/result contracts and pure fail-closed
   validation needed for the initial momentum20 policy; fake reasoner and orchestration live
   in tests. Do not create a Brain project, runtime registration, endpoint, worker or provider.
2. Use one deterministic synthetic instrument with pinned synthetic market/feature revisions,
   existing strategy/backtest/robustness engine and policies unchanged. Input projection contains
   development evidence only. Freeze the complete engine plan/grid before evaluation. Synthetic
   outcome labels must always remain engineering-only, including any positive control.
3. Prove accepted draft -> exact existing EvaluationPlan -> evaluator/underlying BacktestResults.
   Persist the underlying runs through `FinanceBacktestPersistence` into isolated temporary
   SQLite using existing schema; reopen through the existing reader and independently compare
   IDs/checksums/content. Serialize the learning envelope and robustness result in a **test-only**
   bounded manifest for restart/replay assertions, not a second production evidence store.
4. Replay the frozen proposal without invoking the fake reasoner, reproduce existing evaluation
   and run checksums, verify immutable conflict rejection and absence of new duplicate rows.
   Include invalid/duplicate/over-budget/unknown-exposure/rights and risk-denial fixtures.
   Verify invalid output never calls an engine; no-useful/timeout does not affect direct Finance.
5. Assert no hidden-holdout field/proxy crosses the input boundary, culture/order normalization
   invariance, semantic changes alter fingerprints, altered prose cannot reset budgets, all
   engine-owned trials are counted, no claim of fresh holdout after simulated exposure and no
   promotion to risk/execution authority. Preserve native missing-evidence outcomes.

B DoD is a reviewed, reproducible **contract proof**, not a deployed learning loop or production
crash-safe admission ledger. No real provider/data/holdout, production schema change, new API,
scheduler integration, strategy/calculation change or actual scientific conclusion. Required
implementation verification would include focused contract/scientific/persistence regressions,
backend Release build/full tests, existing format gate, docs/diff/secrets checks, and unchanged
Web/runtime/Finance safety boundaries. Counts and success cannot be claimed before implementation.

Real-data adaptive iteration must wait for separate authorization of exposure/family accounting,
atomic production admission/recovery/retention, complete evidence lineage and authentication/
authorization. A fake fixture cannot prove those production properties. If B cannot bind the
existing engine without changing scientific behavior, stop for architecture review rather than
expand it. No campaign success, feature-lineage repair or statistical method is smuggled into B.

## Review questions and limits

Owner/architect approved ADR 0038, the conservative one-invocation initial protocol, synthetic-only
B proposal and explicit real-data/security prerequisites as architecture, not implementation authority. Open future choices:
production journal schema and crash atomicity; protocol/cohort overlap adjudication; data rights
for any future provider; statistical power/confirmation procedure for genuinely adaptive research.
These block production expansion, not documentation publication. No newly reproduced current
correctness/security/scientific/lineage defect requiring blocker handoff was established.

BB-123/124/127/128/129 historical evidence is preserved. No tests, code, schema, packages, CI,
provider state, calculations, scientific results or runtime were changed or executed by A.
No deployment or owner UX/security approval is claimed.
