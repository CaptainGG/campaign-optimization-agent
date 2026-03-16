using CampaignAgent.Core.EventStore;
using CampaignAgent.Core.Models;

namespace CampaignAgent.Core.Agent;

public interface ICampaignAgent
{
    Task<IReadOnlyList<AgentRecommendation>> RunCycleAsync(CancellationToken ct = default);
    Task<AgentRecommendation> AnalyzeCampaignAsync(string campaignId, CancellationToken ct = default);
}

public sealed class CampaignAgent : ICampaignAgent
{
    private readonly IEventStore _eventStore;
    private readonly IMetricsAggregator _aggregator;
    private readonly ILlmClient _llm;
    private readonly ICampaignRepository _campaigns;

    public CampaignAgent(
        IEventStore eventStore,
        IMetricsAggregator aggregator,
        ILlmClient llm,
        ICampaignRepository campaigns)
    {
        _eventStore = eventStore;
        _aggregator = aggregator;
        _llm = llm;
        _campaigns = campaigns;
    }

    /// <summary>
    /// Runs one full observe → analyze → suggest cycle across all active campaigns.
    /// In production this would be triggered by a timer or event hub checkpoint.
    /// </summary>
    public async Task<IReadOnlyList<AgentRecommendation>> RunCycleAsync(CancellationToken ct = default)
    {
        var campaignIds = await _eventStore.GetActiveCampaignIdsAsync(ct);
        var tasks = campaignIds.Select(id => AnalyzeCampaignAsync(id, ct));
        var results = await Task.WhenAll(tasks);
        return results;
    }

    public async Task<AgentRecommendation> AnalyzeCampaignAsync(string campaignId, CancellationToken ct = default)
    {
        var campaign = await _campaigns.GetAsync(campaignId, ct);
        var since = DateTimeOffset.UtcNow.AddHours(-24);
        var events = await _eventStore.GetByCampaignAsync(campaignId, since, ct);
        var metrics = _aggregator.Aggregate(campaign, events);
        return await _llm.GetRecommendationAsync(metrics, ct);
    }
}
