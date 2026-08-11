using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BigBrain.Modules.Finance;

public enum DailyFeatureKind
{
    Unknown = 0, SimpleReturn, LogReturn, SimpleMovingAverage, ExponentialMovingAverage,
    Momentum, RollingVolatility, RelativeStrengthIndex, AverageTrueRange,
    RollingAverageVolume, VolumeRatio
}
public enum FeatureValueState { Unknown = 0, Available, Warmup, Unavailable }
public enum FeatureQualityState { Unknown = 0, Good, GapAffected, InvalidInput }
public enum FeatureOutputType { Unknown = 0, Numeric }

public sealed record DailyFeatureDefinition(
    string Id, string Name, string Version, DailyFeatureKind Kind, int Period,
    IReadOnlyList<string> RequiredInputs, int RequiredLookback, string WarmupBehavior,
    FeatureOutputType OutputType, string MissingDataBehavior, string GapBehavior,
    string CalculationMethod, string PriceBasis, string Fingerprint);

public sealed record DailyFeatureObservation(
    InstrumentId InstrumentId, DateOnly SessionDate, decimal Open, decimal High, decimal Low,
    decimal Close, decimal? Volume, DateTimeOffset KnowledgeTimeUtc,
    DatasetRevisionId SourceRevisionId, bool HasUnresolvedGap = false);

public sealed record DailyFeatureValue(
    InstrumentId InstrumentId, string DefinitionId, string DefinitionVersion,
    string DefinitionFingerprint, DateOnly SessionDate, decimal? Value,
    DatasetRevisionId SourceRevisionId, DateOnly SourceFrom, DateOnly SourceTo,
    DateTimeOffset KnowledgeTimeUtc, FeatureValueState State, FeatureQualityState Quality,
    string EngineVersion);

public sealed record DailyFeatureBuild(
    string FeatureSetId, string FeatureSetFingerprint, string EngineVersion,
    ImmutableArray<DatasetRevisionId> SourceRevisions,
    ImmutableArray<DailyFeatureDefinition> Definitions,
    ImmutableArray<DailyFeatureValue> Values,
    DateOnly? CoverageFrom, DateOnly? CoverageTo, string DeterministicChecksum);

public sealed record FinanceFeatureRevisionSummary(
    string RevisionId, string FeatureSetId, string FeatureSetFingerprint, string EngineVersion,
    IReadOnlyList<string> SourceMarketRevisions, DateOnly? CoverageFrom, DateOnly? CoverageTo,
    int ValueCount, int AvailableCount, int WarmupCount, int QualityIssueCount,
    string Checksum, DateTimeOffset CreatedAtUtc, long BuildElapsedMilliseconds,
    string PriceBasis, string Persistence);

public sealed record FinanceFeatureLatestValue(
    string DefinitionId, string Name, int Period, decimal? Value, DateOnly? SessionDate,
    FeatureValueState State, FeatureQualityState Quality, DateTimeOffset? KnowledgeTimeUtc);

public sealed record FinanceFeatureHistoryPoint(
    DateOnly SessionDate, decimal? Value, FeatureValueState State, FeatureQualityState Quality,
    DateTimeOffset KnowledgeTimeUtc);

public sealed record FinanceFeatureSnapshot(
    DateTimeOffset GeneratedAtUtc, string OperatingMode, string FeatureSetId, string InstrumentId,
    IReadOnlyList<DailyFeatureDefinition> Definitions, FinanceFeatureRevisionSummary? Revision,
    IReadOnlyList<FinanceFeatureLatestValue> Latest,
    string HistoryDefinitionId, IReadOnlyList<FinanceFeatureHistoryPoint> History);

public interface IFinanceFeatureReader
{
    FinanceFeatureSnapshot GetSnapshot(string? instrumentId, string? featureId,
        DateOnly? from, DateOnly? toDate, DateTimeOffset? knowledgeAsOfUtc, int limit);
}

