# Synthetic TEST environment — U1–U3

2026-11-13: **manual synthetic slot installed and runtime verified below; external T22 PARTIAL, user test release BLOCKED**.
Authority is owned solely by the
[TEST owner decision](../checkpoint-workflow.md#syntetisk-test--ägarbeslut-2026-11-13).
The external operator boundary is described in
[Proposed ADR 0041](../../adr/0041-test-environment-operator-boundary.md).
ADR 0001 remains Accepted; ADR 0002 remains Proposed. AGENTS is unchanged.
The owner-approved [Agent Independence requirement](../../architecture/agent-independence.md)
is future architecture preparation only; it adds no current delivery gate, agent integration or cost.

## Ordered tasks, start conditions and limits

The owner authorized U1 → U2 → U3, each with its own time and repair budget. This worktree
implements **U1 only** on `checkpoint/test-environment-u1-20261113`, accepted main
`e63aaa4dc30718f2620f2880a6f2b847e6091d89`, tree
`24f249d76092dc8c3a242ed664ed139dc2eb4d9b`. Parent fetched and independently verified this main baseline before implementation and
again at review. U1-R1 starts on the clean existing branch at
`96b3a06518fd4fd567a1dd6b114637dc84e6b0cb`. The current correction subprocess is limited to
source/docs and focused offline checks; parent owns read-only Docker validation, publication,
hosted CI/artifacts and runtime when admitted. These subprocess limits do not narrow the
owner's series mandate. Parent independently reviews fixed control before installation.
No real model/provider/order or new host privilege is authorized.

| Task | Start condition | Risk / estimate / cap | Repair budget and current use |
| --- | --- | --- | --- |
| U1 manual synthetic slot | Explicit 2026-11-13 scope; baseline/worktree checked; offline implementation first | 3, host operations boundary; 4–8 h / 8 h | 2 of 2 used: fixed relay after parent-reported port failure; then explicit socketpair SKIP for sandbox EPERM and final verification. No automatic repair budget remains. |
| U2 rootless/hardening | U1 safe manual slot retained and its runtime evidence available; within the preapproved series | 3; 2–4 h / 4 h | At most 2; 0 used, NOT STARTED |
| U3 harmless UI/data pilot | Safe U1 and U2 outcome recorded; no Finance; unattended work requires successful U2 | 2; 3–6 h / 6 h | At most 2; 0 used, NOT STARTED |

At a cap, retain results and the safe manual slot, stop the failing task and continue only
independent already authorized work. Failed U2 defers unattended U3. No reset of counters,
or new design loop. Advance to the next preapproved task when its start conditions hold;
no new owner click or separate execution-turn approval is required. P0/P1 stops the affected operation; P2/P3 goes to
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
Secret scanning must succeed (`needs.secrets.result == 'success'`); failed, skipped or
cancelled secret checks block packaging. Red backend/frontend/documentation outcomes may
be recorded without changing their checks. Both image builds are mandatory. PR head must belong to this repository, PR author and actor
must be the owner, and branch must be `checkpoint/...`. Dispatch resolves only an exact
`checkpoint/...` branch. GitHub checks the selected commit's author login and records its exact
SHA/tree before checkout; a moved PR head rejects. No public/fork PR can select this local path.
There is no local runner, deployment job, runtime dispatch hook or per-run approval button.
Checkout does not persist credentials; no token/secret is passed to Docker builds.

The artifact contains `images.tar.gz`, `images.json`, `build.json`, `package.json`, `SHA256SUMS`.
The TEST build derives a temporary Dockerfile from the production API Dockerfile, strictly
replacing only its single SDK FROM with `mcr.microsoft.com/dotnet/sdk:10.0.300` and checking
`global.json` version/rollForward. Production Dockerfile/global.json and ASPNET 10 are unchanged.
`build.json` and package metadata record the selected SDK image and source/derivative SHA256;
the checksum manifest covers this provenance. No local heavy build is required.
Package metadata records source SHA/tree/ref, workflow run/attempt, actual four job statuses,
the tested SHA (PR merge ref may differ from candidate head), image IDs/available repository
digests and file checksums. Docker-save images often have **no registry RepoDigest**; the exact
local image IDs are content digests. Tests of a PR merge ref are never claimed as head tests.
Review the trusted GitHub run and checksums before loading. Self-declared package fields alone
are not authentication. Initial hosted API package build **FAIL** in run `43383141697`: floating SDK 10.0 selected
10.0.404, incompatible with repository 10.0.300/latestPatch. No candidate image artifacts
exist. Corrected hosted build and artifact availability remain **PENDING**.

The following are later parent/operator instructions, **not commands executed in this turn**.
Keep all concrete addresses, client identities, resource endpoints and paths private. Parameters:
`REVIEWED_CONTROL_SHA`, `CONTROL_SOURCE`, `INSTALL_DIR`, `PACKAGE_DIR`, `CANDIDATE_SHA`,
`API_IMAGE_ID`, `WEB_IMAGE_ID`, `TEST_BIND_IP`, and harmless negative-test endpoints are
operator-supplied. Never populate them from production `.env`, credentials or databases.

1. Parent reviews this diff and the owned candidate/run. Obtain the hosted artifact through
   the existing trusted GitHub session, verify exact source/tree/run and `SHA256SUMS` in the
   package directory. A successful secrets check is mandatory. A red backend/frontend/documentation status is allowed for synthetic testing, recorded
   honestly, and is never merge acceptance. No local build/download fallback is implicit.
2. After the existing runtime admission conditions hold, load the checksum-verified archive with
   `docker load --input "$PACKAGE_DIR/images.tar.gz"`. Read the two exact IDs from package.json,
   compare loaded image inspect, and create the reviewed snapshot once:

   ```sh
   python3 "$CONTROL_SOURCE/scripts/test-environment.py" snapshot \
     --destination "$INSTALL_DIR" --control-sha "$REVIEWED_CONTROL_SHA" \
     --allow-client "$TEST_BIND_IP" --allow-client "$APPROVED_CLIENT_IPV4"
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

The table below defines the original runtime cards. Dated actual results are recorded in
[Runtime evidence and access handoff](#runtime-evidence-and-access-handoff--2026-11-13); earlier NOT RUN statements describe the initial handoff. Offline mocks do not satisfy runtime cards.
Record each actual result against candidate/image/control hashes, Engine/Compose versions,
rootful/rootless scope, timestamp and private evidence reference. Runtime card failures consume
the applicable bounded task repair budget, not a fresh budget per error. Original U1 remains
2/2 exhausted; the explicitly authorized U1-R1 correction has its own cap below. No raw private facts in this table.

| Card | Verification and required result |
| --- | --- |
| T01 | Source identity: trusted same-repo owned branch/author; exact source SHA/tree/run, image IDs and OCI revision; reject public/fork source. |
| T02 | Record the four actual CI outcomes (backend/frontend/documentation/secrets) and tested SHA. Red functional results allowed; secrets success mandatory, both package builds required. |
| T03 | Package admission rejects corrupt digests, extra files, traversal, symlinks and oversized input before loading; verify trusted run/checksums. |
| **T04** | Actual nonroot execution, readable configuration/static assets, correct JS/CSS MIME and bodies; no SPA fallback mistaken for assets. |
| **T05** | Actual API through Web proxy, response schema and Problem Details on errors; `/health` alone is insufficient. |
| **T06** | Restart persists unique synthetic fixture with the exact same image IDs; `probe --restart` checks the shopping item. |
| T07 | Synthetic database migration and reset: correct schema, own fixture reset and fresh initialization; no production restore. |
| T08 | Concurrency/idempotency/single slot: overlapping lifecycle denied, repeated actions safe, no stale candidate adoption. |
| T09 | Interruption/failure: stop only validated owned IDs, retain safe partial state and private bounded recovery evidence. |
| T10 | Bounded OOM, timeout and disk failure probes with safe owned-fixture recovery; never exhaust host resources. |
| **T11** | Host/data/mounts/env isolation: no production socket/data/operator home; clean environment, only own writable volumes, no changes to foreign resources. |
| **T12** | Candidate network denies production routes, host gateway and external IPv4/IPv6 using harmless approved canaries; no production service probing. Inspect isolated bridge plus actual negative connections. |
| T13 | Credential absence and synthetic canary checks; no real keys, provider credentials or inherited secrets. |
| T14 | Source/admission rejects extra mounts, candidate Compose/control replacement, unexpected image environment/volumes/healthcheck and any unrecognized effective configuration before start. |
| **T15** | Real cgroup CPU/memory/process enforcement and bounded probes; inspect limits alone does not prove enforcement. Verify nonroot/capabilities/NoNewPrivs/seccomp and bounded logs/tmpfs. |
| **T16** | Cleanup preserves foreign names, label collisions, unknown volumes and symlinks; reject before mutation, confirm/audit only own-fixture deletion and safe reinitialization. |
| T17 | UI/PWA separate owner acceptance: visible TEST + short SHA on narrow viewport/PWA; production has no TEST flag. Owner approval NOT RUN. |
| T18 | Synthetic Finance only: fixture paths/configuration, no real providers/models/market data/orders; no Finance production or nonsynthetic acceptance. |
| T19 | Sentinel integration **NOT RUN**, no stub; `Enabled=false`. No Sentinel integration PASS inferred. |
| T20 | Media stubbed tests only; **NOT RUN** if no actual stubbed tests/evidence. No real provider requests. |
| T21 | Calendar/family synthetic fixture behavior and persistence; no real household data. |
| **T22** | Allowed/denied clients IPv4/IPv6: only selected assigned Tailscale IPv4:13001, no wildcard/LAN/IPv6 listener or container published port. Verify authorized success and unauthorized denial by existing ACL. Fixed exact-Web-ID relay rejects target changes and bounds saturation, deadlines, disconnect/stop and child cleanup. |

These are the original report v1.2 identities. Mandatory before a user link:
**T04–T06, T11, T12, T15, T16, T22** with these meanings. Offline PASS is never card PASS.

T12 requires more than Docker `network inspect`: isolated mode is intended to remove the
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
- `scripts/test-candidate-dockerfile.py`
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

## Independent parent review and bounded exit — 2026-11-13

**Historical initial-candidate findings and bounded exit, retained without resetting U1.**
The initial candidate was BLOCKED for merge, installation, runtime and user link.
The U1-R1 section below records the authorized correction and its distinct verification status. The owner authorized
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
| P1 test-card identity | Verified source comparison; owner's v1.2 minimum T04–T06/T11/T12/T15/T16/T22 | The initial candidate table reassigned existing IDs (restored in U1-R1). In v1.2 T04=nonroot/static assets, T05=API/proxy, T06=restart, T11=host/data, T12=network, T15=actual cgroup CPU/memory/process enforcement, T16=foreign-resource-safe cleanup, T22=allowed/denied IPv4/IPv6 clients. T19=Sentinel NOT RUN, not red CI packaging. Keep these original meanings before claiming acceptance. No mandatory card has a candidate-runtime PASS. |
| P1 canonical mandate drift | Verified document comparison; explicit owner's preapproved series and checkpoint delegation | The proposed canonical TEST paragraph incorrectly promotes the worker's temporary no-push/offline-only instructions into standing authority, and the runbook says no automatic next task. These are not the owner's decision; do not merge those restrictions. The preapproved series can advance when its actual start conditions hold. |
| P1 package admission condition | Verified workflow source; synthetic-only no-keys requirement and U1 accident protection | The job allows failed `needs.secrets.result` through its `always()` condition. Red functional tests may be packaged, but failed secret checks must not qualify. A package from this candidate must never be locally admitted after a failed secrets check. Hosted inspection of an otherwise clean owned source does not authorize runtime. |

The real shopping URL `/api/v1/modules/shopping-list/items` and fixed isolated-network
relay are implemented; the relay primitive passed only on an empty cached probe image.
Interpretation: actual Web/API startup, volume ownership, resource enforcement and access
acceptance remain unknown. Rootless prerequisites were observed read-only: subordinate
UID/GID ranges and rootless setup tooling exist; `newuidmap` and `newgidmap` are absent.
That is a U2 prerequisite gap, not authority to install host packages or widen privileges.

Initial U1 predetermined exit applied: preserve this unaccepted checkpoint and publish it for review;
continue only independent existing hosted CI/evidence. **No third U1 repair, no merge,
no installation, no test slot, no timer/autostart.** U2 is NOT STARTED because its safe-U1
start condition is absent; unattended U3 is DEFERRED, counters 0/2 each. No dependent work
is falsely described as complete, and no additional isolation/design loop is opened.
There is no safe U1 application slot to retain; existing production remains untouched.

Parent raw probes and exact resource identities remain private. The public checkpoint is
reviewable evidence, not accepted source of truth. Owner UX/PWA acceptance remains NOT RUN;
no owner test card is issued until an admitted slot exists and that control is needed.

## U1-R1 bounded correction — 2026-11-13

Owner asked “Hur löser vi det?” after the four blockers; parent architect authorized this
bounded correction on the same checkpoint. Risk **3**, estimate **1–2 active hours**, cap **2 h**,
maximum **2 repairs total**. Initial combined implementation is not a repair retry; **0/2 used**.
Original U1 remains failed/incomplete with **2/2 exhausted**, never reset. At cap preserve
work and evidence; no runtime, new privilege or redesign loop. Parent performs Docker validation,
commit/push, hosted CI/artifact verification and admitted runtime; no extra owner approval is added.

DoD (five criteria):

1. Reproduce Compose 5.3.1 memory digit strings and exactly empty network `ipam: {}`;
   normalize only those known defaults, retain negative mutation rejection.
2. Require secrets-success for packaging while recording red functional CI unchanged.
3. Restore original report v1.2 T01–T22 and preapproved U1 → U2 → U3 conditional mandate.
4. Hosted TEST package builds with repository SDK feature band, recording derived Dockerfile
   hash and SDK without altering production behavior.
5. Focused Python/documentation/diff checks and honest dated status; runtime **NOT RUN**.

Initial fixes implement these source/documentation corrections, including the concrete
package failure in run `43383141697` (SDK 10.0.404 vs 10.0.300/latestPatch). These edits do
not establish hosted build success or resolve runtime acceptance by assertion. Parent's
unchanged 24-test PASS outside the sandbox and four CI checks PASS on `96b3a06` are reused
as **initial-candidate evidence only**. Parent's read-only Compose observation confirms
network IPAM exactly `{}`. No image artifact exists from the failed package build.

Correction verification: **PENDING** focused checks and independent parent verification.
Corrected hosted package build/artifact verification **PENDING**; all candidate runtime cards
**NOT RUN**, no install or user link, no owner UX approval. U2 NOT STARTED until safe U1;
unattended U3 DEFERRED until U2 succeeds. Historical initial findings above remain evidence,
not a claim that corrected code has passed their runtime/hosted acceptance conditions.

Parent correction verification — 2026-11-13: **26 PASS / 0 SKIP**, documentation validator
PASS (295 Markdown / 53 backlog IDs), diff check PASS. The unchanged real Compose 5.3.1
canonical output from the private read-only probe now passes the corrected validator;
negative mutation tests remain required and passed. Anonymous MCR manifest lookup confirms
SDK 10.0.300 is available. The four initial source/document blockers are corrected; hosted
package build, loaded-image validation and actual runtime cards remain pending. U1-R1
repairs 0/2. Agent Independence is documented as the owner's future requirement only.

### U1-R1 repair 1 — installed client policy

Independent source review of `151ab3a` identified conditional LAN routing to the
Tailscale destination address: destination binding alone did not authorize the source.
This is a verified missing source check and a technically motivated exposure scenario,
not an observed production intrusion. Mandatory original T22 is the requirement.

Repair **1/2** installs an exact IPv4 client allowlist in the control snapshot's seal.
Use repeated `snapshot --allow-client` for the local probe address and explicitly selected
owner clients. No CIDR, wildcard, IPv6 or omitted list is accepted. Candidate `render`
cannot select clients. The relay checks accepted peer source before capacity, Docker
inspection or exec, retaining Tailscale ACLs and the fixed target. Control-policy changes
require a newly reviewed installation. This is the existing trusted-operator accident
boundary; seal files do not isolate against a malicious Docker administrator.

**28 focused tests PASS / 0 SKIP** after this repair. Corrected hosted run
`43400474399` is **all green**, including both image builds and artifact upload, for
candidate `151ab3af61c0124855b6086df264ca10dd651e8c`, tree
`3c940b20c8ff084cecce573ea28bf8019d4d621d`. A separately reviewed later control revision
may operate those exact candidate images; record control and candidate identities
separately, and keep the served banner at the actual image revision. This is not permission
to substitute stale images silently. Actual runtime/client cards remain NOT RUN.

### U1-R1 repair 2 — real Engine named-volume representation

The first admitted start stopped **before either container ran**: Engine 29.6.2 represents
the six approved named volumes in HostConfig.Binds as exact `name:target:rw` strings. The
old blanket Binds rejection misclassified those volume mappings. Read-only diagnosis of
actual container copies confirms all other container fields and all network/volume metadata
match the template. No installed policy was edited and no failed acceptance was marked PASS.

Repair **2/2** accepts only the complete exact multiset of the installed own volume mappings
(or absent Binds on Engines using structured mounts); the independent Mounts check still
requires exactly those six local named volumes. Host paths, foreign volume names, extra or
missing entries, duplicates and changed modes reject. New tests cover these real cases.
Original created resources are retained until validated exact-ID cleanup; a newly reviewed
control snapshot is installed for the next bounded start. No third correction or isolation loop.

## Runtime evidence and access handoff — 2026-11-13

Verified image candidate `151ab3af61c0124855b6086df264ca10dd651e8c`, tree
`3c940b20c8ff084cecce573ea28bf8019d4d621d`, hosted run `43400474399`. Artifact
allowlist/checksums, source/run/tree, exact image IDs/labels and loaded images verified.
Reviewed installed control `75e4023c828922d05b6e9f3419be68fdd56f56cb`, tree
`3fb4985a8fcdca01971aedd6350c1a3c43944441`; all five checks green in hosted run
`43402396784`. Focused suite **29 PASS / 0 SKIP**; documentation/diff verification PASS.
Independent source review cleared the client-admission and exact-named-volume corrections
on those exact control identities; it did not certify runtime by source inspection.

| Required card | Actual dated evidence | Status |
| --- | --- | --- |
| T04 | Actual nonroot Web/API; served TEST banner/exact image SHA, JS/CSS MIME/bodies, manifest/icons; fresh slot reprobed | PASS |
| T05 | API through Web; shopping GET schema and invalid-name 400 Problem Details/code | PASS for stated synthetic contract |
| T06 | Unique synthetic shopping item survives clean stop/start with the same exact images | PASS |
| T11 | Effective/actual env/mounts audited; poisoned caller env excluded; own-container Docker socket/operator home absent; no production mount/key | PASS |
| T12 | Isolated bridge and negative TCP canaries for host/Tailscale/LAN/known gateway/external paths; actual IPv6 host canary denied | PASS for v1 accident boundary |
| T15 | Actual cgroup CPU/memory/PID limits; bounded child CPU throttling, memory OOM confinement and process-limit enforcement; zero effective caps/NoNewPrivs/seccomp | PASS |
| T16 | Real foreign volume/name/label collisions and symlink rejected before mutation; original IDs retained; exact own reset then clean reinitialization | PASS |
| T22 | Allowed local probe source succeeds; explicitly LAN-bound local client rejected; LAN destination and IPv6 destinations denied; single selected IPv4 listener/no container port | PARTIAL: external allowed and denied peers NOT RUN |

Private receipts contain **20 boundary checks** and **15 lifecycle/local-access checks**,
plus the scoped live IPv6 canary. The bridge has **no host IPv4**, but Linux retains a
host link-local IPv6 address. Both candidate interfaces have IPv6 disabled and only IPv6
loopback; a real API-container Bash TCP attempt to a listening host link-local canary
returned Network is unreachable and no connection arrived. This supersedes any implication
that isolated IPv4 gateway mode alone removes every host IPv6 address. No host policy was changed.

The first relay/probe invocation failed because parent launched them concurrently and the
nonblocking lifecycle lock rejected relay startup. Ordered startup and unchanged `probe
--restart` then passed. This was an orchestration correction without a third code repair.
The six approved named volumes were reset after all collision fixtures were removed by
their verified probe identities. Fresh Web/API are running with empty synthetic shopping
data; no timers/autostart. All **12 original production containers retain identical IDs,
images and start times**. Old never-started fixtures were removed by verified exact IDs;
the obsolete control seal was retired. No production backup/restore or deployment is claimed.

**T22 remains PARTIAL, not READY and not a released user test link.** A distinct local
source-bound client proves relay admission, not a remote Tailscale peer/ACL path. The
remaining human boundary is a bounded external access-verification handoff: an allowlisted
owner iOS peer must get Healthy, a real unlisted/denied peer must be refused, IPv6 and
LAN access must be refused. Supply only private health-verification endpoints for this
card, not a general application release. Record actual results before T22 PASS or release;
no exception to the owner's condition is claimed. Owner UI/PWA acceptance remains NOT RUN.

Original U1 **2/2** and U1-R1 **2/2** repair budgets remain exhausted; no third code repair.
U2 NOT STARTED pending safe U1 acceptance; observed missing newuidmap/newgidmap remain a
rootless prerequisite gap, not an invitation to install packages or widen privileges.
Unattended U3 remains DEFERRED. Agent Independence stays documented future architecture
only; no external agent, vendor SDK, subscription or additional release gate was added.
