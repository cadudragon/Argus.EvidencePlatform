# Argus Backend API Spec

OpenAPI-style specification for the backend contract currently consumed by the Android app.

Status: `debug/MVP`

App package: `com.argus.android`

Debug base URL in local development:

```text
http://192.168.1.168:5058/api
```

## OpenAPI Summary

```yaml
openapi: 3.0.3
info:
  title: Argus Backend API
  version: 0.1.0-mvp
  description: >
    Current backend contract used by the Android client in debug/MVP mode.
servers:
  - url: http://192.168.1.168:5058/api
    description: Local Wi-Fi backend for debug
tags:
  - name: Enrollment
  - name: Capture
  - name: Device
paths:
  /activate:
    post:
      tags: [Enrollment]
      summary: Activate device using a 9-digit token
  /screenshots:
    post:
      tags: [Capture]
      summary: Upload a screenshot artifact
  /text-captures:
    post:
      tags: [Capture]
      summary: Upload extracted UI text nodes
  /notifications:
    post:
      tags: [Capture]
      summary: Upload a captured notification
  /pong:
    post:
      tags: [Device]
      summary: Send liveness response
  /fcm-token:
    put:
      tags: [Device]
      summary: Update the device FCM token
```

## Common Rules

- All JSON requests use `Content-Type: application/json; charset=utf-8`.
- Screenshot upload uses `multipart/form-data`.
- For non-enrollment endpoints, any `2xx` response is treated as success by the app.
- For non-enrollment endpoints, `401`, `403`, and `410` are treated as terminal server failures.
- For non-enrollment endpoints, any other non-`2xx` response is treated as retryable failure.
- The app expects `deviceId` to be present in all device-scoped operations.

## Schemas

### ActivationRequest

```json
{
  "token": "123456789",
  "deviceId": "android-0123456789abcdef"
}
```

### ActivationSuccessResponse

```json
{
  "caseId": "CASE-001",
  "validUntil": 1767225600000,
  "scope": ["screenshot", "notification", "text"]
}
```

Notes:

- `token` is sanitized by the app before submission.
- Effective format is 9 numeric digits.
- `validUntil` is epoch milliseconds.
- `scope` is optional in practice; the app accepts an empty list.

### TextCaptureItem

```json
{
  "packageName": "com.whatsapp",
  "className": "android.widget.TextView",
  "text": "Message content",
  "contentDescription": null
}
```

### NotificationCaptureRequest

```json
{
  "deviceId": "android-0123456789abcdef",
  "caseId": "CASE-001",
  "sha256": "3f786850e387550fdab836ed7e6dc881de23001b",
  "captureTimestamp": 1767225600000,
  "packageName": "com.whatsapp",
  "title": "Sender",
  "text": "Message preview",
  "bigText": "Expanded message preview",
  "timestamp": 1767225600000,
  "category": "msg"
}
```

### TextCaptureRequest

```json
{
  "deviceId": "android-0123456789abcdef",
  "caseId": "CASE-001",
  "sha256": "3f786850e387550fdab836ed7e6dc881de23001b",
  "captureTimestamp": 1767225600000,
  "captures": [
    {
      "packageName": "com.whatsapp",
      "className": "android.widget.TextView",
      "text": "Message content",
      "contentDescription": null
    }
  ]
}
```

### PongRequest

```json
{
  "deviceId": "android-0123456789abcdef"
}
```

### UpdateFcmTokenRequest

```json
{
  "deviceId": "android-0123456789abcdef",
  "fcmToken": "fcm-token-value"
}
```

## Endpoints

### `POST /activate`

Activates the device using the enrollment token.

Request body:

```json
{
  "token": "123456789",
  "deviceId": "android-0123456789abcdef"
}
```

Success response: `200 OK`

```json
{
  "caseId": "CASE-001",
  "validUntil": 1767225600000,
  "scope": ["screenshot", "notification", "text"]
}
```

