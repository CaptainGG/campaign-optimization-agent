<script lang="ts">
  import type { AgentRecommendation } from './types';
  import { actionIcon, actionLabel, isNoAction } from './recommendations';

  export let recommendations: AgentRecommendation[] = [];
  export let agentRunning = false;
  export let onRunAgent: () => void;

  const urgencyColor: Record<string, string> = {
    low: '#4fffb0',
    medium: '#ffc94d',
    high: '#ff7043',
    critical: '#ff1744'
  };

  $: activeRecs = recommendations.filter((r) => !isNoAction(r.action));
  $: urgencyOrder = { critical: 0, high: 1, medium: 2, low: 3 };
  $: sorted = [...recommendations].sort(
    (a, b) => (urgencyOrder[a.urgency] ?? 9) - (urgencyOrder[b.urgency] ?? 9)
  );
</script>

<div class="panel">
  {#if recommendations.length === 0}
    <div class="empty">
      <div class="empty-icon">*</div>
      <p>Run the agent to generate optimization recommendations across all campaigns.</p>
      <button on:click={onRunAgent} disabled={agentRunning}>
        {agentRunning ? 'Analyzing...' : 'Run Agent'}
      </button>
    </div>
  {:else}
    {#if activeRecs.length > 0}
      <div class="summary-bar">
        <span class="summary-count">{activeRecs.length}</span>
        <span class="summary-label">action{activeRecs.length !== 1 ? 's' : ''} recommended</span>
      </div>
    {:else}
      <div class="all-clear">
        <span class="all-clear-icon">OK</span>
        All campaigns performing within target ranges.
      </div>
    {/if}

    <div class="rec-list">
      {#each sorted as rec}
        <div class="rec-item" class:no-action={isNoAction(rec.action)}>
          <div class="rec-top">
            <span class="rec-icon" style="color: {urgencyColor[rec.urgency]}">{actionIcon(rec.action)}</span>
            <span class="rec-action">{actionLabel(rec.action)}</span>
            <span class="rec-campaign">{rec.campaignId}</span>
            <span class="rec-urgency" style="color: {urgencyColor[rec.urgency]}">{rec.urgency}</span>
          </div>
          <p class="rec-reason">{rec.reason}</p>
          {#if rec.suggestedChange}
            <div class="change-chip">
              <span class="change-field">{rec.suggestedChange.field}</span>
              <span class="change-arrow">-></span>
              <span class="change-to">{String(rec.suggestedChange.to)}</span>
            </div>
          {/if}
        </div>
      {/each}
    </div>
  {/if}
</div>

<style>
  .panel {
    background: #111318;
    border: 1px solid #1e2128;
    border-radius: 12px;
    overflow: hidden;
  }

  .empty {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 12px;
    padding: 40px 24px;
    text-align: center;
  }

  .empty-icon {
    font-size: 28px;
    color: #2a3040;
  }

  .empty p {
    font-size: 13px;
    color: #4a5260;
    line-height: 1.5;
  }

  .empty button {
    background: #4fffb0;
    color: #040608;
    border: none;
    padding: 9px 18px;
    border-radius: 7px;
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    margin-top: 4px;
  }

  .empty button:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  .summary-bar {
    display: flex;
    align-items: baseline;
    gap: 6px;
    padding: 14px 16px;
    background: rgba(255, 112, 67, 0.06);
    border-bottom: 1px solid rgba(255, 112, 67, 0.12);
  }

  .summary-count {
    font-size: 22px;
    font-weight: 700;
    color: #ff7043;
  }

  .summary-label {
    font-size: 12px;
    color: #6a7280;
  }

  .all-clear {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 14px 16px;
    font-size: 13px;
    color: #4a5260;
    border-bottom: 1px solid #1e2128;
  }

  .all-clear-icon {
    color: #4fffb0;
    font-size: 12px;
    font-weight: 700;
  }

  .rec-list {
    display: flex;
    flex-direction: column;
  }

  .rec-item {
    padding: 14px 16px;
    border-bottom: 1px solid #1a1d24;
    transition: background 0.15s;
  }

  .rec-item:last-child {
    border-bottom: none;
  }

  .rec-item:hover {
    background: #141720;
  }

  .rec-item.no-action {
    opacity: 0.45;
  }

  .rec-top {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 6px;
  }

  .rec-icon {
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.2px;
    min-width: 22px;
  }

  .rec-action {
    font-size: 12px;
    font-weight: 600;
    color: #dde0ea;
  }

  .rec-campaign {
    font-size: 12px;
    font-weight: 600;
    color: #9ca3b0;
    font-family: monospace;
    flex: 1;
  }

  .rec-urgency {
    font-size: 10px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.8px;
  }

  .rec-reason {
    font-size: 12px;
    color: #5a6270;
    line-height: 1.5;
  }

  .change-chip {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    margin-top: 8px;
    padding: 4px 10px;
    background: #1a1d24;
    border-radius: 5px;
    font-size: 11px;
    font-family: monospace;
  }

  .change-field {
    color: #6a7280;
  }

  .change-arrow {
    color: #3a4050;
  }

  .change-to {
    color: #4fffb0;
    font-weight: 600;
  }
</style>
