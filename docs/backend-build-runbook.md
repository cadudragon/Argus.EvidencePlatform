# Argus Evidence Platform Build Runbook

Data: 2026-04-02

## Como usar este runbook

Este runbook divide a evolução do backend em entregáveis pequenos (`BB`), com foco em contratos observáveis e prova executável. O objetivo é permitir handover limpo entre sessões e agentes.

## Status permitidos

- `OPEN`
- `IN PROGRESS`
- `DONE`
- `BLOCKED`

## Quadro global de entregas

| BB | Status | Evidence | Blocked by |
|---|---|---|---|
| BB-00 | DONE | runtime local com PostgreSQL + Azurite + API `5058` | — |
| BB-01 | DONE | cases/evidence/exports/audit funcionais | BB-00 |
| BB-02 | DONE | `POST /api/activate` e `POST /api/pong` funcionais | BB-00 |
| BB-03 | DONE | `PUT /api/fcm-token` funcional | BB-02 |
| BB-04 | DONE | `POST /api/device-commands/screenshot` com Firebase Admin SDK | BB-03 |
| BB-05 | DONE | `POST /api/screenshots` com gzip apenas neste endpoint | BB-02, BB-04 |
| BB-05.1 | DONE | export local de screenshots a partir do Azurite | BB-05 |
| BB-06 | OPEN | `POST /api/notifications` | BB-02 |
| BB-07 | OPEN | `POST /api/text-captures` | BB-02 |
| BB-08 | OPEN | leitura HTTP mínima para evidências do caso | BB-05 |
| BB-09 | OPEN | reforço de testes de regressão end-to-end | BB-06, BB-07 |

## Regras operacionais

- não iniciar `BB` dependente enquanto o anterior não estiver `DONE`
- cada entrega fecha um contrato observável
- cada entrega devolve prova executável, não só explicação
- se a prova falhar, a entrega continua aberta
- atualizar os docs quando a operação local mudar

## Prova mínima obrigatória em todas as entregas

- ficheiros criados ou tocados
- comando ou fluxo exato executado
- resultado real observado
- failure path exercitado quando o `BB` toca contrato HTTP, storage ou Firebase
- próximo `BB` recomendado

## Sequência canónica

| BB | Entrega | Fase | Depende de |
|---|---|---|---|
| BB-00 | runtime local e scripts operacionais | 0 | — |
| BB-01 | backbone cases/evidence/exports/audit | 1 | BB-00 |
| BB-02 | enrollment e device liveness | 2 | BB-00 |
| BB-03 | bind de FCM token | 3 | BB-02 |
| BB-04 | comando screenshot por Firebase | 3 | BB-03 |
| BB-05 | ingestão de screenshot com gzip scoped | 4 | BB-02, BB-04 |
| BB-05.1 | export operacional de screenshots | 4 | BB-05 |
| BB-06 | notifications slice | 5 | BB-02 |
| BB-07 | text captures slice | 5 | BB-02 |
| BB-08 | leitura HTTP mínima das evidências | 6 | BB-05 |
| BB-09 | endurecimento de testes e regressão | 6 | BB-06, BB-07 |

## Entregas detalhadas

### BB-00 — Runtime local e scripts operacionais

- Fase: 0
- Estado: DONE
- Objetivo: subir o backend local com dependências reproduzíveis em Windows.
- Escopo:
  - Docker Desktop com Linux engine
  - `compose.yaml` com PostgreSQL e Azurite
  - scripts do repo:
    - `scripts/ensure-runtime-deps.ps1`
    - `scripts/start-runtime.ps1`
    - `scripts/check-runtime.ps1`
  - `AGENT.md` com paths, runtime e convenções locais
- Prova obrigatória:
  - `health/live` responde `200`
  - porta `5058` em `LISTEN`
  - PostgreSQL e Azurite em containers `Up`
- Gate checks:
  - [x] runtime local sobe sem intervenção manual fora de Docker + user-secrets
  - [x] scripts do repo substituem runbook oral

### BB-01 — Backbone cases/evidence/exports/audit

- Fase: 1
- Estado: DONE
- Objetivo: provar o backbone base do backend.
- Escopo:
  - `POST /api/cases`
  - `GET /api/cases/{id}`
  - `POST /api/evidence/artifacts`
  - `GET /api/evidence/cases/{caseId}/timeline`
  - `POST /api/exports`
  - `GET /api/exports/{id}`
  - `GET /api/audit/cases/{caseId}`
- Prova obrigatória:
  - `scripts/e2e-local.ps1` executa o fluxo principal
- Gate checks:
  - [x] caso criado aparece na audit trail
  - [x] artefacto genérico aparece na timeline

### BB-02 — Enrollment e device liveness

- Fase: 2
- Estado: DONE
- Objetivo: suportar ativação e heartbeat técnico do device.
- Escopo:
  - `POST /api/activate`
  - `POST /api/pong`
  - vínculo `deviceId -> caseId`
- Prova obrigatória:
  - token válido ativa device
  - token inválido devolve status funcional