public static class CoreDailyFeatureSet
{
    public const string Id = "core-daily-v1";
    public const string EngineVersion = "daily-feature-engine-v1";
    public const int DecimalPlaces = 12;

    public static readonly ImmutableArray<DailyFeatureDefinition> Definitions =
    [
        Definition("return.simple.1", "Simple 1-period return", DailyFeatureKind.SimpleReturn, 1, ["rawClose"], 1, "close[t]/close[t-1]-1"),
        Definition("return.log.1", "Log 1-period return", DailyFeatureKind.LogReturn, 1, ["rawClose"], 1, "ln(close[t]/close[t-1])"),
        Definition("return.simple.5", "Simple 5-period return", DailyFeatureKind.SimpleReturn, 5, ["rawClose"], 5, "close[t]/close[t-5]-1"),
        Definition("return.simple.20", "Simple 20-period return", DailyFeatureKind.SimpleReturn, 20, ["rawClose"], 20, "close[t]/close[t-20]-1"),
        Definition("sma.5", "SMA 5", DailyFeatureKind.SimpleMovingAverage, 5, ["rawClose"], 4, "arithmetic mean of 5 closes"),
        Definition("sma.10", "SMA 10", DailyFeatureKind.SimpleMovingAverage, 10, ["rawClose"], 9, "arithmetic mean of 10 closes"),
        Definition("sma.20", "SMA 20", DailyFeatureKind.SimpleMovingAverage, 20, ["rawClose"], 19, "arithmetic mean of 20 closes"),
        Definition("sma.50", "SMA 50", DailyFeatureKind.SimpleMovingAverage, 50, ["rawClose"], 49, "arithmetic mean of 50 closes"),
        Definition("ema.5", "EMA 5", DailyFeatureKind.ExponentialMovingAverage, 5, ["rawClose"], 4, "SMA seed then alpha=2/(period+1)"),
        Definition("ema.10", "EMA 10", DailyFeatureKind.ExponentialMovingAverage, 10, ["rawClose"], 9, "SMA seed then alpha=2/(period+1)"),
        Definition("ema.20", "EMA 20", DailyFeatureKind.ExponentialMovingAverage, 20, ["rawClose"], 19, "SMA seed then alpha=2/(period+1)"),
        Definition("ema.50", "EMA 50", DailyFeatureKind.ExponentialMovingAverage, 50, ["rawClose"], 49, "SMA seed then alpha=2/(period+1)"),
        Definition("momentum.5", "Momentum 5", DailyFeatureKind.Momentum, 5, ["rawClose"], 5, "close[t]-close[t-5]"),
        Definition("momentum.10", "Momentum 10", DailyFeatureKind.Momentum, 10, ["rawClose"], 10, "close[t]-close[t-10]"),
        Definition("momentum.20", "Momentum 20", DailyFeatureKind.Momentum, 20, ["rawClose"], 20, "close[t]-close[t-20]"),
        Definition("volatility.10", "Rolling volatility 10", DailyFeatureKind.RollingVolatility, 10, ["rawClose"], 10, "population standard deviation of 10 simple returns"),
        Definition("volatility.20", "Rolling volatility 20", DailyFeatureKind.RollingVolatility, 20, ["rawClose"], 20, "population standard deviation of 20 simple returns"),
        Definition("rsi.14", "RSI 14", DailyFeatureKind.RelativeStrengthIndex, 14, ["rawClose"], 14, "Wilder RSI; arithmetic seed then recursive smoothing"),
        Definition("atr.14", "ATR 14", DailyFeatureKind.AverageTrueRange, 14, ["rawOpen", "rawHigh", "rawLow", "rawClose"], 13, "Wilder ATR; arithmetic TR seed then recursive smoothing"),
        Definition("volume.sma.20", "Average volume 20", DailyFeatureKind.RollingAverageVolume, 20, ["volume"], 19, "arithmetic mean of 20 volume observations", "provider volume classification"),
        Definition("volume.ratio.20", "Volume ratio 20", DailyFeatureKind.VolumeRatio, 20, ["volume"], 19, "volume[t]/averageVolume20[t]", "provider volume classification")
    ];

