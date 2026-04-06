# Argus Evidence Platform Build Plan

Data: 2026-04-03

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

### Princípios vinculativos para transferência de evidência

Estas regras orientam `BB-08` e qualquer evolução futura de upload/download/streaming:

1. **Streaming primeiro para mídia.** Não ler nem devolver blobs grandes como `byte[]` quando um `Stream` resolve o problema.
2. **Read path leve.** Leitura HTTP de evidência não deve fazer processamento pesado nem montagem de artefactos durante a request.
3. **Range requests quando fizer sentido.** Downloads binários grandes devem ser compatíveis com leitura parcial e retomada.
4. **Paginação desde o início.** Listagens de evidência por caso não devem crescer sem controlo.
5. **Limites explícitos por endpoint.** Tamanho de body, cardinalidade e número de itens não ficam implícitos no default do servidor.
6. **Pipeline assíncrono para composições futuras.** Se o backend passar a receber pequenos streams de imagens para gerar vídeo, isso entra como `ingest -> queue -> worker`, não como processamento inline na request.
7. **Blob storage como stream endpoint.** PostgreSQL guarda metadados; blobs e streams longos continuam a viver no storage adequado.

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
- `POST /api/notifications`
- `POST /api/cases`
- `GET /api/cases/{id}`
- `POST /api/evidence/artifacts`
- `GET /api/evidence/cases/{caseId}/timeline`
- `POST /api/exports`
- `GET /api/exports/{id}`
- `GET /api/audit/cases/{caseId}`
- `POST /api/pong`

### Em falta no contrato atual da app

- nenhum endpoint device-facing conhecido do contrato atual da app está em falta

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

- suporte a múltiplas Firebase apps com roteamento por caso
- scripts operacionais estáveis
- documentação canónica no repo
- skills locais do Codex
- testes de regressão dos contratos críticos
- leitura operacional mínima para evidências sem SQL manual
- downloads HTTP de evidência em modo streaming
- desenho já compatível com evolução futura para range requests e artefactos maiores

Gate:
- uma sessão nova consegue retomar o trabalho com o repo e os docs

## BB novo antes do BB-08: multi-Firebase apps

Antes do `BB-08`, o backend deve ganhar suporte a múltiplas Firebase apps/projetos.

Motivação:

- hoje o backend está operacionalmente acoplado a uma única Firebase app global
- a base de casos vai crescer e precisa ser distribuída entre múltiplas apps
- fazer isto depois do `BB-08` aumentaria o custo de refactor em enrollment, binding de `fcmToken` e dispatch de comando

Direção arquitetural:

- introduzir uma entidade/configuração explícita para Firebase app no backend
- cada app Firebase terá uma flag operacional de ativação; o nome final da propriedade pode evoluir, mas a semântica é "esta app pode receber novos casos"
- o backoffice ativa uma nova app Firebase quando quiser expandir capacidade
- novos casos passam a ser atribuídos a uma Firebase app ativa
- o caso persiste essa atribuição
- `PUT /api/fcm-token` e `POST /api/device-commands/screenshot` passam a resolver a Firebase app a partir do caso/device, e não de uma configuração global única
- `POST /api/cases` passa a falhar com `503` quando o backend não consegue resolver exatamente uma app elegível para novos casos

Regra de roteamento esperada:

- a escolha da Firebase app acontece no backend no momento em que um caso novo é preparado para operação
- apenas apps marcadas como ativas entram na seleção de novos casos
- casos já atribuídos continuam presos à Firebase app escolhida, salvo migração explícita futura

Impacto esperado:

- evita refactors tardios nos fluxos já fechados de `activate`, `fcm-token` e comando remoto
- prepara o backend para balancear a base entre múltiplos projetos Firebase
- mantém o contrato device-facing igual, mudando apenas a resolução interna da app Firebase correta

Estado atual após `BB-07.1`:

- o caso persiste `FirebaseAppId`
- o binding atual de `fcmToken` persiste `FirebaseAppId`
- o dispatch por Firebase resolve explicitamente a app pelo caso/device
- o bootstrap persistente das apps usa `Firebase:Apps`

Plano técnico detalhado:

- [backend-bb-07.1-multi-firebase-plan.md](./backend-bb-07.1-multi-firebase-plan.md)

## O que não deve ir para a frente antes da Fase 5

- novas categorias de comando além de screenshot
- automações de compliance não suportadas pelo contrato atual da app
- refactors horizontais que desfaçam os slices já estabilizados
- features de UI/admin antes de fechar `notifications` e `text-captures`

## Direção pós-BB-08 para streams de imagens e vídeo

Depois de `BB-08`, a evolução recomendada para capturas long-running é:

1. manter o request path focado em ingestão rápida e persistência segura
2. persistir segmentos/imagens como unidade de ingestão
3. enfileirar composição ou agregação de vídeo fora da request
4. processar montagem, finalização e derivados em worker dedicado
5. só introduzir chunking/resume quando o volume real justificar a complexidade

Isto evita transformar o endpoint HTTP num gargalo de CPU, memória ou tempo de request.

## Estado alvo

Este plano só fica concluído quando for verdade, ao mesmo tempo, que:

- o runtime local sobe de forma reproduzível
- `deviceId -> caseId` e `deviceId -> fcmToken` estão fechados
- screenshot on-demand funciona end-to-end
- `notifications` e `text-captures` estão implementados
- os contratos de transporte estão documentados e cobertos por testes
- a leitura operacional mínima das evidências não depende de memória oral
