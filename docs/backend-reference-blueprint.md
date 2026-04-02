# Backend Reference Blueprint

## Objetivo deste documento

Este documento é uma referência genérica de arquitetura, implementação e qualidade para criar outro backend com o mesmo padrão deste projeto.

Ele deve ser usado como:

- guia de arquitetura;
- guia de scaffold;
- guia de implementação incremental;
- guia de testes;
- handoff para outro LLM ou outra equipa.

Ele **não** descreve um domínio específico.  
Troca-se o domínio (`Case`, `Evidence`, `Export`, `Audit`) pelo domínio do novo projeto, mas mantém-se a disciplina técnica.

## Resultado esperado

Outro backend gerado com base neste documento deve sair com:

- `Modular Monolith`;
- `Vertical Slices`;
- `Minimal APIs`;
- separação limpa entre `Api`, `Application`, `Domain`, `Infrastructure`, `Contracts`;
- execução `local-first`;
- testes fortes desde o início;
- evolução incremental slice a slice;
- baixo acoplamento;
- documentação operacional mínima.

## Princípios obrigatórios

### 1. Local-first

O projeto deve funcionar localmente antes de qualquer cloud.

Mínimo:

- banco local em Docker;
- blob/object storage local ou emulador;
- auth mock em `Development` e `Testing`;
- bootstrap automático do ambiente local;
- script E2E local.

### 2. Slice-by-slice

Não gerar “todas as camadas completas” antecipadamente.  
Implementar uma slice de cada vez.

Cada slice só pode ser considerada pronta quando:

- comportamento principal está implementado;
- testes unitários da lógica core estão verdes;
- testes de integração HTTP estão verdes;
- documentação mínima está coerente com o comportamento real.

### 3. Arquitetura simples, mas rigorosa

Usar:

- `Vertical Slices` para organizar features;
- `Clean Architecture` apenas como regra de dependência;
- `DDD` apenas onde houver regra de negócio real.

Evitar:

- camadas artificiais;
- repositórios genéricos;
- abstrações prematuras;
- eventos distribuídos sem necessidade;
- microservices no v1.

### 4. Testes fortes nas camadas certas

Meta principal:

- `100%` de cobertura focada em `Domain`, `validators`, `handlers`, e command/query models quando aplicável.

Infraestrutura e wiring:

- validar por testes de integração;
- não forçar unit tests artificiais para EF, HTTP wiring ou DI.

## Stack de referência

### Runtime

- `.NET 10`
- `ASP.NET Core Minimal APIs`
- `Wolverine`
- `EF Core`
- `PostgreSQL`

### Storage

- object/blob storage abstraído;
- em local, usar emulador ou implementação equivalente.

### Observabilidade

- `OpenTelemetry`
- health checks
- `ProblemDetails`
- rate limiting

### Local tooling

- `Docker Compose`
- script E2E local

## Estrutura da solution

```text
src/
  Project.Api/
  Project.Application/
  Project.Contracts/
  Project.Domain/
  Project.Infrastructure/
  Project.Workers/
  Project.AppHost/           # opcional
  Project.ServiceDefaults/   # opcional
tests/
  Project.UnitTests/
  Project.IntegrationTests/
  Project.ArchTests/
docs/
  adr/
  local-development.md
  application-overview.md
```

## Responsabilidade de cada projeto

### `Project.Api`

Responsável por:

- endpoints HTTP;
- auth/authz;
- binding de requests;
- filters;
- OpenAPI;
- health endpoints.

Não deve conter:

- regra de negócio relevante;
- acesso direto complexo à base de dados;
- lógica de domínio.

### `Project.Application`

Responsável por:

- commands;
- queries;
- handlers;
- validação de requests;
- interfaces de infraestrutura;
- orchestration de use cases.

Não deve conter:

- detalhes de EF Core;
- detalhes de Blob Storage;
- código HTTP;
- dependências de UI.

### `Project.Domain`

Responsável por:

- entidades;
- value objects;
- enums;
- invariantes;
- regras puras de negócio.

Não deve conter:

- EF Core;
- JSON serialization;
- HTTP;
- SDKs externos.

### `Project.Infrastructure`

Responsável por:

- EF Core;
- repositórios concretos;
- clients de storage;
- bootstrap local;
- relógio;
- hashing;
- integrações externas.

### `Project.Contracts`

Responsável por:

- DTOs públicos de request/response;
- contratos estáveis de API.

### `Project.Workers`

Responsável por:

- jobs assíncronos;
- export processing;
- projections;
- processamento pós-request.

## Padrão de slice

Cada slice deve seguir um padrão previsível.

Exemplo:

```text
Features/
  Orders/
    CreateOrder/
      Endpoint.cs
      Request.cs
      Response.cs         # quando fizer sentido
      Validator.cs
      Handler.cs
```

Na prática:

- `Endpoint`: recebe HTTP e chama Wolverine;
- `Request`: contrato de entrada HTTP;
- `Validator`: valida request;
- `Handler`: executa o caso de uso;
- `Response`: contrato devolvido.

## Padrão de fluxo

### Escrita

Fluxo esperado:

1. endpoint recebe request;
2. validator valida;
3. endpoint chama `IMessageBus.InvokeAsync(...)`;
4. handler aplica regra;
5. repositório persiste;
6. audit é gravado se aplicável;
7. resposta é devolvida.

### Leitura

Fluxo esperado:

