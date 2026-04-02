using Argus.EvidencePlatform.Application.Common.Abstractions;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public sealed class FirebaseDeviceCommandDispatcher(
    FirebaseAppAccessor firebaseAppAccessor,
    ILogger<FirebaseDeviceCommandDispatcher> logger) : IDeviceCommandDispatcher
{
    public async Task<DeviceCommandDispatchResult> RequestScreenshotAsync(
        string deviceId,
        string fcmToken,
        CancellationToken cancellationToken)
    {
        if (!firebaseAppAccessor.IsConfigured || firebaseAppAccessor.App is null)
        {
            logger.LogError("Firebase command dispatch requested for device {DeviceId}, but Firebase is not configured.", deviceId);
            return new DeviceCommandDispatchResult(DeviceCommandDispatchStatus.Failed, null, "firebase_not_configured");
        }

        try
        {
            var messageId = await FirebaseMessaging.GetMessaging(firebaseAppAccessor.App).SendAsync(
                new Message
                {
                    Token = fcmToken,
                    Data = new Dictionary<string, string>
                    {
                        ["cmd"] = "screenshot"
                    }
                },
                cancellationToken);

            logger.LogInformation(
                "FCM screenshot command sent to device {DeviceId} with message id {MessageId}.",
                deviceId,
                messageId);

            return new DeviceCommandDispatchResult(DeviceCommandDispatchStatus.Success, messageId);
        }
        catch (FirebaseMessagingException exception) when (IsInvalidToken(exception))
        {
            logger.LogWarning(
                exception,
                "FCM token rejected for device {DeviceId}; binding will be invalidated.",
                deviceId);

            return new DeviceCommandDispatchResult(DeviceCommandDispatchStatus.TokenInvalid, null, exception.MessagingErrorCode.ToString());
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to send screenshot command to device {DeviceId}.",
                deviceId);

            return new DeviceCommandDispatchResult(DeviceCommandDispatchStatus.Failed, null, exception.Message);
        }
    }

    private static bool IsInvalidToken(FirebaseMessagingException exception)
    {
        return exception.MessagingErrorCode is MessagingErrorCode.InvalidArgument or MessagingErrorCode.Unregistered;
    }
}
