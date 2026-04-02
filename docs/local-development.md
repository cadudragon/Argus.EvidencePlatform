# Local Development

This repository is currently operating in `functional mode` for local validation.
See [v1-functional-scope.md](./v1-functional-scope.md) for the explicit split between what is mandatory now and what is deferred for compliance.

## Local dependencies

Start PostgreSQL and Azurite:

```powershell
docker compose up -d
```

The local stack uses:
- PostgreSQL on `localhost:5432`
- Azurite blob/queue/table endpoints on `localhost:10000-10002`

In `Development`, the API now bootstraps the relational schema and blob containers automatically on startup.
That bootstrap is controlled by `Infrastructure:BootstrapOnStartup=true` in [appsettings.Development.json](../src/Argus.EvidencePlatform.Api/appsettings.Development.json).

## Local authentication

`Development` and `Testing` use `Authentication:Mode=Mock`.
Production-oriented environments stay on JWT.

## Firebase validation

Firebase is not mocked.
The runtime initializes the real Firebase Admin SDK only when `Firebase:Enabled=true`.

Example local overrides:

```powershell
$env:Firebase__Enabled = "true"
$env:Firebase__ProjectId = "your-firebase-project-id"
$env:Firebase__ServiceAccountPath = "C:\\secrets\\firebase-service-account.json"
```

Relative `Firebase__ServiceAccountPath` values are resolved from the application content root.

## Manual local E2E

Start the dependencies:

```powershell
docker compose up -d
```

Start the API:

```powershell
dotnet run --project src/Argus.EvidencePlatform.Api --launch-profile http
```

Run the end-to-end script:

```powershell
.\scripts\e2e-local.ps1
```

The script creates a case, ingests one artifact, reads the timeline, queues an export job, reads the export job, and reads the audit trail.
