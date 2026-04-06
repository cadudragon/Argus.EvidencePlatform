# BB-07.1 — Multi-Firebase Apps Por Caso

Data: 2026-04-05

## Objetivo

Permitir que o backend opere com múltiplas Firebase apps/projetos ao mesmo tempo, mantendo o contrato atual da app Android, e atribuindo cada caso a uma app Firebase específica.

Isto existe para suportar crescimento operacional:

- o backoffice pode introduzir uma nova Firebase app
- a nova app fica elegível para novos casos
- casos antigos continuam presos à app antiga
- novos APKs passam a ser gerados com a conta Firebase correspondente

## Regra funcional acordada

1. a escolha da Firebase app fica persistida no caso
2. a ativação não escolhe a app; a ativação materializa no device a app já escolhida pelo caso
3. apenas apps marcadas como ativas entram na seleção de novos casos
4. `PUT /api/fcm-token` e `POST /api/device-commands/screenshot` resolvem a Firebase app a partir do caso do device
5. o contrato HTTP da app Android não muda

## Princípios de desenho

- manter Application como dono da regra de roteamento
- manter Infrastructure como adaptador das credenciais, SDK e dispatch
- não manter uma singleton global implícita como única Firebase app
- não inferir a app pelo `fcmToken`
- não recalcular a app do caso em cada request
- não misturar escolha de app nova com migração retroativa de casos antigos

## Modelo conceptual

### Nova entidade operacional

Introduzir uma entidade/configuração explícita de Firebase app, por exemplo:

- `FirebaseProjectBinding`
- `FirebaseAppRegistration`
- `FirebaseMessagingApp`

O nome final pode variar, mas a semântica precisa ser esta:

- identificador estável interno
- nome operacional
- `ProjectId`
- caminho/identificador do service account
- flag operacional do tipo `IsActiveForNewCases`
- timestamps de criação/última atualização

`IsActiveForNewCases` é preferível a um nome genérico como `IsActive`, porque torna explícito que:

- a app pode continuar operacional para casos antigos
- mas pode deixar de ser elegível para novos casos

### Caso

O caso passa a guardar referência para a Firebase app atribuída, por exemplo:

- `FirebaseAppId`

Essa referência deve ser persistida no momento em que o caso é preparado para operação.

### Device

O device não precisa ser o dono primário da decisão.

O device deriva a Firebase app a partir do caso ao qual está vinculado.

## Estado alvo por fluxo

### Criação/preparação do caso

No momento em que um caso novo entra no backend, a Application escolhe uma Firebase app elegível para novos casos.

A escolha mínima pode ser:

- a única app com `IsActiveForNewCases = true`

Se houver mais de uma ativa, a regra precisa ser explícita. Até existir regra melhor, a recomendação é:

- falhar configuração ambígua em vez de fazer balanceamento implícito opaco

Isto mantém o sistema determinístico e evita “distribuição” silenciosa sem critério auditável.

Decisão fechada:

- `POST /api/cases` deve devolver `503 Service Unavailable` quando não existir exatamente uma app elegível para novos casos

### Ativação

`POST /api/activate` continua com o mesmo contrato.

Durante a ativação, o backend:

- resolve o token
- resolve o caso
- vincula `deviceId -> caseId`
- herda a Firebase app do caso

A ativação não escolhe uma app nova. A ativação apenas usa a escolha já persistida no caso.

### Binding do FCM token

`PUT /api/fcm-token` continua com o mesmo contrato.

Durante o binding, o backend:

- resolve o device
- resolve o caso do device
- resolve a Firebase app do caso
- persiste o binding do token no contexto certo

Decisão recomendada:

- adicionar referência explícita da Firebase app ao binding do token

Exemplo:

- `FcmTokenBinding.FirebaseAppId`

Isso elimina ambiguidade futura e facilita auditoria, limpeza e diagnósticos.

### Comando remoto

`POST /api/device-commands/screenshot` continua com o mesmo contrato.

Durante o dispatch, o backend:

- resolve o device
- resolve o binding do token
- resolve a Firebase app do caso/device
- obtém o dispatcher certo para essa app
- envia a mensagem pelo projeto Firebase correspondente

## Abordagem técnica recomendada

### Configuration/Infrastructure

Substituir a configuração única atual:

- `Firebase:Enabled`
- `Firebase:ProjectId`
- `Firebase:ServiceAccountPath`

por uma configuração capaz de representar múltiplas apps.

Exemplo conceptual:

```json
{
  "Firebase": {
    "Apps": [
      {
        "key": "fb-app-01",
        "projectId": "argus-a",
        "serviceAccountPath": "C:\\secrets\\argus-a.json",
        "isActiveForNewCases": true
      },
      {
        "key": "fb-app-02",
        "projectId": "argus-b",
        "serviceAccountPath": "C:\\secrets\\argus-b.json",
        "isActiveForNewCases": false
      }
    ]
  }
}
```

