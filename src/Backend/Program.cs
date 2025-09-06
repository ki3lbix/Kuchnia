
using Backend.Infrastructure;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string
var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
         ?? builder.Configuration.GetConnectionString("Default")
         ?? "Host=localhost;Port=5432;Database=catering;Username=app;Password=app";

builder.Services.AddDbContext<AppDb>(o => o.UseNpgsql(cs));
builder.Services.AddScoped<InventoryService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new { status = "ok" }));

// Reserve stock
app.MapPost("/inventory/reserve", async (InventoryService svc, ReserveRequest req) =>
{
    var res = await svc.ReserveAsync(req.PlanId);
    return Results.Ok(res);
});

// Consume stock
app.MapPost("/inventory/consume", async (InventoryService svc, ConsumeRequest req) =>
{
    var res = await svc.ConsumeAsync(req.PlanId);
    return Results.Ok(res);
});

// Minimal endpoints to manipulate production plan/items for demo
app.MapPost("/production/plan", async (AppDb db, CreatePlanRequest req) =>
{
    var plan = new ProductionPlan { PlanId = Guid.NewGuid(), PlanDate = req.PlanDate, Status = "Planned" };
    db.ProductionPlans.Add(plan);
    await db.SaveChangesAsync();
    return Results.Created($"/production/plan/{plan.PlanId}", plan);
});

app.MapPost("/production/plan/{planId:guid}/item", async (Guid planId, AppDb db, CreatePlanItemRequest req) =>
{
    var item = new ProductionItem { PlanId = planId, RecipeId = req.RecipeId, DietVariant = req.DietVariant ?? "standard", Portions = req.Portions };
    db.ProductionItems.Add(item);
    await db.SaveChangesAsync();
    return Results.Created($"/production/plan/{planId}/item", item);
});

app.Run();

// DTOs
public record ReserveRequest(Guid PlanId);
public record ConsumeRequest(Guid PlanId);
public record CreatePlanRequest(DateTime PlanDate);
public record CreatePlanItemRequest(Guid RecipeId, int Portions, string? DietVariant);
