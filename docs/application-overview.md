# Argus Evidence Platform: Visão Geral

## O que esta app é hoje

`Argus.EvidencePlatform` é um backend `.NET 10` em modo `local-first`, organizado como `Modular Monolith` com `Vertical Slices`.

Hoje ele já funciona como:

- backend HTTP para criação e consulta de casos;
- backend de ingestão de artefactos binários;
- timeline de artefactos por caso;
- leitura HTTP mínima de artefactos por caso com download em streaming;
- criação e consulta de jobs de export em estado de fila;
- trilha de auditoria por caso;
- pontos adicionais de `activate`, `pong`, `fcm-token`, `notifications` e `text-captures` que já existem no código atual.

Ele **não é** hoje um sistema cloud endurecido nem um produto final de compliance.  
O projeto está em `functional mode`, com foco em validação local e evolução incremental.

Referências relacionadas:

- [v1-functional-scope.md](./v1-functional-scope.md)
- [local-development.md](./local-development.md)

## Stack

- `ASP.NET Core Minimal APIs`
- `Wolverine` para handlers/slices
- `EF Core + PostgreSQL`
- `Azure Blob Storage SDK`, usando `Azurite` em local
- `OpenTelemetry`
- `Docker Compose` para dependências locais

## Como arrancar localmente

Subir dependências:

```powershell
docker compose up -d
```

Arrancar a API:

```powershell
dotnet run --project src/Argus.EvidencePlatform.Api --launch-profile http
```

Base URL local típica:

```text
http://localhost:5058/api
```

Na mesma Wi-Fi, quando a API é arrancada com bind externo:

```text
http://192.168.1.168:5058/api
```

Health checks:

- `GET /health/live`
- `GET /health/ready`

## Organização por slices

### 1. Cases

Responsabilidade:

- criar o agregado principal do sistema;
- consultar um caso existente.

Slices:

- `Cases/CreateCase`
- `Cases/GetCase`

Endpoints:

- `POST /api/cases`
- `GET /api/cases/{id}`

Fluxo:

1. criar um caso;
2. usar o `Guid` devolvido como `caseId` nas restantes operações.

### 2. Evidence

Responsabilidade:

- receber um artefacto associado a um caso;
- listar a timeline de artefactos de um caso.

Slices:

- `Evidence/IngestArtifact`
- `Evidence/GetTimeline`
- `Evidence/ListArtifacts`
- `Evidence/GetArtifactContent`

Endpoints:

- `POST /api/evidence/artifacts`
- `GET /api/evidence/cases/{caseId}/timeline`
- `GET /api/evidence/cases/{caseId}/artifacts`
- `GET /api/evidence/artifacts/{artifactId}/content`

Fluxo:

1. enviar um ficheiro via `multipart/form-data`;
2. o backend preserva metadados e blob;
3. consultar a timeline para ver os itens recebidos;
4. listar artefactos e descarregar conteúdo por HTTP quando for preciso ler o binário.

Nota operacional:

- o caminho preferencial de leitura mínima após `BB-08` é `GET /api/evidence/cases/{caseId}/artifacts` seguido de `GET /api/evidence/artifacts/{artifactId}/content`
- o export do Azurite continua útil como fallback operacional, mas deixou de ser a prova principal do read path

### 3. Exports

Responsabilidade:

- criar um job de export para um caso;
- consultar o estado desse job.

Slices:

- `Exports/CreateCaseExport`
- `Exports/GetExportJob`

Endpoints:

- `POST /api/exports`
- `GET /api/exports/{id}`

Fluxo:

1. pedir um export para um caso;
2. receber um `exportJobId`;
3. consultar o estado com `GET`.

Nota operacional:

- o backend ainda não gera package final, manifest final nem artefacto final de export;
- os exports atuais são metadata de fila e auditoria.

### 4. Audit

Responsabilidade:

- devolver a trilha de auditoria de um caso.

Slice:

- `Audit/GetCaseAuditTrail`

Endpoint:

- `GET /api/audit/cases/{caseId}`

Fluxo:

1. criar caso;
2. ingerir artefacto;
3. pedir export;
4. consultar audit trail para ver os eventos gerados.

### 5. Enrollment

Responsabilidade:

- ativar um `deviceId` com base num token de ativação já emitido.

Slice:

- `Enrollment/ActivateDevice`

Endpoint:

- `POST /api/activate`

Observação:

- esta slice já está mapeada e exposta na API;
- o contrato público existe em `Contracts/Enrollment`;
- a emissão de tokens é hoje uma slice interna, sem endpoint HTTP público.

### 6. Device

Responsabilidade:

- registar `pong` para um `deviceId`;
- associar ou atualizar um `fcmToken` por `deviceId`.

Slices:

- `Device/RecordPong`
- `Device/BindFcmToken`

Endpoints:

- `POST /api/pong`
- `PUT /api/fcm-token`

### 7. Notifications

Responsabilidade:

- receber snapshots de notificações do device;
- validar `deviceId + caseId`;
- persistir metadata relacional com trilha de auditoria.

Slice:

- `Notifications/IngestNotification`

Endpoint:

- `POST /api/notifications`

### 8. Text Captures

Responsabilidade:

- receber batches de texto extraído da accessibility tree;
- validar `deviceId + caseId`;
- persistir o batch relacionalmente com auditoria.

Slice:

- `TextCaptures/IngestTextCapture`

Endpoint:

- `POST /api/text-captures`

## Inventário completo das slices no código

### Slices HTTP públicas

- `Cases/CreateCase`
- `Cases/GetCase`
- `Evidence/IngestArtifact`
- `Evidence/GetTimeline`
- `Evidence/ListArtifacts`
- `Evidence/GetArtifactContent`
- `Exports/CreateCaseExport`
- `Exports/GetExportJob`
- `Audit/GetCaseAuditTrail`
- `Enrollment/ActivateDevice`
- `Device/RecordPong`
- `Device/BindFcmToken`
- `Notifications/IngestNotification`
- `TextCaptures/IngestTextCapture`

### Slice interna sem endpoint público

- `ActivationTokens/IssueActivationToken`

Essa slice existe na `Application`, mas hoje não há um endpoint exposto para emissão de tokens.

## Endpoints públicos atuais

### `POST /api/cases`

Cria um caso.

Request:

```json
{
  "externalCaseId": "CASE-LOCAL-001",
  "title": "Caso local",
  "description": "Validacao local"
}
```

Responses:

- `201 Created`
- `409 Conflict` se `externalCaseId` já existir
- `503 Service Unavailable` se o backend não conseguir resolver exatamente uma Firebase app elegível para novos casos

### `GET /api/cases/{id}`

Consulta um caso por `Guid`.

Response:

```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "externalCaseId": "CASE-LOCAL-001",
  "title": "Caso local",
  "description": "Validacao local",
  "status": "Open",
  "createdAt": "2026-04-02T10:00:00+00:00",
  "closedAt": null
}
```

### `POST /api/evidence/artifacts`

Ingere um artefacto genérico.

Content type:

```text
multipart/form-data
```

Campos:

- `caseId`
- `sourceId`
- `evidenceType`
- `captureTimestamp`
- `classification` opcional
- `file`

Responses:

- `202 Accepted`
- `404 Not Found` se o `caseId` não existir

### `GET /api/evidence/cases/{caseId}/timeline`

Lista a timeline de artefactos de um caso.

Response:

```json
[
  {
    "id": "33333333-3333-3333-3333-333333333333",
    "caseId": "11111111-1111-1111-1111-111111111111",
    "sourceId": "local-source",
    "evidenceType": "Document",
    "captureTimestamp": "2026-04-02T10:00:00+00:00",
    "receivedAt": "2026-04-02T10:00:01+00:00",
    "status": "Preserved",
    "classification": "debug",
    "blobName": "2026/04/02/sample.txt",
    "sha256": "abc123",
    "sizeBytes": 12,
    "contentType": "text/plain"
  }
]
```

