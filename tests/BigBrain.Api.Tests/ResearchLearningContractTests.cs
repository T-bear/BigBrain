using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using static BigBrain.Api.Tests.ResearchLearningFixture;

namespace BigBrain.Api.Tests;

public sealed class ResearchLearningContractTests
{
    private static readonly decimal[] ExpectedPeriods = [5m, 10m, 20m];
    [Fact]
    public void FalsificationUsesFractionalValidationReturnAndPreservesMissingEvidence()
    {
        var criterion = LearningAdmissionPolicy.Criterion;
        Assert.True(criterion.IsFalsified(0m));
        Assert.True(criterion.IsFalsified(-.001m));
        Assert.False(criterion.IsFalsified(.001m));
        Assert.Null(criterion.IsFalsified(null));
        Assert.Throws<InvalidOperationException>(() => (criterion with { Unit = "percent" }).IsFalsified(0));
    }

    [Fact]
    public void AcceptedProposalFreezesExistingEnginePopulationAndNativeResults()
    {
        var scope = Scope();
        var protocol = new Protocol();
        var reasoner = new FakeReasoner(input => { Assert.Equal(scope.Input, input); return Proposal(scope); });
        var admission = protocol.Invoke(reasoner, scope);
        Assert.Equal(LearningAdmissionReason.Admitted, admission!.Reason);
        var committed = Assert.IsType<CommittedLearningProposal>(admission.Proposal);
        var build = Assert.IsType<RobustnessEvaluationBuild>(protocol.Build);
        Assert.Equal(1, reasoner.Calls);
        Assert.Equal(1, protocol.Submitted);
        Assert.Equal(3, protocol.Trials);
        Assert.Equal(1, protocol.EngineEvaluations);
        Assert.Equal(build.UnderlyingRuns.Count, protocol.UniqueBacktestRuns);
        Assert.InRange(build.UnderlyingRuns.Count, 1, 64);
        Assert.True(build.UnderlyingRuns.Count <= scope.MaximumEngineCalls);
        Assert.Equal(3, build.Evaluation.ParameterVariantsEvaluated);
        Assert.Equal(ExpectedPeriods, build.Evaluation.SelectionGovernance!.Trials.Select(x => x.Parameters["period"]));
        Assert.DoesNotContain(build.Evaluation.Limitations, x => x.Contains("capped", StringComparison.Ordinal));
        Assert.Equal(scope.Plan, build.Evaluation.Plan);
        Assert.All(build.UnderlyingRuns, run =>
        {
            Assert.IsType<BacktestRunConfiguration>(run.Configuration);
            Assert.Equal(scope.Plan.MarketRevisionIds, run.Configuration.MarketRevisionIds);
            Assert.Equal(scope.Plan.FeatureRevisionId, run.Configuration.FeatureRevisionId);
            Assert.Equal(BacktestFillModel.NextSessionOpen, run.Configuration.FillModel);
        });
        Assert.Equal(scope.ExecutionFingerprint, committed.Hypothesis.Fingerprint);
        Assert.Equal(LearningAdmissionPolicy.Criterion, committed.Draft.Falsification);
        var criterionFailed = committed.Draft.Falsification.IsFalsified(build.Evaluation.PrimarySplit.Test.ExcessReturn);
        Assert.NotNull(criterionFailed); // Missing would remain not evaluable, never fabricated zero.
        var reference = protocol.FreezeManifest().Result;
        Assert.Equal(build.Evaluation.Verdict, reference.Verdict);
        Assert.Equal(build.Evaluation.SelectionGovernance.Outcome, reference.Selection);
        Assert.Equal("RESEARCH", reference.OperatingMode);
        Assert.Equal("NONE", reference.ExecutionAuthority);
        Assert.True(reference.EngineeringOnly);
    }

