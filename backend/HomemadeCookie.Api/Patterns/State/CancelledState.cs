namespace HomemadeCookie.Api.Patterns.State;

public class CancelledState : IOrderState
{
    public void NextState(OrderContext order) =>
        throw new InvalidOperationException("Cancelled order: The order does not exist in the active workflow.");

    public void Cancel(OrderContext order) =>
        throw new InvalidOperationException("Cancelled order: The order has already been cancelled.");

    public string GetStatusName() => "Order Cancelled";
}
