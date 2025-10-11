using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.ContainerInstance.Models;
using Azure.ResourceManager.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MDsToPDFConverter.Function;

public class HttpTriggerContainer
{
    private readonly ILogger<HttpTriggerContainer> _logger;

    public HttpTriggerContainer(ILogger<HttpTriggerContainer> logger)
    {
        _logger = logger;
    }

    [Function("HttpTriggerContainer")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        var credential = new DefaultAzureCredential();
        // Set Azure Resourse Management Client
        var armClient = new ArmClient(credential);
        // Get subscribtion Id if you have only one
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroupName = "md2pdfconverter";
        string containerGroupName = $"job-container-{Guid.NewGuid().ToString().Substring(0, 8)}";
        string containerImage = "ghcr.io/sergperep/md2pdf-container:latest";

        var subscribtionId = subscription.Id.Name;

        var resourceGroup = armClient.GetResourceGroupResource(
            ResourceGroupResource.CreateResourceIdentifier(subscribtionId, resourceGroupName)
        );

        var resourceRequirements = new ContainerResourceRequirements(new ContainerResourceRequestsContent(cpu: 1, memoryInGB: 1));

        var container = new ContainerInstanceContainer(containerGroupName, containerImage, resourceRequirements);
        var envVariableNames = new string[] {
            "MD2PDF_TENANT_ID",
            "MD2PDF_CLIENT_ID",
            "MD2PDF_CLIENT_SECRET",
            "MD2PDF_BLOB_STORAGE_URL",
            "MD2PDF_BLOB_INPUT_FOLDER_NAME",
            "MD2PDF_BLOB_OUTPUT_FOLDER_NAME",
            "MD2PDF_BLOB_OUTPUT_FILE_NAME"
            };

        foreach (var envName in envVariableNames)
            container.EnvironmentVariables.Add(new ContainerEnvironmentVariable(envName) { Value = Environment.GetEnvironmentVariable(envName) });

        var containerGroupData = new ContainerGroupData(
            location: AzureLocation.GermanyWestCentral,
            containers: [container],
            osType: ContainerInstanceOperatingSystemType.Linux)
        { RestartPolicy = ContainerGroupRestartPolicy.Never };

        // Start container Long Runing Operation
        var containerGroupLro = await resourceGroup.GetContainerGroups().CreateOrUpdateAsync(
            Azure.WaitUntil.Completed, containerGroupName, containerGroupData);

        var result = containerGroupLro.Value;
        _logger.LogInformation($"Container started: {result.Id}");

        return new OkObjectResult($"Started container: {result.Data.Name}");
    }
}