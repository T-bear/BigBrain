# BB-130D2 — frontend dependency advisory triage

## Metadata

- Baseline/source of truth: `bcf69191b92b9257627cd3c9814c02758b4ae8ae`.
- Branch: `bb-130d/frontend-dependency-triage`.
- Evidence collected 2026-09-14; interrupted run resumed/completed 2026-09-15.
- Scope: installed dependency/advisory reproduction, source tracing, isolated probes and
  production artifact inventory. No dependency corrections or runtime inspection.
- Detta är en sanerad GitHub-version. No secrets, private addresses, user data or sensitive logs.

## Status

**TRIAGED / REVIEW CANDIDATE ONLY.** No currently reachable BigBrain product security defect
requiring production/configuration correction was established. Normal triage handoff applies;
this is not a declaration that the dependencies are safe or FIXED. Patch maintenance is
recommended in a separately authorized checkpoint. Nothing has been upgraded or mitigated here.

D IN PROGRESS; D2 characterization accepted, cleanup/tool installation/gate NOT STARTED /
NOT COMPLETE. Backend formatter gate remains enabled on main. Finance RESEARCH / 0 SEK / NONE.
No deployment, runtime/device/UX approval, scientific or application behavior change.

## Evidence

### Recovery and reliable baseline

The interruption left the existing branch at baseline with no task commit, report or source
change. The old recovery entry described the previous accepted characterization publication;
a triage interruption entry was reconstructed from retained files and session results before
new work. Main remained exact baseline, the remote triage branch did not yet exist, and
unrelated untracked mockups plus ADR 0006–0009 were preserved. No reset/rebase/recreation.
The completed tests/build/audit and eight saved GitHub advisory records were reused.

From src/BigBrain.Web, on 2026-09-14, Node v20.19.2 / npm 9.2.0:

| Command | Result |
| --- | --- |
| `npm ci` | exit 0; 116 packages installed from unchanged lockfile |
| `npm test -- --run` | exit 0; 199/199 tests across 26 files |
| `npm run build` | exit 0; TypeScript check and Vite production build, 70 transformed modules |
| `npm audit --json` | exit 1; 5 package findings: 3 moderate, 2 high |
| `npm ls vitest @vitest/mocker vite postcss nanoid jsdom undici --all` | exit 0; dependency paths below |

This audit snapshot exactly reproduces the accepted D2 historical 5 / 3 moderate / 2 high
result and the same advisory IDs. It is the current checkpoint's dated audit evidence,
reused at resumption, not a claim that the registry can never change. No lockfile changes
were made to reproduce it. There are **eight distinct GHSAs**, not five distinct vulnerabilities:
Vitest and its mocker share one GHSA; undici has five. GitHub API uses severity `medium`
where npm/GitHub UI use `moderate`; nanoid remains reported high despite a displayed CVSS 5.9.
No severity is silently downgraded based on applicability.

### Installed graph, declared dependency class and actual phase

All versions/ranges below come from package-lock.json and installed package metadata.
Production/development labels describe npm metadata, not proof of what is shipped.

| Package/version | Exact primary path from BigBrain.Web | Direct/transitive; lock class | Actual phase |
| --- | --- | --- | --- |
| vitest 4.1.10 | BigBrain.Web → vitest 4.1.10 | direct devDependency | tests/CI; config also imports vitest/config during build/dev |
| @vitest/mocker 4.1.10 | BigBrain.Web → vitest 4.1.10 → @vitest/mocker 4.1.10 | transitive dev | test tooling; public dev-server/browser plugins are not configured |
| postcss 8.5.22 | BigBrain.Web → vite 8.1.5 → postcss 8.5.22 | transitive production-labeled | CSS build/dev tooling, not browser runtime |
| nanoid 3.3.16 | BigBrain.Web → vite 8.1.5 → postcss 8.5.22 → nanoid 3.3.16 | transitive production-labeled | PostCSS internal input IDs |
| undici 7.28.0 | BigBrain.Web → jsdom 29.1.1 → undici 7.28.0 | transitive dev | jsdom test networking support; not browser runtime |

`npm ls` also shows deduped Vite through @vitejs/plugin-react 6.0.4 and Vitest/@vitest/mocker,
and deduped jsdom through Vitest. These point to the same installed Vite/jsdom versions above;
they are not additional vulnerable copies. No affected package is CI-only: developers and
container build stages install the tooling too. The runtime stage does not copy node_modules.

