using HomemadeCookie.Api.Infrastructure;
using HomemadeCookie.Api.Models;
using Npgsql;

namespace HomemadeCookie.Api.Repositories;

public class CategoryRepository
{
    public async Task<IReadOnlyList<CategoryEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT category_id, name FROM categories ORDER BY category_id";

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var categories = new List<CategoryEntity>();
        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(new CategoryEntity
            {
                CategoryId = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        return categories;
    }

    public async Task<int> InsertAsync(string name, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO categories (name) VALUES (@name) RETURNING category_id";

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("name", name);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateAsync(int categoryId, string name, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE categories SET name = @name WHERE category_id = @categoryId";

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("categoryId", categoryId);
        command.Parameters.AddWithValue("name", name);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM categories WHERE category_id = @categoryId";

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("categoryId", categoryId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
}
