# Synthetic journal mapping — review preparation

Local implementation for the six owner-approved DoD items, risk level 3. Independent
review is pending. No runtime registration, API dependency, SQLite access, provider,
model, trading, persistence or policy changes are introduced by this bridge.
The existing offline ledger auditor is not called by it.

`JournalForwardBridge.EvaluateSynthetic` accepts in-memory synthetic mirrors of
`FinanceShadowPrediction` and `FinanceShadowOutcome` in
`src/BigBrain.Api/Finance/FinanceShadowResearch.cs`. A source contract test pins every
constructor field, its order and type; the state enum is represented by its exact
name as a string. No production record is read. The existing `ForwardEvaluation`
validates the supplied manifest and boundary. The supplied manifest is not evidence
that the journal committed to a cohort before predictions were made.

| Journal field | Partial forward field |
| --- | --- |
| prediction `PredictionId`; outcome `OutcomeId`, `PredictionId` | Retained identities; join only on exact `PredictionId` |
| prediction `StrategyId`, `StrategyVersion`, `ParameterFingerprint` | `Strategy`, `StrategyVersion`, `ParametersHash`, unchanged |
| prediction `InstrumentId`, `Horizon`, `SessionDate` | `Instrument`, `Horizon`, `SourceSession` |
| prediction `SourceRevisionId`, `ObservationKnowledgeUtc` | `SourceRevision`, `SourceKnownAt` (claimed journal knowledge instant) |
| prediction `FeatureRevisionId`, `KnowledgeCutoffUtc`, `CreatedAtUtc` | `FeatureRevision`, `Cutoff`, `CreatedAt` |
| prediction `Signal` | `TargetLong` → true, `TargetFlat` → false; other values → null and diagnostic |
| outcome `TargetSession`, `SourceRevisionId`, `KnowledgeUtc`, `EvaluatedAtUtc` | `TargetSession`, `TargetRevision`, `TargetKnownAt`, `EvaluatedAt` |
| outcome `ReferenceClose`, `RealizedClose` | Same names and values |
| outcome `RealizedReturn`, `DirectionResult` | Retained and checked for consistency; never treated as equity |
| prediction `State`, `OperatingMode` | Require `Evaluated`, `RESEARCH`; invalidation is never overridden |
| prediction `Symbol`, `Provider`, `ReasonCodes` | Retained only; provider is not a verified source identity |

The journal read models cannot supply source/target checksums, acquisition instants
or verified event instants; feature checksum, complete input revisions and their
checksums/times, feature knowledge time, definition hash or engine version; a verified
source identity; an independently bound outcome version key; or a frozen journal
manifest/cohort commitment. Calendar boundaries are used only for chronology checks,
never as fabricated evidence events. Parsed timestamps cannot attest that the stored
strings had explicit zones. Retrospective status, revision ambiguity and complete
population provenance also cannot be established by these read models alone.

Probability, verified regime/knowledge time and net unitized equity/costs are absent.
Direction and close-to-close return cannot substitute for those fields. All metrics
remain `UNKNOWN`; every bridge result is `SYNTHETIC_ONLY / NOT_EVALUABLE`, with zero
genuine future sessions, no groups and no claimed evidence hash. No `ForwardEvidence`
is constructed. This is a useful partial mapping and diagnostic boundary, not an
admission path. No enrichment or relaxation is included.

Review should check ADR 0029 causality, exact source-model mapping, unchanged version
values, duplicate joins, invalidation, missing outcomes and full declared panel
coverage. Synthetic integration tests project the existing 120-session evaluator
fixture into the actual journal shape, prove the lost evidence stays missing, and
exercise chronology, version, duplicate and incomplete-cohort defects. The positive
evaluator tests remain separate and do not establish journal evaluability.

Verification commands:

```sh
dotnet format tests/BigBrain.Finance.OutcomeAudit.Tests/BigBrain.Finance.OutcomeAudit.Tests.csproj --no-restore
dotnet test tests/BigBrain.Finance.OutcomeAudit.Tests/BigBrain.Finance.OutcomeAudit.Tests.csproj --no-restore -p:UseSharedCompilation=false -m:1
```

Local verification, 2026-11-27: test suite **PASS**, 132 passed, zero failed or
skipped. Project-based `dotnet format` was **BLOCKED** by denied named-pipe access;
`dotnet format whitespace --folder --include` on the three affected C# files
**PASS**. Full semantic formatting remains unverified. Remote Git verification was
**BLOCKED** by local SSH configuration permissions. Local HEAD remains `6951df2`;
the bridge and tests are uncommitted. No independent review, publication or deployment
has been performed.
