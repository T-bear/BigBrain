# Historical Finance hosting — preparation and administrator gate

## Status and scope

**2026-10-16: installed preflight failed; NOT READY FOR ENROLLMENT OR INFERENCE.**
Earlier checks and operator reports established217/USER resolved, then `UnexpectedWritableMount`.
The operator subsequently reports one separate diagnostic capture (65 mounts/16 rejections), followed
by removal of the temporary diagnostic unit. No successful G3a host preflight exists.
The current repository prepares `MountFlags=private` in both units, statically verified on systemd257.13.
This blocks later propagation, not inherited mounts. No exact `InaccessiblePaths` targets are yet
justified. The installed maintenance unit is not changed by this checkpoint. The diagnostic now pins
the updated unit and refuses the old one; do not mix versions or repeat the historical procedures below.
Next is architect review of the [partial correction and missing target evidence](../../reports/features/finance/historical-qwen-operation-20261016.md#g3a--private-propagation-preparation-and-unresolved-target-gate).
Installation and any host test require separate approval. No new host action is authorized here.
G1/G2 are complete for the accepted API; G3a remains BLOCKED and G3b NOT RUN under
[FIN-OPS-G3-20261016](../finance-historical-qwen-g3-checklist.md).
Accepted application: `70b659f772b7b7883a5716786859f1e7833a5634`.
This runbook implements no new Finance behavior, scheduler or model supervisor.
The [historical contract](../../architecture/finance/research-learning-contract.md#explicit-historical-operator-path-separate-operational-approval-required)
and its I1/E1/T3/C64/W33s limits remain authoritative. No inference is authorized by a preparation command.
[Observed host evidence](../../reports/features/finance/historical-qwen-operation-20261016.md#hosting-preparation--2026-10-16).

## Exact Docker constraint

The current API has a private cgroup namespace, a read-only cgroup2 mount, root-owned0755 cgroup
root and root-owned0644 `cgroup.procs`. Its root contains API processes and has no enabled subtree
controllers. A worker initially inherits its parent's cgroup: the parent remains alive, so that group
cannot pass the worker's **empty dedicated group** check. Container-wide memory/CPU limits are not
an equivalent substitute.

Mounting an unrelated host leaf does not solve migration. With `nsdelegate`, source and destination
must be reachable from the writer's cgroup namespace. Migration also needs write permission on the
common ancestor's `cgroup.procs`. Merely switching to the host cgroup namespace or chmodding a leaf
is insufficient. Domain controllers require an empty parent before controller/worker child leaves can
be enabled. Do not move the running API or delegate system.slice/root to work around this.
See [kernel delegation/migration rules](https://docs.kernel.org/admin-guide/cgroup-v2.html).

Docker currently uses the systemd driver and a delegated container scope, but its default container
mounts do not expose a usable delegated subtree to the application. `cgroup_parent` selects placement,
not a worker-specific delegation contract. A custom slice is not itself a delegated service/scope;
manual competing ownership violates systemd's single-writer model. See
[Docker driver/placement](https://docs.docker.com/reference/cli/dockerd/),
[Docker v2 namespace defaults](https://docs.docker.com/engine/containers/runmetrics/), and
[systemd delegation](https://systemd.io/CGROUP_DELEGATION/) (accessed2026-10-16).

## Prepared Compose fragment — do not apply

[infrastructure/finance/compose.historical-preparation.yaml](../../../infrastructure/finance/compose.historical-preparation.yaml)
is a **mount/hardening/configuration preparation fragment**, not an inference-ready override.
It merges only the API service; provider/mapping settings are inherited unchanged. Model and research
scheduler stay disabled, maintenance paused. Missing bind sources cannot be created silently. Image
must be the exact verified accepted digest, not an unverified tag. The fragment deliberately does not
mount any cgroup or solve the topology above. Do not run `up` with it against the live API.

Private environment variable names are declared in the fragment. Store values only in an ignored
mode0600 local file, never in Git. Bundle/model mounts are read-only. Coordination is a persistent
UID1654-owned0700 directory; do not use ephemeral tmpfs or clear a retained reservation after a crash.
Authority is a distinct root-owned0750 directory with group1654, outside database/restore roots.
Blank approval timestamps, evidence, checksum or custody decision remain **unconfigured**, not approval.
Profile timestamps must be actual UTC approval/expiry times at final provisioning, not copied fixtures.

Render only to a private file or use `config --quiet`; never print interpolated production environment:

```sh
docker compose --env-file <private-existing-config> --env-file <private-historical-config> \
  -f compose.yaml -f infrastructure/finance/compose.historical-preparation.yaml config --quiet
```

No change to live `.env`, no API recreation and no profile activation is part of this preparation.

## Artifacts and safe verification

The supplied worker hash verifies its bytes but it predates historical/finite/recurring prompt support.
Do not label it as the accepted historical worker. Build the unchanged accepted `worker.cpp` against
retained accepted llama headers/libraries into a **new private staging location**, never overwrite the
previous worker/evidence. Use the existing compiler command with `$ORIGIN/runtime` RUNPATH. Record
accepted source SHA, compiler command, worker hash and every library hash privately.

The API image lacks `libgomp.so.1`. Include the verified existing OS library in the private staged
runtime bundle; this changes no application image or provider configuration. Check the full dependency
closure using `/lib64/ld-linux-x86-64.so.2 --list <worker>` inside the exact image, with no network,
read-only mounts and no capabilities. This invokes the loader only, not worker main or model loading.
The artifact bundle must become root-owned and nonwritable by the runtime before use. Keep the pinned
GGUF hash `b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897`.

## Minimum proposed safe execution placement — administrator action

To preserve the running AAPL container, the smallest identified alternative is a **bounded systemd
maintenance invocation of the same accepted `BigBrain.Finance.Hosting.dll`**, not a new inference
service or scheduler. It retains OwnedReasonerWorker's direct-child/pidfd/kill/reap ownership and all
Finance authority checks. This placement is proposed, not installed or proven on this host yet.

On the observed systemd257, an administrator can provision one dedicated service with `User=1654`,
`Group=1654`, `Delegate=cpu memory pids`, `DelegateSubgroup=controller`, `NoNewPrivileges=yes`, empty
capability bounding/ambient sets, no restart policy/timer, and a bounded runtime. Query actual
`ControlGroup` from systemd; do not infer a private host path. With the controller in its child,
the delegated parent can enable cpu/memory/pids and contain a separate empty `worker` child.
Only that delegated subtree may be writable; other cgroups remain inaccessible/read-only.
No root helper or systemd unit is installed by this checkpoint.

Provision and verify worker directory UID1654/mode0700, empty `cgroup.procs`, and exact values:

| File | Required value |
| --- | --- |
| cpu.max | `200000 100000` |
| memory.max | `4294967296` |
| memory.swap.max | `0` |
| pids.max | `32` |
| memory.oom.group | `1` |

Before enrollment, prove unprivileged **owned-child migration and parent kill/reap** using an inert
probe in that exact final topology, with no GGUF, grant or Finance write. Verify the parent stays in
controller, child enters worker, limits hold, worker leaf becomes empty after exit, and capabilities
remain zero/NoNewPrivs1. No real worker/model invocation is a substitute for this preflight. If this
cannot be proven, stop. Do not loosen containment or attach Qwen to an existing populated cgroup.

Use the same accepted application artifacts, private config, Finance/lifecycle mounts/paths and
Sentinel Unix-socket identity as the current maintenance command. Root must resolve access without
making private keys/world data readable. No application server starts. Native/library compatibility
must be checked in the final execution environment; the Docker loader check alone does not prove a
host-launched maintenance process. Exact-profile identity binds these final paths, so generate it only
after placement is settled. A new wrapper/scheduler or changed OwnedReasonerWorker is not proposed.

## Enrollment, journal and authority-check order

1. Administrator verifies independent custody, restore exclusions, completed isolation/resource probe,
   immutable artifacts, private persistent coordination and unchanged AAPL state. Verify no existing
   authority/program/reservation can be replaced. Do not create an empty journal to satisfy a path check.
2. Use `finance-historical-inspect` read-only. Bind actual source commitment and the approved finite
   budget to the final profile identity from `finance-recurring-profile-identity`. No inferred timestamps.
3. Run `finance-historical-enroll <private-grant-json> <sha256:raw-file-hash>` once under the contract's
   maintenance/disabled-research gates. Save exact output privately; no provider/model runs. Existing
   API AAPL collection need not be disabled. Do not enroll before the protection environment is ready.
4. Root exclusively creates the journal from the exact enrollment record plus final newline, matching
   enrollment checksum/program/grant. Use no-overwrite creation and root:1654 mode0660. Flush file and
   parent directory, set `chattr +a`, flush again. Verify ext4, regular single-link file, append flag,
   protected root-owned ancestors, no symlinks, and no runtime ability to truncate/unlink/replace it.
5. Configure exact enrollment checksum and durable custody decision. Under the **same final execution
   UID/caps/mounts**, run `finance-recurring-authority-check` in a separate maintenance process with
   **all six local command gates false**, maintenance true. These process-local overrides do not change
   the running API's AAPL configuration. Require `RecurringAuthorityMatched`. Never disable production
   AAPL merely to satisfy this diagnostic. This command is read-only and starts no host/provider.
6. STOP: readiness only. No `finance-historical-run` during this task. Later separately instructed run
   enables the reasoner for that invocation only, never changes the budget or restarts a spent attempt.

The known recovery set contains Finance/lifecycle named-volume roots, owner-drop and selected metadata;
the present journal directory is outside those roots and the repository. This proves scope separation
for the inspected set, not universal independence. Administrator must verify **all** scheduled/manual
backup and restore scopes and record explicit journal exclusion; never restore an old active journal,
clone authority or fabricate a custody assertion. Directory presence alone is not sufficient evidence.
The current operator cannot list the protected directory, so file absence is owner-reported, not verified.

## G3a model-free systemd preflight

Versioned preparation now includes
[the dedicated unit](../../../infrastructure/finance/bigbrain-historical-maintenance.service),
[the inert probe](../../../infrastructure/finance/historical-preflight.py), and
[focused tests](../../../tests/deployment/test_historical_preflight.py).
No unit is installed/started by publication. **Administrator privilege is required**; noninteractive
sudo currently refuses access. Do not supply a password to the agent or use Docker to bypass custody.

The unit is deliberately **preflight-only**, not a Finance process launcher. It cannot enroll or infer.
Systemd257 places ExecStart directly in `controller`; no ExecStartPre moves processes behind systemd's
back. The probe refuses a different actual `/proc/self/cgroup`, populated parent, extra child group,
unexpected writable mount, nonzero capability, wrong UID/GID or absent NoNewPrivs. It enables only
cpu/memory/pids **inside this delegated service**, exclusively creates `worker` with mode0700, sets
the five native limits, and migrates one inert direct child. The parent remains in controller and
uses a retained pidfd to kill/reap. Empty worker is removed; no existing group/process is adopted.
The unit bounds the entire test to45s plus5s termination, KillMode=control-group, Restart=no, no timer.
Failure to obtain pidfd leaves whole-unit cleanup to systemd and never uses an arbitrary PID fallback.
The unit also imposes CPU200%,4GiB,swap0,32tasks; these ceilings cannot increase model authority.

`ProtectSystem=strict` plus explicit cgroup read-only coverage is intended to expose only the unit's
own cgroup subtree writable. Private devices/network and disposable tmpfs are intended restrictions;
inherited backing mounts and effective access still require host proof. Private propagation alone
does not establish this isolation. Persistent private StateDirectory contains invocation-bound
`.json` results; `.pending`
means incomplete, never PASS. A repeated invocation identity cannot replace old evidence. Result
includes probe/unit hashes and UTC times; operator must additionally retain effective unit/drop-in
identity. No model, Finance path, grant, credentials or database is supplied to this test.

### Next local administrator action (only the inert preflight)

**Historical initial-installation procedure; already performed by owner. Do not repeat for mount diagnostics.**

Use an owner-local root terminal and the exact published review files, verified against GitHub.
Do not execute these commands through a privileged container. From that verified checkout, first
confirm the destination ancestors are root-owned, nonsymlink directories without group/other write,
and that neither destination file nor any unit/drop-in already exists. STOP on an existing installation
rather than overwriting it. `systemctl cat bigbrain-historical-maintenance.service` must report no unit.

```sh
# Owner-local root terminal only; no password/secret appears in these commands.
set -eu
test "$(id -u)" = 0 || exit 1
python3 -I - <<'PY'
import os, pathlib, stat
for name in ('/usr/local/libexec', '/etc/systemd/system'):
    path = pathlib.Path(name)
    for parent in (path, *path.parents):
        s = parent.lstat()
        if not (stat.S_ISDIR(s.st_mode) and s.st_uid == 0 and not s.st_mode & 0o022):
            raise SystemExit('STOP: destination ancestor is not protected')
for name in ('/usr/local/libexec/bigbrain-historical-preflight.py',
             '/etc/systemd/system/bigbrain-historical-maintenance.service',
             '/etc/systemd/system/bigbrain-historical-maintenance.service.d'):
    if os.path.lexists(name):
        raise SystemExit('STOP: destination already exists')
PY
# Stop if any check above failed. Never silently create/repair shared destination parents.
install -o 0 -g 0 -m 0444 infrastructure/finance/historical-preflight.py \
  /usr/local/libexec/bigbrain-historical-preflight.py
install -o 0 -g 0 -m 0644 infrastructure/finance/bigbrain-historical-maintenance.service \
  /etc/systemd/system/bigbrain-historical-maintenance.service
cmp infrastructure/finance/historical-preflight.py /usr/local/libexec/bigbrain-historical-preflight.py
cmp infrastructure/finance/bigbrain-historical-maintenance.service \
  /etc/systemd/system/bigbrain-historical-maintenance.service
systemd-analyze verify /etc/systemd/system/bigbrain-historical-maintenance.service
systemctl daemon-reload
systemctl start bigbrain-historical-maintenance.service
systemctl show bigbrain-historical-maintenance.service \
  -p Result -p ExecMainStatus -p InvocationID -p ControlGroup -p Delegate -p DelegateControllers
```

Run as a fail-fast local script (`set -eu`), not a paste that continues after failed checks. No `enable`,
API/Web restart, provider call or Finance command is involved. A successful oneshot is subsequently
inactive, not a failed daemon. In the private state directory, require a new matching invocation JSON
with status PASS and systemd Result=success/ExecMainStatus=0; missing/pending/mismatched results fail.
Verify no surviving unit processes or worker cgroup after exit. Retain private effective-unit/result
evidence; publish only sanitized categories. This is the next concrete step, **not** authority to infer.

### Final-context adoption after the probe passes

The current unit has no production data mounts and no Finance ExecStart. It proves only its actual
model-free context. Before enrollment, the administrator must resolve the final same-DLL placement:

1. Export/copy the **existing exact accepted** hosting assemblies/runtime into a new root-protected
   location; do not build another Finance implementation or launch an API. Verify compiled identity.
2. Install new root-owned regular worker/runtime/model copies with no writable ancestors. Resolve only
   previously verified in-bundle library links into regular files, preserving every SONAME filename;
   never follow an outside target. Do not replace the old worker. Compare all checksums against the
   private manifest, including pinned GGUF and accepted worker source identity. Check complete loader
   closure (worker and dynamically loaded CPU backend, including libgomp) **inside the final context**.
3. Provision a distinct persistent coordination directory1654/0700; do not reuse/clear preflight state
   or uncertain runtime reservations. Administrator verifies authority restore exclusion and custody.
4. Resolve existing Finance/lifecycle paths and required Sentinel Unix-socket permissions privately,
   with no other service changes, Docker socket, new exposed secret or broad writable mount. Final
   writable scopes must be individually reviewed; the shipped probe intentionally rejects additional
   writable mounts. Do not call its isolated-context PASS proof of data-enabled maintenance placement.
   If these accesses cannot preserve the existing contracts, STOP before enrollment.
5. Retain the same delegated controller/worker topology and systemd restrictions for the accepted
   maintenance DLL; exact final-context model-free proof remains required before any command that
   writes authority. Bind that evidence and actual final artifact paths into the private profile.
   No automatic command forwarding, scheduler, inferred grants or preflight-triggered execution exists.
6. Follow the historical enrollment → exact journal seed/protection → authority-check order above.
   For authority-check, six local enable gates false and maintenance true; API settings stay untouched.

These final-context actions are **NOT RUN/BLOCKED**, not a claim that the preflight-only unit is an
installed Finance execution environment. The existing root-custody gate prevents advancing here.
No empty journal, fabricated timestamps or early enrollment should be used to make progress appear complete.

## UnexpectedWritableMount — one owner-approved diagnostic

**Historical checkpoint439bbb0 procedure, operator-reported executed once. Do not repeat.**
The aggregate result and current partial correction are linked above. The old hashes below are
preserved as evidence; they do not match the current unit/diagnostic pair and must not be bypassed.
The failed process has exited; its private namespace cannot be recovered through host `findmnt`.
Use the separate [diagnostic unit](../../../infrastructure/finance/bigbrain-historical-mount-diagnostic.service)
and [reader](../../../infrastructure/finance/historical-mount-diagnostic.py), never edit or restart the
installed maintenance preflight. This is one temporary manual unit, not another Finance service.

All isolation/resource settings and the existing state-directory view match the original unit.
Only description, ExecStart, the **new unit's own** delegated cgroup path and private output routing
change; a focused test compares every setting. The reader checks UID/GID/caps/NNP and its actual
controller placement, then reads its own mountinfo. It never writes cgroups, creates children, probes
writes, opens Finance/model files or calls the original probe/main. No broader writable mounts are added.
Only the exact diagnostic cgroup target is renamed **in memory for comparison** with the original
hash-pinned policy; original target/rename marker remain in evidence. No filesystem alias/prefix is allowed.

The private result includes decoded/encoded targets, mount/parent IDs, filesystem types, selected
mount/superblock flags, all unexpected entries, invocation/time and script/unit identities. Raw source,
filesystem-root fields, arbitrary options, environment and credentials are omitted. Mount targets can
still be private: output is root-only, not journal/chat/Git. The reader refuses terminal output.
`DIAGNOSTIC_ONLY`/exit0 means evidence collected, **never G3a PASS**, even if policy accepts that snapshot.
The diagnostic does not reproduce child migration or prove the vanished failed namespace identical.

### Historical single bounded procedure — not for the current tree

Use the exact published checkpoint files in an owner-local root terminal. No password goes to Codex.
The following script refuses existing diagnostic artifacts, baseline drift, running original process,
drop-ins, unsafe parents or a missing/misowned existing state directory. It creates only temporary
diagnostic files under `/run` and starts **only** `bigbrain-historical-mount-diagnostic.service` once.
No `enable`, original-unit start/reset, API/Web/Docker restart or production installation is permitted.

```sh
set -eu
test "$(id -u)" = 0
test "$(systemctl show bigbrain-historical-maintenance.service -p MainPID --value)" = 0
test -z "$(systemctl show bigbrain-historical-maintenance.service -p DropInPaths --value)"
test "$(systemctl show bigbrain-historical-mount-diagnostic.service -p LoadState --value)" = not-found
python3 -I - <<'PY'
import hashlib, os, pathlib, stat
def require(ok):
    if not ok:
        raise SystemExit('STOP: diagnostic prerequisite mismatch')
for name in ('/run', '/run/systemd/system'):
    p = pathlib.Path(name)
    for ancestor in (p, *p.parents):
        s = ancestor.lstat()
        require(stat.S_ISDIR(s.st_mode) and s.st_uid == 0 and not s.st_mode & 0o022)
state = pathlib.Path('/var/lib/bigbrain-historical-preflight')
s = state.lstat()
require(state.resolve() == state and stat.S_ISDIR(s.st_mode) and
        s.st_uid == s.st_gid == 1654 and stat.S_IMODE(s.st_mode) == 0o700)
expected = {
 '/usr/local/libexec/bigbrain-historical-preflight.py': '97ecacefdeed8e95e7056557b0871384d5e4ab0b9194e7846679f4cffe04bbf6',
 '/etc/systemd/system/bigbrain-historical-maintenance.service': 'b643dc93ad1c618753d603072d346af3dfe06d6f3e4e54de05d0a97c6e4b73c4',
 'infrastructure/finance/historical-mount-diagnostic.py': 'e78d15cb9e323e3d07da4bc2bcffd82c1bf7035d397187ab6255a4fbeadce380',
 'infrastructure/finance/bigbrain-historical-mount-diagnostic.service': 'cb67c30e3c8fb7289ee1bcc86b4c83f565b7ffc8d3255553ed6d052c8d9a9686',
}
for name, digest in expected.items():
    require(hashlib.sha256(pathlib.Path(name).read_bytes()).hexdigest() == digest)
for name in ('/run/bigbrain-historical-mount-diagnostic',
             '/run/systemd/system/bigbrain-historical-mount-diagnostic.service',
             '/run/systemd/system/bigbrain-historical-mount-diagnostic.service.d'):
    require(not os.path.lexists(name))
os.mkdir('/run/bigbrain-historical-mount-diagnostic', 0o750)
os.chown('/run/bigbrain-historical-mount-diagnostic', 0, 1654)
os.chmod('/run/bigbrain-historical-mount-diagnostic', 0o750)
fd = os.open('/run/bigbrain-historical-mount-diagnostic/result.json',
             os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
os.fchmod(fd, 0o600)
os.close(fd)
PY
install -o 0 -g 1654 -m 0440 infrastructure/finance/historical-mount-diagnostic.py \
  /run/bigbrain-historical-mount-diagnostic/diagnostic.py
install -o 0 -g 0 -m 0644 infrastructure/finance/bigbrain-historical-mount-diagnostic.service \
  /run/systemd/system/bigbrain-historical-mount-diagnostic.service
cmp infrastructure/finance/historical-mount-diagnostic.py /run/bigbrain-historical-mount-diagnostic/diagnostic.py
cmp infrastructure/finance/bigbrain-historical-mount-diagnostic.service \
  /run/systemd/system/bigbrain-historical-mount-diagnostic.service
systemd-analyze verify /run/systemd/system/bigbrain-historical-mount-diagnostic.service
systemctl daemon-reload
test -z "$(systemctl show bigbrain-historical-mount-diagnostic.service -p DropInPaths --value)"
systemctl start bigbrain-historical-mount-diagnostic.service
systemctl show bigbrain-historical-mount-diagnostic.service -p Result -p ExecMainStatus -p MainPID -p InvocationID
test "$(systemctl show bigbrain-historical-mount-diagnostic.service -p MainPID --value)" = 0
# Remove only this newly created temporary unit definition; retain private evidence, never retry.
rm -- /run/systemd/system/bigbrain-historical-mount-diagnostic.service
systemctl daemon-reload
```

If any command fails, STOP; do not retry, weaken checks or change the installed production unit.
Unit timeout45s/termination5s/KillMode=control-group still apply. No persistent new write permission
is provisioned: only the temporary unit's delegated subtree exists during its lifetime; the original
state directory is unchanged and the diagnostic never writes to it. Root-owned result/script remain
private under `/run` for review, not installed under `/etc` or `/usr/local`. Preserve evidence privately
before reboot if needed; no raw result publication. Original unit and status remain untouched.

Compare private result InvocationID with systemd output. Review every unexpected row and any stacked
read-only counterpart before proposing a correction. A genuine writable host resource requires fixing
isolation, not whitelisting it. A masked/inaccessible resource needs positive effective-protection
evidence before any narrow policy correction. Parser differences require a reproducible fixture from
the relevant sanitized fields. Missing/failed diagnostic evidence leaves G3a BLOCKED.

## Stop and rollback boundaries

No enrollment, journal seed, permission change, cgroup creation, service installation/restart or private
production config mutation was performed here. Prepared Compose can be discarded without production
rollback. Once enrollment/authority exists, retain it on failure; never delete/reset it to retry. Journal
mismatch/uncertain execution stops without refund. Missing root access is resolved in an owner-local
administrator session; passwords must not pass through the agent/chat. No privileged container fallback.
Finance remains **RESEARCH / 0 SEK / NONE**.
