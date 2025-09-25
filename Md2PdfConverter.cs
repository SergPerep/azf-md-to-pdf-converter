using Azure.Identity;
using Azure.Storage.Blobs;
using Markdown2Pdf;
using Markdown2Pdf.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Microsoft.Extensions.Logging;

namespace MDsToPDFConverter.Function;

public class Md2PdfConvertor
{
    private readonly ILogger<Md2PdfConvertor> _logger;

    public Md2PdfConvertor(ILogger<Md2PdfConvertor> logger)
    {
        _logger = logger;
    }

    [Function("Md2PdfConvertor")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        string localInputPath = Path.GetTempPath() + "md2pdf/input";
        string localOutputPath = Path.GetTempPath() + "md2pdf/output";
        // 1. Download files form blob storage to temp
        BlobServiceClient blobServiceClient = new BlobServiceClient(
            new Uri("https://mdstopdfconverterstorage.blob.core.windows.net"),
            new DefaultAzureCredential()
        );
        var virtualFolder = "input/";
        var containerClient = blobServiceClient.GetBlobContainerClient("temp-files");
        if (!Directory.Exists(localInputPath)) Directory.CreateDirectory(localInputPath);

        foreach (var blobItem in containerClient.GetBlobs(prefix: virtualFolder))
        {
            string blobName = blobItem.Name;
            string relativePath = blobName.Substring(virtualFolder.Length);
            string localFilePath = Path.Combine(localInputPath, relativePath);
            var blobClient = containerClient.GetBlobClient(blobName);
            Console.Write($"Downloading {blobName} to {localFilePath}");
            blobClient.DownloadTo(localFilePath);
        }

        // 2. Convert
        var mdFilePath = Directory.GetFiles(localInputPath)
            .First(filePath => Path.GetExtension(filePath) == ".md");
        var options = new Markdown2PdfOptions();
        // options.ChromePath = 
        var converter = new Markdown2PdfConverter(options);
        await converter.Convert(mdFilePath, Path.Combine(localOutputPath, "readme.pdf"));

        // 3. Upload file to blob storages
        foreach (var filePath in Directory.GetFiles(localOutputPath)) {
            var blobName = "output/" + Path.GetFileName(filePath);
            var stream = File.OpenRead(filePath);
            await containerClient.UploadBlobAsync(blobName, stream);
        }
        
        // 4. Clean-up
        // _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Done!");
    }
}