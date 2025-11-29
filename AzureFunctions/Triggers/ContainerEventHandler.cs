using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Shared.Models;

namespace MdsToPDFConverter.Triggers;

public class ContainerEventHandler
{
    private readonly ILogger<ContainerEventHandler> _logger;

    public ContainerEventHandler(ILogger<ContainerEventHandler> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ContainerEventHandler))]
    public async Task Run([EventGridTrigger] EventGridEvent gridEvent, [DurableClient] DurableTaskClient client)
    {
        var converterEventData = gridEvent.Data.ToObjectFromJson<ConverterEventData>();
        var orchInstanceId = converterEventData?.OrchInstanceId;

        _logger.LogInformation("Recieved an event: " + gridEvent.Data.ToString());
        await client.RaiseEventAsync(orchInstanceId, "ConverterResult", converterEventData);
    }
}