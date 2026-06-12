namespace HomemadeCookie.Api.Patterns.Facade;

public enum PaymentOutcome
{
    Success,
    Pending,
    Failed
}

public sealed class PaymentResult
{
    public PaymentOutcome Outcome { get; init; }
    public string Message { get; init; } = string.Empty;
}
