# Historical Finance hosting — preparation and administrator gate

## Status and scope

**2026-10-16: preparation verified; NOT READY FOR ENROLLMENT OR INFERENCE.**
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

## Stop and rollback boundaries

No enrollment, journal seed, permission change, cgroup creation, service installation/restart or private
production config mutation was performed here. Prepared Compose can be discarded without production
rollback. Once enrollment/authority exists, retain it on failure; never delete/reset it to retry. Journal
mismatch/uncertain execution stops without refund. Missing root access is resolved in an owner-local
administrator session; passwords must not pass through the agent/chat. No privileged container fallback.
Finance remains **RESEARCH / 0 SEK / NONE**.
