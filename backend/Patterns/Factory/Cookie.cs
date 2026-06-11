namespace HomemadeCookie.Api.Patterns.Factory;

public abstract class Cookie
{
    public int CookieId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }

    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Stock cannot be negative.");

        Stock = quantity;
    }
}
