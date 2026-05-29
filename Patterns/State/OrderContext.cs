namespace HomemadeCookie.Api.Patterns.State;

/// <summary>State pattern context — represents an order whose behaviour depends on current state.</summary>
public class OrderContext
{
    private IOrderState _currentState;

    public int OrderId { get; init; }
    public int CustomerId { get; init; }
    public int StatusId { get; private set; }

    public OrderContext(int orderId, int customerId, int statusId, IOrderState initialState)
    {
        OrderId = orderId;
        CustomerId = customerId;
        StatusId = statusId;
        _currentState = initialState;
    }

    public void SetState(IOrderState state)
    {
        _currentState = state;
        StatusId = OrderStateFactory.ToStatusId(state);
    }

    public void Proceed() => _currentState.NextState(this);

    public void RequestCancel() => _currentState.Cancel(this);

    public string GetStatus() => _currentState.GetStatusName();
}
