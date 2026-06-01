namespace HomemadeCookie.Api.Models;

public class CreateCookieRequest
{
    public string FactoryKey { get; set; } = string.Empty;
    public string CookieType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
}
