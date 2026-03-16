using CampaignAgent.Core.Models;

namespace CampaignAgent.Core.EventStore;

public interface IEventStore
{
    Task AppendAsync(AdEvent adEvent, CancellationToken ct = default);
    Task<IReadOnlyList<AdEvent>> GetByCampaignAsync(string campaignId, DateTimeOffset since, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetActiveCampaignIdsAsync(CancellationToken ct = default);
}

/// <summary>
/// In-memory event store for development. Replace with Azure Event Hubs +
/// Snowflake for production to match a real pipeline shape.
/// </summary>
public sealed class InMemoryEventStore : IEventStore
{
    private readonly List<AdEvent> _events = [];
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task AppendAsync(AdEvent adEvent, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _events.Add(adEvent);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<AdEvent>> GetByCampaignAsync(
        string campaignId,
        DateTimeOffset since,
        CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            return _events
                .Where(e => e.CampaignId == campaignId && e.Timestamp >= since)
                .OrderBy(e => e.Timestamp)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<string>> GetActiveCampaignIdsAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            return _events
                .Select(e => e.CampaignId)
                .Distinct()
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }
}
