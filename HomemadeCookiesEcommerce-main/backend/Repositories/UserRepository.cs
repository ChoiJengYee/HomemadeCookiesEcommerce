using HomemadeCookie.Api.Infrastructure;
using HomemadeCookie.Api.Models;
using Npgsql;

namespace HomemadeCookie.Api.Repositories;

public class UserRepository
{
    public async Task<UserEntity?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT u.user_id, u.name, u.email, u.role,
       coalesce(c.address, '') AS address,
       coalesce(c.phone_number, '') AS phone_number
FROM users u
LEFT JOIN customers c ON c.user_id = u.user_id
WHERE u.email = @email AND u.password = @password";

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
            Role = reader.GetString(3),
            Address = reader.GetString(4),
            PhoneNumber = reader.GetString(5)
        };
    }

    public async Task<UserEntity?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT u.user_id, u.name, u.email, u.role,
       coalesce(c.address, '') AS address,
       coalesce(c.phone_number, '') AS phone_number
FROM users u
LEFT JOIN customers c ON c.user_id = u.user_id
WHERE u.user_id = @userId";

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
            Role = reader.GetString(3),
            Address = reader.GetString(4),
            PhoneNumber = reader.GetString(5)
        };
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT 1 FROM users WHERE email = @email";

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", email);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken);
    }

    public async Task<UserEntity?> RegisterAsync(string name, string email, string password, string address, string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (await EmailExistsAsync(email, cancellationToken))
            return null;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string insertUserSql = @"
INSERT INTO users (name, email, password, role)
VALUES (@name, @email, @password, 'Customer')
RETURNING user_id";

            await using var insertUser = new NpgsqlCommand(insertUserSql, connection, transaction);
            insertUser.Parameters.AddWithValue("name", name);
            insertUser.Parameters.AddWithValue("email", email);
            insertUser.Parameters.AddWithValue("password", password);
            var userIdValue = await insertUser.ExecuteScalarAsync(cancellationToken);
            if (userIdValue is not int userId)
                throw new InvalidOperationException("Failed to obtain new user id.");

            const string insertCustomerSql = @"
INSERT INTO customers (user_id, address, phone_number)
VALUES (@userId, @address, @phoneNumber)";

            await using var insertCustomer = new NpgsqlCommand(insertCustomerSql, connection, transaction);
            insertCustomer.Parameters.AddWithValue("userId", userId);
            insertCustomer.Parameters.AddWithValue("address", address);
            insertCustomer.Parameters.AddWithValue("phoneNumber", phoneNumber);
            await insertCustomer.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new UserEntity
            {
                UserId = userId,
                Name = name,
                Email = email,
                Role = "Customer",
                Address = address,
                PhoneNumber = phoneNumber
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<UserEntity?> UpdateProfileAsync(int userId, string name, string address, string phoneNumber, CancellationToken cancellationToken = default)
    {
        const string updateUserSql = "UPDATE users SET name = @name WHERE user_id = @userId";
        const string updateCustomerSql = @"
INSERT INTO customers (user_id, address, phone_number)
VALUES (@userId, @address, @phoneNumber)
ON CONFLICT (user_id) DO UPDATE
SET address = excluded.address, phone_number = excluded.phone_number";

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var updateUser = new NpgsqlCommand(updateUserSql, connection, transaction);
            updateUser.Parameters.AddWithValue("name", name);
            updateUser.Parameters.AddWithValue("userId", userId);
            await updateUser.ExecuteNonQueryAsync(cancellationToken);

            await using var updateCustomer = new NpgsqlCommand(updateCustomerSql, connection, transaction);
            updateCustomer.Parameters.AddWithValue("userId", userId);
            updateCustomer.Parameters.AddWithValue("address", address);
            updateCustomer.Parameters.AddWithValue("phoneNumber", phoneNumber);
            await updateCustomer.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        return await GetByIdAsync(userId, cancellationToken);
    }

public async Task<IReadOnlyList<UserEntity>> GetAllAsync(CancellationToken cancellationToken = default)
{
    const string sql = @"
SELECT u.user_id, u.name, u.email, u.role,
       coalesce(c.address, '') AS address,
       coalesce(c.phone_number, '') AS phone_number
FROM users u
LEFT JOIN customers c ON c.user_id = u.user_id
ORDER BY u.user_id";

    await using var connection = DatabaseConnection.Instance.CreateConnection();
    await connection.OpenAsync(cancellationToken);
    await using var command = new NpgsqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync(cancellationToken);

    var users = new List<UserEntity>();
    while (await reader.ReadAsync(cancellationToken))
    {
        users.Add(new UserEntity
        {
            UserId = reader.GetInt32(0),
            Name = reader.GetString(1),
            Email = reader.GetString(2),
            Role = reader.GetString(3),
            Address = reader.GetString(4),
            PhoneNumber = reader.GetString(5)
        });
    }

    return users;
}

public async Task<bool> UpdateRoleAsync(int userId, string role, CancellationToken cancellationToken = default)
{
    const string sql = "UPDATE users SET role = @role WHERE user_id = @userId";

    await using var connection = DatabaseConnection.Instance.CreateConnection();
    await connection.OpenAsync(cancellationToken);
    await using var command = new NpgsqlCommand(sql, connection);
    command.Parameters.AddWithValue("userId", userId);
    command.Parameters.AddWithValue("role", role);
    return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
}
}
