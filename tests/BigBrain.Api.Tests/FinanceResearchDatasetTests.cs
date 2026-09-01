using System.IO.Compression;
using System.Text;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using ClosedXML.Excel;

namespace BigBrain.Api.Tests;

public sealed class FinanceResearchDatasetTests
{
    [Fact]
    public void EligibilitySeparatesOwnerApprovalExternalRightsAndSemanticPurpose()
    {
        var facts = new ResearchDatasetFacts(ResearchDatasetClass.DailyOhlcv,
            DatasetOwnerRightsDecision.ApprovedByOwner, "owner-evidence-v1", DatasetEvidenceResult.Unknown,
            DatasetEvidenceResult.Pass, DatasetEvidenceResult.Unknown, DatasetEvidenceResult.Unknown,
            DatasetEvidenceResult.Unknown, ["survivorship.unknown"]);
        var result = ResearchDatasetEligibilityPolicyV1.Evaluate(facts);

        Assert.Equal(ResearchEligibilityState.EligibleWithLimitations,
            result.For(ResearchDatasetPurpose.BoundedHistoricalBacktest).State);
        Assert.Contains("externalRights.unknownOwnerAcceptedRisk",
            result.For(ResearchDatasetPurpose.BoundedHistoricalBacktest).ReasonCodes);
        Assert.Equal(ResearchEligibilityState.Ineligible,
            result.For(ResearchDatasetPurpose.LongHorizonPerformance).State);

        var denied = ResearchDatasetEligibilityPolicyV1.Evaluate(facts with { ExternalRights = DatasetEvidenceResult.Fail });
        Assert.All(denied.Capabilities, x => Assert.Equal(ResearchEligibilityState.Ineligible, x.State));
    }

    [Fact]
    public void CloseOnlyAndCurrentSnapshotCannotEnterOhlcvHistoricalExperiments()
    {
        ResearchDatasetFacts Facts(ResearchDatasetClass type) => new(type,
            DatasetOwnerRightsDecision.ApprovedByOwner, "owner-evidence-v1", DatasetEvidenceResult.Unknown,
            DatasetEvidenceResult.Pass, DatasetEvidenceResult.Unknown, DatasetEvidenceResult.Unknown,
            DatasetEvidenceResult.Unknown, []);
        Assert.Equal(ResearchEligibilityState.Ineligible,
            ResearchDatasetEligibilityPolicyV1.Evaluate(Facts(ResearchDatasetClass.DailyCloseOnlyMarketContext))
                .For(ResearchDatasetPurpose.BoundedHistoricalBacktest).State);
        Assert.Equal(ResearchEligibilityState.Ineligible,
            ResearchDatasetEligibilityPolicyV1.Evaluate(Facts(ResearchDatasetClass.CurrentSnapshotMetadata))
                .For(ResearchDatasetPurpose.TrainValidationHoldout).State);
    }

    [Fact]
    public void WorkbookCreatesIndependentResearchRevisionsWithoutCanonicalPromotion()
    {
        using var fixture = new Fixture();
        fixture.WriteReadyPackage("owner.xlsx-package.zip", includeAnomalies: true, manifestCountMismatch: true);
        var result = Assert.Single(fixture.Scanner.ScanOnce());

        Assert.Equal("ManualReviewRequired", result.Status);
        Assert.NotNull(result.WorkbookInspection);
        var catalog = fixture.Store.ResearchCatalog();
        Assert.Equal(2, catalog.Datasets.Count);
        var equity = Assert.Single(catalog.Datasets, x => x.Symbol == "GOOG");
        Assert.Equal(ResearchDatasetClass.DailyOhlcv.ToString(), equity.SchemaClass);
        Assert.Equal("ApprovedByOwner", equity.OwnerRightsDecision);
        Assert.Equal("Unknown", equity.ExternalRights);
        Assert.False(equity.CanonicalPromoted);
        Assert.Contains(equity.Limitations, x => x.StartsWith("ManifestObservationMismatch", StringComparison.Ordinal));
        Assert.Single(catalog.Datasets, x => x.Symbol == "VIX");
        Assert.Equal(0, fixture.Count("observations", "provider='OWNER-DROP-UNKNOWN'"));
    }

