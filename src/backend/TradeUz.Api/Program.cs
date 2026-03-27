using Npgsql;
using TradeUz.Api.Data;
using TradeUz.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("TradeUz")
        ?? throw new InvalidOperationException("Connection string 'TradeUz' is not configured.");

    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    return dataSourceBuilder.Build();
});
builder.Services.AddSingleton<PostgresTradeRepository>();
builder.Services.AddSingleton<DatabaseInitializer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/orders", async (PostgresTradeRepository repository, CancellationToken cancellationToken) =>
{
    var orders = await repository.GetOrdersAsync(cancellationToken);
    return Results.Ok(orders);
});

app.MapGet("/api/routes", async (PostgresTradeRepository repository, CancellationToken cancellationToken) =>
{
    var routes = await repository.GetRoutesAsync(cancellationToken);
    return Results.Ok(routes);
});

app.MapPost("/api/orders", async (CreateTradeOrderRequest request, PostgresTradeRepository repository, CancellationToken cancellationToken) =>
{
    var errors = ValidateOrderRequest(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var created = await repository.CreateOrderAsync(request, cancellationToken);
    return Results.Created($"/api/orders/{Uri.EscapeDataString(created.Number)}", created);
});

app.MapPatch("/api/orders/{orderNumber}/advance", async (string orderNumber, PostgresTradeRepository repository, CancellationToken cancellationToken) =>
{
    var success = await repository.AdvanceOrderAsync(orderNumber, cancellationToken);
    return success ? Results.Ok() : Results.NotFound();
});

app.MapPatch("/api/routes/{routeCode}/start", async (string routeCode, PostgresTradeRepository repository, CancellationToken cancellationToken) =>
{
    var success = await repository.StartRouteAsync(routeCode, cancellationToken);
    return success ? Results.Ok() : Results.NotFound();
});

app.MapPatch("/api/routes/{routeCode}/complete-next-stop", async (string routeCode, PostgresTradeRepository repository, CancellationToken cancellationToken) =>
{
    var success = await repository.CompleteNextStopAsync(routeCode, cancellationToken);
    return success ? Results.Ok() : Results.NotFound();
});

app.Run();

static Dictionary<string, string[]> ValidateOrderRequest(CreateTradeOrderRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.AgentName))
    {
        errors[nameof(request.AgentName)] = ["Agent is required."];
    }

    if (string.IsNullOrWhiteSpace(request.RetailPointName))
    {
        errors[nameof(request.RetailPointName)] = ["Retail point is required."];
    }

    if (string.IsNullOrWhiteSpace(request.Address))
    {
        errors[nameof(request.Address)] = ["Address is required."];
    }

    if (request.BoxCount <= 0)
    {
        errors[nameof(request.BoxCount)] = ["Box count must be positive."];
    }

    if (request.Lines.Count == 0)
    {
        errors[nameof(request.Lines)] = ["At least one line is required."];
        return errors;
    }

    if (request.Lines.Any(line => string.IsNullOrWhiteSpace(line.ProductName) || string.IsNullOrWhiteSpace(line.Unit) || line.Quantity <= 0 || line.UnitPrice <= 0))
    {
        errors[nameof(request.Lines)] = ["Each line must contain a product, unit, quantity and price."];
    }

    return errors;
}
