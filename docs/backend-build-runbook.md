# Argus Evidence Platform Build Runbook

Data: 2026-04-03

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
| BB-06 | DONE | `POST /api/notifications` | BB-02 |
| BB-07 | DONE | `POST /api/text-captures` | BB-02 |
| BB-07.1 | DONE | suporte a múltiplas Firebase apps com roteamento por app ativa | BB-03, BB-04 |
| BB-07.2 | OPEN | cleanup de scaffolds e conceitos de compliance não implementados | BB-07.1 |
| BB-08 | OPEN | leitura HTTP mínima para evidências do caso com streaming HTTP | BB-05, BB-07.2 |
| BB-08.1 | OPEN | pipeline assíncrono para segmentos de imagem e composição futura de vídeo | BB-08 |
| BB-09 | OPEN | logging leve de solicitações e acessos por agente/caso | BB-08 |
| BB-10 | OPEN | reforço de testes de regressão end-to-end | BB-06, BB-07, BB-07.1, BB-08, BB-09 |

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
| BB-07.1 | multi-Firebase apps por caso | 6 | BB-03, BB-04 |
| BB-07.2 | cleanup de scaffolds e compliance morto | 6 | BB-07.1 |
| BB-08 | leitura HTTP mínima das evidências | 6 | BB-05, BB-07.2 |
| BB-08.1 | pipeline assíncrono de segmentos e vídeo | 6 | BB-08 |
| BB-09 | logging leve por agente/caso | 6 | BB-08 |
| BB-10 | endurecimento de testes e regressão | 6 | BB-06, BB-07, BB-07.1, BB-08, BB-09 |

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
  - [x] config lida de `Firebase:Enabled` e `Firebase:Apps:*`

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
- Estado: DONE
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
  - [x] contrato do app documentado
  - [x] leitura operacional via `GET /api/audit/cases/{caseId}` consegue ver o resultado

### BB-07 — Text captures slice

- Fase: 5
- Estado: DONE
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
  - [x] limites de tamanho e cardinalidade validados
  - [x] contrato do app preservado

### BB-07.1 — Multi-Firebase apps por caso

- Fase: 6
- Estado: DONE
- Objetivo: suportar múltiplas Firebase apps/projetos no backend para distribuir a base de casos e evitar acoplamento operacional a uma única app global.
- Escopo:
  - introduzir entidade/configuração explícita de app Firebase no backend
  - adicionar flag operacional de ativação por app, com nome final a definir na modelagem
  - permitir que o backoffice marque novas apps Firebase como disponíveis para roteamento
  - persistir no caso qual Firebase app lhe foi atribuída
  - garantir que `fcmToken` e comando remoto usam a Firebase app correta do caso/device
  - manter compatibilidade com o fluxo atual de `activate -> fcm-token -> device-commands/screenshot`
- Prova obrigatória:
  - backend consegue operar com pelo menos duas Firebase apps configuradas
  - um caso novo é associado a uma app Firebase ativa
  - `PUT /api/fcm-token` preserva o vínculo na app correta
  - `POST /api/device-commands/screenshot` usa a app correta e devolve `messageId`
- Gate checks:
  - [x] a escolha da app Firebase não fica hardcoded numa singleton global
  - [x] a atribuição ao caso é persistida e auditável
  - [x] apenas apps marcadas como ativas entram no roteamento de novos casos
  - [x] o desenho evita refactor posterior nos slices de comando já fechados

Prova executável registada:

- `C:\Progra~1\dotnet\dotnet.exe test tests\Argus.EvidencePlatform.UnitTests\Argus.EvidencePlatform.UnitTests.csproj`
- `C:\Progra~1\dotnet\dotnet.exe test tests\Argus.EvidencePlatform.IntegrationTests\Argus.EvidencePlatform.IntegrationTests.csproj`
- `C:\Progra~1\dotnet\dotnet.exe test Argus.EvidencePlatform.slnx`

Resultado observado:

- suite unitária verde
- suite de integração verde
- solution completa verde, incluindo `ArchTests`

Notas de fecho:

- `POST /api/cases` devolve `503 Service Unavailable` quando não existe exatamente uma Firebase app elegível para novos casos
- `Case.FirebaseAppId` passou a ser a source of truth do roteamento
- `FcmTokenBinding.FirebaseAppId` é persistido para bind e auditoria do token atual
- o bootstrap persistente das Firebase apps continua no runtime real; em `Testing` a resolução usa o bootstrap configurado sem depender de hosted seeding

Plano técnico detalhado:

- [backend-bb-07.1-multi-firebase-plan.md](./backend-bb-07.1-multi-firebase-plan.md)

### BB-07.2 — Cleanup de scaffolds e compliance morto

- Fase: 6
- Estado: OPEN
- Objetivo: remover ou rebaixar explicitamente conceitos que hoje parecem funcionais, mas não têm implementação operacional no backend.
- Escopo:
  - mapear conceitos implementados, scaffolds úteis e conceitos mortos
  - decidir o destino de `evidenceContainerName` face ao fluxo real em `staging`
  - rever placeholders como `ImmutabilityState` e `LegalHoldState`
  - rever metadata reservada de export final sem worker real
  - rever o papel do projeto `Workers`
  - atualizar docs canónicos para refletir apenas comportamento real ou scaffold explícito
- Prova obrigatória:
  - inventário final com decisão por conceito: `remover`, `manter como scaffold` ou `implementar depois`
  - código e docs deixam de sugerir capacidades inexistentes como se estivessem prontas
  - suite relevante continua verde após o cleanup
