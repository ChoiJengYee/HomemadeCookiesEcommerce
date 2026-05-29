namespace HomemadeCookie.Api.Models;

public class CartEntity
{
    public int CartId { get; set; }
    public int CustomerId { get; set; }
}

public class CartItemEntity
{
    public int CartItemId { get; set; }
    public int CartId { get; set; }
    public int CookieId { get; set; }
    public string CookieName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}

/// <summary>Line item shape used by checkout / Facade (Phase 3).</summary>
public class CartLine
{
    public int CookieId { get; set; }
    public string CookieName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
