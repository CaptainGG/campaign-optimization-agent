namespace CampaignAgent.Core.Models;

public record AdEvent(
    string EventId,
    string CampaignId,
    string CreativeId,
    AdEventType Type,
    DateTimeOffset Timestamp,
    decimal BidPrice,
    string Placement
);

public enum AdEventType
{
    Impression,
    Click,
    Conversion
}

public record CampaignMetrics(
    string CampaignId,
    string CampaignName,
    long Impressions,
    long Clicks,
    long Conversions,
    decimal SpendToDate,
    decimal DailyBudget,
    decimal TargetCtr,
    decimal TargetCpm,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd
)
{
    public decimal Ctr => Impressions > 0 ? (decimal)Clicks / Impressions * 100 : 0;
    public decimal Cpm => Impressions > 0 ? SpendToDate / Impressions * 1000 : 0;
    public decimal ConversionRate => Clicks > 0 ? (decimal)Conversions / Clicks * 100 : 0;
    public decimal BudgetPacingPct => DailyBudget > 0 ? SpendToDate / DailyBudget * 100 : 0;
}

public record AgentRecommendation(
    string CampaignId,
    RecommendedAction Action,
    string Urgency,
    string Reason,
    SuggestedChange? SuggestedChange,
    DateTimeOffset GeneratedAt
);

public record SuggestedChange(
    string Field,
    object From,
    object To
);

public enum RecommendedAction
{
    NoAction,
    PauseCreative,
    ReallocateBudget,
    IncreaseBid,
    DecreaseBid,
    FlagAnomaly
}