    [Fact]
    public void WorkbookReportsOhlcAnomaliesAndBacktestRetainsResearchLineage()
    {
        using var fixture = new Fixture(); fixture.WriteReadyPackage("research.zip", includeAnomalies: true);
        _ = fixture.Scanner.ScanOnce();
        var item = Assert.Single(fixture.Store.ResearchCatalog().Datasets, x => x.Symbol == "GOOG");
        Assert.Equal(1, item.InconsistentOhlc);
        Assert.Equal("Unknown", item.TechnicalQuality);

        var result = fixture.Store.RunBoundedResearchBacktest(item.RevisionId);
        Assert.NotNull(result.Configuration.ResearchDatasetLineage);
        Assert.Equal(item.RevisionId, result.Configuration.ResearchDatasetLineage!.DatasetRevisionId);
        Assert.Equal(ResearchEligibilityState.EligibleWithLimitations, result.Configuration.ResearchDatasetLineage.Eligibility);
        Assert.Equal("RESEARCH", result.Status);
        Assert.Equal(1, fixture.Count("backtest_runs", "run_id='" + result.RunId + "'"));
        Assert.Equal(0, fixture.Count("observations", "revision_id='" + item.RevisionId + "'"));
    }

    [Fact]
    public void CloseOnlyDatasetIsRejectedByExistingOhlcvBacktestPath()
    {
        using var fixture = new Fixture(); fixture.WriteReadyPackage("close.zip"); _ = fixture.Scanner.ScanOnce();
        var close = Assert.Single(fixture.Store.ResearchCatalog().Datasets, x => x.Symbol == "VIX");
        Assert.Throws<InvalidOperationException>(() => fixture.Store.RunBoundedResearchBacktest(close.RevisionId));
    }

    [Theory]
    [InlineData("xl/vbaProject.bin")]
    [InlineData("xl/externalLinks/externalLink1.xml")]
    public void WorkbookUnsafeFeaturesFailClosed(string unsafeEntry)
    {
        using var fixture = new Fixture(); fixture.WriteReadyPackage("unsafe.zip", unsafeEntry: unsafeEntry);
        var result = Assert.Single(fixture.Scanner.ScanOnce());
        Assert.Equal("Rejected", result.Status);
        Assert.Empty(fixture.Store.ResearchCatalog().Datasets);
        Assert.Equal(0, fixture.Count("observations"));
    }

    [Fact]
    public void WorkbookIntakeIsIdempotentAndChangedBytesCreateDifferentCandidate()
    {
        using var fixture = new Fixture(); fixture.WriteReadyPackage("first.zip");
        var first = Assert.Single(fixture.Scanner.ScanOnce());
        var repeated = Assert.Single(fixture.Scanner.ScanOnce());
        Assert.Equal(first.CandidateId, repeated.CandidateId);
        Assert.Equal(2, fixture.Store.ResearchCatalog().Datasets.Count);

        fixture.WriteReadyPackage("changed.zip", changedNote: "different owner evidence bytes");
        var results = fixture.Scanner.ScanOnce();
        Assert.Equal(2, results.Select(x => x.CandidateId).Distinct().Count());
        Assert.Equal(4, fixture.Store.ResearchCatalog().Datasets.Count);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "bb-research-dataset-" + Guid.NewGuid().ToString("N"));
        internal Fixture()
        {
            Market = new() { DatabasePath = Path.Combine(_root, "finance.db"), PayloadDirectory = Path.Combine(_root, "payloads") };
            Options = new() { QuarantineDirectory = Path.Combine(_root, "quarantine"), OwnerDropDirectory = Path.Combine(_root, "drop"), MinimumFreeBytesAfterDownload = 0 };
            _ = new EodhdMarketMemory(Market); Store = new(Market, Options); Scanner = new(Options, Store);
        }
        internal EodhdFinanceOptions Market { get; }
        internal FinanceDatasetOptions Options { get; }
        internal FinanceDatasetIntakeStore Store { get; }
        internal FinanceOwnerDatasetDropScanner Scanner { get; }