    public static readonly string Fingerprint = Hash(string.Join('\n', Definitions.Select(value => value.Fingerprint)));

    private static DailyFeatureDefinition Definition(string id, string name, DailyFeatureKind kind, int period,
        IReadOnlyList<string> inputs, int lookback, string method, string priceBasis = "raw close/OHLC; never adjusted")
    {
        var canonical = $"{id}|v1|{kind}|{period}|{string.Join(',', inputs)}|{lookback}|{method}|{priceBasis}|round:{DecimalPlaces}";
        return new(id, name, "v1", kind, period, inputs, lookback,
            "Unavailable as warmup until the complete declared lookback exists; never backfilled or zero-filled.",
            FeatureOutputType.Numeric,
            "Missing/invalid required input produces unavailable; zero prior price invalidates returns.",
            "Explicit unresolved gaps reset recursive/rolling state; values remain unavailable until warmup completes.",
            method, priceBasis, Hash(canonical));
    }

    internal static string Hash(string value) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()}";
}

public sealed class DeterministicDailyFeatureEngine
{
    public static DailyFeatureBuild Build(IEnumerable<DailyFeatureObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        var input = observations.ToArray();
        Validate(input);
        var sourceRevisions = input.Select(value => value.SourceRevisionId).Distinct()
            .OrderBy(value => value.Value, StringComparer.Ordinal).ToImmutableArray();
        var values = input.GroupBy(value => value.InstrumentId)
            .OrderBy(group => group.Key.Value, StringComparer.Ordinal)
            .SelectMany(group => CalculateInstrument(group.OrderBy(value => value.SessionDate).ToArray()))
            .OrderBy(value => value.InstrumentId.Value, StringComparer.Ordinal)
            .ThenBy(value => value.SessionDate)
            .ThenBy(value => value.DefinitionId, StringComparer.Ordinal)
            .ToImmutableArray();
        var checksum = Checksum(sourceRevisions, values);
        return new(CoreDailyFeatureSet.Id, CoreDailyFeatureSet.Fingerprint, CoreDailyFeatureSet.EngineVersion,
            sourceRevisions, CoreDailyFeatureSet.Definitions, values,
            input.Length == 0 ? null : input.Min(value => value.SessionDate),
            input.Length == 0 ? null : input.Max(value => value.SessionDate), checksum);
    }

    private static IEnumerable<DailyFeatureValue> CalculateInstrument(DailyFeatureObservation[] rows)
    {
        var ema = new Dictionary<int, decimal>();
        decimal? rsiGain = null, rsiLoss = null, atr = null;
        var segmentStart = 0;
        for (var index = 0; index < rows.Length; index++)
        {
            if (rows[index].HasUnresolvedGap)
            {
                segmentStart = index; ema.Clear(); rsiGain = null; rsiLoss = null; atr = null;
            }
            var localCount = index - segmentStart + 1;
            foreach (var definition in CoreDailyFeatureSet.Definitions)
            {
                var result = Calculate(definition, rows, index, segmentStart, localCount, ema, ref rsiGain, ref rsiLoss, ref atr);
                var sourceFrom = rows[result.SourceIndex].SessionDate;
                var knowledge = rows[result.SourceIndex..(index + 1)].Max(value => value.KnowledgeTimeUtc);
                yield return new(rows[index].InstrumentId, definition.Id, definition.Version,
                    definition.Fingerprint, rows[index].SessionDate, result.Value,
                    rows[index].SourceRevisionId, sourceFrom, rows[index].SessionDate, knowledge,
                    rows[index].HasUnresolvedGap ? FeatureValueState.Unavailable : result.State,
                    rows[index].HasUnresolvedGap ? FeatureQualityState.GapAffected : result.Quality,
                    CoreDailyFeatureSet.EngineVersion);
            }
        }
    }

