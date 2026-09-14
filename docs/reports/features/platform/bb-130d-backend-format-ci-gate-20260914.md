# BB-130D — backend format CI gate

## Metadata

- Date: 2026-09-14.
- Baseline/accepted main: `e6fca47d87bc77bce153e77c98251d3b7d5e546b`.
- Branch: `bb-130d/backend-format-ci-gate`; approved candidate `332bc90975f6479d25622635f13d4b5567f82634`.
- Scope: one backend CI verification step and canonical documentation only.
- Detta är en sanerad GitHub-version. No secrets, private addresses, user data or raw logs.

## Status

**ACCEPTED / MERGED / CI VERIFIED / ENABLED ON MAIN.**
Owner/architect approved candidate `332bc90975f6479d25622635f13d4b5567f82634` unchanged.
Merge/main `47542cc9815b4f95c2dbb1c73dc8538dd29d0240` passed
[CI 34842088867](https://github.com/T-bear/BigBrain/actions/runs/34842088867):
backend, frontend, documentation and secrets SUCCESS. Backend formatter step 5 actually
executed and passed, after restore step 4 and before build/test steps 6/7.
The check-only gate is now enforced on main. Original candidate evidence below is historical.
A/B COMPLETE; C COMPLETE / EXIT APPROVED; D IN PROGRESS. D1 and whitespace cleanup
remain ACCEPTED / MERGED / CI VERIFIED; backend format baseline CLEAN. D2 NOT STARTED.
Finance RESEARCH / 0 SEK / NONE. No deployment, runtime/device approval or scientific change.

## Evidence

Preflight fetch verified exact baseline and clean tracked tree. Unrelated mockups and ADR
0006–0009 remain preserved/excluded. Baseline
[CI 34806907520](https://github.com/T-bear/BigBrain/actions/runs/34806907520) passed;
that run is baseline evidence only.

Installed .NET 10 SDK feature band 300, patch 302 and formatter version are unchanged;
exact `dotnet --version` and `dotnet format --version` output matches
[TESTING's D1 toolchain record](../../../../TESTING.md#bb-130d1-backend-quality-gate-baseline--2026-09-14).
No SDK/global.json/analyzer/EditorConfig/package changes. Known MSBuild/Roslyn/test local-pipe
permissions from preceding checkpoints were used for the local format/build/test commands.

The following sequence ran both before the CI edit and afterward:

| Command | Baseline | Candidate |
| --- | --- | --- |
| `dotnet restore BigBrain.slnx` | exit 0 | exit 0 |
| `dotnet format BigBrain.slnx --verify-no-changes --no-restore` | exit 0, clean | exit 0, clean |
| `dotnet build BigBrain.slnx --configuration Release --no-restore` | exit 0, zero warnings/errors | exit 0, zero warnings/errors |
| `dotnet test BigBrain.slnx --configuration Release --no-build` | exit 0, API 664/664 + Sentinel 32/32, none skipped | exit 0, API 664/664 + Sentinel 32/32, none skipped |

### Gate behavior and scope

One explicit `run` step sits after restore and before Release build/test. The
`--verify-no-changes` flag checks without fixing; `--no-restore` uses the restored solution
without another restore. No formatter write/fix invocation occurred.
A nonzero formatter exit fails the backend job under its normal step behavior; no
continue-on-error, failure masking, conditional skip or alternate success path was added.
The existing D1 failure characterization supplies evidence that format debt yields a nonzero
exit; this checkpoint does not deliberately dirty source or manufacture a failing CI run.

A byte-exact workflow comparison confirms removing just the inserted line restores the
baseline workflow. This verifies checkout/setup, existing backend restore/build/test,
frontend install/test/build, documentation and full-history secrets jobs remain unchanged.
No source/test/frontend/project/configuration files were reformatted or edited. Local Web
suites were not rerun because the frontend job and its execution settings are byte-identical;
this is not a frontend checkpoint. No new application behavior needs new tests.

### Publication checks

- `node scripts/verify-documentation.mjs`: exit 0; 238 Markdown files / 90 unique BB IDs.
- `git diff --check` and `git diff --cached --check`: exit 0.
- Gitleaks v8.28.0 `git --log-opts='--all' --redact --no-banner`: exit 0; 267 commits, no leaks.
- Gitleaks v8.28.0 `git --pre-commit --staged --redact --no-banner`: exit 0; no leaks.
- Diff review confirms only one CI line and seven documentation files. Prepublication fetch
  must still match the exact baseline; after push, remote candidate must match HEAD and
  origin/main must remain unchanged.

## Changes

- `.github/workflows/ci.yml`: one check-only backend formatting step.
- `TESTING.md`, `docs/STATUS.md`, `docs/BACKLOG.md`,
  `docs/architecture/bb-130-stabilization.md`, `docs/operations/codex-recovery.md`,
  `docs/reports/REPORT-CATALOG.md` and this report: candidate state, verification and handoff.

Historical D1 and cleanup reports remain unchanged. README, ARCHITECTURE/ADRs, module,
knowledge/index documents and runtime/security/rollback runbooks were assessed; no behavioral
or architectural update is needed. The report catalog provides discovery.

## Security

Nullable, TreatWarningsAsErrors and latest-recommended analyzers remain intact. No authority,
API/schema/data/provider or Finance behavior changes. RESEARCH / 0 SEK / NONE; no broker,
orders, PAPER/LIVE/AUTO, capital, provider activation or scientific retuning. Fail-closed and
NOT EVALUABLE behavior remain unchanged. No runtime services or user data were touched.

## Accepted-main verification and publication reconciliation

On 2026-09-14, after merge, the same four commands in the table above ran locally against
exact merge/main: restore exit 0; check-only format exit 0 / CLEAN; Release build exit 0
with zero warnings/errors; full tests exit 0, API 664/664 and Sentinel 32/32, none skipped.
No formatter write mode, application/source changes or additional CI edits occurred.
Only seven canonical documentation files are reconciled; D1 and cleanup reports remain intact.
Publication checks passed: documentation verifier exit 0 (238 Markdown files / 90 BB IDs),
unstaged/staged diff checks exit 0, Gitleaks v8.28.0 staged and full-history (268 commits)
exit 0 with no leaks. The verifier required permission for its Git subprocess; its required
Resumption heading was restored before the successful run. Exact final-main CI must also
pass all four jobs, with formatter execution/order verified again.

## Remaining work

D remains IN PROGRESS; D2 NOT STARTED and separately authorized. No deployment/runtime/device
approval, Research Learning or Finance work follows from publication.

## Resumption

The final reconciliation is the main commit `docs: reconcile accepted BB-130D backend format gate`.
Resolve its SHA from Git history and its exact-head CI from GitHub Actions as described in
[recovery](../../../operations/codex-recovery.md). Its own SHA/run cannot be embedded in itself;
GitHub commit/run records provide final publication evidence. Stop after final CI verification
and return to ChatGPT with "Codex är klar".
