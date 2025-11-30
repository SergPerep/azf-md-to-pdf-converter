# Md to pdf converter

Converts a collection of markdown files into a single PDF, preserving links, images and mermaid diagrams. Deployed to Microsoft Azure.

## Flow

- **Azure Function App** orchestrates the flow. Triggered by http request.
- **Azure Blob Storage** - used for storing inputs, outputs and intermediate files.
- **Azure Container Instance** handles actuall md-to-pdf conversion. At the end of processing send event grid event about the result of convertion to an Event Grid Topic.
- **Event Grid Topic** catches event from container and directs it to Azure Function App


![Diagram](./Docs/Img/Md2Pdf%20Converter%20Azure.png)

