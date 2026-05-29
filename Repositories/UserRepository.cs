using HomemadeCookie.Api.Infrastructure;
using HomemadeCookie.Api.Models;
using Npgsql;

namespace HomemadeCookie.Api.Repositories;

public class UserRepository
{
    public async Task<UserEntity?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT user_id, name, email, role FROM users WHERE email = @email AND password = @password";

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", email);
        command.Parameters.AddWithValue("password", password);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new UserEntity
        {
            UserId = reader.GetInt32(0),
            Name = reader.GetString(1),
            Email = reader.GetString(2),
            Role = reader.GetString(3)
        };
    }

    public async Task<UserEntity?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT user_id, name, email, role FROM users WHERE user_id = @userId";

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new UserEntity
        {
            UserId = reader.GetInt32(0),
            Name = reader.GetString(1),
            Email = reader.GetString(2),
            Role = reader.GetString(3)
        };
    }
}
