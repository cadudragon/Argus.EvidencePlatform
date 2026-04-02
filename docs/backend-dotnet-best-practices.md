# Argus Backend Best Practices for .NET

Scope: backend for the current Android client `com.argus.android`

Current app contract:

- `POST /api/activate`
- `POST /api/screenshots`
- `POST /api/text-captures`
- `POST /api/notifications`
- `POST /api/pong`
- `PUT /api/fcm-token`

Current command channel:

- Firebase Cloud Messaging data messages

Current package/app id:

```text
com.argus.android
```

## Objectives

- keep device identity stable through `deviceId`
- associate every device to a `caseId`
- store and update the current `fcmToken`
- send commands to the correct device through Firebase Admin SDK
- receive media and persist it under the correct case/device
- avoid fragile integration details such as unexpected gzip handling

## Recommended Architecture

Use a simple layered backend:

- API layer
- application/service layer
- persistence layer
- background jobs only when necessary

Recommended responsibilities:

- `ActivationController`: activation token flow
- `DeviceController`: FCM token update, pong, device status
- `CaptureController`: screenshots, text captures, notifications
- `FcmCommandService`: send FCM data messages
- `DeviceRepository`: resolve `deviceId`, `caseId`, `fcmToken`
- `CaptureStorageService`: persist files and metadata

## Required Configuration

Minimum config:

```json
{
  "Firebase": {
    "ProjectId": "your-project-id",
    "CredentialsPath": "C:\\secrets\\firebase-admin.json"
  }
}
```

Do not commit service account files to source control.

Prefer one of these, in order:

1. secret manager
2. environment variable pointing to a secure path
3. mounted secret volume
4. local developer-only file outside the repo

## Identity Model

Treat these identifiers differently:

- `deviceId`: technical identity of the Android device
- `caseId`: investigative/business grouping
- `fcmToken`: current delivery token for Firebase

Recommended rule:

- `deviceId` is the primary lookup key for device operations
- `caseId` groups one or more devices
- `fcmToken` is mutable and should always be replaceable

## Minimum Data Model

### Devices

Suggested columns:

- `DeviceId` string unique
- `CaseId` string indexed
- `FcmToken` string nullable
- `ActivatedAtUtc` datetime
- `ValidUntilUtc` datetime
- `Status` string
- `LastSeenAtUtc` datetime nullable
- `LastPongAtUtc` datetime nullable

### Captures

Suggested columns:

- `Id`
- `CaseId`
- `DeviceId`
- `CaptureType`
- `Sha256`
- `CaptureTimestampUtc`
- `StoragePath`
- `CreatedAtUtc`

### ActivationTokens

Suggested columns:

- `Token`
- `CaseId`
- `ValidUntilUtc`
- `UsedAtUtc` nullable
- `Status`

## Activation Flow

Endpoint:

```text
POST /api/activate
```

Expected request:

```json
{
  "token": "123456789",
  "deviceId": "android-0123456789abcdef"
}
```

Best practices:

- validate request body strictly
- accept plain JSON, not gzip, for this endpoint
- normalize the token before lookup
- resolve `token -> caseId`
- persist or update `deviceId -> caseId`
- return only what the app needs

Expected success response:

```json
{
  "caseId": "CASE-001",
  "validUntil": 1767225600000,
  "scope": ["screenshot", "notification", "text"]
}
```

Recommended status mapping:

- `200` success
- `404` invalid token
- `410` expired or already used token
- `400` invalid request payload
- `500` unexpected server failure

Do not use `500` for request-shape problems. Return `400` with useful validation errors.

## FCM Token Update

Endpoint:

```text
PUT /api/fcm-token
```

Expected request:

```json
{
  "deviceId": "android-0123456789abcdef",
  "fcmToken": "fcm-token-value"
}
```

Best practices:

- upsert by `deviceId`
- overwrite old tokens safely
- track update timestamp
- log token refresh events without logging the full raw token in production logs

## Command Delivery via Firebase

Use Firebase Admin SDK.

Current supported command payloads:

```json
{ "cmd": "screenshot" }
{ "cmd": "ping" }
{ "cmd": "stream_start", "fps": "2" }
{ "cmd": "stream_stop" }
```

Best practices:

