using HomemadeCookie.Api.Models;

namespace HomemadeCookie.Api.Patterns.State;

public static class OrderStateFactory
{
    public static IOrderState FromStatusId(int statusId) => statusId switch
    {
        OrderStatusIds.Pending => new PendingState(),
        OrderStatusIds.Confirmed => new ConfirmedState(),
        OrderStatusIds.Baking => new BakingState(),
        OrderStatusIds.Ready => new ReadyState(),
        OrderStatusIds.Completed => new CompletedState(),
        OrderStatusIds.Cancelled => new CancelledState(),
        _ => throw new ArgumentException($"Unknown status id: {statusId}")
    };

    public static int ToStatusId(IOrderState state) => state switch
    {
        PendingState => OrderStatusIds.Pending,
        ConfirmedState => OrderStatusIds.Confirmed,
        BakingState => OrderStatusIds.Baking,
        ReadyState => OrderStatusIds.Ready,
        CompletedState => OrderStatusIds.Completed,
        CancelledState => OrderStatusIds.Cancelled,
        _ => throw new ArgumentException("Unknown state type.")
    };

    public static OrderContext FromOrder(int orderId, int customerId, int statusId) =>
        new(orderId, customerId, statusId, FromStatusId(statusId));
}
