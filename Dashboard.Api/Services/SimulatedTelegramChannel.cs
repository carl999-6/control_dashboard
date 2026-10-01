using Dashboard.Api.Domain;

namespace Dashboard.Api.Services;

public sealed class SimulatedTelegramChannel : INotificationChannel
{
    public Task<NotificationDeliveryResult> DeliverAsync(NotificationRecord notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(notification.SimulateFailure
            ? new NotificationDeliveryResult(false, "Fallo simulado de Telegram; no se realizó ninguna llamada externa.")
            : new NotificationDeliveryResult(true, string.Empty));
    }
}
