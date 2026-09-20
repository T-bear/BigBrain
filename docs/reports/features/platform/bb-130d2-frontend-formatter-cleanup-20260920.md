# BB-130D2 — frontend formatter cleanup

## Metadata

- Date: 2026-09-20. Filename uses the actual work date; the handoff brief anticipated a
  `20260917` suffix before the interruption.
- Baseline / checkpoint source: `5cb179d2558fb9bddc8f256d0fcc06251826a1f5`, verified equal to
  `origin/main` by `git ls-remote` at start and unchanged throughout.
- Branch: `bb-130d/frontend-formatter-cleanup` (approved candidate subsequently merged; publication below).
- Agent handoff: **Codex → Claude**. The Codex session was interrupted by an exhausted weekly
  usage limit. Repository and Git evidence, not the previous transcript, established the state.
- Contract: [accepted characterization](bb-130d2-frontend-quality-characterization-20260914.md)
  and [accepted tooling](bb-130d2-frontend-formatter-tooling-20260917.md).
- Detta är en sanerad GitHub-version. Aggregate evidence only; no credentials, private
  addresses, user data, runtime identifiers or raw sensitive logs.

## Status

**ACCEPTED / MERGED / CI VERIFIED.**

Accepted and merged; NOT deployed. No frontend formatter CI gate was added and none is
authorized. Finance remains `RESEARCH / 0 SEK / NONE`.

### Accepted publication — 2026-09-21

