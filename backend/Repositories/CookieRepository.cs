using HomemadeCookie.Api.Infrastructure;
using HomemadeCookie.Api.Models;
using Npgsql;

namespace HomemadeCookie.Api.Repositories;

public class CookieRepository
{
    public async Task<IReadOnlyList<CookieEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT c.cookie_id, c.name, c.description, c.image_url, c.price, c.stock, c.category_id
            FROM cookies c
            ORDER BY c.cookie_id
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var cookies = new List<CookieEntity>();
        while (await reader.ReadAsync(cancellationToken))
        {
            cookies.Add(MapCookie(reader));
        }

        return cookies;
    }

    public async Task<CookieEntity?> GetByIdAsync(int cookieId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT c.cookie_id, c.name, c.description, c.image_url, c.price, c.stock, c.category_id
            FROM cookies c
            WHERE c.cookie_id = @cookieId
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("cookieId", cookieId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapCookie(reader);
    }

    public async Task<int> InsertAsync(CookieEntity cookie, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO cookies (name, description, image_url, price, stock, category_id)
            VALUES (@name, @description, @imageUrl, @price, @stock, @categoryId)
            RETURNING cookie_id
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("name", cookie.Name);
        command.Parameters.AddWithValue("description", (object?)cookie.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("imageUrl", (object?)cookie.ImageUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("price", cookie.Price);
        command.Parameters.AddWithValue("stock", cookie.Stock);
        command.Parameters.AddWithValue("categoryId", cookie.CategoryId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task UpdateStockAsync(int cookieId, int stock, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE cookies SET stock = @stock WHERE cookie_id = @cookieId";

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("stock", stock);
        command.Parameters.AddWithValue("cookieId", cookieId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ReduceStockAsync(
        IReadOnlyList<(int CookieId, int Quantity)> lines,
        CancellationToken cancellationToken = default)
    {
        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string sql = """
                UPDATE cookies
                SET stock = stock - @quantity
                WHERE cookie_id = @cookieId AND stock >= @quantity
                """;

            foreach (var (cookieId, quantity) in lines)
            {
                await using var command = new NpgsqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("quantity", quantity);
                command.Parameters.AddWithValue("cookieId", cookieId);

                var rows = await command.ExecuteNonQueryAsync(cancellationToken);

                if (rows == 0)
                    throw new InvalidOperationException($"Insufficient stock for cookie {cookieId}.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int cookieId, UpdateCookieRequest request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE cookies
            SET name = @name,
                description = @description,
                image_url = @imageUrl,
                price = @price,
                stock = @stock,
                category_id = @categoryId
            WHERE cookie_id = @cookieId
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("name", request.Name);
        command.Parameters.AddWithValue("description", (object?)request.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("imageUrl", (object?)request.ImageUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("price", request.Price);
        command.Parameters.AddWithValue("stock", request.Stock);
        command.Parameters.AddWithValue("categoryId", request.CategoryId);
        command.Parameters.AddWithValue("cookieId", cookieId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int cookieId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM cookies WHERE cookie_id = @cookieId";

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("cookieId", cookieId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static CookieEntity MapCookie(NpgsqlDataReader reader) => new()
    {
        CookieId = reader.GetInt32(0),
        Name = reader.GetString(1),
        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
        ImageUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
        Price = reader.GetDecimal(4),
        Stock = reader.GetInt32(5),
        CategoryId = reader.GetInt32(6)
    };
}