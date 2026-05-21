using HomemadeCookie.Api.Infrastructure;
using HomemadeCookie.Api.Models;
using Npgsql;

namespace HomemadeCookie.Api.Repositories;

public class UserRepository
{
    public async Task<UserAccount?> ValidateLoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.user_id, u.name, u.email, u.role, c.address, c.phone_number
            FROM users u
            LEFT JOIN customers c ON c.user_id = u.user_id
            WHERE LOWER(u.email) = LOWER(@email) AND u.password = @password
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", email);
        command.Parameters.AddWithValue("password", password);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new UserAccount
        {
            UserId = reader.GetInt32(0),
            Name = reader.GetString(1),
            Email = reader.GetString(2),
            Role = reader.GetString(3),
            Address = reader.IsDBNull(4) ? null : reader.GetString(4),
            PhoneNumber = reader.IsDBNull(5) ? null : reader.GetString(5)
        };
    }

    public async Task<UserAccount?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.user_id, u.name, u.email, u.role, c.address, c.phone_number
            FROM users u
            LEFT JOIN customers c ON c.user_id = u.user_id
            WHERE u.user_id = @userId
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new UserAccount
        {
            UserId = reader.GetInt32(0),
            Name = reader.GetString(1),
            Email = reader.GetString(2),
            Role = reader.GetString(3),
            Address = reader.IsDBNull(4) ? null : reader.GetString(4),
            PhoneNumber = reader.IsDBNull(5) ? null : reader.GetString(5)
        };
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT 1 FROM users WHERE LOWER(email) = LOWER(@email)";

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", email);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    public async Task<int> RegisterCustomerAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string userSql = """
                INSERT INTO users (name, email, password, role)
                VALUES (@name, @email, @password, 'Customer')
                RETURNING user_id
                """;

            await using var userCommand = new NpgsqlCommand(userSql, connection, transaction);
            userCommand.Parameters.AddWithValue("name", request.Name);
            userCommand.Parameters.AddWithValue("email", request.Email);
            userCommand.Parameters.AddWithValue("password", request.Password);
            var userId = Convert.ToInt32(await userCommand.ExecuteScalarAsync(cancellationToken));

            const string customerSql = """
                INSERT INTO customers (user_id, address, phone_number)
                VALUES (@userId, @address, @phone)
                """;

            await using var customerCommand = new NpgsqlCommand(customerSql, connection, transaction);
            customerCommand.Parameters.AddWithValue("userId", userId);
            customerCommand.Parameters.AddWithValue("address", request.Address);
            customerCommand.Parameters.AddWithValue("phone", request.PhoneNumber);
            await customerCommand.ExecuteNonQueryAsync(cancellationToken);

            const string cartSql = "INSERT INTO carts (customer_id) VALUES (@userId)";
            await using var cartCommand = new NpgsqlCommand(cartSql, connection, transaction);
            cartCommand.Parameters.AddWithValue("userId", userId);
            await cartCommand.ExecuteNonQueryAsync(cancellationToken);

            const string wishlistSql = "INSERT INTO wishlists (customer_id) VALUES (@userId)";
            await using var wishlistCommand = new NpgsqlCommand(wishlistSql, connection, transaction);
            wishlistCommand.Parameters.AddWithValue("userId", userId);
            await wishlistCommand.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return userId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
