using System.Globalization;
using System.Text;
using System.Text.Json;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Tests;

public sealed class CanonicalDatasetRevisionIdentityTests
{
    private const string Digest = "31c6ed4a006c7514de08409dca1998be6c8e7af9ee2e06a72cb349f69aa15522";
    private const string Revision = "dataset-v2-" + Digest;
    private const string Csv = "ticker,date,open,high,low,close,volume\nAAPL,2024-01-02,100,102,99,101,1000\nMSFT,2024-01-02,50,51,49,50.5,2000\n";
    private static CanonicalDatasetIdentityRow[] Rows =>
    [new("MSFT", new(2024, 1, 2), 50m, 51m, 49m, 50.5m, null, 2000m),
     new("AAPL", new(2024, 1, 2), 100m, 102m, 99m, 101m, null, 1000m)];

    [Theory]
    [InlineData("", "")]
    [InlineData("en-US", "sv-SE")]
    [InlineData("sv-SE", "th-TH")]
    [InlineData("th-TH", "en-US")]
    public void ExactV2BytesAndIdentityAreCultureAndCalendarIndependent(string culture, string uiCulture)
    {
        using var scope = new CultureScope(culture, uiCulture);
        const string expected = "canonical-dataset-revision-v2\nNASDAQ-WIKI\nPRICES\nAAPL|2024-01-02|100|102|99|101||1000\nMSFT|2024-01-02|50|51|49|50.5||2000";
        Assert.Equal(Encoding.UTF8.GetBytes(expected), CanonicalDatasetRevisionIdentityV2.Serialize(" nasdaq-wiki ", " prices ", Rows));
        var result = CanonicalDatasetRevisionIdentityV2.Create("nasdaq-wiki", "prices", Rows.Reverse());
        Assert.Equal("NASDAQ-WIKI", result.Source);
        Assert.Equal("PRICES", result.Product);
        Assert.Equal(Revision, result.RevisionId);
        Assert.Equal("sha256:" + Digest, result.Checksum);
    }

