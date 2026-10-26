# Restricted BigBrain operations wrapper

Status: **REVIEW CHECKPOINT — NOT INSTALLED**. Baseline
`4647cd8119edb5b7cd0872639fd79ba26a38b0d3`; branch `ops/approved-operations-wrapper`.
Implementation and offline evidence dated 2026-10-26. This document grants no installation,
sudoers, deployment, restore or source-fetch permission. Check [freezes](../freezes.md).
Finance and BB-158 remain frozen. No application/runtime configuration is changed here.

## Purpose and trust boundary

[The wrapper](../../../scripts/approved-ops/bigbrain-approved-ops) is an approval gateway
for the existing six-database backup and explicitly registered deployment contracts. It does not implement another
Compose deployment engine. Only four literal operations are accepted, without additional arguments:

| Operation | Root-held authority | Result |
| --- | --- | --- |
| `verify-backup` | Backup ID and manifest SHA-256; expires within 24 hours | Integrity/FK/schema/hash/readback of the six retained copies; no production DB reads |
| `backup-approved` | Exact API container, image, revision and five-volume mapping; single use | New private SQLite online backups, manifest and private result receipt |
| `start-approved` | Separate deployment approval, exact contract, package, revision, fixed service scope, backup and dependency hashes; single use | Submit existing `work`/`recover` contract to bounded systemd unit; **SUBMITTED is not DEPLOYED** |
| `status` | Installed wrapper and protected receipts | Fixed state labels only; no paths, manifest, environment, data, recovery inputs or raw logs |

**enigma already has Docker-group access to the rootful daemon and is not OS-isolated.**
This gateway removes the need to grant general passwordless sudo; it cannot protect root-owned
files against a malicious user exercising root-equivalent Docker access. No Docker bypass is
used here. A separate agent identity without Docker, broad sudo or write access to root-executed
files is a future hardening decision, not implemented by this checkpoint.

Root, the OS Python interpreter/stdlib/shared libraries, Docker daemon and administrator-approved
worker code are trusted. A package hash proves integrity, not that arbitrary Python is safe.
The administrator must independently review **all** package code and side effects before sealing
an approval. This includes `runner.py`, `backup.py`, `verify-backup.py`, Compose inputs, healthchecks,
container images, mounts, environment changes and recovery. This is not a sandbox for arbitrary
agent-generated programs. Root/admin modification while an approved operation is running is outside
the threat model. External operators must not run an old deployment concurrently.

## Reused mechanisms and boundaries

