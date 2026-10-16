# Historical Qwen integration — 2026-10-16

Detta är en sanerad GitHub-version.

## Metadata

- Baseline: `be7634bd82f52b3cb536142ae3e03dd0e2656aa2`, tree `84b751733b63eb54c926bb380fe59f1686f4230b`.
- Branch: `finance/historical-qwen-research`.
- Publication subject: `review: integrate bounded historical Qwen research`. Resolve its exact SHA/tree
  from Git metadata on this branch; no self-referential hash is embedded in its own document.
- Operational input: checkpoint `7faba26a391c2246ede7b73b8329b66b3cd7d290` on
  `review/finance-recurring-research-deployment`; inspected, not merged or treated as accepted code.
## Status

**MERGE CANDIDATE — implementation ready for independent review.** Implemented and model-free tested;
  **not deployed, not activated, no real Qwen inference or provider call**.
- Finance remains **RESEARCH / 0 SEK / NONE**. Existing AAPL collection/configuration was not touched.

## Changes

Historical admission now composes existing Finance source storage, recurring reservation/authority,
BRF1 local composition, daily features, deterministic robustness evaluator and native result stores.
It adds neither database nor migration, scheduler, generic runner, strategy language or grant-reset path.
The exact [contract and operator sequence](../../../architecture/finance/research-learning-contract.md#historical-qwen-admission--implementation-checkpoint-2026-10-16)
is authoritative; this report records implementation/verification rather than duplicating it.

- Source: NASDAQ-WIKI AAPL, `wiki-5713d7dccfa38f56`,748 sessions2014-01-02–2016-12-19.
  Artifact `sha256:dd5127aae478d270150904fcbad6e96a42e461e13c3d48a1587edb9b89cea43e`.
  Rights/promotion/raw-price capability, exact coverage, numeric validation and genuine acquisition
  timestamps are checked before authority can be exercised. Full source commitment is frozen in grant.
- Evaluation: fixed first500 sessions2014-01-02–2015-12-24, chosen before inference to retain64-call
  reservation and unchanged scientific calculations. Other248 rows are committed, not evaluated/searchable.
  Chronological partition, embargo, momentum20 and sensitivity5/10/20 remain accepted evaluator semantics.
- Historical raw-price simulation is explicitly separate from actual acquisition/knowledge time.
  Adjusted/total-return capability is not fabricated. Corporate-action and survivorship uncertainty,
  and unproven previous historical exposure, are mandatory limitations. No prospective claim.
- Qwen sees development-only price statistics/eight recent samples and permitted hashes. A model proposal
  has the existing closed momentum schema. Holdout prices/results/full-source identity never enter BRF1.
  Source metadata and acquisition time remain available to Finance for auditing after restart.
- Model pin: Qwen3-1.7B-Q4_K_M, existing SHA-256
  `b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897`.
  Hosting checks actual GGUF bytes against the grant before composing historical execution.
  Profile identity binds worker/configuration/evidence as before; no download or larger deadline.
- New maintenance verbs inspect, explicitly enroll, run opportunity1 and read results. They do not start
  an API host, provider, scheduler or automatic prospective research. The maintenance path allows existing
  AAPL collection to remain active while competing research stays paused/disabled.
- Same protected journal is checked before inference and evaluation. A reserve consumes budget even if
  timeout/cancellation/failure follows. Replay returns existing state, never a second inference.
  Source/native-result commitments are verified on reopen; uncertain execution never retries/refunds.
- Stored artifacts: existing `recurring_research_program` snapshot/attempt/proposal/runtime audit,
  `robustness_evaluations` and `backtest_runs`. `finance-historical-result` validates/reopens read-only.
  No successful AI-generated hypothesis is claimed here: test proposals/prices are explicitly fixtures.

### Changed files

Domain/transport:

- `src/BigBrain.Modules/Finance/HistoricalResearchAdmission.cs`
- `src/BigBrain.Modules/Finance/RecurringResearch.cs`
- `src/BigBrain.Modules/Finance/ResearchLearningContracts.cs`
- `src/BigBrain.Modules/Finance/ResearchLearningReply.cs`
- `src/BigBrain.Brain/LocalReasonerProtocol.cs`
- `src/BigBrain.LocalReasoner.Worker/worker.cpp`

Existing owner/hosting/maintenance:

- `src/BigBrain.Api/Finance/FinanceHistoricalResearch.cs`
- `src/BigBrain.Api/Finance/FinanceHistoricalResearchCommand.cs`
- `src/BigBrain.Api/Finance/FinanceRecurringResearch.cs`
- `src/BigBrain.Api/Finance/FinanceRecurringMaintenanceCommand.cs`
- `src/BigBrain.Api/Finance/FinanceResearchScheduler.cs`
- `src/BigBrain.Api/Program.cs`
- `src/BigBrain.Finance.Hosting/LocalRecurringResearchComposition.cs`

Tests: `tests/BigBrain.Api.Tests/FinanceHistoricalResearchTests.cs`.
Documentation: this report, report catalog, research-learning contract, STATUS, BACKLOG and the single
recovery note. Unrelated untracked design work and Sentinel ADR drafts are preserved and excluded.

## Evidence

HIGH risk: shared Finance admission/persistence/projection plus trusted model hosting. Focused tests
first, one broader Release/API run after corrections. No real data acquisition/inference or production
inspection was necessary to test this code. Inputs in new tests are generated fixtures with explicitly
labelled metadata, not claims of a new real dataset or copied production payload.

Commands executed:

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --no-restore \
  --filter 'FullyQualifiedName~FinanceHistoricalResearchTests|FullyQualifiedName~FinanceRecurringResearchTests|FullyQualifiedName~ResearchLearning|FullyQualifiedName~LocalReasoner|FullyQualifiedName~FinanceMarketObservation|FullyQualifiedName~FinanceDaily' \
  --logger 'console;verbosity=normal'
dotnet build BigBrain.slnx -c Release --no-restore -v:minimal
env -u BB132A_LOCAL_ACCEPTANCE -u BB132C_LOCAL_ACCEPTANCE \
  dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-build --no-restore \
  --logger 'console;verbosity=normal' --logger 'trx;LogFileName=historical-api.trx' \
  --results-directory .git/historical-qwen-verification
g++ -std=c++17 -fsyntax-only -I data/bb132a/include src/BigBrain.LocalReasoner.Worker/worker.cpp
node scripts/verify-documentation.mjs
git diff --check
```

- Focused: **231 passed,0 failed,0 skipped**. This precedes the final exact artifact/model pin tightening;
  final code is covered by the broader Release run below.
- Release solution build: **PASS,0 warnings,0 errors**.
- API regression: **1215 passed,0 failed,8 skipped (1223 total)**. All8 skips are explicit real-model
  acceptance cases; their opt-in environment variables were removed. Duration2.73minutes.
- Additional targeted test `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --no-restore
  --filter FullyQualifiedName~FinanceHistoricalResearchTests.HostingRejectsDifferentActualModelBytesBeforeRuntimeConstruction
  --logger "console;verbosity=normal"`: **1 passed,0 failed,0 skipped**. Added after the broad run to
  directly prove actual model-byte mismatch stops composition before runtime construction. Production
  code was unchanged, so the broad suite was not repeated.
- Native worker syntax with retained accepted headers: **PASS**, compilation only, no model loading.
- Formatting: `dotnet format BigBrain.slnx --no-restore --include <13 changed C# paths>` applied;
  final `--verify-no-changes` **PASS**. The exact13 paths are all C# files listed above.
- Whitespace (`git diff --cached --check`), staged Gitleaks (`gitleaks git --staged --redact --no-banner`),
  explicit20-file scope review: **PASS**. Documentation verifier **PASS:268 Markdown files,91 unique
  backlog IDs**, after required report headings/sanitization notice were added.

Development failures were corrected within scope: compiler caught the constant property analyzer and
incorrect test property name; initial source commitment accidentally doubled the `sha256:` prefix,
which13 focused tests rejected before inference. Correcting it produced14/14, then the expanded focused
set231/231. An initial format invocation used nonexistent `BigBrain.sln`; corrected to `.slnx`.
No failed/unexecuted run is represented as passed. No previous production authority was consumed.

New assertions cover complete source/rights/clock corruption refusal, truthful scope/budget/pin refusal,
real-shaped historical data to model-visible numeric input, protected holdout noninterference,
native proposal/result/budget persistence, reopen/replay, concurrent attempts, failure without refund,
missing authority, older-database rollback against independent journal, governor refusal, existing
owned proof-worker transport/audit, explicit enrollment with AAPL gates unaffected, and scheduler
exclusion. Existing recurring/learning/runtime/observation regression evidence is exercised again
because these exact shared components changed; evaluator formulas and journal protocol are reused.
No frontend/Sentinel suite: their code/contracts are unchanged. No migration tests: schema remains99.
No new recovery drill or real-model acceptance tests; those require separate authority.

## Security

No secrets, private paths, raw source payloads or production observations are published. Hashes are
source/model/evidence commitments, not credentials. The code changes no deployment configuration,
protected journal, database schema, grant in production or AAPL collection. Unrelated work is excluded.

## Remaining work

The code is not permission to create a protected authority, touch the journal, activate a profile,
merge or deploy. The user's current request explicitly reserves those for separate approval.
The latest operational input found no existing historical authority, so **no real inference was run**.

After architect review, approve a single concrete operational package:

1. Exact reviewed merge/deployment for this addition, preserving active AAPL and other services.
2. The verified WIKI source commitment and retrospective limitations; fixed500-session experiment,
   protected holdout boundary, one program in the existing Finance store, no singleton replacement.
3. One grant: **1 inference,1 evaluation,3 trials,64 underlying calls,33 reserved seconds**, explicit
   due/expiry times and reasoner-profile identity, no retries/refund/renewal.
4. Existing root-protected append-only authority enrollment, outside Finance/lifecycle recovery sets,
   with its independent custody preflight. No protected journal is created by this implementation.
5. Existing pinned Qwen profile: CPU2,4GiB memory,swap0,pids32,one inference,30s invocation,
   250ms grace,2s reap; immutable provisioned model/runtime artifacts and resource governor must pass.
   No automatic scheduler activation, old180s authorization or new model. Actual successful completion
   within30s has **not** been established; decline/timeout/failure is a consumed, terminal attempt.

This checkpoint permits reading/reusing the saved scientific evidence, not a second model iteration.
Further inference needs separate authority consistent with existing exposure history; a new database
or program identity must never evade the singleton or consumed grant. No training/fine-tuning.
## Resumption

Stop after publication for independent review. No provider call, Qwen, deployment, credentials/journal
change, AAPL runtime change, SHADOW, PAPER/LIVE, broker, orders or real capital occurred.
