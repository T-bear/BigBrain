# BB-130D2 — frontend quality-gate characterization

## Metadata

- Date: 2026-09-14.
- Baseline: `1d824f6d2a9b4e6e2fc679705bd357860042ddbb`.
- Branch: `bb-130d/frontend-quality-characterization`.
- Scope: read-only inventory, ephemeral formatter checks and documentation only.
- Detta är en sanerad GitHub-version. Aggregate evidence only; no credentials, private
  addresses, user data, runtime identifiers or raw sensitive logs.

## Status

**BB-130D2 characterization ACCEPTED / MERGED / CI VERIFIED.**
Publication amendment: owner/architect approved exact candidate
`01129fc54bdd3871b61eeda47e5cadbadc3ad81c`, merged unchanged as
`9181f2c75b8ffc12520ce497b7067e60527f459f`.
[CI 34877864965](https://github.com/T-bear/BigBrain/actions/runs/34877864965) passed backend,
frontend, documentation and secrets on that exact merge SHA. Backend formatter step 5 actually
passed after restore step 4, before build/test steps 6/7. Frontend npm ci, npm test -- --run
and npm run build all passed; no frontend lint/format gate. Original characterization evidence
below remains historical. D2 cleanup/tool installation/gate NOT STARTED / NOT COMPLETE.
No frontend formatter/linter dependency, config, script or CI gate has been committed.
No source formatting, application/UX/scientific behavior or runtime change.
A/B COMPLETE; C COMPLETE / EXIT APPROVED; D IN PROGRESS. D1 and backend cleanup accepted.
Backend formatter gate remains ACCEPTED / MERGED / CI VERIFIED / ENABLED ON MAIN, baseline CLEAN.
Finance RESEARCH / 0 SEK / NONE. No deployment or runtime/device/owner UX approval.

Continuity: the preceding task's remaining final-CI check was completed before D2 started.
[CI 34842836925](https://github.com/T-bear/BigBrain/actions/runs/34842836925) is SUCCESS for
exact baseline SHA above: backend, frontend, documentation and secrets. Actual backend steps
4 restore, 5 check-only dotnet format, 6 Release build and 7 tests all passed in that order.
No merge/reconciliation was repeated. Untracked mockups and ADR 0006–0009 are preserved/excluded.

## Evidence

### Inventory and existing policy

`git ls-files src/BigBrain.Web` is the inventory authority, not node_modules/dist or untracked files.
There are 113 tracked frontend files: 17 `.ts` (including vite.config.ts), 66 `.tsx`, 13 `.css`,
3 `.js`, zero `.mjs`/`.cjs`, 5 `.json`, 5 PNG, one HTML, one webmanifest, Dockerfile and nginx.conf.
The three JS files are ThemeAssets.test.js, UXPolishStyles.test.js and calendar/CalendarLayout.test.js;
there is no tracked service worker in this baseline. This corrects the earlier D1 inspection wording.
Relevant configuration: package.json, package-lock.json, tsconfig.json, tsconfig.app.json,
tsconfig.node.json, vite.config.ts, index.html, public/manifest.webmanifest, Dockerfile, nginx.conf;
repository CI and .gitattributes were also inspected. Canonical roadmap is root ROADMAP.md.

Scripts are exactly `dev: vite`, `build: tsc -b && vite build`, `test: vitest run`.
CI uses Node 20, checkout/setup-node with npm cache, then `npm ci`, `npm test -- --run`,
`npm run build` in src/BigBrain.Web. Backend/doc/secrets jobs remain unchanged.
Lockfile version 3 contains 165 package records; manifest and lock contain no Prettier,
ESLint or Biome. Tracked-name and config searches across the repository found no formatter/
linter/EditorConfig/Stylelint configuration. Dependencies' internal configs are not repository policy.

TypeScript is pinned at 7.0.2. Root config references app and node configs. Both use strict=true,
noEmit=true, skipLibCheck=true, ESNext modules and Bundler resolution. App includes src,
ES2022+DOM libraries, allowJs=false, react-jsx, isolatedModules, consistent filename casing,
JSON imports and interop. Node config includes vite.config.ts, targets ES2023 and enables
verbatimModuleSyntax, moduleDetection=force and allowImportingTsExtensions.
No explicit noUnusedLocals/noUnusedParameters/noUncheckedIndexedAccess/exactOptionalPropertyTypes
is configured. Build covers TS tests under src but not the three JavaScript tests; Vitest runs those.
Adding a standalone typecheck gate would duplicate the existing `tsc -b` build component.

### Whole-scope style measurements

All 83 tracked TS/TSX files were inspected programmatically (7,822 lines), with direct reading
of shell, shared components, Finance, dashboard, tests and CSS to interpret the measurements.
Prettier's bundled TypeScript parser was used read-only for AST observations; these are style
measurements, not accepted policy or semantic-equivalence proofs.

- Parsed ordinary string literals: 5,569 single-quoted, zero double-quoted; all 1,316 JSX string
  attributes use double quotes. Template literals are not included in these counts.
- 292 import declarations, only 3 multiline. Existing imports mix type-only and inline type
  specifiers; no universal sorting/grouping policy. Preserve import order; no sorting plugin.
- 386 bare versus 7 parenthesized simple, untyped single-parameter arrows.
- Only 2 physical lines end with a semicolon. Dense inline statements and type members can
  still contain semicolons; this is not a count of all semicolon tokens.
- No tab-indented lines or CRLF files. Of 5,812 nonblank space-indented lines, 5,783 have even
  indentation and 29 odd indentation. Two-space nesting predominates; compact lines remain common.
- 767 physical lines end in commas. Parsed multiline objects: 87 end with a trailing comma,
  29 do not. Multiline calls include nested callbacks, so their raw counts do not establish a
  universal trailing-comma rule. `all` is a proposed normalization, not a claim of current uniformity.
- 2,095 lines exceed 80 columns, 1,291 exceed 100, 894 exceed 120. JSX often combines an entire
  subtree on one line; props, callbacks and type members vary between compact and expanded forms.
  There is no established hard maximum line length. Print width 120 is a proposed compromise.

### Baseline and candidate validation

Local Node v20.19.2, npm 9.2.0. From src/BigBrain.Web:

| Command | Untouched baseline | After documentation changes |
| --- | --- | --- |
| `npm ci` | exit 0; 116 packages installed | not repeated; lock unchanged |
| `npm test -- --run` | exit 0; 199/199 tests, 26/26 files | exit 0; 199/199 tests, 26/26 files |
| `npm run build` | exit 0; TypeScript and Vite production bundle pass | exit 0; TypeScript and Vite production bundle pass |

Vitest emitted jsdom HTMLMediaElement pause/Window scrollTo not-implemented messages; all tests passed.
No runtime or physical-device verification is claimed. Baseline documentation verifier passed
238 Markdown files / 90 BB IDs; diff check passed. Gitleaks v8.28.0 full history passed:
269 commits, no leaks. Local process permission was needed for npm/test subprocesses and
formatter CLI capture; a sandbox attempt with empty CLI capture was discarded and rerun.

### Candidate tools and scope selection

| Approach | Assessment |
| --- | --- |
| A. Formatter only | Preferred first step: one exact devDependency, one config, explicit TS/TSX scope. Gives repeatable presentation checks beyond compiler/build. |
| B. Formatter plus narrow lint | Not justified in the same checkpoint. Adds parser/plugin/version/rule decisions and potentially semantic corrections before an established format baseline. No lint rules were run or claimed clean. |
| C. Existing compiler/build only | Keep as the enforced frontend baseline while cleanup is reviewed; sufficient to defer tooling, but does not verify presentation consistency required by D. |

Prettier **3.9.6** was selected from the registry and installed only into an isolated temporary
prefix with `npm install --prefix /tmp/bb130d2-formatter --ignore-scripts --no-audit --no-fund --save-exact prettier@3.9.6`.
No repository dependency/lockfile changed. Future installation should use `--save-dev --save-exact`
and commit the lockfile; upgrades require fresh characterization. No plugins or experimental options.
[Prettier installation guidance](https://prettier.io/docs/install) recommends an exact version;
[CLI documentation](https://prettier.io/docs/cli) defines check exit 1 as formatting differences.

Chosen scope: **83 tracked files**, `src/BigBrain.Web/src/**/*.{ts,tsx}` plus
`src/BigBrain.Web/vite.config.ts`, including 23 TS/TSX test files. This follows the existing
TypeScript project boundary. Exclude CSS and the three JS asset/layout tests from this first
scope: those tests assert literal compact CSS strings, so CSS normalization needs its own
characterization and test-contract review. Exclude HTML/JSON/lockfile/webmanifest, runtime
Docker/nginx config, images, generated dist/node_modules, repository scripts/docs and external
themes: different ownership/formatters or generated artifacts. Do not select only already-clean files.

Proposed `.prettierrc.json` inside src/BigBrain.Web (not installed):

```json
{
  "singleQuote": true,
  "semi": false,
  "tabWidth": 2,
  "useTabs": false,
  "trailingComma": "all",
  "arrowParens": "avoid",
  "endOfLine": "lf",
  "jsxSingleQuote": false,
  "embeddedLanguageFormatting": "off",
  "printWidth": 120
}
```

Other defaults remain pinned by version; embedded-language formatting is disabled to bound scope.
Quotes, semicolons, indentation and arrows match prevailing style. Width/trailing commas are explicit
proposals. [Option semantics](https://prettier.io/docs/options) distinguish print width from a hard
line-length limit. No import sorting, hooks rewriting or broad stylistic lint rules.

Exact read-only reproduction from repository root after creating the temporary JSON config above
as `/tmp/bb130d2-prettier-120.json`:

```python
import subprocess
files = subprocess.check_output(
    ['git', 'ls-files', 'src/BigBrain.Web'], text=True).splitlines()
files = [f for f in files if f.endswith(('.ts', '.tsx'))]
assert len(files) == 83
result = subprocess.run([
    'node', '/tmp/bb130d2-formatter/node_modules/prettier/bin/prettier.cjs',
    '--check', '--config', '/tmp/bb130d2-prettier-120.json',
    '--no-editorconfig', '--ignore-path', '/dev/null', *files])
print(result.returncode)
```

The actual invocation used this exact argument vector, with captured output. Two successful CLI
invocations both returned **exit 1**, with byte-identical output listing **80 nonconforming files**,
no parser errors. All 83 files were also compared read-only through `prettier.format(text,
{...config, filepath})` in memory; no formatted content was written to repository files.
Repeated complete file/count records matched. This proves local repeatability for the pinned
version/source/configuration, not every future Node/tool release.

| Scope category | Checked | Would change | Input lines | In-memory formatted lines |
| --- | ---: | ---: | ---: | ---: |
| TS/TSX tests | 23 | 23 | 3,258 | 5,872 |
| Production TSX | 47 | 46 | 3,334 | 9,980 |
| Other TS including setup | 12 | 11 | 1,214 | 2,331 |
| Vite config | 1 | 0 | 16 | 16 |
| Total | 83 | 80 | 7,822 | 18,199 |

By extension, 15/17 TS and 65/66 TSX would change. Only main.tsx, test/setup.ts and
vite.config.ts are already clean. Counts are files/physical lines, not correctness diagnostics
or Git added/deleted line counts. Main change categories are whitespace/spacing, JSX and import
wrapping, multiline callbacks, compact statement/type expansion and punctuation normalization.
This is not a whitespace-only transformation guarantee: parentheses, commas, ASI protection and
JSX text representation may change. Future cleanup must review syntax/literals/JSX semantics and
run existing tests; whitespace-ignored diff alone is insufficient.

Width sensitivity using the same options: 80/100/120 each flag 80 files, producing
22,459/19,717/18,199 lines respectively. An in-memory `trailingComma: es5` comparison at 120
flags 81 files with the same total line count, so it provides no scope reduction. The preferred
120/all policy minimizes measured wrapping among these conventional widths, but remains large:
FinanceObservation.tsx alone expands from 149 to 1,495 lines. This is evidence against one
unreviewed mass-format operation, not permission to hide debt with a huge print width or ignores.

### Final publication checks

After documentation changes: verifier exit 0 (239 Markdown files / 90 unique BB IDs),
`git diff --check` exit 0. `git diff --exit-code -- src .github/workflows/ci.yml` exit 0:
all source/package/CI files remain byte-identical. Final staging is limited to the seven
listed documents; staged diff check and Gitleaks v8.28.0 both exit 0, no leaks. Full-history
Gitleaks baseline result above remains valid because no commits have been added locally.
Prepublication fetch confirms origin/main still equals the exact baseline. Publication will
verify remote candidate equals HEAD and main remains unchanged. No merge or candidate CI claim.

## Changes

Only this report and six canonical documents change: STATUS, BACKLOG, BB-130 stabilization plan,
TESTING, REPORT-CATALOG and codex-recovery. Frontend/backend source, tests, package files,
CI, configs, policies and historical D1/backend reports remain unchanged.
README, ARCHITECTURE/ADRs, root ROADMAP, modules, knowledge/index documents and runbooks were
assessed; no behavioral/architecture/runtime updates needed. Catalog supplies discovery.

## Security

`npm ci` reported 5 dependency audit findings (3 moderate, 2 high); `npm audit --json` returned
exit 1. Installed paths: Vite → PostCSS → nanoid; jsdom → undici; Vitest → @vitest/mocker.
Advisories: [Vitest](https://github.com/advisories/GHSA-82fw-gwwq-j7x9),
[nanoid](https://github.com/advisories/GHSA-2v37-7h3g-55p8),
[PostCSS](https://github.com/advisories/GHSA-fxqj-rqcc-2cmp), and undici
[cache](https://github.com/advisories/GHSA-4cwx-7wf7-3272),
[retry](https://github.com/advisories/GHSA-8xcm-r25x-g524),
[body](https://github.com/advisories/GHSA-m8rv-5g2x-5cg5),
[cache parsing](https://github.com/advisories/GHSA-jr45-8vmc-qm54),
[cookies](https://github.com/advisories/GHSA-v3r7-h72x-cjcm).
These are registry-reported dependency matches, not a reproduced exploitable BigBrain behavior.
No exploit, vulnerable feature activation or production reachability was tested; do not call the
security baseline clean or silently apply audit fix. Separate owner/architect dependency triage
is recommended before cleanup/tool installation. If it reproduces a defect requiring behavior
changes, use BLOCKER HANDOFF — NOT MERGEABLE and a separately authorized correction.

No broker/orders/PAPER/LIVE/AUTO/capital/provider activation, scientific or Finance changes.
RESEARCH / 0 SEK / NONE; deterministic identities, fail-closed and NOT EVALUABLE untouched.
No deployment, runtime data or services touched. Gitleaks and existing safety policies unchanged.

## Remaining work

1. Characterization is accepted; D2 implementation/cleanup/tool installation/gate is NOT STARTED / NOT COMPLETE.
2. Dependency advisories: TRIAGE REQUIRED; exploitability is unverified. Separate applicability review; no upgrades here.
3. If approved, establish formatter baseline through separately authorized bounded cleanup batches
   grouped by responsibility (shared TS, shell/components, feature areas/tests), with exact scope,
   semantic/JSX review and full Web tests/build. No full-scope green gate until every batch is clean.
4. Separately activate the check-only CI gate only after the full approved scope passes.
5. Lint is not recommended now. A later React-hooks correctness characterization may be useful,
   but no rule set is accepted or demonstrated clean. Keep format and semantic corrections separate;
   no duplicate quotes/semicolon/import-style lint or duplicate TypeScript checks.

Proposed future npm scripts, from the Web directory (not added or executed in write mode):

```json
{
  "format:check": "prettier --check --config .prettierrc.json --no-editorconfig \"src/**/*.{ts,tsx}\" vite.config.ts",
  "format:write": "prettier --write --config .prettierrc.json --no-editorconfig \"src/**/*.{ts,tsx}\" vite.config.ts"
}
```

Future scripts cover the same 83 files on a clean checkout; verify glob inventory before adoption.
No custom framework, lint dependency or CI step is needed in this characterization.
[Formatter versus lint responsibilities](https://prettier.io/docs/comparison) support separating
presentation from correctness rules. No cleanup, final gate, D completion, deployment or new
Finance/Research Learning checkpoint is authorized by this recommendation.

## Publication reconciliation

This merge/publication changes only the same seven canonical documents. No local formatter execution,
package/tool installation, source formatting, frontend gate, deployment or behavior changes.
Historical local tests/build and formatter counts remain unchanged; both exact-main CI runs
provide publication regression evidence. Local source suites were not rerun for documentation-only
reconciliation. README, ARCHITECTURE/ADRs, roadmap, modules, knowledge/index and runbooks were
reassessed; no behavioral or architectural updates needed. Catalog retains historical entries.

Publication checks passed: `node scripts/verify-documentation.mjs` exit 0 (239 Markdown files /
90 BB IDs); `git diff --check` and `git diff --cached --check` exit 0; Gitleaks v8.28.0
`git --pre-commit --staged --redact --no-banner` and
`git --log-opts='--all' --redact --no-banner` exit 0, no leaks (270 history commits).
Only the seven intended documents are staged; remote main/candidate remain expected.
Final reconciliation CI must pass all
four jobs, with the backend formatter step explicitly verified again, before completion.

## Resumption

Read AGENTS, START-HERE and canonical recovery. Final source of truth is the separate main
commit `docs: reconcile accepted BB-130D2 characterization` containing this amendment.
Resolve its SHA from Git history and inspect GitHub Actions for that exact head_sha, including
actual formatter execution. Its own future SHA/run cannot be embedded in itself; GitHub history
and exact-head CI retain the definitive publication evidence. Preserve unrelated work and resume
only incomplete publication verification if interrupted. After final CI passes, return to ChatGPT
with "Codex är klar" and stop. No cleanup, triage implementation or gate activation is authorized.
