namespace HomemadeCookie.Api.Patterns.State;

public class CompletedState : IOrderState
{
    public void NextState(OrderContext order) =>
        throw new InvalidOperationException("Reach final destination: Cookies are delivered to customer!");

    public void Cancel(OrderContext order) =>
        throw new InvalidOperationException("Cannot cancel: Cookies are handed over to customer!");

    public string GetStatusName() => "Order is completed";
}
