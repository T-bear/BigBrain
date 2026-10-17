# Media integrated search — merge and deployment gate, 2026-10-17

Detta är en sanerad GitHub-version. No secrets, private paths, raw provider data or credentials are published.

## Current outcome — merge completed, deployment blocked by CI

**MERGED / NOT DEPLOYED — CI FAILED.** This section supersedes the earlier PR-permission
blocker below. The owner explicitly authorized direct normal Git merge/push and CI on main;
no PR or permission change was required.

- Verified baseline: `70b659f772b7b7883a5716786859f1e7833a5634`.
- Verified candidate: `1ddfaebff19d67f68bf208c43cf6dd4c3c496b39`, one ahead/zero behind;
  exact reviewed 23-file scope, tracked source clean.
- Merge and verified remote main: `08e1bcea6ae190026d4d60a543067c06b71351b1`.
- First parent: `70b659f772b7b7883a5716786859f1e7833a5634`.
- Second parent: `1ddfaebff19d67f68bf208c43cf6dd4c3c496b39`.
- Merge tree = approved candidate tree: `ad178e419746b9f3d3b3976af429366a3d814bac`.
- No squash, reset, rebase, force push, candidate amendment or implementation correction.

[Exact merge CI run 256](https://github.com/T-bear/BigBrain/actions/runs/39023635027)
started from the push to main. Frontend **FAIL**: `npm test -- --run` reports
214 passed, 1 failed (26 test files passed, 1 failed). The failure is
`src/BigBrain.Web/src/MediaDashboard.test.tsx:189`, test
`keeps technical modules in Administration, closed by default and reachable on request`.
It still requests heading `Hitta film och serier`; the approved UI renders
`Hitta film, serier och ljudböcker`. This is a concrete stale assertion identified in CI,
not a corrected or waived check. Frontend build after that step did not run.
Backend, documentation and secrets jobs **PASS**. All four jobs completed; the workflow
is failed because frontend failed. No automatic rerun.

The task expressly requires stopping before deployment on any CI failure. Accordingly:
**no image was built, no Web deployment/restart occurred, and no other service was changed**.
Prior Web image remains
`sha256:1c10a3caf19eddaa8849eec6f5eee527073fc8a2c9ec66ba9054ff74dfa2ddcd`.
Read-only predeployment checks returned HTTP 200 for Web-proxied `/health`, audiobook overview
and a one-item catalog page. Compose showed API/Web and configured healthchecked companion
services healthy; Audiobookshelf running. These checks establish reachability, not full
catalog/progress integrity or the new UI's deployed behavior. No playback, acquisition or
user-data write was performed. Live Film/Serie search, new UI, postdeployment checks and
physical iPhone/PWA verification remain **NOT RUN**.

Existing 104 focused candidate tests and local build/layout evidence were reused without
rerunning unchanged code. The full CI suite exposed an assertion outside that focused set.
Finance remains FROZEN; no Finance code, runtime, provider, database or model operation.
Unrelated untracked mockups and Sentinel ADR drafts are preserved.

This additive documentation checkpoint stays on `review/media-merge-deployment` and is
**not** a replacement implementation or a branch to merge to main. Resolve its SHA/tree
from subject `docs(media): record merged candidate and CI deployment stop`.
Next decision: authorize the bounded stale-test correction and review its exact commit;
then require successful CI before resuming the already authorized Web-only deployment.
No correction or deployment is included here.

Documentation publication verification: `node scripts/verify-documentation.mjs` PASS
(269 Markdown files, 91 unique backlog IDs, relative links); `git diff --check` PASS;
staged scope limited to this report and the Media recovery section; staged Gitleaks PASS.
No application tests repeated for this documentation-only checkpoint.

## Earlier attempt — historical evidence, superseded above

## Metadata

- Scope: owner-approved exact Media merge and Web-only deployment; stopped at CI prerequisite.
- Accepted main: `70b659f772b7b7883a5716786859f1e7833a5634`.
- Main tree: `6433c956ff87bc8434a9a8fff6c5294fe45b258d`.
- Approved branch: `media/integrated-audiobook-search`.
- Approved candidate: `1ddfaebff19d67f68bf208c43cf6dd4c3c496b39`.
- Candidate tree: `ad178e419746b9f3d3b3976af429366a3d814bac`.
- Documentation-only publication branch: `review/media-merge-deployment`, based on exact accepted main. Resolve publication SHA/tree from subject `docs(media): record CI permission blocker before deployment`.

## Status

**BLOCKED BEFORE MERGE AND DEPLOYMENT.** No new main commit, no image built and no running service changed. The approved candidate branch is preserved unchanged. This publication does not request acceptance of different implementation content.

## Evidence

Fetched origin and verified exact candidate/main identities. Candidate is one commit ahead and zero behind unchanged main; tracked source is clean. Its 23-file delta is limited to Media Web/search/navigation, focused tests, UX Lab and related documentation. No API, Finance, migration or infrastructure implementation delta.

Reused the approved candidate's recorded evidence: 104 focused tests passed, Web build/formatter, documentation/links, whitespace, Gitleaks and isolated Firefox layout checks passed. No unchanged local code tests were repeated. [Immutable implementation report](https://github.com/T-bear/BigBrain/blob/1ddfaebff19d67f68bf208c43cf6dd4c3c496b39/docs/reports/features/media/integrated-audiobook-search-20261017.md).

GitHub connector returned no open PR for the candidate branch, no PR-triggered workflow runs for the exact SHA, and no commit statuses. The accepted `.github/workflows/ci.yml` triggers CI on pull requests and pushes to main, not ordinary review-branch pushes. Therefore local evidence is not described as GitHub CI success.

Creating the prerequisite PR was rejected by GitHub:

`403 Resource not accessible by integration`

No alternative API credential, permission workaround or merge-before-CI was attempted.

Read-only Compose inspection showed API, Web, Sentinel, Librarr and FlareSolverr running/healthy; Audiobookshelf running with no configured health result. These are container states, not proof of catalog/data integrity. Prior running Web image:
`sha256:1c10a3caf19eddaa8849eec6f5eee527073fc8a2c9ec66ba9054ff74dfa2ddcd`.
No Finance or media database inspection, live searches, playback or acquisition was performed.

Documentation publication checks: `node scripts/verify-documentation.mjs` PASS (269 Markdown files, 91 unique backlog IDs, relative links), `git diff --check` PASS, staged scope PASS (three documentation files only), staged Gitleaks PASS (no leaks). No application tests run for this documentation-only delta.

## Changes

Only this operational handoff, report catalog and recovery reference are published on the separate documentation branch. The approved candidate remains exact and main remains unchanged. Unrelated untracked mockups and Sentinel ADR drafts remain untouched.

## Security

Finance remains FROZEN / RESEARCH / 0 SEK / NONE. No Finance deployment, configuration, troubleshooting, model run or provider request. No API/Web restart, image replacement, volume operation, media configuration change or user-data mutation. A later deployment must remain Web-only and preserve API and external media services.

## Remaining work

An authorized GitHub user must create the PR for the unchanged approved branch and allow its CI to complete:
[Open the candidate PR](https://github.com/T-bear/BigBrain/compare/main...media/integrated-audiobook-search?expand=1).
After successful CI, resume the already approved exact-SHA merge/deployment task with fresh identity checks. A failed check, changed candidate or moved baseline requires the owner's specified STOP/review handling. Do not merge this documentation branch as a substitute for the approved implementation.

Live Film/Serie search, Audiobookshelf catalog/data preservation, deployed bundle identity, postdeployment health and physical iPhone/PWA checks are **NOT RUN**. No deployment-complete claim is made.

## Resumption

Read this handoff and AGENTS.md. Reverify candidate `1ddfaebff19d67f68bf208c43cf6dd4c3c496b39`, its tree and main; inspect exact PR CI. Do not repeat valid local tests solely because a PR is opened. The deployment remains conditional on successful CI and safe Web-only operational preflight. No broader deployment or Finance operation is authorized.
