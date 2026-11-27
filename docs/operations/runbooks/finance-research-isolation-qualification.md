# Finance RESEARCH isolation qualification — 2026-11-27

**REVIEW CHECKPOINT / RISK 3 / NOT QUALIFIED.** Owner-authorized repository-only
qualification from `bb8c4b021dea69a674df92ade79b45d3c440fb6e`, on
`checkpoint/finance-research-level2-isolation-20261127`.
The [five host conditions and four CI guarantees](../../policy/risk-policy.md#finance-research-och-isoleringsvillkor)
remain the DoD. No policy change or owner acceptance is recorded.
**RESEARCH / 0 SEK / NONE; G3a BLOCKED / G3b NOT RUN.**

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

**H1–H5 are all PENDING; no host inspection was performed.** A separately authorized
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
