# Argus Evidence Platform Build Plan

Data: 2026-04-02

## Escopo e fonte de verdade

Este plano foi construido a partir de:

- [application-overview.md](./application-overview.md)
- [local-development.md](./local-development.md)
- [backend-dotnet-best-practices.md](./backend-dotnet-best-practices.md)
- estado real implementado no repositório e validado localmente com Docker Desktop, PostgreSQL, Azurite e Firebase Admin SDK

Este plano e o runbook [backend-build-runbook.md](./backend-build-runbook.md) são a referência canónica para ordem de trabalho, escopo de cada entrega e critérios de passagem no backend.

## Identidade do projeto

| Campo | Valor |
|---|---|
| Codename | **Argus Evidence Platform** |
| Tipo | Backend `.NET 10` local-first |
| Arquitetura | Modular Monolith + Vertical Slices |
| Cliente device atual | `com.argus.android` |
| Runtime local | Windows + Docker Desktop + Linux engine |

## Decisões arquiteturais vinculativas

### Stack

| Componente | Escolha | Razão |
|---|---|---|
| Runtime | `.NET 10` | base atual do repositório |
| API | ASP.NET Core Minimal APIs | superfície HTTP leve por slice |
| Orquestração de use cases | Wolverine | handlers/slices consistentes |
| Persistência relacional | PostgreSQL | store principal de casos, devices, audit e metadados |
| Blob storage local | Azurite | substituto local de Azure Blob |
| ORM | EF Core | padrão atual do projeto |
| Observabilidade | OpenTelemetry + logs ASP.NET | stack já presente |
| Mensageria device | Firebase Admin SDK oficial | comando on-demand para Android |
| Testes | Unit + Integration + ArchTests | organização já existente no repositório |

### Princípios arquiteturais

1. **Vertical slices primeiro.** Cada fluxo funcional deve entrar como slice, não como serviço transversal genérico.
2. **Application governa regra técnica.** A API valida e encaminha; a Infrastructure adapta.
3. **Contracts explícitos por endpoint.** Não inferir gzip, formatos de tempo ou payloads implicitamente.
4. **Storage separado por responsabilidade.** PostgreSQL para metadados; Azurite/Azure Blob para binários.
5. **Device identity por `deviceId`.** `caseId` é contexto; `fcmToken` é canal mutável.
6. **Config local segura.** Segredos fora do repo; usar `user-secrets` ou env vars.
7. **Prova executável acima de narrativa.** Toda entrega fecha com teste ou fluxo reproduzível.

### O que não fazer

- não reintroduzir request decompression global se o contrato atual só pede gzip em `/api/screenshots`
- não misturar comando de device com ingestão de evidência no mesmo endpoint
- não pôr service-account JSON no repositório
- não espalhar lógica de device/fcm em controllers utilitários fora dos slices
- não depender de comandos inline frágeis quando já há scripts do repositório

## Mapa funcional atual

### Já implementado e validado

- `POST /api/activate`
- `PUT /api/fcm-token`
- `POST /api/device-commands/screenshot`
- `POST /api/screenshots`
- `POST /api/cases`
- `GET /api/cases/{id}`
- `POST /api/evidence/artifacts`
- `GET /api/evidence/cases/{caseId}/timeline`
- `POST /api/exports`
- `GET /api/exports/{id}`
- `GET /api/audit/cases/{caseId}`
- `POST /api/pong`

### Em falta no contrato atual da app

- `POST /api/notifications`
- `POST /api/text-captures`

## Fluxo causal atual do backend

```text
Activation token + deviceId
  -> POST /api/activate
    -> Enrollment/ActivateDevice
      -> vincula deviceId ao caseId
      -> devolve caseId, validUntil, scope
```

```text
deviceId + fcmToken
  -> PUT /api/fcm-token
    -> Device/BindFcmToken
      -> grava ou atualiza binding do canal FCM
```

```text
Operador pede screenshot
  -> POST /api/device-commands/screenshot
    -> Device/RequestScreenshot
      -> resolve deviceId -> fcmToken
      -> envia FCM {"cmd":"screenshot"}
```

