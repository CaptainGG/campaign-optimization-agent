using CampaignAgent.Core.Models;

namespace CampaignAgent.Core.Agent;

public interface IMetricsAggregator
{
    CampaignMetrics Aggregate(Campaign campaign, IReadOnlyList<AdEvent> events);
}

public sealed class MetricsAggregator : IMetricsAggregator
{
    public CampaignMetrics Aggregate(Campaign campaign, IReadOnlyList<AdEvent> events)
    {
        var impressions = events.Count(e => e.Type == AdEventType.Impression);
        var clicks = events.Count(e => e.Type == AdEventType.Click);
        var conversions = events.Count(e => e.Type == AdEventType.Conversion);
        var spend = events
            .Where(e => e.Type == AdEventType.Impression)
            .Sum(e => e.BidPrice / 1000); // CPM → per impression

        return new CampaignMetrics(
            CampaignId: campaign.Id,
            CampaignName: campaign.Name,
            Impressions: impressions,
            Clicks: clicks,
            Conversions: conversions,
            SpendToDate: spend,
            DailyBudget: campaign.DailyBudget,
            TargetCtr: campaign.TargetCtr,
            TargetCpm: campaign.TargetCpm,
            PeriodStart: campaign.PeriodStart,
            PeriodEnd: campaign.PeriodEnd
        );
    }
}

// ---------- Campaign domain model & repository ----------

public record Campaign(
    string Id,
    string Name,
    decimal DailyBudget,
    decimal TargetCtr,
    decimal TargetCpm,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd
);

public interface ICampaignRepository
{
    Task<Campaign> GetAsync(string campaignId, CancellationToken ct = default);
    Task<IReadOnlyList<Campaign>> GetAllAsync(CancellationToken ct = default);
}

/// <summary>
/// Seeded in-memory campaign catalog. In production, this would be backed
/// by the platform database (campaign configs, targeting, creatives).
/// </summary>
public sealed class InMemoryCampaignRepository : ICampaignRepository
{
    private static readonly Dictionary<string, Campaign> _campaigns = new()
    {
        ["camp-001"] = new Campaign(
            Id: "camp-001",
            Name: "Summer Brand Launch",
            DailyBudget: 1200m,
            TargetCtr: 1.5m,
            TargetCpm: 8.50m,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-7),
            PeriodEnd: DateTimeOffset.UtcNow.AddDays(14)
        ),
        ["camp-002"] = new Campaign(
            Id: "camp-002",
            Name: "EV Awareness Drive",
            DailyBudget: 2500m,
            TargetCtr: 0.8m,
            TargetCpm: 12.00m,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-3),
            PeriodEnd: DateTimeOffset.UtcNow.AddDays(28)
        ),
        ["camp-003"] = new Campaign(
            Id: "camp-003",
            Name: "Back to School Promo",
            DailyBudget: 500m,
            TargetCtr: 1.2m,
            TargetCpm: 6.00m,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd: DateTimeOffset.UtcNow.AddDays(10)
        ),
    };

    public Task<Campaign> GetAsync(string campaignId, CancellationToken ct = default)
    {
        if (_campaigns.TryGetValue(campaignId, out var campaign))
            return Task.FromResult(campaign);
        throw new KeyNotFoundException($"Campaign '{campaignId}' not found.");
    }

    public Task<IReadOnlyList<Campaign>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Campaign>>(_campaigns.Values.ToList());
}
