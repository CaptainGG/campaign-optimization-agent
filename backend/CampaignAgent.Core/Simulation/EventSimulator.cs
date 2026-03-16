using CampaignAgent.Core.EventStore;
using CampaignAgent.Core.Models;
using CampaignAgent.Core.Agent;

namespace CampaignAgent.Core.Simulation;

/// <summary>
/// Simulates a realistic ad event stream for development.
/// Generates impression, click, and conversion events across campaigns
/// with configurable rates that create varied scenarios for the agent to analyze.
/// </summary>
public sealed class EventSimulator : BackgroundService
{
    private static readonly Random _rng = new();
    private readonly IEventStore _eventStore;
    private readonly ICampaignRepository _campaigns;
    private readonly ILogger<EventSimulator> _logger;

    // Campaign-specific profiles create varied scenarios for the agent to analyze
    private static readonly Dictionary<string, SimProfile> _profiles = new()
    {
        ["camp-001"] = new SimProfile(ImpressionsPerTick: 80, CtrRate: 0.018m, CvRate: 0.03m, AvgBid: 8.20m),  // Healthy
        ["camp-002"] = new SimProfile(ImpressionsPerTick: 120, CtrRate: 0.003m, CvRate: 0.01m, AvgBid: 15.50m), // High CPM, low CTR
        ["camp-003"] = new SimProfile(ImpressionsPerTick: 40, CtrRate: 0.004m, CvRate: 0.02m, AvgBid: 6.10m),   // Underperforming CTR
    };

    private static readonly string[] _placements = ["header", "sidebar", "mid-article", "footer", "interstitial"];
    private static readonly string[] _creatives = ["creative-a", "creative-b", "creative-c"];

    public EventSimulator(IEventStore eventStore, ICampaignRepository campaigns, ILogger<EventSimulator> logger)
    {
        _eventStore = eventStore;
        _campaigns = campaigns;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event simulator started. Generating ad events every 2 seconds.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var campaigns = await _campaigns.GetAllAsync(stoppingToken);

            foreach (var campaign in campaigns)
            {
                if (!_profiles.TryGetValue(campaign.Id, out var profile))
                    continue;

                await SimulateCampaignTick(campaign.Id, profile, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task SimulateCampaignTick(string campaignId, SimProfile profile, CancellationToken ct)
    {
        var impressionCount = _rng.Next(
            (int)(profile.ImpressionsPerTick * 0.7),
            (int)(profile.ImpressionsPerTick * 1.3));

        for (int i = 0; i < impressionCount; i++)
        {
            var eventId = Guid.NewGuid().ToString("N")[..8];
            var placement = _placements[_rng.Next(_placements.Length)];
            var creative = _creatives[_rng.Next(_creatives.Length)];
            var bid = profile.AvgBid * (decimal)(0.85 + _rng.NextDouble() * 0.3);
            var ts = DateTimeOffset.UtcNow.AddMilliseconds(-_rng.Next(0, 2000));

            await _eventStore.AppendAsync(new AdEvent(eventId, campaignId, creative, AdEventType.Impression, ts, bid, placement), ct);

            if ((decimal)_rng.NextDouble() < profile.CtrRate)
            {
                await _eventStore.AppendAsync(new AdEvent(Guid.NewGuid().ToString("N")[..8], campaignId, creative, AdEventType.Click, ts.AddSeconds(1), 0, placement), ct);

                if ((decimal)_rng.NextDouble() < profile.CvRate)
                {
                    await _eventStore.AppendAsync(new AdEvent(Guid.NewGuid().ToString("N")[..8], campaignId, creative, AdEventType.Conversion, ts.AddSeconds(30), 0, placement), ct);
                }
            }
        }
    }

    private record SimProfile(int ImpressionsPerTick, decimal CtrRate, decimal CvRate, decimal AvgBid);
}
