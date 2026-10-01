using Dashboard.Api.Domain;

namespace Dashboard.Api.Services;

public sealed record NotificationDeliveryResult(bool Delivered, string ErrorMessage);

public interface INotificationChannel
{
    Task<NotificationDeliveryResult> DeliverAsync(NotificationRecord notification, CancellationToken cancellationToken);
}