        internal void WriteReadyPackage(string name, bool includeAnomalies = false, bool manifestCountMismatch = false,
            string? unsafeEntry = null, string changedNote = "")
        {
            Directory.CreateDirectory(Options.OwnerDropDirectory);
            var workbookPath = Path.Combine(_root, Guid.NewGuid().ToString("N") + ".xlsx");
            using (var workbook = new XLWorkbook())
            {
                var goog = workbook.AddWorksheet("GOOG");
                goog.Cell(1, 1).Value = "Date"; goog.Cell(1, 2).Value = "Open"; goog.Cell(1, 3).Value = "High";
                goog.Cell(1, 4).Value = "Low"; goog.Cell(1, 5).Value = "Close"; goog.Cell(1, 6).Value = "Volume";
                for (var i = 0; i < 120; i++)
                {
                    var row = i + 2; var open = 100m + i / 10m;
                    goog.Cell(row, 1).Value = new DateTime(2026, 1, 1).AddDays(i); goog.Cell(row, 2).Value = open;
                    goog.Cell(row, 3).Value = open + 2m; goog.Cell(row, 4).Value = open - 1m;
                    goog.Cell(row, 5).Value = includeAnomalies && i == 1 ? open - 2m : open + 1m;
                    goog.Cell(row, 6).Value = 1000L + i;
                }
                var vix = workbook.AddWorksheet("VIX"); vix.Cell(1, 1).Value = "Date"; vix.Cell(1, 2).Value = "Close";
                for (var i = 0; i < 4; i++) { vix.Cell(i + 2, 1).Value = new DateTime(2026, 8, 25 + i); vix.Cell(i + 2, 2).Value = 15m + i; }
                var current = workbook.AddWorksheet("CURRENT_METADATA"); current.Cell(1, 1).Value = "Symbol"; current.Cell(1, 2).Value = "PE"; current.Cell(2, 1).Value = "GOOG"; current.Cell(2, 2).Value = 20m;
                var manifest = workbook.AddWorksheet("EXPORT_MANIFEST");
                var headers = new[] { "dataset_id", "sheet", "provider", "instrument_type", "symbol", "exchange", "data_type", "price_basis", "owner_use_decision", "external_rights_verified", "observations", "notes" };
                for (var i = 0; i < headers.Length; i++) manifest.Cell(1, i + 1).Value = headers[i];
                string[][] claims =
                [
                    ["googlefinance-nasdaq-goog-test", "GOOG", "Google Finance / GOOGLEFINANCE", "EQUITY", "GOOG", "NASDAQ", "OHLCV", "RAW", "APPROVED_BY_OWNER", "UNKNOWN", manifestCountMismatch ? "99" : "120", changedNote],
                    ["googlefinance-vix-test", "VIX", "Google Finance / GOOGLEFINANCE", "INDEX", "VIX", "INDEXCBOE", "CLOSE_ONLY", "N/A", "APPROVED_BY_OWNER", "UNKNOWN", "4", "context"]
                ];
                for (var row = 0; row < claims.Length; row++) for (var column = 0; column < claims[row].Length; column++)
                    manifest.Cell(row + 2, column + 1).Value = claims[row][column];
                workbook.SaveAs(workbookPath);
            }
            if (unsafeEntry is not null)
            {
                using var archive = ZipFile.Open(workbookPath, ZipArchiveMode.Update);
                using var writer = new StreamWriter(archive.CreateEntry(unsafeEntry).Open(), Encoding.UTF8); writer.Write("unsafe-feature-fixture");
            }
            var package = Path.Combine(Options.OwnerDropDirectory, name);
            using (var archive = ZipFile.Open(package, ZipArchiveMode.Create))
                archive.CreateEntryFromFile(workbookPath, "package/owner.xlsx");
            File.WriteAllText(package + ".ready", "");
        }

        internal long Count(string table, string? where = null)
        {
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Market.DatabasePath}"); connection.Open();
            using var command = connection.CreateCommand(); command.CommandText = $"SELECT COUNT(*) FROM {table}" + (where is null ? "" : " WHERE " + where);
            return (long)command.ExecuteScalar()!;
        }
        public void Dispose() { try { Directory.Delete(_root, true); } catch (IOException) { } }
    }
}
