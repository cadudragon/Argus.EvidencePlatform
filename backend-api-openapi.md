# Argus Backend API Spec

OpenAPI-style specification for the backend contract currently consumed by the Android app.

Status: `debug/MVP`

App package: `com.argus.android`

Debug base URL in local development:

```text
http://192.168.1.68:5058/api
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
  - url: http://192.168.1.68:5058/api
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
  /device-commands/screenshot:
    post:
      tags: [Device]
      summary: Request an on-demand screenshot through encrypted FCM command data
```

## Common Rules

- All JSON requests use `Content-Type: application/json; charset=utf-8`.
- Screenshot upload uses `multipart/form-data` with `Content-Encoding: gzip`.
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
  "fcmToken": "fcm-token-value",
  "fcmCommandKey": {
    "alg": "ECDH-P256",
    "kid": "device-ecdh-android-0123456789abcdef",
    "publicKey": "base64url-no-padding-spki-p256-public-key"
  }
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

Required content encoding:

```text
gzip
```

Form fields:

- `deviceId`: string
- `sha256`: string
- `caseId`: string
- `captureTimestamp`: stringified epoch millis
- `image`: file field, filename `capture.jpg`, MIME `image/jpeg`

Minimal success response:

```http
HTTP/1.1 2xx
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
  "fcmToken": "fcm-token-value",
  "fcmCommandKey": {
    "alg": "ECDH-P256",
    "kid": "device-ecdh-android-0123456789abcdef",
    "publicKey": "base64url-no-padding-spki-p256-public-key"
  }
}
```

Rules:

- `fcmCommandKey.alg` must be `ECDH-P256`.
- `fcmCommandKey.publicKey` is a base64url-no-padding SPKI P-256 ECDH public key generated by the device.
- The corresponding device private key never leaves the Android Keystore.
- The backend stores the public key and uses it to encrypt FCM command envelopes for this device.

Minimal success response:

```http
HTTP/1.1 200 OK
```

Expected status handling:

- any `2xx`: success
- `401`, `403`, `410`: terminal failure
- any other non-`2xx`: retryable failure

### `POST /device-commands/screenshot`

Requests one on-demand screenshot for a device. This endpoint is backend/operator-facing; it dispatches an encrypted FCM command to the Android app.

Request body:

```json
{
  "deviceId": "android-0123456789abcdef"
}
```

Success response: `202 Accepted`

```json
{
  "deviceId": "android-0123456789abcdef",
  "caseId": "CASE-001",
  "messageId": "projects/project-id/messages/message-id"
}
```

Rules:

- The backend resolves the current `fcmToken` and `fcmCommandKey` for the device.
- The outbound FCM data payload uses the encrypted command envelope below.
- If the device binding has no command key, the request fails instead of sending plaintext FCM command data.

## FCM Command Contract

This is not an HTTP endpoint, but it is part of the backend contract.

Production data payload:

```json
{
  "enc": "1",
  "alg": "ECDH-P256-HKDF-SHA256+A256GCM",
  "kid": "backend-ecdh-key-id",
  "dkid": "device-ecdh-android-0123456789abcdef",
  "iv": "base64url-no-padding-12-byte-iv",
  "ct": "base64url-no-padding-ciphertext-plus-gcm-tag"
}
```

Internal encrypted payload:

```json
{
  "cmd": "screenshot",
  "iat": 1775124000000,
  "exp": 1775124060000,
  "nonce": "command-nonce"
}
```

Rules:

- FCM production data payload must contain only `enc`, `alg`, `kid`, `dkid`, `iv`, and `ct`.
- Plaintext `cmd` is not the production contract.
- Supported decrypted commands are `screenshot`, `stream_start`, `stream_stop`, and `ping`.
- `stream_start.fps`, when present after decryption, must be an integer between `1` and `10`.
- The backend derives the AES-GCM key with raw ECDH P-256 shared secret plus HKDF-SHA256.
- The Android app rejects malformed, expired, replayed, or unauthenticated messages before dispatching commands.

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
