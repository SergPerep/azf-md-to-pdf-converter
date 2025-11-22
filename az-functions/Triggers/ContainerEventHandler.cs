using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

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
        var instanceId = gridEvent.Subject;
        var converterEventData = gridEvent.Data.ToObjectFromJson<ConverterEventData>();

        await client.RaiseEventAsync(instanceId, "ConverterResult_" + converterEventData?.ContainerId, converterEventData);
        // _logger.LogInformation("Event type: {type}, Event subject: {subject}", cloudEvent.Type, cloudEvent.Subject);
    }
}

class ConverterEventData
{
    public string Status { get; set; }
    public string ContainerId { get; set; }
}