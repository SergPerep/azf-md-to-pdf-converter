using Md2PDFConverter.Services;
using Microsoft.Azure.Functions.Worker;

namespace Md2PDFConverter.Activities;

public class GenerateDownloadLink(IStorageService storageService)
{
    [Function(nameof(GenerateDownloadLink))]
    public async Task<string> Run([ActivityTrigger] string BlobName)
    {
        var downloadLink = storageService.GenerateDownloadBlobLink(BlobName);
        return downloadLink;
    }
}