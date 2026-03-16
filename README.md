# Campaign Optimization Agent

A lightweight campaign optimization agent that simulates high-volume ad event ingestion, runs an LLM-driven analysis loop, and surfaces actionable recommendations through a real-time Svelte dashboard.

---

## What it does

The agent follows a simple observe, analyze, suggest loop:

1. **Observe.** A .NET event simulator streams mock impression, click, and conversion events into an in-memory store, mimicking the shape of a real ad event pipeline.
2. **Analyze.** The campaign agent reads the aggregated metrics (CTR, CPM, conversion rate, budget pacing) and builds a structured prompt for GPT-4o.
3. **Suggest.** The LLM returns actionable recommendations like pausing underperforming creatives, reallocating budget, or flagging anomalies. These show up in the dashboard alongside raw metrics.

The frontend is a Svelte dashboard that polls the API and displays live campaign state and agent output.

---

## Why I built it this way

A few deliberate choices:

- **Minimal API over a full framework.** The backend is intentionally thin. The agent loop is the interesting part; wrapping it in a heavier framework would just add noise.
- **Mocked LLM with a real prompt structure.** The `ILlmClient` interface lets you swap in a real OpenAI client with one line change. The mock returns realistic, structured responses so the whole system behaves as if the LLM is live.
- **Kept the scope honest.** Built to be shippable in a weekend with something meaningful at the end, not a half-finished skeleton of something bigger.
- **Svelte for the frontend.** Its reactivity model works naturally for a dashboard that needs to update frequently from a live source.

---

## Project structure

```text
campaign-optimization-agent/
|-- backend/
|   |-- CampaignAgent.Api/          # .NET Minimal API, endpoints and DI wiring
|   |-- CampaignAgent.Core/         # Domain: events, campaigns, agent logic
|   `-- CampaignAgent.Tests/        # xUnit unit tests
|-- frontend/
|   `-- dashboard/                  # Svelte dashboard
|-- docs/
|   `-- architecture.md             # System design notes
`-- docker-compose.yml
```

---

## Tech stack

| Layer | Technology |
|---|---|
| Backend | .NET 8, C#, Minimal API |
| AI Agent | OpenAI GPT-4o (mocked in dev) |
| Frontend | Svelte, TypeScript |
| Testing | xUnit, NSubstitute |
| Infra | Docker Compose |

---

## Getting started

### Prerequisites
- .NET 8 SDK
- Node.js 20+
- Docker (optional)

### Run with Docker Compose

```bash
docker-compose up
```

Dashboard: http://localhost:5173  
API: http://localhost:5000

### Run manually

**Backend:**
```bash
cd backend/CampaignAgent.Api
dotnet run
```

**Frontend:**
```bash
cd frontend/dashboard
npm install
npm run dev
```

### Enable real OpenAI calls

In `appsettings.json`, set:
```json
{
  "LlmProvider": "openai",
  "OpenAI": {
    "ApiKey": "your-key-here",
    "Model": "gpt-4o"
  }
}
```

Then register `OpenAiLlmClient` instead of `MockLlmClient` in `Program.cs`.

---

## Running tests

```bash
cd backend
dotnet test
```

---

## Agent prompt design

The agent receives a structured context object per campaign and returns a JSON recommendation. Each suggestion includes a `reason` field explaining the logic, which shows up in the dashboard so recommendations are auditable, not just a black box.

Example output:

```json
{
  "campaignId": "camp-003",
  "action": "reallocate_budget",
  "urgency": "high",
  "reason": "CTR is 0.8% against a 1.5% target with 60% of budget spent. Current pacing suggests the campaign will exhaust budget before reaching impression goals.",
  "suggestedChange": {
    "field": "dailyBudget",
    "from": 500,
    "to": 320
  }
}
```

---

## What I'd build next

- **Persistent event store.** Swap the in-memory store for something like Azure Event Hubs and Snowflake for a proper production pipeline.
- **Agent memory.** Let the agent track its own past recommendations and whether they actually improved performance.
- **Webhook delivery.** Push recommendations somewhere actionable like Slack or an internal tool, not just the dashboard.
- **Real evaluation.** Measure recommendation quality against actual campaign outcomes over time.

---

*Built by Harsh Kakroo*
