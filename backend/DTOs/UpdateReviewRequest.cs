// DTOs/UpdateReviewRequest.cs
namespace HomemadeCookie.Api.DTOs;

public class UpdateReviewRequest
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}