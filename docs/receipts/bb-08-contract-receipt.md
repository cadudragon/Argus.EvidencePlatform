# Contract Receipt

- contract_id: `BB-08`
- status: `APPROVED`
- approved_by: `user`
- approved_via: `chat`
- stack_hint: `.NET`
- scope_summary: `Leitura HTTP mínima de evidências com listagem paginada por caso e download por streaming, preservando a timeline existente.`

## Public Boundaries

- `GET /api/evidence/cases/{caseId}/artifacts`
- `GET /api/evidence/artifacts/{artifactId}/content`

## Behavioral Contract

- `GET /api/evidence/cases/{caseId}/artifacts`
  - resposta com `items` e `nextCursor`
  - ordenação por `captureTimestamp desc`, `receivedAt desc`, `id desc`
  - não lê blobs para compor a listagem
  - `400` para `cursor` inválido ou `pageSize` fora do limite
- `GET /api/evidence/artifacts/{artifactId}/content`
  - usa `Stream`, não `byte[]`
  - define `Content-Type`
  - suporta `Range` simples quando aplicável
  - `404` para artefacto inexistente
  - `409` quando existe metadata relacional mas o blob não pode ser aberto

## Constraints

- preservar `GET /api/evidence/cases/{caseId}/timeline`
- não introduzir processamento pesado no read path
- introduzir boundary explícita de leitura de blob em `Application/Infrastructure`
- atualizar docs canónicos se a surface final divergir da recomendação textual existente

## allowed_files

- `docs/application-overview.md`
- `docs/backend-build-plan.md`
- `docs/backend-build-runbook.md`
- `docs/receipts/bb-08-contract-receipt.md`
- `docs/receipts/bb-08-delivery-receipt.md`
- `docs/receipts/bb-08-verification-verdict.md`
- `src/Argus.EvidencePlatform.Api/Features/Evidence/EvidenceEndpoints.cs`
- `src/Argus.EvidencePlatform.Application/Common/Abstractions/IEvidenceRepository.cs`
- `src/Argus.EvidencePlatform.Application/Common/Abstractions/ArtifactListCursor.cs`
- `src/Argus.EvidencePlatform.Application/Common/Abstractions/ArtifactListPage.cs`
- `src/Argus.EvidencePlatform.Application/Common/Abstractions/EvidenceArtifactDescriptor.cs`
- `src/Argus.EvidencePlatform.Application/Common/Abstractions/EvidenceArtifactListItem.cs`
- `src/Argus.EvidencePlatform.Application/Common/Abstractions/EvidenceContentStream.cs`
- `src/Argus.EvidencePlatform.Application/Common/Abstractions/IEvidenceBlobReader.cs`
- `src/Argus.EvidencePlatform.Application/Evidence/ListArtifacts/ListCaseArtifactsQuery.cs`
- `src/Argus.EvidencePlatform.Application/Evidence/ListArtifacts/ListCaseArtifactsHandler.cs`
- `src/Argus.EvidencePlatform.Application/Evidence/GetArtifactContent/GetArtifactContentQuery.cs`
- `src/Argus.EvidencePlatform.Application/Evidence/GetArtifactContent/GetArtifactContentHandler.cs`
- `src/Argus.EvidencePlatform.Application/Evidence/GetArtifactContent/EvidenceContentOutcome.cs`
- `src/Argus.EvidencePlatform.Application/Evidence/GetArtifactContent/EvidenceContentResult.cs`
- `src/Argus.EvidencePlatform.Contracts/Evidence/ArtifactListItemResponse.cs`
- `src/Argus.EvidencePlatform.Contracts/Evidence/ListCaseArtifactsResponse.cs`
- `src/Argus.EvidencePlatform.Infrastructure/DependencyInjection.cs`
- `src/Argus.EvidencePlatform.Infrastructure/Persistence/Repositories/EvidenceRepository.cs`
- `src/Argus.EvidencePlatform.Infrastructure/Storage/AzureEvidenceBlobReader.cs`
- `tests/Argus.EvidencePlatform.IntegrationTests/HealthEndpointsTests.cs`
- `tests/Argus.EvidencePlatform.IntegrationTests/PostgresScreenshotsEndpointsTests.cs`
- `tests/Argus.EvidencePlatform.IntegrationTests/EvidenceReadEndpointsTests.cs`
- `tests/Argus.EvidencePlatform.UnitTests/FinalizeEvidenceIntakeHandlerTests.cs`
- `tests/Argus.EvidencePlatform.UnitTests/GetEvidenceTimelineHandlerTests.cs`
- `tests/Argus.EvidencePlatform.UnitTests/GetArtifactContentHandlerTests.cs`
- `tests/Argus.EvidencePlatform.UnitTests/ListCaseArtifactsHandlerTests.cs`