[backup_support.py](../../../scripts/approved-ops/backup_support.py) retains the online-backup,
inventory and retained-verification functions from `scripts/backup-school-lunch-api.py` at
[operations checkpoint 7971fa2](https://github.com/T-bear/BigBrain/blob/7971fa2c8cb2345d6bcd71627923a7d2e33417b9/scripts/backup-school-lunch-api.py).
The old revision/image constants, CLI, Docker inspector and runtime-plan CLI parsing are removed;
wrapper approvals now bind those identities. The old failure-path print of the private backup path
is removed. Backup algorithms, six-file inventory, WAL handling and per-database consistency remain.
Mechanical AST comparison confirms preservation of all 12 retained definitions, except that
private-path print. This reuse does not accept other code on that operations branch.

Only these files in the five exact mounts are backed up:

| Mount | Files |
| --- | --- |
| `/data` | `meal-planner.db`, `audiobook-acquisition.db` |
| `/shopping-data` | `shopping-list.db` |
| `/calendar-data` | `calendar.db` |
| `/settings-data` | `settings.db` |
| `/lifecycle-data` | `lifecycle.db` |

Sources come from read-only Docker inspect and must equal the approval's container ID, image,
revision and named-volume mappings. Nested/overlapping mounts, symlinks, unexpected source files,
missing DBs and identity drift fail closed. SQLite sources use `mode=ro`, `query_only` and online
backup, never immutable on live sources or a manual WAL checkpoint. No Finance data is read.
Each DB is consistent independently; no claim of cross-database atomicity or production restore
validation is made. Files are limited to 256 MiB, backup progress to 120 seconds and wrapper to a
180-second signal deadline. Incomplete sets are retained, never promoted or automatically removed.

## Reviewed deployment contract register — P1 follow-up

Review of checkpoint `f819ec739c18972768d1fb5a107d8756ceed2bdd` found that shared gateway code
hardcoded the SchoolLunch v3 API+Web worker. This follow-up separates that interface into a small,
immutable `CONTRACTS` mapping compiled into the root-installed wrapper. No JSON/plugin registry,
new installer or deployment engine is introduced. The four operations and sudoers file are unchanged.

Common code continues to enforce root ownership, directory-chain/symlink checks, exact approvals,
package/dependency hashes, immutable execution snapshots, backup/runtime identity, single-use
claims, concurrency interlock and sanitized status. Each register entry supplies its exact service
set/order, package file set, unit/run prefix, worker/actions/timeouts, verification and recovery
requirements, external gates and commit-bound provenance. Only the root-held approval selects an
entry. The plan's hashed `contract` field must match that selection; plan targets/candidates and
both Compose files must match its service set. An API+Web approval cannot be narrowed to API-only.

| Contract | Fixed services / package / execution | Verification, recovery and external gates | Availability |
| --- | --- | --- | --- |
| `school-lunch-v3-systemd-v1` | API then Web; seven files listed below; `school-lunch-v3-` prefix; `runner.py work` / `recover`; 480/180 second deadlines | Existing exact-runtime/config/volume/backup checks, quiescence and clean-stop, health/data identity and unchanged other services. Restart exact old cleanly stopped container only before replacement; unknown replacement requires manual review, no DB restore. Consumers paused; source checks separately authorized and supplier interval established. | Interface supported; **not** a production approval |
| `api-only-diagnostics-v1` | API only; package structure/worker/recovery unverified and deliberately unset | Requires API-only health/config checks, unchanged Web/other services and flags/cache policy, no forced school fetch, verified last attempts or six-hour no-fetch window, current backup and phase-aware recovery. | **BLOCKED / disabled**; rejected before any Docker/systemd call or claim |

Registry requirements describe the existing worker's obligations and administrative review gates;
their text is not executable validation or evidence that a gate has passed. Common checks enforce
integrity and service scope, while the individually reviewed worker must enforce its own preflight,
configuration/health checks and recovery. A Boolean or a contract label does not prove maintenance,
permission to contact sources or a safe supplier interval. Package-code review remains mandatory.

**Verified existing routine:** the
[SchoolLunch v3 worker at 7971fa2](https://github.com/T-bear/BigBrain/blob/7971fa2c8cb2345d6bcd71627923a7d2e33417b9/scripts/deploy-school-lunch-v3.py)
defines `ROLES = ('api', 'web')`, exact package coverage, both-service preflight/verification,
`work` and phase-aware `recover`, and the prefix in its package/cgroup assertions. The gateway
continues to invoke `python3.13 -IS runner.py work <protected-run> <package-hash>` with `recover`
in `ExecStopPost`, never the old `launch` action. Its `load` accepts the additional plan contract
field without changing its existing invariants. No old production package is reapproved or edited.

**API-only evidence gap, not a guessed implementation:** the
[diagnostics checkpoint f631f09](https://github.com/T-bear/BigBrain/blob/f631f09bb315b356d7681bdecd80e14b88af89c5/docs/reports/features/family/school-meals-preparation-20261023.md#skollunch-diagnostics--deployment-preflight-blocked-2026-10-26)
contains a report-only deployment delta, explicitly no prepared executable package. It records
unverified latest source attempts, backup identity/freshness and replacement recovery. Its proposed
API-only continuation is a plan, not a verified worker. The v3 worker's `functional` calls school
endpoints and its service scope includes Web, violating diagnostics-only constraints if reused.
The earlier
[v2 worker at 2cc6bbe](https://github.com/T-bear/BigBrain/blob/2cc6bbe2611a9a67d4dad35b66a898ec2d9c00c8/scripts/deploy-school-lunch-v2.py)
also fixes API+Web. Main's appliance lifecycle procedure operates the stack and supplies no
verified API-only contract. These are Git/code findings, not fresh production checks. No frozen
Finance/BB-158 procedure is promoted into authority for this task.

Consequently this review fixes gateway/contract coupling but does **not** claim the approved
API-only diagnostics deployment is now runnable. The missing API-only executable/recovery
contract and its interval/backup evidence remain the explicit blocker. Minimum next step:
provide an existing reviewed API-only routine with evidence, or separately authorize a bounded
adaptation of the existing worker and its offline fault tests. Only after independent review,
exact-code owner approval and administrator installation can the reserved entry be enabled and
a separate production approval sealed. No new procedure is improvised here.

The agent cannot add/enable contracts through arguments, approval fields, package files or environment.
Register changes require a separately reviewed wrapper-code change and root installation. The mapping
and all nested entries are immutable; additional approval keys/package files are rejected. Installed
code remains root-protected, subject to the existing Docker/root trust limitation stated above.

## Protected installation and approvals

Designated paths (not created by this implementation task):

| Path | Owner / permissions | Purpose |
| --- | --- | --- |
| `/usr/local/sbin/bigbrain-approved-ops` | root:root 0755 | Fixed Python 3.13 isolated-mode executable |
| `/usr/local/libexec/bigbrain-approved-ops/backup_support.py` | root:root 0644 | Byte-hash-pinned backup core |
| `/etc/bigbrain-approved-ops/` | root:root 0700 | Three operation approval JSON files |
| `/etc/bigbrain-approved-ops/packages/<id>/` | root:root 0700, files 0600 | Administrator-reviewed immutable package inputs |
| `/etc/bigbrain-approved-ops/docker/` | root:root 0700, empty | No agent Docker config/plugin/credential injection |
| `/var/lib/bigbrain-approved-ops/` | root:root 0700 | Lock, claims, active interlock, backup receipts |
| `/var/lib/bigbrain-approved-ops/claims/` and `runs/` | root:root 0700 | Durable unique claims and private worker inputs/results |
| `/var/backups/bigbrain-school-lunch/` | root:root 0700 | New timestamp/UUID backup sets, files 0600 |

All ancestor directories must be root-owned and not group/other writable. No symlinks or hardlinked
regular inputs. Descriptor-relative `O_NOFOLLOW` traversal checks ownership/modes before reads;
only bounded regular files are accepted. Verified package **bytes** are copied into an exclusive
root-only run directory; no reread of an agent-controlled original is used for execution. The
runner receives only that snapshot. Root-owned dependencies cannot be swapped by the caller
between verification and execution under the stated trust model. Approval JSON rejects duplicate
keys and unexpected fields; caller has no approval path, SHA, environment or executable argument.

Approval files are `<operation>.json` for the first three operations. Each has exactly the common
fields `schema: 1`, `operation`, `id` (32 lower-case hex digits), `expiresAt` (future Unix seconds,
maximum 24 hours), plus the operation-specific fields below. No real approval is included in Git.

| Operation | Additional fields |
| --- | --- |
| `verify-backup` | `backupId` (`YYYYMMDDTHHMMSSZ-<32hex>`), `manifestSha256` (64hex) |
| `backup-approved` | `revision` (40hex), `runtime: {containerId, image, volumes}`. `volumes` maps all five destinations to exact `Type`, `Name`, `Source`, `Driver`, `RW` from current Docker inspect |
| `start-approved` | `revision`, `services` (exact registered order/set), `packageSha256`, `backupId`, `manifestSha256`, `dependencies`, `contract` (enabled register key), `usersPaused: true` |

`dependencies` maps **exactly** `/usr/bin/python3.13`, `/usr/bin/docker`, `/usr/bin/systemd-run` and
`/usr/libexec/docker/cli-plugins/docker-compose` to their SHA-256. OS updates require renewed
review of these hashes; no silent dependency update. Interpreter/stdlib and loader ownership are
also administrator prerequisites because Python cannot validate its interpreter before startup.
The fixed deployment contract requires the actual Compose installation at that protected path;
no caller override or fallback to an agent plugin is permitted.

A deployment ID identifies `/etc/bigbrain-approved-ops/packages/<id>/`. For the supported
API+Web contract the directory contains only
`package.json` and these seven files: `runner.py`, `plan.json`, `release.compose.json`,
`rollback.compose.json`, `backup.py`, `backup-runtime.json`, `verify-backup.py`. `package.json` maps
exactly those seven names to SHA-256. Approval binds its byte hash. `plan.run` equals approval ID;
`plan.revision` equals approved revision; `plan.contract` must equal the root-approved register key.
Targets/candidates and both Compose service sets must equal the registered services. API-only has
no permitted package structure until its missing routine is independently verified. Omitting a
contract ID does not fall back to the old SchoolLunch-specific behavior.
The root-mode worker audit must also check its own subprocess environment: it must use the
protected empty Docker configuration and the pinned Compose plugin, without loading extra
executables or agent-owned inputs. The old worker's reduced `ENV` is not proof of that contract.
Private files may contain configuration secrets; they never belong in Git or public output.
All executables/data subsequently consumed by the reviewed worker must be audited; immutable
original-config hashes may compare existing private configuration, but must not execute it.

## Administrator's one-time actions — only after separate approval

1. Review the published exact wrapper tree, tests and this boundary; independently retrieve it
   into root-only staging. Do **not** execute or blindly install an agent-writable checkout as root.
   Verify staged SHA-256 against the reviewed tree (including the support hash embedded in wrapper).
2. Check `namei -l`/ownership/ACLs for all destination ancestors, Python interpreter/stdlib/loader,
   systemd, Docker and Compose. Reject symlinks, writable code/config chains and unexpected plugin
   locations. Stop if host layout differs; do not relax the wrapper or grant general sudo.
3. Create only the protected directories above, with `install -d -o root -g root -m 0700`; keep
   standard `/usr/local/sbin` and `/usr/local/libexec` ancestors root-owned and non-writable by agent.
   Install reviewed wrapper 0755 and support 0644 with `install -o root -g root -m ...` from root-only
   staging. Never recursively chmod/chown existing backup sets or production volumes.
4. With **no** approvals installed, verify status returns `NO_WRAPPER_RUN`; the three privileged
   operations must deny. Perform an isolated administrator acceptance test with a harmless fixture
   worker before approving any production package. Offline mocks do not prove host systemd behavior.
5. Review [sudoers.example](../../../scripts/approved-ops/sudoers.example), run `visudo -c -f` on the
   staged file, and only then install it as `/etc/sudoers.d/bigbrain-approved-ops` root:root 0440.
   Validate full policy with `visudo -c` and `sudo -l -U enigma`. The four exact command/argument
   matches use `NOPASSWD: NOSETENV`; no wildcard, generic interpreter, Docker or systemd-run grant.
   Retain sudo environment filtering. Test extra arguments/other operations/SETENV are denied.
6. For **each** separately owner-approved operation, review current runtime and gates, seal fresh
   root-owned approval/package files atomically from root-only staging, then expose only the fixed
   command to the agent. No agent command may create, replace or retire an approval. Root approval
   is evidence of the administrator's translation of an owner decision, not self-authorisation.

After those separate actions, the only permitted caller commands are:

```sh
sudo -n /usr/local/sbin/bigbrain-approved-ops verify-backup
sudo -n /usr/local/sbin/bigbrain-approved-ops backup-approved
sudo -n /usr/local/sbin/bigbrain-approved-ops start-approved
sudo -n /usr/local/sbin/bigbrain-approved-ops status
```

These are interface examples, **not instructions to deploy this checkpoint**. Verify-backup may
repeat within its approval window; backup/start are single-use. Source data and retained backup
paths/hashes remain private. An administrator can read the root-only backup receipt and manifest
for independent verification without rereading production. Status reports durable worker evidence,
not a fresh health check, and never expands arbitrary worker/error text. `active.json` now binds
the contract as well as the run ID; status obtains the run prefix from that registered contract.
Old/unknown/missing contract metadata fails closed. No deployed wrapper state is migrated here;
preserve any old artifacts and let an administrator inspect them rather than infer a contract.

## Failure, concurrency and rollback

A global nonblocking flock serializes wrapper calls. Backup/start write an exclusive claim and
fsync it **before** work. The private claim retains the full approval and its digest for audit,
even when the administrator later replaces the current approval file. Start also persists `active.json` before submitting systemd, blocking
subsequent backup/start even after the caller disconnects or the unit exits. A timed-out submission
may already have started: no blind retry, no automatic approval reset. Already claimed backup IDs
cannot be reused; incomplete backups stay available for administrator inspection.

Submission uses `--no-block`; the returned receipt records only queued submission.
The transient unit is fixed: root, oneshot, start timeout 480s, stop/recovery timeout 180s,
control-group kill, no restart, umask 0077, core dumps disabled. Standard output/error are discarded;
private persistent worker receipts are the diagnostic source. Wrapper subprocess output is bounded
at 4 MiB, command deadlines kill/reap the child process group, and public errors are fixed sanitized
JSON. Python uses `-IS` (isolated mode, no site/customization imports); caller environment is cleared and executable paths are fixed. No shell.

Only the existing worker may recover automatically: restart the exact old container after a
verified clean stop where replacement has not begun. Unknown replacement/data state remains manual;
no database restore or automatic image downgrade. Administrator checks systemd terminal state,
`state.json`, `complete.json`, `recovery.json`, all runtime identities and any timed-out Docker
operation before retiring the active interlock. Preserve claims/results permanently; a subsequent
deployment needs a **new** ID and separate approval, never reuse or edit an attempted package.

To revoke wrapper access, the administrator removes only its sudoers entry and validates sudoers.
Do not kill a running unit or remove its inputs/recovery code. Let its bounded recovery complete,
then inspect receipts. Disable unconsumed approvals by moving them into root-only custody. Keep
backups, claims and all execution artifacts. For wrapper-version rollback, restore independently
verified previous root-staged wrapper/support together after no unit is active and revalidate the
embedded hash; this does not roll back application images or data. There is no restore operation
in this interface. Root administrative recovery remains possible without passwordless wrapper access.

## Offline verification and remaining evidence

[Tests](../../../tests/deployment/test_approved_operations.py): **40 PASS**, command:

```sh
python3 -m unittest discover -s tests/deployment -p test_approved_operations.py -q
```

Synthetic six-DB WAL backup and retained readback; all four operations; altered runtime/image/health/
volume; unexpected files; FK failure; expiry; invalid/extra args and duplicate JSON; traversal,
symlink/hardlink/FIFO/writable-chain rejection; package/dependency tamper; unauthorized services and
Compose side inputs; immutable-byte execution snapshot; concurrent calls; consumed ambiguous launch;
persistent interlock; sanitized status/error; subprocess environment/output/time bounds.
The previous 33 tests remain; seven new regressions cover unknown/disabled contracts, API+Web
packages presented as API-only, unauthorized service subsets/supersets, resealed mismatched plan
contracts, API-only plans under the API+Web contract, caller-supplied registry/enable attempts, and
status without a valid contract. The blocked API-only entry has no positive execution test because
no safe executable contract exists; denial is the required result.
Docker/systemd are mocked. Subprocess limit tests run only local isolated Python children.
No production DB, Docker mutation, systemd unit, supplier HTTP or sudo policy was exercised.

Security self-review: callers cannot choose executable paths or inject arguments; root-sealed package
code is explicitly the privileged trust boundary, not treated as untrusted sandbox input. Backup
core identity is pinned; all paths and ancestor protections are checked; launch ambiguity remains
blocked rather than retried. No real production approval/package is supplied. Installation acceptance,
OS dependency audit and each production worker's root-mode/side-effect review remain mandatory.
Documentation/scope/secrets checks are recorded in the checkpoint handoff; application test suites
are not relevant to this host-tool-only change. Independent security review is required before
merge/installation. This checkpoint changes no runtime or permissions.