Observação:

- a configuração em ficheiro é só bootstrap local
- o estado operacional efetivo para roteamento deve ficar persistido na base

### Bootstrap

No arranque, a Infrastructure:

- carrega as apps configuradas
- inicializa um `FirebaseApp` por registo
- expõe um registry/factory para resolução por `FirebaseAppId`

Evitar:

- um único `FirebaseMessaging.DefaultInstance`
- um wrapper que esconda a identidade da app usada

Preferir:

- `IFirebaseAppRegistry`
- `IFirebaseMessagingClientFactory`
- resolução explícita por app id interno

### Application

Criar abstrações como:

- `IFirebaseAppAssignmentPolicy`
- `IFirebaseAppRepository`
- `IFirebaseAppResolver`

Responsabilidades:

- escolher app para novos casos
- garantir que a app atribuída existe
- recusar operar se a configuração persistida do caso estiver inválida

### Persistência

Mudanças mínimas esperadas:

- nova tabela para apps Firebase
- nova FK do caso para a app Firebase
- nova FK opcional do `FcmTokenBinding` para a app Firebase

### Auditoria

Adicionar eventos auditáveis quando fizer sentido:

- caso atribuído a Firebase app
- binding de `fcmToken` atualizado com a app Firebase
- comando enviado usando app Firebase específica

Não é necessário auditar todo detalhe técnico, mas a atribuição do caso deve ficar clara.

## Sequência recomendada de implementação

1. introduzir o modelo persistente de Firebase app
2. adicionar referência do caso para Firebase app
3. criar política de seleção para novos casos
4. adaptar o fluxo de criação/preparação do caso para persistir a app escolhida
5. adaptar `activate` para respeitar a app já atribuída ao caso
6. adaptar `fcm-token` para persistir binding no contexto da app correta
7. substituir dispatcher global por resolução por app
8. adaptar `device-commands/screenshot` para usar a app correta
9. adicionar auditoria mínima da atribuição
10. adicionar testes unitários e de integração
11. atualizar docs canónicos e só então fechar o `BB`

## Testes obrigatórios

### Unit

- policy escolhe app elegível quando existe exatamente uma ativa
- policy falha quando não existe nenhuma ativa
- policy falha quando existem múltiplas ativas e não há regra explícita adicional
- `activate` preserva a app já atribuída ao caso
- `request screenshot` resolve dispatcher pela app do caso/device

### Integration

- caso novo fica associado à app Firebase ativa
- binding de `fcmToken` persiste com app correta
- screenshot command usa a app correta e devolve `messageId`
- app inativa não entra na seleção de novos casos
- caso legado já atribuído continua a usar a sua app mesmo após ativação de nova app

## Failure paths obrigatórios

- nenhuma app elegível para novos casos
- múltiplas apps elegíveis sem política determinística
- caso sem app Firebase atribuída
- device vinculado a caso sem app válida
- service account configurado mas indisponível

## O que fica fora deste BB

- balanceamento sofisticado entre múltiplas apps ativas
- migração automática de casos antigos entre apps
- rotação automática de service accounts
- suporte a múltiplos tipos de comando além do caminho já existente
- alteração do contrato HTTP da app Android
- alteração do processo de upload de evidência

## Critério de fecho

O `BB-07.1` só pode ser marcado como `DONE` quando for verdade ao mesmo tempo que:

- o backend consegue arrancar com pelo menos duas Firebase apps configuradas
- um caso novo recebe uma app Firebase persistida
- `activate` continua funcional sem mudar o contrato da app
- `PUT /api/fcm-token` continua funcional e amarra o binding ao contexto correto
- `POST /api/device-commands/screenshot` usa a app Firebase correta do caso/device
- há prova executável do happy path e de pelo menos um failure path

## Estado de implementação

Fechado em 2026-04-06 com:

- `FirebaseAppRegistration` persistida no backend
- `Case.FirebaseAppId` persistido e usado como source of truth
- `FcmTokenBinding.FirebaseAppId` persistido no bind atual
- policy determinística de seleção: exatamente uma app ativa para novos casos
- bootstrap/configuração via `Firebase:Apps`
- `POST /api/cases` com `503` para ausência ou ambiguidade de app elegível
- testes unitários, integração e solution completa verdes

## Notas para a sessão que for implementar

- começar pelo modelo e pela política de atribuição, não pelo dispatcher
- não mexer no contrato HTTP da app
- não tentar resolver balanceamento avançado nesta primeira iteração
- se houver necessidade operacional imediata, preferir a regra simples:
  - exatamente uma app ativa para novos casos
  - qualquer ambiguidade vira erro explícito
