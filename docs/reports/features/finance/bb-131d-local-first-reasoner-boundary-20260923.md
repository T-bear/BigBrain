# BB-131D — Local-first reasoner boundary and security contract

Detta är en sanerad GitHub-version. No credentials, private addresses, raw research/provider/model
payloads or host inventory identifiers are published.

## Metadata

- Draft/source analysis: 2026-09-23; final continuation/review: 2026-09-28.
- Baseline: `269fbd7e786a09ba2bf39fe77492d62d8056a52a`.
- Branch: `bb-131d/local-first-reasoner-boundary`.
- Scope: architecture/security design and documentation only.
- [Detailed contract](../../../architecture/finance/local-first-reasoner-boundary.md).
- [ADR 0039 — Accepted](../../../adr/0039-local-first-research-reasoner-boundary.md);
  [ADR 0038 — Accepted, unchanged](../../../adr/0038-finance-research-learning-authority-contract.md).

## Status

**ACCEPTED / MERGED / CI VERIFIED — architecture only**.
Not implemented, deployed, runtime tested or owner UX verified.
Finance **RESEARCH / 0 SEK / NONE**. AI MAY PROPOSE. DATA MUST PROVE. RISK MAY VETO.
A/B/C remain accepted; BB-130 remains closed. BB-131E NOT STARTED / NOT AUTHORIZED.


### Accepted publication — 2026-09-28

Explicit owner approval applies to exact reviewed candidate `8fbf1f497b7dc3b2cdceca7a02b752a8d7ce58e7`.
Pre-merge origin/main, candidate parent and merge-base were `269fbd7e786a09ba2bf39fe77492d62d8056a52a`;
one ahead/zero behind and exactly the reviewed 16 Markdown files, no implementation/config changes.
Merge `aad0e63718571e6a214b4de6d8e16a95b0bbf860` has baseline as first parent and candidate as second.
Candidate and merge share tree `0781add2bdb3756941a6ce5c824f247d713247d9`; exact content is preserved.

