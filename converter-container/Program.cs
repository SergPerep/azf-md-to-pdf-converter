using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Markdown2Pdf;
using Markdown2Pdf.Options;
using Microsoft.Extensions.Configuration;

string localInputPath = Path.GetTempPath() + "md2pdf/input";
string localOutputPath = Path.GetTempPath() + "md2pdf/output";

// 1. Get env variables
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

List<string> missingEnvVariables = [];
var tenantId = GetEnvironmentalVariable("MD2PDF_TENANT_ID", missingEnvVariables);
var clientId = GetEnvironmentalVariable("MD2PDF_CLIENT_ID", missingEnvVariables);
var clientSecret = GetEnvironmentalVariable("MD2PDF_CLIENT_SECRET", missingEnvVariables);
var blobStorageUrl = GetEnvironmentalVariable("MD2PDF_BLOB_STORAGE_URL", missingEnvVariables);
var blobStorageInputFolderName = GetEnvironmentalVariable("MD2PDF_BLOB_INPUT_FOLDER_NAME", missingEnvVariables);
var blobStorageOutputFolderName = GetEnvironmentalVariable("MD2PDF_BLOB_OUTPUT_FOLDER_NAME", missingEnvVariables);
var outputFileName = GetEnvironmentalVariable("MD2PDF_BLOB_OUTPUT_FILE_NAME", missingEnvVariables);

if (missingEnvVariables.Any())
{
    throw new Exception("Missing env variables: " + string.Join(", ", missingEnvVariables));
};

// 2. Authenticate
var uri = new Uri(blobStorageUrl!);
var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
var containerClient = new BlobContainerClient(uri, credential);

// 3. Download input files
if (!Directory.Exists(localInputPath)) Directory.CreateDirectory(localInputPath);
var virtualFolder = blobStorageInputFolderName + "/";

foreach (var blobItem in containerClient.GetBlobs(prefix: virtualFolder))
{
    string blobName = blobItem.Name;
    string relativePath = blobName.Substring(virtualFolder.Length);
    string localFilePath = Path.Combine(localInputPath, relativePath);
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

string? GetEnvironmentalVariable(string envName, List<string> missingEnvVariables)
{
    var envVal = config[envName];
    if (string.IsNullOrWhiteSpace(envVal)) missingEnvVariables.Add(envName);
    return envVal;
}

