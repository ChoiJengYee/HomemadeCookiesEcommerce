namespace HomemadeCookie.Api.Patterns.Facade;

/// <summary>Stub notification service — logs to console (prototype).</summary>
public class EmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public void SendConfirmation(string email, int orderId)
    {
        _logger.LogInformation(
            "Email sent to {Email} for order {OrderId}: Order placed successfully.",
            email,
            orderId);
    }

    public void SendPendingEmail(string email, int orderId)
    {
        _logger.LogInformation(
            "Email sent to {Email} for order {OrderId}: Payment pending. Please retry later.",
            email,
            orderId);
    }
}
