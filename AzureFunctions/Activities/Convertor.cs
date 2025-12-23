using Md2PDFConverter.Models;
using Md2PDFConverter.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Md2PDFConverter.Activities;

public class Convertor(IContainerService containerService)
{
    [Function(nameof(Convertor))]
    public async Task<ConverterResponse> Run([ActivityTrigger] ConverterRequest request, FunctionContext context)
    {
        var logger = context.GetLogger(nameof(Convertor));

        var (containerGroupName, containerGroupData) = await containerService.CreateContainerGroupDataAsync(
            runId: request.RunId,
            inputFolderPath: request.InputFolderPath,
            outputFolderPath: request.OutputFolderPath,
            outputFileName: request.OutputFileName,
            orchInstanceId: request.OrchInstanceId
        );

        // Start container Long Runing Operation
        try
        {
            var result = await containerService.StartContainerAsync(containerGroupName, containerGroupData);
            logger.LogInformation($"Container started: {result.Id}");
            return new ConverterResponse
            {
                ContainerInstanceId = result.Id.ToString()
            };

        }
        catch (Exception ex)
        {
            logger.LogError("Failed to start container instance");
            throw new InvalidOperationException("Container failed to run", ex);
        }
    }
}