### Advisory-by-advisory decision

Classification uses the requested vocabulary. **C** = reproduced affected dependency/version,
not reachable through the current traced BigBrain use; **D** = dev/test/build-time exposure only.
D describes the package's trust boundary, not proof of an exploitable dev-server path. Neither
classification means FIXED. No A/B/E/F finding was assigned within the inspected repository scope;
unobserved live developer/runtime configurations remain outside the evidence.

| Advisory/source | Package; authoritative severity | Vulnerable range relevant to installed line; first fixed version | Classification and current applicability |
| --- | --- | --- | --- |
| [GHSA-82fw-gwwq-j7x9](https://github.com/advisories/GHSA-82fw-gwwq-j7x9) | vitest + @vitest/mocker; moderate | >=2.1.0 <4.1.11; 4.1.11 | D. Redirect mock requires public mocker/interceptor plugin WebSocket or authenticated browser RPC. App config uses react plugin and jsdom, no browser mode or public mocker plugin. Resolved dev plugins confirm absence. |
| [GHSA-2v37-7h3g-55p8](https://github.com/advisories/GHSA-2v37-7h3g-55p8) | nanoid; high | <3.3.18; 3.3.18 | C. Vulnerable customAlphabet/customRandom with zero size is not the consumer call: PostCSS imports nanoid/non-secure and calls nanoid(6). No application import or attacker-controlled size path found. |
| [GHSA-fxqj-rqcc-2cmp](https://github.com/advisories/GHSA-fxqj-rqcc-2cmp) | postcss; moderate | <=8.5.22; 8.5.23 | D. Reading an outside .map requires attacker CSS, omitted from, and disclosure of resulting map. Vite runPostCSS supplies from: source; its import path supplies from: filename. No direct app PostCSS caller or custom PostCSS config. Input CSS is tracked build input. |
| [GHSA-8xcm-r25x-g524](https://github.com/advisories/GHSA-8xcm-r25x-g524) | undici; moderate | >=7.0.0 <7.29.0; 7.29.0 | C. Requires retry interceptor, malformed partial upstream response and forwarding stale Content-Length downstream. No retry interceptor configured; jsdom is not the production proxy. nginx handles production forwarding. |
| [GHSA-4cwx-7wf7-3272](https://github.com/advisories/GHSA-4cwx-7wf7-3272) | undici; high | >=7.0.0 <7.29.0; 7.29.0 | C. Cache parser crash/disclosure requires cache interceptor; shared disclosure also requires matching cache keys. No cache interceptor or shared response cache configured in app/tests/jsdom defaults. |
| [GHSA-m8rv-5g2x-5cg5](https://github.com/advisories/GHSA-m8rv-5g2x-5cg5) | undici; moderate | >=7.0.0 <7.29.0; 7.29.0 | C. Requires low-level non-fetch HTTP/1 dispatch of duck-typed Blob with untrusted type. No direct app undici use; jsdom XHR serializes bodies to bytes and uses explicit headers. Browser fetch is browser-native; upstream explicitly excludes fetch from this advisory. |
| [GHSA-jr45-8vmc-qm54](https://github.com/advisories/GHSA-jr45-8vmc-qm54) | undici; moderate | >=7.0.0 <7.29.0; 7.29.0 | C. Requires shared cache interceptor with qualified cache directives and authorization-sensitive responses. That interceptor is not configured. |
| [GHSA-v3r7-h72x-cjcm](https://github.com/advisories/GHSA-v3r7-h72x-cjcm) | undici; moderate | >=7.0.0 <7.29.0; 7.29.0 | C. Requires undici setCookie with untrusted domain/unparsed values. jsdom uses tough-cookie CookieJar, not that helper; no repository caller found. |

Other advisory branches, not installed here: nanoid >=4.0.0 <5.1.6 fixed at 5.1.6;
Vitest/mocker >=5.0.0-beta.1 <5.0.0-rc.2 fixed at rc.2. Undici retry/body/cookie advisories
also list <6.28.0 fixed at 6.28.0, and all five list >=8.0.0 <8.9.0 fixed at 8.9.0.
These are first patched releases, not claims that every later release lacks other advisories.

### Source trace and preconditions

- `vite.config.ts`: react plugin, API/health proxy targets, jsdom environment and local setup.
  No host override, browser test mode, mocker/interceptor plugin or environmentOptions.
  `resolveConfig({}, 'serve')` returned default localhost and no mock/interceptor plugin.
  No server was started. Localhost alone would not prove safety; plugin absence is independent
  evidence. Manual CLI host overrides, developer proxies and live listeners were not inspected.
- Installed Vite `dist/node/chunks/node.js`, runPostCSS: explicit `from: source` / `to: source`;
  postcss-import parser uses `from: filename`. PostCSS lib/input.js imports non-secure nanoid
  and uses fixed 6, not customRandom/customAlphabet. No custom PostCSS config is tracked.
- jsdom lib/api.js extractResourcesOptions defaults to no automatic subresource loading,
  empty user interceptors plus decompression. XHR still works: absence of subresource loading
  is not a claim of no network capability. No setGlobalDispatcher/cache/retry customization
  was found in repository source/tests; jsdom's dispatcher uses tough-cookie for cookies.
- jsdom XMLHttpRequest-impl.js extracts request bodies before dispatch and describes processed
  Uint8Array bodies; no repository low-level undici/duck-Blob caller was found. Test fetches
  are commonly stubbed; that observation is supplementary, not the primary reachability proof.
- Installed vulnerable functions remain present in node_modules. Conditional non-use today
  does not protect a future plugin, server or interceptor addition without renewed review.
  Untrusted test/config code can already execute with developer/CI privileges; no claim is
  made that processing arbitrary untrusted repository contributions is safe.

### Bounded isolated reproduction

`node /tmp/bb130triage-probes.cjs` ran on 2026-09-15, exit 0, against existing installed
packages. It used only newly created public synthetic files under a temporary directory,
no sockets, credentials, runtime data or target services. Results:

- PostCSS processing a synthetic outside-map reference without from exposed the synthetic
  marker in result.map; the control with from in a nested input directory did not.
- Unused secure nanoid customAlphabet with default size zero exceeded a one-second child
  timeout and was killed. Actual non-secure nanoid(6) returned length 6.
- Unused undici cache parser threw TypeError for mixed private directives and failed to
  recognize whitespace-qualified private. No shared-cache disclosure was attempted.
- Unused undici setCookie accepted an injected attribute in synthetic example.test metadata.
  No cookie was sent or set on a real application.

These demonstrate dependency-level behavior; they do not turn C/D into a reachable BigBrain
product defect. Vitest WebSocket, undici retry framing and blob HTTP injection were traced
statically against advisory preconditions, not actively exploited. No unsafe negative test was
added to CI. Probe source is preserved below for reproducibility; initial result inspection
needed an undefined-map guard, after which the full probe passed.

### Production artifact boundary

The baseline `npm run build` generated index.html, one JS chunk (424,487 bytes), CSS and copied
public assets. A temporary Vite build with `build.write=false` and a generateBundle observer
recorded all 70 chunk module IDs. External modules were only react, react-dom and scheduler;
**zero postcss/nanoid/undici/vitest/@vitest/mocker modules**. Generated JS/CSS bytes matched
baseline dist exactly. Plain bundle-string search also had zero package-name matches, but
module inventory plus byte comparison, not minified-name search alone, supports the conclusion.

The first helper mixed serve config resolution with build in one process: Vite set NODE_ENV
while resolving serve and the subsequent build used the wrong environment, causing an artifact
filename mismatch. That attempt was rejected. Restoring the original NODE_ENV before the
in-memory build produced the exact production bytes above. No repository config or dist was
changed by that helper; this was instrumentation state, not a production behavior correction.

Dockerfile: Node build stage runs npm ci/build, then independent unprivileged nginx stage copies
only dist and nginx.conf. Compose selects that Dockerfile and exposes nginx; it does not start
Vite/Vitest/Node as the Web runtime. No SSR server or affected Node package is delivered by this
repository-defined frontend artifact. Node's bundled internal undici is a separate component;
this audit evaluates the npm-installed jsdom dependency, not the Node image's entire security state.
No live container/image inspection, deployment attestation or device test was performed.

### Smallest plausible remediation, not implemented

Read-only `npm view <package>@<version> version engines dependencies --json` verified these
published releases. A previous Vitest lookup timed out; its successful resume lookup is used.

| Finding(s) | Smallest plausible correction | Compatibility and required verification |
| --- | --- | --- |
| Vitest/mocker GHSA | Direct devDependency vitest 4.1.10 → 4.1.11; aligned @vitest family updates via its exact dependencies | Patch, same Node 20/22/24 ranges; Vite range still includes 8. Full Web tests/build, mock behavior and exact resolved graph/audit; no arbitrary mocker override. |
| PostCSS GHSA | Resolve postcss 8.5.23 within Vite's ^8.5.17 | Transitive patch; no direct Vite upgrade needed by range. CSS output/asset regression and full Web tests/build; retain explicit from. |
| nanoid GHSA | Resolve nanoid 3.3.18 within PostCSS's ^3.3.16 (also unchanged in postcss 8.5.23) | Transitive patch; no major Nano ID migration. Recheck fixed-size consumer path, tests/build and audit; do not assume PostCSS refresh alone selects fixed nanoid. |
| All five undici GHSAs | Resolve undici 7.29.0 within jsdom's ^7.25.0 | Transitive minor, Node >=20.18.1 compatible with local Node. Full jsdom/Web tests/build, request/cookie/body behavior and graph/audit. Greater networking regression risk than formatting; no jsdom major required by range. |

These are metadata-supported candidates, not tested upgrades. A future checkpoint should change
only the necessary manifest/lock resolutions, verify integrity/graph, run npm ci, full tests,
production build, audit, documentation/diff/secrets checks, and review output changes and exact CI.
Do not mix with formatter cleanup. No npm update, audit fix, replacement install, new override
or package change occurred here. Temporary non-use mitigations are not new policy acceptance:
keep public mocker plugins disabled; retain PostCSS from; do not add untrusted sizes, shared
cache/retry interceptors or unsafe blob/cookie metadata. Review any such usage changes first.

### Publication verification — 2026-09-15

- Reused baseline Web tests/build after explicit unchanged-source comparison; resumption
  authorization avoids rerunning valid suites solely for reassurance. No application, test,
  package, lockfile or workflow bytes changed since those successful commands.
- `git diff bcf69191b92b9257627cd3c9814c02758b4ae8ae --exit-code -- src/BigBrain.Web .github/workflows/ci.yml`:
  exit 0, including package.json/package-lock.json and all frontend production source.
- `node scripts/verify-documentation.mjs`: exit 0, 240 Markdown files / 90 BB IDs.
- `git diff --check` and `git diff --cached --check`: exit 0.
- Gitleaks v8.28.0 `git --log-opts='--all' --redact --no-banner`: exit 0, 271 commits, no leaks.
- Gitleaks v8.28.0 `git --pre-commit --staged --redact --no-banner`: exit 0, no leaks.
- Prepublication fetch: origin/main still exact baseline; only seven intended docs staged.
  Remote branch SHA must match HEAD after push; main remains unchanged. No merge or candidate
  CI success is claimed (existing workflow triggers main pushes and pull requests).

## Changes

Only this report and six canonical documents: STATUS, BACKLOG, BB-130 stabilization plan,
TESTING, REPORT-CATALOG and codex-recovery. Existing characterization and backend reports
remain historical. README, ARCHITECTURE/ADRs, ROADMAP, modules, knowledge/index and operational/
security/rollback runbooks were assessed: no behavior/architecture/deployment changes require
updates. The catalog supplies discovery. No source, package, lockfile, tests or CI change.

## Security

**No mandatory blocker established within current traced BigBrain use.** Six advisories are C;
Vitest/mocker and PostCSS are D with missing exploit preconditions in current configuration.
The library probes reproduce defects only under deliberately introduced synthetic conditions.
A reachable current product defect is not inferred from an affected version alone, nor is absence
of an exploit proof of safety. Findings are TRIAGED / PATCH MAINTENANCE RECOMMENDED, not FIXED.
If later evidence establishes reachable behavior requiring correction, stop under blocker handoff.

Finance RESEARCH / 0 SEK / NONE; no provider/broker/orders/PAPER/LIVE/AUTO/capital work,
scientific behavior, identities/checksums, entitlement/fail-closed or NOT EVALUABLE changes.
No deployment, runtime/device/UX approval, Research Learning or Finance feature work.

## Remaining work

Architect/owner review of exact triage candidate. Separately authorize the minimal dependency
maintenance checkpoint above before frontend formatting work, with fresh advisory applicability
and regression checks on its baseline. No cleanup, tooling installation or frontend gate starts
here; D2 implementation remains NOT STARTED / NOT COMPLETE. Unknown live developer overrides
and future dependency/code changes require new applicability assessment. No advisory is closed.

## Resumption

Read AGENTS, START-HERE, current STATUS/BACKLOG and the sole recovery note. Verify main and
exact remote triage SHA, preserve unrelated work, return to ChatGPT with "Codex är klar" and
stop. This branch is REVIEW CANDIDATE ONLY; main remains accepted source. No merge authorized.

### Reproducible isolated probes

From src/BigBrain.Web, save this only as a temporary `.cjs` file and run with Node.
The timeout is intentional; this does not exercise the application path.

```javascript
const fs=require('node:fs'); const path=require('node:path'); const {spawnSync}=require('node:child_process');
const root=process.cwd(); const modules=path.join(root,'node_modules');
(async()=>{
 const dir=fs.mkdtempSync('/tmp/bb130triage-fixture-');fs.mkdirSync(path.join(dir,'input'));
 const map=path.join(dir,'outside.map');const marker='PUBLIC_SYNTHETIC_TRIAGE_MARKER';
 fs.writeFileSync(map,JSON.stringify({version:3,sources:['synthetic.ts'],sourcesContent:[marker],names:[],mappings:''}));
 const postcss=require(path.join(modules,'postcss')); const css='a{color:red}\n/*# sourceMappingURL='+map+' */';
 const noFrom=await postcss([]).process(css,{map:true});const withFrom=await postcss([]).process(css,{from:path.join(dir,'input','style.css'),map:true});
 console.log('postcss synthetic outside map: without from='+String(JSON.stringify(noFrom.map)).includes(marker)+'; with from='+String(JSON.stringify(withFrom.map)).includes(marker));
 const nanoPath=path.join(modules,'nanoid');const r=spawnSync(process.execPath,['-e','require(process.argv[1]).customAlphabet("ab",0)()',nanoPath],{timeout:1000,killSignal:'SIGKILL'});
 console.log('nanoid unused secure customAlphabet zero times out='+(r.error?.code==='ETIMEDOUT'));console.log('actual non-secure nanoid(6) length='+require(path.join(nanoPath,'non-secure')).nanoid(6).length);
 const {parseCacheControlHeader:parse}=require(path.join(modules,'undici/lib/util/cache.js'));
 let crash=false;try{parse('private, private="header"')}catch(e){crash=e instanceof TypeError}
 console.log('unused undici cache parser mixed-private TypeError='+crash+'; whitespace-private ignored='+!Object.hasOwn(parse('private ="authorization"'),'private'));
 const {Headers,setCookie}=require(path.join(modules,'undici'));const h=new Headers();setCookie(h,{name:'fixture',value:'public',domain:'example.test; SameSite=None'});
 console.log('unused undici cookie helper accepts injected attribute='+h.get('set-cookie').includes('SameSite=None'));
})().catch(e=>{console.error(e.message);process.exitCode=1});
```

### Reproducible artifact inventory

From src/BigBrain.Web, run this temporary ES module. Resolve serve configuration in a separate
process if checking dev plugins, so its NODE_ENV side effect cannot affect this production build.
No new package, config file or generated artifact is written.

```javascript
import fs from 'node:fs'
import { pathToFileURL } from 'node:url'
const { build } = await import(pathToFileURL(`${process.cwd()}/node_modules/vite/dist/node/index.js`))
const ids = new Set()
const result = await build({
  logLevel: 'silent', build: { write: false },
  plugins: [{ name: 'inventory-only', generateBundle(options, bundle) {
    for (const item of Object.values(bundle))
      if (item.type === 'chunk') Object.keys(item.modules).forEach(id => ids.add(id))
  } }],
})
const affected = [...ids].filter(id => /node_modules\/(postcss|nanoid|undici|vitest|@vitest\/mocker)\//.test(id))
const identical = result.output.filter(x => x.type === 'chunk' || x.fileName.endsWith('.css'))
  .every(x => Buffer.from(x.type === 'chunk' ? x.code : x.source).equals(fs.readFileSync(`dist/${x.fileName}`)))
console.log({ modules: ids.size, affected, identical })
```
