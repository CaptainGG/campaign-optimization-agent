<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import CampaignCard from './lib/CampaignCard.svelte';
  import AgentPanel from './lib/AgentPanel.svelte';
  import type { CampaignMetrics, AgentRecommendation, Campaign } from './lib/types';

  const API = 'http://localhost:5000';

  let campaigns: Campaign[] = [];
  let metricsMap: Record<string, CampaignMetrics> = {};
  let recommendations: AgentRecommendation[] = [];
  let agentRunning = false;
  let lastRunAt: Date | null = null;
  let pollingInterval: ReturnType<typeof setInterval>;

  async function fetchCampaigns() {
    const res = await fetch(`${API}/campaigns`);
    campaigns = await res.json();
  }

  async function fetchMetrics() {
    await Promise.all(
      campaigns.map(async (c) => {
        const res = await fetch(`${API}/campaigns/${c.id}/metrics`);
        const data: CampaignMetrics = await res.json();
        metricsMap = { ...metricsMap, [c.id]: data };
      })
    );
  }

  async function runAgent() {
    agentRunning = true;
    try {
      const res = await fetch(`${API}/agent/run`, { method: 'POST' });
      recommendations = await res.json();
      lastRunAt = new Date();
    } finally {
      agentRunning = false;
    }
  }

  onMount(async () => {
    await fetchCampaigns();
    await fetchMetrics();
    pollingInterval = setInterval(fetchMetrics, 3000);
  });

  onDestroy(() => clearInterval(pollingInterval));
</script>

<main>
  <header>
    <div class="logo">
      <span class="logo-mark">◆</span>
      <span class="logo-text">Campaign Optimization</span>
      <span class="logo-sub">Campaign Agent</span>
    </div>
    <div class="header-right">
      {#if lastRunAt}
        <span class="last-run">Last run: {lastRunAt.toLocaleTimeString()}</span>
      {/if}
      <button class="run-btn" on:click={runAgent} disabled={agentRunning}>
        {#if agentRunning}
          <span class="spinner"></span> Analyzing…
        {:else}
          ▶ Run Agent
        {/if}
      </button>
    </div>
  </header>

  <div class="layout">
    <section class="campaigns">
      <h2 class="section-title">Live Campaigns</h2>
      <div class="campaign-grid">
        {#each campaigns as campaign}
          <CampaignCard
            {campaign}
            metrics={metricsMap[campaign.id]}
            recommendation={recommendations.find(r => r.campaignId === campaign.id)}
          />
        {/each}
      </div>
    </section>

    <aside class="agent-sidebar">
      <h2 class="section-title">Agent Recommendations</h2>
      <AgentPanel {recommendations} {agentRunning} onRunAgent={runAgent} />
    </aside>
  </div>
</main>

<style>
  :global(*, *::before, *::after) { box-sizing: border-box; margin: 0; padding: 0; }

  :global(body) {
    background: #0a0c10;
    color: #e8eaf0;
    font-family: 'DM Sans', 'Helvetica Neue', sans-serif;
    min-height: 100vh;
  }

  main { max-width: 1400px; margin: 0 auto; padding: 0 24px 48px; }

  header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 24px 0 32px;
    border-bottom: 1px solid rgba(255,255,255,0.06);
    margin-bottom: 36px;
  }

  .logo { display: flex; align-items: center; gap: 10px; }

  .logo-mark {
    color: #4fffb0;
    font-size: 22px;
  }

  .logo-text {
    font-size: 20px;
    font-weight: 700;
    letter-spacing: -0.3px;
    color: #fff;
  }

  .logo-sub {
    font-size: 13px;
    color: #5a6070;
    padding-left: 10px;
    border-left: 1px solid #2a2d35;
    margin-left: 2px;
  }

  .header-right { display: flex; align-items: center; gap: 16px; }

  .last-run { font-size: 12px; color: #4a5260; }

  .run-btn {
    background: #4fffb0;
    color: #040608;
    border: none;
    padding: 10px 20px;
    border-radius: 8px;
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 8px;
    transition: opacity 0.15s, transform 0.1s;
  }

  .run-btn:hover:not(:disabled) { opacity: 0.88; transform: translateY(-1px); }
  .run-btn:disabled { opacity: 0.5; cursor: not-allowed; }

  .spinner {
    width: 12px; height: 12px;
    border: 2px solid rgba(4,6,8,0.3);
    border-top-color: #040608;
    border-radius: 50%;
    animation: spin 0.7s linear infinite;
    display: inline-block;
  }

  @keyframes spin { to { transform: rotate(360deg); } }

  .layout {
    display: grid;
    grid-template-columns: 1fr 340px;
    gap: 32px;
    align-items: start;
  }

  .section-title {
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 1.2px;
    color: #4a5260;
    margin-bottom: 16px;
  }

  .campaign-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 16px;
  }

  @media (max-width: 900px) {
    .layout { grid-template-columns: 1fr; }
  }
</style>
