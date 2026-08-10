namespace BigBrain.Modules.Finance;

public enum PaperOrderSide
{
    Buy,
    Sell
}

public sealed record PaperOrder(
    FinanceId PaperOrderId,
    FinanceId DecisionId,
    FinanceId CorrelationId,
    InstrumentId InstrumentId,
    PaperOrderSide Side,
    Quantity Quantity,
    Price RequestedPrice,
    DateTimeOffset SubmittedAtUtc);

public sealed record PaperFill(
    FinanceId FillId,
    FinanceId PaperOrderId,
    Quantity Quantity,
    Price Price,
    Money Fee,
    DateTimeOffset FilledAtUtc);

public sealed record Position(InstrumentId InstrumentId, Quantity Quantity, Money CostBasis);

public sealed record TradeResult(
    FinanceId OutcomeId,
    FinanceId CorrelationId,
    Money GrossProfitLoss,
    Money Fees,
    Money NetProfitLoss,
    DateTimeOffset ClosedAtUtc);
