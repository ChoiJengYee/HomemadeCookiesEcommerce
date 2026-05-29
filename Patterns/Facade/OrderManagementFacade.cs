using HomemadeCookie.Api.Models;
using HomemadeCookie.Api.Repositories;

namespace HomemadeCookie.Api.Patterns.Facade;

/// <summary>
/// Facade — single entry point for checkout; coordinates inventory, payment, persistence, and email.
/// </summary>
public class OrderManagementFacade
{
    private readonly InventorySystem _inventory;
    private readonly PaymentGateway _payment;
    private readonly OrderRepository _orders;
    private readonly CartRepository _cart;
    private readonly EmailService _email;

    public OrderManagementFacade(
        InventorySystem inventory,
        PaymentGateway payment,
        OrderRepository orders,
        CartRepository cart,
        EmailService email)
    {
        _inventory = inventory;
        _payment = payment;
        _orders = orders;
        _cart = cart;
        _email = email;
    }

    public async Task<PlaceOrderResult> PlaceOrderAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var lines = (await _cart.GetItemsAsync(request.CustomerId, cancellationToken))
            .Select(i => new CartLine
            {
                CookieId = i.CookieId,
                CookieName = i.CookieName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            })
            .ToList();

        if (lines.Count == 0)
        {
            return new PlaceOrderResult
            {
                Success = false,
                Outcome = "EmptyCart",
                Message = "Your cart is empty. Add cookies before checkout."
            };
        }

        var (stockOk, outOfStockName) = await _inventory.CheckStockAsync(lines, cancellationToken);
        if (!stockOk)
        {
            return new PlaceOrderResult
            {
                Success = false,
                Outcome = "OutOfStock",
                Message = $"Out of stock: {outOfStockName}. Order cancelled."
            };
        }

        var total = lines.Sum(l => l.UnitPrice * l.Quantity);
        var paymentResult = _payment.ProcessPayment(request.CardDetails, total);

        var email = request.CustomerEmail
            ?? await _orders.GetCustomerEmailAsync(request.CustomerId, cancellationToken)
            ?? "customer@homemadecookies.com";

        if (paymentResult.Outcome == PaymentOutcome.Failed)
        {
            return new PlaceOrderResult
            {
                Success = false,
                Outcome = "PaymentFailed",
                Message = paymentResult.Message
            };
        }

        if (paymentResult.Outcome == PaymentOutcome.Pending)
        {
            var orderId = await SaveOrderAsync(
                request.CustomerId,
                lines,
                total,
                OrderStatusIds.Pending,
                request.PaymentMethod,
                "Pending",
                cancellationToken);

            _email.SendPendingEmail(email, orderId);

            return new PlaceOrderResult
            {
                Success = false,
                Outcome = "PaymentPending",
                Message = "Payment pending. Please retry later or use another payment method.",
                OrderId = orderId
            };
        }

        await _inventory.ReduceStockAsync(lines, cancellationToken);

        var confirmedOrderId = await SaveOrderAsync(
            request.CustomerId,
            lines,
            total,
            OrderStatusIds.Pending,
            request.PaymentMethod,
            "Paid",
            cancellationToken);

        await _cart.ClearAsync(request.CustomerId, cancellationToken);
        _email.SendConfirmation(email, confirmedOrderId);

        return new PlaceOrderResult
        {
            Success = true,
            Outcome = "Success",
            Message = "Order placed successfully!",
            OrderId = confirmedOrderId
        };
    }

    private async Task<int> SaveOrderAsync(
        int customerId,
        IReadOnlyList<CartLine> lines,
        decimal total,
        int statusId,
        string paymentMethod,
        string paymentStatus,
        CancellationToken cancellationToken)
    {
        var orderLines = lines
            .Select(l => (l.CookieId, l.Quantity, l.UnitPrice))
            .ToList();

        var orderId = await _orders.CreateOrderAsync(customerId, total, statusId, orderLines, cancellationToken);
        await _orders.CreatePaymentAsync(orderId, paymentMethod, total, paymentStatus, cancellationToken);
        return orderId;
    }
}