    [Fact]
    public void EveryIdentityFieldAndDecimalScaleParticipatesAndRowsSortOrdinallyThenByDate()
    {
        var row = Rows[1];
        var reference = CanonicalDatasetRevisionIdentityV2.Create("SOURCE", "PRICES", [row]);
        CanonicalDatasetIdentityRow[] changes =
        [row with { Symbol = "MSFT" }, row with { Date = row.Date.AddDays(1) },
         row with { Open = 100.1m }, row with { High = 103m }, row with { Low = 98m },
         row with { Close = 101.1m }, row with { AdjustedClose = 101m }, row with { Volume = 1001m },
         row with { Open = 100.00m }, row with { Volume = 1000.00m }];
        foreach (var changed in changes)
            Assert.NotEqual(reference.RevisionId, CanonicalDatasetRevisionIdentityV2.Create("SOURCE", "PRICES", [changed]).RevisionId);
        Assert.NotEqual(reference.RevisionId, CanonicalDatasetRevisionIdentityV2.Create("SOURCE", "OTHER", [row]).RevisionId);
        Assert.NotEqual(reference.RevisionId, CanonicalDatasetRevisionIdentityV2.Create("OTHER", "PRICES", [row]).RevisionId);
        var scaled = row with { Open = 100.00m, AdjustedClose = 90.500m, Volume = 1000.0m };
        Assert.EndsWith("AAPL|2024-01-02|100.00|102|99|101|90.500|1000.0", Encoding.UTF8.GetString(CanonicalDatasetRevisionIdentityV2.Serialize("SOURCE", "PRICES", [scaled])));
        var ordered = new[] { row with { Symbol = "Z" }, row with { Symbol = "A" }, row with { Date = row.Date.AddDays(1) }, row };
        var bytes = CanonicalDatasetRevisionIdentityV2.Serialize("SOURCE", "PRICES", ordered);
        Assert.Equal(bytes, CanonicalDatasetRevisionIdentityV2.Serialize("SOURCE", "PRICES", ordered.Reverse()));
        Assert.Equal(["A", "AAPL", "AAPL", "Z"], Encoding.UTF8.GetString(bytes).Split('\n').Skip(3).Select(x => x.Split('|')[0]));
        Assert.Contains("AAPL|2024-01-02", Encoding.UTF8.GetString(bytes).Split('\n')[4], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a/b")]
    [InlineData("a|b")]
    [InlineData("a\nb")]
    [InlineData("å")]
    [InlineData(".prices")]
    [InlineData("has spaces")]
    public void InvalidIdentifiersFailClosed(string? invalid)
    {
        Assert.Throws<InvalidDataException>(() => CanonicalDatasetRevisionIdentityV2.Create("SOURCE", invalid, Rows));
        Assert.Throws<InvalidDataException>(() => CanonicalDatasetRevisionIdentityV2.NormalizeIdentifier(invalid));
    }

    [Fact]
    public void IdentifierLengthAndAsciiContractAreBounded()
    {
        Assert.Equal("A.B_C-9", CanonicalDatasetRevisionIdentityV2.NormalizeIdentifier(" a.b_c-9 "));
        Assert.Equal(new string('A', 64), CanonicalDatasetRevisionIdentityV2.NormalizeIdentifier(new string('a', 64)));
        Assert.Throws<InvalidDataException>(() => CanonicalDatasetRevisionIdentityV2.NormalizeIdentifier(new string('a', 65)));
        Assert.Throws<InvalidDataException>(() => CanonicalDatasetRevisionIdentityV2.Create("a|b", "PRICES", Rows));
        Assert.Throws<InvalidDataException>(() => CanonicalDatasetRevisionIdentityV2.Serialize("SOURCE", "PRICES", [Rows[0] with { Symbol = "A|B" }]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("en-US")]
    [InlineData("sv-SE")]
    [InlineData("th-TH")]
    public void EquivalentCandidatesAcrossCulturesAndRestartShareOnePhysicalRowSet(string culture)
    {
        using var scope = new CultureScope("", "");
        using var fixture = new Fixture();
        var a = fixture.Store.InspectValidatePromote(Candidate("a"), fixture.Path);
        Assert.Equal("Promoted", a.Status);
        Assert.Equal(Revision, a.CanonicalRevisionId);
        Assert.Equal(2, a.PromotedObservationCount);
        var schema = fixture.Scalar("SELECT group_concat(sql,';') FROM sqlite_master ORDER BY name");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        var store = fixture.Restart();
        var replay = store.InspectValidatePromote(Candidate("a") with { CanonicalProduct = "IGNORED-ON-TERMINAL-REPLAY" }, fixture.Path);
        Assert.Equal(a.CanonicalRevisionId, replay.CanonicalRevisionId);
        var b = store.InspectValidatePromote(Candidate("b") with
        {
            SourceName = " nasdaq-wiki ", CanonicalProduct = " prices ",
            Provenance = "Equivalent independent submission", SourceUrl = "https://example.test/other-mirror", OriginalFilename = "renamed.csv"
        }, fixture.Path);
        Assert.Equal(a.ArtifactSha256, b.ArtifactSha256);
        Assert.Equal(a.CanonicalRevisionId, b.CanonicalRevisionId);
        Assert.Equal("1", fixture.Scalar("SELECT COUNT(*) FROM revisions"));
        Assert.Equal("2", fixture.Scalar("SELECT COUNT(*) FROM observations"));
        Assert.Equal("2", fixture.Scalar("SELECT observation_count FROM revisions"));
        Assert.Equal("2", fixture.Scalar("SELECT COUNT(*) FROM dataset_candidates WHERE state='Promoted'"));
        Assert.Equal("NASDAQ-WIKI|PRICES", fixture.Scalar("SELECT provider||'|'||product FROM observations LIMIT 1"));
        Assert.All(store.Catalog().Datasets, x => Assert.Equal(2, x.PromotedObservationCount));
        Assert.Equal(schema, fixture.Scalar("SELECT group_concat(sql,';') FROM sqlite_master ORDER BY name"));
        using var manifest = JsonDocument.Parse(fixture.Scalar("SELECT manifest_json FROM dataset_candidates WHERE candidate_id='b'"));
        var identity = manifest.RootElement.GetProperty("canonicalIdentity");
        Assert.Equal("canonical-dataset-revision-v2", identity.GetProperty("algorithm").GetString());
        Assert.Equal("NASDAQ-WIKI", identity.GetProperty("source").GetString());
        Assert.Equal("PRICES", identity.GetProperty("product").GetString());
    }

    [Fact]
    public void ProductAndSourceSeparateStorageWhileEquivalentReorderedRowsDeduplicate()
    {
        using var fixture = new Fixture();
        var first = fixture.Store.InspectValidatePromote(Candidate("a", "SOURCE", "P1"), fixture.Path);
        File.WriteAllText(fixture.Path, "ticker,date,open,high,low,close,volume\nMSFT,2024-01-02,50,51,49,50.5,2000\nAAPL,2024-01-02,100,102,99,101,1000\n");
        var second = fixture.Store.InspectValidatePromote(Candidate("b", "SOURCE", "P1"), fixture.Path);
        Assert.Equal(first.CanonicalRevisionId, second.CanonicalRevisionId);
        Assert.NotEqual(first.ArtifactSha256, second.ArtifactSha256);
        var product = fixture.Store.InspectValidatePromote(Candidate("c", "SOURCE", "P2"), fixture.Path);
        var source = fixture.Store.InspectValidatePromote(Candidate("d", "OTHER", "P1"), fixture.Path);
        Assert.Equal(3, new[] { first.CanonicalRevisionId, product.CanonicalRevisionId, source.CanonicalRevisionId }.Distinct().Count());
        Assert.Equal("3", fixture.Scalar("SELECT COUNT(*) FROM revisions"));
        Assert.Equal("6", fixture.Scalar("SELECT COUNT(*) FROM observations"));
        Assert.All(fixture.Store.Catalog().Datasets, x => Assert.Equal(2, x.PromotedObservationCount));
    }

    [Theory]
    [InlineData("SOURCE", null)]
    [InlineData("SOURCE", "../prices")]
    [InlineData("SOURCE", "")]
    [InlineData("Source Name", "PRICES")]
    public void PromotionRejectsMissingOrInvalidScopeWithoutCanonicalWrites(string source, string? product)
    {
        using var fixture = new Fixture();
        var result = fixture.Store.InspectValidatePromote(Candidate("invalid", source, product), fixture.Path);
        Assert.Equal("Rejected", result.Status);
        Assert.Equal("Fail", result.PromotionDecision);
        Assert.Contains("CanonicalSourceOrProductMissingOrInvalid", result.Limitations);
        Assert.Null(result.CanonicalRevisionId);
        Assert.Equal("0", fixture.Scalar("SELECT COUNT(*) FROM observations"));
    }

    [Fact]
    public void DiscoveredProductClaimIsRetainedAcrossRestartAndCannotBeReplacedByCaller()
    {
        using var fixture = new Fixture();
        fixture.Store.Discover(Candidate("pending", "SOURCE", "P1"), "fixture");
        var result = fixture.Restart().InspectValidatePromote(Candidate("pending", "OTHER", "P2"), fixture.Path);
        Assert.Equal("Promoted", result.Status);
        Assert.Equal("SOURCE|P1", fixture.Scalar("SELECT provider||'|'||product FROM observations LIMIT 1"));
        using var manifest = JsonDocument.Parse(fixture.Scalar("SELECT manifest_json FROM dataset_candidates WHERE candidate_id='pending'"));
        Assert.Equal("P1", manifest.RootElement.GetProperty("canonicalProduct").GetString());
    }

    [Fact]
    public void AClaimCannotBeRetrofittedToAnExistingCandidateWithNoProduct()
    {
        using var fixture = new Fixture();
        fixture.Store.Discover(Candidate("pending", product: null), "fixture");
        var result = fixture.Restart().InspectValidatePromote(Candidate("pending"), fixture.Path);
        Assert.Equal("Rejected", result.Status);
        Assert.Null(result.CanonicalRevisionId);
        Assert.Equal("0", fixture.Scalar("SELECT COUNT(*) FROM observations"));
    }

    [Fact]
    public void LegacyTerminalBindingAndRowsSurviveRestartReplayAndNewV2Promotion()
    {
        using var fixture = new Fixture();
        fixture.SeedLegacy();
        var before = fixture.LegacySnapshot();
        var store = fixture.Restart();
        var legacy = store.InspectValidatePromote(Candidate("legacy"), fixture.Path);
        Assert.Equal("wiki-dde8381ccbe9ccc5", legacy.CanonicalRevisionId);
        Assert.Equal(2, legacy.PromotedObservationCount);
        var future = store.InspectValidatePromote(Candidate("future"), fixture.Path);
        Assert.Equal(Revision, future.CanonicalRevisionId);
        Assert.Equal(before, fixture.LegacySnapshot());
        Assert.Equal("2", fixture.Scalar("SELECT COUNT(*) FROM revisions"));
        Assert.Equal("4", fixture.Scalar("SELECT COUNT(*) FROM observations"));
    }

    private static ExternalDatasetCandidate Candidate(string id, string source = "NASDAQ-WIKI", string? product = "PRICES") =>
        new(id, source, "https://example.test/data", "fixture", "prices.csv",
            new(DatasetLicenseClass.PublicDomain, "Public domain", "https://example.test/rights", new(2026, 9, 7), "fixture", DatasetEvidenceResult.Pass, true, "fixture"),
            "fixture", DatasetPriceBasis.RawAndAdjusted, DatasetSurvivorshipBias.SurvivorshipUnknown, CanonicalProduct: product);

    [Fact]
    public void NonEodhdEvidenceUsesSharedPersistenceWithoutAcquisitionAndSurvivesRestart()
    {
        using var fixture = new Fixture();
        var canonical = fixture.Store.InspectValidatePromote(Candidate("neutral"), fixture.Path);
        var memory = fixture.Memory();
        // Default BuildFeatures still means EODHD; explicit lineage selects the non-EODHD revision.
        Assert.Throws<InvalidOperationException>(() => memory.BuildFeatures());
        var features = memory.BuildFeatures([canonical.CanonicalRevisionId!]);
        Assert.Equal([Revision], features.SourceMarketRevisions);
        memory.BuildReferenceBacktests();
        var runs = memory.BacktestCatalog().Runs;
        Assert.Equal(6, runs.Count);
        var before = runs.Select(x => JsonSerializer.Serialize(memory.BacktestResult(x.RunId))).ToArray();
        var counts = fixture.Scalar("SELECT (SELECT COUNT(*) FROM backtest_runs)||'|'||(SELECT COUNT(*) FROM backtest_events)||'|'||(SELECT COUNT(*) FROM backtest_fills)||'|'||(SELECT COUNT(*) FROM backtest_equity)");

        var restarted = fixture.Memory();
        Assert.True(restarted.BuildFeatures([Revision]).Idempotent);
        Assert.Equal(features.RevisionId, restarted.FeatureSnapshot(null, null, null, null, null, 10).Revision!.RevisionId);
        Assert.Equal(before, restarted.BacktestCatalog().Runs.Select(x => JsonSerializer.Serialize(restarted.BacktestResult(x.RunId))));
        Assert.Null(restarted.BacktestResult("missing-fixture-run"));
        using var connection = fixture.Connection();
        var result = restarted.BacktestResult(runs[0].RunId)!;
        Assert.Equal([Revision], result.Configuration.MarketRevisionIds);
        Assert.Equal(features.RevisionId, result.Configuration.FeatureRevisionId);
        Assert.False(FinanceBacktestPersistence.PersistBacktest(connection, result));
        Assert.Throws<InvalidOperationException>(() => FinanceBacktestPersistence.PersistBacktest(connection, result with { Checksum = "sha256:synthetic-conflict" }));
        Assert.Equal(counts, fixture.Scalar("SELECT (SELECT COUNT(*) FROM backtest_runs)||'|'||(SELECT COUNT(*) FROM backtest_events)||'|'||(SELECT COUNT(*) FROM backtest_fills)||'|'||(SELECT COUNT(*) FROM backtest_equity)"));
        Assert.Equal(before[0], JsonSerializer.Serialize(restarted.BacktestResult(result.RunId)));
        Assert.Equal("0", fixture.Scalar("SELECT COUNT(*) FROM acquisitions"));
        Assert.Equal("0", fixture.Scalar("SELECT COUNT(*) FROM observations WHERE provider='EODHD'"));
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _ui = CultureInfo.CurrentUICulture;
        internal CultureScope(string culture, string ui) { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture); CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(ui); }
        public void Dispose() { CultureInfo.CurrentCulture = _culture; CultureInfo.CurrentUICulture = _ui; }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bb-v2-identity-tests", Guid.NewGuid().ToString("N"));
        private readonly EodhdFinanceOptions _market;
        private readonly FinanceDatasetOptions _options;
        internal Fixture()
        {
            Directory.CreateDirectory(_root);
            _market = new() { DatabasePath = System.IO.Path.Combine(_root, "finance.db"), PayloadDirectory = System.IO.Path.Combine(_root, "payloads") };
            _options = new() { QuarantineDirectory = System.IO.Path.Combine(_root, "quarantine") };
            _ = new EodhdMarketMemory(_market);
            Store = Restart();
            Path = System.IO.Path.Combine(_root, "prices.csv"); File.WriteAllText(Path, Csv);
        }
        internal string Path { get; }
        internal FinanceDatasetIntakeStore Store { get; }
        internal FinanceDatasetIntakeStore Restart() => new(_market, _options);
        internal EodhdMarketMemory Memory()
        {
            Assert.False(_market.Enabled);
            Assert.False(_market.AccountActive);
            Assert.True(string.IsNullOrEmpty(_market.ApiToken));
            return new(_market);
        }
        internal SqliteConnection Connection()
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _market.DatabasePath }.ToString());
            connection.Open();
            return connection;
        }
        internal string Scalar(string sql)
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _market.DatabasePath }.ToString()); connection.Open();
            using var command = connection.CreateCommand(); command.CommandText = sql;
            return Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture) ?? "";
        }
        internal void SeedLegacy()
        {
            // Synthetic legacy rows/ID from the accepted invariant blocker fixture, never production evidence.
            Store.Discover(Candidate("legacy") with { CanonicalProduct = null }, "fixture");
            using var artifact = File.OpenRead(Path); var hash = DatasetContentIdentity.Sha256(artifact);
            _ = Scalar("UPDATE dataset_candidates SET state='Promoted',canonical_revision_id='wiki-dde8381ccbe9ccc5',manifest_json='{\"legacy\":true}',artifact_sha256='" + hash + "' WHERE candidate_id='legacy'");
            _ = Scalar("INSERT INTO revisions VALUES('wiki-dde8381ccbe9ccc5','sha256:dde8381ccbe9ccc5c416332ae2d92402f4b90c7e97578e206b36f73d3bd61bfe','2024-01-03T00:00:00Z',2,1)");
            foreach (var row in Rows)
            {
                var sql = FormattableString.Invariant($"INSERT INTO observations VALUES('NASDAQ-WIKI','WIKI/PRICES','dataset-promotion-v1','{row.Symbol}','{row.Symbol}','{row.Symbol}','XNAS','2024-01-02','{row.Open}','{row.High}','{row.Low}','{row.Close}','',{row.Volume},'2024-01-03T00:00:00Z','wiki-dde8381ccbe9ccc5')");
                _ = Scalar(sql);
            }
        }
        internal string[] LegacySnapshot() =>
        [Scalar("SELECT group_concat(quote(provider)||quote(product)||quote(instrument_id)||quote(session_date)||quote(open)||quote(high)||quote(low)||quote(close)||quote(adjusted_close)||quote(volume)||quote(acquired_utc)||quote(revision_id)) FROM observations WHERE revision_id='wiki-dde8381ccbe9ccc5'"),
         Scalar("SELECT quote(revision_id)||quote(checksum)||quote(created_utc)||quote(observation_count) FROM revisions WHERE revision_id='wiki-dde8381ccbe9ccc5'"),
         Scalar("SELECT quote(manifest_json)||quote(canonical_revision_id)||quote(artifact_sha256)||quote(state)||quote(updated_utc) FROM dataset_candidates WHERE candidate_id='legacy'")];
        public void Dispose() => Directory.Delete(_root, true);
    }
}
