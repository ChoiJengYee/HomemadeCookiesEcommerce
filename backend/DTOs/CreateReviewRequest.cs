public class CreateReviewRequest
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public int CookieId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}