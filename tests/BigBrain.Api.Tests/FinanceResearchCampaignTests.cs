using System.Text.Json;
using Microsoft.Data.Sqlite;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceResearchCampaignTests
{

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public void SqliteCampaignReloadAndExactReplayPreserveImmutableEvidence()
    {
        // Reuse the BB-127 ingestion fixture: one OHLCV series and one close-only
        // context series, neither with immutable feature/holdout evidence.
        using var fixture = new FinanceResearchDatasetTests.Fixture();
        fixture.WriteReadyPackage("campaign.zip");
        _ = Assert.Single(fixture.Scanner.ScanOnce());
        var datasets = fixture.Store.ResearchCatalog().Datasets.OrderBy(x => x.RevisionId, StringComparer.Ordinal).ToArray();
        Assert.Equal(2, datasets.Length);
        Assert.All(datasets, x => Assert.False(x.CanonicalPromoted));
        var knowledgeTime = new DateTimeOffset(2026, 9, 3, 8, 30, 0, TimeSpan.Zero);
        var first = fixture.Store.RunResearchCampaign(knowledgeTime);
        Assert.Equal(ResearchCampaignStatus.Completed, first.Status);
        Assert.Equal(knowledgeTime, first.CreatedUtc);
        Assert.Equal(knowledgeTime, first.Definition.KnowledgeTimeUtc);
        Assert.Equal(FinanceResearchContracts.Fingerprint(first.Definition), first.Checksum);
        Assert.Equal("campaign-" + first.Checksum[7..23], first.CampaignId);
        // Time is part of the definition, not a fresh wall-clock stamp at replay.
        Assert.NotEqual(first.Checksum, FinanceResearchContracts.Fingerprint(
            first.Definition with { KnowledgeTimeUtc = knowledgeTime.AddSeconds(1) }));
        Assert.Equal(datasets.Select(x => x.RevisionId), first.Definition.DatasetRevisionIds);
        Assert.Equal(JsonSerializer.Serialize(FinanceResearchCampaignPolicy.Population(), Json),
            JsonSerializer.Serialize(first.Definition.Population, Json));
        Assert.Equal(6, first.Results.Count);
        Assert.Equal(6, first.Results.Select(x => x.ResultId).Distinct(StringComparer.Ordinal).Count());
        var familyOrdinals = new Dictionary<string, int>(StringComparer.Ordinal);
        var resultIndex = 0;
        foreach (var dataset in datasets)
        foreach (var variant in first.Definition.Population)
        {
            var attempt = first.Results[resultIndex++];
            Assert.Equal(dataset.RevisionId, attempt.DatasetRevisionId);
            Assert.Equal(dataset.DatasetFingerprint, attempt.DatasetFingerprint);
            Assert.Equal(dataset.Symbol, attempt.Instrument);
            Assert.Equal(variant.HypothesisId, attempt.HypothesisId);
            Assert.Equal(variant.FamilyId, attempt.FamilyId);
            familyOrdinals.TryGetValue(variant.FamilyId, out var ordinal);
            Assert.Equal(ordinal + 1, attempt.FamilyAttemptOrdinal);
            familyOrdinals[variant.FamilyId] = ordinal + 1;
            Assert.Equal("campaign-result-" + FinanceResearchContracts.Fingerprint(new
            { campaignId = first.CampaignId, dataset.RevisionId, variant.HypothesisId })[7..23], attempt.ResultId);
            Assert.Equal(ResearchCampaignDisposition.InconclusiveNotEvaluable, attempt.Disposition);
            Assert.Null(attempt.BacktestRunId);
            Assert.NotEmpty(attempt.Limitations);
            Assert.Equal(dataset.SchemaClass == ResearchDatasetClass.DailyOhlcv.ToString()
                ? "INSUFFICIENT_DATA" : "DATASET_INELIGIBLE", Assert.Single(attempt.ReasonCodes));
        }
        Assert.Equal(6, first.Scorecard.TotalAttempts);
        Assert.Equal(6, first.Scorecard.InconclusiveNotEvaluable);
        Assert.Equal(0, first.Scorecard.Rejected);
        Assert.Equal(0, first.Scorecard.SurvivedInitialScreen);
        Assert.Equal(0, first.Scorecard.RobustCandidates);
        Assert.Equal(3, first.Scorecard.Reasons["INSUFFICIENT_DATA"]);
        Assert.Equal(3, first.Scorecard.Reasons["DATASET_INELIGIBLE"]);
        Assert.Equal("RESEARCH / 0 SEK / NONE", first.Scorecard.SafetyState);

        var before = CampaignRows(fixture.Market);
        Assert.Single(before);
        var datasetsBefore = DatasetRows(fixture.Market);
        Assert.Equal(126, datasetsBefore.Length); // two revision rows + 120 OHLCV + four context rows
        var evidence = JsonSerializer.Serialize(first, Json);
        // All production calls above disposed their method-local connections.
        // The store has no IDisposable/in-memory campaign cache. Clear this file's
        // pool and construct new store + public reader, never reusing the old store.
        using (var poolKey = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = fixture.Market.DatabasePath }.ToString())) SqliteConnection.ClearPool(poolKey);
        var restarted = new FinanceDatasetIntakeStore(fixture.Market, fixture.Options);
        var reader = new FinanceResearchCampaignReader(restarted);
        var loaded = Assert.IsType<ResearchCampaign>(reader.GetDetail(first.CampaignId));
        Assert.Equal(evidence, JsonSerializer.Serialize(loaded, Json));
        Assert.Equal(before, CampaignRows(fixture.Market));
        Assert.Equal(datasetsBefore, DatasetRows(fixture.Market));
        Assert.Null(reader.GetDetail("missing-synthetic-campaign"));
        var catalog = reader.GetCatalog();
        Assert.Equal("RESEARCH", catalog.OperatingMode);
        Assert.Equal(0m, catalog.BudgetSek);
        Assert.Equal("NONE", catalog.ExecutionAuthority);
        var summary = Assert.Single(catalog.Campaigns);
        Assert.Equal(first.CampaignId, summary.CampaignId);
        Assert.Equal(first.Checksum, summary.Checksum);
        Assert.Equal(first.CreatedUtc, summary.CreatedUtc);
        Assert.Equal(first.Status, summary.Status);
        Assert.Equal(6, summary.TotalAttempts);
        Assert.Equal(0, summary.RobustCandidates);
        Assert.Equal(before, CampaignRows(fixture.Market));

        var replay = restarted.RunResearchCampaign(knowledgeTime);
        Assert.Equal(evidence, JsonSerializer.Serialize(replay, Json));
        var reloaded = new FinanceResearchCampaignReader(new FinanceDatasetIntakeStore(fixture.Market, fixture.Options));
        Assert.Equal(evidence, JsonSerializer.Serialize(reloaded.GetDetail(first.CampaignId), Json));
        Assert.Single(reloaded.GetCatalog().Campaigns);
        Assert.Equal(before, CampaignRows(fixture.Market));
        Assert.Equal(datasetsBefore, DatasetRows(fixture.Market));
        Assert.Equal(0, fixture.Count("backtest_runs"));
        Assert.Equal(0, fixture.Count("observations"));
    }

    private static string[] CampaignRows(EodhdFinanceOptions options) => Rows(options,
        "SELECT campaign_id,checksum,status,created_utc,definition_json,results_json,scorecard_json FROM research_campaigns ORDER BY campaign_id");

    private static string[] DatasetRows(EodhdFinanceOptions options) =>
        Rows(options, "SELECT * FROM research_dataset_revisions ORDER BY revision_id")
            .Concat(Rows(options, "SELECT * FROM research_dataset_observations ORDER BY revision_id,session_date")).ToArray();

    private static string[] Rows(EodhdFinanceOptions options, string sql)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = options.DatabasePath, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read())
        {
            var values = new object[reader.FieldCount];
            _ = reader.GetValues(values);
            // Keep original JSON/text/timestamps and SQLite values verbatim; only
            // delimit the snapshot structurally. No scientific normalization.
            rows.Add(JsonSerializer.Serialize(values, Json));
        }
        return rows.ToArray();
    }

    [Fact]
    public void PopulationIsDeterministicBoundedAndPredeclared()
    {
        var first=FinanceResearchCampaignPolicy.Population();var second=FinanceResearchCampaignPolicy.Population();
        Assert.Equal(FinanceResearchContracts.Fingerprint(first),FinanceResearchContracts.Fingerprint(second));Assert.Equal(3,first.Count);Assert.True(first.Select(x=>x.FamilyId).Distinct().Count()<=FinanceResearchCampaignPolicy.Limits.MaximumFamilies);
        Assert.All(first.GroupBy(x=>x.FamilyId),x=>Assert.True(x.Count()<=FinanceResearchCampaignPolicy.Limits.MaximumVariantsPerFamily));
        Assert.Equal(24,FinanceResearchCampaignPolicy.Limits.MaximumRuns);Assert.Equal(1,FinanceResearchCampaignPolicy.Limits.MaximumConcurrency);Assert.Equal(0,FinanceResearchCampaignPolicy.Limits.MaximumRetries);
    }

    [Theory]
    [InlineData(false,true,true,true,true,true,true,ResearchCampaignDisposition.InconclusiveNotEvaluable,"DATASET_INELIGIBLE")]
    [InlineData(true,false,true,true,true,true,true,ResearchCampaignDisposition.InconclusiveNotEvaluable,"SCHEMA_INCOMPATIBLE")]
    [InlineData(true,true,true,true,false,true,true,ResearchCampaignDisposition.Rejected,"HOLDOUT_CONTAMINATED")]
    [InlineData(true,true,false,true,true,true,true,ResearchCampaignDisposition.Rejected,"RESEARCH_INTEGRITY_FAILURE")]
    [InlineData(true,true,true,false,true,true,true,ResearchCampaignDisposition.Rejected,"OOS_FAILURE")]
    [InlineData(true,true,true,true,true,true,false,ResearchCampaignDisposition.Rejected,"COST_FRAGILE")]
    [InlineData(true,true,true,true,true,false,true,ResearchCampaignDisposition.Rejected,"ROBUSTNESS_FAILURE")]
    [InlineData(true,true,true,true,true,true,true,ResearchCampaignDisposition.RobustCandidate,"ROBUSTNESS_PASS")]
    public void DispositionIsCategoricalAndFailedGatesOverrideReturn(bool eligible,bool compatible,bool integrity,bool oos,bool holdout,bool robustness,bool costs,ResearchCampaignDisposition expected,string reason)
    {var actual=FinanceResearchCampaignPolicy.Disposition(eligible,compatible,integrity,oos,holdout,robustness,costs);Assert.Equal((expected,reason),actual);}
}
