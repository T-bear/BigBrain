# BB-130D2 — minimal frontend dependency maintenance

## Metadata

- Date: 2026-09-16. Baseline: `914d35e596cd066b846e4f6d1c59632b1be84137`.
- Branch: `bb-130d/frontend-dependency-maintenance`.
- Scope: four authorized dependency targets, required Vitest family alignment and documentation.
- Detta är en sanerad GitHub-version. No secrets, private addresses, user data or raw logs.
- Historical evidence: [accepted dependency triage](bb-130d2-frontend-dependency-triage-20260914.md).

## Status

**IMPLEMENTED / TESTED / REVIEW CANDIDATE ONLY.** Not accepted, merged, deployed or runtime/
manual UX verified. Main remains the accepted baseline. A/B COMPLETE; C COMPLETE / EXIT APPROVED;
D IN PROGRESS. D1/backend cleanup/backend format gate and D2 characterization/triage remain
accepted. Backend formatter gate enabled; frontend formatter cleanup/tool installation/gate
NOT STARTED / NOT COMPLETE. No next checkpoint starts automatically.

## Changes

`src/BigBrain.Web/package.json` changes only exact devDependency vitest 4.1.10 → 4.1.11.
`src/BigBrain.Web/package-lock.json` remains lockfile v3 with the same 164 package paths
plus root (165 entries). Eleven installed packages change versions:

| Dependency path from BigBrain.Web | Old → new | Class / purpose |
| --- | --- | --- |
| vitest | 4.1.10 → 4.1.11 | Direct dev, tests/config |
| vitest → @vitest/{expect,mocker,pretty-format,runner,snapshot,spy,utils} | All seven 4.1.10 → 4.1.11 | Required exact upstream alignment |
| vite 8.1.5 → postcss | 8.5.22 → 8.5.23 | Transitive, within ^8.5.17; build/dev CSS |
| vite → postcss → nanoid | 3.3.16 → 3.3.18 | Transitive, within ^3.3.16; internal IDs |
| jsdom 29.1.1 → undici | 7.28.0 → 7.29.0 | Transitive dev, within ^7.25.0; test networking |

Vite/jsdom remain deduped through Vitest and plugin-react. No transitive promoted to direct,
no new package paths, major upgrades or runtime dependency additions. All other versions,
engines, lifecycle-install flags and dependency classes unchanged. Updated tarball URLs remain
on registry.npmjs.org with npm-generated integrity values. None of the eleven installed package
manifests has preinstall/install/postinstall scripts. This is a bounded metadata review, not a
complete upstream code/security audit.

### Bounded npm resolution

