using HomemadeCookie.Api.Infrastructure;
using HomemadeCookie.Api.Models;
using Npgsql;

namespace HomemadeCookie.Api.Repositories;

public class OrderRepository
{
    public async Task<IReadOnlyList<OrderEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT o.order_id, o.customer_id, o.order_date, o.total_amount, o.status_id, s.status_name
            FROM orders o
            INNER JOIN order_status_lookup s ON s.status_id = o.status_id
            ORDER BY o.order_date DESC, o.order_id DESC
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var orders = new List<OrderEntity>();
        while (await reader.ReadAsync(cancellationToken))
        {
            orders.Add(MapOrder(reader));
        }

        return orders;
    }

    public async Task<IReadOnlyList<OrderEntity>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT o.order_id, o.customer_id, o.order_date, o.total_amount, o.status_id, s.status_name
            FROM orders o
            INNER JOIN order_status_lookup s ON s.status_id = o.status_id
            WHERE o.customer_id = @customerId
            ORDER BY o.order_date DESC, o.order_id DESC
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("customerId", customerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var orders = new List<OrderEntity>();
        while (await reader.ReadAsync(cancellationToken))
        {
            orders.Add(MapOrder(reader));
        }

        return orders;
    }

    public async Task<object?> GetAdminOrderDetailsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await GetByIdAsync(orderId, cancellationToken);
        if (order is null) return null;

        const string customerSql = """
            SELECT u.name, c.phone_number, c.address
            FROM orders o
            INNER JOIN users u ON u.user_id = o.customer_id
            INNER JOIN customers c ON c.user_id = o.customer_id
            WHERE o.order_id = @orderId
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var customerCommand = new NpgsqlCommand(customerSql, connection);
        customerCommand.Parameters.AddWithValue("orderId", orderId);
        await using var customerReader = await customerCommand.ExecuteReaderAsync(cancellationToken);

        string customerName = "";
        string phoneNumber = "";
        string address = "";

        if (await customerReader.ReadAsync(cancellationToken))
        {
            customerName = customerReader.GetString(0);
            phoneNumber = customerReader.GetString(1);
            address = customerReader.GetString(2);
        }

        await customerReader.CloseAsync();

        var items = await GetItemsAsync(orderId, cancellationToken);

        return new
        {
            order.OrderId,
            order.StatusName,
            order.OrderDate,
            order.TotalAmount,
            CustomerName = customerName,
            PhoneNumber = phoneNumber,
            Address = address,
            Items = items
        };
    }

    public async Task<OrderEntity?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT o.order_id, o.customer_id, o.order_date, o.total_amount, o.status_id, s.status_name
            FROM orders o
            INNER JOIN order_status_lookup s ON s.status_id = o.status_id
            WHERE o.order_id = @orderId
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("orderId", orderId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapOrder(reader);
    }

    public async Task<IReadOnlyList<OrderItemEntity>> GetItemsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT oi.order_item_id, oi.order_id, oi.cookie_id, c.name, oi.quantity, oi.price_at_purchase
            FROM order_items oi
            INNER JOIN cookies c ON c.cookie_id = oi.cookie_id
            WHERE oi.order_id = @orderId
            ORDER BY oi.order_item_id
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("orderId", orderId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<OrderItemEntity>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new OrderItemEntity
            {
                OrderItemId = reader.GetInt32(0),
                OrderId = reader.GetInt32(1),
                CookieId = reader.GetInt32(2),
                CookieName = reader.GetString(3),
                Quantity = reader.GetInt32(4),
                PriceAtPurchase = reader.GetDecimal(5)
            });
        }

        return items;
    }

    public async Task UpdateStatusAsync(int orderId, int statusId, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE orders SET status_id = @statusId WHERE order_id = @orderId";

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("statusId", statusId);
        command.Parameters.AddWithValue("orderId", orderId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> CreateOrderAsync(
        int customerId,
        decimal totalAmount,
        int statusId,
        IReadOnlyList<(int CookieId, int Quantity, decimal PriceAtPurchase)> lines,
        CancellationToken cancellationToken = default)
    {
        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string orderSql = """
                INSERT INTO orders (customer_id, total_amount, status_id)
                VALUES (@customerId, @totalAmount, @statusId)
                RETURNING order_id
                """;

            await using (var orderCommand = new NpgsqlCommand(orderSql, connection, transaction))
            {
                orderCommand.Parameters.AddWithValue("customerId", customerId);
                orderCommand.Parameters.AddWithValue("totalAmount", totalAmount);
                orderCommand.Parameters.AddWithValue("statusId", statusId);
                var orderId = Convert.ToInt32(await orderCommand.ExecuteScalarAsync(cancellationToken));

                const string itemSql = """
                    INSERT INTO order_items (order_id, cookie_id, quantity, price_at_purchase)
                    VALUES (@orderId, @cookieId, @quantity, @priceAtPurchase)
                    """;

                foreach (var line in lines)
                {
                    await using var itemCommand = new NpgsqlCommand(itemSql, connection, transaction);
                    itemCommand.Parameters.AddWithValue("orderId", orderId);
                    itemCommand.Parameters.AddWithValue("cookieId", line.CookieId);
                    itemCommand.Parameters.AddWithValue("quantity", line.Quantity);
                    itemCommand.Parameters.AddWithValue("priceAtPurchase", line.PriceAtPurchase);
                    await itemCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                return orderId;
            }
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<string?> GetCustomerEmailAsync(int customerId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT email FROM users WHERE user_id = @customerId";

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("customerId", customerId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? null : Convert.ToString(result);
    }

    public async Task CreatePaymentAsync(
        int orderId,
        string method,
        decimal amount,
        string status,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO payments (order_id, method, amount, status)
            VALUES (@orderId, @method, @amount, @status)
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("orderId", orderId);
        command.Parameters.AddWithValue("method", method);
        command.Parameters.AddWithValue("amount", amount);
        command.Parameters.AddWithValue("status", status);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<object>> GetCookieSalesAsync(
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                c.name AS cookie_name,
                SUM(oi.quantity) AS total_quantity,
                SUM(oi.quantity * oi.price_at_purchase) AS total_sales
            FROM order_items oi
            INNER JOIN orders o ON o.order_id = oi.order_id
            INNER JOIN cookies c ON c.cookie_id = oi.cookie_id
            WHERE o.status_id <> 6
            AND (@startDate IS NULL OR o.order_date >= @startDate)
            AND (@endDate IS NULL OR o.order_date <= @endDate)
            GROUP BY c.name
            ORDER BY total_sales DESC
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("startDate", startDate.HasValue ? startDate.Value : DBNull.Value);
        command.Parameters.AddWithValue("endDate", endDate.HasValue ? endDate.Value.Date.AddDays(1).AddTicks(-1) : DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<object>();

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new
            {
                CookieName = reader.GetString(0),
                TotalQuantity = reader.GetInt32(1),
                TotalSales = reader.GetDecimal(2)
            });
        }

        return result;
    }

    private static OrderEntity MapOrder(NpgsqlDataReader reader) => new()
    {
        OrderId = reader.GetInt32(0),
        CustomerId = reader.GetInt32(1),
        OrderDate = reader.GetDateTime(2),
        TotalAmount = reader.GetDecimal(3),
        StatusId = reader.GetInt32(4),
        StatusName = reader.GetString(5)
    };
}
