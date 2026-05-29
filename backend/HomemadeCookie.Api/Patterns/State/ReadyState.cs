namespace HomemadeCookie.Api.Patterns.State;

public class ReadyState : IOrderState
{
    public void NextState(OrderContext order) => order.SetState(new CompletedState());

    public void Cancel(OrderContext order) =>
        throw new InvalidOperationException("Cannot cancel: Cookies are packaged or dispatched!");

    public string GetStatusName() => "Ready for pickup";
}
