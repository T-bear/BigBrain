# BB-130D — backend format CI gate

## Metadata

- Date: 2026-09-14.
- Baseline/accepted main: `e6fca47d87bc77bce153e77c98251d3b7d5e546b`.
- Branch: `bb-130d/backend-format-ci-gate`; candidate is the commit containing this report.
- Scope: one backend CI verification step and canonical documentation only.
- Detta är en sanerad GitHub-version. No secrets, private addresses, user data or raw logs.

## Status

**IMPLEMENTED / LOCALLY VERIFIED / REVIEW CANDIDATE ONLY.**
The candidate backend job enforces check-only formatting. The gate is NOT accepted,
merged or enforced on main yet; candidate CI is not claimed. The unchanged workflow triggers
main pushes/pull requests, so branch publication alone does not run candidate CI.
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

## Remaining work

Architect/owner review of the exact remote candidate SHA before merge. Only a separately
approved merge and exact-main CI can establish accepted/enforced-on-main status. D remains
in progress; D2, deployment, Research Learning and Finance features are not started.

## Resumption

Read START-HERE and the canonical recovery note. Verify baseline and remote candidate SHA,
preserve unrelated work, then return to ChatGPT with "Codex är klar". Stop before merge or
any next checkpoint. The commit title is `ci: enforce backend format verification`.