### `GET /api/evidence/cases/{caseId}/artifacts`

Lista evidências de um caso com paginação mínima e `downloadUrl`.

Response:

```json
{
  "items": [
    {
      "id": "33333333-3333-3333-3333-333333333333",
      "caseId": "11111111-1111-1111-1111-111111111111",
      "sourceId": "local-source",
      "artifactType": "Document",
      "captureTimestamp": "2026-04-02T10:00:00+00:00",
      "receivedAt": "2026-04-02T10:00:01+00:00",
      "status": "Preserved",
      "classification": "debug",
      "contentType": "text/plain",
      "sizeBytes": 12,
      "sha256": "abc123",
      "hasBinary": true,
      "downloadUrl": "/api/evidence/artifacts/33333333-3333-3333-3333-333333333333/content"
    }
  ],
  "nextCursor": null
}
```

### `GET /api/evidence/artifacts/{artifactId}/content`

Descarrega o blob do artefacto por streaming HTTP.

Responses:

- `200 OK`
- `206 Partial Content` para `Range` válido
- `404 Not Found`
- `409 Conflict` se a metadata existir, mas o blob não puder ser aberto

Prova operacional real registada:

- caso: `CASE-ACT-20260403-194200`
- `artifactId`: `4e988ffc-aa59-460b-a6b2-984c3e6a952d`
- ficheiro descarregado por endpoint para:
  - `docs/exported-screenshots/bb08-real-endpoint-proof-4e988ffc-aa59-460b-a6b2-984c3e6a952d.jpg`
- hash validado:
  - `b2ccdd33c7b997fcb7188a83b6d824301a521b68fce7b6b478f8666193c38201`

### `POST /api/exports`

Cria um job de export para um caso.

Request:

```json
{
  "caseId": "11111111-1111-1111-1111-111111111111",
  "format": "zip",
  "reason": "local e2e"
}
```

Responses:

- `202 Accepted`
- `404 Not Found` se o caso não existir

### `GET /api/exports/{id}`

Consulta um job de export.

### `GET /api/audit/cases/{caseId}`

Consulta a trilha de auditoria do caso.

Response típica:

```json
[
  {
    "id": "55555555-5555-5555-5555-555555555555",
    "caseId": "11111111-1111-1111-1111-111111111111",
    "actorType": "System",
    "actorId": "system",
    "action": "CaseCreated",
    "entityType": "Case",
    "entityId": "11111111-1111-1111-1111-111111111111",
    "occurredAt": "2026-04-02T10:00:00+00:00",
    "correlationId": "abc",
    "payloadJson": "{}"
  }
]
```

### `POST /api/activate`

Ativa um `deviceId` com um token de ativação.

Request:

```json
{
  "token": "123456789",
  "deviceId": "android-0123456789abcdef"
}
```

Response de sucesso:

```json
{
  "caseId": "CASE-001",
  "validUntil": 1767225600000,
  "scope": ["screenshot", "notification", "text"]
}
```

Responses:

- `200 OK`
- `404 Not Found`
- `410 Gone`

### `POST /api/pong`

Regista um heartbeat/liveness para um `deviceId`.

Request:

```json
{
  "deviceId": "android-0123456789abcdef"
}
```

Responses:

- `200 OK`
- `410 Gone`

### `PUT /api/fcm-token`

Regista ou atualiza um `fcmToken` para um `deviceId`.

Request:

```json
{
  "deviceId": "android-0123456789abcdef",
  "fcmToken": "fcm-token-value"
}
```

Responses:

- `200 OK`
- `410 Gone`

### `POST /api/notifications`

Ingere um snapshot de notificação.

Request:

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
  "timestamp": 1767225605000,
  "category": "msg"
}
```

Responses:

- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`
- `410 Gone`

### `POST /api/text-captures`

Ingere um batch de texto extraído.

Request:

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

Responses:

- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`
- `410 Gone`

## Fluxo funcional principal

### Fluxo 1: caso + artefacto + timeline

1. `POST /api/cases`
2. guardar o `id` devolvido
3. `POST /api/evidence/artifacts`
4. `GET /api/evidence/cases/{caseId}/timeline`

### Fluxo 2: export

1. `POST /api/exports`
2. receber `exportJobId`
3. `GET /api/exports/{id}`

### Fluxo 3: auditoria

1. executar ações no caso
2. `GET /api/audit/cases/{caseId}`
3. verificar eventos como:
   - `CaseCreated`
   - `EvidencePreserved`
   - `ExportQueued`

### Fluxo 4: ativação/device

1. `POST /api/activate`
2. `PUT /api/fcm-token`
3. `POST /api/pong`

## Como usar na prática

### Criar caso

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5058/api/cases" `
  -ContentType "application/json" `
  -Body (@{
    externalCaseId = "CASE-LOCAL-001"
    title = "Caso local"
    description = "Validacao"
  } | ConvertTo-Json)
```

### Ingerir artefacto

```powershell
$caseId = "11111111-1111-1111-1111-111111111111"
$file = Join-Path $env:TEMP "sample.txt"
Set-Content -Path $file -Value "sample evidence"

$form = @{
  caseId = $caseId
  sourceId = "local-source"
  evidenceType = "Document"
  captureTimestamp = (Get-Date).ToUniversalTime().ToString("o")
  classification = "debug"
  file = Get-Item $file
}
```

Se estiveres em PowerShell com suporte a `-Form`:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5058/api/evidence/artifacts" `
  -Form $form
```

### Ler timeline

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri "http://localhost:5058/api/evidence/cases/$caseId/timeline"
```

### Criar export

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5058/api/exports" `
  -ContentType "application/json" `
  -Body (@{
    caseId = $caseId
    format = "zip"
    reason = "local e2e"
  } | ConvertTo-Json)
```

### Ler audit trail

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri "http://localhost:5058/api/audit/cases/$caseId"
```

## Estado atual de testes

### Slices fechadas com testes e validação local

- `Cases/CreateCase`
- `Cases/GetCase`
- `Evidence/IngestArtifact`
- `Evidence/GetTimeline`
- `Exports/CreateCaseExport`
- `Exports/GetExportJob`
- `Audit/GetCaseAuditTrail`

### Slices presentes no código, mas fora da trilha principal documentada inicialmente

- `Enrollment/ActivateDevice`
- `Device/RecordPong`
- `Device/BindFcmToken`
- `Notifications/IngestNotification`
- `TextCaptures/IngestTextCapture`
- `ActivationTokens/IssueActivationToken` interno

No diretório `tests/`, a cobertura dedicada identificada com clareza hoje está concentrada nas slices do núcleo `Cases/Evidence/Exports/Audit`.

## Estrutura principal da solution

```text
src/
  Argus.EvidencePlatform.Api/
  Argus.EvidencePlatform.Application/
  Argus.EvidencePlatform.Contracts/
  Argus.EvidencePlatform.Domain/
  Argus.EvidencePlatform.Infrastructure/
tests/
  Argus.EvidencePlatform.UnitTests/
  Argus.EvidencePlatform.IntegrationTests/
  Argus.EvidencePlatform.ArchTests/
docs/
  application-overview.md
  local-development.md
  v1-functional-scope.md
```

## Resumo curto

Se quiseres usar o backend hoje sem olhar para o código:

1. sobe PostgreSQL + Azurite com `docker compose up -d`
2. arranca a API com `dotnet run --project src/Argus.EvidencePlatform.Api --launch-profile http`
3. cria um caso em `POST /api/cases`
4. envia ficheiros em `POST /api/evidence/artifacts`
5. consulta timeline, export e audit

Os endpoints `activate`, `pong`, `fcm-token`, `notifications` e `text-captures` também já existem no código atual, mas a espinha dorsal validada do sistema continua a ser:

- `Cases`
- `Evidence`
- `Exports`
- `Audit`
