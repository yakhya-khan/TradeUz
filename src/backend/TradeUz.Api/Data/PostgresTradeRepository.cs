using Npgsql;
using TradeUz.Contracts;

namespace TradeUz.Api.Data;

public sealed class PostgresTradeRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresTradeRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<TradeOrder>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT o.id,
       o.number,
       o.agent_name,
       o.retail_point_name,
       o.address,
       o.city,
       o.created_at,
       o.planned_delivery_at,
       o.total_amount,
       o.payment_type,
       o.comment,
       o.is_priority,
       o.box_count,
       o.status,
       ol.product_name,
       ol.quantity,
       ol.unit,
       ol.unit_price
FROM orders o
LEFT JOIN order_lines ol ON ol.order_id = o.id
ORDER BY o.is_priority DESC, o.planned_delivery_at, o.number, ol.id;";

        var orders = new Dictionary<long, TradeOrder>();
        var orderSequence = new List<TradeOrder>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var orderId = reader.GetInt64(0);
            if (!orders.TryGetValue(orderId, out var order))
            {
                order = new TradeOrder
                {
                    Number = reader.GetString(1),
                    AgentName = reader.GetString(2),
                    RetailPointName = reader.GetString(3),
                    Address = reader.GetString(4),
                    City = reader.GetString(5),
                    CreatedAt = reader.GetDateTime(6),
                    PlannedDeliveryAt = reader.GetDateTime(7),
                    TotalAmount = reader.GetDecimal(8),
                    PaymentType = reader.GetString(9),
                    Comment = reader.GetString(10),
                    IsPriority = reader.GetBoolean(11),
                    BoxCount = reader.GetInt32(12),
                    Status = (OrderStatus)reader.GetInt16(13)
                };

                orders[orderId] = order;
                orderSequence.Add(order);
            }

            if (!reader.IsDBNull(14))
            {
                order.Lines.Add(new OrderLine
                {
                    ProductName = reader.GetString(14),
                    Quantity = reader.GetInt32(15),
                    Unit = reader.GetString(16),
                    UnitPrice = reader.GetDecimal(17)
                });
            }
        }

        return orderSequence;
    }

    public async Task<IReadOnlyList<DeliveryRoute>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT r.id,
       r.route_code,
       r.region,
       r.driver_name,
       r.vehicle_number,
       r.dispatcher_name,
       r.planned_departure_at,
       r.status,
       s.sequence,
       s.order_number,
       s.retail_point_name,
       s.address,
       s.contact_phone,
       s.planned_arrival_at,
       s.amount,
       s.status
