namespace HomemadeCookie.Api.Patterns.State;

public class ConfirmedState : IOrderState
{
    public void NextState(OrderContext order) => order.SetState(new BakingState());

    public void Cancel(OrderContext order) =>
        throw new InvalidOperationException("Cannot cancel: Order has been confirmed!");

    public string GetStatusName() => "Order Confirmed";
}
