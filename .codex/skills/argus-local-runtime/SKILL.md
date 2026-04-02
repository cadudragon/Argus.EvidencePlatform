---
name: argus-local-runtime
description: Start, restart, and verify the local Argus EvidencePlatform backend stack on Windows using Docker Desktop, PostgreSQL, Azurite, and the API on port 5058. Use when Codex needs to bring up local dependencies, restart the backend, inspect health, or recover from broken Windows PATH and shell quoting during local backend operations in this repository.
---

# Argus Local Runtime

Use the repository scripts in [scripts](C:\Src\Argus.EvidencePlatform\scripts) instead of ad hoc inline commands.

## Runbook

1. Ensure Docker Desktop and the Linux engine are up.
2. Run [ensure-runtime-deps.ps1](C:\Src\Argus.EvidencePlatform\scripts\ensure-runtime-deps.ps1).
3. Run [start-runtime.ps1](C:\Src\Argus.EvidencePlatform\scripts\start-runtime.ps1).
4. Run [check-runtime.ps1](C:\Src\Argus.EvidencePlatform\scripts\check-runtime.ps1).

## Constraints

- Use `C:\Program Files\dotnet\dotnet.exe` explicitly.
- Prefer `.ps1` files over inline PowerShell in this workspace.
- Treat `127.0.0.1:5432` failures as a Docker/PostgreSQL readiness issue first.
- Treat Azurite as mandatory for local evidence storage.