    private static Calculation Calculate(DailyFeatureDefinition definition, DailyFeatureObservation[] rows,
        int index, int segmentStart, int localCount, Dictionary<int, decimal> ema,
        ref decimal? rsiGain, ref decimal? rsiLoss, ref decimal? atr)
    {
        var period = definition.Period;
        switch (definition.Kind)
        {
            case DailyFeatureKind.SimpleReturn:
                if (localCount <= period) return Warmup(segmentStart);
                if (rows[index - period].Close == 0) return Invalid(index - period);
                return Available(rows[index].Close / rows[index - period].Close - 1m, index - period);
            case DailyFeatureKind.LogReturn:
                if (localCount <= period) return Warmup(segmentStart);
                if (rows[index - period].Close <= 0 || rows[index].Close <= 0) return Invalid(index - period);
                return Available((decimal)Math.Log((double)(rows[index].Close / rows[index - period].Close)), index - period);
            case DailyFeatureKind.SimpleMovingAverage:
                if (localCount < period) return Warmup(segmentStart);
                return Available(rows[(index - period + 1)..(index + 1)].Average(value => value.Close), index - period + 1);
            case DailyFeatureKind.ExponentialMovingAverage:
                if (localCount < period) return Warmup(segmentStart);
                if (localCount == period) ema[period] = rows[segmentStart..(index + 1)].Average(value => value.Close);
                else ema[period] = rows[index].Close * (2m / (period + 1m)) + ema[period] * (1m - 2m / (period + 1m));
                return Available(ema[period], segmentStart);
            case DailyFeatureKind.Momentum:
                if (localCount <= period) return Warmup(segmentStart);
                return Available(rows[index].Close - rows[index - period].Close, index - period);
            case DailyFeatureKind.RollingVolatility:
                if (localCount <= period) return Warmup(segmentStart);
                var returns = Enumerable.Range(index - period + 1, period)
                    .Select(position => rows[position - 1].Close == 0 ? (decimal?)null : rows[position].Close / rows[position - 1].Close - 1m).ToArray();
                if (returns.Any(value => value is null)) return Invalid(index - period);
                var mean = returns.Average(value => value!.Value);
                var variance = returns.Average(value => (value!.Value - mean) * (value.Value - mean));
                return Available((decimal)Math.Sqrt((double)variance), index - period);
            case DailyFeatureKind.RelativeStrengthIndex:
                if (localCount <= period) return Warmup(segmentStart);
                var change = rows[index].Close - rows[index - 1].Close;
                if (localCount == period + 1)
                {
                    var changes = Enumerable.Range(segmentStart + 1, period).Select(position => rows[position].Close - rows[position - 1].Close).ToArray();
                    rsiGain = changes.Average(value => Math.Max(value, 0));
                    rsiLoss = changes.Average(value => Math.Max(-value, 0));
                }
                else
                {
                    rsiGain = ((rsiGain ?? 0) * (period - 1) + Math.Max(change, 0)) / period;
                    rsiLoss = ((rsiLoss ?? 0) * (period - 1) + Math.Max(-change, 0)) / period;
                }
                var rsi = rsiLoss == 0 ? 100m : 100m - 100m / (1m + rsiGain!.Value / rsiLoss.Value);
                return Available(rsi, segmentStart);
            case DailyFeatureKind.AverageTrueRange:
                var trueRange = TrueRange(rows, index, segmentStart);
                if (localCount < period) return Warmup(segmentStart);
                if (localCount == period)
                    atr = Enumerable.Range(segmentStart, period).Average(position => TrueRange(rows, position, segmentStart));
                else atr = ((atr ?? 0) * (period - 1) + trueRange) / period;
                return Available(atr.Value, segmentStart);
            case DailyFeatureKind.RollingAverageVolume:
            case DailyFeatureKind.VolumeRatio:
                if (localCount < period) return Warmup(segmentStart);
                var volumes = rows[(index - period + 1)..(index + 1)].Select(value => value.Volume).ToArray();
                if (volumes.Any(value => value is null)) return Invalid(index - period + 1);
                var average = volumes.Average(value => value!.Value);
                if (definition.Kind == DailyFeatureKind.VolumeRatio && average == 0) return Invalid(index - period + 1);
                return Available(definition.Kind == DailyFeatureKind.VolumeRatio ? rows[index].Volume!.Value / average : average, index - period + 1);
            default: throw new ArgumentOutOfRangeException(nameof(definition));
        }
    }

