public class ReviewDisplayDto
{
    public int ReviewId { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public int CookieId { get; set; }
    public string CookieName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}