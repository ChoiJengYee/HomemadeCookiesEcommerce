namespace HomemadeCookie.Api.Patterns.State;

public interface IOrderState
{
    void NextState(OrderContext order);
    void Cancel(OrderContext order);
    string GetStatusName();
}
