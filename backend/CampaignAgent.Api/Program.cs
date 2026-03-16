using CampaignAgent.Core.Agent;
using CampaignAgent.Core.EventStore;
using CampaignAgent.Core.Simulation;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();
builder.Services.AddSingleton<ICampaignRepository, InMemoryCampaignRepository>();
builder.Services.AddSingleton<IMetricsAggregator, MetricsAggregator>();

// Swap MockLlmClient → OpenAiLlmClient here when a real API key is available
builder.Services.AddSingleton<ILlmClient, MockLlmClient>();
builder.Services.AddSingleton<ICampaignAgent, CampaignAgent>();

// Background event simulator (dev only)
builder.Services.AddHostedService<EventSimulator>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors();

// --- Endpoints ---

// GET /campaigns — list all known campaigns
app.MapGet("/campaigns", async (ICampaignRepository repo) =>
{
    var campaigns = await repo.GetAllAsync();
    return Results.Ok(campaigns);
});

// GET /campaigns/{id}/metrics — live metrics for a campaign
app.MapGet("/campaigns/{id}/metrics", async (
    string id,
    IEventStore store,
    IMetricsAggregator aggregator,
    ICampaignRepository repo) =>
{
    try
    {
        var campaign = await repo.GetAsync(id);
        var since = DateTimeOffset.UtcNow.AddHours(-24);
        var events = await store.GetByCampaignAsync(id, since);
        var metrics = aggregator.Aggregate(campaign, events);
        return Results.Ok(metrics);
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

// POST /agent/run — trigger one full optimization cycle across all campaigns
app.MapPost("/agent/run", async (ICampaignAgent agent) =>
{
    var recommendations = await agent.RunCycleAsync();
    return Results.Ok(recommendations);
});

// POST /agent/campaigns/{id}/analyze — analyze a single campaign
app.MapPost("/agent/campaigns/{id}/analyze", async (string id, ICampaignAgent agent) =>
{
    try
    {
        var rec = await agent.AnalyzeCampaignAsync(id);
        return Results.Ok(rec);
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

// GET /health
app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTimeOffset.UtcNow }));

app.Run();
