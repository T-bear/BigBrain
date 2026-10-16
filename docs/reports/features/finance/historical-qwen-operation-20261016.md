# Historical Qwen deployment and execution — 2026-10-16

Detta är en sanerad GitHub-version.

## Metadata

- Operational review, not a development sprint; owner-approved merge/deployment and one real session.
- Previous accepted main: `be7634bd82f52b3cb536142ae3e03dd0e2656aa2`.
- Approved candidate: `87b08d3d074beb02a4d1d41e467c4e0c28a057bf`.
- Accepted merge: `70b659f772b7b7883a5716786859f1e7833a5634`.
- Candidate and merge tree: `6433c956ff87bc8434a9a8fff6c5294fe45b258d`.
- Merge parents are exactly previous main and approved candidate, in that order.
- Branch: `review/finance-historical-qwen-deployment`; resolve this documentation checkpoint's SHA/tree
  from Git metadata for publication subject `review: record historical Qwen deployment and authority gate`.

## Status

Latest disposition: [G3a existing diagnostic access blocked](#g3a--existing-diagnostic-access-blocked).
The operator reports one diagnostic capture after checkpoint439bbb0. Earlier NOT RUN statements
retain their historical scope. A partial correction is prepared in Git only; no successful host preflight is implied.

**REVIEW CHECKPOINT / historical execution BLOCKED before enrollment or inference.**
Merge accepted/published and API deployed/healthy; deployment evidence is recorded below. No new product code was changed.
No AI-generated hypothesis, evaluation or successful model run is claimed. No inference/evaluation
budget was consumed or reset. No program, journal or model profile was provisioned.

The owner approved one session I1/E1/T3/C64, within the accepted CPU2/memory4GiB/swap0/pids32,
30s inference/250ms grace/2s reap envelope (33s reservation). This approval is not missing and must
not be requested again. Current historical composition runs opportunity1 only; it does not authorize
continuing historical iterations. The prospective scheduler remains disabled.

## Evidence

### Merge and existing verification

Exact remote/main/candidate checks, clean tracked source and ancestry passed before merge. Unrelated
untracked design work and four Sentinel ADR drafts were preserved. Normal `git merge --no-ff` and
`git push origin main` succeeded; remote tree equals the reviewed candidate. No history rewriting.
Candidate branch had no Actions run: CI is configured for main pushes and pull requests, not branch
pushes. The merged revision's [CI run38863227447](https://github.com/T-bear/BigBrain/actions/runs/38863227447)
completed **SUCCESS**: backend, frontend, documentation and secrets. No local test-suite repetition;
[unchanged implementation evidence](historical-qwen-integration-20261016.md#evidence) remains applicable.

### Build and deployment

`bash scripts/build-bigbrain-api.sh 70b659f772b7b7883a5716786859f1e7833a5634` completed with exit0.
The build used the accepted immutable Git archive; ignored configuration/untracked work was excluded.
Build ran detached with closed stdin and durable exit/log files; no active waiting loop was used.
Image: `sha256:26a2b150f449c2c7adce3b591b3ed10f3f3d35c5ba91042ff21150cd535b80fc`.
OCI revision equals accepted merge. Predeployment API health/recovery/clock were healthy, NTP synchronized,
Finance/lifecycle stores and clock marker present. Available storage was approximately406GiB.

The only deployment command was `docker compose up -d --no-deps --no-build --pull never --wait
--wait-timeout 60 api`. Existing environment matched resolved Compose before recreation; no external
configuration or credentials were changed. The local secret file remained mode0600.

API started2026-10-16T06:52:20.069029818Z. Compose reported healthy; `/health` returned HTTP200.
Container image ID equals the built image above. Both running OCI label and
`docker exec <api> dotnet BigBrain.Api.dll system-build-identity` equal
`70b659f772b7b7883a5716786859f1e7833a5634`. Recovery reports healthy/completed,
clock synchronized, lowDisk=false and operatingMode=RESEARCH.

All environment key/value pairs are exactly unchanged (order-insensitive comparison), and all10
mount source/destination/type/read-write tuples match predeployment. Every unrelated running container
retained its ID, start timestamp and restart count. The initial environment list-order assertion was
corrected to compare actual key/value semantics; no configuration difference was found.

ObservationRuntime=true, Alpaca transport=true, cadence360minutes and lookback3days remain unchanged;
the exact existing AAPL mapping JSON is preserved. EODHD/FRED/ResearchScheduler remain false and
ResearchOperations MaintenancePaused=true. API recreation restarts the accepted full-cadence wait;
no immediate/manual acquisition or cycle trigger was performed. No claim of an observed postdeployment
provider request is made. Other services and their configurations were not changed.

The running container's `finance-historical-inspect` succeeded after deployment and returned the
same complete source commitment as before deployment. Read-only reopen validates the historical
source, not an AI result. There is no new hypothesis/result to reopen.

### Historical source, read-only

The new accepted image ran only `finance-historical-inspect`, with a read-only Finance volume,
no network, no capabilities and no-new-privileges. This command does not migrate, enroll or invoke a model.

- Source: NASDAQ-WIKI / WIKI/PRICES / AAPL, revision `wiki-5713d7dccfa38f56`.
- Exactly748 observations2014-01-02–2016-12-19; fixed500-session evaluation2014-01-02–2015-12-24.
- Artifact: `sha256:dd5127aae478d270150904fcbad6e96a42e461e13c3d48a1587edb9b89cea43e`.
- Full admission commitment: `sha256:ad5db63d5996463ea4ce23a9c28fe53e14aadcd137616dce6267f9bf997c405e`.
- Rights/promotion/raw capability/coverage and real acquisition chronology validated by accepted code.
- Limitations preserved: historical, not prospective; raw, not total return; unresolved corporate actions;
  unknown survivorship and unproven prior historical exposure. No holdout/model input was generated.

Two preliminary isolated read-only diagnostic attempts failed: the first returned failure without a
specific diagnostic retained; the second was rejected by Docker because its old image reference was
unavailable for a fresh container after the build. Neither is counted as a passed check. The new
accepted image's own read-only command then succeeded before and after deployment. These diagnostics
did not enroll authority, run inference or modify source evidence.

### Concrete execution gate

The live configuration has no `Finance:RecurringAuthority` journal binding and no historical model
profile. Its existing process also has no configured no-new-privileges/cap-drop isolation required by
protected authority. No protected authority directory is mounted. `sudo -n -l` returned
`sudo: a password is required`; noninteractive host-root provisioning is unavailable.

The [accepted operator contract](../../../architecture/finance/research-learning-contract.md#explicit-historical-operator-path-separate-operational-approval-required)
requires independent host-root custody: protected root-owned ancestor chain, dedicated ext4 journal,
append-only kernel attribute, exact enrollment commitment, restore exclusion, and nonroot/capability-free
execution. The single missing operational prerequisite is completion of this trusted host provisioning,
including the bound isolated model profile, through an owner-local privileged session. Docker privileges
were not used as an alternative root channel. No passphrase/password was requested, transported or logged.

Enrollment was deliberately not attempted before that environment can pass its protection checks.
No empty file, config flag, test fixture, replacement database or new program identity stands in for
protected authority. The prior owner approval remains valid; this is a technical/local custody gate,
not another request to approve the same research budget.

## Changes

Documentation only: this report/catalog, STATUS, BACKLOG and the single recovery note. Accepted product
code is unchanged. Operational deployment changes only the API image/process, preserving configuration
and persistent mounts. No new architecture, scheduler, model, data provider or feature sprint.

Documentation verification: `node scripts/verify-documentation.mjs` PASS (269 Markdown files,
91 unique backlog IDs); `git diff --cached --check` PASS; staged
`gitleaks git --staged --redact --no-banner` PASS with no leaks. Explicit five-document scope review
passed. No application code/test suite was rerun locally: exact approved implementation and green
merge CI were reused. Only the API image was rebuilt for its accepted revision identity.

## Security

No credentials, private paths, raw provider/model input, OHLCV values, private configuration or identities
are published. Dataset/artifact/source hashes are non-secret provenance. No protected journal change,
grant, inference, manual provider call, trading, broker, order, position or real capital operation occurred.
Existing AAPL automatic collection remains independently authorized; its mapping/cadence was not altered.
Finance remains **RESEARCH / 0 SEK / NONE**.

## Remaining work

Complete owner-local trusted provisioning under the already approved contract, then verify it before
issuing the one historical program and consuming I1/E1/T3/C64. Do not increase30s timeout, retry a spent
attempt, or renew authority. Until then there is no saved AI hypothesis and no independent historical
research process. The conditional feature freeze was not declared satisfied; no further feature
implementation or new sprint is started. See [operational gate](../../../BACKLOG.md#historical-qwen-operational-gate).

## Resumption

The approved merge/deployment must not be repeated merely because the research gate is blocked.
Use the [single recovery handoff](../../../operations/codex-recovery.md#historical-qwen-deployment-and-execution--2026-10-16)
and actual deployed identity. Owner-local root provisioning, not new development or a general review,
is the next necessary action. Stop for independent architect review of this sanitized checkpoint.

## Hosting preparation — 2026-10-16

**REVIEW CHECKPOINT / NOT READY FOR ENROLLMENT / NO INFERENCE.** Continuation on the same
operational branch; accepted main remains `70b659f772b7b7883a5716786859f1e7833a5634`. Resolve the
additive commit/tree by subject `review: prepare historical Qwen hosting and document cgroup gate`.
The independent external Finance review report was not consulted or used as implementation input.

### What is implemented and what is not

Added a disabled **preparation-only** Compose fragment and a bounded
[operator runbook](../../../operations/runbooks/finance-historical-hosting.md). The fragment declares
read-only bundle/model mounts, persistent private coordination, separate authority-directory mount,
zero capabilities and no-new-privileges. It does **not** provision a worker cgroup, solve Docker's
migration boundary or provide an inference-ready deployment. It was not applied to any service.

A mode0600 ignored local preparation file records the existing exact image and staging locations.
It contains no credential, grant, approval/expiry timestamp or enrollment checksum. It is not the live
`.env`. The coordination directory, protected journal and effective model profile remain unprovisioned.
An updated native worker was built into private staging from the exact accepted source; no production
artifact was overwritten and no worker main/model inference was executed.

### Host and artifact evidence

- Deployed OCI/compiled revision remains the accepted main. API healthy; start timestamp remains
  `2026-10-16T06:52:20.069029818Z`. No API restart/recreation or AAPL configuration change.
- Host systemd257, Docker systemd cgroup driver, unified cgroup2 with nsdelegate. API scope Delegate=yes;
  container namespace private, cgroup mount read-only, root0755 and cgroup.procs0644 owned by root,
  subtree controllers empty. API UID/GID1654 cannot write the common-ancestor migration file.
- Supplied model hash matches `b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897`.
- Supplied old worker hash matches `631deb1ccfece198309c0485ada2d5eae62ecd3b146c1efc945078e97dd2eb1b`,
  but binary bytes lack historicalDevelopment, recurringHistory and finiteHistory prompt additions.
  Identity match alone does not make this the accepted historical worker.
- New unchanged-source build hash: `0e41bc5c59e300434b888a7cf3533aa19ca8a3a976021590e3f89841db0cb830`.
  Source worker hash `cb4f39a1b96e5fbfb87ad332fa422bc65b2d0c2819159ec137e4449ebff65463`;
  containment hash `c1cbb52e92484ee2de272ae894ec80c20fd62b025bcfcafee6cd2646e24d4c2d`.
- Existing API image lacks libgomp.so.1. Loader-only check failed127 for that missing dependency.
  Adding the existing OS library to the **private staged bundle** made worker and Haswell-backend
  dependency checks both PASS/exit0. Library hash:
  `6456b504ac347bd50c419667440b691f6966ad8aff5309d2f06c977fe2f9debe`.
  No package installation/image rebuild, model load or artifact download. The bundle is not yet
  administrator-protected and is not approved as an active model profile.
- Existing authority directory and ancestor metadata are root-owned; directory root:1654 mode0750,
  filesystem ext4. Current operator cannot list its contents and sudo requires a password. No journal
  is configured/mounted in the API; absence of the file inside the protected directory is owner-reported.
- Canonicalized journal-directory location is outside Finance/lifecycle/owner-drop source roots and
  repository. The existing capture helper uses those three explicit data scopes, not the host root.
  This does not prove exclusion from every possible administrative restore; root must attest all
  applicable scopes before enrollment. No backup/restoration was reopened or executed.

### Actual commands and checks

- `git fetch origin`, exact accepted identities, tracked production-code/compose comparison: PASS.
- `sha256sum` of the supplied GGUF/worker: both exact matches.
- `g++ -std=c++17 -O2 -fopenmp -Wl,--no-as-needed -I data/bb132a/include
  src/BigBrain.LocalReasoner.Worker/worker.cpp -L data/bb132a/runtime -lllama -lggml -lggml-base
  '-Wl,-rpath,$ORIGIN/runtime' -o <private-new-worker>`: PASS. Never executes the worker.
- Disposable exact-image loader containers: no network, read-only bundle, cap-dropALL,
  no-new-privileges,256MiB/pids32; `ld-linux-x86-64.so.2 --list` on worker and backend: PASS after
  the documented missing-library correction. No Finance/lifecycle/journal/model mounts or credentials.
- Compose `config --quiet` and parsed-object assertions: PASS. Unrelated services and all existing
  AAPL/provider environment values identical; model/scheduler disabled, no cgroup mount or host-path
  auto-creation. Missing mandatory preparation inputs rejected. Initial assertion assumed an explicit
  false `privileged` key; Compose normalizes it away. Corrected semantic check passed; no security
  setting was weakened. These are render checks, not live topology or inference tests.
- Existing local systemd257 manpage confirms DelegateSubgroup behavior; official kernel/Docker/systemd
  documentation consulted for the precise migration constraint, linked in the runbook.
- Documentation verifier PASS270 Markdown files/91 unique backlog IDs; whitespace/scope/staged secrets
  checked before publication. No full application suites: product code/scientific contracts unchanged.

### Required administrator action and stop

A mount-only API override cannot satisfy the observed cgroup topology. No cgroup:host shortcut,
privileged container, broad writable host cgroup mount or change to the running API was attempted.
Minimum proposed placement: the **same accepted maintenance DLL** in one bounded, nonroot delegated
systemd unit with controller and empty worker child groups. No new inference engine/daemon/scheduler.
This is proposed, not installed or integration-tested: an owner-local administrator must establish the
exact delegation/mount/resource boundary and prove inert-child migration/cleanup before enrollment.
The runbook specifies exact limits, immutable artifact custody and enrollment → journal seed/protection
→ read-only authority-check order, with process-local disabled gates preserving live AAPL collection.
No inference is permitted in this task, even if those preconditions later pass.

Versioned changes are the preparation fragment, runbook and existing report/recovery/backlog handoff.
Private staged binaries/library manifest/config are excluded from Git. No production permissions,
configuration, cgroup, journal, database, grant or service changed. Finance **RESEARCH / 0 SEK / NONE**.

## G3a release completion — 2026-10-16

**REVIEW CHECKPOINT / G3a BLOCKED / G3b NOT RUN.** This is continuation, not a new Finance sprint.
Accepted main70b659f772b7b7883a5716786859f1e7833a5634/tree6433c956ff87bc8434a9a8fff6c5294fe45b258d
and previous checkpointe97baa8c110b1ed9604cd2a0be823bc5dea710e8/tree7bc6bb89cb10529e1aee0a75910499134db1ee2e
were fetched and verified. Same branch; no review history rewritten. New publication resolves by
subject `review: prepare G3a systemd isolation and release gates`. The external standalone Finance
review report was not used. Earlier dated evidence above is preserved and reused within its scope.

### Implemented, without production activation

- [FIN-OPS-G3-20261016](../../../operations/finance-historical-qwen-g3-checklist.md) is the canonical
  release-local G1/G2/G3a/G3b definition, invariants, severity rules and two-round review limit.
  STATUS/BACKLOG now distinguish accepted API deployment from unverified Qwen hosting. No general
  AGENTS/ADR rule or scientific contract was changed.
- `infrastructure/finance/bigbrain-historical-maintenance.service`: preflight-only, UID/GID1654,
  Delegate cpu/memory/pids, DelegateSubgroup controller, zero capabilities/NNP, no restart/timer,
  isolated network/devices/tmp, read-only system, only its own delegated cgroup subtree writable
  plus persistent private result directory. Whole-unit limits cannot expand the approved model limits.
- `infrastructure/finance/historical-preflight.py`: no command/path parameters or Finance imports.
  Validates actual topology, namespace mounts, identity, parent emptiness, controllers; exclusively
  creates worker0700 and the five exact native limits; migrates one inert child; verifies unchanged
  parent, child identity, pidfd kill/reap and empty leaf. Exceptions also terminate/reap the owned child;
  systemd bounds uncertain cleanup. No existing worker/process or result can be adopted/overwritten.
  Results are persistent/private and bound to fresh systemd InvocationID plus probe/unit hashes.
- `tests/deployment/test_historical_preflight.py`: fixture policy refusals plus real harmless child
  lifecycle tests, without systemd/cgroup/Finance writes. The existing runbook contains exact next
  administrator steps and the boundary between this probe and final data-enabled maintenance context.

The expected topology is a dedicated service parent with empty `cgroup.procs`, sibling `controller`
and `worker`; only descendants of this delegated service are managed by the probe. This topology is
**not yet observed** on the host. The current API's private/readonly Docker cgroup remains unchanged.
Systemd257's installed manual confirms DelegateSubgroup applies to main ExecStart, not ancillary
ExecStartPre commands; the unit uses only ExecStart. No privileged container, host cgroup namespace,
broad writable cgroup mount or native containment change is introduced.

### Verification and retained evidence

| Check / command | Result and exact scope |
| --- | --- |
| `git fetch origin`; main/branch/tree checks | PASS, exact identities above; unrelated untracked work preserved |
| `python3 -B tests/deployment/test_historical_preflight.py -v` | PASS:11 tests,0 failures,0 skips; actual inert child kill/reap on success/error. Does not prove cgroup migration. |
| `systemd-analyze verify infrastructure/finance/bigbrain-historical-maintenance.service` | PASS/exit0 under systemd257.13; syntax/config only |
| Actual systemd unit start / child migration / mount enforcement | BLOCKED: `sudo -n true` requires a password. No unit installed, cgroup created or permission changed. |
| `systemctl show bigbrain-historical-maintenance.service -p LoadState -p ActiveState` | PASS as an absence check only: not-found/inactive; no existing installation overwritten |
| Retained private artifact manifest | PASS7/7 file commitments; updated worker remains0e41bc5c59e300434b888a7cf3533aa19ca8a3a976021590e3f89841db0cb830 |
| `sha256sum` pinned GGUF | PASS: b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897 |
| Host `ld-linux-x86-64.so.2 --library-path <staged-runtime> --list <worker/CPU-backend>` | PASS twice, no worker main/model load;9 resolved dependencies hashed into private host-loader manifest, including libgomp |
| Final root-owned artifact installation and final-unit loader closure | NOT RUN / administrator gate; staged files remain operator-owned. Contained library links must become protected regular copies. |
| Running API image/OCI/compiled identity/health | PASS: same image26a2b150…, accepted70b659f…, healthy; start remains06:52:20.069029818Z |
| Current environment/mount equality against retained postdeployment evidence | PASS, exact comparisons without printing values. AAPL/Alpaca true, EODHD/FRED/ResearchScheduler false. |
| Full Finance/backend/frontend tests | NOT RUN; Finance/application code unchanged. Reuse exact accepted implementation/CI evidence above. |
| Historical inspect in final context / profile / enrollment / journal / authority-check | NOT RUN; isolation and root custody must pass first. Previous read-only source validation remains dated evidence only. |

Initial focused test run was10 PASS/1 FAIL: a unit-file assertion matched the literal `[Install]` in
a comment. Checking actual lines corrected the test; no unit security setting changed. Initial private
manifest comparison omitted its `sha256:` prefix and matched0/7; corrected canonical comparison passed
7/7 without changing any artifact. These were verification mistakes, not hidden host successes.
The expected negative duplicate-result test prints a sanitized FAIL category while its assertion passes.

`node scripts/verify-documentation.mjs`: PASS271 Markdown files/91 unique backlog IDs, including
relative-link checks. Python AST syntax checks PASS for both new scripts. `git diff --cached --check`
and intended10-file scope PASS; no source/application/production Compose/AGENTS changes. Staged
`gitleaks git --staged --redact --no-banner .` (8.28.0): PASS, no leaks. Unrelated untracked work excluded.
No actual model inference or provider command was invoked. No production configuration, database,
grant, journal, API container or other service was modified. Private manifests are excluded from Git.

### Remaining P1, exact next action and G3 disposition

No P0 invariant breach was found. **P1 remains:** missing actual final host isolation/protected-artifact/
authority evidence on the selected maintenance path. G3a cannot pass until this evidence exists;
publication does not approve away that gate. No grant/enrollment was issued to mask missing isolation.

Next concrete action: owner-local administrator follows
[G3a preflight installation/start](../../../operations/runbooks/finance-historical-hosting.md#g3a-model-free-systemd-preflight),
using these exact reviewed files. It starts only the inert unit, never Finance or Qwen. After a fresh
PASS, complete the same runbook's root-protected final-context/artifact/coordination and restore-exclusion
checks; then follow the existing historical sequence, ending in `RecurringAuthorityMatched` and STOP.
The final same-DLL context, including Finance/lifecycle and Sentinel access, must be proven without
altering those services; the preflight-only unit is not falsely presented as a complete runnable host.

Existing50.458s inference evidence exceeds the authorized30s cap by20.458s. The cap remains unchanged;
a later correctly authorized timeout is an outcome, not permission for retry/refund or a larger grant.
G1/G2 remain verified; G3a BLOCKED; G3b NOT RUN. Finance **RESEARCH / 0 SEK / NONE**.

## UnexpectedWritableMount diagnostic checkpoint — 2026-10-16

**REVIEW CHECKPOINT / G3a BLOCKED / NO POLICY RELAXATION.** Same operational branch. Baseline main
`70b659f772b7b7883a5716786859f1e7833a5634`, tree`6433c956ff87bc8434a9a8fff6c5294fe45b258d`;
previous reviewed checkpoint`bfa99fb98da83f6dcee7618f1c1a2d26cb94f62d`,
tree`8b39e20a22aa6055d37ad9a35474eca87a07d549`, all verified after fetch. Resolve this additive SHA/tree
by subject `review: add isolated G3a mount diagnostics without policy relaxation`. No external Finance
review, unrelated files or implementation branch was used as development input.

### Verified facts versus operator evidence

- Read-only `systemctl show`: installed unit loaded/static, failed/exit1, MainPID0, no drop-ins.
  Last start and exit both2026-10-16T17:59:09+02:00. No process remains whose mount namespace can be read.
- Installed probe and unit match the repository byte-for-byte. SHA-256 respectively
  `97ecacefdeed8e95e7056557b0871384d5e4ab0b9194e7846679f4cffe04bbf6` and
  `b643dc93ad1c618753d603072d346af3dfe06d6f3e4e54de05d0a97c6e4b73c4`.
- Owner reports initial217/USER resolved by dedicated UID/GID1654 account, then persistent result
  FAIL/UnexpectedWritableMount. This checkpoint did **not** read the protected result or recover the
  failed namespace. Accessible journal output did not independently establish the exact category.
- `sudo -n true` still refuses without a password. No administrator operation was attempted.

### Code findings and remaining hypotheses

The original parser uses the correct mountinfo target and per-mount-option fields and locates the
separator after optional fields. It rejects any unlisted `rw` mount, stops at the first one and saves
only the category. It neither reports the offending target nor distinguishes masked/stacked mounts;
it also leaves path escapes undecoded. There is **no evidence yet** that an escape or field-index
error caused this host failure. All current permitted targets are fixed ASCII paths.

Possible explanations remain: a truly unintended writable host mount; a systemd-created protection
or private-device mount rejected by the narrow predicate; a stacked/hidden mount; or input handling
that requires a real offending fixture. None is selected as the cause and no extra mount is allowed.
VFS mount options and superblock options differ; both are captured separately in the diagnostic.
[Linux mountinfo specification](https://man7.org/linux/man-pages/man5/proc_pid_mountinfo.5.html),
accessed2026-10-16, defines those fields and the mount-parent information needed for stacked mounts.
Systemd257 documents that ProtectKernelLogs makes kernel-log paths inaccessible; this motivates a
masked-mount hypothesis, **not** proof of this host's mechanism or permission to whitelist such a path.
[Version-pinned systemd source manual](https://raw.githubusercontent.com/systemd/systemd/v257/man/systemd.exec.xml),
accessed2026-10-16; the freedesktop rendered-manual request returned403, so was not used as evidence.

### Bounded diagnostic and checks

Added separate `historical-mount-diagnostic.py` and `bigbrain-historical-mount-diagnostic.service`.
The unit preserves every isolation/resource setting; only executable/description, its own cgroup
name and private stdout/stderr routing differ. It reuses the existing state-directory view without
writing its contents. The script imports the **hash-pinned installed** policy without invoking probe/main,
checks its own identity/controller context and reads only mount metadata. It creates no child, mount,
cgroup, grant or Finance state. It captures all entries, including readonly/stacked rows, with source,
filesystem-root and arbitrary option values omitted. Output requires a precreated root0600 regular
file; terminal/journal fallback is refused. The result remains private because targets may be sensitive.

Only the exact diagnostic unit cgroup target is substituted for original-policy comparison in memory;
actual target and an explicit substitution marker remain recorded. Unknown paths, subtrees, escaped
paths and masked examples remain rejected. `DIAGNOSTIC_ONLY` is never a G3a success declaration.
The original `historical-preflight.py` and maintenance unit are **unchanged**, locally and on the host.

| Check | Outcome |
| --- | --- |
| `python3 -B tests/deployment/test_historical_mount_diagnostic.py -v` | PASS10/FAIL0/SKIP0: all unexpected rows, VFS/superblock distinction, escapes, stacked records, exact cgroup-name substitution, redaction, malformed/bounded inputs, terminal refusal, pinned original hashes and full unit-setting equivalence |
| `python3 -B tests/deployment/test_historical_preflight.py -v` | PASS11/FAIL0/SKIP0; unchanged fail-closed behavior and inert owned-child cleanup. Expected negative fixture prints a FAIL category. |
| `systemd-analyze verify infrastructure/finance/bigbrain-historical-mount-diagnostic.service` | PASS/exit0, static only |
| Real diagnostic namespace capture | NOT RUN; requires explicit owner approval and local administrator access |
| New installed-preflight attempt / inference / Finance operations | NOT RUN / prohibited by this task |

No broader application suite is justified: Finance code and scientific/runtime contracts did not
change. The separate parser/test additions do not modify installed acceptance behavior. Publication
checks: `node scripts/verify-documentation.mjs` PASS271 Markdown files/91 unique backlog IDs and
relative links; Python AST and administrator procedure `bash -n`/embedded Python syntax PASS without
execution; all four runbook-pinned input hashes match. `git diff --cached --check` and nine-file scope
PASS. Gitleaks8.28.0 `git --staged --redact --no-banner .` PASS/no leaks. Original production probe/unit,
application source, Compose, AGENTS and unrelated untracked work are excluded from the delta.

### Decision and next action

**Root cause unverified; P1 remains for actual mount isolation.** No verified P0 or justified production
policy correction was found. One concrete missing item is a private mount snapshot from the equivalently
isolated diagnostic process. The [single pending-approval local procedure](../../../operations/runbooks/finance-historical-hosting.md#unexpectedwritablemount--one-owner-approved-diagnostic)
creates only temporary diagnostic artifacts, starts that separate unit once, preserves private evidence
and removes only its temporary unit definition after completion. It does not restart the installed
preflight/API/Web/Docker or create persistent write permissions. On failure STOP without retry.

Owner approval is requested for that diagnostic only, as required by this task's administrator-test
boundary. Further policy correction must be supported by its actual findings. Successful diagnostic
collection alone cannot clear G3a; a later corrected real preflight must pass independently. No Qwen,
enrollment, authority change, AAPL change, provider request, deployment or merge occurred.
Finance **RESEARCH / 0 SEK / NONE**.

## G3a – UnexpectedWritableMount: Root Cause Analysis and Proposed Correction

**REVIEW CHECKPOINT / ANALYSIS AND PROPOSAL ONLY / G3a BLOCKED / G3b NOT RUN.**
This amendment publishes the completed read-only analysis, not a security-policy change or a new
execution. It supersedes the earlier diagnostic NOT RUN/awaiting-approval status only to the extent
of the later operator report below. Historical failures and checkpoint evidence remain intact.
The same distinction applies to the earlier diagnostic-preparation wording in the checklist/runbook.

### Baseline, authority and evidence provenance

- Accepted `origin/main`: `70b659f772b7b7883a5716786859f1e7833a5634`;
  tree `6433c956ff87bc8434a9a8fff6c5294fe45b258d`.
- Existing branch: `review/finance-historical-qwen-deployment`. Before publication, local HEAD and
  remote HEAD both `439bbb05f36ee71e8b3846581e817f17770a7c82`;
  tree `19d023e1d6206719f18b70635cfa1112a17586bd`. Earlier bfa99fb/e97baa8 history is preserved.
- `git fetch origin`, ref/tree checks and tracked working-tree inspection repeated for publication;
  no tracked delta existed before these documentation edits. Unrelated untracked work is excluded.
- Resolve this publication's exact SHA/tree from Git metadata for subject
  `docs(finance): publish G3a mount isolation root cause analysis`, not a self-referential report hash.
- Repository evidence: AGENTS/START-HERE, relevant ARCHITECTURE and Accepted ADR0039/0040,
  [research contract](../../../architecture/finance/research-learning-contract.md),
  [G3 gates](../../../operations/finance-historical-qwen-g3-checklist.md), hosting runbook,
  STATUS/BACKLOG/recovery, both preflight/diagnostic scripts and units, their deployment tests,
  and the native worker's `containment.h`. No separate external Finance review was used.
- Read-only analysis independently confirmed local systemd version `257.13-1~deb13u1` and code
  behavior. Attempted reading of the existing private diagnostic result returned PermissionDenied;
  there was no privilege escalation. This publication does not repeat that inspection.
- Primary Linux/systemd sources below were consulted in the preceding analysis on2026-10-16.
  Upstream source semantics are not proof of this host's exact mount targets or Debian patch behavior.

### Operator-reported diagnostic outcome — not independently reproduced

After the previously published diagnostic-preparation checkpoint, the operator reports **exactly one**
approved separate diagnostic run on Debian13/systemd257.13. Systemd reported Result=success and
ExecMainStatus=0; the result reported status=DIAGNOSTIC_ONLY, policyOutcome=UnexpectedWritableMount.
There were65 mount entries and16 policy rejections:

| Filesystem | Count | Sanitized location class |
| --- | --- | --- |
| nsfs | 12 | Under `/run`, all with the same direct parent |
| tmpfs | 2 | Under `/run` |
| binfmt_misc | 1 | Under `/proc` |
| hugetlbfs | 1 | Under `/dev` |

All16 reportedly have rw in both per-mount and superblock flags. The host's `/`, `/run`, `/proc`
and `/dev` reportedly have shared propagation. The temporary diagnostic unit was reportedly removed;
the original preflight remained unchanged and was not restarted by this analysis/publication.
No private target names, namespace identifiers, raw mount records or payloads are published here.

DIAGNOSTIC_ONLY/exit0 means that collection completed, not that isolation passed. The diagnostic
compares its own namespace against the hash-pinned policy, substituting only its exact delegated
cgroup target for comparison. It does not reproduce the original failed namespace, migrate a child,
or establish G3a. Successful collection is not evidence of successful enrollment or authority matching.

### Verified policy behavior and its limits

`infrastructure/finance/historical-preflight.py:mount_policy` examines each mountinfo record's exact
target, filesystem type and per-mount rw flag. It permits rw only for:

| Filesystem | Exact permitted target |
| --- | --- |
| cgroup2 | The original maintenance unit's own delegated cgroup mount |
| Any type | The fixed preflight StateDirectory mount |
| tmpfs | `/tmp`, `/var/tmp`, `/dev`, `/dev/shm` |
| devpts | `/dev/pts` |
| mqueue | `/dev/mqueue` |
| proc | `/proc` |

All other rw records fail on the first match. The delegated rw cgroup mount is also mandatory.
Records without rw bypass this predicate. Therefore all four reported categories are rejected by
construction. This is the verified immediate failure mechanism, not proof of an exploitable host write.

The parser uses the correct target and per-mount fields, with the separator after optional fields.
It ignores superblock flags, mount-root/source, propagation relationships, effective DAC/ACL and
whether a record is hidden by another mount. It does not decode target escapes. No evidence selects
an escape/indexing error as this incident's cause. Since both flag sets reportedly contain rw,
confusing superblock rw with a read-only VFS mount does not explain these16 rejections.
[Linux mountinfo specification](https://man7.org/linux/man-pages/man5/proc_pid_mountinfo.5.html).

The diagnostic preserves all rows, targets, mount/parent IDs and selected flag sets; it intentionally
omits source/root/arbitrary options and propagation optional fields. It collects no target ownership,
ACL or actual-access evidence. Its stacked-mount fixture proves records are retained, not that a
lower mount is safe. The first offending original-preflight row is not established by the aggregate.

### Ranked causes and per-filesystem security assessment

1. **Strongest source-backed explanation: incomplete assumptions about systemd-created mounts.**
   Systemd257.13 `mount_private_dev()` binds `/dev/hugepages` if present. Its read-only treatment of
   private `/dev` does not automatically make all submounts read-only. If the reported hugetlbfs target
   is that standard path, this is a concrete creation mechanism without any late propagation.
   Exact host correspondence remains unverified.
2. **Possible: inherited mounts or later host-to-unit propagation.** A private mount namespace does
   not inherently reject incoming host mounts. Host shared flags support possibility, not timing or
   causation for any particular record. Existing versus later mounts cannot be distinguished here.
3. **Possible: hidden/overmounted records.** The policy reads every row rather than resolving actual
   access through the top mount. Systemd's remount implementation explicitly handles accessible
   top mounts; underlying records need not describe a reachable writable path. No such topology is
   yet established for these16 records.
4. **Weakly supported: parser error.** The known field handling explains rejection without one.

Relevant source implementations:
[systemd257.13 namespace.c](https://raw.githubusercontent.com/systemd/systemd/v257.13/src/core/namespace.c),
[systemd257.13 mount-util.c](https://raw.githubusercontent.com/systemd/systemd/v257.13/src/shared/mount-util.c).

| Type | Security meaning and remaining uncertainty |
| --- | --- |
| nsfs | Namespace handles, not ordinary writable data directories. Opening a handle does not grant permission to join its namespace. RestrictNamespaces=yes is intended to prohibit switching; setns has capability checks. Likely overclassification as file writability, but exact object, reachability and namespace ownership are unknown. A shared parent does not prove Docker origin. No blanket nsfs exemption is justified. |
| tmpfs | Can expose shared host storage, private storage or inaccessible replacements. Type/rw alone cannot distinguish them. Exact target, overmount relationship, parent traversal and DAC/ACL are needed. Neither all tmpfs nor all of `/run` may be allowed. |
| binfmt_misc | A binary-format control interface. Linux's register/status defaults restrict writes to the owner, but actual ownership/permissions and owning user namespace are unverified. Zero capabilities alone is not a substitute for checking access to existing control files. If beneath `/proc/sys`, explain rw against the explicit read-only protection before accepting anything. |
| hugetlbfs | PrivateDevices may bind an existing host resource, not create a private copy. Actual DAC/ACL and reachability are unknown. Access can consume HugeTLB resources; MemoryMax=4G alone is not proof of HugeTLB accounting/limits. Prefer denying unnecessary access rather than permitting this filesystem. |

[Linux namespaces](https://man7.org/linux/man-pages/man7/namespaces.7.html),
[setns permissions](https://man7.org/linux/man-pages/man2/setns.2.html),
[Linux binfmt_misc implementation](https://raw.githubusercontent.com/torvalds/linux/v6.12/fs/binfmt_misc.c),
[cgroup-v2 HugeTLB accounting](https://docs.kernel.org/admin-guide/cgroup-v2.html).

### Flags, propagation, masking and effective access

rw/rw removes two mount-level write prohibitions; it does not establish that UID1654 can perform a
particular write. Reachable path traversal, inode permissions/ACLs, filesystem operations, namespace
ownership and security filters also matter. Conversely zero capabilities/NoNewPrivileges do not
forbid normal writes already allowed by DAC. NoNewPrivileges prevents privilege gain, not all I/O.

A bind mount exposes existing objects at another path; it is not a private copy. Masking covers a
path with an inaccessible replacement. Propagation distributes mount/unmount events between
namespaces. None can be inferred solely from rw. ProtectSystem=strict is not a promise that every
API filesystem is read-only. ReadOnlyPaths does not automatically constrain later propagated mounts.
PrivateMounts=yes alone does not stop host-to-unit propagation. MountFlags=private can stop that
direction, but also stops host unmount events from reaching the unit, potentially retaining mounts
until its processes/namespace terminate.
[Version-specific systemd.exec contract](https://raw.githubusercontent.com/systemd/systemd/v257.13/man/systemd.exec.xml).

There are two distinct policy risks: it can reject harmless/inaccessible records, yet its accepted
names/types do not prove private backing or stable isolation. Systemd also binds `/dev/shm` and
`/dev/mqueue` in PrivateDevices; the existing allowlist accepts them. Their privacy/access must not
be assumed just because they are absent from the16 rejections. The runbook's blanket private/shared
temporary-storage claim is stronger than demonstrated evidence. Native Landlock/seccomp in
containment.h remains defense in depth, not proof of the controller's access or this diagnostic's
security context; no native containment/model bootstrap was executed by the diagnostic.

### Proposed minimum correction — NOT IMPLEMENTED OR AUTHORIZED

Prefer a unit-local reduction in exposure while initially retaining the original mount predicate:

1. Privately review the already collected targets and mount-parent/overmount relations, binding the
   result to its invocation and unit/script identities. Select only individually justified unwanted
   resources. Use `InaccessiblePaths=` for confirmed targets or a confirmed common parent, never
   a guessed private parent or a blanket writable exception. `/dev/hugepages` and
   `/proc/sys/fs/binfmt_misc` are standard-path candidates **only if the result confirms them**.
   The two tmpfs targets and nsfs parent are not established in this public evidence.
2. Proposed `MountFlags=private` in the bounded maintenance unit prevents future incoming propagation.
   It does not remove existing mounts; step1 remains necessary. Review retained-mount and cleanup
   consequences under the existing45s start/5s stop, KillMode=control-group and Restart=no limits.
   Do not change global host propagation, Docker namespaces or another service.
3. Keep mount_policy fail-closed. Masking must actually eliminate or safely restrict exposure; it is
   not guaranteed to remove every recorded rw row. If hidden records still cause failure, do not
   discard them by assumption. Any later narrow effective-access correction requires separate proof
   of the inaccessible topology, inability to uncover it, and negative tests for an accessible twin.

This is a conditional correction proposal, not a validated drop-in or a claim that adding two
directives necessarily resolves all16 entries. Missing target/access evidence is a concrete remaining
dependency, not grounds for a broader allowlist or a new architecture project.

| Exact repository file | Proposed bounded responsibility |
| --- | --- |
| `infrastructure/finance/bigbrain-historical-maintenance.service` | Individually justified restrictions and propagation setting; preserve identity/cgroup/capability/resource controls |
| `infrastructure/finance/bigbrain-historical-mount-diagnostic.service` | Keep equivalent isolation if reused under separate host-test approval |
| `tests/deployment/test_historical_preflight.py` | Prove unchanged rejection boundary and required unit protections |
| `tests/deployment/test_historical_mount_diagnostic.py` | Equivalent units, reported mount classes/stacked cases and exact cgroup substitution |
| `infrastructure/finance/historical-preflight.py` | No acceptance-policy change proposed merely to allow more types |
| `infrastructure/finance/historical-mount-diagnostic.py` | No change required to read existing evidence; bounded additional metadata only if a specifically missing fact requires separately approved collection |
| `docs/operations/runbooks/finance-historical-hosting.md` | Future reviewed installation/verification procedure and corrected privacy assumptions |
| This report, G3 checklist, STATUS/BACKLOG/recovery | Actual evidence/status only as the approved correction progresses |

Alternatives are limited to: (A) recommended unit-local access restriction plus justified propagation
containment; (B) access restriction without changing propagation, only if equivalent continuing
isolation is positively proven; (C) narrow semantic-policy handling for individually proven inaccessible
or non-writable objects. B retains a future-mount risk; C adds parser/access/topology complexity and
must not become an allowlist by filesystem or prefix. No alternative is approved by this report.

### Verification plan and severity

Risk remains **HIGH**. No verified P0 write/escape or data loss was found. **P1 remains** for effective
host-resource access and isolation stability on the selected execution path. Zero unexpected rows
is necessary for the unchanged predicate but insufficient by itself to prove the whole environment.

| Future check | PASS requirement | FAIL / STOP condition |
| --- | --- | --- |
| Focused policy fixtures | All reported classes remain rejected when exposed; exact required scopes accepted; escapes/stacked rows cannot silently bypass policy | Broad filesystem/prefix exception, hidden-row guessing or missing delegated mount accepted |
| Unit/static tests | Exact UID/GID1654, zero-capability/NNP controls, cgroup ownership/topology, fixed limits and timeout/cleanup preserved; diagnostic has only reviewed deltas | More privilege, broader writable cgroup or changed resource authority |
| Actual final mount context | Every previous rejection accounted for; denied resources absent/inaccessible; allowed scopes' backing/access verified, including shm/mqueue | Unknown path/overmount, unexpected writable host resource or only a host findmnt substitute |
| Propagation | Final namespace relationships satisfy chosen containment; harmless isolated fixture proves a late mount cannot introduce an unauthorized write surface | Incoming mount creates reachable write access or necessary cleanup is lost |
| Actual inert-child preflight | Parent remains controller; owned child enters exact worker; CPU200%,4GiB,swap0,pids32,oom.group1; kill/reap/empty checks pass | Wrong placement/limits, surviving child, cleanup failure, nonzero capabilities or lost NNP |
| Evidence binding | Current invocation and exact installed/effective unit/script identities, bounded private results | Stale/mismatched result, unreviewed drop-in, unknown installation or result treated as inference authority |

These are **NOT RUN** correction tests. Any future propagation fixture uses isolated harmless test
resources, never real Finance paths or writes to kernel control interfaces. Actual unit installation
and host execution need separate authorization. No write probe against binfmt_misc/hugetlbfs is proposed.
Publication reuses the unchanged checkpoint's10 diagnostic/11 preflight PASS results and static unit
verification only within their original scope; none proves corrected or successful host isolation.
No full Finance/API suite is justified for this documentation-only publication.

Exact outstanding evidence: existing private result invocation/hashes, target and mount-parent
relationships including overlays; for still reachable objects, UID/GID/mode/relevant ACL and traversal
in the service context; propagation optional fields if the propagation hypothesis must be resolved.
The existing diagnostic did not retain the latter access/propagation facts. Host metadata after exit
cannot recreate the vanished namespace. Request only missing decision-critical facts after reviewing
the retained result; no curiosity-driven repeat diagnostic or original-preflight restart.

### Gate disposition, next decision and publication checks

**G3a BLOCKED.** After a reviewed correction, actual isolation must pass. Final artifact/loader/model
identity, protected coordination/profile, source-bound enrollment, append-only independent journal
and RecurringAuthorityMatched remain required in the final security context. This report does not
perform or authorize them. Adding production data/artifacts later requires verification of that final
context; a preflight-only environment does not automatically prove it.

**G3b NOT RUN.** A separate execution phase remains necessary. The approved I1/E1/T3/C64/W33s and
30s inference cap are unchanged. Historical50.458s evidence is a timeout risk, not authority to increase
the cap, retry, refund or renew. No successful Qwen analysis is claimed.

Recommended architect decision: review the bounded unit-isolation proposal, retain G3a/P1 STOP,
and require existing private target evidence before approving exact restrictions. Owner approval is
then needed for implementation and any actual host test; this documentation publication supplies neither.
No generic investigation sprint, broad mount permission, new Finance feature or ADR is proposed.

Publication changes exactly this report, `docs/STATUS.md`, `docs/BACKLOG.md` and
`docs/operations/codex-recovery.md`. No application/test/script/unit/security-policy file changes.
No diagnostic/preflight/Qwen start, enrollment, grant/journal/DB mutation, production/AAPL change,
provider call, merge or deployment. Sensitive mount targets, raw reports, private configuration and
secrets are excluded. Finance remains **RESEARCH / 0 SEK / NONE**.

### Documentation publication verification

| Executed check | Result and scope |
| --- | --- |
| `node scripts/verify-documentation.mjs` | PASS:271 Markdown files,91 unique backlog IDs, documentation/relative-link validation |
| `git diff --check` and `git diff --cached --check` | PASS: no whitespace errors |
| Explicit staged-name comparison against the four documents above | PASS: exact authorized documentation scope; no application, test, unit, script, configuration or policy changes |
| Gitleaks8.28.0 `git --staged --redact --no-banner .` | PASS: no leaks found in staged changes |
| Sanitized diff review | PASS: operator evidence labeled; proposals not presented as implemented; no private mount targets, raw result, secrets or protected authority content added |
| Host diagnostic/preflight, model, application tests/build | NOT RUN: publication is documentation-only; previous focused evidence retains its original scope |

The final publication repeats staged whitespace/secret and documentation validation after adding this
verification record. Exact published commit/tree and remote verification are resolved from Git metadata
and the delivery message. Main remains unchanged; no CI or host-validation result is invented.


## G3a — private propagation preparation and unresolved target gate

**2026-10-16 — REVIEW CHECKPOINT; partial correction, NOT HOST VERIFIED; G3a BLOCKED / G3b NOT RUN.**
Owner authorized repository-only correction after the published root-cause analysis. Accepted main
was fetched and verified as `70b659f772b7b7883a5716786859f1e7833a5634`,
tree `6433c956ff87bc8434a9a8fff6c5294fe45b258d`. Starting review HEAD was
`31711a1dc1492e04876ed13b6ca03ba5bcbef883`, tree `9e67a2989e02c1ebd9005810a1d557704e9e7fc9`.
Tracked work was clean; unrelated design mockups and four Sentinel ADR drafts remain excluded.
Continue `review/finance-historical-qwen-deployment`; this additive checkpoint resolves by publication
subject `review: bound G3a mount propagation without policy relaxation` and its Git commit/tree metadata.
No earlier checkpoint is rewritten. No external Finance review report was used.

### Implemented scope and verified semantics

Only `MountFlags=private` is added to both
`infrastructure/finance/bigbrain-historical-maintenance.service` and
`infrastructure/finance/bigbrain-historical-mount-diagnostic.service`.
The local static verifier identifies systemd **257.13-1~deb13u1**. Version-pinned
[systemd257.13 execution documentation](https://github.com/systemd/systemd/blob/v257.13/man/systemd.exec.xml)
and [namespace implementation](https://github.com/systemd/systemd/blob/v257.13/src/core/namespace.c),
accessed2026-10-16, establish that the directive is supported: after namespace construction systemd
applies the selected propagation mode recursively. `PrivateMounts=yes` alone would keep incoming
host propagation and therefore is not the selected correction.

This change prevents later incoming/outgoing propagation across that private namespace boundary;
it does **not** remove mounts inherited during namespace construction, make inherited writable
mounts read-only, or prove DAC/namespace/capability isolation. It does not change global propagation
or grant a new writable host path. Actual final mount flags/topology must still be verified on host.
Private propagation also prevents receiving host unmounts; inherited mounts can remain pinned until
the service namespace is destroyed. The unchanged bounded oneshot/whole-control-group cleanup is
therefore essential. Do not export namespace handles or leave descendants holding the namespace.
Timeout settings are not proof of cleanup under a kernel hang; lingering processes/handles are STOP.

`historical-preflight.py` is byte-identical to the reviewed version, SHA-256
`97ecacefdeed8e95e7056557b0871384d5e4ab0b9194e7846679f4cffe04bbf6`.
No allowlist, parser, cgroup mutation, identity or failure behavior is relaxed.
The diagnostic reader changes **only its pinned required maintenance-unit checksum**, to
`212558051adef7b1d42afa3e825fed5fe15a97ef006e0bf0676bfa302f1967f3`.
It will refuse the old installed unit rather than falsely treating different isolation as equivalent.
Both unit files must correspond to this reviewed version for any separately authorized future test;
no installation or test is authorized by publication. The previous diagnostic procedure/hashes remain
historical evidence, not a runnable procedure for this new tree.

### Why target masking is still blocked

The earlier single capture remains **operator-reported**, not reproduced by Codex:65 mounts,
16 unexpected entries (12nsfs under/run,2tmpfs under/run,1binfmt_misc under/proc,1hugetlbfs under/dev),
all reporting rw in both sets of flags. Host shared propagation is also operator-reported.
The temporary diagnostic unit was reportedly removed; the installed preflight was not changed here.
Exact target identities, stacked-mount relationships, backing access and effective UID1654 access
remain unverified. Those aggregate counts do not establish that propagation caused every entry,
or that rw proves usable write access. Inherited or systemd-bound mounts can remain after this fix.

**No `InaccessiblePaths` entry is added.** Even a familiar filesystem type does not prove its actual
target or safe masking boundary. The next needed evidence is the exact targets/parent relationships
from the already captured private result, plus their required-use and effective-access assessment.
Do not publish private targets or collect a second diagnostic by default. If an exact safe mask is
established, it requires an explicit reviewed correction; otherwise retain the fail-closed blocker.
The current unit may still reject all16 entries. This is not a complete UnexpectedWritableMount fix.

### Focused verification and unchanged boundaries

| Executed check | Outcome |
| --- | --- |
| `python3 -B tests/deployment/test_historical_preflight.py -v` | PASS:15 tests,0 failures,0 skips. Includes16 synthetic mount-class cases, prefix/submount rejection, rejection before cgroup writes/child creation, original cleanup/identity checks and exact prior-unit hash after removing only the new directive. |
| `python3 -B tests/deployment/test_historical_mount_diagnostic.py -v` | PASS:11 tests,0 failures,0 skips. Exact equivalent isolation, unchanged policy hash, new unit pin and rejection of old unit bytes; bounded sanitized parsing retained. |
| `systemd-analyze verify infrastructure/finance/bigbrain-historical-maintenance.service infrastructure/finance/bigbrain-historical-mount-diagnostic.service` | PASS:exit0, no diagnostics; static verification only, neither unit started. |
| Production preflight, diagnostic, namespace/cgroup host experiment, Qwen, Finance tests/build | NOT RUN. No host action authorized; unchanged Finance code does not justify a full suite. |

The fixture test's printed `HistoricalIsolationPreflight:FAIL:PreflightSystemFailure` is an expected
negative-path test output, not a host preflight execution or failed test. Synthetic target names are
not claims about actual private mounts. No Finance/model/data access occurs in these tests.

Byte-level unit regression proves unchanged UID/GID1654, Delegate/DelegateSubgroup, NoNewPrivileges,
empty capabilities, network/device isolation, CPU200%, memory4GiB, swap0, TasksMax32,45s startup/5s
stop, KillMode=control-group, SendSIGKILL=yes and Restart=no. Worker controls and0700 enforcement
remain in the unchanged probe. Static evidence does not prove successful child migration on host.

### Review decision and remaining release evidence

Recommend reviewing this **partial propagation hardening only**, retaining P1 STOP for missing
actual host isolation evidence and unresolved inherited mount targets. No P0 violation is demonstrated
by the rw flags alone. Broad filesystem/prefix exceptions remain prohibited. No P1 is waived.
After target-specific review and separate owner authorization, a matched host installation/test must
prove: no unauthorized writable/accessible backing; private propagation in the service context;
unchanged controller/worker topology, effective identity, caps0/NNP1, exact limits; inert child
migration/kill/reap with empty worker and no leftover processes. Any mismatch or residual unexpected
mount is FAIL/STOP, never authority to broaden policy. The existing checklist still requires protected
artifact/loader checks, enrollment, correct append-only journal and `RecurringAuthorityMatched` in the
final context. None was performed here. G3b remains separately gated; I1/E1/T3/C64/W33s and30s model
cap remain unchanged, including the historical50.458s timeout risk. No model execution, deployment,
service start/restart, production mutation, AAPL change, grant/journal action or provider call occurred.
Finance remains **RESEARCH / 0 SEK / NONE**.


### Partial-correction publication checks

Documentation/link validation (`node scripts/verify-documentation.mjs`) PASS:271 Markdown files,
91 unique backlog IDs. `git diff --check` and staged whitespace/scope checks PASS.
Gitleaks8.28.0 `git --staged --redact --no-banner .` PASS:no leaks found.
The final staged snapshot is checked again after recording these outcomes.

Exact changed-file scope is11: the two units, the diagnostic checksum pin, the two deployment test
files named above, this existing report, `docs/STATUS.md`, `docs/BACKLOG.md`,
`docs/operations/codex-recovery.md`, `docs/operations/finance-historical-qwen-g3-checklist.md`, and
`docs/operations/runbooks/finance-historical-hosting.md`. The original probe, application code,
Finance stores, host configuration and unrelated work are untouched. Existing report-catalog links
remain sufficient; no new report/ADR or architectural authority is introduced. Private targets,
raw captures, secrets and protected-authority contents are excluded. This is a REVIEW CHECKPOINT,
not a Merge Candidate. No new CI or runtime success is claimed. Remote HEAD/main identities are
verified at publication; exact new commit/tree are reported from Git metadata.


## G3a — existing diagnostic access blocked

**2026-10-16 — DOCUMENTATION-ONLY REVIEW CHECKPOINT; G3a BLOCKED / G3b NOT RUN.**
Fetched accepted main: `70b659f772b7b7883a5716786859f1e7833a5634`,
tree `6433c956ff87bc8434a9a8fff6c5294fe45b258d`. Local and remote review HEAD both matched
`351f25fe125f64322e4ebf5524f32a8ef1712e6e`, tree `40612a0f8e90f5c750f9cdc9c6915c9aa60ee69b`.
Tracked source was clean. The existing design mockups and four unrelated Sentinel ADR drafts were
preserved. This additive publication on `review/finance-historical-qwen-deployment` resolves by
subject `docs(finance): record protected mount diagnostic access blocker` and Git SHA/tree metadata.

### Actual evidence and stop

One attempt to open the owner-designated existing protected diagnostic result with current process
permissions, using `O_RDONLY | O_NOFOLLOW | O_NONBLOCK`, failed with **PermissionError/EACCES**.
No bytes were read. No raw content, mount targets or private metadata were printed, copied or logged.
No sudo, alternate identity, container access, permission change or new diagnostic was attempted.
Inspection stopped at this boundary, as explicitly required by the owner.

This verifies only that the current process cannot open that path. It does not independently verify
the file's current existence, owner/mode, contents, completeness or which ancestor denied traversal.
The previous65-mount/16-rejection result remains operator-reported:12nsfs,2tmpfs,1binfmt_misc,
1hugetlbfs. No category's exact targets, parent/stack relationships, effective UID1654 access,
origin (inherited/systemd-created/propagated), or suitability for masking was verified in this task.
There is no new root-cause conclusion and no individually justified `InaccessiblePaths` proposal.

### Minimum next decision and correction boundary

Separately authorize a **local read-only review by an already privileged administrator** of the
same captured file, in a private local terminal. Keep the file and its protection unchanged; no
copy to a less protected location, raw terminal transcript to Codex/Git, or new diagnostic run.
The administrator needs to verify capture status/invocation and recorded script/unit identities,
then privately correlate all16 rejected records with their exact targets, mount/parent IDs,
filesystem and mount/superblock flags and any stacked entries. For each candidate restriction,
retain the exact target and justification privately; return only a sanitized assessment of whether
it is required by this preflight, what masking would hide, and which access/origin facts remain unknown.
A snapshot of mountinfo alone cannot prove effective access or timing of propagation; missing
facts must remain unknown rather than inferred from rw or filesystem type.

Only after that evidence supports an individually safe target restriction can an explicit correction
be proposed for the maintenance and equivalent diagnostic units. If an exact directive contains a
private target, its disclosure/configuration location needs review before any publication. Do not
substitute a generic prefix or filesystem allowlist. The existing repository-only `MountFlags=private`
preparation and unchanged fail-closed probe remain as reviewed at351f25f; nothing is installed here.
Any implementation or host test remains separately authorized. No broad investigation or automatic
rerun is requested. Missing target evidence and actual isolation proof remain P1 blockers, not a
newly demonstrated P0 violation. G3a still also requires its existing artifact/authority gates;
G3b remains NOT RUN. Finance remains **RESEARCH / 0 SEK / NONE**.

### Verification scope

The access check is **BLOCKED**, not PASS. Category analysis and new target-specific correction are
**NOT PERFORMED** because no diagnostic content was readable. The previous26 focused tests and
static unit verification retain their original scope: no implementation changed, so they are not
rerun or presented as host proof. No preflight, diagnostic, systemd installation/start, Qwen,
Finance/API/AAPL/grant/journal/database operation, merge or deployment occurred.
Only this report, STATUS, BACKLOG and recovery are updated. Documentation/link, whitespace, exact
four-file staged scope and staged Gitleaks verification are required before publication; results are
recorded below. Existing report catalog, runbook, G3 checklist and ADRs require no change: their
security contracts and blocked gate remain correct.

Publication checks: `node scripts/verify-documentation.mjs` **PASS** (271 Markdown files,91 unique
backlog IDs, relative-link validation); `git diff --check` and `git diff --cached --check` **PASS**;
explicit staged-name comparison **PASS** (exactly four documents, no unstaged tracked delta);
Gitleaks8.28.0 `git --staged --redact --no-banner .` **PASS**, no leaks. Final staged checks are
repeated after this evidence entry. No code tests/build or host checks are claimed for this publication.
