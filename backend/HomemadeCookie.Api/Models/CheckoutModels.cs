namespace HomemadeCookie.Api.Models;

public class CheckoutRequest
{
    public int CustomerId { get; set; }
    public string PaymentMethod { get; set; } = "Card";
    public string CardDetails { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
}

public class PlaceOrderResult
{
    public bool Success { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? OrderId { get; set; }
}
