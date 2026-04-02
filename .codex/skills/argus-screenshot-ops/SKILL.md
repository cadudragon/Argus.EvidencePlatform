---
name: argus-screenshot-ops
description: Trigger on-demand screenshots, verify screenshot ingestion, and export screenshot blobs for the local Argus EvidencePlatform backend on Windows. Use when Codex needs to send the Firebase screenshot command, check the latest screenshot evidence in PostgreSQL, or download screenshots from Azurite into a local folder for inspection.
---

# Argus Screenshot Ops

Use the repository scripts in [scripts](C:\Src\Argus.EvidencePlatform\scripts) for all screenshot operational flows.

## Runbook

1. Ensure the local runtime is healthy.
2. Trigger a screenshot with [request-screenshot.ps1](C:\Src\Argus.EvidencePlatform\scripts\request-screenshot.ps1).
3. Verify ingestion with [check-latest-screenshots.ps1](C:\Src\Argus.EvidencePlatform\scripts\check-latest-screenshots.ps1).
4. Export the blobs with [export-case-screenshots.ps1](C:\Src\Argus.EvidencePlatform\scripts\export-case-screenshots.ps1).

## Parameters

- `request-screenshot.ps1` requires `-DeviceId`
- `check-latest-screenshots.ps1` requires `-CaseExternalId`
- `export-case-screenshots.ps1` requires `-CaseExternalId`

## Constraints

- `/api/screenshots` is the only current endpoint with request gzip support.
- Screenshot commands depend on a valid `fcmToken` already persisted for the device.
- Repeated `404` entries for `/api/notifications` are expected until that slice is implemented.
