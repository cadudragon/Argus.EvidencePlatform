using Argus.EvidencePlatform.Domain.Firebase;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public sealed class FirebaseAppRegistry : IDisposable
{
    private readonly Dictionary<Guid, FirebaseApp> _apps = [];

    public FirebaseAppRegistry(
        IOptions<FirebaseOptions> options,
        IHostEnvironment hostEnvironment,
        ILogger<FirebaseAppRegistry> logger)
    {
        var firebaseOptions = options.Value;

        if (!firebaseOptions.Enabled)
        {
            logger.LogInformation("Firebase integration is disabled.");
            return;
        }

        foreach (var appOptions in firebaseOptions.Apps)
        {
            var registration = FirebaseAppRegistration.Create(
                appOptions.Key,
                string.IsNullOrWhiteSpace(appOptions.DisplayName) ? appOptions.Key : appOptions.DisplayName,
                appOptions.ProjectId,
                appOptions.ServiceAccountPath,
                appOptions.IsActiveForNewCases,
                DateTimeOffset.UtcNow);
            var serviceAccountPath = ResolvePath(registration.ServiceAccountPath, hostEnvironment.ContentRootPath);
            var googleCredential = CredentialFactory
                .FromFile<ServiceAccountCredential>(serviceAccountPath)
                .ToGoogleCredential();
            var firebaseApp = FirebaseApp.Create(
                new AppOptions
                {
                    Credential = googleCredential,
                    ProjectId = registration.ProjectId
                },
                $"Argus.EvidencePlatform.{registration.Key}.{registration.Id:N}");

            _apps[registration.Id] = firebaseApp;

            logger.LogInformation(
                "Firebase Admin SDK initialized for app {FirebaseAppKey} using service account {ServiceAccountPath}.",
                registration.Key,
                serviceAccountPath);
        }
    }

    public bool TryGet(Guid firebaseAppId, out FirebaseApp? firebaseApp)
    {
        var found = _apps.TryGetValue(firebaseAppId, out var resolvedApp);
        firebaseApp = resolvedApp;
        return found;
    }

    public void Dispose()
    {
        foreach (var app in _apps.Values)
        {
            app.Delete();
        }
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
