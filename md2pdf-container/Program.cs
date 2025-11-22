using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using Markdown2Pdf;
using Markdown2Pdf.Options;
using Microsoft.Extensions.Configuration;

// 1. Get env variables
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var localInputPath = Path.GetTempPath() + "input";
var localOutputPath = Path.GetTempPath() + "output";

EventGridPublisherClient? publisherClient = null;

List<string> missingEnvVariables = [];
var containerId = GetEnvironmentalVariable("CONTAINER_ID", missingEnvVariables);
var blobStorageUrl = GetEnvironmentalVariable("BLOB_STORAGE_URL", missingEnvVariables);
var blobStorageContainerName = GetEnvironmentalVariable("BLOB_STORAGE_CONTAINER_NAME", missingEnvVariables);
var blobStorageInputFolderName = GetEnvironmentalVariable("BLOB_INPUT_FOLDER_NAME", missingEnvVariables);
var blobStorageOutputFolderName = GetEnvironmentalVariable("BLOB_OUTPUT_FOLDER_NAME", missingEnvVariables);
var outputFileName = GetEnvironmentalVariable("BLOB_OUTPUT_FILE_NAME", missingEnvVariables);
var topicEndpoint = GetEnvironmentalVariable("TOPIC_ENDPOINT", missingEnvVariables);

if (missingEnvVariables.Any())
{
    throw new Exception("Missing env variables: " + string.Join(", ", missingEnvVariables));
}

try
{
    // 2. Authenticate
    var uri = new Uri(Path.Combine(blobStorageUrl!, blobStorageContainerName!));
    var credential = new ManagedIdentityCredential();
    publisherClient = new EventGridPublisherClient(new Uri(topicEndpoint), credential);

    var containerClient = new BlobContainerClient(uri, credential);

    // 3. Download input files
    if (!Directory.Exists(localInputPath)) Directory.CreateDirectory(localInputPath);
    var virtualFolder = blobStorageInputFolderName + "/";

    foreach (var blobItem in containerClient.GetBlobs(prefix: virtualFolder))
    {
        string blobName = blobItem.Name;
        string relativePath = blobName.Substring(virtualFolder.Length);
        string localFilePath = Path.Combine(localInputPath, relativePath);

        var localFolderPath = Path.GetDirectoryName(localFilePath);
        if (!string.IsNullOrWhiteSpace(localFolderPath) && !Directory.Exists(localFolderPath))
            Directory.CreateDirectory(localFolderPath);

        var blobClient = containerClient.GetBlobClient(blobName);
        Console.WriteLine($"Download {blobName} to {localFilePath}");
        blobClient.DownloadTo(localFilePath);
    }

    // 4. Convert to PDF
    var mdFilePath = Directory.GetFiles(localInputPath)
        .First(filePath => Path.GetExtension(filePath) == ".md");
    var options = new Markdown2PdfOptions();
    options.ChromePath = Path.Combine(Environment.CurrentDirectory, "ChromeHeadlessShell/chrome-headless-shell");
    Console.WriteLine($"Chrome path: {options.ChromePath}");

    var converter = new Markdown2PdfConverter(options);
    await converter.Convert(mdFilePath, Path.Combine(localOutputPath, outputFileName!));

    // 3. Upload PDF to blob storage
    foreach (var filePath in Directory.GetFiles(localOutputPath))
    {
        var blobName = blobStorageOutputFolderName + "/" + Path.GetFileName(filePath);
        var stream = File.OpenRead(filePath);
        await containerClient.UploadBlobAsync(blobName, stream);
    }

    Console.WriteLine($"PDF has been generated!");

    var evt = new EventGridEvent(
        subject: $"container/{containerId}/result",
        eventType: "Container.JobCompleted",
        dataVersion: "1.0",
        data: new ConverterEventData
        {
            Status = "Completed",
            ContainerId = containerId
        }
    );

    await publisherClient.SendEventAsync(evt);
}
catch (Exception ex)
{
    var evt = new EventGridEvent(
        subject: $"container/{containerId}/result",
        eventType: "Container.JobFailed",
        dataVersion: "1.0",
        data: new
        {
            Status = "Failed",
            ErrorMessage = ex.ToString()
        }
    );
    if (publisherClient != null)
        await publisherClient.SendEventAsync(evt);
    throw;
}

string? GetEnvironmentalVariable(string envName, List<string> missingEnvVariables)
{
    var envVal = config[envName];
    if (string.IsNullOrWhiteSpace(envVal)) missingEnvVariables.Add(envName);
    return envVal;
}

class ConverterEventData
{
    public string Status { get; set; }
    public string ContainerId { get; set; }
}