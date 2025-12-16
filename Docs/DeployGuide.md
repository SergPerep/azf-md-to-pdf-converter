# Deploy guide

## Create / Deploy resources

### Function App

- Hosting plan: Consumption
- Operating system: Windows
- Runtime: .NET 8 (LTS), isolated worker model
- Storage account: create dedicated Storage account
- Azure files: Disable. We don't need them.
- Diagnostic settings for the storage: Configure later
- Networking: Public access
- Monitoring: Create dedicated Application Isights resource

After the creation of the resourse. Configure deployment: Github Account

### Managed Identity for Container Instance

### Blob Storage

### EventGrid Topic

- Log Analytics Workspace
- Applicatio Insights for Function App
- User-Assigned Managed Identity for Container Instance resource
- Event Grid Topic
- Blob Storage

## Container: Build, publish and pull

It is cheaper to use github contianer registry.

## Role based access control

1. Activate Sytem-Assigned Managed Identity (SAMI) for Function App
2. Create a User-Assigned Managed Idenity (UAMI) resource for Container Instance (ACI). Azure Function will be creating ACI resource and connection this Managed Identity to it.

| Resource | Role | Scope |
|--|--|--|
| Function App (SAMI) | Azure Container Instances Contributor role | Resource Group |
| Function App (SAMI) | Managed Identity Operator | Resource Group |
| Function App (SAMI) | Storage Blob Data Contributor | Blob Storage |
| Container Instance (UAMI) | EventGrid Data Contributor | EventGrid |
| Container Instance (UAMI) | Storage Blob Data Contributor | Blob Storage | 