Owner/architect approved candidate `b547219473c7ef12cec81b6778bd454a2ce33058`,
exactly one commit above baseline `5cb179d2558fb9bddc8f256d0fcc06251826a1f5`.
Codex merged it unchanged as `8e966404b2b7055acf21ccedabaa883ecaf225ca`; the merge
file tree equals the approved candidate. Claude implemented the cleanup; Codex performed
only this approved merge/publication, without formatter write mode.
[Merge CI 35540892280](https://github.com/T-bear/BigBrain/actions/runs/35540892280):
SUCCESS for backend, frontend, documentation and secrets. Backend restore → actual
`dotnet format BigBrain.slnx --verify-no-changes --no-restore` → Release build → tests
all passed. Frontend ran only `npm ci`, `npm test -- --run`, `npm run build`.

Fresh post-merge local commands: `npm ci`, `npm run format:check`, `npm test -- --run`,
`npm run build`, `npm audit --json` all exit 0: 83/83 conforming, 199/199 tests in 26 files,
production build PASS (70 modules), zero audit findings. Prettier remains exactly 3.9.6,
dev-only. Scope comparison confirms exactly the accepted 80-file debt set; the three
already-clean files, package/lock, formatter config/scripts, CI, backend, CSS, excluded JS
and runtime/deployment configuration are unchanged. The focused Finance 46/46 and semantic/
artifact comparisons above remain evidence from Claude's reviewed candidate, not new runs.
The owner-approved ThemeControl.test.tsx historical two-pass convergence remains documented;
the committed source is already conforming and no write-mode operation was run for publication.

Cleanup is ACCEPTED / MERGED / CI VERIFIED. A/B COMPLETE; C COMPLETE / EXIT APPROVED;
D IN PROGRESS. Frontend formatter CI gate NOT STARTED / NOT ENABLED; lint deferred.
Finance RESEARCH / 0 SEK / NONE. No deployment, runtime, device, UX or scientific behavior
change. Next proposed checkpoint: **BB-130D2 — Frontend Formatter CI Gate**, separately
authorized; neither it nor the agent-neutral workflow/recovery improvement starts here.

### Discovered deviation and the owner decision that resolves it

The accepted post-format contract assumed `format:check` would exit 0 after a single
`npm run format` and that a second write run would produce no further diff. **That did not
hold.** One file of 83, `src/ThemeControl.test.tsx`, is not a Prettier fixed point after one
pass. A second pass rewrites it and reaches a stable fixed point; a third pass is a no-op.

Work was halted at that point under the checkpoint's stop rule and the owner decided.

**Owner decision, 2026-09-20 — option (a) approved:** accept the converged two-pass formatter
result as the cleanup candidate baseline, with the one-pass fixed-point deviation explicitly
documented and preserved rather than hidden or normalized away.

**Accepted architectural interpretation, recorded verbatim in effect:**

- This does **not** establish "run Prettier twice" as the normal formatter workflow.
- The historical pre-cleanup source contained one file, `src/ThemeControl.test.tsx`, for which
  Prettier 3.9.6 required two write passes to reach its stable fixed point.
- After the second pass the complete 83-file formatter scope is conforming.
- The third write pass was a no-op.
- The resulting source is therefore currently at the stable Prettier 3.9.6 fixed point.
- Future formatter CI must check that **committed source is already conforming**.
- Future CI must **not** depend on running write-mode formatting twice.

The deviation is a formatter presentation-level fixed-point defect, not a correctness,
security or scientific defect, so the blocker-handoff path does not apply.

## Evidence

### Recovered starting state

`git fetch origin`; HEAD, the local branch and `origin/main` all equalled
`5cb179d2558fb9bddc8f256d0fcc06251826a1f5`. `git ls-remote` confirmed no remote
`bb-130d/frontend-formatter-cleanup` existed. `git status` showed zero tracked and zero
staged modifications, no stash entries, and only the known unrelated untracked material
(local design mockups and unpublished ADR 0006–0009). `git diff` against the baseline was
empty, so **formatter write mode had not been executed before the interruption**; this was
established from Git evidence, not from the handoff summary. The reflog shows the branch
checkout as the last recorded operation. Unrelated untracked material was preserved and
excluded throughout; nothing was reset, cleaned or force-checked-out.

### Environment and formatter contract, independently reproduced

Node v20.19.2, npm 9.2.0. Installed Prettier resolves to exactly **3.9.6** both through the
package manifest and through `prettier --version`. `.prettierrc.json`, the `format` and
`format:check` scripts, `package.json`, `package-lock.json` and all TypeScript/Vite
configuration were byte-identical to the accepted tooling checkpoint and were not modified.

Scope proof, reproduced from the repository root: the Git inventory of tracked `.ts`/`.tsx`
files under the Web project is **83** files (17 `.ts`, 66 `.tsx`), and the filesystem
expansion of `src/**/*.ts`, `src/**/*.tsx` plus `vite.config.ts` equals that set exactly,
with no extra, missing or untracked file.

### Baseline, reproduced rather than reused

| Command | Result |
| --- | --- |
| `npm ci` | exit 0 |
| `npm test -- --run` | exit 0, **199/199** tests, 26 files |
| `npm run build` | exit 0, **70 modules transformed** |
| `npm audit --json` | exit 0, **zero** findings in every severity |
| `npm run format:check` | exit 1, **80 nonconforming of 83 selected** |

The three already-conforming files are exactly `src/main.tsx`, `src/test/setup.ts` and
`vite.config.ts`, matching the accepted contract. Category breakdown also reproduced the
accepted characterization exactly: TS/TSX tests 23 checked / 23 changing, production TSX
47 / 46, other TS including setup 12 / 11, Vite config 1 / 0.

Deterministic production artifact inventory was captured before formatting: SHA-256 for all
nine emitted files, including `index.html`, the CSS bundle, the JS bundle and static icons.

### Formatter write mode

`npm run format` exit 0. Prettier processed 83 files, rewrote **80** and reported the
expected 3 unchanged. `git diff --name-only` returned exactly **80** files, 15 `.ts` and
65 `.tsx`, and that set is **identical** to the characterized nonconforming set — no
unexpected file, no missing file. Nothing outside `src/BigBrain.Web/src/**/*.{ts,tsx}`
changed. Totals: 80 files changed, 12,873 insertions, 2,498 deletions.

### Post-format contract — DISCOVERED DEVIATION (owner-accepted, see Status)

| Step | Required | Observed |
| --- | --- | --- |
| `format:check` after one write | exit 0, 83/83 conform | **exit 1**, `src/ThemeControl.test.tsx` nonconforming |
| second `npm run format` | no additional diff | **one file rewritten again** |
| `format:check` after second write | — | exit 0, 83/83 conform |
| third `npm run format` | — | no-op, 83/83 unchanged; diff byte-identical to the second |

A read-only in-memory sweep of all 83 files, formatting each baseline file once, twice and
three times, isolates the instability precisely: **82 of 83 files reach a fixed point on the
first pass; exactly one does not.** `src/ThemeControl.test.tsx` produces 101 lines on pass 1
and 99 lines on pass 2, and pass 3 equals pass 2.

The pass-1 → pass-2 difference is confined to one member chain inside a test setup block:
pass 1 breaks `vi.fn().mockResolvedValue(...)` across lines, pass 2 collapses it back once
the re-parsed input changes the chain heuristic's input. No token is added or removed and no
argument changes. This is a formatter fixed-point defect in presentation only, not a
correctness, security or scientific defect, so the blocker-handoff path does not apply; it
is a deviation from the accepted checkpoint contract and requires an owner/architect decision.

### Semantic, token, AST, literal and JSX verification

TypeScript 7.0.2 in this repository is the native port and exposes no JavaScript compiler
API, so `createSourceFile`/`createScanner` are unavailable; this is the most likely cause of
the parser-tooling failure reported before the interruption. Verification therefore uses
**Prettier 3.9.6's own bundled TypeScript parser** through `__debug.parse`, which guarantees
the comparison parser is the same one that produced the output. All 83 files parsed with
zero parse errors before and after. Method, applied per file:

1. **Position-independent normalized AST.** The massaged AST with `range`, `loc`, `start`,
   `end` and the internal `__contentEnd` offset removed, and with literal `raw`
   representation replaced by the cooked `value`. Template literal raw text is deliberately
   retained because it is semantic.
2. **Independent ordered feature extractions** compared as exact sequences: identifiers,
   JSX identifiers, imports, exports, numeric literals, BigInt literals, regex pattern and
   flags, template quasis (raw and cooked), JSX element and component names, JSX props and
   values, operators, object property key/computed/shorthand/kind, member-access paths and
   the executable statement sequence.
3. **React JSX children rendering model**, which applies React's whitespace rules to JSX
   text, treats a `{' '}` expression container as a literal space, merges adjacent text
   children and compares the resulting rendered child sequence.
4. **Comment extraction** with indentation normalized and text preserved.

Result across all 80 reformatted files: **every semantic invariant is identical** —
identifiers, JSX identifiers, imports, exports, numbers, BigInts, regexes, templates, JSX
element names, JSX props, rendered JSX children, operators, object keys, executable
statements, member paths and comments.

Differences exist only at representation level, and each class was enumerated and explained:

- **JSX text re-wrapping** in 48 files. Raw and value text differ, collapsed text is
  identical, and the rendering model is identical.
- **`{' '}` separators**, 28 inserted across 7 files. The only string-literal delta anywhere
  in the scope is the added single space; no string content was altered or removed in any
  file.
- **One `EmptyStatement`** in `src/calendar/Calendar.tsx`. With `semi: false` Prettier moves
  the terminating semicolon of `(result[event.date] ??= []).push(event)` to the start of the
  line as an ASI guard, which re-parses as a no-op empty statement. The executable statement
  sequence is unchanged, which is why the dedicated executable-statement invariant holds.
- **Trailing commas, statement-separator semicolons and clarifying parentheses** in
  non-JSX TypeScript. None of these appear in the parsed AST; the affected Finance files
  have byte-identical normalized ASTs.

Limitations, stated explicitly: this proves structural and literal equivalence under one
pinned parser and version, for these exact files. It is not a type-level proof, not a proof
for future Prettier or Node releases, and it does not by itself establish runtime behavior —
the production artifact comparison and the test suite cover that. Parenthesization and
trailing commas are invisible to this ESTree AST by construction, so they are verified
through the source-level diff review and the build comparison instead.

### Production artifact comparison

Rebuilt after formatting: exit 0, 70 modules transformed, unchanged module count.

- CSS bundle: **byte-identical**, same content hash.
- Icons and web manifest: **byte-identical**.
- `index.html`: differs only in the content-hashed JS asset filename.
- JS bundle: 424,487 → 424,568 bytes, **+81 bytes**.

The JS difference was investigated rather than dismissed. Two global, chunk-independent
checks establish its nature:

1. Extracting every string/template literal from both bundles and concatenating them in
   order yields **82,558 bytes in both**, byte-identical. No shipped text changed anywhere.
2. Replacing every literal with a placeholder and then collapsing runs of adjacent literal
   arguments makes the two code skeletons **byte-identical at 332,891 bytes each**.

A hunk-level review of all 21 differing regions independently confirms the same thing: every
difference splits one JSX text child into two adjacent text children whose concatenation is
the identical string, for example `" · "` becoming `" "` plus `"· "`. React renders adjacent
text children contiguously, so rendered output is unchanged; the element tree simply carries
one extra adjacent text child per split.

### Regression verification after formatting

| Command | Result |
| --- | --- |
| `npm test -- --run` | exit 0, **199/199** tests, 26 files |
| `npm run build` | exit 0, 70 modules transformed |
| `npm audit --json` | exit 0, zero findings |
| `npm run format:check` | exit 0, 83/83 conform (at the converged fixed point) |

### Largest and highest-risk diffs reviewed

| File | Lines before → after | Added / deleted |
| --- | --- | ---: |
| `src/finance/FinanceObservation.tsx` | 150 → 1,496 | 1,446 / 100 |
| `src/finance/FinanceObservation.test.tsx` | 728 → 1,553 | 996 / 171 |
| `src/audiobooks/Audiobooks.tsx` | — | 816 / 61 |
| `src/types.ts` | — | 810 / 60 |
| `src/audiobooks/Audiobooks.test.tsx` | — | 791 / 203 |
| `src/meal-planner/MealPlanner.tsx` | — | 746 / 162 |
| `src/shopping-list/ShoppingList.tsx` | — | 482 / 28 |
| `src/dashboard/appWidgets.tsx` | — | 447 / 48 |

The `FinanceObservation.tsx` expansion matches the accepted characterization's prediction of
roughly 149 → 1,495 lines, confirming the measurement was reproduced rather than assumed.

**Explicit `FinanceObservation.tsx` review.** Its normalized AST has exactly 45 differences,
in three classes only: 15 JSX children-array length changes, and 15 JSX text raw plus 15 JSX
text value re-wraps, every one of which has identical collapsed text. The single
string-literal delta is the 17 inserted `{' '}` separators. Template literals are identical.
The rendered JSX children sequence is identical. Occurrence counts of the safety and
scientific copy are unchanged, including `RESEARCH`, `0 SEK`, `NOT EVALUABLE`,
`INSUFFICIENT`, `Syntetisk`, `stale`, `entitlement`, `broker` and `order`. An earlier
line-based count appeared to differ only because the file grew from 150 to 1,496 lines;
occurrence counts are identical. `FinanceObservation.test.tsx` has exactly one AST
difference, a children-array length change.

### Finance safety and scientific invariants

No backend source changed at all, so Finance deterministic identities, checksums, lineage,
dataset and revision identity, campaign, holdout/OOS and robustness semantics, cost
assumptions, entitlement and fail-closed behaviour and `NOT EVALUABLE` handling cannot have
been affected by this checkpoint. The changed surface is presentation-only frontend code.

Six Finance frontend files are in scope. Four of them —
`financeSnapshotCache.ts`, `financeSnapshotCache.test.ts`, `useFinanceBacktestDetails.ts`
and `useFinanceObservation.ts` — have **byte-identical normalized ASTs** before and after,
so the BB-128B/C cache version, projection, stale/degraded, single-loader and retry
semantics and the BB-130C selected-result identity logic are structurally untouched. The two
`FinanceObservation` files differ only as described above with identical rendered output.
Occurrence counts of every safety and scientific term are identical in all six files.
Focused Finance suites pass **46/46**. Finance remains `RESEARCH / 0 SEK / NONE`.

### Scope verification

Verified unchanged: `package.json`, `package-lock.json`, `.prettierrc.json`, all three
`tsconfig` files, `vite.config.ts`, the Web `Dockerfile` and `nginx.conf`,
`.github/workflows/ci.yml`, `compose.yaml`, `Directory.Build.props`, `global.json`, all CSS,
all `.js` including the three excluded asset/layout tests, all JSON, HTML and the web
manifest, all backend source, backend tests and tools, and everything under `deploy`,
`infrastructure` and `scripts`.

## Changes

Historical implementation inventory (now committed and merged): exactly 80 tracked TypeScript/TSX source files under
`src/BigBrain.Web/src` were rewritten by the accepted formatter command. No application source
was edited by hand, no expression simplified, no logic reordered, no variable renamed, no
import manually reorganized, no component extracted, no unrelated warning or test touched and
no dependency maintenance performed. Documentation added or changed: this report, the
canonical recovery note and the report catalog entry.

Documentation changed with the candidate, following the established candidate-commit
convention: this report, `docs/STATUS.md`, `docs/BACKLOG.md`,
`docs/architecture/bb-130-stabilization.md`, `TESTING.md`,
`docs/operations/codex-recovery.md` and `docs/reports/REPORT-CATALOG.md`. Every entry states
REVIEW CANDIDATE ONLY and records the deviation and the owner decision.

`README.md`, `ARCHITECTURE.md`, ADRs, `ROADMAP.md`, module contracts, the knowledge index and
runbooks were assessed and need no change: no architecture, contract, runtime or behaviour
changed.

## Security

`npm audit --json` returned zero findings before and after. That is dated registry evidence
for this dependency graph, not a blanket security approval, and no advisory was fixed,
introduced or re-triaged here. No dependency, version, override or lockfile entry changed.

No secrets, credentials, tokens, private addresses, user data, runtime identifiers or raw
logs were read into, written to or published by this checkpoint. No deployment, runtime,
device or owner UX verification occurred and none is claimed. No provider, broker, order,
PAPER, LIVE, AUTO or capital surface exists or was touched. No Research Learning or Finance
feature work was performed. The Sentinel boundary was not touched. Finance remains
`RESEARCH / 0 SEK / NONE`.

## Remaining work

The cleanup is accepted and merged. Next is **BB-130D2 — Frontend Formatter CI Gate**,
requiring separate authorization. It must check committed conformance and must not depend
on two formatter write passes. Lint, CSS and excluded JS tests remain outside this scope.
No deployment or agent-workflow redesign is authorized.

### Publication checks — 2026-09-20

Re-verified immediately before publication, after the owner decision and after all
documentation edits: `npm run format:check` exit 0 with 83/83 conforming; a further
`npm run format` reported 83/83 unchanged and produced a byte-identical diff; a repeated
`format:check` exit 0; tests 199/199 in 26 files; focused Finance 46/46; production build
exit 0 with 70 modules; `npm audit --json` exit 0 with zero findings. A fresh parse of all 83
files matched the verified semantic snapshot byte for byte, so the semantic, literal and JSX
evidence above applies to exactly the committed tree.

Scope re-verified unchanged: `package.json`, `package-lock.json`, `.prettierrc.json`,
`.github/workflows/ci.yml`, `compose.yaml`, all CSS, JS, JSON, HTML and the web manifest, all
backend source, backend tests, tools, deploy, infrastructure and scripts. Source changes
remain 80 TS/TSX files inside the Web source scope.

Repository gates: `node scripts/verify-documentation.mjs` exit 0; `git diff --check` and
`git diff --cached --check` exit 0; `docker compose config --quiet` exit 0; Gitleaks v8.28.0
full history exit 0 with no leaks, and a scan restricted to this checkpoint's files exit 0
with no leaks. A pre-existing Gitleaks directory-mode finding in
`docs/security/finance-threat-model.md` is already allowlisted in `.gitleaksignore`, passes
the authoritative full-history scan and is untouched by this checkpoint; no action was taken.

Prepublication fetch confirmed `origin/main` still equals the checkpoint baseline
`5cb179d2558fb9bddc8f256d0fcc06251826a1f5` and that no remote candidate branch existed.
Publication pushes only `bb-130d/frontend-formatter-cleanup` as REVIEW CANDIDATE ONLY. No
merge, no force operation, no push to main, no CI gate change and no deployment. The remote
branch SHA is verified to equal the local candidate SHA after the push.

Historical pre-merge rollback was to leave the candidate unmerged. After acceptance, any
rollback requires a separately approved normal revert and verification; never reset or
rewrite published history. No runtime rollback is needed because nothing was deployed.

## Resumption

Read `AGENTS.md`, [Start here](../../../START-HERE.md) and the
[canonical recovery note](../../../operations/codex-recovery.md) first. The recovery note is
authoritative for the exact working-tree state, what was verified and the next safe action.

Repository and Git state take precedence over any agent summary. Preserve the unrelated
untracked design mockups and unpublished ADR 0006–0009. Do not re-run the formatter expecting a different
result; the tree is already at the stable fixed point. Do not merge, deploy, enable a
frontend formatter CI gate or start another checkpoint without explicit owner authorization.
This exact candidate has already been approved and merged; do not merge it again.
The current publication evidence above supersedes historical pre-merge instructions.
