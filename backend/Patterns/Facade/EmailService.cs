using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HomemadeCookie.Api.Patterns.Facade;

/// <summary>
/// Email notification service that sends real emails
/// </summary>
public class EmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendConfirmationAsync(string email, int orderId)
    {
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("⚠️ Cannot send confirmation email: email is null or empty");
            return;
        }

        try
        {
            var subject = $"Order Confirmation #{orderId}";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: #8B4513; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background: #f9f9f9; }}
                        .footer {{ text-align: center; padding: 10px; font-size: 12px; color: #666; }}
                        .button {{ display: inline-block; padding: 10px 20px; background: #8B4513; color: white; text-decoration: none; border-radius: 5px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🍪 Order Confirmation</h1>
                        </div>
                        <div class='content'>
                            <h2>Thank you for your order!</h2>
                            <p>Your order <strong>#{orderId}</strong> has been placed successfully.</p>
                            <p>We'll notify you when your cookies are ready for pickup or delivery.</p>
                            <p>
                                <a href='http://localhost:5000/track-order.html?orderId={orderId}' class='button'>
                                    Track Your Order
                                </a>
                            </p>
                            <p>Order Details:</p>
                            <ul>
                                <li>Order ID: #{orderId}</li>
                                <li>Date: {DateTime.Now:MMMM dd, yyyy}</li>
                                <li>Status: Confirmed</li>
                            </ul>
                        </div>
                        <div class='footer'>
                            <p>Homemade Cookies - Freshly baked with love ❤️</p>
                            <p>© {DateTime.Now.Year} Homemade Cookies. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(email, subject, body);
            
            _logger.LogInformation("✅ Real confirmation email sent to {Email} for order #{OrderId}", email, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send confirmation email to {Email} for order #{OrderId}", email, orderId);
            throw;
        }
    }

    public async Task SendPendingEmailAsync(string email, int orderId)
    {
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("⚠️ Cannot send pending email: email is null or empty");
            return;
        }

        try
        {
            var subject = $"Payment Pending - Order #{orderId}";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: #ff9800; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background: #f9f9f9; }}
                        .footer {{ text-align: center; padding: 10px; font-size: 12px; color: #666; }}
                        .button {{ display: inline-block; padding: 10px 20px; background: #ff9800; color: white; text-decoration: none; border-radius: 5px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>⏳ Payment Pending</h1>
                        </div>
                        <div class='content'>
                            <h2>Your order #{orderId} is pending payment</h2>
                            <p>We're waiting for payment confirmation to process your order.</p>
                            <p>
                                <a href='http://localhost:5000/checkout.html?pendingOrderId={orderId}' class='button'>
                                    Complete Payment
                                </a>
                            </p>
                            <p>If you've already completed the payment, please ignore this email.</p>
                        </div>
                        <div class='footer'>
                            <p>Homemade Cookies - Freshly baked with love ❤️</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(email, subject, body);
            
            _logger.LogInformation("✅ Real pending email sent to {Email} for order #{OrderId}", email, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send pending email to {Email} for order #{OrderId}", email, orderId);
            throw;
        }
    }

    public async Task SendCancellationEmailAsync(string email, int orderId)
    {
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("⚠️ Cannot send cancellation email: email is null or empty");
            return;
        }

        try
        {
            var subject = $"Order Cancelled #{orderId}";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: #dc3545; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background: #f9f9f9; }}
                        .footer {{ text-align: center; padding: 10px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>❌ Order Cancelled</h1>
                        </div>
                        <div class='content'>
                            <h2>Your order #{orderId} has been cancelled</h2>
                            <p>Your order has been successfully cancelled as requested.</p>
                            <p>If this was a mistake, you can place a new order anytime.</p>
                            <p>
                                <a href='http://localhost:5000/index.html' style='display: inline-block; padding: 10px 20px; background: #8B4513; color: white; text-decoration: none; border-radius: 5px;'>
                                    Shop Again
                                </a>
                            </p>
                        </div>
                        <div class='footer'>
                            <p>Homemade Cookies - Freshly baked with love ❤️</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(email, subject, body);
            
            _logger.LogInformation("✅ Real cancellation email sent to {Email} for order #{OrderId}", email, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send cancellation email to {Email} for order #{OrderId}", email, orderId);
            throw;
        }
    }

    public async Task SendShippedEmailAsync(string email, int orderId, string trackingNumber)
    {
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("⚠️ Cannot send shipped email: email is null or empty");
            return;
        }

        try
        {
            var subject = $"Order Shipped! #{orderId}";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: #28a745; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background: #f9f9f9; }}
                        .footer {{ text-align: center; padding: 10px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>📦 Order Shipped!</h1>
                        </div>
                        <div class='content'>
                            <h2>Your order #{orderId} is on the way!</h2>
                            <p>Your cookies have been shipped and are on their way to you.</p>
                            <p><strong>Tracking Number:</strong> {trackingNumber}</p>
                            <p>Expected delivery: 3-5 business days</p>
                            <p>
                                <a href='http://localhost:5000/track-order.html?orderId={orderId}' style='display: inline-block; padding: 10px 20px; background: #28a745; color: white; text-decoration: none; border-radius: 5px;'>
                                    Track Your Order
                                </a>
                            </p>
                        </div>
                        <div class='footer'>
                            <p>Homemade Cookies - Freshly baked with love ❤️</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(email, subject, body);
            
            _logger.LogInformation("✅ Real shipping email sent to {Email} for order #{OrderId}", email, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send shipping email to {Email} for order #{OrderId}", email, orderId);
            throw;
        }
    }

    // Main email sending method
    private async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpSettings = _configuration.GetSection("EmailSettings");
        
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(
            smtpSettings["FromName"] ?? "Homemade Cookies",
            smtpSettings["FromEmail"] ?? "noreply@homemadecookies.com"
        ));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = body
        };
        email.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        
        try
        {
            await client.ConnectAsync(
                smtpSettings["SmtpServer"] ?? "smtp.gmail.com",
                int.Parse(smtpSettings["SmtpPort"] ?? "587"),
                SecureSocketOptions.StartTls
            );

            await client.AuthenticateAsync(
                smtpSettings["SmtpUsername"],
                smtpSettings["SmtpPassword"]
            );

            await client.SendAsync(email);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }
}