    private static decimal TrueRange(DailyFeatureObservation[] rows, int index, int segmentStart)
    {
        if (index == segmentStart) return rows[index].High - rows[index].Low;
        var previous = rows[index - 1].Close;
        return Math.Max(rows[index].High - rows[index].Low,
            Math.Max(Math.Abs(rows[index].High - previous), Math.Abs(rows[index].Low - previous)));
    }

    private static Calculation Available(decimal value, int sourceIndex) =>
        new(Round(value), FeatureValueState.Available, FeatureQualityState.Good, sourceIndex);
    private static Calculation Warmup(int sourceIndex) => new(null, FeatureValueState.Warmup, FeatureQualityState.Good, sourceIndex);
    private static Calculation Invalid(int sourceIndex) => new(null, FeatureValueState.Unavailable, FeatureQualityState.InvalidInput, sourceIndex);
    private static decimal Round(decimal value) => Math.Round(value, CoreDailyFeatureSet.DecimalPlaces, MidpointRounding.AwayFromZero);

    private static void Validate(DailyFeatureObservation[] input)
    {
        foreach (var value in input)
        {
            if (value.Open <= 0 || value.High <= 0 || value.Low <= 0 || value.Close <= 0 || value.High < value.Open ||
                value.High < value.Close || value.Low > value.Open || value.Low > value.Close || value.Low > value.High)
                throw new ArgumentException("Feature observations require valid positive raw OHLC.", nameof(input));
            if (value.Volume < 0) throw new ArgumentException("Feature observation volume cannot be negative.", nameof(input));
            if (value.KnowledgeTimeUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Feature knowledge time must be UTC.", nameof(input));
            if (value.KnowledgeTimeUtc < new DateTimeOffset(value.SessionDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero))
                throw new ArgumentException("Feature input cannot be known before its market session.", nameof(input));
        }
        if (input.GroupBy(value => new { value.InstrumentId, value.SessionDate }).Any(group => group.Count() != 1))
            throw new ArgumentException("Feature inputs must be unique per instrument/session.", nameof(input));
    }

    private static string Checksum(IEnumerable<DatasetRevisionId> revisions, IEnumerable<DailyFeatureValue> values)
    {
        var builder = new StringBuilder().Append(CoreDailyFeatureSet.Id).Append('|').Append(CoreDailyFeatureSet.Fingerprint).Append('\n');
        foreach (var revision in revisions) builder.Append("source|").Append(revision.Value).Append('\n');
        foreach (var value in values)
            builder.Append(value.InstrumentId.Value).Append('|').Append(value.SessionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('|')
                .Append(value.DefinitionId).Append('|').Append(value.Value?.ToString(CultureInfo.InvariantCulture) ?? "null").Append('|')
                .Append(value.State).Append('|').Append(value.Quality).Append('|').Append(value.SourceFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('|')
                .Append(value.SourceTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('|').Append(value.KnowledgeTimeUtc.ToString("O", CultureInfo.InvariantCulture)).Append('\n');
        return CoreDailyFeatureSet.Hash(builder.ToString());
    }

    private sealed record Calculation(decimal? Value, FeatureValueState State, FeatureQualityState Quality, int SourceIndex);
}
