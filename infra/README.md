# Argus.EvidencePlatform Infrastructure

This directory contains the Azure Terraform skeleton for the evidence intake platform.

Scope:
- Azure foundation for `dev`
- remote state bootstrap
- reusable modules
- Azure Container Apps deployment shape for `Api` and `Workers`
- private data-plane defaults for PostgreSQL, Blob Storage, and Key Vault

Current layout:

```text
infra/
  bootstrap/
    tfstate/
  environments/
    dev/
  modules/
    container_app/
    container_apps_environment/
    container_registry/
    key_vault/
    monitoring/
    network/
    postgresql_flexible_server/
    resource_group/
    storage/
```

## Apply Order

1. Bootstrap the remote state storage account.
2. Copy the bootstrap outputs into `environments/dev/backend.hcl`.
3. Initialize the `dev` environment with the remote backend.
4. Set real values in `terraform.tfvars`.
5. Apply the foundation.
6. Turn on `deploy_workloads` only after container images exist in ACR.

## Notes

- The workload storage account is configured for managed-identity access from Container Apps.
- `Storage` in the application should use `Storage__ServiceUri` instead of a connection string in Azure.
- PostgreSQL currently uses password authentication because the .NET scaffold does not yet implement Azure AD auth for database access.
- Key Vault is provisioned as a control-plane resource with RBAC enabled. Secrets and keys are intentionally not created in this first scaffold because they require data-plane role propagation and tend to make the first apply brittle.
- ACR is public in `dev` by default so image pull setup is straightforward. Storage, Key Vault, and PostgreSQL remain private by default.

## Sources

- Azure Terraform state in Blob Storage: <https://learn.microsoft.com/en-us/azure/developer/terraform/store-state-in-azure-storage>
- Azure Container Apps managed identity image pull: <https://learn.microsoft.com/en-us/azure/container-apps/managed-identity-image-pull>
- Azure Storage private endpoints: <https://learn.microsoft.com/en-us/azure/storage/common/storage-private-endpoints>
- Azure Key Vault RBAC guide: <https://learn.microsoft.com/en-us/azure/key-vault/general/rbac-guide>

