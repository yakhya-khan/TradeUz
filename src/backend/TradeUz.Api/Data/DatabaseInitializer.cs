using Microsoft.Extensions.Configuration;
using Npgsql;
using TradeUz.Contracts;

namespace TradeUz.Api.Data;

public sealed class DatabaseInitializer
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly string _connectionString;

    public DatabaseInitializer(NpgsqlDataSource dataSource, IConfiguration configuration)
    {
        _dataSource = dataSource;
        _connectionString = configuration.GetConnectionString("TradeUz")
            ?? throw new InvalidOperationException("Connection string 'TradeUz' is not configured.");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseExistsAsync(cancellationToken);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
CREATE TABLE IF NOT EXISTS orders (
    id BIGSERIAL PRIMARY KEY,
    number TEXT NOT NULL UNIQUE,
    agent_name TEXT NOT NULL,
    retail_point_name TEXT NOT NULL,
    address TEXT NOT NULL,
    city TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL,
    planned_delivery_at TIMESTAMP NOT NULL,
    total_amount NUMERIC(18,2) NOT NULL,
    payment_type TEXT NOT NULL,
    comment TEXT NOT NULL,
    is_priority BOOLEAN NOT NULL,
    box_count INTEGER NOT NULL,
    status SMALLINT NOT NULL
);

CREATE TABLE IF NOT EXISTS order_lines (
    id BIGSERIAL PRIMARY KEY,
    order_id BIGINT NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_name TEXT NOT NULL,
    quantity INTEGER NOT NULL,
    unit TEXT NOT NULL,
    unit_price NUMERIC(18,2) NOT NULL
);

CREATE TABLE IF NOT EXISTS delivery_routes (
    id BIGSERIAL PRIMARY KEY,
    route_code TEXT NOT NULL UNIQUE,
    region TEXT NOT NULL,
    driver_name TEXT NOT NULL,
    vehicle_number TEXT NOT NULL,
    dispatcher_name TEXT NOT NULL,
    planned_departure_at TIMESTAMP NOT NULL,
    status SMALLINT NOT NULL
);

CREATE TABLE IF NOT EXISTS delivery_stops (
    id BIGSERIAL PRIMARY KEY,
    route_id BIGINT NOT NULL REFERENCES delivery_routes(id) ON DELETE CASCADE,
    sequence INTEGER NOT NULL,
    order_number TEXT NOT NULL,
    retail_point_name TEXT NOT NULL,
    address TEXT NOT NULL,
    contact_phone TEXT NOT NULL,
    planned_arrival_at TIMESTAMP NOT NULL,
    amount NUMERIC(18,2) NOT NULL,
    status SMALLINT NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_order_lines_order_id ON order_lines(order_id);
CREATE INDEX IF NOT EXISTS ix_delivery_stops_route_id ON delivery_stops(route_id);
CREATE INDEX IF NOT EXISTS ix_delivery_stops_order_number ON delivery_stops(order_number);
";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await SeedIfEmptyAsync(connection, cancellationToken);
    }

    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        var target = new NpgsqlConnectionStringBuilder(_connectionString);
        if (string.IsNullOrWhiteSpace(target.Database))
        {
            throw new InvalidOperationException("В строке подключения TradeUz не указано имя базы данных.");
        }

        var admin = new NpgsqlConnectionStringBuilder(_connectionString)
        {
            Database = "postgres",
            Pooling = false
        };

        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var existsCommand = connection.CreateCommand())
        {
            existsCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @database_name;";
            existsCommand.Parameters.AddWithValue("database_name", target.Database);
            var exists = await existsCommand.ExecuteScalarAsync(cancellationToken);
            if (exists is not null)
            {
                return;
            }
        }

        var safeDatabaseName = QuoteIdentifier(target.Database);
        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE {safeDatabaseName};";

        try
        {
            await createCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        {
            throw new InvalidOperationException($"Пользователь PostgreSQL не может создать базу '{target.Database}'. Создайте ее вручную или выдайте права CREATEDB.", ex);
        }
    }

    private static async Task SeedIfEmptyAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using (var countCommand = connection.CreateCommand())
        {
            countCommand.CommandText = "SELECT COUNT(*) FROM orders;";
            var count = (long)(await countCommand.ExecuteScalarAsync(cancellationToken) ?? 0L);
            if (count > 0)
            {
                return;
            }
        }

        var snapshot = TradeDemoSeed.Create();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var order in snapshot.Orders)
        {
            await using var orderCommand = connection.CreateCommand();
            orderCommand.Transaction = transaction;
            orderCommand.CommandText = @"
INSERT INTO orders (number, agent_name, retail_point_name, address, city, created_at, planned_delivery_at, total_amount, payment_type, comment, is_priority, box_count, status)
VALUES (@number, @agent_name, @retail_point_name, @address, @city, @created_at, @planned_delivery_at, @total_amount, @payment_type, @comment, @is_priority, @box_count, @status)
RETURNING id;";
            orderCommand.Parameters.AddWithValue("number", order.Number);
            orderCommand.Parameters.AddWithValue("agent_name", order.AgentName);
            orderCommand.Parameters.AddWithValue("retail_point_name", order.RetailPointName);
            orderCommand.Parameters.AddWithValue("address", order.Address);
            orderCommand.Parameters.AddWithValue("city", order.City);
            orderCommand.Parameters.AddWithValue("created_at", order.CreatedAt);
            orderCommand.Parameters.AddWithValue("planned_delivery_at", order.PlannedDeliveryAt);
            orderCommand.Parameters.AddWithValue("total_amount", order.TotalAmount);
            orderCommand.Parameters.AddWithValue("payment_type", order.PaymentType);
            orderCommand.Parameters.AddWithValue("comment", order.Comment);
            orderCommand.Parameters.AddWithValue("is_priority", order.IsPriority);
            orderCommand.Parameters.AddWithValue("box_count", order.BoxCount);
            orderCommand.Parameters.AddWithValue("status", (short)order.Status);

            var orderId = (long)(await orderCommand.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException("Failed to insert seed order."));

            foreach (var line in order.Lines)
            {
                await using var lineCommand = connection.CreateCommand();
                lineCommand.Transaction = transaction;
                lineCommand.CommandText = @"
INSERT INTO order_lines (order_id, product_name, quantity, unit, unit_price)
VALUES (@order_id, @product_name, @quantity, @unit, @unit_price);";
                lineCommand.Parameters.AddWithValue("order_id", orderId);
                lineCommand.Parameters.AddWithValue("product_name", line.ProductName);
                lineCommand.Parameters.AddWithValue("quantity", line.Quantity);
                lineCommand.Parameters.AddWithValue("unit", line.Unit);
                lineCommand.Parameters.AddWithValue("unit_price", line.UnitPrice);
                await lineCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        foreach (var route in snapshot.Routes)
        {
            await using var routeCommand = connection.CreateCommand();
            routeCommand.Transaction = transaction;
            routeCommand.CommandText = @"
INSERT INTO delivery_routes (route_code, region, driver_name, vehicle_number, dispatcher_name, planned_departure_at, status)
VALUES (@route_code, @region, @driver_name, @vehicle_number, @dispatcher_name, @planned_departure_at, @status)
RETURNING id;";
            routeCommand.Parameters.AddWithValue("route_code", route.RouteCode);
            routeCommand.Parameters.AddWithValue("region", route.Region);
            routeCommand.Parameters.AddWithValue("driver_name", route.DriverName);
            routeCommand.Parameters.AddWithValue("vehicle_number", route.VehicleNumber);
            routeCommand.Parameters.AddWithValue("dispatcher_name", route.DispatcherName);
            routeCommand.Parameters.AddWithValue("planned_departure_at", route.PlannedDepartureAt);
            routeCommand.Parameters.AddWithValue("status", (short)route.Status);

            var routeId = (long)(await routeCommand.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException("Failed to insert seed route."));

            foreach (var stop in route.Stops)
            {
                await using var stopCommand = connection.CreateCommand();
                stopCommand.Transaction = transaction;
                stopCommand.CommandText = @"
INSERT INTO delivery_stops (route_id, sequence, order_number, retail_point_name, address, contact_phone, planned_arrival_at, amount, status)
VALUES (@route_id, @sequence, @order_number, @retail_point_name, @address, @contact_phone, @planned_arrival_at, @amount, @status);";
                stopCommand.Parameters.AddWithValue("route_id", routeId);
                stopCommand.Parameters.AddWithValue("sequence", stop.Sequence);
                stopCommand.Parameters.AddWithValue("order_number", stop.OrderNumber);
                stopCommand.Parameters.AddWithValue("retail_point_name", stop.RetailPointName);
                stopCommand.Parameters.AddWithValue("address", stop.Address);
                stopCommand.Parameters.AddWithValue("contact_phone", stop.ContactPhone);
                stopCommand.Parameters.AddWithValue("planned_arrival_at", stop.PlannedArrivalAt);
                stopCommand.Parameters.AddWithValue("amount", stop.Amount);
                stopCommand.Parameters.AddWithValue("status", (short)stop.Status);
                await stopCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static string QuoteIdentifier(string identifier)
        => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
