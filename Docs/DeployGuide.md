# Deploy guide

## Create / Deploy resources

## Container: Build, publish and pull

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