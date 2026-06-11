namespace HomemadeCookie.Api.DTOs;

public class CreateCookieRequest
{
    public required string FactoryKey { get; set; }
    public required string CookieType { get; set; }

    public string Description { get; set; } = "";

    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
}