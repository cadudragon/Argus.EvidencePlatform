# Argus EvidencePlatform Agent Notes

## Scope

This repository is the .NET backend for the Android client `com.argus.android`.

Keep this file technical and operational. Do not place soft product rationale here.

Canonical planning references:

- [docs/backend-build-plan.md](C:\Src\Argus.EvidencePlatform\docs\backend-build-plan.md)
- [docs/backend-build-runbook.md](C:\Src\Argus.EvidencePlatform\docs\backend-build-runbook.md)
- [docs/backend-bb-07.1-multi-firebase-plan.md](C:\Src\Argus.EvidencePlatform\docs\backend-bb-07.1-multi-firebase-plan.md)

## Architecture

- Vertical slices under [src/Argus.EvidencePlatform.Api/Features](C:\Src\Argus.EvidencePlatform\src\Argus.EvidencePlatform.Api\Features)
- Main slices currently relevant to device ingestion and command delivery:
  - `Enrollment`
  - `Device`
  - `Screenshots`
  - `Evidence`
- Application handlers live under [src/Argus.EvidencePlatform.Application](C:\Src\Argus.EvidencePlatform\src\Argus.EvidencePlatform.Application)
- Infrastructure adapters live under [src/Argus.EvidencePlatform.Infrastructure](C:\Src\Argus.EvidencePlatform\src\Argus.EvidencePlatform.Infrastructure)

## Local Runtime

The local stack assumes Docker Desktop with the Linux engine running.

Dependencies from [compose.yaml](C:\Src\Argus.EvidencePlatform\compose.yaml):
- PostgreSQL 16
- Azurite `3.35.0`

Ports:
- API: `5058`
- PostgreSQL: `5432`
- Azurite blob/queue/table: `10000-10002`

Runbooks are scripted in [scripts](C:\Src\Argus.EvidencePlatform\scripts):
- [ensure-runtime-deps.ps1](C:\Src\Argus.EvidencePlatform\scripts\ensure-runtime-deps.ps1)
- [start-runtime.ps1](C:\Src\Argus.EvidencePlatform\scripts\start-runtime.ps1)
- [check-runtime.ps1](C:\Src\Argus.EvidencePlatform\scripts\check-runtime.ps1)

Important:
- Use the absolute `dotnet.exe` path when the Windows `PATH` is unreliable:
  - `C:\Program Files\dotnet\dotnet.exe`
- Use PowerShell script files for multi-step local operations. Inline PowerShell through the terminal shell in this workspace is unreliable and frequently mangles quoting.

## Current Local Paths

These are current local-machine conventions used in this repository workflow:

- API stdout log: `C:\Src\argus-api.stdout.log`
- API stderr log: `C:\Src\argus-api.stderr.log`
- Temporary exported screenshots folder: `C:\Src\exported-screenshots`
- Firebase service-account JSON stays outside the repo and is referenced by user-secrets

## Current Operational Test State

This state is transient. Update it when local testing rotates to a new case or device.

- Last known local Wi-Fi API base URL: `http://192.168.1.68:5058`
- Current validated Android package id: `com.argus.android`
- Last working local device id: `android-d4e40efbdeb91b34`
- Last working local case external id for screenshot tests: `CASE-ACT-20260403-194200`

Treat these values as operational hints, not stable domain constants.

## Current Device-Facing Contracts

### Activation

- `POST /api/activate`
- Plain JSON only
- No gzip

### FCM Token Binding

- `PUT /api/fcm-token`
- Plain JSON only

### Screenshot Ingestion

- `POST /api/screenshots`
- This is the only current endpoint that accepts request `gzip`
- Expected transport:
  - `multipart/form-data`
  - `Content-Encoding: gzip`

Implementation detail:
- Gzip support is intentionally scoped to `/api/screenshots` in [Program.cs](C:\Src\Argus.EvidencePlatform\src\Argus.EvidencePlatform.Api\Program.cs)
- Do not reintroduce global request decompression unless the contract changes

### Notification Ingestion

- `POST /api/notifications`
- Plain JSON only
- No gzip
- Persists notification metadata in PostgreSQL after validating `deviceId + caseId`

### Text Capture Ingestion

- `POST /api/text-captures`
- Plain JSON only
- No gzip
- Persists text-capture batches in PostgreSQL after validating `deviceId + caseId`

