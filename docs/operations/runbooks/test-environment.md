# Synthetic TEST environment — U1–U3

2026-11-13: **local U1 implementation for parent review; not installed, ready or runtime verified**.
Authority is owned solely by the
[TEST owner decision](../checkpoint-workflow.md#syntetisk-test--ägarbeslut-2026-11-13).
The external operator boundary is described in
[Proposed ADR 0041](../../adr/0041-test-environment-operator-boundary.md).
ADR 0001 remains Accepted; ADR 0002 remains Proposed. AGENTS is unchanged.

## Ordered tasks, start conditions and limits

The owner authorized U1 → U2 → U3, each with its own time and repair budget. This worktree
implements **U1 only** on `checkpoint/test-environment-u1-20261113`, accepted main
`e63aaa4dc30718f2620f2880a6f2b847e6091d89`, tree
`24f249d76092dc8c3a242ed664ed139dc2eb4d9b`. The initially clean local HEAD and local
`origin/main` match that baseline. Remote was not fetched or independently refreshed in
this offline task. No commit, push, merge, install, image build/download, Docker call,
host/user/network/firewall change, sudo, credential access or production data access is authorized here.
Parent ChatGPT reviews the resulting diff before any host runtime. No real model/provider/order.

| Task | Start condition | Risk / estimate / cap | Repair budget and current use |
| --- | --- | --- | --- |
| U1 manual synthetic slot | Explicit 2026-11-13 scope; baseline/worktree checked; offline implementation first | 3, host operations boundary; 4–8 h / 8 h | 2 of 2 used: fixed relay after parent-reported port failure; then explicit socketpair SKIP for sandbox EPERM and final verification. No automatic repair budget remains. |
| U2 rootless/hardening | U1 safe manual slot retained and its runtime evidence available; separate bounded execution turn | 3; 2–4 h / 4 h | At most 2; 0 used, NOT STARTED |
| U3 harmless UI/data pilot | Safe U1 and U2 outcome recorded; no Finance; unattended work requires successful U2 | 2; 3–6 h / 6 h | At most 2; 0 used, NOT STARTED |

At a cap, retain results and the safe manual slot, stop the failing task and continue only
independent already authorized work. Failed U2 defers unattended U3. No reset of counters,
new design loop or automatic next task. P0/P1 stops the affected operation; P2/P3 goes to
the existing backlog if a concrete deferred issue is found. No speculative redesign.

U1 DoD (five criteria; runtime portions remain pending in this offline handoff):

1. Canonical authority/freeze text corrected, one runbook, Proposed ADR, concise STATUS;
   fixed control snapshot is separate from candidate images and all product processes.
2. Two-service standalone Compose and CLI enforce exact images/revisions, own resources,
   clean environment, nonroot limits and pre-start inspect; offline negative checks pass.
3. Hosted trusted-owner candidate packaging builds both images even when existing tests
   are red, records source/tree/run/test outcomes/digests/checksums and no build secrets;
   a real downloadable package is verified before runtime (currently NOT RUN).
4. Real T04–T06, T11, T12, T15, T16 and T22 PASS before any user link. Source/asset
   readability, synthetic fixture persistence/reset and noninterference are demonstrated.
5. Honest dated evidence, remaining gates, repair count and exact files handed to parent;
   no deployment, production backup approval, owner UX acceptance or U2/U3 readiness claim.

U2 DoD (planned, no implementation here):

1. Record exact authorized rootless/hardening scope and preserve the known safe manual slot.
2. Verify supported image/UID/filesystem/network/resource behavior under the chosen rootless boundary.
3. Re-run affected isolation, reachability, lifecycle and resource test cards with real evidence.
4. Record success or bounded failure within 4 h / two repairs; no silent privilege widening.
5. Preserve manual U1 on failure and mark unattended U3 deferred; document safe recovery.

U3 DoD (planned, no implementation here):

1. Choose an authorized harmless UI/data pilot with no Finance, providers or production data.
2. Bind trusted source/image/provenance to the permitted U1/U2 runtime scope.
3. Verify pilot behavior and fixture lifecycle with focused tests and real browser evidence.
4. Demonstrate the allowed manual or unattended lifecycle without broad host authority.
5. Record actual acceptance and limitations within 6 h / two repairs; no automatic expansion.

## Implementation and fixed installation boundary

`compose.test.yaml` uses JSON syntax, a YAML subset, so Python needs no YAML dependency.
It is a standalone file, never a production override. Services are only `api` and `web`.
Project `bb-test-u1`, network `bb-test-u1-net`, containers `bb-test-u1-api`/`bb-test-u1-web`
and volumes `bb-test-u1-{data,shopping,calendar,settings,finance,lifecycle}` are fixed names.
An installation-specific random label prevents accidental adoption across installations.
All database and fixture paths point into those volumes, including audiobook acquisition,
Finance quarantine/payload/backup/staging and lifecycle data. These are synthetic stores,
not a production restore or proof of the still-BLOCKED production database backup gate.

API runs UID/GID 1654; Web 101. Limits are API 0.75 CPU/1152 MiB and Web 0.25 CPU/384 MiB,
64 PIDs each, memory+swap equal to memory, read-only root, bounded `/tmp`, all capabilities
dropped, no-new-privileges, 5 MiB × 2 log files, restart `no`. No socket, device, bind mount,
host namespace, published container port, extra service, timer or autostart. The six mount targets already exist with
API ownership in the source image; Docker's initial named-volume copy-up must be checked.
The TEST Web Dockerfile normalizes generated static files/config to nonroot-readable modes.

API providers/observation/scheduler/real reasoner, school sources and Media requests are
explicitly off; maintenance is paused, keys empty and Sentinel false. Media service URLs
point to the container loopback discard port. OpenLibrary's existing URL validator requires
its HTTPS hostname, so no fake configurable off flag is invented: outbound isolation is
mandatory, including for endpoints without an application-level off switch.
Clock synchronization remains unavailable without a host mount; no model/Finance readiness
is inferred from `/health`. Existing app defaults/configuration in every trusted candidate
still need source review; OCI labels and clean environment do not prove arbitrary image contents.

The installed CLI always invokes `/usr/bin/docker` with a small explicit environment, an
empty private Docker client directory, fixed rootful socket, isolated working/project directory,
explicit `--env-file EMPTY` and explicit candidate file. It never sources `.env`, shell profiles,
Docker credentials or inherited Compose/proxy/provider settings. It accepts only local
`sha256:` image IDs and compares OCI revision and TEST kind, entrypoint, user and image env.
Effective Compose is compared to the sealed template; unrecognized normalized fields fail.
Containers are created without starting or pulling, then inspected before exact-ID starts.
Actual env, mounts, networks, absence of container ports, capabilities, resource/restart/log settings are checked.
Known harmless Compose canonical defaults are normalized; version differences fail closed.

`snapshot` is an explicit later operator action, not executed by this implementation turn.
It copies the reviewed CLI/template to a new directory outside Git worktrees, creates EMPTY,
records control revision/hash/slot identity and makes control files read-only. The parent
must compare installed hashes to the reviewed source; the operator-supplied revision is
an assertion, not a Git signature. Candidates cannot supply a Compose path or lifecycle hook.
Start never updates the installation. The fixed manual `relay` command binds only the selected
Tailscale IPv4:13001; it forwards raw bytes through `docker exec -i` on the exact verified
Web ID to `/bin/busybox nc` at loopback:8080. No arbitrary target, address or command argument
exists. Initial and per-connection checks pin slot/state/image/container identity; no shell
interprets input. Four active connections, 64 KiB buffers per direction, 30-second connection
deadline and 35-second in-container BusyBox timeout bound work. Client children are terminated
and reaped on disconnect/error/shutdown; the container timeout also bounds exec after client loss.
The command remains foreground/manual, has a separate single-relay lock, and exits on Ctrl-C
or a reset/new Web ID. It may stay bound during same-ID stop/start; connections to a stopped
or changed target fail. No systemd, timer, generic proxy API, credential or extra container.
Parent may own its lifetime in a task-owned terminal/tmux session after review. Real BusyBox
flags, cleanup, saturation, deadline behavior and response completeness still require T22.
Hashes detect drift, not a malicious operator who can
rewrite the seal; rootful Docker access remains root-equivalent and is not granted by this tool.

One nonblocking filesystem lock covers each installed lifecycle operation. All colliding
project/prefix/slot resources are inspected; mismatched names, roles, labels, volume drivers,
bind-backed volumes or foreign network attachments reject the operation. Reset requires
`--confirm bb-test-u1-fixtures`, records its authorization before mutation, and targets exact
validated container/network IDs and six validated volume names. It uses no prune, `down -v`,
or global mutation. Docker must also reject a volume still attached to a foreign container.
The lock coordinates this CLI only; concurrent manual Docker administration is out of scope
and must not occur. Failed startup stops validated IDs, retaining containers/data/receipts.
Unexpected partial creation is retained for review, never broadly cleaned up.

Private installation files `request.json`, `candidate.json`, `state.json`, `events.jsonl`
retain parameters, package provenance, exact IDs and dated events (0700 directory/0600 data).
Do not put them in Git or publish raw Docker/HTTP errors. CLI output omits addresses and paths.
There is no automated stale cleanup. Preserve the original request/control snapshot after
a partial failure; use status/validated reset or parent review, never swap in another candidate
to make cleanup pass. Do not remove an image while its slot needs lifecycle validation.

## Candidate package and later operator sequence

The additional `test-candidate-package` hosted CI job depends on all four existing jobs with
`always()` and `!cancelled()`. Their failures remain failures; no `continue-on-error` is added.
Both image builds are mandatory. PR head must belong to this repository, PR author and actor
must be the owner, and branch must be `checkpoint/...`. Dispatch resolves only an exact
`checkpoint/...` branch. GitHub checks the selected commit's author login and records its exact
SHA/tree before checkout; a moved PR head rejects. No public/fork PR can select this local path.
There is no local runner, deployment job, runtime dispatch hook or per-run approval button.
Checkout does not persist credentials; no token/secret is passed to Docker builds.

The artifact contains `images.tar.gz`, `images.json`, `package.json`, `SHA256SUMS`.
Package metadata records source SHA/tree/ref, workflow run/attempt, actual four job statuses,
the tested SHA (PR merge ref may differ from candidate head), image IDs/available repository
digests and file checksums. Docker-save images often have **no registry RepoDigest**; the exact
local image IDs are content digests. Tests of a PR merge ref are never claimed as head tests.
Review the trusted GitHub run and checksums before loading. Self-declared package fields alone
are not authentication. Hosted builds and artifact availability are currently **NOT RUN**.

The following are later parent/operator instructions, **not commands executed in this turn**.
Keep all concrete addresses, client identities, resource endpoints and paths private. Parameters:
`REVIEWED_CONTROL_SHA`, `CONTROL_SOURCE`, `INSTALL_DIR`, `PACKAGE_DIR`, `CANDIDATE_SHA`,
`API_IMAGE_ID`, `WEB_IMAGE_ID`, `TEST_BIND_IP`, and harmless negative-test endpoints are
operator-supplied. Never populate them from production `.env`, credentials or databases.

1. Parent reviews this diff and the owned candidate/run. Obtain the hosted artifact through
   the existing trusted GitHub session, verify exact source/tree/run and `SHA256SUMS` in the
   package directory. A red backend/frontend status is allowed for synthetic testing, recorded
   honestly, and is never merge acceptance. No local build/download fallback is implicit.
2. In the separately authorized runtime phase, load the checksum-verified archive with
   `docker load --input "$PACKAGE_DIR/images.tar.gz"`. Read the two exact IDs from package.json,
   compare loaded image inspect, and create the reviewed snapshot once:

   ```sh
   python3 "$CONTROL_SOURCE/scripts/test-environment.py" snapshot \
     --destination "$INSTALL_DIR" --control-sha "$REVIEWED_CONTROL_SHA"
   python3 "$INSTALL_DIR/test-environment.py" render \
     --candidate-sha "$CANDIDATE_SHA" --api-image "$API_IMAGE_ID" --web-image "$WEB_IMAGE_ID" \
     --bind-ip "$TEST_BIND_IP" --package "$PACKAGE_DIR/package.json"
   python3 "$INSTALL_DIR/test-environment.py" validate
   ```

3. Compare installed control hashes and actual selected Tailscale interface address. Run `start`
   (waits for internal Web/API HTTP readiness), then `relay` in a separate manually owned
   foreground session using the same installed CLI. Run `status`, then `probe --restart` in
   the operator session. Probe uses actual HTTP through the relay/Web, checks API health, banner/SHA,
   JS/CSS, manifest and icons, and on `--restart` writes a unique synthetic shopping item and
   verifies it survives stop/start. It never follows HTTP redirects or inherited proxies.
4. Complete required real cards below, including client ACL deny and external/host deny using
   harmless approved endpoints. Stop on a failure. Record sanitized results and private raw
   evidence; only then share the selected Tailscale URL. iPhone/PWA approval stays owner-only.
5. `stop` retains fixtures. `reset --confirm bb-test-u1-fixtures` stops/removes only validated
   slot containers/network/volumes. After successful reset, render another trusted package.
   No `sudo`, global prune, daemon/group/firewall modification or production cleanup.

## Test cards and current evidence

All runtime cards below are **NOT RUN as of 2026-11-13**. Offline mocks do not satisfy them.
Record each actual result against candidate/image/control hashes, Engine/Compose versions,
rootful/rootless scope, timestamp and private evidence reference. Runtime card failures consume
the same U1 task repair budget, not a fresh budget per error. No raw private facts in this table.

| Card | Verification and required result |
| --- | --- |
| T01 | Trusted same-repo owned branch and author; exact source/tree/run; fork/public trigger rejected. Hosted artifact available. |
| T02 | Verify artifact checksums, loaded image IDs and OCI source revision/kind. Reject tag, digest/revision mismatch and missing provenance. |
| T03 | Render from sealed installation under poisoned caller env; no inherited Compose/.env/proxy/key settings. Unknown template option rejected. |
| **T04** | Actual rootful container inspect and own-container `/proc` show nonroot UID, zero effective capabilities, NoNewPrivs, seccomp and enforced CPU/memory/PID/log limits; only two services. |
| **T05** | Actual internal bridge has isolated gateway option and IPv6 off. Own-container connections to operator-approved harmless external IPv4/IPv6/DNS canaries fail; ordinary internal bridge alone is insufficient. No real provider request. |
| **T06** | Parent's harmless network experiment: host bridge interface has no assigned host IPv4/IPv6 address; container cannot reach harmless host/LAN/other-network canaries, including known gateway/host routes. No probing production services. |
| T07 | Web can reach API on the sole own network; neither service has any published port or other network attachment. |
| T08 | All real database paths are on own writable volumes, first-start copy-up ownership works without root helpers; source/config assets readable by nonroot. |
| T09 | API effective configuration disables providers/scheduler/real reasoner/Sentinel; no keys, model artifacts or host clock mount. No real data inspected. |
| T10 | Real attempt to start tampered mount/network/env/image/port/capability configuration rejected before start. Use disposable own fixtures only. |
| **T11** | `probe` gets actual HTML TEST + short SHA, exact revision, correct JS/CSS MIME and bodies, manifest/icons and API `/health` through Web. No SPA fallback mistaken for assets. |
| **T12** | Reset requires exact own-fixture confirmation and audit; mismatched name/label/external-volume/foreign attachment fixtures reject before mutation. Confirm only own fixture data deleted and fresh slot works. |
| T13 | Safe baseline comparison shows no other containers, volumes, worktrees or production configuration changed by slot lifecycle; inspect metadata only, no production data. |
| T14 | Concurrent lifecycle invocation fails immediately under lock; no overlapping reset/start or stale candidate adoption. |
| **T15** | `probe --restart` persists unique synthetic shopping item across actual stop/start; no autostart policy. Reset deletes fixture, rebuild starts empty. |
| **T16** | User-selected IP is actually assigned to the Tailscale interface; fixed operator relay binds only that IP:13001. Authorized client succeeds, unauthorized client denied by existing ACL; no LAN/wildcard/IPv6 listener exposure or container published port. Client ACL evidence NOT RUN until supplied/tested. |
| T17 | Failure after start stops only validated IDs; logs/receipts bounded and private; partial creation retained safely. |
| T18 | Test banner remains visible on narrow viewport/PWA and production image contains no TEST banner/flag. Owner UX approval remains PENDING. |
| T19 | Candidate with red backend/frontend results can still produce both usable hosted images; missing/failed build cannot publish successful package. |
| T20 | Exact image environment/config and declared image volumes/healthcheck inspected; unexpected additions rejected. Image content trust is owned-source review, not label cryptography. |
| T21 | Private receipts contain provenance and exact owned resources; published summary contains no literal host IPs/private homes/raw logs. |
| **T22** | Installed CLI/template hashes match reviewed source; candidate's Compose/CLI cannot change installed control. Installed drift rejected. Explicit EMPTY/project directory used; no new host privileges. Relay pins exact Web ID/image; client bytes cannot choose command/target. Saturation refuses excess connections, deadline/disconnect/stop cleans owned client children and container exec ends by its timeout; no hidden background lifetime. |

T05/T06 require more than Docker `network inspect`: isolated mode is intended to remove the
bridge's host address, while ordinary internal bridges have one. Inspect flags express intent;
read-only host interface observation and harmless negative connections establish actual behavior.
Parent's 2026-11-13 handoff reports an actual harmless experiment using an owned cached Python
image/empty tmpfs: isolated bridge blocked outgoing host/LAN/Tailscale/external connections,
but Docker-published port failed. A fixed operator TCP relay through exact-ID Docker exec
returned HTTP 200 without extra networks, host bridge addresses, credentials or privileges.
Parent reports probe cleanup and unchanged production identities; private receipts remain
with parent. This is parent-reported primitive evidence, **not PASS for this U1 candidate**.
The first U1 repair replaces Docker port publication with that fixed manually started relay.
Candidate compatibility and reachability remain **NOT RUN**. Do not replace isolated with a
regular internal bridge, change firewall, add a privileged helper or reroute to a new platform.
The CLI checks this U1 **rootful** scope only and explicitly rejects rootless; U2 is not claimed.

Focused offline verification commands (no Docker or .NET suite):

```sh
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/test_environment -p 'test_*.py' -v
node scripts/verify-documentation.mjs
git diff --check
```

Initial offline verification: **PASS**, 18 tests in 0.022 s; documentation validator PASS
(295 Markdown files / 53 unique backlog IDs), `git diff --check` PASS. This preceded the
parent network handoff and relay repair. First repair verification: 23 test cases passed,
one socketpair byte-transfer test errored on sandbox `EPERM`; no Docker/runtime involved.
The documentation validator also failed that invocation with `spawnSync git EPERM`, despite
receiving Git listing output. Second repair makes the denied socket test explicitly SKIP
before spawning its echo child. Final focused verification on 2026-11-13:
**23 PASS / 1 SKIP**, 24 tests in 0.026 s; SKIP is sandbox-denied AF_UNIX byte transfer,
not relay traffic success. Documentation validator **PASS** (295 Markdown files / 53 unique
backlog IDs) and `git diff --check` **PASS**. A sequential retry of the unchanged documentation
validator succeeded; no validator or host permission changes were made. U1 repair budget
is exhausted at 2/2; further blocking runtime findings require the bounded parent/owner
decision specified above, not an automatic third repair. U2/U3 counters remain 0/2.
Only the parent's bounded primitive evidence above informs the repair; no candidate runtime
PASS is reused. No local image build, application suite, hosted workflow,
installation, provider/model call, browser session or client Tailscale ACL check has been run.

## Exact changed-file scope

- `compose.test.yaml`
- `scripts/test-environment.py`
- `tests/test_environment/test_test_environment.py`
- `infrastructure/test-environment/Dockerfile.api`
- `infrastructure/test-environment/Dockerfile.web`
- `.github/workflows/ci.yml`
- `TESTING.md` (only stale Finance/BB-158 freeze statements)
- `docs/START-HERE.md` (closing stale exact-SHA approval statement)
- `docs/operations/checkpoint-workflow.md` (canonical TEST authority)
- `docs/operations/runbooks/test-environment.md` (this runbook)
- `docs/adr/0041-test-environment-operator-boundary.md` (Proposed)
- `docs/indexes/documentation.md` (one entry link)
- `docs/STATUS.md` (local/planned/offline status)

AGENTS, production Compose/Dockerfiles and API/Web product source are unchanged. No separate
report rounds, speculative backlog items or recovery note are needed for an uninterrupted
offline review handoff; runtime remains expressly pending. If interrupted, use only the existing
[recovery note](../codex-recovery.md), preserving this worktree and the original private slot state.
The concurrently supplied `TEST-U1-PARENT-HANDOFF.md` belongs to parent, is preserved unchanged,
and must be excluded from publication; its relevant sanitized network finding is retained above.

## Independent parent review and bounded exit — 2026-11-13

**BLOCKED for merge, installation, runtime and user link.** This section supersedes the
implementation handoff's readiness and authority descriptions above. The owner authorized
the complete U1 → U2 → U3 series with conditional starts; the subprocess's offline-only
restriction was not a restriction on the owner's mandate. Remote main was fetched and
independently checked by the parent before implementation, and checked again after review:
`e63aaa4dc30718f2620f2880a6f2b847e6091d89`. No new report round or authority change is made.

Verified: the worker consumed **2/2 U1 repair attempts** (relay integration, then a sandbox
socket-test adjustment). Parent reran the unchanged focused suite outside that sandbox:
**24 PASS, 0 SKIP**; documentation validator PASS (295 Markdown / 53 backlog IDs), diff check
PASS. These checks do not establish real application runtime or negate the blockers below.

| Finding | Evidence class and mandatory source | Reproduction / consequence |
| --- | --- | --- |
| P1 Compose compatibility | Verified host observation; U1 DoD actual installed validator before start | Compose 5.3.1 serializes both memory values as digit strings and adds network IPAM defaults. `validate_effective` rejects this unchanged canonical template. Parent's read-only config probe reproduces rejection before any container creation. Runtime must stay blocked. |
| P1 test-card identity | Verified source comparison; owner's v1.2 minimum T04–T06/T11/T12/T15/T16/T22 | The table above reassigns existing IDs. In v1.2 T04=nonroot/static assets, T05=API/proxy, T06=restart, T11=host/data, T12=network, T15=actual cgroup CPU/memory/process enforcement, T16=foreign-resource-safe cleanup, T22=allowed/denied IPv4/IPv6 clients. T19=Sentinel NOT RUN, not red CI packaging. Keep these original meanings before claiming acceptance. No mandatory card has a candidate-runtime PASS. |
| P1 canonical mandate drift | Verified document comparison; explicit owner's preapproved series and checkpoint delegation | The proposed canonical TEST paragraph incorrectly promotes the worker's temporary no-push/offline-only instructions into standing authority, and the runbook says no automatic next task. These are not the owner's decision; do not merge those restrictions. The preapproved series can advance when its actual start conditions hold. |
| P1 package admission condition | Verified workflow source; synthetic-only no-keys requirement and U1 accident protection | The job allows failed `needs.secrets.result` through its `always()` condition. Red functional tests may be packaged, but failed secret checks must not qualify. A package from this candidate must never be locally admitted after a failed secrets check. Hosted inspection of an otherwise clean owned source does not authorize runtime. |

The real shopping URL `/api/v1/modules/shopping-list/items` and fixed isolated-network
relay are implemented; the relay primitive passed only on an empty cached probe image.
Interpretation: actual Web/API startup, volume ownership, resource enforcement and access
acceptance remain unknown. Rootless prerequisites were observed read-only: subordinate
UID/GID ranges and rootless setup tooling exist; `newuidmap` and `newgidmap` are absent.
That is a U2 prerequisite gap, not authority to install host packages or widen privileges.

Predetermined exit applied: preserve this unaccepted checkpoint and publish it for review;
continue only independent existing hosted CI/evidence. **No third U1 repair, no merge,
no installation, no test slot, no timer/autostart.** U2 is NOT STARTED because its safe-U1
start condition is absent; unattended U3 is DEFERRED, counters 0/2 each. No dependent work
is falsely described as complete, and no additional isolation/design loop is opened.
There is no safe U1 application slot to retain; existing production remains untouched.

Parent raw probes and exact resource identities remain private. The public checkpoint is
reviewable evidence, not accepted source of truth. Owner UX/PWA acceptance remains NOT RUN;
no owner test card is issued until an admitted slot exists and that control is needed.