- send data messages, not notification messages
- fetch the current `fcmToken` by `deviceId`
- log the Firebase `messageId`
- handle token invalidation explicitly
- when Firebase reports invalid/unregistered token, mark the token stale

### Sample .NET service

```csharp
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

public sealed class FirebaseOptions
{
    public string ProjectId { get; set; } = "";
    public string CredentialsPath { get; set; } = "";
}

public sealed class FcmCommandService
{
    public async Task<string> SendScreenshotAsync(string fcmToken, CancellationToken ct = default)
    {
        var message = new Message
        {
            Token = fcmToken,
            Data = new Dictionary<string, string>
            {
                ["cmd"] = "screenshot"
            }
        };

        return await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
    }
}
```

### Initialization

```csharp
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var firebase = builder.Configuration.GetSection("Firebase");

FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(firebase["CredentialsPath"]!),
    ProjectId = firebase["ProjectId"]
});
```

## Media Upload Handling

### Screenshot Upload

Endpoint:

```text
POST /api/screenshots
```

Current app behavior:

- `multipart/form-data`
- `Content-Encoding: gzip`

Required form fields:

- `deviceId`
- `sha256`
- `caseId`
- `captureTimestamp`
- `image`

Best practices:

- explicitly support gzip on this endpoint
- support multipart streaming rather than buffering everything into memory
- enforce max request size
- validate file presence and MIME type defensively
- compute server-side checksum if chain-of-custody matters
- store file and metadata atomically or with compensating cleanup

### Text Captures

Endpoint:

```text
POST /api/text-captures
```

Current app behavior:

- plain JSON
- no gzip

Best practices:

- validate array length and field sizes
- reject pathological payloads early with `400`
- persist capture metadata and source package names

### Notifications

Endpoint:

```text
POST /api/notifications
```

Current app behavior:

- plain JSON
- no gzip

Best practices:

- validate required fields
- sanitize and cap oversized text
- store the original timestamps sent by the device

## Pong Handling

Endpoint:

```text
POST /api/pong
```

Use this endpoint to:

- update `LastSeenAtUtc`
- update `LastPongAtUtc`
- clear temporary offline markers if any

Do not make `pong` heavy.

## Validation and Error Handling

Best practices:

- return `400` for malformed payloads
- return `404` only when the requested activation token or device does not exist
- return `410` for expired or invalidated activation state
- reserve `500` for actual server failures
- include correlation IDs in logs

Recommended approach:

- FluentValidation or minimal explicit validators
- global exception middleware
- ProblemDetails for API errors

## Gzip Handling

Current app contract:

- gzip only on `POST /api/screenshots`
- all other current endpoints are plain JSON without gzip

Best practices:

- do not assume all endpoints are gzip-compressed
- support gzip only where required by contract
- avoid transparent heuristics that guess payload encoding
- validate `Content-Encoding` explicitly

## Security

- require HTTPS outside local MVP
- never log full activation tokens or full FCM tokens in production
- never commit Firebase service account JSON
- validate `deviceId` shape, length, and duplication rules
- use request size limits on media endpoints
- restrict service account permissions to messaging use

## Observability

At minimum, log:

- `deviceId`
- `caseId`
- endpoint name
- HTTP status
- Firebase `messageId`
- storage path or capture id
- error category

Do not log raw media bytes or full secret values.

## Operational Recommendations

- add a health endpoint for the backend
- add a simple admin endpoint or internal tool to trigger commands by `deviceId`
- add retry only for outbound FCM if failure is transient
- mark bad FCM tokens inactive after confirmed invalidation
- maintain idempotency where practical on token update and media ingestion

## Minimal Controller Set

Recommended first cut:

- `POST /api/activate`
- `PUT /api/fcm-token`
- `POST /api/screenshots`
- `POST /api/text-captures`
- `POST /api/notifications`
- `POST /api/pong`
- internal/admin command trigger by `deviceId`

## MVP Readiness Checklist

- Firebase Admin SDK configured
- service account loaded securely
- `deviceId -> caseId` persistence working
- `deviceId -> fcmToken` persistence working
- screenshot command dispatch working
- screenshot endpoint accepts gzip multipart
- activation endpoint accepts plain JSON and returns correct status codes
- media saved under the correct case/device path
- logs correlate upload, command dispatch, and device identity
