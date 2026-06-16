using HomemadeCookie.Api.Infrastructure;
using HomemadeCookie.Api.Models;
using HomemadeCookie.Api.DTOs;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HomemadeCookie.Api.Repositories;

public class ReviewRepository
{
    // Check if customer can review a product (has completed order with this product)
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

    // Check if user has already reviewed a specific order/cookie
    public async Task<bool> HasReviewedAsync(int orderId, int customerId, int cookieId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM reviews
            WHERE order_id = @orderId
              AND customer_id = @customerId
              AND cookie_id = @cookieId
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("orderId", orderId);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("cookieId", cookieId);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    // Create a new review
    public async Task<int> CreateAsync(CreateReviewRequest request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO reviews (order_id, cookie_id, customer_id, rating, comment, created_at)
            VALUES (@orderId, @cookieId, @customerId, @rating, @comment, @createdAt)
            RETURNING review_id
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("orderId", request.OrderId);
        command.Parameters.AddWithValue("cookieId", request.CookieId);
        command.Parameters.AddWithValue("customerId", request.CustomerId);
        command.Parameters.AddWithValue("rating", request.Rating);
        command.Parameters.AddWithValue("comment", (object?)request.Comment ?? DBNull.Value);
        command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    // Get reviews by Order ID
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

    // Get reviews by Cookie ID (Product)
    public async Task<IReadOnlyList<ReviewDisplayDto>> GetByCookieIdAsync(int cookieId, CancellationToken cancellationToken = default)
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
            WHERE r.cookie_id = @cookieId
            ORDER BY r.created_at DESC
        """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("cookieId", cookieId);

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

    // Get reviews by Customer ID
    public async Task<IReadOnlyList<ReviewDisplayDto>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
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
            WHERE r.customer_id = @customerId
            ORDER BY r.created_at DESC
        """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("customerId", customerId);

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

    // Get all reviews (Admin)
    public async Task<IReadOnlyList<ReviewDisplayDto>> GetAllReviewsAsync(CancellationToken cancellationToken = default)
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

    // Get a single review by ID
    public async Task<ReviewDisplayDto?> GetReviewByIdAsync(int reviewId, CancellationToken cancellationToken = default)
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
            WHERE r.review_id = @reviewId
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("reviewId", reviewId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return new ReviewDisplayDto
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
            };
        }

        return null;
    }

    // Delete a review (Admin or Owner)
    public async Task<bool> DeleteReviewAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM reviews
            WHERE review_id = @reviewId
            RETURNING review_id
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("reviewId", reviewId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null;
    }

    // Get average rating for a cookie
    public async Task<double> GetAverageRatingAsync(int cookieId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COALESCE(AVG(rating), 0)
            FROM reviews
            WHERE cookie_id = @cookieId
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("cookieId", cookieId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToDouble(result);
    }

    // Get review count for a cookie
    public async Task<int> GetReviewCountAsync(int cookieId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM reviews
            WHERE cookie_id = @cookieId
            """;

        await using var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("cookieId", cookieId);

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

}