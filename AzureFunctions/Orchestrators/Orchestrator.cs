using Azure;
using Md2PDFConverter.Activities;
using Md2PDFConverter.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Shared.Models;

namespace Md2PDFConverter.Orchestrators;

public static class Orchestrator
{
    [Function(nameof(Orchestrator))]
    public static async Task<string> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(Orchestrator));

        var scanFilesResponse = await context.CallActivityAsync<ScanFilesResponse>(
            nameof(ScanFiles),
            new ScanFilesRequest { FolderPath = "input" });

        var runId = context.NewGuid();

        await context.CallActivityAsync<string>(nameof(OrchLogger), $"Generated runId: {runId}");

        var tempFolderPath = $"_temp-{runId}";
        var outputFolderPath = $"output-{runId}";
        var outputFileName = "readme.pdf";

        var mapImagePathsResponse = await context.CallActivityAsync<MapImagePathsResponse>(
            nameof(MapImagePaths),
            new MapImagePathsRequest { Mds = scanFilesResponse.Mds, ImagePaths = scanFilesResponse.ImageFilePaths });

        var moveImagesResponse = await context.CallActivityAsync<MoveImagesResponse>(
            nameof(MoveImages),
            new MoveImagesRequest { DestFolderPath = Path.Combine(tempFolderPath, "img"), Mds = mapImagePathsResponse.Mds });

        var combineMdsResponse = await context.CallActivityAsync<CombineMdsResponse>(
            nameof(CombineMds),
            new CombineMdsRequest { DestFilePath = Path.Combine(tempFolderPath, "index.md"), Mds = moveImagesResponse.Mds });

        var converterRequest = new ConverterRequest
        {
            RunId = runId,
            InputFolderPath = Path.GetDirectoryName(combineMdsResponse.MdFilePath),
            OutputFileName = outputFileName,
            OutputFolderPath = outputFolderPath,
            OrchInstanceId = context.InstanceId
        };

        var converterResponse = await context.CallActivityAsync<ConverterResponse>(nameof(Convertor), converterRequest);

        await context.CallActivityAsync(nameof(OrchLogger), "Waiting for event...");

        var (IsTimedOut, converterEventData) = await WaitForEventWithTimeOut<ConverterEventData>(
            context: context,
            timeOutIn: TimeSpan.FromMinutes(15),
            eventName: "ConverterResult"
        );

        if (IsTimedOut)
        {
            throw new TimeoutException("The waiting for container event has timed out");
        }

        if (converterEventData?.Status != ConverterEventDataStatus.Completed)
        {
            throw new InvalidOperationException("The converter container has failed to generate PDF. Details: " + converterEventData?.ErrorMessage);
        }

        var downloadLink = await context.CallActivityAsync<string>(
            nameof(GenerateDownloadLink),
            Path.Combine(outputFolderPath, outputFileName)
        );

        var responseMessage = $"PDF has been created: {downloadLink}";

        await context.CallActivityAsync<string>(nameof(OrchLogger), responseMessage);

        // Clean up if run is successful
        await context.CallActivityAsync<CleanUpRequest>(
            nameof(CleanUp),
            new CleanUpRequest { ContainerGroupName = converterResponse?.ContainerGroupName ?? "", TempFolderPath = tempFolderPath }
        );

        return responseMessage;
    }

    [Function("Orchestrator_HttpStart")]
    public static async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("Orchestrator_HttpStart");

        // Function input comes from the request content.
        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(Orchestrator));

        logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        // Returns an HTTP 202 response with an instance management payload.
        // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }

    static private async Task<(bool IsTimedOut, T? EventData)> WaitForEventWithTimeOut<T>(
    TaskOrchestrationContext context,
    TimeSpan timeOutIn,
    string eventName
    )
    {
        var cancellationTokenResource = new CancellationTokenSource();
        var timeoutAt = context.CurrentUtcDateTime + timeOutIn;
        var timeOutTask = context.CreateTimer(timeoutAt, cancellationTokenResource.Token);
        var eventTask = context.WaitForExternalEvent<T>(eventName);
        var winner = await Task.WhenAny(eventTask, timeOutTask);

        if (winner == eventTask)
        {
            cancellationTokenResource.Cancel();
            return (false, await eventTask);
        }
        return (true, default(T));
    }
}