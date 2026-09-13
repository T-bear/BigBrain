# BB-130D1 — backend quality-gate baseline

## Metadata

- Date: 2026-09-14 (local characterization began 2026-09-13, Europe/Stockholm).
- Baseline/source of truth: `df848b422022d7a257eff34d27d2572503ff807c`.
- Branch: `bb-130d/backend-quality-gate-baseline`.
- Candidate: the documentation commit containing this report; resolve its exact SHA
  from the remote branch. Main remains the accepted source until explicit approval/merge.
- Scope: existing backend build/test/format characterization and frontend tooling inspection.
- Detta är en sanerad GitHub-version. Only aggregate diagnostics and repository paths
  are published; local raw reports/logs are not publication artifacts.

## Status

**REVIEW CANDIDATE ONLY — evidence checkpoint, no CI gate implemented.**
A/B COMPLETE; C COMPLETE / EXIT APPROVED; D started with D1 only and remains partial.
Backend baseline automatically verified. Full format verification reproducibly fails
on existing whitespace debt; this is not a green formatter implementation or a reproduced
correctness/security/scientific/lineage defect. No source fixes or analyzer-policy changes.
No deployment, runtime/device testing or owner UX approval. Finance RESEARCH / 0 SEK / NONE.

Baseline [CI 34367694026](https://github.com/T-bear/BigBrain/actions/runs/34367694026)
succeeded for the exact baseline SHA. Candidate CI is not claimed: the unchanged workflow
runs on main pushes and pull requests, not ordinary checkpoint branch pushes.

## Evidence

Clean tracked baseline, with unrelated untracked mockups and ADR 0006–0009 preserved
and excluded. `git fetch origin` and `git rev-parse origin/main HEAD` matched the baseline.
`git diff --exit-code` after the first format run returned 0: verification wrote no tracked files.

Toolchain: .NET 10 SDK, feature band 300, patch 302; exact SDK and formatter version
strings are recorded in [TESTING](../../../../TESTING.md#bb-130d1-backend-quality-gate-baseline--2026-09-14).
The report sanitizer treats three-part .NET version strings as private-address patterns;
its rule is preserved. `global.json` requests feature band 300 with latestPatch; CI setup
requests the .NET 10 release family and the
repository SDK resolver still applies. There is no tracked EditorConfig or formatter package.
All seven solution projects target net10.0; the solution includes the persistence benchmark.
Nullable, TreatWarningsAsErrors and AnalysisLevel latest-recommended remain unchanged.

| Command | Exit/result |
| --- | --- |
| `dotnet restore BigBrain.slnx` | 0; all projects restored/up to date |
| `dotnet build BigBrain.slnx --configuration Release --no-restore` | 0; zero warnings/errors |
| `dotnet test BigBrain.slnx --configuration Release --no-build` | 0; API 664/664, Sentinel 32/32, zero skipped |
| `dotnet format BigBrain.slnx --verify-no-changes --no-restore --report /tmp/bb130d1-format-first.json` | 2; 59 files would change, 22,573 WHITESPACE diagnostic locations |

Tests and formatter initially exited 1 inside the sandbox because local MSBuild/Roslyn
pipe connections were denied; the same commands were rerun with approved host execution.
Those startup failures are not test failures or formatter debt evidence.
The format command was executed twice with the same SDK/source/options; the first JSON
was copied to `/tmp/bb130d1-format-baseline.json` before the second completed.
Both runs exited 2. Sorted file paths plus complete FileChanges arrays (diagnostic IDs,
line/column locations and descriptions) were identical. Volatile DocumentId values were
excluded from comparison. This proves repeatability on this SDK/source, not across SDK patches.
Counts describe formatter-reported locations, not changed lines or independent bugs.
Generated files are excluded by the formatter default; scope is solution-loaded documents,
not arbitrary files on disk. No write/fix-mode formatter invocation was made.

| Project area | Files that would change | WHITESPACE locations |
| --- | ---: | ---: |
| src/BigBrain.Api | 28 | 12,847 |
| src/BigBrain.Modules | 7 | 2,379 |
| tests/BigBrain.Api.Tests | 23 | 7,346 |
| tools/BigBrain.Finance.PersistenceBenchmarks | 1 | 1 |
| Total | 59 | 22,573 |

The solution contains 181 tracked C# files. No format changes were reported in
Sentinel, Sentinel.Contracts or Sentinel.Tests. No other diagnostic category was
reported at this command's default severity; this does not assert absence of all
hidden/informational style diagnostics.

### Bounded-scope decision

Do not enable the full gate against this baseline. Whitespace-only verification cannot
solve the observed failure because all reported changes are whitespace. A style/analyzer-only
gate would omit the known debt and overlap existing build analysis. Selecting only clean
projects would leave API/Modules/API tests outside the intended backend baseline without
a responsibility-based reason. No narrower gate is adopted merely to obtain a green result.
Recommend a separately authorized, reviewed whitespace-cleanup checkpoint before enabling
the full check; choose bounded batches from this evidence, preserve behavior and rerun
full backend regression. SDK-patch reproducibility must be rechecked when tooling changes.

### Frontend read-only assessment / D2 input

`src/BigBrain.Web/package.json` has only dev, build and test. Build already executes
`tsc -b && vite build`; TypeScript configs enable strict checking for src and vite.config.ts.
The manifest, lockfile and tracked configuration contain no ESLint, Prettier or Biome.
No dependencies, frontend files or frontend CI steps were changed or installed in D1.

Smallest proposed D2: separately authorize a pinned formatter-only characterization of
`src/BigBrain.Web/src/**/*.{ts,tsx}` and `src/BigBrain.Web/vite.config.ts`, with explicit
check-only scope and documented configuration matching existing single quotes/no semicolons.
There are 83 tracked TS/TSX files including Vite configuration. Avoid adding semantic lint
rules in the same checkpoint; a standalone typecheck script would duplicate the current build.
Likely initial config scope is package.json/package-lock.json plus formatter config and a
check script; add an explicit CI check only after that baseline passes. CSS (13 files), JS
(3 files, including tests/service worker), HTML/JSON/assets and repository documentation
need separate explicit scope, not an implicit repository-wide formatter glob.

Initial pass status is UNKNOWN: no new formatter was installed or run. Dense inline callbacks
and JSX in existing source suggest line-wrapping debt, but this is an inspection-based
expectation, not measured diagnostics. D2 must characterize first and request separate cleanup
scope if broad debt appears. D2 is not started or authorized by this recommendation.

### Publication checks

- `node scripts/verify-documentation.mjs`: exit 0; 236 Markdown files, 90 unique backlog IDs.
  Initial report verification flagged the .NET version as a private-address pattern;
  exact version strings were moved to TESTING and the unchanged verifier then passed.
- `git diff --check` and `git diff --cached --check`: exit 0.
- `/tmp/bb130d1-tools/gitleaks version`: 8.28.0.
- `/tmp/bb130d1-tools/gitleaks git --log-opts='--all' --redact --no-banner`:
  exit 0; 263 commits scanned, no leaks. Repository is not shallow.
- `/tmp/bb130d1-tools/gitleaks git --pre-commit --staged --redact --no-banner`:
  exit 0; no leaks in the D1 staged patch.
- Prepublication `git fetch origin` / `git rev-parse origin/main`: exact baseline unchanged.
  Publication must also compare remote branch SHA with HEAD and recheck main after push.
Frontend tests/build were not rerun: D1 changes documentation only; CI YAML, scripts,
frontend and backend source/configuration remain identical to the verified baseline.
No new behavior requires new tests. No runtime, Compose or deployment configuration is changed.

## Changes

Only seven documentation files change: docs/STATUS.md, docs/BACKLOG.md,
docs/architecture/bb-130-stabilization.md, TESTING.md, docs/operations/codex-recovery.md,
docs/reports/REPORT-CATALOG.md and this report. They record current D1 authority/results,
backend format debt, D2 input and the exact next review boundary. C reports remain unchanged.
README, ARCHITECTURE, ADRs, module contracts, knowledge/index documents and operational
runbooks were assessed: no product, architecture, API, runtime or rollback behavior changed,
so no updates there are needed. The existing report catalog supplies discovery.

## Security

No production code, data, provider/broker settings, credentials or external-service state
was changed. No private addresses, raw payloads, user identities or sensitive logs are
published. Existing Gitleaks rules and historical fingerprint exception remain unchanged.
BB-127 dataset/XLSX semantics; BB-128B/C cache/degraded/single-loader behavior;
BB-129A campaigns; BB-123 cost/execution identity; BB-124 OOS/holdout/integrity;
entitlement/fail-closed behavior and deterministic identities/checksums remain untouched.
NOT EVALUABLE remains valid. No PAPER/LIVE/AUTO/capital or scientific work.

## Remaining work

D1 evidence awaits architect/owner review of the exact remote candidate SHA. Backend
format debt is known and deferred, not repaired. D2 frontend characterization and final D
reconciliation remain separately authorized work. Candidate CI, merge and runtime approval
are not inferred from baseline CI or local tests. Existing accepted post-BB-130 debt remains deferred.

## Resumption

Read START-HERE, STATUS, BACKLOG, the BB-130 plan and the canonical recovery note.
Verify baseline and remote candidate SHA. Next action is architect/owner review before
any merge; do not start cleanup, D2, Research Learning or new Finance work.
