# Md to pdf converter

Converts a collection of markdown files into a single PDF, preserving links, images and mermaid diagrams. Deployed to Microsoft Azure.

The project is built on top of the [Markdown2Pdf library](https://github.com/Flayms/Markdown2Pdf), but the library itself can handle only one-to-one conversion. This implementation through receives multiple markdown files, validates them, compiles them into a single markdown file and then puts it though converter. 

## Flow

- **Azure Function App** orchestrates the flow. Triggered by http request.
- **Azure Blob Storage** - used for storing inputs, outputs and intermediate files.
- **Azure Container Instance (ACI)** handles actuall md-to-pdf conversion. At the end of the conversion sends an event grid event about the result of the convertion to the Event Grid Topic.
- **Event Grid Topic** catches event from ACI and directs it to Azure Function App

![Diagram](./Docs/Img/Md2Pdf%20Converter%20Azure.png)


Why container? [The Markdown2Pdf library](https://github.com/Flayms/Markdown2Pdf) converts via Chrome. And Chrome cannot be installed on Azure Function - the file sistem is locked during runtime.

Why Event Grid? ACI cannot communicate with Azure Function directly. The event flow can be confgured via Queues but for the sake of the exercise the Event Grid Topic has been chosen.

# Preparing input

For the succesfull run the markdown files must be stuctured properly. That way the process knows in what order merge files and how to nest its content.

Rules to follow:

- The root folder must contain `index.md`
- Each md file must contain a single H1 header (no more no less). It is going to be replaced based on folder structure.
- The md name must be `index.md` or start with 2-digit number followed by underscore e.g. `08_`
- The folder name must start with 2-digit number followed by underscore
- It does not matter how images are named and where to they are places, but the have to be inside the root folder.
- The numbered files and folders can skip a number. The numbers used for figuring order.
- Each folder must have index.md

**Header degradation**. Headers will be amended based on the folder structure and `.md` file name. For example:

- The h1 of `index.md` in the root folder will stay h1
- The h1 of `02_Definition` in the root folder will become h2
- The h1 of `04_History/index.md` will become h2
- The h1 of the `04_History/01_Ancient history.md` will become h3

This applies not only for h1 but for every header level.


Example of the correct folder structure:

```
01_Etymology and nomenclature/
|- 01_Pagan.md
|- 02_Helene.md
|- 03_Heathen.md
|- index.md
04_History/
|- img/
|- 01_Ancient history.md
|- 02_Christianisation.md
|- 03_Postclassical history.md
|- index.md
05_Modern paganism/
|- img/
|- index.md
02_Definition.md
03_Perception and Ethnocentrism.md
index.md
```

- [ ] Add input example
- [ ] Add Env variables  
- [ ] Add Image registry to diagram