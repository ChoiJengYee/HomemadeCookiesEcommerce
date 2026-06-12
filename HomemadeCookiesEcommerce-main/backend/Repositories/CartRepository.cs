using HomemadeCookie.Api.Infrastructure;
using HomemadeCookie.Api.Models;
using Npgsql;

namespace HomemadeCookie.Api.Repositories;

public class CartRepository
{
    public async Task<int> EnsureCartAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var cartId = await GetCartIdByCustomerAsync(customerId, cancellationToken);
        if (cartId.HasValue)
            return cartId.Value;

        const string insertSql = """
            INSERT INTO carts (customer_id)
            VALUES (@customerId)
            RETURNING cart_id
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(insertSql, connection);
        command.Parameters.AddWithValue("customerId", customerId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<int?> GetCartIdByCustomerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT cart_id FROM carts WHERE customer_id = @customerId";

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("customerId", customerId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null ? null : Convert.ToInt32(result);
    }

    public async Task<IReadOnlyList<CartItemEntity>> GetItemsAsync(int customerId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ci.cart_item_id, ci.cart_id, ci.cookie_id, c.name, ci.quantity, ci.unit_price
            FROM cart_items ci
            INNER JOIN carts cart ON cart.cart_id = ci.cart_id
            INNER JOIN cookies c ON c.cookie_id = ci.cookie_id
            WHERE cart.customer_id = @customerId
            ORDER BY ci.cart_item_id
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("customerId", customerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<CartItemEntity>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new CartItemEntity
            {
                CartItemId = reader.GetInt32(0),
                CartId = reader.GetInt32(1),
                CookieId = reader.GetInt32(2),
                CookieName = reader.GetString(3),
                Quantity = reader.GetInt32(4),
                UnitPrice = reader.GetDecimal(5)
            });
        }

        return items;
    }

    public async Task AddItemAsync(int customerId, int cookieId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        var cookie = await new CookieRepository().GetByIdAsync(cookieId, cancellationToken)
            ?? throw new InvalidOperationException($"Cookie {cookieId} not found.");

        if (quantity > cookie.Stock)
            throw new InvalidOperationException($"Insufficient stock for {cookie.Name}.");

        var cartId = await EnsureCartAsync(customerId, cancellationToken);

        const string upsertSql = """
            INSERT INTO cart_items (cart_id, cookie_id, quantity, unit_price)
            VALUES (@cartId, @cookieId, @quantity, @unitPrice)
            ON CONFLICT (cart_id, cookie_id)
            DO UPDATE SET
                quantity = cart_items.quantity + EXCLUDED.quantity,
                unit_price = EXCLUDED.unit_price
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(upsertSql, connection);
        command.Parameters.AddWithValue("cartId", cartId);
        command.Parameters.AddWithValue("cookieId", cookieId);
        command.Parameters.AddWithValue("quantity", quantity);
        command.Parameters.AddWithValue("unitPrice", cookie.Price);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetItemQuantityAsync(int customerId, int cookieId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        var cookie = await new CookieRepository().GetByIdAsync(cookieId, cancellationToken)
            ?? throw new InvalidOperationException($"Cookie {cookieId} not found.");

        if (quantity > cookie.Stock)
            throw new InvalidOperationException($"Insufficient stock for {cookie.Name}.");

        var cartId = await EnsureCartAsync(customerId, cancellationToken);

        const string updateSql = """
            UPDATE cart_items ci
            SET quantity = @quantity, unit_price = @unitPrice
            FROM carts cart
            WHERE ci.cart_id = cart.cart_id
              AND cart.customer_id = @customerId
              AND ci.cookie_id = @cookieId
              AND ci.cart_id = @cartId
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(updateSql, connection);
        command.Parameters.AddWithValue("quantity", quantity);
        command.Parameters.AddWithValue("unitPrice", cookie.Price);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("cookieId", cookieId);
        command.Parameters.AddWithValue("cartId", cartId);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);

        if (rows == 0)
        {
            const string insertSql = """
                INSERT INTO cart_items (cart_id, cookie_id, quantity, unit_price)
                VALUES (@cartId, @cookieId, @quantity, @unitPrice)
                """;

            await using var insertCommand = new NpgsqlCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("cartId", cartId);
            insertCommand.Parameters.AddWithValue("cookieId", cookieId);
            insertCommand.Parameters.AddWithValue("quantity", quantity);
            insertCommand.Parameters.AddWithValue("unitPrice", cookie.Price);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task RemoveItemAsync(int customerId, int cookieId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM cart_items ci
            USING carts cart
            WHERE ci.cart_id = cart.cart_id
              AND cart.customer_id = @customerId
              AND ci.cookie_id = @cookieId
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("cookieId", cookieId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ClearAsync(int customerId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM cart_items ci
            USING carts cart
            WHERE ci.cart_id = cart.cart_id
              AND cart.customer_id = @customerId
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("customerId", customerId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
