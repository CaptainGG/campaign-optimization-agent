<script lang="ts">
  import type { Campaign, CampaignMetrics, AgentRecommendation } from './types';

  export let campaign: Campaign;
  export let metrics: CampaignMetrics | undefined;
  export let recommendation: AgentRecommendation | undefined;

  const urgencyColor: Record<string, string> = {
    low: '#4fffb0',
    medium: '#ffc94d',
    high: '#ff7043',
    critical: '#ff1744',
  };

  const actionLabel: Record<string, string> = {
    NoAction: '✓ No action',
    PauseCreative: '⏸ Pause creative',
    ReallocateBudget: '↔ Reallocate budget',
    IncreaseBid: '↑ Increase bid',
    DecreaseBid: '↓ Decrease bid',
    FlagAnomaly: '⚠ Anomaly flagged',
  };

  $: ctrDelta = metrics ? metrics.ctr - metrics.targetCtr : 0;
  $: cpmDelta = metrics ? metrics.cpm - metrics.targetCpm : 0;
</script>

<div class="card" class:has-alert={recommendation && recommendation.action !== 'NoAction'}>
  <div class="card-header">
    <span class="campaign-name">{campaign.name}</span>
    <span class="campaign-id">{campaign.id}</span>
  </div>

  {#if metrics}
    <div class="metrics-grid">
      <div class="metric">
        <span class="metric-label">Impressions</span>
        <span class="metric-value">{metrics.impressions.toLocaleString()}</span>
      </div>
      <div class="metric">
        <span class="metric-label">Clicks</span>
        <span class="metric-value">{metrics.clicks.toLocaleString()}</span>
      </div>
      <div class="metric">
        <span class="metric-label">CTR</span>
        <span class="metric-value" class:positive={ctrDelta >= 0} class:negative={ctrDelta < 0}>
          {metrics.ctr.toFixed(2)}%
          <span class="delta">({ctrDelta >= 0 ? '+' : ''}{ctrDelta.toFixed(2)}%)</span>
        </span>
      </div>
      <div class="metric">
        <span class="metric-label">CPM</span>
        <span class="metric-value" class:positive={cpmDelta <= 0} class:negative={cpmDelta > 0}>
          {metrics.cpm.toFixed(2)}
          <span class="delta">({cpmDelta >= 0 ? '+' : ''}{cpmDelta.toFixed(2)})</span>
        </span>
      </div>
    </div>

    <div class="budget-bar-wrapper">
      <div class="budget-bar-label">
        <span>Budget pacing</span>
        <span>{metrics.budgetPacingPct.toFixed(0)}%</span>
      </div>
      <div class="budget-bar-track">
        <div
          class="budget-bar-fill"
          style="width: {Math.min(metrics.budgetPacingPct, 100)}%; background: {metrics.budgetPacingPct > 85 ? '#ff7043' : '#4fffb0'}"
        ></div>
      </div>
    </div>
  {:else}
    <div class="loading">Loading metrics…</div>
  {/if}

  {#if recommendation}
    <div class="recommendation" style="border-color: {urgencyColor[recommendation.urgency] ?? '#4fffb0'}22; background: {urgencyColor[recommendation.urgency] ?? '#4fffb0'}08">
      <div class="rec-action" style="color: {urgencyColor[recommendation.urgency]}">
        {actionLabel[recommendation.action] ?? recommendation.action}
      </div>
      <p class="rec-reason">{recommendation.reason}</p>
    </div>
  {/if}
</div>

<style>
  .card {
    background: #111318;
    border: 1px solid #1e2128;
    border-radius: 12px;
    padding: 20px;
    transition: border-color 0.2s;
  }

  .card.has-alert { border-color: #2a2230; }

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
    margin-bottom: 16px;
  }

  .campaign-name { font-size: 15px; font-weight: 600; color: #dde0ea; }
  .campaign-id { font-size: 11px; color: #3a4050; font-family: monospace; }

  .metrics-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
    margin-bottom: 16px;
  }

  .metric { display: flex; flex-direction: column; gap: 3px; }
  .metric-label { font-size: 10px; text-transform: uppercase; letter-spacing: 0.8px; color: #4a5260; }
  .metric-value { font-size: 16px; font-weight: 600; color: #cdd0da; }
  .metric-value.positive { color: #4fffb0; }
  .metric-value.negative { color: #ff7043; }
  .delta { font-size: 11px; font-weight: 400; opacity: 0.7; }

  .budget-bar-wrapper { margin-bottom: 16px; }
  .budget-bar-label {
    display: flex;
    justify-content: space-between;
    font-size: 11px;
    color: #4a5260;
    margin-bottom: 6px;
  }
  .budget-bar-track {
    height: 4px;
    background: #1e2128;
    border-radius: 2px;
    overflow: hidden;
  }
  .budget-bar-fill {
    height: 100%;
    border-radius: 2px;
    transition: width 0.6s ease;
  }

  .recommendation {
    border: 1px solid;
    border-radius: 8px;
    padding: 12px;
  }

  .rec-action { font-size: 12px; font-weight: 700; margin-bottom: 6px; }
  .rec-reason { font-size: 12px; color: #6a7280; line-height: 1.5; }

  .loading { font-size: 12px; color: #3a4050; text-align: center; padding: 20px 0; }
</style>
