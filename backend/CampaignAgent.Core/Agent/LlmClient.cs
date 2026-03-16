using CampaignAgent.Core.Models;
using System.Text.Json;

namespace CampaignAgent.Core.Agent;

public interface ILlmClient
{
    /// <summary>
    /// Given structured campaign metrics, returns an optimization recommendation.
    /// Swap MockLlmClient for OpenAiLlmClient in DI to use a real model.
    /// </summary>
    Task<AgentRecommendation> GetRecommendationAsync(
        CampaignMetrics metrics,
        CancellationToken ct = default);
}

/// <summary>
/// Mock LLM client that returns realistic, deterministic recommendations
/// based on simple rules, mirroring what a real LLM would be prompted to do.
/// Lets the full system run without an API key.
/// </summary>
public sealed class MockLlmClient : ILlmClient
{
    private static readonly Random _rng = new(42);

    public Task<AgentRecommendation> GetRecommendationAsync(
        CampaignMetrics metrics,
        CancellationToken ct = default)
    {
        var recommendation = Analyze(metrics);
        return Task.FromResult(recommendation);
    }

    private static AgentRecommendation Analyze(CampaignMetrics m)
    {
        // Simulate the reasoning a well-prompted LLM would apply

        // Anomaly: CTR is impossibly high (bot traffic signal)
        if (m.Ctr > 10)
        {
            return Recommend(m.CampaignId, RecommendedAction.FlagAnomaly, "critical",
                $"CTR of {m.Ctr:F1}% is statistically implausible. Possible bot traffic or tracking misconfiguration. Manual review recommended before further spend.",
                null);
        }

        // Severely underperforming CTR with significant spend
        if (m.Ctr < m.TargetCtr * 0.5m && m.BudgetPacingPct > 40)
        {
            return Recommend(m.CampaignId, RecommendedAction.PauseCreative, "high",
                $"CTR is {m.Ctr:F2}% — less than 50% of the {m.TargetCtr:F2}% target — with {m.BudgetPacingPct:F0}% of budget already consumed. Creative is not resonating. Pause and rotate.",
                new SuggestedChange("creativeStatus", "active", "paused"));
        }

        // CPM above target — overpaying for inventory
        if (m.Cpm > m.TargetCpm * 1.3m)
        {
            var suggestedBid = Math.Round(m.Cpm * 0.85m, 2);
            return Recommend(m.CampaignId, RecommendedAction.DecreaseBid, "medium",
                $"CPM of {m.Cpm:F2} is {((m.Cpm / m.TargetCpm - 1) * 100):F0}% above target. Reducing max bid to bring CPM in range.",
                new SuggestedChange("maxBid", m.Cpm, suggestedBid));
        }

        // Budget pacing ahead of schedule — risk of exhausting before period end
        if (m.BudgetPacingPct > 80 && m.Ctr >= m.TargetCtr)
        {
            var reducedBudget = Math.Round(m.DailyBudget * 0.75m, 2);
            return Recommend(m.CampaignId, RecommendedAction.ReallocateBudget, "medium",
                $"Campaign has spent {m.BudgetPacingPct:F0}% of daily budget. At current pace it will exhaust funds early. Consider redistributing to a higher-performing time window.",
                new SuggestedChange("dailyBudget", m.DailyBudget, reducedBudget));
        }

        // Underperforming CPM but good CTR — low bid limiting reach
        if (m.Cpm < m.TargetCpm * 0.6m && m.Ctr >= m.TargetCtr)
        {
            var suggestedBid = Math.Round(m.TargetCpm * 0.9m, 2);
            return Recommend(m.CampaignId, RecommendedAction.IncreaseBid, "low",
                $"CTR is healthy at {m.Ctr:F2}% but CPM of {m.Cpm:F2} suggests the campaign is winning only low-value inventory. Increasing bid may unlock better placements.",
                new SuggestedChange("maxBid", m.Cpm, suggestedBid));
        }

        return Recommend(m.CampaignId, RecommendedAction.NoAction, "low",
            $"Campaign metrics are within acceptable ranges. CTR {m.Ctr:F2}% vs target {m.TargetCtr:F2}%, CPM {m.Cpm:F2} vs target {m.TargetCpm:F2}. No action required.",
            null);
    }

    private static AgentRecommendation Recommend(
        string campaignId,
        RecommendedAction action,
        string urgency,
        string reason,
        SuggestedChange? change) =>
        new(campaignId, action, urgency, reason, change, DateTimeOffset.UtcNow);
}

/// <summary>
/// Builds the structured prompt sent to GPT-4o.
/// Kept separate so prompt logic is easy to iterate on independently.
/// </summary>
public static class PromptBuilder
{
    public static string BuildSystemPrompt() => """
        You are a campaign optimization agent for a digital advertising platform.
        
        Your job is to analyze campaign performance metrics and return a single, actionable recommendation.
        
        Rules:
        - Be concise and specific. Reference actual numbers from the metrics.
        - Return ONLY valid JSON matching the schema below — no markdown, no preamble.
        - Urgency must be one of: "low", "medium", "high", "critical"
        - Action must be one of: "no_action", "pause_creative", "reallocate_budget", "increase_bid", "decrease_bid", "flag_anomaly"
        
        Response schema:
        {
          "campaignId": string,
          "action": string,
          "urgency": string,
          "reason": string,
          "suggestedChange": { "field": string, "from": any, "to": any } | null
        }
        """;

    public static string BuildUserPrompt(CampaignMetrics m) => $"""
        Analyze this campaign and return a recommendation:

        Campaign: {m.CampaignName} ({m.CampaignId})
        Period: {m.PeriodStart:yyyy-MM-dd} to {m.PeriodEnd:yyyy-MM-dd}
        
        Metrics:
        - Impressions: {m.Impressions:N0}
        - Clicks: {m.Clicks:N0}
        - Conversions: {m.Conversions:N0}
        - CTR: {m.Ctr:F2}% (target: {m.TargetCtr:F2}%)
        - CPM: {m.Cpm:F2} (target: {m.TargetCpm:F2})
        - Conversion rate: {m.ConversionRate:F2}%
        - Spend to date: {m.SpendToDate:C}
        - Daily budget: {m.DailyBudget:C}
        - Budget pacing: {m.BudgetPacingPct:F0}%
        """;
}
