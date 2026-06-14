namespace HomemadeCookie.Api.Models;

public class AddCartItemRequest
{
    public int CookieId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}