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
