namespace HomemadeCookie.Api.Models;

public class AddCartItemRequest
{
    public int CookieId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal? CustomPrice { get; set; }
    public string? PackageType { get; set; }
    public string? GiftBox { get; set; }
}

public class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}