namespace HomemadeCookie.Api.Models;

public class OrderEntity
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

public class OrderItemEntity
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public int CookieId { get; set; }
    public string CookieName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }
}

public static class OrderStatusIds
{
    public const int Pending = 1;
    public const int Confirmed = 2;
    public const int Baking = 3;
    public const int Ready = 4;
    public const int Completed = 5;
    public const int Cancelled = 6;
}