Expected status handling:

- `200`: activation success
- `404`: invalid token
- `410`: expired or already used token
- any other status: generic activation error

### `POST /screenshots`

Uploads one screenshot file plus capture metadata.

Request content type:

```text
multipart/form-data
```

Form fields:

- `deviceId`: string
- `sha256`: string
- `caseId`: string
- `captureTimestamp`: stringified epoch millis
- `image`: file field, filename `capture.jpg`, MIME `image/jpeg`

Minimal success response:

```http
HTTP/1.1 200 OK
```

Expected status handling:

- any `2xx`: success
- `401`, `403`, `410`: terminal failure
- any other non-`2xx`: retryable failure

### `POST /text-captures`

Uploads extracted text nodes from the accessibility tree.

Request body:

```json
{
  "deviceId": "android-0123456789abcdef",
  "caseId": "CASE-001",
  "sha256": "3f786850e387550fdab836ed7e6dc881de23001b",
  "captureTimestamp": 1767225600000,
  "captures": [
    {
      "packageName": "com.whatsapp",
      "className": "android.widget.TextView",
      "text": "Message content",
      "contentDescription": null
    }
  ]
}
```

Minimal success response:

```http
HTTP/1.1 200 OK
```

Expected status handling:

- any `2xx`: success
- `401`, `403`, `410`: terminal failure
- any other non-`2xx`: retryable failure

### `POST /notifications`

Uploads one notification snapshot.

Request body:

```json
{
  "deviceId": "android-0123456789abcdef",
  "caseId": "CASE-001",
  "sha256": "3f786850e387550fdab836ed7e6dc881de23001b",
  "captureTimestamp": 1767225600000,
  "packageName": "com.whatsapp",
  "title": "Sender",
  "text": "Message preview",
  "bigText": "Expanded message preview",
  "timestamp": 1767225600000,
  "category": "msg"
}
```

Minimal success response:

```http
HTTP/1.1 200 OK
```

Expected status handling:

- any `2xx`: success
- `401`, `403`, `410`: terminal failure
- any other non-`2xx`: retryable failure

### `POST /pong`

Liveness endpoint used by the app in response to `ping`.

Request body:

```json
{
  "deviceId": "android-0123456789abcdef"
}
```

Minimal success response:

```http
HTTP/1.1 200 OK
```

Expected status handling:

- any `2xx`: success
- `401`, `403`, `410`: terminal failure
- any other non-`2xx`: retryable failure

### `PUT /fcm-token`

Stores or updates the device FCM token.

Request body:

```json
{
  "deviceId": "android-0123456789abcdef",
  "fcmToken": "fcm-token-value"
}
```

Minimal success response:

```http
HTTP/1.1 200 OK
```

Expected status handling:

- any `2xx`: success
- `401`, `403`, `410`: terminal failure
- any other non-`2xx`: retryable failure

## FCM Command Contract

This is not an HTTP endpoint, but it is part of the backend contract.

Supported data payloads:

```json
{ "cmd": "screenshot" }
{ "cmd": "stream_start", "fps": "2" }
{ "cmd": "stream_stop" }
{ "cmd": "ping" }
```

Rules:

- command key is `cmd`
- `fps` must be an integer between `1` and `10`
- malformed commands are ignored by the app
- duplicate, expired, and rate-limited messages are discarded by the app

## Notes For Backend Implementation

- The current MVP backend only needs to satisfy the request/response contracts above.
- The app does not currently require response bodies for:
  - `/screenshots`
  - `/text-captures`
  - `/notifications`
  - `/pong`
  - `/fcm-token`
- The app currently sends only:
  - `deviceId`
  - `caseId`
  - `sha256`
  - `captureTimestamp`
  - endpoint-specific payload fields
- Although the domain model contains `signature`, `rfc3161Token`, `auditLogHash`, and `integrityToken`, those values are not yet serialized in the current HTTP contract.
