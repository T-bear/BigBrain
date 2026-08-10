namespace BigBrain.Modules.Finance;

public sealed record StrategyIdentity(string Id, string Version)
{
    public StrategyIdentity Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(Version);
        return this;
    }
}

public enum SignalDirection
{
    None,
    Buy,
    Sell
}

public sealed record StrategySignal(
    SignalDirection Direction,
    decimal Score,
    Price? ProposedEntry,
    Price? ProposedStop,
    Price? ProposedTarget,
    IReadOnlyList<string> ReasonCodes)
{
    public StrategySignal Validate()
    {
        if (Score is < -1 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Score), "Signal score must be between -1 and 1.");
        }

        if (Direction == SignalDirection.None && ProposedEntry is not null)
        {
            throw new ArgumentException("A no-trade signal cannot propose an entry.");
        }

        return this;
    }
}

public sealed record StrategyContext(
    FinanceId EvaluationId,
    FinanceId CorrelationId,
    MarketDataObservation Observation,
    IReadOnlyDictionary<string, decimal> Parameters,
    DateTimeOffset EvaluatedAtUtc);

public sealed record StrategyEvaluation(
    FinanceId EvaluationId,
    FinanceId CorrelationId,
    StrategyIdentity Strategy,
    FinanceId ObservationId,
    InstrumentId InstrumentId,
    DateTimeOffset EvaluatedAtUtc,
    StrategySignal Signal);

public interface IFinanceStrategy
{
    StrategyIdentity Identity { get; }
    StrategyEvaluation Evaluate(StrategyContext context);
}