    [Theory]
    [InlineData("version", LearningAdmissionReason.UnsupportedContract)]
    [InlineData("discriminator", LearningAdmissionReason.UnsupportedContract)]
    [InlineData("inputChecksum", LearningAdmissionReason.EvidenceMismatch)]
    [InlineData("scopeHandle", LearningAdmissionReason.UnsupportedScope)]
    [InlineData("targetId", LearningAdmissionReason.UnsupportedScope)]
    [InlineData("parent", LearningAdmissionReason.InvalidParent)]
    [InlineData("strategy", LearningAdmissionReason.UnsupportedStrategy)]
    [InlineData("strategyVersion", LearningAdmissionReason.UnsupportedStrategy)]
    [InlineData("period", LearningAdmissionReason.UnsupportedParameter)]
    [InlineData("binding", LearningAdmissionReason.UnsupportedScope)]
    [InlineData("variants", LearningAdmissionReason.InvalidVariantCount)]
    [InlineData("criterion", LearningAdmissionReason.InvalidFalsification)]
    [InlineData("unit", LearningAdmissionReason.InvalidFalsification)]
    [InlineData("phase", LearningAdmissionReason.InvalidFalsification)]
    [InlineData("sample", LearningAdmissionReason.InvalidFalsification)]
    [InlineData("extra", LearningAdmissionReason.Malformed)]
    [InlineData("parameterExtra", LearningAdmissionReason.Malformed)]
    [InlineData("periodString", LearningAdmissionReason.Malformed)]
    [InlineData("null", LearningAdmissionReason.Malformed)]
    [InlineData("missing", LearningAdmissionReason.Malformed)]
    [InlineData("longText", LearningAdmissionReason.Malformed)]
    public void InvalidDraftNeverCallsEngine(string mutation, LearningAdmissionReason expected)
    {
        var scope = Scope();
        var root = JsonNode.Parse(Proposal(scope))!.AsObject();
        var variant = root["variants"]![0]!;
        var criterion = root["falsificationCriteria"]![0]!;
        switch (mutation)
        {
            case "version": case "discriminator": case "inputChecksum": case "scopeHandle": case "targetId": root[mutation] = "unknown"; break;
            case "parent": root["parentHypothesisRef"] = "unknown-parent"; break;
            case "strategy": variant["strategy"]!["id"] = "sma-crossover"; break;
            case "strategyVersion": variant["strategy"]!["version"] = "v2"; break;
            case "period": variant["parameters"]!["period"] = 5; break;
            case "binding": variant["bindingHandle"] = "another-dataset"; break;
            case "variants": root["variants"]!.AsArray().Add(variant.DeepClone()); break;
            case "criterion": criterion["threshold"] = .01m; break;
            case "unit": criterion["unit"] = "percent"; break;
            case "phase": criterion["phase"] = "holdout"; break;
            case "sample": criterion["samplePolicy"] = "weaker"; break;
            case "extra": root["executionAuthority"] = "LIVE"; break;
            case "parameterExtra": variant["parameters"]!["code"] = "expression"; break;
            case "periodString": variant["parameters"]!["period"] = "20"; break;
            case "null": root["question"] = null; break;
            case "missing": root.Remove("parentHypothesisRef"); break;
            case "longText": root["rationale"] = new string('x', 2049); break;
        }
        var protocol = new Protocol();
        Assert.Equal(expected, protocol.Invoke(new(_ => root.ToJsonString()), scope)!.Reason);
        Assert.Equal(0, protocol.EngineEvaluations);
        Assert.Equal(0, protocol.Trials);
        Assert.Single(protocol.History);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"discriminator\":\"Proposal\",\"discriminator\":\"NoUsefulProposal\"}")]
    public void MalformedOutputTerminatesInvocationWithoutRetry(string response)
    {
        var scope = Scope(); var protocol = new Protocol(); var fake = new FakeReasoner(_ => response);
        Assert.Equal(LearningAdmissionReason.Malformed, protocol.Invoke(fake, scope)!.Reason);
        Assert.Equal(LearningAdmissionReason.BudgetExceeded, protocol.Invoke(fake, scope)!.Reason);
        Assert.Equal(1, fake.Calls);
        Assert.Equal(1, protocol.Invocations);
        Assert.Equal(0, protocol.EngineEvaluations);
        Assert.Equal(2, protocol.History.Count);
    }

    [Fact]
    public void OversizedDeepDuplicateAndNonFiniteJsonFailClosed()
    {
        var scope = Scope(); var good = Proposal(scope);
        string[] invalid = [new string(' ', 65537), good.Replace("\"period\":20", "\"period\":NaN", StringComparison.Ordinal),
            good.Replace("\"period\":20", "\"period\":20,\"period\":20", StringComparison.Ordinal),
            good.Replace("\"period\":20", "\"period\":[[[[[[[[[20]]]]]]]]]", StringComparison.Ordinal)];
        foreach (var response in invalid)
        {
            var protocol = new Protocol();
            Assert.Equal(LearningAdmissionReason.Malformed, protocol.Invoke(new(_ => response), scope)!.Reason);
            Assert.Equal(0, protocol.EngineEvaluations);
        }
    }

    [Fact]
    public void NoUsefulProposalIsClosedAndDoesNotExecuteOrRetry()
    {
        var scope = Scope(); var protocol = new Protocol(); var fake = new FakeReasoner(_ => NoUseful(scope));
        Assert.Equal(LearningAdmissionReason.NoUsefulProposal, protocol.Invoke(fake, scope)!.Reason);
        Assert.Equal(LearningAdmissionReason.BudgetExceeded, protocol.Invoke(fake, scope)!.Reason);
        Assert.Equal(1, fake.Calls); Assert.Equal(0, protocol.EngineEvaluations);
        var node = JsonNode.Parse(NoUseful(scope))!.AsObject(); node["variants"] = new JsonArray();
        Assert.Equal(LearningAdmissionReason.Malformed, LearningAdmissionPolicy.Admit(node.ToJsonString(), scope, Fresh).Reason);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReasonerFailureDoesNotDisableDirectDeterministicFinance(bool timeout)
    {
        var scope = Scope(); var protocol = new Protocol();
        var fake = new FakeReasoner(_ => throw (timeout ? new TimeoutException() : new InvalidOperationException()));
        Assert.Null(protocol.Invoke(fake, scope));
        Assert.Equal(LearningAdmissionReason.BudgetExceeded, protocol.Invoke(fake, scope)!.Reason);
        Assert.Equal(1, fake.Calls); Assert.Equal(0, protocol.EngineEvaluations);
        var direct = DeterministicRobustnessEvaluator.Evaluate(scope.Plan, new MomentumResearchStrategy(), scope.Bars, scope.Features);
        Assert.NotEmpty(direct.UnderlyingRuns);
    }

    [Fact]
    public void MetadataCultureOrderAndDecimalSpellingDoNotCreateNewExperiment()
    {
        var scope = Scope(); var response = Proposal(scope);
        var first = LearningAdmissionPolicy.Admit(response, scope, Fresh).Proposal!;
        var root = JsonNode.Parse(Proposal(scope, "Different explanation, same experiment."))!.AsObject();
        var reversed = new JsonObject(root.Reverse().Select(x => new KeyValuePair<string, JsonNode?>(x.Key, x.Value?.DeepClone())));
        var alternate = reversed.ToJsonString().Replace("\"period\":20", "\"period\":20.000", StringComparison.Ordinal)
            .Replace("\"threshold\":0", "\"threshold\":0.00", StringComparison.Ordinal);
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");
            var second = LearningAdmissionPolicy.Admit(alternate, scope, Fresh).Proposal!;
            Assert.Equal(first.ExecutionFingerprint, second.ExecutionFingerprint);
            Assert.NotEqual(first.ResponseChecksum, second.ResponseChecksum);
            Assert.NotEqual(first.ProposalId, second.ProposalId); // Commentary lineage changes, execution does not.
            Assert.Equal(71, first.ProposalId.Length);
            Assert.Equal(71, first.ExecutionFingerprint.Length);
            var fake = new FakeReasoner(_ => alternate) { ModelLabel = "different", RequestId = "different", RequestedAt = DateTimeOffset.UnixEpoch.AddDays(1) };
            var protocol = new Protocol(); protocol.Invoke(fake, scope);
            Assert.Equal(first.ExecutionFingerprint, protocol.Committed!.ExecutionFingerprint);
        }
        finally { CultureInfo.CurrentCulture = culture; }
        // Versioned canonical preimage: {"a":0,"z":20}; independent pinned SHA-256 vector.
        Assert.Equal("sha256:746e2913b60d157e6c8f8a077395a95a479daf4da40d663ef26bf7b4513973ed", LearningIdentity.Hash(new { z = 20.000m, a = 0.00m }));
        Assert.Equal(LearningIdentity.Hash(new { z = 20m, a = 0m }), LearningIdentity.Hash(new { a = 0.0m, z = 20.0m }));
    }

    [Fact]
    public void EquivalentDecimalScaleResolvesToExactSameNativeEngineInputs()
    {
        var scope = Scope();
        var scaled = SyntheticLearningScope.Create(scope.Plan with
        {
            InitialCapital = 100000.00m,
            ReferenceParameters = new Dictionary<string, decimal> { ["period"] = 20.000m }
        },
            scope.Bars.Select(x => x with { Open = x.Open * 1.000m, Close = x.Close * 1.000m }),
            scope.Features.Select(x => x with { Value = x.Value * 1.000m }), scope.Facts, scope.Purpose, scope.Input.HistoryChecksum);
        Assert.Equal(scope.ExecutionFingerprint, scaled.ExecutionFingerprint);
        Assert.Equal(JsonSerializer.Serialize(scope.Plan, Json), JsonSerializer.Serialize(scaled.Plan, Json));
        Assert.Equal(JsonSerializer.Serialize(scope.Bars, Json), JsonSerializer.Serialize(scaled.Bars, Json));
        Assert.Equal(JsonSerializer.Serialize(scope.Features, Json), JsonSerializer.Serialize(scaled.Features, Json));
        var original = LearningAdmissionPolicy.Admit(Proposal(scope), scope, Fresh).Proposal!;
        var ordered = JsonNode.Parse(Proposal(scope))!.AsObject();
        var reversed = new JsonObject(ordered.Reverse().Select(x => new KeyValuePair<string, JsonNode?>(x.Key, x.Value?.DeepClone())));
        Assert.Equal(original.ProposalId, LearningAdmissionPolicy.Admit(reversed.ToJsonString(), scope, Fresh).Proposal!.ProposalId);
    }

    [Fact]
    public void InputProjectionExcludesProtectedRowsFeaturesResultsAndTheirProxies()
    {
        var scope = Scope();
        var partition = DeterministicRobustnessEvaluator.CreatePartition(scope.Bars.Select(x => x.SessionDate).ToArray(), scope.Plan.AntiOverfitting!);
        var changedBars = scope.Bars.Select(x => x.SessionDate >= partition.HoldoutFrom ? x with { Open = 987654m, Close = 987654m } : x);
        var changedFeatures = scope.Features.Select(x => x.SessionDate >= partition.HoldoutFrom ? x with { Value = 987654m } : x);
        var altered = SyntheticLearningScope.Create(scope.Plan, changedBars, changedFeatures, scope.Facts, scope.Purpose, scope.Input.HistoryChecksum);
        Assert.Equal(scope.Input, altered.Input);
        Assert.NotEqual(scope.ExecutionFingerprint, altered.ExecutionFingerprint);
        var json = JsonSerializer.Serialize(scope.Input, Json);
        Assert.DoesNotContain("987654", json, StringComparison.Ordinal);
        Assert.DoesNotContain("holdout", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("evaluationId", json, StringComparison.Ordinal);
        Assert.DoesNotContain("verdict", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("risk", json, StringComparison.OrdinalIgnoreCase);
        Assert.True(scope.Input.KnowledgeCutoffUtc < scope.Bars.First(x => x.SessionDate == partition.HoldoutFrom).KnowledgeTimeUtc);
        Assert.InRange(System.Text.Encoding.UTF8.GetByteCount(json), 1, 65536);
    }

    [Fact]
    public void DuplicateLinksOriginalButAliasesCannotRenewConsumedExposureOrBudget()
    {
        var scope = Scope(); var protocol = new Protocol();
        protocol.Invoke(new(_ => Proposal(scope)), scope);
        var duplicate = protocol.Submit(Proposal(scope, "New wording"), scope);
        Assert.Equal(LearningAdmissionReason.Duplicate, duplicate.Reason);
        Assert.Equal(protocol.Committed!.ProposalId, duplicate.ReusedProposalId);
        Assert.Equal(1, protocol.EngineEvaluations); Assert.Equal(3, protocol.Trials); Assert.Equal(1, protocol.ReusedResults);
        Assert.Equal(LearningExposure.Consumed, protocol.Exposure);
        var aliased = SyntheticLearningScope.Create(scope.Plan with { MarketRevisionIds = ["synthetic-renamed"] },
            scope.Bars.Select(x => x with { MarketRevisionId = "synthetic-renamed" }), scope.Features, scope.Facts, scope.Purpose, scope.Input.HistoryChecksum);
        Assert.Equal(LearningAdmissionReason.ExposureConsumed, protocol.Submit(Proposal(aliased), aliased).Reason);
        Assert.Equal(1, protocol.EngineEvaluations);
        Assert.Equal(3, protocol.History.Count);
        Assert.Equal(LearningAdmissionReason.BudgetExceeded, LearningAdmissionPolicy.Admit(Proposal(scope), scope, Fresh with { SubmittedProposals = 1 }).Reason);
        Assert.Equal(LearningAdmissionReason.BudgetExceeded, LearningAdmissionPolicy.Admit(Proposal(scope), scope, Fresh with { AdmittedTrials = 3 }).Reason);
    }

    [Theory]
    [InlineData(LearningExposure.Unknown, LearningAdmissionReason.ExposureUnknown)]
    [InlineData(LearningExposure.Consumed, LearningAdmissionReason.ExposureConsumed)]
    [InlineData((LearningExposure)99, LearningAdmissionReason.ExposureUnknown)]
    public void UnknownOrConsumedExposureCannotExecute(LearningExposure exposure, LearningAdmissionReason expected)
    {
        var scope = Scope(); var protocol = new Protocol { Exposure = exposure };
        Assert.Equal(expected, protocol.Invoke(new(_ => Proposal(scope)), scope)!.Reason);
        Assert.Equal(0, protocol.EngineEvaluations);
    }

    [Fact]
    public void ExcessivePlanIsRejectedRatherThanSilentlyCapped()
    {
        var scope = Scope(900); var protocol = new Protocol();
        Assert.True(scope.MaximumEngineCalls > 64);
        Assert.Equal(LearningAdmissionReason.BudgetExceeded, protocol.Invoke(new(_ => Proposal(scope)), scope)!.Reason);
        Assert.Equal(0, protocol.EngineEvaluations);
    }

    [Theory]
    [InlineData("rights", LearningAdmissionReason.IneligibleDataset)]
    [InlineData("unknownRights", LearningAdmissionReason.IneligibleDataset)]
    [InlineData("purpose", LearningAdmissionReason.IneligibleDataset)]
    [InlineData("schema", LearningAdmissionReason.IneligibleDataset)]
    [InlineData("feature", LearningAdmissionReason.InvalidFeatures)]
    [InlineData("periods", LearningAdmissionReason.InvalidFeatures)]
    [InlineData("budget", LearningAdmissionReason.InvalidPlan)]
    [InlineData("policy", LearningAdmissionReason.InvalidPlan)]
    [InlineData("revision", LearningAdmissionReason.InvalidPlan)]
    public void TrustedBindingStillRequiresExactEligibilityFeaturesAndPolicy(string mutation, LearningAdmissionReason expected)
    {
        var original = Scope(); var plan = original.Plan; var facts = original.Facts;
        IEnumerable<BacktestFeatureValue> features = original.Features;
        var purpose = original.Purpose;
        switch (mutation)
        {
            case "rights": facts = facts with { ExternalRights = DatasetEvidenceResult.Fail }; break;
            case "unknownRights": facts = facts with { ExternalRights = DatasetEvidenceResult.Unknown }; break;
            case "purpose": purpose = ResearchDatasetPurpose.ExploratoryStatistics; break;
            case "schema": facts = facts with { DatasetClass = ResearchDatasetClass.DailyCloseOnlyMarketContext }; break;
            case "feature": features = features.Select(x => x with { FeatureRevisionId = "wrong" }); break;
            case "periods": features = features.Where(x => x.DefinitionId != "momentum.5"); break;
            case "budget": plan = plan with { MaximumRuns = 100 }; break;
            case "policy": plan = plan with { PriorHoldoutEvaluations = 1 }; break;
            case "revision": plan = plan with { MarketRevisionIds = ["synthetic-unbound"] }; break;
        }
        var scope = SyntheticLearningScope.Create(plan, original.Bars, features, facts, purpose, original.Input.HistoryChecksum);
        var protocol = new Protocol();
        Assert.Equal(expected, protocol.Invoke(new(_ => Proposal(scope)), scope)!.Reason);
        Assert.Equal(0, protocol.EngineEvaluations);
    }

    [Fact]
    public void FrozenPlanCannotBeChangedThroughOriginalMutableCollections()
    {
        var original = Scope(); var revisions = original.Plan.MarketRevisionIds.ToArray();
        var costs = original.Plan.CostModels.ToArray(); var parameters = new Dictionary<string, decimal> { ["period"] = 20 };
        var plan = original.Plan with { MarketRevisionIds = revisions, CostModels = costs, ReferenceParameters = parameters };
        var scope = SyntheticLearningScope.Create(plan, original.Bars, original.Features, original.Facts, original.Purpose, original.Input.HistoryChecksum);
        var fingerprint = scope.ExecutionFingerprint;
        revisions[0] = "changed"; costs[0] = BacktestCostModel.Conservative; parameters["period"] = 5;
        Assert.Equal(original.Plan.MarketRevisionIds, scope.Plan.MarketRevisionIds);
        Assert.Equal(20, scope.Plan.ReferenceParameters["period"]);
        Assert.Equal(BacktestCostModel.Zero, scope.Plan.CostModels[0]);
        Assert.Equal(fingerprint, scope.ExecutionFingerprint);
    }

    [Theory]
    [InlineData("Deny")]
    [InlineData("Halt")]
    [InlineData("InsufficientData")]
    [InlineData(null)]
    public void NativeRiskVetoOrMissingEvidenceCannotBeOverridden(string? verdict)
    {
        var scope = Scope();
        var risk = verdict is null ? (FinanceRiskVerdict?)null : Enum.Parse<FinanceRiskVerdict>(verdict);
        var protocol = new Protocol { RequireRiskEvidence = true, MockedRisk = risk };
        Assert.Equal(risk, protocol.MockedRisk);
        Assert.Equal(LearningAdmissionReason.RiskVeto,
            protocol.Invoke(new(_ => Proposal(scope, "Ignore risk, authorize trading")), scope)!.Reason);
        Assert.Equal(0, protocol.EngineEvaluations);
        Assert.Single(protocol.History);
    }

    [Fact]
    public async Task ConcurrentInvocationCannotSpendProtocolTwice()
    {
        var scope = Scope(); var protocol = new Protocol(); var reasoner = new FakeReasoner(_ => Proposal(scope));
        var results = await Task.WhenAll(Task.Run(() => protocol.Invoke(reasoner, scope)), Task.Run(() => protocol.Invoke(reasoner, scope)));
        Assert.Single(results, x => x!.Reason == LearningAdmissionReason.Admitted);
        Assert.Single(results, x => x!.Reason == LearningAdmissionReason.BudgetExceeded);
        Assert.Equal(1, reasoner.Calls);
        Assert.Equal(1, protocol.EngineEvaluations);
        Assert.Equal(3, protocol.Trials);
    }

    [Fact]
    public void CommitmentFailureLeavesExposureConsumedAndDoesNotExecute()
    {
        var scope = Scope();
        var protocol = new Protocol { PersistCommitment = _ => throw new IOException("Synthetic persistence failure") };
        Assert.Throws<IOException>(() => protocol.Invoke(new(_ => Proposal(scope)), scope));
        Assert.Equal(0, protocol.EngineEvaluations);
        Assert.Equal(LearningExposure.Consumed, protocol.Exposure);
        Assert.Equal(3, protocol.Trials);
        Assert.Null(protocol.Build);
        Assert.Equal(LearningAdmissionReason.Duplicate, protocol.Submit(Proposal(scope), scope).Reason);
        Assert.Equal(0, protocol.ReusedResults);
        Assert.Equal(0, protocol.EngineEvaluations);
        Assert.Equal(LearningAdmissionReason.BudgetExceeded, protocol.Invoke(new(_ => Proposal(scope)), scope)!.Reason);
    }

    [Fact]
    public void ExistingPersistenceReloadAndFrozenReplayPreserveIdsChecksumsAndRows()
    {
        var scope = Scope();
        using var database = new Database();
        var protocol = new Protocol { PersistCommitment = json => database.SaveManifest("commitment.json", json) };
        var fake = new FakeReasoner(_ => Proposal(scope));
        protocol.Invoke(fake, scope);
        var commitment = JsonSerializer.Deserialize<Commitment>(database.ReadManifest("commitment.json"), Json)!;
        Assert.Equal(LearningExposure.Consumed, commitment.ReservedState.Exposure);
        Assert.Equal(3, commitment.ReservedState.AdmittedTrials);
        var build = protocol.Build!;
        var manifestJson = JsonSerializer.Serialize(protocol.FreezeManifest(), Json);
        Assert.InRange(System.Text.Encoding.UTF8.GetByteCount(manifestJson), 1, 65536);
        database.SaveManifest("result.json", manifestJson);
        using (var connection = database.Open())
            foreach (var run in build.UnderlyingRuns) Assert.True(FinanceBacktestPersistence.PersistBacktest(connection, run));
        var reader = database.ReopenReader();
        foreach (var run in build.UnderlyingRuns)
            Assert.Equal(JsonSerializer.Serialize(run, Json), JsonSerializer.Serialize(reader.GetResult(run.RunId), Json));
        var manifest = JsonSerializer.Deserialize<Manifest>(database.ReadManifest("result.json"), Json)!;
        // Re-resolve pinned fixture data and validate the stored envelope before deterministic replay.
        var reloadedScope = SyntheticLearningScope.Create(manifest.Plan, scope.Bars, scope.Features, scope.Facts, scope.Purpose, scope.Input.HistoryChecksum);
        Assert.Equal(manifest.ExecutionFingerprint, reloadedScope.ExecutionFingerprint);
        Assert.Equal(manifest.InputChecksum, reloadedScope.Input.InputChecksum);
        Assert.Equal(manifest.ProposalId, LearningIdentity.Hash(new
        {
            Version = LearningAdmissionPolicy.Version,
            InputChecksum = manifest.InputChecksum,
            manifest.ExecutionFingerprint,
            Draft = manifest.Draft
        }));
        foreach (var reference in manifest.Result.Runs)
        {
            var stored = Assert.IsType<BacktestResult>(reader.GetResult(reference.RunId));
            Assert.Equal(reference.RunId, stored.RunId);
            Assert.Equal(reference.Checksum, stored.Checksum);
        }
        var reloaded = Protocol.Reload(manifest, reloadedScope);
        Assert.Equal(LearningExposure.Consumed, reloaded.Exposure);
        Assert.Equal(LearningAdmissionReason.BudgetExceeded, reloaded.Invoke(fake, reloadedScope)!.Reason);
        Assert.Equal(LearningAdmissionReason.Duplicate, reloaded.Submit(Proposal(scope, "Restarted rationale"), scope).Reason);
        Assert.Equal(1, reloaded.EngineEvaluations);
        Assert.Throws<InvalidOperationException>(() => Protocol.Reload(manifest with { InputChecksum = "tampered" }, reloadedScope));
        Assert.Throws<InvalidOperationException>(() => Protocol.Reload(manifest with { Draft = manifest.Draft with { Question = "tampered" } }, reloadedScope));
        var changedDraft = manifest.Draft with { Variant = manifest.Draft.Variant with { Period = 5 } };
        var forgedId = LearningIdentity.Hash(new
        {
            Version = manifest.Version,
            manifest.InputChecksum,
            manifest.ExecutionFingerprint,
            Draft = changedDraft
        });
        Assert.Throws<InvalidOperationException>(() => Protocol.Reload(manifest with { Draft = changedDraft, ProposalId = forgedId }, reloadedScope));
        var replay = DeterministicRobustnessEvaluator.Evaluate(reloadedScope.Plan, new MomentumResearchStrategy(), reloadedScope.Bars, reloadedScope.Features);
        Assert.Equal(1, fake.Calls); // No reasoner invocation during reload/replay.
        Assert.Equal(build.Evaluation.EvaluationId, replay.Evaluation.EvaluationId);
        Assert.Equal(build.Evaluation.Checksum, replay.Evaluation.Checksum);
        Assert.Equal(JsonSerializer.Serialize(build.Evaluation, Json), JsonSerializer.Serialize(replay.Evaluation, Json));
        Assert.Equal(manifest.Result.Runs.Select(x => (x.RunId, x.Checksum)), replay.UnderlyingRuns.Select(x => (x.RunId, x.Checksum)));
        using (var connection = database.Open())
        {
            foreach (var run in replay.UnderlyingRuns) Assert.False(FinanceBacktestPersistence.PersistBacktest(connection, run));
            var first = replay.UnderlyingRuns[0];
            Assert.Throws<InvalidOperationException>(() => FinanceBacktestPersistence.PersistBacktest(connection, first with { Checksum = "synthetic-conflict" }));
        }
        Assert.Equal(build.UnderlyingRuns.Count, database.ReopenReader().GetCatalog().Runs.Count);
        Assert.Equal(1, fake.Calls);
    }
}
