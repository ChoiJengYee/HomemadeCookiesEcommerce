namespace HomemadeCookie.Api.Patterns.State;

public class BakingState : IOrderState
{
    public void NextState(OrderContext order) => order.SetState(new ReadyState());

    public void Cancel(OrderContext order) =>
        throw new InvalidOperationException("Cannot cancel: Cookies are in the oven!");

    public string GetStatusName() => "Baking in Progress";
}
