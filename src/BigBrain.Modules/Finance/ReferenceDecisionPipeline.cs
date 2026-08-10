namespace BigBrain.Modules.Finance;

public sealed class ReferenceDecisionPipeline(IDecisionJournal journal)
{
    public DecisionJournalEntry Evaluate(
        StrategyEvaluation strategy,
        RiskEvaluation risk,
        PolicyEvaluation policy,
        TradingMode mode,
        DateTimeOffset decidedAtUtc)
    {
        FinanceTime.RequireUtc(decidedAtUtc, nameof(decidedAtUtc));
        var action = ResolveAction(strategy.Signal, risk, policy, mode);
        var reasons = strategy.Signal.ReasonCodes
            .Concat(risk.ReasonCodes)
            .Concat(policy.ReasonCodes)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var decision = new FinanceDecision(
            NewId(),
            strategy.CorrelationId,
            strategy.EvaluationId,
            decidedAtUtc,
            mode,
            action,
            reasons);
        var entry = new DecisionJournalEntry(
            NewId(),
            strategy.CorrelationId,
            decidedAtUtc,
            strategy.InstrumentId,
            strategy.Strategy,
            strategy.ObservationId,
            strategy.EvaluationId,
            strategy.Signal,
            risk,
            policy,
            decision,
            null,
            null);
        journal.Append(entry);
        return entry;
    }

    private static DecisionAction ResolveAction(
        StrategySignal signal,
        RiskEvaluation risk,
        PolicyEvaluation policy,
        TradingMode mode)
    {
        if (signal.Direction == SignalDirection.None)
        {
            return DecisionAction.NoTrade;
        }

        if (!risk.IsAccepted || !policy.IsAllowed)
        {
            return DecisionAction.Rejected;
        }

        if (mode != TradingMode.Paper)
        {
            return DecisionAction.NoTrade;
        }

        return signal.Direction == SignalDirection.Buy
            ? DecisionAction.PaperBuy
            : DecisionAction.PaperSell;
    }

    private static FinanceId NewId() => new(Guid.NewGuid());
}
