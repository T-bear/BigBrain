using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceDailyFeatureEngineTests
{
    private static readonly InstrumentId Instrument = new("US:XNAS:TEST");
    private static readonly DateOnly Start = new(2026, 1, 2);

    [Fact]
    public void CoreSetHasVersionedImmutableDefinitionsAndKnownAnswers()
    {
        var build = DeterministicDailyFeatureEngine.Build(Rows(60, index => index + 1m, index => 100m + index));

        Assert.Equal("core-daily-v1", build.FeatureSetId);
        Assert.Equal(21, build.Definitions.Length);
        Assert.All(build.Definitions, definition =>
        {
            Assert.Equal("v1", definition.Version);
            Assert.StartsWith("sha256:", definition.Fingerprint);
            Assert.NotEmpty(definition.RequiredInputs);
            if (!definition.Id.StartsWith("volume.", StringComparison.Ordinal))
                Assert.Contains("raw", definition.PriceBasis, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Equal(5m, Available(build, "return.simple.5", 5));
        Assert.Equal(Math.Round((decimal)Math.Log(2), 12, MidpointRounding.AwayFromZero),
            Available(DeterministicDailyFeatureEngine.Build(Rows(12, index => (decimal)Math.Pow(2, index))), "return.log.1", 11));
        Assert.Equal(58m, Available(build, "sma.5", 59));
        Assert.Equal(4m, Available(build, "ema.5", 5));
        Assert.Equal(5m, Available(build, "momentum.5", 5));

        var doubles = DeterministicDailyFeatureEngine.Build(Rows(21, index => (decimal)Math.Pow(2, index), _ => 100m));
        Assert.Equal(0m, Available(doubles, "volatility.10", 20));
        Assert.Equal(100m, Available(build, "rsi.14", 59));
        Assert.Equal(2m, Available(DeterministicDailyFeatureEngine.Build(Rows(60, index => 100m + index)), "atr.14", 59));
        Assert.Equal(149.5m, Available(build, "volume.sma.20", 59));
        Assert.Equal(Math.Round(159m / 149.5m, 12, MidpointRounding.AwayFromZero), Available(build, "volume.ratio.20", 59));
    }

    [Fact]
    public void WarmupMissingVolumeGapAndOrderingAreExplicit()
    {
        var rows = Rows(25, index => 100m + index).ToArray();
        rows[10] = rows[10] with { HasUnresolvedGap = true };
        rows[22] = rows[22] with { Volume = null };
        var build = DeterministicDailyFeatureEngine.Build(rows);

        Assert.Equal(FeatureValueState.Warmup, Value(build, "sma.20", 5).State);
        Assert.Equal(FeatureValueState.Unavailable, Value(build, "sma.5", 10).State);
        Assert.Equal(FeatureQualityState.GapAffected, Value(build, "sma.5", 10).Quality);
        Assert.Equal(FeatureValueState.Warmup, Value(build, "sma.20", 24).State);
        var missingVolume = DeterministicDailyFeatureEngine.Build(Rows(25, index => 100m + index)
            .Select((value, index) => index == 22 ? value with { Volume = null } : value));
        Assert.Equal(FeatureValueState.Unavailable, Value(missingVolume, "volume.sma.20", 24).State);
        Assert.Equal(FeatureQualityState.InvalidInput, Value(missingVolume, "volume.sma.20", 24).Quality);

        Assert.Throws<ArgumentException>(() => DeterministicDailyFeatureEngine.Build([rows[0], rows[0]]));
        Assert.Throws<ArgumentException>(() => DeterministicDailyFeatureEngine.Build([rows[0] with { Close = 0 }]));
        Assert.Empty(DeterministicDailyFeatureEngine.Build([]).Values);
    }

    [Fact]
    public void FutureHorizonCannotChangeEarlierFeatureKnowledge()
    {
        var first = DeterministicDailyFeatureEngine.Build(Rows(25, index => 100m + index));
        var future = DeterministicDailyFeatureEngine.Build(Rows(40, index => index < 25 ? 100m + index : 10_000m + index));
        var before = first.Values.Where(value => value.SessionDate <= Start.AddDays(24)).ToArray();
        var after = future.Values.Where(value => value.SessionDate <= Start.AddDays(24)).ToArray();

        Assert.Equal(before, after);
        var boundary = new DateTimeOffset(Start.AddDays(25).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        Assert.All(before, value => Assert.True(value.KnowledgeTimeUtc <= boundary));
    }

    [Fact]
    public void RepeatedEvidenceIsDeterministicAndCorrectionCreatesNewRevisionChecksum()
    {
        var rows = Rows(30, index => 100m + index).ToArray();
        var first = DeterministicDailyFeatureEngine.Build(rows);
        var repeated = DeterministicDailyFeatureEngine.Build(rows);
        var corrected = rows.ToArray();
        corrected[15] = corrected[15] with { Close = 999m, High = 1000m, SourceRevisionId = new("market-correction") };
        var changed = DeterministicDailyFeatureEngine.Build(corrected);

        Assert.Equal(first.DeterministicChecksum, repeated.DeterministicChecksum);
        Assert.Equal(first.Values, repeated.Values);
        Assert.NotEqual(first.DeterministicChecksum, changed.DeterministicChecksum);
        Assert.Contains(new DatasetRevisionId("market-original"), first.SourceRevisions);
        Assert.Contains(new DatasetRevisionId("market-correction"), changed.SourceRevisions);
    }

    private static DailyFeatureValue Value(DailyFeatureBuild build, string id, int index) =>
        build.Values.Single(value => value.DefinitionId == id && value.SessionDate == Start.AddDays(index));
    private static decimal Available(DailyFeatureBuild build, string id, int index) => Value(build, id, index).Value!.Value;

    private static IEnumerable<DailyFeatureObservation> Rows(int count, Func<int, decimal> close,
        Func<int, decimal>? volume = null)
    {
        for (var index = 0; index < count; index++)
        {
            var value = close(index); var date = Start.AddDays(index);
            yield return new(Instrument, date, value, value + 1, Math.Max(value - 1, 0.5m), value,
                volume?.Invoke(index) ?? 1_000m,
                new DateTimeOffset(date.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                new DatasetRevisionId("market-original"));
        }
    }
}
