using System.Globalization;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

internal sealed record FinanceOverviewSignal(string InstrumentId,string Symbol,string Name,string State,
    decimal? SessionChangePercent,int PositiveStrategies,int NeutralStrategies,int NegativeStrategies,int StrategyCount,
    string Agreement,string Freshness,IReadOnlyList<string> PredictionIds);
internal sealed record FinanceOverviewProspective(int Valid,int Pending,int Evaluated,int Invalidated,int Correct,int Incorrect,
    decimal? DirectionalAccuracy,decimal? MeanRealizedReturn,string EvidenceMaturity,IReadOnlyList<FinanceOverviewCurvePoint> Curve);
internal sealed record FinanceOverviewCurvePoint(DateOnly Session,decimal CumulativeReturn);
internal sealed record FinanceOverviewResponse(DateTimeOffset GeneratedAtUtc,string Mode,string Provider,string ObservationClass,
    DateOnly? LatestSession,string Freshness,int Tracked,int Up,int Down,int Unchanged,string MarketSummary,
    IReadOnlyList<FinanceOverviewSignal> Signals,FinanceOverviewProspective Prospective,FinanceCadenceSnapshot Cadence,
    string Disclaimer,string EvidenceSeparation);

internal sealed partial class EodhdMarketMemory
{
    internal FinanceOverviewResponse Overview(EodhdFinanceOptions provider,FinanceCadenceOptions cadence,bool clockIntegrity)
    {
        var snapshot=Snapshot(provider.Enabled,!string.IsNullOrWhiteSpace(provider.ApiToken),provider.AccountActive);
        var latestSession=snapshot.Watchlist.Where(x=>x.ObservedAtUtc.HasValue).Select(x=>(DateOnly?)DateOnly.FromDateTime(x.ObservedAtUtc!.Value.UtcDateTime)).Max();
        var available=snapshot.Watchlist.Where(x=>x.Price.HasValue).ToArray();var up=available.Count(x=>x.DailyChangePercent>0);var down=available.Count(x=>x.DailyChangePercent<0);var unchanged=available.Length-up-down;
        var summary=available.Length==0?"Ingen aktuell marknadsobservation är tillgänglig.":
            $"{up} av {available.Length} bevakade instrument steg under senaste tillgängliga marknadssessionen; {down} föll och {unchanged} var oförändrade.";
        var predictions=ShadowCatalog(null,null,null,null,null,200).Predictions.Where(x=>x.State!=FinanceShadowState.Invalidated).ToArray();
        var latestPredictionSession=predictions.Select(x=>(DateOnly?)x.SessionDate).Max();
        var latest=predictions.Where(x=>x.SessionDate==latestPredictionSession).ToArray();
        var signals=available.Select(item=>
        {
            var values=latest.Where(x=>x.InstrumentId==item.InstrumentId).ToArray();var positive=values.Count(x=>x.Signal=="TargetLong");var negative=values.Count(x=>x.Signal=="TargetFlat");var neutral=values.Length-positive-negative;
            var state=values.Length==0?"INSUFFICIENT":positive>negative?"POSITIVE":negative>positive?"NEGATIVE":"NEUTRAL";
            var agreement=values.Length==0?"No eligible prospective strategy output":$"{Math.Max(positive,Math.Max(negative,neutral))}/{values.Length} strategies agree; positive {positive}, neutral {neutral}, negative {negative}";
            return new FinanceOverviewSignal(item.InstrumentId,item.Symbol,item.DisplayName,state,item.DailyChangePercent,positive,neutral,negative,values.Length,agreement,item.Freshness.ToString(),values.Select(x=>x.PredictionId).Order(StringComparer.Ordinal).ToArray());
        }).ToArray();
        var prospective=ProspectiveOverview();
        return new(DateTimeOffset.UtcNow,"RESEARCH",Provider,"CURRENT EOD / PROSPECTIVE EOD",latestSession,
            snapshot.Watchlist.Any(x=>x.Freshness.ToString()=="Stale")?"STALE":"CURRENT EOD",available.Length,up,down,unchanged,summary,signals,prospective,
            CadenceSnapshot(provider.Enabled,clockIntegrity,cadence.ProviderWindowStartUtcHour,cadence.InternalCheckMinutes),
            "Research results — no money is traded. Signals are not recommendations.",
            "Prospective evidence records prior decisions; historical backtests remain separate and are not included.");
    }

    private FinanceOverviewProspective ProspectiveOverview()
    {
        using var connection=new SqliteConnection(ConnectionString);connection.Open();
        var invalidated=Scalar(connection,"SELECT COUNT(*) FROM shadow_predictions WHERE state='Invalidated'");
        var valid=Scalar(connection,"SELECT COUNT(*) FROM shadow_predictions WHERE state!='Invalidated'");
        var evaluated=Scalar(connection,"SELECT COUNT(*) FROM shadow_outcomes o JOIN shadow_predictions p ON p.prediction_id=o.prediction_id WHERE p.state!='Invalidated'");
        var pending=Math.Max(0,valid-evaluated);var correct=Scalar(connection,"SELECT COUNT(*) FROM shadow_outcomes o JOIN shadow_predictions p ON p.prediction_id=o.prediction_id WHERE p.state!='Invalidated' AND o.direction_result='CORRECT'");
        var incorrect=Scalar(connection,"SELECT COUNT(*) FROM shadow_outcomes o JOIN shadow_predictions p ON p.prediction_id=o.prediction_id WHERE p.state!='Invalidated' AND o.direction_result='INCORRECT'");
        decimal? mean=null;using(var avg=connection.CreateCommand()){avg.CommandText="SELECT AVG(CAST(o.realized_return AS REAL)) FROM shadow_outcomes o JOIN shadow_predictions p ON p.prediction_id=o.prediction_id WHERE p.state!='Invalidated'";var value=avg.ExecuteScalar();if(value is double number)mean=(decimal)number;}
        var curve=new List<FinanceOverviewCurvePoint>();decimal cumulative=0;using(var command=connection.CreateCommand()){command.CommandText="SELECT o.target_session,AVG(CAST(o.realized_return AS REAL)) FROM shadow_outcomes o JOIN shadow_predictions p ON p.prediction_id=o.prediction_id WHERE p.state!='Invalidated' GROUP BY o.target_session ORDER BY o.target_session";using var reader=command.ExecuteReader();while(reader.Read()){var value=(decimal)reader.GetDouble(1);cumulative=(1+cumulative)*(1+value)-1;curve.Add(new(DateOnly.Parse(reader.GetString(0),CultureInfo.InvariantCulture),cumulative));}}
        var scored=correct+incorrect;var maturity=evaluated<20?"BOOTSTRAPPING":evaluated<100?"EARLY":evaluated<250?"DEVELOPING":"SUFFICIENT_FOR_REVIEW";
        return new(valid,pending,evaluated,invalidated,correct,incorrect,scored==0?null:(decimal)correct/scored,mean,maturity,curve.Count<2?[]:curve);
    }
}