### On-Demand Screenshot Command

- `POST /api/device-commands/screenshot`
- Command dispatch uses Firebase Admin SDK
- Message payload currently sent to the Android app:
  - `{"cmd":"screenshot"}`

The operational helpers are:
- [request-screenshot.ps1](C:\Src\Argus.EvidencePlatform\scripts\request-screenshot.ps1)
- [check-latest-screenshots.ps1](C:\Src\Argus.EvidencePlatform\scripts\check-latest-screenshots.ps1)
- [export-case-screenshots.ps1](C:\Src\Argus.EvidencePlatform\scripts\export-case-screenshots.ps1)

## Firebase Configuration

The backend code currently reads:
- `Firebase:Enabled`
- `Firebase:Apps:{n}:Key`
- `Firebase:Apps:{n}:DisplayName`
- `Firebase:Apps:{n}:ProjectId`
- `Firebase:Apps:{n}:ServiceAccountPath`
- `Firebase:Apps:{n}:IsActiveForNewCases`

For local development, store these with `dotnet user-secrets` on the API project. Do not commit service-account JSON files.

Important:
- `POST /api/cases` now returns `503` when the backend cannot resolve exactly one eligible Firebase app for new cases
- `Firebase:Apps:{n}:ProjectId` must be the Firebase project id, not the Android package name
- `Firebase:Apps:{n}:ServiceAccountPath` must point to a real JSON file on disk
- `Case.FirebaseAppId` is now the source of truth for command routing
- `FcmTokenBinding.FirebaseAppId` is persisted as routing/audit context for the current token

Current architecture:

- multi-Firebase support by case is implemented before `BB-08`
- `BB-07.2` cleaned dead compliance/scaffold concepts before `BB-08`
- `BB-07.3` moved relational schema evolution to EF Core migrations-first
- application schema DDL no longer lives in runtime bootstrap SQL
- existing local databases are adopted automatically into `__EFMigrationsHistory` before pending migrations are applied
- legacy drift from pre-`BB-07.2` columns such as `ImmutabilityState`, `LegalHoldState`, `ManifestBlobName`, and `PackageBlobName` is reconciled by the EF migration `ReconcileLegacySchema`
- the next structural backend block is `BB-08`
- case creation assigns exactly one active Firebase app for new cases
- device activation materializes the case assignment, not choose the Firebase app
- `PUT /api/fcm-token` and `POST /api/device-commands/screenshot` resolve routing from the case/device context
- detailed implementation planning and closure criteria live in [docs/backend-bb-07.1-multi-firebase-plan.md](C:\Src\Argus.EvidencePlatform\docs\backend-bb-07.1-multi-firebase-plan.md)

## Storage

Local binary evidence goes to Azurite via `UseDevelopmentStorage=true`.

For screenshots:
- metadata is stored in PostgreSQL
- binary content is stored in Azurite blobs
- current write path stores screenshot and generic evidence blobs in container `staging`
- `evidence` is not part of the functional runtime path today
- `exports` remains a reserved container name for future export materialization, not a completed export pipeline

The export helper downloads screenshots for a case into a local folder:
- [export-case-screenshots.ps1](C:\Src\Argus.EvidencePlatform\scripts\export-case-screenshots.ps1)

## Known Technical Constraints

- The screenshot ingestion contract uses `captureTimestamp` as Unix epoch milliseconds from Android. Backend code already normalizes this.
- Local Firebase testing depends on a valid `fcmToken` being persisted for the active `deviceId`.
- Existing local databases from before `BB-07.3` may still start without `__EFMigrationsHistory`; runtime adoption into migrations history is now part of startup behavior.
- Do not reintroduce `EnsureCreated`, `CreateTables`, or runtime DDL for application schema. Schema evolution belongs in EF Core migrations under `src/Argus.EvidencePlatform.Infrastructure/Persistence/Migrations`.

## What Belongs Here vs Skills

Keep in `AGENT.md`:
- repository-specific architecture
- local runtime assumptions
- exact config keys
- endpoint transport constraints
- local infrastructure dependencies
- current log paths and local runbook conventions

Prefer skills plus scripts for:
- starting and checking the local stack
- triggering on-demand screenshots
- exporting screenshot blobs to local files
- DB inspection workflows that are repetitive and procedural
