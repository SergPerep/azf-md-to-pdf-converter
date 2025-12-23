using Md2PDFConverter.Models;
using Md2PDFConverter.Services;
using Microsoft.Azure.Functions.Worker;

namespace Md2PDFConverter.Activities;

public class CleanUp(IStorageService storageService, IContainerService containerService)
{
    [Function(nameof(CleanUp))]
    public async Task Run([ActivityTrigger] CleanUpRequest request, FunctionContext context)
    {
        // Delete temporary folder
        await storageService.DeleteFolderAsync(request.TempFolderPath);

        // Delete container instance
        await containerService.DeleteContainerAsync(request.ContainerGroupName);
    }
}