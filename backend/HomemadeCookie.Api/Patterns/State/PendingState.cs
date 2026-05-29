namespace HomemadeCookie.Api.Patterns.State;

public class PendingState : IOrderState
{
    public void NextState(OrderContext order) => order.SetState(new ConfirmedState());

    public void Cancel(OrderContext order) => order.SetState(new CancelledState());

    public string GetStatusName() => "Placing order / Awaiting payment";
}
