param()

$ErrorActionPreference = "Stop"

$docker = "C:\Program Files\Docker\Docker\resources\bin\docker.exe"

& $docker compose -f "C:\Src\Argus.EvidencePlatform\compose.yaml" up -d postgres azurite
& $docker ps --format "table {{.Names}}`t{{.Status}}`t{{.Ports}}"
