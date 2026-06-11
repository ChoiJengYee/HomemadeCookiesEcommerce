namespace HomemadeCookie.Api.Patterns.Facade;

/// <summary>Mock payment gateway — no real bank integration (per project scope).</summary>
public class PaymentGateway
{
    public PaymentResult ProcessPayment(string cardDetails, decimal amount)
    {
        var normalized = (cardDetails ?? string.Empty).Trim().ToUpperInvariant();

        if (normalized.Contains("FAIL", StringComparison.Ordinal))
        {
            return new PaymentResult
            {
                Outcome = PaymentOutcome.Failed,
                Message = "Payment declined. Please try another card."
            };
        }

        if (normalized.Contains("PENDING", StringComparison.Ordinal)
            || normalized.Contains("LIMIT", StringComparison.Ordinal))
        {
            return new PaymentResult
            {
                Outcome = PaymentOutcome.Pending,
                Message = "Limit exceeded / payment pending."
            };
        }

        return new PaymentResult
        {
            Outcome = PaymentOutcome.Success,
            Message = "Payment processed successfully."
        };
    }
}
