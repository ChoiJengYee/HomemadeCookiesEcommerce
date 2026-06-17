namespace HomemadeCookie.Api.Patterns.Facade;

public interface IOrderNotificationFacade
{
    Task SendOrderConfirmationAsync(string email, int orderId);
    Task SendOrderPendingAsync(string email, int orderId);
    Task SendOrderShippedAsync(string email, int orderId, string trackingNumber);
    Task SendOrderCancelledAsync(string email, int orderId);
}