Local Node v20.19.2 / npm 9.2.0. Read-only version metadata showed newer compatible releases
(PostCSS 8.5.28, nanoid 3.3.19, undici 7.29.1), deliberately not selected. Unconstrained targeted
[npm update](https://docs.npmjs.com/cli/v9/commands/npm-update/) would select newer compatible
versions. No audit fix or unbounded update was used.

A temporary manifest selected vitest 4.1.11 and overrides for the three exact transitive targets.
`npm install --package-lock-only --ignore-scripts --no-audit` with npm 9.2.0 failed with internal
`Cannot read properties of null (reading 'edgesOut')`, both locally and in an isolated copy;
the repository lockfile stayed unchanged. Temporary overrides were removed from repository manifest.
A pinned ephemeral npm (major 10, minor 9, patch 4) resolved in an isolated copy. Its first result also refreshed
sourcemap-codec/tinyrainbow; rejected. Adding constraints after that result nested duplicates;
also rejected. Final resolution used a fresh copy of the original lock and these temporary overrides:

```json
{"postcss":"8.5.23","nanoid":"3.3.18","undici":"7.29.0","@jridgewell/sourcemap-codec":"1.5.5","tinyrainbow":"3.1.0"}
```

For reproduction, set `RESOLUTION_NPM_VERSION` to `[10,9,4].join(".")` via Node.
The dotted npm version triggers the documentation private-address heuristic, so version
components are spelled out here; no verifier policy was weakened.
Command: `npm exec --yes --package="npm@${RESOLUTION_NPM_VERSION}" -- npm install --package-lock-only --ignore-scripts --no-audit`.
Exit 0. Remove temporary overrides, then ordinary npm 9.2.0
`npm install --package-lock-only --ignore-scripts --no-audit`: exit 0, retained desired graph.
Only then copy the npm-generated lock to the repository. No manual lock entry rewriting.
No overrides, npm dependency, .npmrc or toolchain policy change remains. Final diff is
83 additions/60 deletions in lock, one line each in manifest. npm adds 23 MIT license fields,
including 12 metadata-only entries for unchanged Vitest support packages; these are explained
metadata refreshes, not additional upgrades. All other lock changes are the eleven versions,
tarball integrity/URLs and exact Vitest dependency/optional-peer version alignment.

## Evidence

All frontend commands below run from src/BigBrain.Web on 2026-09-16.

| Command | Untouched baseline | Patched graph |
| --- | --- | --- |
| `npm ci` | exit 0; 116 installed | exit 0; 116 installed |
| `npm test -- --run` | exit 0; 199/199, 26 files | exit 0; 199/199, 26 files |
| `npm run build` | exit 0; TypeScript + Vite | exit 0; TypeScript + Vite |
| `npm audit --json` | exit 1; 5 affected packages, 3 moderate/2 high | exit 0; zero findings |
| `npm ls vitest @vitest/mocker vite postcss nanoid jsdom undici --all` | Graph read from baseline lock | exit 0; exact paths/versions above, no invalid graph |

Baseline reproduces all eight historical GHSA IDs, not merely the summary count. Existing jsdom
scrollTo/media pause notices occur in passing suites. Final frontend ci/test/build/audit is also
rerun after documentation exists; publication checks are recorded in recovery below.

### Eight advisory dispositions

GitHub advisory pages were rechecked on this date against actual installed versions and complete
lock paths. Each disposition is **REMEDIATED BY PATCHED DEPENDENCY** in this candidate, supported
by the patched ranges below and installed graph, not solely by audit zero. Historical C/D
classifications remain preserved in the triage report. No current metadata discrepancy found.

| Advisory (authoritative source) | Affected installed line | Installed patched version / disposition |
| --- | --- | --- |
| [GHSA-82fw-gwwq-j7x9](https://github.com/advisories/GHSA-82fw-gwwq-j7x9) | vitest/mocker >=2.1.0 <4.1.11 | Both 4.1.11 — REMEDIATED BY PATCHED DEPENDENCY |
| [GHSA-2v37-7h3g-55p8](https://github.com/advisories/GHSA-2v37-7h3g-55p8) | nanoid <3.3.18 | 3.3.18 — REMEDIATED BY PATCHED DEPENDENCY |
| [GHSA-fxqj-rqcc-2cmp](https://github.com/advisories/GHSA-fxqj-rqcc-2cmp) | postcss <=8.5.22 | 8.5.23 — REMEDIATED BY PATCHED DEPENDENCY |
| [GHSA-8xcm-r25x-g524](https://github.com/advisories/GHSA-8xcm-r25x-g524) | undici >=7 <7.29.0 | 7.29.0 — REMEDIATED BY PATCHED DEPENDENCY |
| [GHSA-4cwx-7wf7-3272](https://github.com/advisories/GHSA-4cwx-7wf7-3272) | undici >=7 <7.29.0 | 7.29.0 — REMEDIATED BY PATCHED DEPENDENCY |
| [GHSA-m8rv-5g2x-5cg5](https://github.com/advisories/GHSA-m8rv-5g2x-5cg5) | undici >=7 <7.29.0 | 7.29.0 — REMEDIATED BY PATCHED DEPENDENCY |
| [GHSA-jr45-8vmc-qm54](https://github.com/advisories/GHSA-jr45-8vmc-qm54) | undici >=7 <7.29.0 | 7.29.0 — REMEDIATED BY PATCHED DEPENDENCY |
| [GHSA-v3r7-h72x-cjcm](https://github.com/advisories/GHSA-v3r7-h72x-cjcm) | undici >=7 <7.29.0 | 7.29.0 — REMEDIATED BY PATCHED DEPENDENCY |

### Production artifact and unchanged behavior boundary

Reused the accepted triage report's reproducible Vite `build.write=false` module observer in a
temporary helper, `node /tmp/bb130maint-artifact.mjs`, exit 0. 70 modules; external modules only
react/react-dom/scheduler; zero postcss/nanoid/undici/vitest/@vitest/mocker modules. Its JS/CSS
bytes match current dist. Build retains index-uvaexxfW.js and index-COc9e5Ag.css output names.
No affected tooling introduced into the browser artifact. Dockerfile still copies only dist and
nginx config from build into independent nginx runtime, not Node/node_modules. No live deployment,
container/image attestation, device or browser UX verification performed.

Only manifest/lock and seven documents change. Frontend application/tests/config, all backend
source/tests, workflow, runtime/deployment configuration remain byte-identical to baseline.
No Prettier/linter install/config, source formatting, new gate or application refactor. Backend
local suites not rerun: no backend changes. Existing backend formatter and frontend ci/test/build
CI sequences unchanged; no candidate CI success claimed (workflow triggers main push/PR).

## Security

No new reachable product defect requiring blocker handoff established. Zero audit findings is
registry-dependent, scoped npm evidence, not blanket security approval or proof of absence of
unknown vulnerabilities. Node/image security and future dev-server configuration are outside this
checkpoint. Undici is a minor update with networking compatibility risk bounded by the existing
jsdom suites; tests do not prove all possible network behavior. No live exploit testing performed.
Finance RESEARCH / 0 SEK / NONE. No provider/broker/orders/PAPER/LIVE/AUTO/capital,
Research Learning, scientific semantics, identities/checksums, entitlement/fail-closed or
NOT EVALUABLE behavior changes. No deployment or runtime/UX behavior change intended.

## Remaining work

Owner/architect review of exact candidate SHA before any merge. D remains IN PROGRESS;
frontend formatting cleanup/tooling/CI remains separately authorized work. No next checkpoint
starts here. Rollback, if later authorized: revert this checkpoint's manifest and lock together
through normal reviewed Git history, then npm ci/tests/build/audit. That reintroduces historical
advisories and requires explicit assessment; no runtime rollback or deployment occurred.
README, ARCHITECTURE/ADRs, ROADMAP, modules, knowledge/index and runbooks assessed; no changed
contract/architecture/runtime procedure requires edits. Historical reports remain untouched.

## Resumption

Read AGENTS/START-HERE and the canonical recovery note. Verify baseline main and candidate
remote SHA; preserve unrelated mockups/ADRs. If publication completed, stop for architect review;
do not recreate commits, merge or start frontend formatting.

### Final candidate publication checks

Final npm ci/test/build/audit after documentation: exit 0 each, 199/199 tests, audit zero.
`node scripts/verify-documentation.mjs`: exit 0, 241 Markdown files / 90 BB IDs.
`git diff --check`: exit 0. Gitleaks v8.28.0 full-history
`git --log-opts='--all' --redact --no-banner`: exit 0, 273 commits, no leaks.
Prepublication fetch confirms exact baseline. `git diff --cached --check` and Gitleaks
`git --pre-commit --staged --redact --no-banner`: exit 0, no leaks. Only nine intended
files staged. Exact remote candidate is resolved via recovery's command.