[Exact merge CI 36374731050](https://github.com/T-bear/BigBrain/actions/runs/36374731050): **SUCCESS**.
Actual jobs/steps inspected, all successful:

- Backend: checkout/setup, `dotnet restore BigBrain.slnx`,
  `dotnet format BigBrain.slnx --verify-no-changes --no-restore`,
  `dotnet build BigBrain.slnx --configuration Release --no-restore`,
  `dotnet test BigBrain.slnx --configuration Release --no-build`.
- Frontend: checkout/setup, `npm ci`, `npm run format:check`, `npm test -- --run`, `npm run build`.
- Documentation: `node scripts/verify-documentation.mjs`.
- Secrets: `gitleaks/gitleaks-action@v2`.

BB-131D **ACCEPTED / MERGED / CI VERIFIED**; ADR 0039 **Accepted**. The architecture itself is
unchanged by this documentation-only reconciliation. Local-first/no-required-paid-AI is accepted;
reasoner DESIGN ACCEPTED / NOT IMPLEMENTED / NOT INVOKED. Local model NOT SELECTED / NOT INSTALLED /
NOT INVOKED. External AI OPTIONAL FUTURE ONLY / DISABLED / NOT CONFIGURED. No deployment or scientific/
trading authority change. Finance **RESEARCH / 0 SEK / NONE**. BB-131E **NOT STARTED / NOT AUTHORIZED**.
All first-local/first-external implementation, rights/auth/isolation/resource/audit/testing gates remain.

No interrupted D work remains. Historical candidate/recovery observations retain their original scope.
The final documentation-only reconciliation SHA is resolved from main by
`git log -1 --format=%H --grep='^docs: reconcile accepted BB-131D reasoner boundary$' main`.
Its own exact-SHA CI must also pass before handoff; GitHub commit/Actions history and the final handoff
record that SHA/run without embedding a commit's own hash inside itself.

## Recovered work

First interruption left only preflight inspection: main/HEAD equalled baseline, no D branch or
tracked implementation. The 2026-09-23 continuation created the authorized branch, inspected
architecture/code/rights/auth and wrote the 387-line design plus 45-line Proposed ADR 0039.
The recovery note was written before those drafts and had not yet been advanced to their state.
The 2026-09-28 continuation matched their contents to the recorded draft creation, reused both
files and prior source analysis, and updated the single canonical recovery note. No baseline
change, unexplained file or missing implementation was inferred. No earlier final D checks existed.
No D commit or remote branch existed before this publication.

This continuation completed draft review, explicit input/criterion details, this report/DoD matrix,
canonical status/index/security references and final hygiene. It did not recreate the branch or
repeat scientific implementation/tests. Unrelated `deisgnMockups/` and local ADR 0006–0009 remain
excluded. No reset, clean, stash, rebase, overwrite, force push or model operation occurred.

## Evidence

### Source and accepted evidence reused

AGENTS/START-HERE, ARCHITECTURE, ROADMAP, STATUS/BACKLOG/TESTING, Finance module/master roadmap,
[accepted Research Learning contract](../../../architecture/finance/research-learning-contract.md),
ADR 0021/0022/0038, [authentication knowledge](../../../knowledge/authentication.md),
[Finance threat model](../../../security/finance-threat-model.md), report/index conventions and
[A](bb-131a-research-learning-architecture-20260921.md),
[B](bb-131b-synthetic-contract-replay-proof-20260922.md),
[C](bb-131c-persistent-learning-ledger-20260922.md) evidence were inspected.
The detailed contract maps exact source files to each boundary.

Actual source findings: no Brain project in `BigBrain.slnx`; no general application auth middleware/
endpoint enforcement in API Program; existing B closed admission and initial empty-history
projection; C private full frozen snapshot, one protocol and durable invocation/budget/exposure;
Finance use-specific entitlements with Unknown denial. These facts are reused without modifying
source. Human Finance responses and C snapshots are not safe model context. Existing provider or
Sentinel authentication does not establish application-principal authorization.

Repository Markdown hardware search found no corroborating inventory for i5-6600/8 GB RAM/GTX
1050 Ti/4 GB VRAM. The owner-supplied class is a planning assumption, not measured hardware/free
capacity. No model/runtime compatibility or speed claim. Selecting/benchmarking/downloading a model
would exceed D; future gate requirements handle that uncertainty without making cloud mandatory.

OWASP prompt-injection guidance was read on 2026-09-23 and is linked in the contract. No Finance
data was supplied to that public reference lookup, no educational data was ingested, and no external
AI API was invoked. No external product, model, price or legal entitlement was selected/asserted.

### Definition of Done / architecture consistency matrix

| Required answer | Design result / location in detailed contract |
| --- | --- |
| 0-SEK local path and weak models | Product decision: offline local primary, no paid prerequisite, quality is usefulness only |
| Current hardware | Hardware characterization: owner-reported constraints, measurements deferred |
| Brain vs Finance | Ownership map and sequence: Brain orchestrates; Finance projects/adjudicates/persists |
| Provider-neutral port | One request, Proposal or NoUsefulProposal; errors separate, no model-selected tools |
| Local connection/isolation | Fixed authenticated local transport; dedicated runtime, no DB/files/shell/network capabilities |
| Input and holdout | Initial B projection only; development digest, no full scope/results/proxies; future noninterference test |
| Locality/rights | Independent local Allowed and external Allowed/Prohibited/Unknown; unknown never exports |
| Optional cloud | Explicit future adapter/export/credential/terms/cost gates; no automatic fallback |
| Who may invoke | Separate scoped evidence-read, reasoner-request and proposal-submit permissions; no general auth claim |
| Untrusted output | Existing exact v1; bounded bytes/depth/text, duplicate/unknown rejection; no repair/aliases/code |
| Injection | Structured policy/data separation plus zero tools and independent deterministic validation |
| Local resource use | One call, no retries/backlog, enforced memory/CPU/optional-GPU, context/deadline/cancellation |
| Unexpected charges | External disabled by default; explicit mode and conservative cost reservation; no auto-purchase |
| Audit | Durable sanitized intent/outcome and identities; operational, not scientific proof; no raw prompts |
| Failure/disable | Explicit operational outcome table; no refund/holdout leak/cloud retry; deterministic Finance independent |
| First local invocation | Ten required gates; offline runtime, provenance, auth/isolation/audit/limits/admission/security tests |
| First external invocation | Same security properties plus recipient export, provider/owner, credentials/TLS/egress/privacy/cost |
| Threat coverage | Abuse matrix includes requested holdout/SQL/shell/URLs/code, invented references/risk, JSON, crashes and cloud bypass |
| Scientific authority | No new result vocabulary, identity, engine, risk grant, PAPER/LIVE/AUTO/broker/order/capital path |

Consistency review compared this design with ADR 0038, accepted B/C source and the logical Brain/
Sentinel boundaries. No accepted decision changed. ADR 0039 was Proposed at candidate review; it is now Accepted as design only. There is no dedicated
reasoner architecture checker in this repository; manual source/ADR mapping and documentation gates
are the D evidence. Future adversarial tests are explicitly not claimed as executed.

### Final verification

Final verification on 2026-09-28: documentation verifier PASS (253 Markdown files, 91 unique
backlog IDs), including relative links and report/index conventions. Working-tree/staged diff checks
PASS. Gitleaks history PASS (289 commits, no leaks); explicit staged D scan PASS (no leaks).
Manual source/ADR consistency and all DoD matrix rows reviewed; no dedicated D architecture test
exists. Baseline diff inventory contains exactly 16 intended Markdown files, no source/test/package/
CI/schema/runtime/provider changes. Unrelated files are excluded.

No unrelated full backend/frontend suite or model benchmark is required for this
Markdown-only change; no application build/test evidence is substituted for security implementation.

Exact commands:

```sh
node scripts/verify-documentation.mjs
git diff --check
git diff --cached --check
gitleaks git --log-opts=--all --redact --no-banner
gitleaks git --pre-commit --staged --redact --no-banner
```

## Changes

The existing D design and Proposed ADR are retained. Canonical ARCHITECTURE/ROADMAP, status/backlog,
TESTING, Finance module/master/research contract, Finance threat model, ADR/documentation indexes,
report/catalog and the single recovery note receive only the bounded D direction/state/references.
Historical A/B/C acceptance and prior authorization statements are preserved as dated history.
README, other modules, runtime runbooks and unrelated knowledge need no change: no delivered behavior
or operational procedure changed. No production/test/package/schema/CI/runtime/provider/model files.

## Security

The local model remains untrusted. Finance is scientific authority; risk may veto. No key/SDK/model,
networked reasoner, installation, inference, provider activation, export, deployment or execution
capability. General auth, operational audit, runtime isolation and resource enforcement remain future
implementation gates. Hashes are not permissions, anonymity or proof of independent holdout.
Source review is not a penetration test; no reproducible new product defect was established.

## Remaining work

Independent post-merge verification and next-checkpoint planning. Before any real local invocation: separately
authorized implementation, hardware/runtime/model provenance and capacity evidence, approved projection/
use rights, scoped identity/audit, isolation/limits/cancellation/kill behavior and adversarial tests.
Optional cloud additionally requires explicit export/provider/privacy/egress/cost approval. General
adaptive history/cross-cohort accounting, C stale-backup recovery and broader application security
remain limitations. No capability needs to be relaxed because a local model is weak or slow.

## Resumption

The [single recovery state](../../../operations/codex-recovery.md) and branch commit are authoritative.
Resolve exact candidate from `origin/bb-131d/local-first-reasoner-boundary`; parent must equal baseline.
STOP — return accepted BB-131D main to owner/architect for independent post-merge verification
and next-checkpoint planning. Do not install/invoke a model, start BB-131E or deploy. Approval of design alone must
not be interpreted as permission for a later runtime checkpoint.
