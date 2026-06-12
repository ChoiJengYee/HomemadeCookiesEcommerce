using HomemadeCookie.Api.Infrastructure;
using HomemadeCookie.Api.Models;
using HomemadeCookie.Api.DTOs;
using Npgsql;

namespace HomemadeCookie.Api.Repositories;

public class ReviewRepository
{
    public async Task<bool> CanReviewAsync(int orderId, int customerId, int cookieId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM orders o
            INNER JOIN order_items oi ON oi.order_id = o.order_id
            WHERE o.order_id = @orderId
              AND o.customer_id = @customerId
              AND oi.cookie_id = @cookieId
              AND o.status_id = @completedStatus
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("orderId", orderId);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("cookieId", cookieId);
        command.Parameters.AddWithValue("completedStatus", OrderStatusIds.Completed);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    public async Task<int> CreateAsync(CreateReviewRequest request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO reviews (order_id, customer_id, cookie_id, rating, comment)
            VALUES (@orderId, @customerId, @cookieId, @rating, @comment)
            RETURNING review_id
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("orderId", request.OrderId);
        command.Parameters.AddWithValue("customerId", request.CustomerId);
        command.Parameters.AddWithValue("cookieId", request.CookieId);
        command.Parameters.AddWithValue("rating", request.Rating);
        command.Parameters.AddWithValue("comment", (object?)request.Comment ?? DBNull.Value);

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<ReviewDisplayDto>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                r.review_id,
                r.order_id,
                r.customer_id,
                r.cookie_id,
                c.name AS cookie_name,
                u.name AS customer_name,
                r.rating,
                r.comment,
                r.created_at
            FROM reviews r
            INNER JOIN cookies c ON c.cookie_id = r.cookie_id
            INNER JOIN users u ON u.user_id = r.customer_id
            WHERE r.order_id = @orderId
            ORDER BY r.created_at DESC
        """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("orderId", orderId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var reviews = new List<ReviewDisplayDto>();

        while (await reader.ReadAsync(cancellationToken))
        {
            reviews.Add(new ReviewDisplayDto
            {
                ReviewId = reader.GetInt32(0),
                OrderId = reader.GetInt32(1),
                CustomerId = reader.GetInt32(2),
                CookieId = reader.GetInt32(3),
                CookieName = reader.GetString(4),
                CustomerName = reader.GetString(5),
                Rating = reader.GetInt32(6),
                Comment = reader.IsDBNull(7) ? null : reader.GetString(7),
                CreatedAt = reader.GetDateTime(8)
            });
        }

        return reviews;
    }

    public async Task<IReadOnlyList<ReviewDisplayDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT r.review_id, r.order_id, r.customer_id, r.cookie_id,
                   c.name AS cookie_name, u.name AS customer_name,
                   r.rating, r.comment, r.created_at
            FROM reviews r
            INNER JOIN cookies c ON c.cookie_id = r.cookie_id
            INNER JOIN users u ON u.user_id = r.customer_id
            ORDER BY r.created_at DESC
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var reviews = new List<ReviewDisplayDto>();

        while (await reader.ReadAsync(cancellationToken))
        {
            reviews.Add(new ReviewDisplayDto
            {
                ReviewId = reader.GetInt32(0),
                OrderId = reader.GetInt32(1),
                CustomerId = reader.GetInt32(2),
                CookieId = reader.GetInt32(3),
                CookieName = reader.GetString(4),
                CustomerName = reader.GetString(5),
                Rating = reader.GetInt32(6),
                Comment = reader.IsDBNull(7) ? null : reader.GetString(7),
                CreatedAt = reader.GetDateTime(8)
            });
        }

        return reviews;
    }
}