```text
App envia screenshot
  -> POST /api/screenshots (multipart + gzip)
    -> Screenshots/IngestScreenshot
      -> valida deviceId <-> caseId
      -> grava blob no Azurite
      -> grava metadados no PostgreSQL
```

## Estado atual

**Slices funcionais validadas localmente:**
- Enrollment
- Device
- Screenshots
- Cases
- Evidence
- Exports
- Audit

**Infra local validada:**
- Docker Desktop
- PostgreSQL local
- Azurite local
- Firebase Admin SDK com service account local

**Fluxos validados end-to-end:**
- ativação
- binding de `fcmToken`
- comando on-demand screenshot
- upload de screenshot gzip
- export local das imagens do caso

## Fases de construção do backend

### Fase 0 — Runtime local e baseline arquitetural

Objetivo: garantir que o backend sobe localmente, com dependências, health checks e testes base.

Entregas obrigatórias:

- compose com PostgreSQL e Azurite
- API local em `5058`
- health endpoints funcionais
- documentação mínima de arquitetura e desenvolvimento local
- scripts de runtime local

Gate:
- runtime local reproduzível
- `health/live` e `health/ready` operacionais

### Fase 1 — Core case/evidence/audit/export

Objetivo: provar o backbone do backend de evidência.

Inclui:

- criação de casos
- ingestão genérica de artefactos
- timeline
- export jobs
- audit trail

Gate:
- fluxo `case -> artifact -> timeline -> export -> audit` reproduzível

### Fase 2 — Enrollment e identidade de device

Objetivo: suportar ativação técnica do device e heartbeat.

Inclui:

- `POST /api/activate`
- `POST /api/pong`
- persistência `deviceId -> caseId`

Gate:
- token válido ativa device e vincula ao caso certo

### Fase 3 — Canal de comando remoto

Objetivo: permitir que o backend comande o device correto.

Inclui:

- `PUT /api/fcm-token`
- persistência `deviceId -> fcmToken`
- integração com Firebase Admin SDK
- `POST /api/device-commands/screenshot`

Gate:
- backend envia FCM para o device certo

### Fase 4 — Screenshot on-demand end-to-end

Objetivo: fechar o ciclo comando -> captura -> upload -> persistência.

Inclui:

- `/api/screenshots`
- suporte a `gzip` apenas neste endpoint
- validação `deviceId + caseId`
- blob + metadata persistidos
- export local das imagens

Gate:
- screenshot pedida por comando remoto chega ao caso correto e pode ser exportada

### Fase 5 — Contrato restante da app

Objetivo: cobrir os uploads que o cliente já tenta usar.

Inclui:

- `POST /api/notifications`
- `POST /api/text-captures`
- validação de payloads
- persistência por `deviceId + caseId`

Gate:
- logs deixam de mostrar `404` para esses uploads

### Fase 6 — Operação e endurecimento

Objetivo: reduzir atrito operacional e endurecer o backend sem sair do modo local-first.

Inclui:

- scripts operacionais estáveis
- documentação canónica no repo
- skills locais do Codex
- testes de regressão dos contratos críticos
- leitura operacional mínima para evidências sem SQL manual

Gate:
- uma sessão nova consegue retomar o trabalho com o repo e os docs

## O que não deve ir para a frente antes da Fase 5

- novas categorias de comando além de screenshot
- automações de compliance não suportadas pelo contrato atual da app
- refactors horizontais que desfaçam os slices já estabilizados
- features de UI/admin antes de fechar `notifications` e `text-captures`

## Estado alvo

Este plano só fica concluído quando for verdade, ao mesmo tempo, que:

- o runtime local sobe de forma reproduzível
- `deviceId -> caseId` e `deviceId -> fcmToken` estão fechados
- screenshot on-demand funciona end-to-end
- `notifications` e `text-captures` estão implementados
- os contratos de transporte estão documentados e cobertos por testes
- a leitura operacional mínima das evidências não depende de memória oral