1. endpoint recebe route/query params;
2. chama query handler;
3. handler monta DTO de leitura;
4. endpoint devolve `200`, `404`, etc.

## Convenções HTTP

### Status codes

Usar convenções simples:

- `200 OK` para leitura;
- `201 Created` para criação síncrona;
- `202 Accepted` para intake ou processamento assíncrono;
- `404 Not Found` para recurso inexistente;
- `409 Conflict` para duplicidade ou violação de unicidade;
- `400` ou validation problem para input inválido.

### Erros

Preferir:

- `ProblemDetails`;
- respostas de validação consistentes;
- mensagens técnicas curtas e estáveis.

### Rotas

Seguir rotas explícitas e orientadas a recurso.

Exemplos:

- `POST /api/orders`
- `GET /api/orders/{id}`
- `POST /api/orders/{id}/attachments`
- `GET /api/orders/{id}/timeline`

## Persistência

### Banco relacional

Usar:

- `PostgreSQL`;
- `EF Core`;
- `DbContext` com mapeamento explícito.

Convenções:

- schema explícito;
- nomes de tabela estáveis;
- índices únicos onde houver identificadores externos;
- conversões explícitas para enums;
- tamanho máximo para strings importantes.

### Object/blob storage

Usar abstração como:

- `IBlobStagingService`
- `IObjectStorage`

Padrão:

- staging primeiro;
- persistência lógica depois;
- guardar `sha256`, nome lógico, tamanho e `contentType`.

## Bootstrap local

O projeto deve arrancar localmente sem passos obscuros.

Obrigatório:

- `docker compose up -d`
- API sobe
- schema é criado automaticamente em `Development`
- containers/blob buckets locais são criados automaticamente

Se houver emulador com incompatibilidade de versão de API, isso deve ser resolvido no próprio `compose` ou na configuração local.

## Test strategy

## 1. Unit tests

Cobrir:

- domínio;
- validators;
- handlers;
- command/query models, quando fizer sentido.

Padrão:

- inputs válidos;
- inputs inválidos;
- not found;
- conflict;
- mutações de estado;
- audit gerado;
- normalização de strings/IDs se existir.

## 2. Integration tests

Cobrir:

- endpoints HTTP;
- status codes;
- fluxo request/response;
- persistência em modo `Testing`;
- ordenação de timelines;
- cenários `404`, `409`, `202`, etc.

Padrão:

- `WebApplicationFactory`;
- `Testing` environment;
- auth mock;
- in-memory persistence ou setup controlado.

## 3. Architecture tests

Validar:

- `Domain` não depende de `Infrastructure`;
- `Application` não depende de `Api`;
- `Contracts` não depende de camadas de runtime.

## Definition of Done por slice

Uma slice só está pronta quando:

- endpoint existe;
- request/response estão estáveis;
- validator existe, se aplicável;
- handler existe;
- persistência mínima existe;
- audit é feito quando necessário;
- unit tests core estão verdes;
- integration tests estão verdes;
- a slice pode ser explicada em 5 linhas.

## Ordem de implementação recomendada

Para um backend novo, seguir esta ordem:

1. scaffold base da solution;
2. bootstrap local;
3. primeira slice de criação do agregado principal;
4. primeira slice de leitura;
5. primeira slice de ingestão/attachment;
6. primeira slice de timeline/listagem;
7. export/job assíncrono;
8. audit;
9. workers;
10. cloud/IaC depois.

## Convenções de qualidade para outro LLM

Se este documento for entregue a outro LLM, ele deve seguir estas regras:

- não gerar tudo de uma vez;
- implementar uma slice por vez;
- usar `Vertical Slices`;
- manter a solution compilável a cada passo;
- adicionar testes junto com a slice;
- não introduzir abstrações sem uso imediato;
- não usar banco/cloud reais no primeiro passo;
- documentar o que foi implementado e o que falta;
- nunca contradizer o comportamento real do código com documentação inventada.

## Prompt base recomendado para outro LLM

Usa este backend blueprint para gerar uma app `.NET 10` com `ASP.NET Core Minimal APIs`, `Wolverine`, `EF Core`, `PostgreSQL`, `Vertical Slices` e execução `local-first`.

Regras:

- implementar uma slice por vez;
- não criar todas as camadas antecipadamente;
- manter `Api`, `Application`, `Domain`, `Infrastructure`, `Contracts`;
- adicionar testes unitários para `Domain`, validators e handlers;
- adicionar testes de integração para os endpoints;
- criar bootstrap local automático para banco e blob storage;
- manter documentação simples em `docs/`;
- só avançar para a próxima slice quando a atual estiver verde.

## O que reaproveitar deste projeto

Pode ser copiado como padrão:

- estrutura da solution;
- modelo de `Vertical Slices`;
- estratégia `local-first`;
- bootstrap do schema local;
- script E2E;
- abordagem de testes;
- convenções de endpoint e response handling;
- disciplina de documentação.

Não deve ser copiado cegamente:

- nomes de domínio;
- nomes de entidades;
- payloads específicos;
- decisões de negócio que pertencem apenas a este projeto.

## Resumo final

Este blueprint define um backend de alta qualidade com estas características:

- simples de arrancar localmente;
- modular sem overengineering;
- orientado a slices;
- com testes desde o início;
- preparado para crescer;
- suficientemente genérico para outro domínio.

Se outro backend seguir este documento, deve conseguir atingir o mesmo padrão estrutural e operacional deste projeto sem herdar o domínio específico.
