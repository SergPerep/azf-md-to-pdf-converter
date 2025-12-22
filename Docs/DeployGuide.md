# Deploy guide

## Create & deploy resources

### 1. Function App

Create resource:

- Hosting plan: Consumption
- Operating system: Windows
- Runtime: .NET 8 (LTS), isolated worker model
- Storage account: create dedicated Storage account
- Azure files: Disable. We don't need them.
- Diagnostic settings for the storage: Configure later
- Networking: Public access
- Monitoring: Create dedicated Application Isights resource

Afterwards:

- Configure deployment: Github Account
- Enable System-Assigned Managed Identity for the Azure Function App

### 2. Event Grid Topic

Create resource:

- On portal go to Event Grid -> Custom Event -> Topic.
- Networking: Public Access
- Event Schema: Event Grid Schema
- Data residency type: Regional

Create a subscription:

- Event Schema: Event Grid Schemas
- Endpoint Type: Azure Function
- Endpoint: Azure Function Event Handler name - "ContainerEventHandler"

### 3. Managed Identity for Container Instance

Create User Assigned Managed Identity.

### 4. Blob Storage

- Redundancy: LRS
- Public network access: Enable
- Public network access scope: Enable from all networks
- Data Protection: Disable everything
- Add container: "temp-files"
- Upload the "Input" folder (in this repository) or use your own project

### 5. GitHub Container Registry

Using [Github Container Registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry) is way cheaper than Azure Container Registry resource. So do that.

## Set Role Based Access Control

Configure RBAC for managed identities and mind the scopes.

| Resource | Role | Scope |
|--|--|--|
| Function App (SAMI) | Azure Container Instances Contributor role | Resource Group |
| Function App (SAMI) | Managed Identity Operator | Resource Group |
| Function App (SAMI) | Storage Blob Data Contributor | Blob Storage |
| Container Instance (UAMI) | EventGrid Data Contributor | EventGrid |
| Container Instance (UAMI) | Storage Blob Data Contributor | Blob Storage | 

- SAMI - Sytem-Assigned Managed Identity for Function App
- UAMI - User-Assigned Managed Idenity resource for Container Instance (ACI). Azure Function will be creating ACI resource and connection this Managed Identity to it.


## Set Envs for Azure Fucntion App

| Environment variable | Where to find |
|--|--|
| `BLOB_STORAGE_URL` | Storage Account |
| `BLOB_STORAGE_CONTAINER_NAME` | Storage Account Container |
| `CONTAINER_IMAGE` | GitHub registiry (Package) |
| `TOPIC_ENDPOINT` | Event Grid |
| `CONTAINER_UAMI_RESOURCE_ID` | User-Assigned Managed Identity |