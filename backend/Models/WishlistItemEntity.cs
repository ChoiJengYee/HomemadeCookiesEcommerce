using System;

namespace HomemadeCookie.Api.Models;

public class WishlistItemEntity
{
    public int WishlistItemId { get; set; }
    public int WishlistId { get; set; }
    public int CookieId { get; set; }
    public DateTime AddedAt { get; set; }
    public string? CookieName { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
