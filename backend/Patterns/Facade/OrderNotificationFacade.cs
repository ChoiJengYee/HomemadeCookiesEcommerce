namespace HomemadeCookie.Api.Patterns.Facade;

public class OrderNotificationFacade : IOrderNotificationFacade
{
    private readonly EmailService _emailService;
    private readonly ILogger<OrderNotificationFacade> _logger;

    public OrderNotificationFacade(
        EmailService emailService,
        ILogger<OrderNotificationFacade> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendOrderConfirmationAsync(string email, int orderId)
    {
        try
        {
            _logger.LogInformation("📋 Processing order confirmation for Order #{OrderId}", orderId);
            await _emailService.SendConfirmationAsync(email, orderId);
            _logger.LogInformation("✅ Order #{OrderId} confirmation processed successfully", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send order confirmation for Order #{OrderId}", orderId);
            throw;
        }
    }

    public async Task SendOrderPendingAsync(string email, int orderId)
    {
        try
        {
            _logger.LogInformation("📋 Processing pending notification for Order #{OrderId}", orderId);
            await _emailService.SendPendingEmailAsync(email, orderId);
            _logger.LogInformation("✅ Pending notification for Order #{OrderId} processed successfully", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send pending notification for Order #{OrderId}", orderId);
            throw;
        }
    }

    public async Task SendOrderShippedAsync(string email, int orderId, string trackingNumber)
    {
        try
        {
            _logger.LogInformation("📋 Processing shipping notification for Order #{OrderId}", orderId);
            await _emailService.SendShippedEmailAsync(email, orderId, trackingNumber);
            _logger.LogInformation("✅ Shipping notification for Order #{OrderId} processed successfully", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send shipping notification for Order #{OrderId}", orderId);
            throw;
        }
    }

    public async Task SendOrderCancelledAsync(string email, int orderId)
    {
        try
        {
            _logger.LogInformation("📋 Processing cancellation notification for Order #{OrderId}", orderId);
            await _emailService.SendCancellationEmailAsync(email, orderId);
            _logger.LogInformation("✅ Cancellation notification for Order #{OrderId} processed successfully", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send cancellation notification for Order #{OrderId}", orderId);
            throw;
        }
    }
}