using HomemadeCookie.Api.Models;
using HomemadeCookie.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomemadeCookie.Api.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly ReviewRepository _reviews;

    public ReviewsController(ReviewRepository reviews)
    {
        _reviews = reviews;
    }

    // POST: api/reviews
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CreateReview(
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        // Get customer ID from token if not provided
        if (request.CustomerId == 0)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { message = "User not authenticated" });
            
            request.CustomerId = int.Parse(userIdClaim);
        }

        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { message = "Rating must be between 1 and 5." });

        if (string.IsNullOrWhiteSpace(request.Comment))
            return BadRequest(new { message = "Comment is required." });

        // Check if user can review (has completed order with this product)
        var canReview = await _reviews.CanReviewAsync(
            request.OrderId,
            request.CustomerId,
            request.CookieId,
            cancellationToken);

        if (!canReview)
            return BadRequest(new { message = "You can only review cookies from completed orders." });

        // Check if already reviewed
        var hasReviewed = await _reviews.HasReviewedAsync(
            request.OrderId,
            request.CustomerId,
            request.CookieId,
            cancellationToken);

        if (hasReviewed)
            return BadRequest(new { message = "You have already reviewed this cookie for this order." });

        try
        {
            var reviewId = await _reviews.CreateAsync(request, cancellationToken);

            return Ok(new
            {
                reviewId,
                message = "Review submitted successfully. Thank you for your feedback!"
            });
        }
        catch 
        {
            return StatusCode(500, new { message = "An error occurred while submitting your review." });
        }
    }

    // GET: api/reviews/order/{orderId}
    [HttpGet("order/{orderId:int}")]
    public async Task<IActionResult> GetOrderReviews(int orderId, CancellationToken cancellationToken)
    {
        var reviews = await _reviews.GetByOrderIdAsync(orderId, cancellationToken);
        return Ok(reviews);
    }

    // GET: api/reviews/product/{cookieId}
    [HttpGet("product/{cookieId:int}")]
    public async Task<IActionResult> GetProductReviews(int cookieId, CancellationToken cancellationToken)
    {
        var reviews = await _reviews.GetByCookieIdAsync(cookieId, cancellationToken);
        return Ok(reviews);
    }

    // GET: api/reviews/customer/{customerId}
    [HttpGet("customer/{customerId:int}")]
    [Authorize]
    public async Task<IActionResult> GetCustomerReviews(int customerId, CancellationToken cancellationToken)
    {
        // Check if user is requesting their own reviews or is admin
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole("Admin");
        
        if (!isAdmin && (string.IsNullOrEmpty(userIdClaim) || int.Parse(userIdClaim) != customerId))
            return Unauthorized(new { message = "You can only view your own reviews." });

        var reviews = await _reviews.GetByCustomerIdAsync(customerId, cancellationToken);
        return Ok(reviews);
    }

    // GET: api/reviews/average/{cookieId}
    [HttpGet("average/{cookieId:int}")]
    public async Task<IActionResult> GetAverageRating(int cookieId, CancellationToken cancellationToken)
    {
        var average = await _reviews.GetAverageRatingAsync(cookieId, cancellationToken);
        var count = await _reviews.GetReviewCountAsync(cookieId, cancellationToken);
        
        return Ok(new { 
            averageRating = Math.Round(average, 1),
            reviewCount = count
        });
    }

    // GET: api/reviews/check
    [HttpGet("check")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CanReview(
        [FromQuery] int orderId,
        [FromQuery] int cookieId,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized();

        var customerId = int.Parse(userIdClaim);

        var canReview = await _reviews.CanReviewAsync(
            orderId,
            customerId,
            cookieId,
            cancellationToken);

        var hasReviewed = await _reviews.HasReviewedAsync(
            orderId,
            customerId,
            cookieId,
            cancellationToken);

        return Ok(new { 
            canReview, 
            hasReviewed,
            canSubmit = canReview && !hasReviewed
        });
    }

    // GET: api/reviews/{reviewId}
    [HttpGet("{reviewId:int}")]
    public async Task<IActionResult> GetReviewById(int reviewId, CancellationToken cancellationToken)
    {
        try
        {
            var review = await _reviews.GetReviewByIdAsync(reviewId, cancellationToken);
            if (review == null)
                return NotFound(new { message = "Review not found" });

            return Ok(review);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting review {reviewId}: {ex.Message}");
            return StatusCode(500, new { message = $"Error retrieving review: {ex.Message}" });
        }
    }
    

    // ✅ FIXED: DELETE: api/reviews/{reviewId} with better error handling
    [HttpDelete("{reviewId:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(int reviewId, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"Delete review called for ID: {reviewId}");
            
            // Get user ID from token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                Console.WriteLine("User not authenticated - no userId claim");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var customerId = int.Parse(userIdClaim);
            var isAdmin = User.IsInRole("Admin");
            Console.WriteLine($"User ID: {customerId}, IsAdmin: {isAdmin}");

            // Get the review to check ownership
            var review = await _reviews.GetReviewByIdAsync(reviewId, cancellationToken);
            if (review == null)
            {
                Console.WriteLine($"Review {reviewId} not found");
                return NotFound(new { message = "Review not found" });
            }

            Console.WriteLine($"Review found: CustomerId={review.CustomerId}, CookieId={review.CookieId}");

            // Check if user is admin or the review owner
            if (!isAdmin && review.CustomerId != customerId)
            {
                Console.WriteLine($"Unauthorized: User {customerId} trying to delete review owned by {review.CustomerId}");
                return Unauthorized(new { message = "You can only delete your own reviews." });
            }

            var result = await _reviews.DeleteReviewAsync(reviewId, cancellationToken);
            if (!result)
            {
                Console.WriteLine($"Delete failed for review {reviewId}");
                return NotFound(new { message = "Review not found" });
            }

            Console.WriteLine($"Review {reviewId} deleted successfully");
            return Ok(new { message = "Review deleted successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting review {reviewId}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new { message = $"Error deleting review: {ex.Message}" });
        }
    }

    // GET: api/reviews/admin/all
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllReviews(CancellationToken cancellationToken)
    {
        var reviews = await _reviews.GetAllReviewsAsync(cancellationToken);
        return Ok(reviews);
    }

    // DELETE: api/reviews/admin/{reviewId}
    [HttpDelete("admin/{reviewId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminDeleteReview(int reviewId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _reviews.DeleteReviewAsync(reviewId, cancellationToken);
            if (!result)
                return NotFound(new { message = "Review not found" });

            return Ok(new { message = "Review deleted successfully by admin" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error admin deleting review {reviewId}: {ex.Message}");
            return StatusCode(500, new { message = $"Error deleting review: {ex.Message}" });
        }
    }
}