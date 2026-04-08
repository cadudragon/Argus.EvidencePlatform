# Delivery Receipt

- contract_id: `BB-08`
- delivery_status: `IMPLEMENTED`
- stack_hint: `.NET`

## Delivered Public Surface

- `GET /api/evidence/cases/{caseId}/artifacts`
- `GET /api/evidence/artifacts/{artifactId}/content`

## Changed Files

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
- `docs/exported-screenshots/bb08-real-endpoint-proof-4e988ffc-aa59-460b-a6b2-984c3e6a952d.jpg`

## Delivery Notes

- a timeline existente foi preservada
- a listagem nova devolve `items` e `nextCursor`
- o download usa `Stream` via boundary `IEvidenceBlobReader`
- `Range` foi mantido no read path através de `Results.File(..., enableRangeProcessing: true)`
- docs canónicos do `BB-08` foram atualizados para refletir o estado final
- prova operacional real foi fechada com screenshot do caso `CASE-ACT-20260403-194200`
- o download real usou o endpoint novo, não o export do Azurite

## Proof Commands

1. `C:\Progra~1\dotnet\dotnet.exe build Argus.EvidencePlatform.slnx`
   - resultado observado: `Build succeeded. 0 Warning(s), 0 Error(s).`
2. `C:\Progra~1\dotnet\dotnet.exe test tests\Argus.EvidencePlatform.UnitTests\Argus.EvidencePlatform.UnitTests.csproj --filter "FullyQualifiedName~ListCaseArtifactsHandlerTests|FullyQualifiedName~GetArtifactContentHandlerTests"`
   - resultado observado: `Passed. Failed: 0, Passed: 6, Skipped: 0, Total: 6.`
3. `C:\Progra~1\dotnet\dotnet.exe test tests\Argus.EvidencePlatform.IntegrationTests\Argus.EvidencePlatform.IntegrationTests.csproj --filter "FullyQualifiedName~EvidenceReadEndpointsTests"`
   - resultado observado: `Passed. Failed: 0, Passed: 7, Skipped: 0, Total: 7.`
4. `C:\Progra~1\dotnet\dotnet.exe test Argus.EvidencePlatform.slnx`
   - resultado observado:
     - `UnitTests: Passed 97`
     - `IntegrationTests: Passed 44`
     - `ArchTests: Passed 1`

## Failure Paths Exercised

- `GET /api/evidence/artifacts/{artifactId}/content` -> `404 Not Found`
- `GET /api/evidence/artifacts/{artifactId}/content` -> `409 Conflict`
- `GET /api/evidence/artifacts/{artifactId}/content` -> `206 Partial Content`
- `GET /api/evidence/cases/{caseId}/artifacts?pageSize=1` -> paginação com `nextCursor`
- `GET /api/evidence/cases/{caseId}/artifacts?cursor=not-base64` -> `400 Bad Request`
- `GET /api/evidence/cases/{caseId}/artifacts?pageSize=0` -> `400 Bad Request`

## Operational Proof

- caso real: `CASE-ACT-20260403-194200`
- `artifactId` real: `4e988ffc-aa59-460b-a6b2-984c3e6a952d`
- endpoint usado:
  - `GET /api/evidence/cases/6d0fc5f2-3f6a-4f0f-8fd7-c13c55d21611/artifacts?pageSize=20`
  - `GET /api/evidence/artifacts/4e988ffc-aa59-460b-a6b2-984c3e6a952d/content`
- ficheiro salvo:
  - `C:\Src\Argus.EvidencePlatform\docs\exported-screenshots\bb08-real-endpoint-proof-4e988ffc-aa59-460b-a6b2-984c3e6a952d.jpg`
- resultado observado:
  - `savedSizeBytes = 165148`
  - `hashMatches = true`
  - `imageWidth = 1220`
  - `imageHeight = 2712`
