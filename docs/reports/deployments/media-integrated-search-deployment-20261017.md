# Media integrated search — merge and deployment gate, 2026-10-17

Detta är en sanerad GitHub-version. No secrets, private paths, raw provider data or credentials are published.

## Mediekontroll Web-only deployment and process-discipline merge — 2026-10-17

**MEDIA DEPLOYMENT COMPLETE — automated checks PASS; new physical iPhone/PWA acceptance pending.**
This section is the latest deployment evidence; earlier dated sections remain history.

Owner-approved process-discipline candidate `03d7116f85fe6fa33e96a294a2732493bbff7260`,
tree `408dfeeabe24acdbb5701e8d0a334c2cad986695`, was verified against unchanged main
`3375e83c871e5ce333238e4447de7e3edc143435`: exactly 18 added lines in AGENTS.md,
zero deletions and no other changed file. Normal merge pushed as
`133b2b19287c74368828e6904cef241248bdae5e`, with first parent the baseline and second
parent the exact candidate; merge tree equals candidate tree. No history rewrite.
[Merge CI](https://github.com/T-bear/BigBrain/actions/runs/39067345966): frontend,
backend, documentation and secrets all **SUCCESS** for that exact merge SHA.

The separately approved Web deploy source is Mediekontroll merge
`3375e83c871e5ce333238e4447de7e3edc143435`, tree
`accd5365a3e097495275aa284c4752e0dcb36fe4`.
[Deploy-source CI](https://github.com/T-bear/BigBrain/actions/runs/39066173388):
all four jobs **SUCCESS**, verified before deployment. Web source, Dockerfile, Compose
and build exclusions are identical between this deploy SHA and the new documentation-only main.
Reused unchanged candidate tests/build evidence; no local application test suite repeated.

The existing Compose/Web Dockerfile built only `web` from a temporary `git archive` of
the deploy SHA, with `--env-file /dev/null`; ignored/untracked files and local secrets were
excluded. After successful build, existing Compose executed
`docker compose up -d --no-deps --no-build --wait --wait-timeout 60 web`.
Only Web was recreated. Running image:
`sha256:48f8ba78000cd1c619a73f4d8bd55c0ecf01e2a110ddb7ac0351a6a17ecf9942`.
Started `2026-10-17T20:37:38.694649266Z`; container **healthy**, `/` and `/health` HTTP 200.
The existing image has no compiled/OCI Git identity; attribution uses exact archived source,
captured build/image identity and verified served assets, not an invented revision endpoint.

HTTP-served files equal running-container files byte-for-byte (SHA-256):

- `index.html`: `6994ab8da9f9b4a73c55c81b895d0a261795f95bf723ff80e7476b11a1f8e0fc`.
- `index-CuUnHOTD.js`: `57011db688b010ad930d5b53917fbcf02c0366fb86953ed629d88c655f108864`.
- `index-C1iS_zo5.css`: `0e49b1cf19d662f0788cf61bc66d1811047e80ba659405d73eb2df4481f70d51`.

Overview and all three direct routes returned the correct SPA shell. Disposable Firefox loaded
the actual deployed overview at 390×844: three ordered cards, no horizontal overflow, minimum
card height 94 px and visible keyboard focus. API fetches were disabled only in that disposable
browser, so this proves deployed rendering, not live download/job/provider actions. The first
harness invocation failed because Node 20 did not enable its WebSocket global; rerunning with
the existing `--experimental-websocket` flag passed. No product change or installed dependency.
Physical iPhone/PWA cache, navigation and acceptance remain pending for Mediekontroll.

Build audit reported two high-severity dependency entries. Read-only package-lock audit/lineage
identified `source-map-js@1.2.1` through PostCSS/jsdom and `undici@7.29.0` through jsdom.
These are existing build/test dependencies; the final Nginx stage copies only static output,
not Node or node_modules. No dependency fix was made or audit success claimed. This is retained
toolchain maintenance evidence, not a demonstrated vulnerability in the served application.

All **14 non-Web containers** retained exact IDs, image IDs, start times, status and restart
counts. Web has no persistent mounts. No API, Finance, external-media service, database, volume,
configuration, provider call, model invocation or user-data operation. Finance stays **FROZEN**.
Unrelated local mockups and Sentinel drafts preserved. This single necessary runtime-evidence
update continues `review/media-merge-deployment`; it is not merged into main. Resolve publication
by subject `docs(media): record verified Mediekontroll Web deployment`.
Documentation/relative-link validation, whitespace, report-only scope and staged Gitleaks PASS.

## Approved CI correction and Web-only deployment — 2026-10-17

**MEDIA DEPLOYMENT COMPLETE — automated deployment checks passed; physical iPhone/PWA review pending.**

The owner approved exact test correction `56c4995673b9283be0ad83cc8d27bedd05c512e6`,
tree `a36bb7dcd390c51be395a3de1105cbaefa5c4a29`, and conditional Web-only deployment.
Fetched origin: candidate one ahead/zero behind expected main; only one test assertion and
its two documentation files changed. Tracked source clean; unrelated files preserved.

Normal merge pushed and remote verified:

- Main/merge: `628041b200b1ff1283e29955073db91539bac422`.
- First parent: `08e1bcea6ae190026d4d60a543067c06b71351b1`.
- Second parent: `56c4995673b9283be0ad83cc8d27bedd05c512e6`.
- Tree: `a36bb7dcd390c51be395a3de1105cbaefa5c4a29`, identical to approved candidate.
- [Exact merge CI](https://github.com/T-bear/BigBrain/actions/runs/39025570041):
  **SUCCESS**, frontend/backend/documentation/secrets all completed **SUCCESS** before build.

Reused the correction's 215 passing local frontend tests and the exact merge CI; no redundant
local suites. No PR, new implementation, permission change or history rewrite.

### Web build and deployment evidence

**Build, Web-only recreation and read-only browser verification PASS.**
Build uses existing Compose/Web Dockerfile with a temporary `git archive` of exact merge SHA
and `--env-file /dev/null`. Only `build web` is selected. Ignored/untracked files, host secrets
and user data are excluded. Temporary context is managed by Python TemporaryDirectory; the
initial shell cleanup form was rejected before execution and replaced with scoped cleanup.
The executed replacement command was `docker compose up -d --no-deps --no-build --wait --wait-timeout 60 web`,
separating build from documented Web-only recreation without changing service configuration.
Compose returned success and healthy. Running image:
`sha256:b0022f525ea53f9799ee8c8b104241ef396b1db32f6c79721a570dbdd08f70d4`.
Web started `2026-10-17T15:16:30.847766139Z`. The accepted Web Dockerfile has no compiled
revision/OCI revision field: revision attribution is the exact archived Git source plus captured
build/image identity, not an invented runtime revision endpoint.

HTTP-served assets matched the running container byte-for-byte:

- `index.html`: SHA-256 `8b372c1fc0c64c8677b47e5deb7b331bdc88edc7d5eb4cea01ba5d845296adf7`.
- `index-BCXjikXV.js`: SHA-256 `f98fd469a80fafc50a3b07980e779ccf45ada8ef6aba5c12f435acbc86e76dbb`.
- `index-cD0Tvv2e.css`: SHA-256 `8965968cd56aca61d50112de5c1234b53a3ed8c1826dc55b792a81e0454a873c`.

Postdeployment `/health`, audiobook overview and library GETs returned HTTP 200, with
Audiobookshelf `configuredHealthy`. All 11 non-Web containers retained exact IDs, images and
start times. API and external media infrastructure were not recreated or configured. No volume,
database, playback/progress or acquisition operation. This verifies preserved runtime identity
and read access, not a byte-for-byte audit of media databases.

### Live browser verification and limits

Isolated disposable Firefox loaded actual deployed assets and real GET API responses, without
fixtures. The harness blocked non-GET requests and Finance paths; no acquisition or playback
was triggered. At 390×844 and 1440×844, across obsidian-gold/forest-night/arctic-wind:
Film/Serie/Ljudbok present, Båda absent, audiobook deep link selected Ljudbok, controls at least
44 px, no horizontal overflow, no standalone audiobook widget or native audio element.
Theme variants were applied only in the disposable browser DOM, not server settings.

Submitting Mina bibliotek made actual GETs to existing BigBrain audiobook overview/library
endpoints; both returned HTTP 200. The sample query produced the truthful no-local-match state.
The first harness incorrectly required a nonempty book-result section and timed out; correcting
only that temporary verification assumption allowed the observed empty-state result to pass.
No product change or fabricated book response was used. Film/Serie provider lookup GETs were
verified before recreation; their unchanged API was not redeployed. Candidate/CI tests verify
external audiobook search routing, request cancellation and mode/type transitions. Live external
acquisition, downloads, playback and progress writes were deliberately not exercised.

Manual owner checks remain: iPhone/PWA refresh and cache, keyboard/focus/scroll, search with a
known library title, and Audiobookshelf handoff/playback. Automated Firefox checks do not assert
physical iOS approval or full-catalog search; the existing bounded catalog limitation remains.

### Predeployment evidence and boundaries

Captured IDs, image IDs and start times for all 12 running containers for later comparison.
Web has no persistent volume mount. Existing `/health`, audiobook overview and one-item library
page return HTTP 200; Audiobookshelf reports `configuredHealthy`. Film and Serie lookup GETs
return HTTP 200 with the intended single provider. No acquisition, playback or progress write.
Read-only reachability is distinct from successful title acquisition or playback.

Finance remains FROZEN. No API, Finance, external-media service, database, configuration or
user-data operation is part of this deployment. Physical iPhone/PWA validation remains separate.
This publication continues `review/media-merge-deployment`; no unrelated documentation history
is merged into main. Resolve publication SHA/tree from subject
`docs(media): record verified Web-only deployment`. Earlier stops below remain historical evidence.

Documentation validator (including relative links), whitespace, staged scope and staged Gitleaks
checks PASS. Only this existing deployment report and the Media recovery section are changed.
Main and approved candidate identities are rechecked after publication; no intended changes
remain unpublished. No automatic next implementation or deployment follows.

## Earlier CI stop — historical evidence

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
