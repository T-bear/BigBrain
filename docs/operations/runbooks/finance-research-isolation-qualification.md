# Finance RESEARCH isolation qualification — 2026-11-27

**REVIEW CHECKPOINT / RISK 3 / NOT QUALIFIED.** Owner-authorized repository-only
qualification from `bb8c4b021dea69a674df92ade79b45d3c440fb6e`, on
`checkpoint/finance-research-level2-isolation-20261127`.
The [five host conditions and four CI guarantees](../../policy/risk-policy.md#finance-research-och-isoleringsvillkor)
remain the DoD. No policy change or owner acceptance is recorded.
**RESEARCH / 0 SEK / NONE; G3a BLOCKED / G3b NOT RUN.**

## Bounded host-evidence follow-up — 2026-11-28

**REVIEW CHECKPOINT / RISK 3 / NOT QUALIFIED.** This section supersedes the older
CI/publication and host-evidence status below, without replacing historical results.
Owner-supplied main is `bb8c4b021dea69a674df92ade79b45d3c440fb6e`; existing checkpoint
is `0b84a6d96dd656daa5772b8233f75d167f524e4e`. Local HEAD, cached main and ancestry
match. Initial remote verification was blocked by local SSH configuration permissions;
later push and remote verification succeeded without SSH or privilege changes.
The owner reports all six CI checks
PASS, isolated tests 234/234 and Python 5/5 for that checkpoint. These are supplied
results, not a new CI inspection or evidence for a later revision.

### Requirements and design

The authorized follow-up DoD is: (1) establish accepted requirements and source;
(2) reconcile supplied host evidence with explicit limits; (3) identify H2's accepted
ingress/release and literal network gap; (4) specify H3/H4 metadata without values or
provider access; (5) identify H5's accepted architecture and minimum separation;
(6) verify focused tests/documentation, preserve unrelated cache, and report exact
delta/publication blockers. Completion of this follow-up does **not** complete the
five host conditions or the real execution-boundary qualification.

Policy, ADR 0028/0029/0038, Compose, ingress/backup source, deployment rules and the
referenced accepted evidence are unchanged between supplied main and checkpoint.
Reuse existing source, metadata views and tests. No new collector, credential scanner,
runtime verifier or synthetic topology model is needed: none would supply the missing
effective policy or custody facts. No executable change is made. The designs below
are proposals for separate scope; they do not change accepted architecture or operations.

### Supplied host observations and their limits

Provenance: owner's summary of separately authorized operator inspection dated
2026-11-28; this agent performed **no host inspection**. Exact observation UTC,
runtime/config identities and private receipt references were not supplied here.
Keep those bindings in the existing private evidence mechanism before final qualification;
do not publish raw inventories, identities, addresses, secrets or private paths.

| Condition | Concrete supplied evidence | Current conclusion |
| --- | --- | --- |
| H1 | 12 running containers; zero Docker socket or ancestor-mount overlaps. | **PASS for the supplied running-container mount inspection**, owner-reported. No new socket access. |
| H2 | Zero privileged containers, additional capabilities, devices or host PID/IPC/network modes. API has two Finance-labelled mounts, one RW; no other container overlaps. API shares one network with nine Media-related services and another with Web/Sentinel; no dedicated Finance network. | Privilege/mount observations support those subchecks. **Incomplete / NOT QUALIFIED:** shared membership is established; current effective ingress/routing enforcement is not. No claim of a current successful Media HTTP request. |
| H3 | No authoritative broker credential capability inventory identified. | **PENDING**, not a finding that trading keys exist or that none exist. |
| H4 | Tailscale Running with three peers; local status lacks effective ACL. | **PENDING**. Peer count/status proves neither permission nor denial. |
| H5 | Household backup and receipt directories root-owned 0700, ancestors 0755, no mount overlaps; operator denied. API has RW `/finance-data`, no nested backup mount; stat of Finance backup location denied EACCES. | Household evidence supports that scoped boundary only. **Finance protection PENDING**: no effective backup ownership/ACL evidence or deletion test; EACCES for operator is not denial for API. |

### H2 — accepted ingress, release limit and minimum network design

[Accepted source](../../../src/BigBrain.Api/Program.cs) guards the Finance route prefix
with [FinanceAccessGuard](../../../src/BigBrain.Api/FinanceAccessGuard.cs);
[Compose](../../../compose.yaml) requires the guard and publishes API on loopback.
[Accepted dated evidence](../../reports/features/finance/finance-bb-087-prospective-shadow-foundation-20260815.md#reused-evidence-and-current-gates)
records Nov 19 owner acceptance: direct API loopback, missing/wrong credentials denied,
authorized Web unlock accepted. The owner-supplied Nov 18 Media HTTP 200 is historical;
it does not establish today's exposure after that ingress correction. Neither source
nor the Nov 19 acceptance establishes current enforcement or network isolation.

The existing [STANDARD runner](../../../scripts/deploy-standard.py) has a one-off
`--finance-ingress-release` pinned to source `38698f3377327dfe8c18ae29ef720935faf24b23`,
fixed prior runtime revisions and unchanged source/build inputs. Its compatibility
[contract](../deployment/README.md#bigbrain-101--stöd-för-dubbla-tidigare-privata-releaser-kandidat-2026-11-18)
preserves API volumes and Media network identity. This is not a generic Finance release
or network-change route. No old release is rerun; current installed identity is unknown here.

Minimum verification: reuse a sanitized release receipt binding current image/compiled
revision, port bindings, guard-enabled/token-configured **booleans**, Web proxy route and
effective host/container forwarding rules. Compare Media → API and Media → Web/proxy
paths, including published ports and IPv6, with dated expected/observed allow/deny results
from existing authorized evidence. A 401 establishes authentication, not network denial.
New probes require a separate bounded operator scope and are not performed here.

Literal policy H2 requires Media without access to Finance networks. Do not treat the
absence of a network *named* Finance or the bearer token as satisfying it. Minimum
candidate design keeps the existing API and Finance storage owner: remove the Finance-
hosting API's shared Media-network attachment, retain an API/Web backend without Media
members, and enforce denial of Media-initiated access to that backend and all Finance
proxy/published-port paths. Preserve required API-initiated Media adapter connections
through explicitly bounded routing with return traffic only; review service discovery,
ports and legitimate Web access before selecting exact rules. Separate network names
without enforced routing denial do not suffice. No new Finance service is presumed.

**Implementation stop:** this topology cannot be delivered through the accepted runner's
network-preservation contract. A separately reviewed network/release-contract change and
operator scope are needed first; no Compose, runner or firewall change is made here.
If required Media functionality needs the prohibited inbound path, report that concrete
conflict before implementation instead of relaxing H2 or assuming a service split.

### H3/H4 — exact owner-provided metadata

Use existing private records/exports; provide sanitized conclusions and opaque references.
No account login, provider call, secret file/value, credential hash or new inventory service.

| Gap | Minimum owner-provided facts | Decision enabled |
| --- | --- | --- |
| H3 credential capability and custody | For each credential label: issuer/product, credential/account class (data-only, broker/account, PAPER/LIVE or unknown), authoritative permission record/reference and date, allowed data/account/order/transfer/withdrawal/admin capabilities, active/revoked status, and all server custody locations by role. State inventory coverage of running/stopped services, config/secret stores, image layers/build caches, volumes, user storage and backups, including old/revoked copies. Give explicit zero/unknown counts and known omissions. | No broker/trading keys on server under literal H3. Broker/account keys do not become data-only because code uses only market-data GETs or a service is disabled. Known broker/trading keys require separate disposition scope; unknown capability/coverage stays PENDING. |
| H4 effective network policy | Current effective ACL/grants revision and observation time; server principal/tags and expanded group/autogroup/wildcard memberships; owner endpoint labels; subnet, exit-node and shared-device routes; protocol/port allow matrix and existing policy-test results for that same revision. Include public/non-Tailscale account-access paths and credential/session custody from H3. Owner marks each allowed server → owner endpoint/account edge necessary with purpose and exact bounds, or unnecessary. | Compare actual permissions with necessity. Missing identity/route/account coverage stays PENDING; unnecessary allowed edges need separately scoped policy correction. Local three-peer status is not this export. |

The accepted [Alpaca adapter](../../../src/BigBrain.Api/Finance/AlpacaDailyMarketObservations.cs)
uses a fixed data endpoint and configured key/secret fields. This establishes intended
client behavior only, not deployed credential presence or provider-side capabilities.
An issuer/product permission record must support any data-only classification; this task
does not seek new provider confirmation or repeat previously answered entitlement questions.

### H5 — accepted backup architecture and minimum separation

[ADR 0028](../../adr/0028-finance-provider-tagged-backup-restore-and-cleanup.md) owns
provider-tagged selections, immutable manifests, staged verification and maintenance-only
creation/restore. [Backup documentation](../backup-restore/README.md) distinguishes
WIKI-only eligible backups, restricted EODHD copies, BB-132K and household backups.
Household's six-database mechanism excludes Finance. It cannot prove Finance coverage.

Compose configures `/finance-data/backups` beneath API's RW Finance mount. The
[store constructor](../../../src/BigBrain.Api/Finance/FinanceDataProtection.cs) creates
backup/restore directories and deletes `.staging-*`; the maintenance dispatcher also
constructs `EodhdMarketMemory`. Consequently `finance-backup-inventory` and
`finance-backup-verify` are **not safe read-only evidence commands**. Do not run them.
Checksums/COMPLETE manifests prove integrity at verification, not deletion isolation.
RW mount plus EACCES does not prove an actual deletable backup: effective configuration,
backup existence, owner/mode/ACL, parent rights and API UID mapping remain unknown.

First reuse authorized metadata: effective backup directory/config revision, custody
inventory of completed copies, ownership/ACL and parent traversal/removal rights, API
UID/GID mapping, all alias/ancestor mounts and any container-accessible deletion interface.
Keep H5 PENDING until that evidence can decide container authority; no payload or deletion
test is needed for this read-only follow-up.

Minimum separation proposal if the copies are API-writable: preserve the existing
provider-tagged format and trusted host maintenance workflow, but place completed backups
under a protected owner outside every container-writable source/ancestor/alias. Containers
receive no archive write/delete capability or management credential; expose only read-only
inventory/archive access if required. Keep export/restore scratch separate. Existing
completed copies must be included in the reviewed cutover; an extra protected copy alone
does not establish that all existing backups meet H5. Copy/move/removal is separately
authorized and must preserve rights, manifests and recovery; none occurs here.

**Implementation stop:** merely making today's backup directory read-only is incompatible
with constructor cleanup and creation assumptions. A future narrow code change must
separate read-only inventory from maintenance initialization/cleanup, followed by a reviewed
storage/maintenance cutover. Only the trusted host operator publishes protected copies;
no container, including a maintenance container, gets authority to delete that archive.
No scheduler, remote backup service or generic privilege expansion is proposed. Preserve
ADR 0028, restricted-copy scope and BB-132K; independently verify the existing daily-backup/
real-restore gate without substituting synthetic evidence.

### Implementation, verification and handoff

Implementation is documentation only: this runbook, STATUS and reconciliation of the stale
recovery note. Existing evidence mechanisms suffice; no new executable tooling or tests.
No independent candidate review is claimed. No merge, runtime change, probe, data operation
or host access. Local verification on 2026-11-28:

| Check | Result and limit |
| --- | --- |
| `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/deployment -p 'test_finance_research_boundary.py' -v` | **PASS 5/5**, including synthetic forbidden-dependency mutations. |
| Existing `test_standard_deployment` tests: three `StandardTests.test_finance_ingress_*` cases for candidate/rollback preservation, rejection and runtime fingerprint; two `BackupHelperTests` cases for no production traversal and alias/nested/missing/duplicate mounts | **PASS 5/5**. Fake Docker and temporary synthetic databases only; no runtime or credential read. Reproduce with `PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=tests/deployment python3 -m unittest` and the five fully qualified method names from the test source. |
| `python3 scripts/verify-finance-research-boundary.py --resolved` | **PASS**, 12 source files and cached resolved dependency closure; no restore/network. |
| `node scripts/verify-documentation.mjs` | **PASS**, 298 Markdown files, 57 unique backlog IDs. An intermediate rerun was blocked by `spawnSync git EPERM`; after successful publication the unchanged validator passed. Separate targeted new-fragment checks PASS (3 links). |
| `git diff --check`; source scope; unrelated cache byte comparison | **PASS**; documentation-only delta, existing cache preserved. |
| Fresh C# isolation / GitHub CI / independent risk-3 review | **NOT RUN**. Executable tree unchanged; reuse owner-supplied checkpoint evidence only within its scope. |
| Remote main/checkpoint verification | Initial SSH permission failure; subsequent push and `git ls-remote` **PASS**: unchanged supplied main, checkpoint `edca674cc8b7e37f581f85dce49676ee27424016`, local tree `936fd4c68857e2ba5ce8d6ceaca5a29c273b34d2`. Final reconciliation identity is in Git/handoff. |

Git publication identities and exact diff are obtained from Git metadata at handoff.
The initial access failures are retained as historical failures, not current blockers;
no configuration repair or additional implementation loop occurred.

Minimum owner actions: supply the H3 inventory and H4 effective-policy/necessity metadata;
bind the Nov 28 evidence and existing ingress/backup receipts to runtime/config identities;
then separately authorize only the necessary H2 network/release-contract and H5 backup
separation work after those facts are assessed. Independent candidate review remains
pending; publishing this documentation does not authorize those operations.

## Waterfall phases and scope

1. Requirements: HEAD and cached `origin/main` match the supplied baseline; initial
   worktree clean. Remote verification failed at local SSH configuration validation.
   The [current thaw](../finance-freeze-20261016.md#aktuellt-ägarbeslut--administrativ-upptining-2026-11-08)
   permits this explicit development assignment, not deployment or real execution.
2. Design: reuse internal OutcomeAudit/ProvenanceAudit libraries and synthetic SQLite
   fixtures. Add tests/CI only; no production entrypoint, persistence owner, provider,
   grant or runtime sandbox. Preserve ADR 0028/0029/0038 and trust/provenance/rights.
3. Implementation: source/project/resolved dependency checks, negative mutations,
   compiled-library assertions and a standalone fixture executable in an ephemeral
   bubblewrap namespace. Production code and accepted architecture are unchanged.
4. Verification: dated results below. Ordinary fixture success is not isolated-test
   success. GitHub isolation failed with `BWRAP_MISSING`; the repaired CI run, host
   qualification and independent risk-3 review remain pending.
5. Handoff: review checkpoint only; no merge, deployment, classification change,
   real data, provider traffic, secrets/.env, BigBrain root/sudo, Docker access or trading.
   The bounded CI repair below permits package installation only on the disposable
   GitHub-hosted runner.

The candidate boundary is **only the two offline libraries and five linked domain
sources**. It does not cover API/Hosting, acquisition, recurring research or all Finance
code. Existing paths include persistence/provider composition; a green offline fixture
cannot qualify them as read-only research. Selecting/verifying a real execution boundary
remains a qualification gap, not an implicit architecture change.
The offline reader remains an internal synthetic prototype, not an authorized operator
or live snapshot reader. Digests/immutable SQLite do not prove custody, completeness,
independent time attestation or prospective admission. No ADR/grant is relaxed.

## CI guarantees and negative controls

| Policy guarantee | Candidate check | Limit/result |
| --- | --- | --- |
| No broker/order/trading client imports | `verify-finance-research-boundary.py` checks both projects, linked sources and transport/dynamic-loading constructs; `--resolved` checks exact restored transitive package/project closure. Compiled tests reject unreviewed nonframework assemblies and network/reflection/process imports. Synthetic package/project/link/build-hook/transport/order/dynamic-load/transitive-client mutations must be rejected. | Local PASS for this slice only. Static scanning is defense in depth, not hostile-code verification. Runtime/SQLite supply-chain trust remains. |
| Read-only DB; writes fail | Existing `OfflineImage.OpenImmutable` tests attempt UPDATE, DELETE, DDL and temporary DDL, compare bytes, and independently disable `query_only` while expecting `SQLITE_READONLY`. WAL/sidecar/digest-negative tests remain. | Local library tests PASS. Actual custody, metadata and concurrent-source protection remain unverified. |
| Test environment without network or keys | Separate CI job builds first, stages DLL/SO, exact deps/runtime JSON and three schema source files. Non-setuid bubblewrap creates an empty root, clean environment, private namespaces, dropped capabilities, read-only runtime/artifacts/synthetic source and fresh writable tmpfs. Only the public New York zoneinfo file is added for the existing calendar. No checkout, home, config, private host data or socket mount enters the child. | Initial sandbox launch blocked; prior direct unprivileged BigBrain run PASS after fixture-only repair; GitHub job 140719681087 FAIL: `BWRAP_MISSING`, suite not started; repaired CI run PENDING. Fresh runner runtime/system libraries are trusted inputs. Build/restore and runner package installation are outside the isolated test phase. |
| Order/production-write negative test makes CI red if permitted | Tests attempt TCP to TEST-NET and an absent Unix order socket, plus SQL/file create/overwrite/delete on a read-only **synthetic production surrogate**. Unexpected connection/write success fails before any order payload. `-failSkips` makes isolated skips fatal; launch failure is nonzero with no fallback. | Three isolated tests SKIP outside the dedicated runner; all passed inside it on BigBrain. No real production path or order endpoint is addressed. |

The new `Finance RESEARCH synthetic isolation` job follows existing PR/main/dispatch
triggers, read-only permissions and checkout with credential persistence disabled.
Before the isolated fixture step, the disposable GitHub-hosted `ubuntu-22.04` runner
installs Ubuntu's distro `bubblewrap` package with `sudo apt-get update` and
`sudo apt-get install --yes --no-install-recommends bubblewrap`.
This is runner preparation only; no package installation or sudo execution on BigBrain.
The unchanged script still requires **non-setid bubblewrap** and working unprivileged
namespaces. Missing prerequisites, setuid/setgid binaries or namespace failures make
CI red. No fallback, setid bypass, namespace-policy or other security-check change.
GitHub branch-protection required-check configuration is **not verified or changed**.
The existing candidate-package job remains unchanged and explicitly non-accepting.

## Five host conditions — read-only evidence procedure

**Initial 2026-11-27 status: H1–H5 PENDING; superseded by the Nov 28 summary above.** A separately authorized
operator may use existing approved read-only views/evidence. This procedure authorizes
no privilege acquisition, root/sudo, Docker socket/group access, container exec, service
change, secret/.env read, packet probe, data copy, deletion or restore. If the view lacks
necessary facts, record PENDING and the missing field. Desired Compose YAML is not proof
of effective running configuration.

For each condition retain privately: observation UTC, effective runtime/config revision,
complete scoped inventory, evidence provenance, verifier and decision. Public output uses
stable labels (API, Web, Media-A, Finance-store, Backup-A, owner-device-A), counts and
booleans. Never publish raw inspect output, environment, addresses, account/device names,
private paths, logs or credentials; do not hash credentials. Bind sanitized results to
an opaque private evidence reference and candidate SHA/tree. Unknown coverage is PENDING;
an observed prohibited edge is FAIL. Repeat after relevant topology/config changes.

| Condition | Read-only collection/comparison | PASS requirement |
| --- | --- | --- |
| H1: Docker socket absent from API/Web/Media | From an already authorized runtime inventory view enumerate **all** running instances, effective mounts/types/readonly flags, ancestor host-directory coverage and device mounts. Privately resolve aliases and alternate socket locations without opening any socket. | Complete inventory; zero direct, aliased or ancestor-mounted Docker sockets. Publish per-role mount coverage and zero exposure count. |
| H2: unprivileged Media; no Finance volume/network | Inspect effective privileged flag, UID/GID/user-namespace mapping, capabilities, security options, devices, host PID/IPC/network sharing, mounts and networks. Compare mount sources/ancestors/aliases with Finance storage. Review effective routes/firewall/published Finance ports, including indirect reachability. | Every Media instance unprivileged; no root-equivalent device/capability/namespace route, Finance storage edge or reachable Finance network/service. Different network names alone are insufficient. Missing effective policy facts remain PENDING. |
| H3: no broker/trading keys on server | Review existing credential-management **metadata/capability inventory**, deployment/image/volume/backup custody records and principal/location coverage without opening values. Distinguish data-only credentials from account/order-capable credentials using existing authoritative capability evidence; no provider calls. Include inactive services, images, backups and user storage. | Complete authoritative inventory proves zero broker/trading-capable keys. Git scans or environment-name checks alone are insufficient. Unknown location/capability coverage is PENDING; capable credentials are FAIL, with removal requiring a separate decision. |
| H4: Tailscale restricts server access to owner devices/accounts | Read current effective policy revision and device/tag/group/route membership via an authorized read-only admin view/export. Expand ACLs/grants/autogroups/wildcards, subnet/exit/shared-device routes. Build server-principal → owner-device/account-service → protocol/port matrix; compare each allowed edge to a documented necessary purpose. Review existing policy-test evidence for the same revision; do not send probes or edit rules. | No unnecessary owner endpoint reachable; necessary exceptions bounded and justified. Account access through credentials/public/non-Tailscale routes is also covered. Tailscale alone cannot prove account isolation. Unknown membership/route/account coverage stays PENDING. |
| H5: compromised containers cannot delete backups | Review effective custody, mounts/ancestors/aliases, ownership/ACLs, UID mappings/capabilities, backup-service permissions and retention/immutability metadata. Include indirect deletion through parent directories, management APIs or container-accessible credentials. Read existing protected backup/drill receipts only, not payloads; no delete or restore experiment. | No container principal can unlink/rename/overwrite backups, change protection or invoke indirect deletion. Complete custody evidence; same-owner chmod/read-only source flags alone are insufficient. |

H5 deletion isolation and the separate [daily-backup/real-restore gate](../backup-restore/README.md#nivå-2-grind--evidens-och-återstående-verifiering)
are distinct and both remain pending here. Review existing dated daily-run coverage and
real isolated-restore receipts against the exact store/revision/manifest; missing receipts
cannot be replaced by synthetic tests. A new restore needs a separate bounded operation.
ADR 0028 provider tagging, restricted-copy/retention rules and BB-132K remain binding.

## Follow-up verification and checkpoint recovery — 2026-11-27

Owner authorized additional repair resources after the first direct BigBrain run (232 PASS / 2 FAIL / 0 SKIP). One additional bounded repair supplied a synthetic DNS file containing no nameservers and asserted the exact sandbox PWD=/app. No host DNS, privileges, production data or network policy were changed. Direct unprivileged Release build: 0 errors/warnings; isolated suite: 234 PASS / 0 FAIL / 0 SKIP; Python: 5 PASS; resolved dependency closure: PASS (12 files); Git diff checks: PASS. Remote main verified at the original baseline from the existing authorized operator environment. H1–H5, real execution boundary and independent candidate review remain PENDING. This prior direct-run evidence is not GitHub CI evidence; the later GitHub failure is recorded below. The historical sandbox failures below are retained as historical evidence, superseded for the direct run only.

## Bounded GitHub CI prerequisite repair — 2026-11-27

GitHub job [140719681087](https://github.com/T-bear/BigBrain/actions/runs/46062442703/job/140719681087)
failed with `BWRAP_MISSING` and exit 1 before the isolated suite started. Its log identifies
the PR #126 test merge `5ad6c2a85bf7fe227eedf9ce536479185a27c1d8`, combining checkpoint
`6d31d130bd19e4ce3fa73b4b7cea283f47be92ef` with main
`bb8c4b021dea69a674df92ade79b45d3c440fb6e`. Dependency checks, 5 Python tests and
the Release build passed in that job; they do not turn the isolation failure into PASS.

The owner authorized this risk-3 repair only: install the Ubuntu distro package on
the disposable GitHub-hosted runner before the unchanged fixture command. The new
GitHub run is **PENDING / NOT RUN** for this repair. The earlier 234/234 direct BigBrain
result remains separate historical evidence. No additional repair, host change, merge,
deployment, real-data execution or qualification acceptance is authorized here.

GitHub API verification in this repair confirmed main and checkpoint at the identities
above; local SSH verification remains blocked by configuration permissions. Publication
of this repair is pending; local Git metadata is outside the writable workspace.

Local repair checks: **PASS** — 5 focused Python tests, resolved dependency boundary
(12 source files), `git diff --check`, and YAML parsing with semantic comparison against HEAD.
Documentation validation passed before the final evidence/metadata edits (298 Markdown
files / 57 backlog IDs); its final rerun is **BLOCKED** by `spawnSync git EPERM`, not PASS.
The only
workflow change is the installation step immediately before the unchanged fixture
command. The first semantic-comparison attempt was blocked by Node `spawnSync git EPERM`;
comparison passed using the same baseline read by Git directly. The isolation script
and existing untracked `scripts/__pycache__/` bytes are unchanged. No local package
installation, sudo, new C# fixture run or GitHub run was performed for this repair.

## Verification record

Prior local results dated 2026-11-27, before the GitHub failure and bounded CI repair
above; exact checkpoint identities come from Git metadata. These are retained evidence,
not verification of the repaired workflow.

| Command/check | Result |
| --- | --- |
| HEAD, branch, cached `origin/main`, initial worktree | PASS: supplied baseline, clean worktree. |
| `git ls-remote origin refs/heads/main` | FAIL/BLOCKED: local SSH configuration permissions rejected; remote not verified, configuration unchanged. |
| Focused cached-package restore | PASS on explicit minimal-output invocation; initial quiet invocation exited 1 without diagnostics. No provider access. |
| Python dependency suite; `verify-finance-research-boundary.py --resolved` | PASS: 5 tests including negative mutation groups; 12 source files and exact resolved closure. |
| Initial standalone C# fixture run | FAIL: 233 total, 230 PASS, 3 FAIL (missing conditional-SKIP reason); corrected in repair 1. |
| Release build with `--no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false` | PASS: 0 errors/warnings. Build-server invocation had exited 1 without compiler diagnostics; disabling build servers fixed tooling only. |
| `test-finance-research-isolated.py` | FAIL/BLOCKED: NETLINK_ROUTE socket denied during bubblewrap namespace setup; suite not started, no fallback. |
| Semantic `dotnet format ... --verify-no-changes --no-restore` | FAIL/BLOCKED: formatter build-host IPC denied; no formatting defect established. |
| `dotnet format whitespace --folder tests/BigBrain.Finance.OutcomeAudit.Tests --include ResearchIsolationTests.cs --verify-no-changes` | PASS; whitespace only, does not substitute for semantic format verification. |
| `dotnet tests/BigBrain.Finance.OutcomeAudit.Tests/bin/Release/net10.0/BigBrain.Finance.OutcomeAudit.Tests.dll -noLogo` | PASS: 234 total, 231 PASS / 0 FAIL / 3 explicit isolated-environment SKIP. A subsequent optional `-help` probe exited 3 (unsupported option); it was not a test failure. |
| `-class BigBrain.Finance.OutcomeAudit.Tests.ResearchIsolationTests -failSkips -noLogo` | PASS as negative control: expected exit 1, 4 total / 3 FAIL_SKIP / 0 SKIP. The command's red result proves absent isolation cannot silently pass the dedicated suite. |
| `node scripts/verify-documentation.mjs`; `git diff --check` | PASS before the final publication/recovery notes: 298 Markdown files, 57 unique backlog IDs; no whitespace errors. Final documentation rerun BLOCKED by `spawnSync git EPERM`; final note validation is not claimed PASS. |
| Secret scanner | SKIP: gitleaks not installed; no GitHub secrets job run. Changed text reviewed locally; not equivalent to scanner PASS. |
| Python syntax; timezone prerequisite; workflow parser | PASS: all three new Python files parse and calendar zoneinfo exists. YAML parser SKIP: PyYAML unavailable; workflow reviewed as text, GitHub parsing PENDING. |
| H1–H5, actual source/custody/rights/backup qualification | PENDING / NOT RUN. |
| GitHub CI, required-check enforcement and independent risk-3 review | PENDING / NOT RUN. |

No production action or rollback occurred; cleanup concerns fresh synthetic fixtures only.
Repository test success cannot close these gaps or downgrade Finance.
Two source repair rounds used: (1) conditional-SKIP declaration and completion of compiled/
resolved dependency coverage; (2) the public timezone mount required by the unchanged
calendar. No further automatic repair loop in the original mandate. The follow-up owner-authorized repair and direct isolated execution are recorded above.

Historical publication attempt in Codex sandbox: **commit FAIL/BLOCKED** because Git could not create `index.lock`
on its read-only metadata filesystem. At that attempt, changes remained in the worktree/index,
no candidate commit/tree existed, and remote SHA/tree and main were unverified after SSH
rejection. Subsequent publication produced the checkpoint identified above; that historical
failure is not its current publication state. Independent exact-candidate review and a successful isolated CI run
remain necessary next evidence, together with H1–H5 and an explicit real execution scope.

Reproduction from the repository root (restore uses the local package cache in this run;
ordinary CI restore uses NuGet before isolation):

```sh
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/deployment -p 'test_finance_research_boundary.py' -v
dotnet build tests/BigBrain.Finance.OutcomeAudit.Tests/BigBrain.Finance.OutcomeAudit.Tests.csproj --no-restore --configuration Release --disable-build-servers -m:1 -p:UseSharedCompilation=false
python3 scripts/verify-finance-research-boundary.py --resolved
dotnet tests/BigBrain.Finance.OutcomeAudit.Tests/bin/Release/net10.0/BigBrain.Finance.OutcomeAudit.Tests.dll -noLogo
python3 scripts/test-finance-research-isolated.py
node scripts/verify-documentation.mjs
git diff --check
```
