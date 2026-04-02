using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public sealed class FirebaseAppAccessor : IDisposable
{
    public FirebaseAppAccessor(
        IOptions<FirebaseOptions> options,
        IHostEnvironment hostEnvironment,
        ILogger<FirebaseAppAccessor> logger)
    {
        var firebaseOptions = options.Value;

        if (!firebaseOptions.Enabled)
        {
            logger.LogInformation("Firebase integration is disabled.");
            return;
        }

        if (string.IsNullOrWhiteSpace(firebaseOptions.ServiceAccountPath))
        {
            throw new InvalidOperationException(
                "Firebase is enabled but Firebase:ServiceAccountPath is not configured.");
        }

        var serviceAccountPath = ResolvePath(firebaseOptions.ServiceAccountPath, hostEnvironment.ContentRootPath);
        var googleCredential = CredentialFactory
            .FromFile<ServiceAccountCredential>(serviceAccountPath)
            .ToGoogleCredential();
        var appOptions = new AppOptions
        {
            Credential = googleCredential
        };

        if (!string.IsNullOrWhiteSpace(firebaseOptions.ProjectId))
        {
            appOptions.ProjectId = firebaseOptions.ProjectId;
        }

        App = FirebaseApp.Create(appOptions, $"Argus.EvidencePlatform.{Guid.NewGuid():N}");

        logger.LogInformation(
            "Firebase Admin SDK initialized using service account {ServiceAccountPath}.",
            serviceAccountPath);
    }

    public FirebaseApp? App { get; }

    public bool IsConfigured => App is not null;

    public void Dispose()
    {
        App?.Delete();
    }

    private static string ResolvePath(string path, string contentRootPath)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(contentRootPath, path));
    }
}
