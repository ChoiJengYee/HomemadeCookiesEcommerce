using HomemadeCookie.Api.Models;
using HomemadeCookie.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

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

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Customer")]
    public async Task<IActionResult> CreateReview(
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { message = "Rating must be between 1 and 5." });

        var canReview = await _reviews.CanReviewAsync(
            request.OrderId,
            request.CustomerId,
            request.CookieId,
            cancellationToken);

        if (!canReview)
            return BadRequest(new { message = "You can only review cookies from completed orders." });

        try
        {
            var reviewId = await _reviews.CreateAsync(request, cancellationToken);

            return Ok(new
            {
                reviewId,
                message = "Review submitted successfully."
            });
        }
        catch
        {
            return BadRequest(new { message = "You have already reviewed this cookie for this order." });
        }
    }

    [HttpGet("order/{orderId:int}")]
    public async Task<IActionResult> GetOrderReviews(int orderId, CancellationToken cancellationToken)
    {
        var reviews = await _reviews.GetByOrderIdAsync(orderId, cancellationToken);
        return Ok(reviews);
    }
}