- Gate checks:
  - [x] `deviceId` fica associado ao caso certo
  - [x] `activate` não depende de gzip

### BB-03 — Bind de FCM token

- Fase: 3
- Estado: DONE
- Objetivo: persistir o canal de comando remoto.
- Escopo:
  - `PUT /api/fcm-token`
  - persistência e update de `fcmToken`
- Prova obrigatória:
  - app envia `deviceId + fcmToken`
  - binding fica visível na base
- Gate checks:
  - [x] token novo substitui o anterior
  - [x] binding é por `deviceId`

### BB-04 — Comando screenshot por Firebase

- Fase: 3
- Estado: DONE
- Objetivo: enviar comando on-demand ao device via Firebase.
- Escopo:
  - `POST /api/device-commands/screenshot`
  - `FirebaseAdmin`
  - `IDeviceCommandDispatcher`
  - limpeza de token inválido
- Prova obrigatória:
  - endpoint devolve `messageId`
  - binding inválido é tratado
- Gate checks:
  - [x] usa pacote oficial do Firebase para .NET
  - [x] config lida de `Firebase:Enabled`, `Firebase:ProjectId`, `Firebase:ServiceAccountPath`

### BB-05 — Ingestão de screenshot com gzip scoped

- Fase: 4
- Estado: DONE
- Objetivo: fechar o upload de screenshot do Android.
- Escopo:
  - `POST /api/screenshots`
  - `gzip` só neste endpoint
  - multipart + validação forte
  - persistência blob + metadados
  - `captureTimestamp` em Unix epoch milliseconds
- Prova obrigatória:
  - screenshot comandada via FCM chega ao caso correto
  - blob criado no Azurite
  - metadata criada no PostgreSQL
- Gate checks:
  - [x] `gzip` não é global
  - [x] mismatch `deviceId/caseId` falha
  - [x] erro de payload inválido não explode em `500`

### BB-05.1 — Export operacional de screenshots

- Fase: 4
- Estado: DONE
- Objetivo: permitir inspeção local das imagens sem UI.
- Escopo:
  - `scripts/export-case-screenshots.ps1`
  - download de blobs do Azurite para pasta local
- Prova obrigatória:
  - screenshots exportadas para `C:\Src\exported-screenshots`
- Gate checks:
  - [x] não depende de memória da sessão
  - [x] funciona para múltiplas imagens do mesmo caso

### BB-06 — Notifications slice

- Fase: 5
- Estado: OPEN
- Objetivo: implementar o contrato atual de notificações do app.
- Escopo:
  - `POST /api/notifications`
  - validator
  - handler
  - persistência por `deviceId + caseId`
- Prova obrigatória:
  - app deixa de gerar `404` para notificações
  - payload válido é persistido
  - payload inválido devolve `400`
- Gate checks:
  - [ ] contrato do app documentado
  - [ ] timeline ou leitura operacional consegue ver o resultado

### BB-07 — Text captures slice

- Fase: 5
- Estado: OPEN
- Objetivo: implementar o contrato atual de text extraction do app.
- Escopo:
  - `POST /api/text-captures`
  - validator
  - handler
  - persistência por `deviceId + caseId`
- Prova obrigatória:
  - app deixa de gerar `404` para text captures
  - payload válido é persistido
  - payload inválido devolve `400`
- Gate checks:
  - [ ] limites de tamanho e cardinalidade validados
  - [ ] contrato do app preservado

### BB-08 — Leitura HTTP mínima das evidências

- Fase: 6
- Estado: OPEN
- Objetivo: reduzir dependência de SQL + Azurite tooling para ler evidências.
- Escopo:
  - endpoint simples para listar ou descarregar screenshots por caso
  - sem UI obrigatória
- Prova obrigatória:
  - evidência pode ser lida por HTTP no ambiente local
- Gate checks:
  - [ ] caminho operacional não depende de acesso direto à base

### BB-09 — Endurecimento de regressão

- Fase: 6
- Estado: OPEN
- Objetivo: estabilizar os contratos device-facing já fechados.
- Escopo:
  - testes de integração adicionais:
    - activate plain JSON
    - fcm-token
    - device-commands/screenshot
    - screenshots gzip
    - notifications
    - text-captures
- Prova obrigatória:
  - suite relevante verde
- Gate checks:
  - [ ] contratos críticos protegidos contra regressão

## Critério de passagem entre entregas

Não avançar para a próxima entrega se qualquer uma destas condições falhar:

- o contrato HTTP da entrega atual não está reproduzível
- não existe prova do happy path e do failure path
- a entrega mexeu fora do slice sem justificação
- a documentação canónica não foi atualizada quando o runtime ou contrato mudou

## Fecho do programa backend

O programa do backend só fecha quando tudo isto for verdade ao mesmo tempo:

- runtime local reproduzível
- `activate`, `fcm-token`, `pong` funcionais
- screenshot on-demand funciona end-to-end
- `notifications` e `text-captures` implementados
- blobs e metadados podem ser inspecionados sem contexto oral
- contratos críticos estão cobertos por testes
