using Md2PDFConverter.Models;
using Md2PDFConverter.Services;
using Microsoft.Azure.Functions.Worker;

namespace Md2PDFConverter.Activities;

public class CleanUp(IStorageService storageService)
{
    [Function(nameof(CleanUp))]
    public async Task<CleanUpResponse> Run([ActivityTrigger] CleanUpRequest request, FunctionContext context)
    {
        // Delete temporary folder
        await storageService.DeleteFolderAsync(request.TempFolderPath);

        // Delete container instance

        return new CleanUpResponse(){};
    }
}