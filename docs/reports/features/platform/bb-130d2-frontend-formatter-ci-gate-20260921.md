# BB-130D2 — Frontend Formatter CI Gate

## Metadata

- Date: 2026-09-21.
- Baseline: `1e3552c5142ee3360e6a96036e8f7ed86b4f4d57`.
- Branch: `bb-130d/frontend-formatter-ci-gate`.
- Detta är en sanerad GitHub-version. No secrets, private addresses or raw sensitive logs.
- Prior [accepted cleanup](bb-130d2-frontend-formatter-cleanup-20260920.md) and
  [formatter tooling](bb-130d2-frontend-formatter-tooling-20260917.md) remain unchanged.

## Status

**IMPLEMENTED / TESTED / REVIEW CANDIDATE ONLY.** Not accepted, merged, deployed or
owner verified. BB-130D remains IN PROGRESS. The frontend gate is implemented on this
candidate only; it is not yet enforced on main. No lint or subsequent checkpoint started.

## Changes

Exactly one workflow line: `.github/workflows/ci.yml`, frontend job, adds
`- run: npm run format:check` immediately after `npm ci`, before `npm test -- --run`
and `npm run build`. Existing job working directory is `src/BigBrain.Web`.
The normal GitHub Actions run step propagates nonzero exit status; there is no
continue-on-error, conditional skip, failure suppression, write mode or auto-fix.
Backend restore/format/Release build/test, documentation and secrets jobs are unchanged.

The existing package script remains the command authority:

```text
prettier --check --config .prettierrc.json --no-editorconfig "src/**/*.{ts,tsx}" vite.config.ts
```

Prettier is pinned exactly 3.9.6, dev-only, installed by npm ci from the unchanged lock.
The unchanged config is singleQuote=true, semi=false, tabWidth=2, useTabs=false,
trailingComma=all, arrowParens=avoid, endOfLine=lf, jsxSingleQuote=false,
embeddedLanguageFormatting=off, printWidth=120. No command/config duplication in CI.
Scope on disk was compared with tracked paths: exactly 83 (17 TS, 66 TSX), including
vite.config.ts. CSS, JS, JSON, dependencies and generated output are excluded.

## Evidence

Baseline and post-edit runs each executed, in order, from the frontend directory:

| Command | Baseline | Post-edit |
| --- | --- | --- |
| `npm ci` | exit 0 | exit 0 |
| `npm run format:check` | exit 0, 83/83 | exit 0, 83/83 |
| `npm test -- --run` | exit 0, 199/199, 26 files | exit 0, 199/199, 26 files |
| `npm run build` | exit 0 | exit 0 |
| `npm audit --json` | exit 0, zero findings | exit 0, zero findings |

Negative characterization: saved exact bytes of tracked `src/main.tsx`, first verified
against HEAD. Temporarily changed only `import { StrictMode }` to `import {StrictMode}`.
`npm run format:check` exited 1 and reported only that file. A finally block restored
exact original bytes; byte equality and `git diff --exit-code -- src` both passed.
A further `npm run format:check` exited 0. No deliberate violation remains or is committed.
No formatter write-mode command was executed.

The historical ThemeControl.test.tsx two-pass convergence belongs to the accepted cleanup
only. Committed source already conforms. No special case or second write pass is used.

Repository checks: documentation verifier passed (244 Markdown files, 90 unique backlog IDs);
`git diff --check` and `git diff --cached --check` passed. Staged Gitleaks 8.28.0 and
full-history scan (279 commits) passed with no leaks. All checks exited 0.
Source/package/config invariance is verified by empty diff against baseline for all source,
tests, package.json, package-lock.json, .prettierrc.json, CSS, excluded JS, backend and
runtime/deployment files. The complete diff contains only this workflow line and seven docs.

## Security

Finance remains **RESEARCH / 0 SEK / NONE**. No Finance code, data, lineage, dataset/revision,
backtest, robustness, entitlement or fail-closed behavior changed. No provider/broker/orders,
PAPER/LIVE/AUTO, capital, Research Learning, deployment or runtime change.
Unrelated untracked mockups and unpublished ADR 0006–0009 are preserved and excluded.
No dependency updates, lint tooling, application refactor or new packages.

## Remaining work

Architect review and owner approval of the exact candidate SHA before merge. GitHub Actions
execution on accepted main remains a later publication step; local positive/negative checks
do not claim hosted CI execution. No runtime/device/UX or blanket security approval.
Audit zero is a dated registry result, not proof that every possible vulnerability is absent.
Rollback: separately authorize removal/revert of the single frontend format-check CI step;
no source, dependency or runtime rollback is needed.
README, architecture/ADRs, module contracts, roadmap, knowledge/indexes and deployment runbooks
were assessed; no changes are needed because application architecture and runtime are unchanged.

## Resumption

Read AGENTS.md, START-HERE and the canonical recovery note. Resolve the exact remote
`bb-130d/frontend-formatter-ci-gate` SHA; compare main with the baseline before any approved
merge. Stop after candidate publication. Do not start BB-130D exit, lint or another checkpoint.