FROM delivery_routes r
LEFT JOIN delivery_stops s ON s.route_id = r.id
ORDER BY CASE r.status WHEN 1 THEN 0 WHEN 3 THEN 1 ELSE 2 END, r.planned_departure_at, r.route_code, s.sequence;";

        var routes = new Dictionary<long, DeliveryRoute>();
        var routeSequence = new List<DeliveryRoute>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var routeId = reader.GetInt64(0);
            if (!routes.TryGetValue(routeId, out var route))
            {
                route = new DeliveryRoute
                {
                    RouteCode = reader.GetString(1),
                    Region = reader.GetString(2),
                    DriverName = reader.GetString(3),
                    VehicleNumber = reader.GetString(4),
                    DispatcherName = reader.GetString(5),
                    PlannedDepartureAt = reader.GetDateTime(6),
                    Status = (DeliveryRouteStatus)reader.GetInt16(7)
                };

                routes[routeId] = route;
                routeSequence.Add(route);
            }

            if (!reader.IsDBNull(8))
            {
                route.Stops.Add(new DeliveryStop
                {
                    Sequence = reader.GetInt32(8),
                    OrderNumber = reader.GetString(9),
                    RetailPointName = reader.GetString(10),
                    Address = reader.GetString(11),
                    ContactPhone = reader.GetString(12),
                    PlannedArrivalAt = reader.GetDateTime(13),
                    Amount = reader.GetDecimal(14),
                    Status = (DeliveryStopStatus)reader.GetInt16(15)
                });
            }
        }

        return routeSequence;
    }

    public async Task<TradeOrder> CreateOrderAsync(CreateTradeOrderRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var number = await GenerateOrderNumberAsync(connection, transaction, request.PlannedDeliveryAt, cancellationToken);
        var createdAt = DateTime.Now;
        var totalAmount = request.Lines.Sum(line => line.Quantity * line.UnitPrice);

        await using var orderCommand = connection.CreateCommand();
        orderCommand.Transaction = transaction;
        orderCommand.CommandText = @"
INSERT INTO orders (number, agent_name, retail_point_name, address, city, created_at, planned_delivery_at, total_amount, payment_type, comment, is_priority, box_count, status)
VALUES (@number, @agent_name, @retail_point_name, @address, @city, @created_at, @planned_delivery_at, @total_amount, @payment_type, @comment, @is_priority, @box_count, @status)
RETURNING id;";
        orderCommand.Parameters.AddWithValue("number", number);
        orderCommand.Parameters.AddWithValue("agent_name", request.AgentName.Trim());
        orderCommand.Parameters.AddWithValue("retail_point_name", request.RetailPointName.Trim());
        orderCommand.Parameters.AddWithValue("address", request.Address.Trim());
        orderCommand.Parameters.AddWithValue("city", string.IsNullOrWhiteSpace(request.City) ? "Toshkent" : request.City.Trim());
        orderCommand.Parameters.AddWithValue("created_at", createdAt);
        orderCommand.Parameters.AddWithValue("planned_delivery_at", request.PlannedDeliveryAt);
        orderCommand.Parameters.AddWithValue("total_amount", totalAmount);
        orderCommand.Parameters.AddWithValue("payment_type", request.PaymentType.Trim());
        orderCommand.Parameters.AddWithValue("comment", request.Comment.Trim());
        orderCommand.Parameters.AddWithValue("is_priority", request.IsPriority);
        orderCommand.Parameters.AddWithValue("box_count", request.BoxCount);
        orderCommand.Parameters.AddWithValue("status", (short)OrderStatus.New);

        var orderId = (long)(await orderCommand.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Failed to create order."));

        foreach (var line in request.Lines)
        {
            await using var lineCommand = connection.CreateCommand();
            lineCommand.Transaction = transaction;
            lineCommand.CommandText = @"
INSERT INTO order_lines (order_id, product_name, quantity, unit, unit_price)
VALUES (@order_id, @product_name, @quantity, @unit, @unit_price);";
            lineCommand.Parameters.AddWithValue("order_id", orderId);
            lineCommand.Parameters.AddWithValue("product_name", line.ProductName.Trim());
            lineCommand.Parameters.AddWithValue("quantity", line.Quantity);
            lineCommand.Parameters.AddWithValue("unit", line.Unit.Trim());
            lineCommand.Parameters.AddWithValue("unit_price", line.UnitPrice);
            await lineCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        var order = new TradeOrder
        {
            Number = number,
            AgentName = request.AgentName.Trim(),
            RetailPointName = request.RetailPointName.Trim(),
            Address = request.Address.Trim(),
            City = string.IsNullOrWhiteSpace(request.City) ? "Toshkent" : request.City.Trim(),
            CreatedAt = createdAt,
            PlannedDeliveryAt = request.PlannedDeliveryAt,
            TotalAmount = totalAmount,
            PaymentType = request.PaymentType.Trim(),
            Comment = request.Comment.Trim(),
            IsPriority = request.IsPriority,
            BoxCount = request.BoxCount,
            Status = OrderStatus.New
        };

        foreach (var line in request.Lines)
        {
            order.Lines.Add(new OrderLine
            {
                ProductName = line.ProductName.Trim(),
                Quantity = line.Quantity,
                Unit = line.Unit.Trim(),
                UnitPrice = line.UnitPrice
            });
        }

        return order;
    }

    public async Task<bool> AdvanceOrderAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        long orderId;
        OrderStatus currentStatus;
        await using (var orderCommand = connection.CreateCommand())
        {
            orderCommand.Transaction = transaction;
            orderCommand.CommandText = "SELECT id, status FROM orders WHERE number = @number;";
            orderCommand.Parameters.AddWithValue("number", orderNumber);
            await using var reader = await orderCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return false;
            }

            orderId = reader.GetInt64(0);
            currentStatus = (OrderStatus)reader.GetInt16(1);
        }

        var nextStatus = currentStatus switch
        {
            OrderStatus.New => OrderStatus.Approved,
            OrderStatus.Approved => OrderStatus.Picking,
            OrderStatus.Picking => OrderStatus.OnRoute,
            OrderStatus.Delayed => OrderStatus.OnRoute,
            OrderStatus.OnRoute => OrderStatus.Delivered,
            _ => currentStatus
        };

        await using (var updateOrder = connection.CreateCommand())
        {
            updateOrder.Transaction = transaction;
            updateOrder.CommandText = "UPDATE orders SET status = @status WHERE id = @id;";
            updateOrder.Parameters.AddWithValue("status", (short)nextStatus);
            updateOrder.Parameters.AddWithValue("id", orderId);
            await updateOrder.ExecuteNonQueryAsync(cancellationToken);
        }

        long? routeId = null;
        DeliveryStopStatus? stopStatus = null;
        await using (var stopCommand = connection.CreateCommand())
        {
            stopCommand.Transaction = transaction;
            stopCommand.CommandText = "SELECT route_id, status FROM delivery_stops WHERE order_number = @number ORDER BY sequence LIMIT 1;";
            stopCommand.Parameters.AddWithValue("number", orderNumber);
            await using var reader = await stopCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                routeId = reader.GetInt64(0);
                stopStatus = (DeliveryStopStatus)reader.GetInt16(1);
            }
        }

        if (routeId.HasValue)
        {
            if (nextStatus == OrderStatus.OnRoute && stopStatus == DeliveryStopStatus.Planned)
            {
                await using var updateStop = connection.CreateCommand();
                updateStop.Transaction = transaction;
                updateStop.CommandText = "UPDATE delivery_stops SET status = @status WHERE order_number = @number;";
                updateStop.Parameters.AddWithValue("status", (short)DeliveryStopStatus.OnRoute);
                updateStop.Parameters.AddWithValue("number", orderNumber);
                await updateStop.ExecuteNonQueryAsync(cancellationToken);
            }

            if (nextStatus == OrderStatus.Delivered)
            {
                await using var deliveredStop = connection.CreateCommand();
                deliveredStop.Transaction = transaction;
                deliveredStop.CommandText = "UPDATE delivery_stops SET status = @status WHERE order_number = @number;";
                deliveredStop.Parameters.AddWithValue("status", (short)DeliveryStopStatus.Delivered);
                deliveredStop.Parameters.AddWithValue("number", orderNumber);
                await deliveredStop.ExecuteNonQueryAsync(cancellationToken);
            }

            await UpdateRouteStatusAsync(connection, transaction, routeId.Value, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> StartRouteAsync(string routeCode, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var routeId = await GetActiveRouteIdAsync(connection, transaction, routeCode, cancellationToken);
        if (!routeId.HasValue)
        {
            return false;
        }

        await StartRouteInternalAsync(connection, transaction, routeId.Value, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CompleteNextStopAsync(string routeCode, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        long routeId;
        DeliveryRouteStatus routeStatus;
        await using (var routeCommand = connection.CreateCommand())
        {
            routeCommand.Transaction = transaction;
            routeCommand.CommandText = "SELECT id, status FROM delivery_routes WHERE route_code = @route_code;";
            routeCommand.Parameters.AddWithValue("route_code", routeCode);
            await using var reader = await routeCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return false;
            }

            routeId = reader.GetInt64(0);
            routeStatus = (DeliveryRouteStatus)reader.GetInt16(1);
        }

        if (routeStatus == DeliveryRouteStatus.Completed)
        {
            return false;
        }

        if (routeStatus == DeliveryRouteStatus.Planned)
        {
            await StartRouteInternalAsync(connection, transaction, routeId, cancellationToken);
        }

        long? stopId = null;
        string? orderNumber = null;
        await using (var stopCommand = connection.CreateCommand())
        {
            stopCommand.Transaction = transaction;
            stopCommand.CommandText = @"
SELECT id, order_number
FROM delivery_stops
WHERE route_id = @route_id AND status <> @delivered_status
ORDER BY sequence
LIMIT 1;";
            stopCommand.Parameters.AddWithValue("route_id", routeId);
            stopCommand.Parameters.AddWithValue("delivered_status", (short)DeliveryStopStatus.Delivered);
            await using var reader = await stopCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                stopId = reader.GetInt64(0);
                orderNumber = reader.GetString(1);
            }
        }

        if (!stopId.HasValue)
        {
            await using var completeRoute = connection.CreateCommand();
            completeRoute.Transaction = transaction;
            completeRoute.CommandText = "UPDATE delivery_routes SET status = @status WHERE id = @id;";
            completeRoute.Parameters.AddWithValue("status", (short)DeliveryRouteStatus.Completed);
            completeRoute.Parameters.AddWithValue("id", routeId);
            await completeRoute.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        await using (var updateStop = connection.CreateCommand())
        {
            updateStop.Transaction = transaction;
            updateStop.CommandText = "UPDATE delivery_stops SET status = @status WHERE id = @id;";
            updateStop.Parameters.AddWithValue("status", (short)DeliveryStopStatus.Delivered);
            updateStop.Parameters.AddWithValue("id", stopId.Value);
            await updateStop.ExecuteNonQueryAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(orderNumber))
        {
            await using var updateOrder = connection.CreateCommand();
            updateOrder.Transaction = transaction;
            updateOrder.CommandText = "UPDATE orders SET status = @status WHERE number = @number;";
            updateOrder.Parameters.AddWithValue("status", (short)OrderStatus.Delivered);
            updateOrder.Parameters.AddWithValue("number", orderNumber);
            await updateOrder.ExecuteNonQueryAsync(cancellationToken);
        }

        long? nextStopId = null;
        await using (var nextStopCommand = connection.CreateCommand())
        {
            nextStopCommand.Transaction = transaction;
            nextStopCommand.CommandText = @"
SELECT id
FROM delivery_stops
WHERE route_id = @route_id AND status = @planned_status
ORDER BY sequence
LIMIT 1;";
            nextStopCommand.Parameters.AddWithValue("route_id", routeId);
            nextStopCommand.Parameters.AddWithValue("planned_status", (short)DeliveryStopStatus.Planned);
            var scalar = await nextStopCommand.ExecuteScalarAsync(cancellationToken);
            if (scalar is long nextId)
            {
                nextStopId = nextId;
            }
        }

        if (nextStopId.HasValue)
        {
            await using var promoteStop = connection.CreateCommand();
            promoteStop.Transaction = transaction;
            promoteStop.CommandText = "UPDATE delivery_stops SET status = @status WHERE id = @id;";
            promoteStop.Parameters.AddWithValue("status", (short)DeliveryStopStatus.OnRoute);
            promoteStop.Parameters.AddWithValue("id", nextStopId.Value);
            await promoteStop.ExecuteNonQueryAsync(cancellationToken);
        }

        await UpdateRouteStatusAsync(connection, transaction, routeId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static async Task<string> GenerateOrderNumberAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, DateTime plannedDate, CancellationToken cancellationToken)
    {
        var prefix = $"ORD-{plannedDate:yyMMdd}";
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
SELECT COALESCE(MAX(CAST(RIGHT(number, 3) AS INTEGER)), 0)
FROM orders
WHERE number LIKE @prefix || '-%';";
        command.Parameters.AddWithValue("prefix", prefix);
        var nextNumber = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0) + 1;
        return $"{prefix}-{nextNumber:000}";
    }

    private static async Task<long?> GetActiveRouteIdAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string routeCode, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT id, status FROM delivery_routes WHERE route_code = @route_code;";
        command.Parameters.AddWithValue("route_code", routeCode);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var status = (DeliveryRouteStatus)reader.GetInt16(1);
        return status == DeliveryRouteStatus.Completed ? null : reader.GetInt64(0);
    }

    private static async Task StartRouteInternalAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, long routeId, CancellationToken cancellationToken)
    {
        await using (var updateRoute = connection.CreateCommand())
        {
            updateRoute.Transaction = transaction;
            updateRoute.CommandText = "UPDATE delivery_routes SET status = @status WHERE id = @id;";
            updateRoute.Parameters.AddWithValue("status", (short)DeliveryRouteStatus.OnRoute);
            updateRoute.Parameters.AddWithValue("id", routeId);
            await updateRoute.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var updateStops = connection.CreateCommand())
        {
            updateStops.Transaction = transaction;
            updateStops.CommandText = @"
UPDATE delivery_stops
SET status = @status
WHERE route_id = @route_id AND status = @planned_status;";
            updateStops.Parameters.AddWithValue("status", (short)DeliveryStopStatus.OnRoute);
            updateStops.Parameters.AddWithValue("route_id", routeId);
            updateStops.Parameters.AddWithValue("planned_status", (short)DeliveryStopStatus.Planned);
            await updateStops.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var updateOrders = connection.CreateCommand())
        {
            updateOrders.Transaction = transaction;
            updateOrders.CommandText = @"
UPDATE orders o
SET status = @status
FROM delivery_stops s
WHERE s.route_id = @route_id AND s.order_number = o.number AND o.status <> @delivered_status;";
            updateOrders.Parameters.AddWithValue("status", (short)OrderStatus.OnRoute);
            updateOrders.Parameters.AddWithValue("route_id", routeId);
            updateOrders.Parameters.AddWithValue("delivered_status", (short)OrderStatus.Delivered);
            await updateOrders.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task UpdateRouteStatusAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, long routeId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
UPDATE delivery_routes r
SET status = CASE
    WHEN NOT EXISTS (
        SELECT 1 FROM delivery_stops s
        WHERE s.route_id = r.id AND s.status <> @delivered_status
    ) THEN @completed_status
    WHEN EXISTS (
        SELECT 1 FROM delivery_stops s
        WHERE s.route_id = r.id AND s.status = @delayed_status
    ) THEN @route_delayed_status
    WHEN EXISTS (
        SELECT 1 FROM delivery_stops s
        WHERE s.route_id = r.id AND s.status IN (@on_route_status, @delivered_status)
    ) THEN @on_route_route_status
    ELSE @planned_route_status
END
WHERE r.id = @route_id;";
        command.Parameters.AddWithValue("delivered_status", (short)DeliveryStopStatus.Delivered);
        command.Parameters.AddWithValue("completed_status", (short)DeliveryRouteStatus.Completed);
        command.Parameters.AddWithValue("delayed_status", (short)DeliveryStopStatus.Delayed);
        command.Parameters.AddWithValue("route_delayed_status", (short)DeliveryRouteStatus.Delayed);
        command.Parameters.AddWithValue("on_route_status", (short)DeliveryStopStatus.OnRoute);
        command.Parameters.AddWithValue("on_route_route_status", (short)DeliveryRouteStatus.OnRoute);
        command.Parameters.AddWithValue("planned_route_status", (short)DeliveryRouteStatus.Planned);
        command.Parameters.AddWithValue("route_id", routeId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
