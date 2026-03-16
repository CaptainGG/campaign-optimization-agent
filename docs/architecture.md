# Architecture Notes

## System overview

```
┌─────────────────────────────────────────────────────────┐
│                     Svelte Dashboard                    │
│  CampaignCard ─── AgentPanel ─── Live polling (3s)     │
└────────────────────────┬────────────────────────────────┘
                         │ HTTP
┌────────────────────────▼────────────────────────────────┐
│                  .NET Minimal API                       │
│  GET /campaigns                                         │
│  GET /campaigns/{id}/metrics                            │
│  POST /agent/run                                        │
│  POST /agent/campaigns/{id}/analyze                     │
└────┬───────────────────┬────────────────────────────────┘
     │                   │
┌────▼──────┐   ┌────────▼────────────────────────────────┐
│ Event     │   │              Campaign Agent              │
│ Simulator │   │                                         │
│           │   │  1. Observe  → fetch events from store  │
│ Generates │   │  2. Aggregate → compute CTR, CPM, etc.  │
│ realistic │   │  3. Analyze  → send metrics to LLM      │
│ ad events │   │  4. Suggest  → return recommendation    │
│ every 2s  │   │                                         │
└────┬──────┘   └────────────────────┬────────────────────┘
     │                               │
┌────▼───────────────────┐  ┌────────▼──────────┐
│   In-Memory Event      │  │   LLM Client      │
│   Store                │  │                   │
│   (→ Azure Event Hubs  │  │  MockLlmClient    │
│    + Snowflake in prod)│  │  (→ OpenAI GPT-4o │
└────────────────────────┘  │   in prod)        │
                            └───────────────────┘
```

## Key design decisions

### Why Minimal API, not a full MVC controller setup?

The agent loop is the interesting part of this system. A heavyweight controller framework would add noise without benefit at this scale. Minimal API lets the endpoints read almost like documentation of what the system does.

### Why an `ILlmClient` interface?

Swappability. The `MockLlmClient` returns deterministic, realistic responses that make the whole system testable and runnable without credentials. Swapping in `OpenAiLlmClient` is a single line in `Program.cs`. It also makes it easy to run evals: you can compare mock vs. real LLM outputs against the same event fixtures.

### Why poll from the frontend rather than websockets?

For a weekend project, polling every 3 seconds is good enough and keeps things simple. For production I'd use server-sent events (SSE) or SignalR. Svelte handles reactive updates well either way.

### Agent loop: observe, analyze, suggest

The agent doesn't maintain state between cycles, which is intentional. Each cycle re-reads the event store and re-derives recommendations from scratch. This makes the system easier to reason about and debug. A production version would want:
- Agent memory (track which recommendations were acted on)
- Feedback loop (did the suggested change improve metrics?)
- Durable state (persist recommendations to a DB)

### What would production look like?

| Dev (this project) | Production |
|---|---|
| In-memory event store | Azure Event Hubs |
| In-memory campaign repo | Platform DB |
| Simulated events | Real ad server events |
| Mock LLM | GPT-4o via OpenAI API |
| Polling dashboard | SSE / SignalR |
| Single process | Separate services |