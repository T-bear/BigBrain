namespace BigBrain.Modules.Finance;

public enum TradingMode
{
    Research = 0,
    Backtest,
    Paper,
    ManualApproval,
    LimitedAuto,
    Auto,
    Halted
}

public enum EvaluationResult
{
    Missing = 0,
    Rejected,
    Accepted
}

public sealed record RiskEvaluation(
    EvaluationResult Result,
    string PolicyVersion,
    IReadOnlyList<string> ReasonCodes)
{
    public bool IsAccepted => Result == EvaluationResult.Accepted;
}

public sealed record PolicyEvaluation(
    EvaluationResult Result,
    string PolicyVersion,
    IReadOnlyList<string> ReasonCodes)
{
    public bool IsAllowed => Result == EvaluationResult.Accepted;
}

public enum DecisionAction
{
    NoTrade = 0,
    Rejected,
    PaperBuy,
    PaperSell
}

public sealed record FinanceDecision(
    FinanceId DecisionId,
    FinanceId CorrelationId,
    FinanceId EvaluationId,
    DateTimeOffset DecidedAtUtc,
    TradingMode Mode,
    DecisionAction Action,
    IReadOnlyList<string> ReasonCodes)
{
    public bool CreatesPaperIntent => Action is DecisionAction.PaperBuy or DecisionAction.PaperSell;
}

public sealed record DecisionJournalEntry(
    FinanceId JournalEntryId,
    FinanceId CorrelationId,
    DateTimeOffset TimestampUtc,
    InstrumentId InstrumentId,
    StrategyIdentity Strategy,
    FinanceId ObservationId,
    FinanceId EvaluationId,
    StrategySignal Signal,
    RiskEvaluation Risk,
    PolicyEvaluation Policy,
    FinanceDecision Decision,
    FinanceId? PaperOrderId,
    FinanceId? OutcomeId);

public interface IDecisionJournal
{
    void Append(DecisionJournalEntry entry);
    IReadOnlyList<DecisionJournalEntry> GetEntries();
}

public sealed class InMemoryDecisionJournal : IDecisionJournal
{
    private readonly List<DecisionJournalEntry> _entries = [];

    public void Append(DecisionJournalEntry entry) => _entries.Add(entry);

    public IReadOnlyList<DecisionJournalEntry> GetEntries() => _entries.ToArray();
}
