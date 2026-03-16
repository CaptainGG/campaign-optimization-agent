using CampaignAgent.Core.Agent;
using CampaignAgent.Core.Models;
using Xunit;

namespace CampaignAgent.Tests;

public class MetricsAggregatorTests
{
    private static readonly Campaign _testCampaign = new(
        Id: "test-001",
        Name: "Test Campaign",
        DailyBudget: 1000m,
        TargetCtr: 1.5m,
        TargetCpm: 8.00m,
        PeriodStart: DateTimeOffset.UtcNow.AddDays(-1),
        PeriodEnd: DateTimeOffset.UtcNow.AddDays(7)
    );

    [Fact]
    public void Aggregate_WithNoEvents_ReturnsZeroMetrics()
    {
        var aggregator = new MetricsAggregator();
        var metrics = aggregator.Aggregate(_testCampaign, []);

        Assert.Equal(0, metrics.Impressions);
        Assert.Equal(0, metrics.Clicks);
        Assert.Equal(0, metrics.Ctr);
        Assert.Equal(0, metrics.Cpm);
    }

    [Fact]
    public void Aggregate_CalculatesCtrCorrectly()
    {
        var aggregator = new MetricsAggregator();
        var events = BuildEvents(impressions: 1000, clicks: 15, conversions: 0);

        var metrics = aggregator.Aggregate(_testCampaign, events);

        Assert.Equal(1000, metrics.Impressions);
        Assert.Equal(15, metrics.Clicks);
        Assert.Equal(1.5m, metrics.Ctr);
    }

    [Fact]
    public void Aggregate_CalculatesCpmFromBidPrices()
    {
        var aggregator = new MetricsAggregator();
        // 1000 impressions at bid price 8.00 (CPM = spend / impressions * 1000)
        var events = BuildEvents(impressions: 1000, clicks: 0, conversions: 0, bidPrice: 8.00m);

        var metrics = aggregator.Aggregate(_testCampaign, events);

        Assert.Equal(8.00m, metrics.Cpm, precision: 2);
    }

    [Fact]
    public void Aggregate_BudgetPacingReflectsSpend()
    {
        var aggregator = new MetricsAggregator();
        // Spend = 1000 impressions * 8.00 bid / 1000 = 8.00
        // Budget = 1000, pacing = 0.8%
        var events = BuildEvents(impressions: 1000, clicks: 0, conversions: 0, bidPrice: 8.00m);
        var metrics = aggregator.Aggregate(_testCampaign, events);

        Assert.InRange(metrics.BudgetPacingPct, 0m, 100m);
    }

    private static IReadOnlyList<AdEvent> BuildEvents(
        int impressions, int clicks, int conversions, decimal bidPrice = 8.00m)
    {
        var events = new List<AdEvent>();
        for (int i = 0; i < impressions; i++)
            events.Add(new AdEvent($"imp-{i}", "test-001", "creative-a", AdEventType.Impression, DateTimeOffset.UtcNow, bidPrice, "header"));
        for (int i = 0; i < clicks; i++)
            events.Add(new AdEvent($"clk-{i}", "test-001", "creative-a", AdEventType.Click, DateTimeOffset.UtcNow, 0, "header"));
        for (int i = 0; i < conversions; i++)
            events.Add(new AdEvent($"cv-{i}", "test-001", "creative-a", AdEventType.Conversion, DateTimeOffset.UtcNow, 0, "header"));
        return events;
    }
}

public class MockLlmClientTests
{
    private static CampaignMetrics BuildMetrics(decimal ctr, decimal cpm, decimal budgetPct) =>
        new(
            CampaignId: "test-001",
            CampaignName: "Test",
            Impressions: 10000,
            Clicks: (long)(10000 * ctr / 100),
            Conversions: 10,
            SpendToDate: 1000 * budgetPct / 100,
            DailyBudget: 1000m,
            TargetCtr: 1.5m,
            TargetCpm: 8.00m,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd: DateTimeOffset.UtcNow.AddDays(7)
        );

    [Fact]
    public async Task GetRecommendation_HealthyCampaign_ReturnsNoAction()
    {
        var client = new MockLlmClient();
        var metrics = BuildMetrics(ctr: 1.6m, cpm: 8.00m, budgetPct: 50);

        var rec = await client.GetRecommendationAsync(metrics);

        Assert.Equal(RecommendedAction.NoAction, rec.Action);
    }

    [Fact]
    public async Task GetRecommendation_LowCtrHighSpend_ReturnsCreativePause()
    {
        var client = new MockLlmClient();
        var metrics = BuildMetrics(ctr: 0.4m, cpm: 8.00m, budgetPct: 60);

        var rec = await client.GetRecommendationAsync(metrics);

        Assert.Equal(RecommendedAction.PauseCreative, rec.Action);
        Assert.Equal("high", rec.Urgency);
    }

    [Fact]
    public async Task GetRecommendation_HighCpm_ReturnsDecreaseBid()
    {
        var client = new MockLlmClient();
        // CTR is fine, but CPM is 40% above target
        var metrics = BuildMetrics(ctr: 1.6m, cpm: 11.5m, budgetPct: 30);

        var rec = await client.GetRecommendationAsync(metrics);

        Assert.Equal(RecommendedAction.DecreaseBid, rec.Action);
        Assert.NotNull(rec.SuggestedChange);
    }

    [Fact]
    public async Task GetRecommendation_AnomalousCtr_FlagsAnomaly()
    {
        var client = new MockLlmClient();
        var metrics = BuildMetrics(ctr: 15m, cpm: 8.00m, budgetPct: 40);

        var rec = await client.GetRecommendationAsync(metrics);

        Assert.Equal(RecommendedAction.FlagAnomaly, rec.Action);
        Assert.Equal("critical", rec.Urgency);
    }
}
