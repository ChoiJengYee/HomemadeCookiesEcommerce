// DTOs/ReviewStatsDto.cs
namespace HomemadeCookie.Api.DTOs;

public class ReviewStatsDto
{
    public int TotalReviews { get; set; }
    public double AverageRating { get; set; }
    public int PositiveReviews { get; set; }
    public int NegativeReviews { get; set; }
}