- Gate checks:
  - [ ] nenhum conceito morto permanece descrito como funcional
  - [ ] nenhum scaffold remanescente fica ambíguo quanto ao seu estado
  - [ ] o backend continua alinhado ao modelo local-first sem parafernália de compliance não usada

### BB-08 — Leitura HTTP mínima das evidências

- Fase: 6
- Estado: OPEN
- Objetivo: reduzir dependência de SQL + Azurite tooling para ler evidências sem criar gargalos de memória no read path.
- Escopo:
  - endpoint HTTP para listar evidências de um caso com paginação mínima
  - endpoint HTTP para descarregar blobs/evidências relevantes por streaming
  - suporte a `Range` quando aplicável ao artefacto descarregado
  - sem UI obrigatória
  - não devolver blobs grandes como `byte[]`
- Prova obrigatória:
  - evidência pode ser listada por HTTP no ambiente local
  - blob pode ser descarregado por HTTP em modo streaming
  - leitura parcial/range funciona quando aplicável
- Gate checks:
  - [ ] caminho operacional não depende de acesso direto à base
  - [ ] download não bufferiza o blob inteiro em memória da API
  - [ ] contrato de leitura não mistura download com processamento pesado

#### Plano de continuação em sessão limpa

Contexto de arranque para qualquer nova sessão:

- tratar `BB-08` como `read-only` e `streaming-first`
- começar por screenshots, porque já têm blob + metadata + caso de uso operacional validado
- não tentar resolver vídeo, composição de artefactos ou streams long-running neste `BB`

Contrato alvo recomendado para esta entrega:

1. `GET /api/evidence/cases/{caseId}/artifacts`
2. `GET /api/evidence/artifacts/{artifactId}/content`

Resposta alvo recomendada para listagem:

```json
{
  "items": [
    {
      "id": "guid",
      "caseId": "guid",
      "sourceId": "android-xxx",
      "artifactType": "Screenshot",
      "captureTimestamp": "2026-04-03T10:00:00Z",
      "receivedAt": "2026-04-03T10:00:01Z",
      "contentType": "image/jpeg",
      "sizeBytes": 12345,
      "sha256": "abc",
      "hasBinary": true,
      "downloadUrl": "/api/evidence/artifacts/guid/content"
    }
  ],
  "nextCursor": "opaque-or-null"
}
```

Comportamento alvo recomendado para download:

- devolver `Stream`, não `byte[]`
- definir `Content-Type`
- preencher `Content-Length` quando conhecido
- suportar `Range` quando aplicável
- devolver `404` se o artefacto não existir
- devolver `409` se houver metadata inconsistente com o blob

Sequência recomendada de implementação:

1. mapear exatamente como o screenshot atual é persistido em metadata + blob
2. criar query/listagem paginada por caso para artefactos binários relevantes
3. criar endpoint de download por `artifactId`
4. adaptar Infrastructure para abrir stream do blob sem materializar o ficheiro inteiro
5. adicionar testes de integração para:
   - listagem `200`
   - download `200`
   - artefacto inexistente `404`
   - `Range` parcial quando o host de teste o permitir
6. só depois atualizar docs e marcar `BB-08` como `DONE`

O que fica explicitamente fora do `BB-08`:

- composição de vídeo
- fila/worker para segmentos de imagem
- resumable upload/chunking
- transformação inline de artefactos
- unificação prematura de todos os tipos de evidência num endpoint complexo

Critério prático para considerar o `BB-08` fechado:

- uma sessão nova consegue listar screenshots de um caso por HTTP
- uma sessão nova consegue descarregar um screenshot por HTTP
- a leitura não depende de SQL manual nem de export script para esse fluxo mínimo

### BB-08.1 — Pipeline assíncrono para segmentos de imagem e composição futura de vídeo

- Fase: 6
- Estado: OPEN
- Objetivo: preparar a evolução do backend para capturas long-running sem transformar requests HTTP em workers improvisados.
- Escopo:
  - modelo de ingestão de segmentos/imagens pequenos
  - fila com capacidade limitada e backpressure
  - worker dedicado para composição/agregação posterior
  - persistência separada entre segmento bruto e artefacto derivado final
- Prova obrigatória:
  - request de ingestão devolve rápido sem compor vídeo inline
  - item é enfileirado para processamento posterior
  - worker consome a fila sem rebentar memória do processo da API
- Gate checks:
  - [ ] composição futura de vídeo não acontece dentro da request HTTP
- [ ] existe backpressure explícito
- [ ] desenho separa ingestão, processamento e leitura

### BB-09 — Logging leve por agente/caso

- Fase: 6
- Estado: OPEN
- Objetivo: registar de forma leve o que foi solicitado e acedido por agente dentro de um caso, sem virar trilha regulatória pesada.
- Escopo:
  - evento simples por `caseId`, actor e tipo de ação
  - logging de solicitações relevantes e acessos relevantes
  - leitura operacional mínima desses eventos
  - desenho separado de qualquer framework futuro de compliance formal
- Prova obrigatória:
  - uma ação de leitura relevante gera registo observável
  - uma ação operacional relevante gera registo observável
  - a leitura desses eventos funciona sem SQL manual no fluxo mínimo definido
- Gate checks:
  - [ ] implementação permanece leve e barata de operar
  - [ ] não reintroduz conceitos de compliance removidos no `BB-07.2`
  - [ ] registo por agente/caso fica suficientemente claro para suporte operacional

### BB-10 — Endurecimento de regressão

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
