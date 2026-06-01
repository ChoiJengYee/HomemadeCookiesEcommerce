namespace HomemadeCookie.Api.Models;

public class CookieEntity
{
    public int CookieId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
}
