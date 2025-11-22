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

public static class TestOrchestrator
{
    [Function(nameof(TestOrchestrator))]
    public static async Task<string> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(TestOrchestrator));

        // Replace name and input with values relevant for your Durable Functions Activity
        var scanFilesResponse = await context.CallActivityAsync<ScanFilesResponse>(nameof(ScanFiles), new ScanFilesRequest { FolderPath = "input" });
        var mapImagePathsResponse = await context.CallActivityAsync<MapImagePathsResponse>(nameof(MapImagePaths), new MapImagePathsRequest { Mds = scanFilesResponse.Mds, ImagePaths = scanFilesResponse.ImageFilePaths });

        var moveImagesResponse = await context.CallActivityAsync<MoveImagesResponse>(nameof(MoveImages), new MoveImagesRequest { DestFolderPath = "_temp/img", Mds = mapImagePathsResponse.Mds });

        var combineMdsResponse = await context.CallActivityAsync<CombineMdsResponse>(nameof(CombineMds), new CombineMdsRequest { DestFilePath = "_temp/index.md", Mds = moveImagesResponse.Mds });

        var convertorRequest = new ConverterRequest
        {
            InputFolderPath = Path.GetDirectoryName(combineMdsResponse.MdFilePath),
            OutputFileName = "readme.pdf",
            OutputFolderPath = "output"
        };

        var converterResponse = await context.CallActivityAsync<ConverterResponse>(nameof(Convertor), convertorRequest);

        logger.LogInformation("Waiting for event...");
        var (IsTimedOut, converterEventData) = await WaitForEventWithTimeOut<ConverterEventData>(
            context: context,
            timeOutIn: TimeSpan.FromMinutes(10),
            eventName: "ConverterResult"
        );

        if (IsTimedOut)
        {
            throw new Exception("The await of container event is timed out");
        }

        if (converterEventData?.Status != ConverterEventDataStatus.Completed)
        {
            throw new Exception("The converter container failed to generate PDF. Details: " + converterEventData?.ErrorMessage);
        }

        return "PDF is created!";
    }

    [Function("TestOrchestrator_HttpStart")]
    public static async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("Orchestrator_HttpStart");

        // Function input comes from the request content.
        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(TestOrchestrator));

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