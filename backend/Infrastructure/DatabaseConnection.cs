using Npgsql;

namespace HomemadeCookie.Api.Infrastructure;

/// <summary>
/// Thread-safe Singleton — sole entry point for PostgreSQL connections.
/// </summary>
public sealed class DatabaseConnection
{
    private static readonly Lazy<DatabaseConnection> InstanceHolder = new(() => new DatabaseConnection());
    private string? _connectionString;

    private DatabaseConnection()
    {
    }

    public static DatabaseConnection Instance => InstanceHolder.Value;

    public void Configure(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));

        _connectionString = connectionString;
    }

    public NpgsqlConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("DatabaseConnection is not configured. Call Configure() at startup.");

        return new NpgsqlConnection(_connectionString);
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }
}
