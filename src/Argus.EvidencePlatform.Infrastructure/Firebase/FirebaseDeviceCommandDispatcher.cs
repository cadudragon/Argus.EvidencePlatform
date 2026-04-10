using Argus.EvidencePlatform.Application.Common.Abstractions;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public sealed class FirebaseDeviceCommandDispatcher(
    FirebaseAppRegistry firebaseAppRegistry,
    IFcmCommandDataPayloadBuilder payloadBuilder,
    ILogger<FirebaseDeviceCommandDispatcher> logger) : IDeviceCommandDispatcher
{
    public async Task<DeviceCommandDispatchResult> DispatchAsync(
        DeviceCommandDispatchRequest request,
        CancellationToken cancellationToken)
    {
        if (!firebaseAppRegistry.TryGet(request.FirebaseAppId, out var firebaseApp) || firebaseApp is null)
        {
            logger.LogError(
                "Firebase command dispatch requested for device {DeviceId}, but Firebase app {FirebaseAppId} is not configured.",
                request.DeviceId,
                request.FirebaseAppId);
            return new DeviceCommandDispatchResult(DeviceCommandDispatchStatus.Failed, null, "firebase_not_configured");
        }

        try
        {
            var messageId = await FirebaseMessaging.GetMessaging(firebaseApp).SendAsync(
                new Message
                {
                    Token = request.FcmToken,
                    Data = payloadBuilder.Build(request)
                },
                cancellationToken);

            logger.LogInformation(
                "FCM command {Command} sent to device {DeviceId} with message id {MessageId}.",
                request.Command,
                request.DeviceId,
                messageId);

            return new DeviceCommandDispatchResult(DeviceCommandDispatchStatus.Success, messageId);
        }
        catch (FirebaseMessagingException exception) when (IsInvalidToken(exception))
        {
            logger.LogWarning(
                exception,
                "FCM token rejected for device {DeviceId}; binding will be invalidated.",
                request.DeviceId);

            return new DeviceCommandDispatchResult(DeviceCommandDispatchStatus.TokenInvalid, null, exception.MessagingErrorCode.ToString());
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to send screenshot command to device {DeviceId}.",
                request.DeviceId);

            return new DeviceCommandDispatchResult(DeviceCommandDispatchStatus.Failed, null, exception.Message);
        }
    }

    private static bool IsInvalidToken(FirebaseMessagingException exception)
    {
        return exception.MessagingErrorCode is MessagingErrorCode.InvalidArgument or MessagingErrorCode.Unregistered;
    }
}
