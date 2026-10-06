using Dashboard.Api.Domain;

namespace Dashboard.Api.Services;

public sealed record NotificationDeliveryResult(bool Delivered, string ErrorMessage);

public interface INotificationChannel
{
    string Mode { get; }
    Task<NotificationDeliveryResult> DeliverAsync(NotificationRecord notification, string projectName, CancellationToken cancellationToken);
}
