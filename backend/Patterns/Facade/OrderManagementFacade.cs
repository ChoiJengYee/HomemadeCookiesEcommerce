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
    private readonly IOrderNotificationFacade _notificationFacade;
    private readonly ILogger<OrderManagementFacade> _logger;

    public OrderManagementFacade(
        InventorySystem inventory,
        PaymentGateway payment,
        OrderRepository orders,
        CartRepository cart,
        IOrderNotificationFacade notificationFacade,
        EmailService email,
        ILogger<OrderManagementFacade> logger)
    {
        _inventory = inventory;
        _payment = payment;
        _orders = orders;
        _cart = cart;
        _notificationFacade = notificationFacade;
        _email = email;
        _logger = logger;
    }

    public async Task<PlaceOrderResult> PlaceOrderAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var lines = (await _cart.GetItemsAsync(request.CustomerId, cancellationToken))
            .Select(i => new CartLine
            {
                CookieId = i.CookieId,
                CookieName = i.CookieName ?? "Unknown Cookie",
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
                Message = $"Out of stock: {outOfStockName ?? "Unknown product"}. Order cancelled."
            };
        }

        var total = lines.Sum(l => l.UnitPrice * l.Quantity);
        var paymentResult = _payment.ProcessPayment(request.CardDetails, total);

        var email = request.CustomerEmail
            ?? await _orders.GetCustomerEmailAsync(request.CustomerId, cancellationToken)
            ?? string.Empty;

        if (paymentResult.Outcome == PaymentOutcome.Failed)
        {
            return new PlaceOrderResult
            {
                Success = false,
                Outcome = "PaymentFailed",
                Message = paymentResult.Message ?? "Payment failed"
            };
        }

        if (paymentResult.Outcome == PaymentOutcome.Pending)
        {
            var orderId = await SaveOrderAsync(
                request.CustomerId,
                lines,
                total,
                OrderStatusIds.Pending,
                request.PaymentMethod ?? "Unknown",
                "Pending",
                cancellationToken);

            if (!string.IsNullOrEmpty(email))
            {
                try
                {
                    await _notificationFacade.SendOrderPendingAsync(email, orderId);
                    _logger.LogInformation("Pending email sent to {Email} for order #{OrderId}", email, orderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send pending email for order #{OrderId}", orderId);
                }
            }

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
            OrderStatusIds.Confirmed,
            request.PaymentMethod ?? "Unknown",
            "Paid",
            cancellationToken);

        await _cart.ClearAsync(request.CustomerId, cancellationToken);

        if (!string.IsNullOrEmpty(email))
        {
            try
            {
                await _notificationFacade.SendOrderConfirmationAsync(email, confirmedOrderId);
                _logger.LogInformation("Confirmation email sent to {Email} for order #{OrderId}", email, confirmedOrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email for order #{OrderId}", confirmedOrderId);
            }
        }

        return new PlaceOrderResult
        {
            Success = true,
            Outcome = "Success",
            Message = "Order placed successfully! A confirmation email has been sent.",
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

    public async Task<PlaceOrderResult> SavePendingOrderAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var lines = (await _cart.GetItemsAsync(request.CustomerId, cancellationToken))
            .Select(i => new CartLine
            {
                CookieId = i.CookieId,
                CookieName = i.CookieName ?? "Unknown Cookie",
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
                Message = "Your cart is empty."
            };
        }

        var total = lines.Sum(l => l.UnitPrice * l.Quantity);

        var orderId = await SaveOrderAsync(
            request.CustomerId,
            lines,
            total,
            OrderStatusIds.Pending,
            request.PaymentMethod ?? "Unknown",
            "AwaitingPayment",
            cancellationToken);

        await _cart.ClearAsync(request.CustomerId, cancellationToken);

        var email = request.CustomerEmail
            ?? await _orders.GetCustomerEmailAsync(request.CustomerId, cancellationToken)
            ?? string.Empty;

        if (!string.IsNullOrEmpty(email))
        {
            try
            {
                await _notificationFacade.SendOrderPendingAsync(email, orderId);
                _logger.LogInformation("Pending email sent to {Email} for saved order #{OrderId}", email, orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send pending email for saved order #{OrderId}", orderId);
            }
        }

        return new PlaceOrderResult
        {
            Success = true,
            Outcome = "SavedPendingOrder",
            Message = "Order saved as pending. You can pay or cancel it later. A confirmation email has been sent.",
            OrderId = orderId
        };
    }

    public async Task<PlaceOrderResult> PayPendingOrderAsync(
        int orderId,
        CheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(orderId, cancellationToken);

        if (order == null)
        {
            return new PlaceOrderResult
            {
                Success = false,
                Outcome = "NotFound",
                Message = "Order not found."
            };
        }

        if (order.StatusId != OrderStatusIds.Pending)
        {
            return new PlaceOrderResult
            {
                Success = false,
                Outcome = "InvalidState",
                Message = "Only pending orders can be paid."
            };
        }

        var payment = _payment.ProcessPayment(request.CardDetails, order.TotalAmount);

        if (payment.Outcome != PaymentOutcome.Success)
        {
            return new PlaceOrderResult
            {
                Success = false,
                Outcome = payment.Outcome.ToString() ?? "PaymentFailed",
                Message = payment.Message ?? "Payment failed"
            };
        }

        await _orders.UpdateStatusAsync(orderId, OrderStatusIds.Confirmed, cancellationToken);

        var email = request.CustomerEmail
            ?? await _orders.GetCustomerEmailAsync(order.CustomerId, cancellationToken)
            ?? string.Empty;

        if (!string.IsNullOrEmpty(email))
        {
            try
            {
                await _notificationFacade.SendOrderConfirmationAsync(email, orderId);
                _logger.LogInformation("Confirmation email sent to {Email} for paid pending order #{OrderId}", email, orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email for paid pending order #{OrderId}", orderId);
            }
        }

        return new PlaceOrderResult
        {
            Success = true,
            Outcome = "Paid",
            Message = "Payment successful. Your order is now confirmed. A confirmation email has been sent.",
            OrderId = orderId
        };
    }

    public async Task<PlaceOrderResult> CancelOrderAsync(
        int orderId,
        string? email = null,
        string? reason = "Customer requested cancellation",
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(orderId, cancellationToken);

        if (order == null)
        {
            return new PlaceOrderResult
            {
                Success = false,
                Outcome = "NotFound",
                Message = "Order not found."
            };
        }

        if (order.StatusId == OrderStatusIds.Cancelled)
        {
            return new PlaceOrderResult
            {
                Success = false,
                Outcome = "AlreadyCancelled",
                Message = "Order is already cancelled."
            };
        }

        if (order.StatusId == OrderStatusIds.Completed || order.StatusId == OrderStatusIds.Shipped)
        {
            return new PlaceOrderResult
            {
                Success = false,
                Outcome = "CannotCancel",
                Message = "Order cannot be cancelled at this stage."
            };
        }

        await _orders.UpdateStatusAsync(orderId, OrderStatusIds.Cancelled, cancellationToken);

        if (order.StatusId == OrderStatusIds.Confirmed)
        {
            var items = await _orders.GetItemsAsync(orderId, cancellationToken);
            var lines = items.Select(i => new CartLine
            {
                CookieId = i.CookieId,
                CookieName = i.CookieName ?? "Unknown Cookie",
                Quantity = i.Quantity,
                UnitPrice = i.PriceAtPurchase
            }).ToList();
            
            await _inventory.ReleaseStockAsync(lines, cancellationToken);
            _logger.LogInformation("Inventory released for cancelled order #{OrderId}", orderId);
        }

        var customerEmail = email 
            ?? await _orders.GetCustomerEmailAsync(order.CustomerId, cancellationToken)
            ?? string.Empty;

        if (!string.IsNullOrEmpty(customerEmail))
        {
            try
            {
                await _notificationFacade.SendOrderCancelledAsync(customerEmail, orderId);
                _logger.LogInformation("Cancellation email sent to {Email} for order #{OrderId}", customerEmail, orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send cancellation email for order #{OrderId}", orderId);
            }
        }

        return new PlaceOrderResult
        {
            Success = true,
            Outcome = "Cancelled",
            Message = "Order cancelled successfully. A confirmation email has been sent.",
            OrderId = orderId
        };
    }
}

// Helper classes with nullable properties marked properly
public static class OrderStatusIds
{
    public const int Pending = 1;
    public const int Confirmed = 2;
    public const int Baking = 3;
    public const int Shipped = 4;
    public const int Cancelled = 5;
    public const int Completed = 6;
}

public class CartLine
{
    public int CookieId { get; set; }
    public string? CookieName { get; set; }  // Made nullable
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class PlaceOrderResult
{
    public bool Success { get; set; }
    public string? Outcome { get; set; }   // Made nullable
    public string? Message { get; set; }   // Made nullable
    public int? OrderId { get; set; }
}