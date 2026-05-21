using HomemadeCookie.Api.Infrastructure;
using HomemadeCookie.Api.Models;
using Npgsql;

namespace HomemadeCookie.Api.Repositories;

public class WishlistRepository
{
    public async Task<int> EnsureWishlistAsync(int customerId, CancellationToken cancellationToken = default)
    {
        const string selectSql = "SELECT wishlist_id FROM wishlists WHERE customer_id = @customerId";

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using (var selectCommand = new NpgsqlCommand(selectSql, connection))
        {
            selectCommand.Parameters.AddWithValue("customerId", customerId);
            var existing = await selectCommand.ExecuteScalarAsync(cancellationToken);
            if (existing is not null)
                return Convert.ToInt32(existing);
        }

        const string insertSql = """
            INSERT INTO wishlists (customer_id)
            VALUES (@customerId)
            RETURNING wishlist_id
            """;

        await using var insertCommand = new NpgsqlCommand(insertSql, connection);
        insertCommand.Parameters.AddWithValue("customerId", customerId);
        return Convert.ToInt32(await insertCommand.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<WishlistItemEntity>> GetItemsAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT wi.wishlist_item_id, wi.wishlist_id, wi.cookie_id, c.name, c.price, c.stock, wi.added_at
            FROM wishlist_items wi
            INNER JOIN wishlists w ON w.wishlist_id = wi.wishlist_id
            INNER JOIN cookies c ON c.cookie_id = wi.cookie_id
            WHERE w.customer_id = @customerId
            ORDER BY wi.added_at DESC
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("customerId", customerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<WishlistItemEntity>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new WishlistItemEntity
            {
                WishlistItemId = reader.GetInt32(0),
                WishlistId = reader.GetInt32(1),
                CookieId = reader.GetInt32(2),
                CookieName = reader.GetString(3),
                Price = reader.GetDecimal(4),
                Stock = reader.GetInt32(5),
                AddedAt = reader.GetDateTime(6)
            });
        }

        return items;
    }

    public async Task AddItemAsync(int customerId, int cookieId, CancellationToken cancellationToken = default)
    {
        var wishlistId = await EnsureWishlistAsync(customerId, cancellationToken);

        const string sql = """
            INSERT INTO wishlist_items (wishlist_id, cookie_id)
            VALUES (@wishlistId, @cookieId)
            ON CONFLICT (wishlist_id, cookie_id) DO NOTHING
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("wishlistId", wishlistId);
        command.Parameters.AddWithValue("cookieId", cookieId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RemoveItemAsync(int customerId, int cookieId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM wishlist_items wi
            USING wishlists w
            WHERE wi.wishlist_id = w.wishlist_id
              AND w.customer_id = @customerId
              AND wi.cookie_id = @cookieId
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("cookieId", cookieId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
