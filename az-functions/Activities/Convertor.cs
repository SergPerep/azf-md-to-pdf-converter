using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.ContainerInstance.Models;
using Azure.ResourceManager.Models;
using Azure.ResourceManager.Resources;
using Md2PDFConverter.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Md2PDFConverter.Activities;

public class Convertor
{
    [Function(nameof(Convertor))]
    public async Task<ConverterResponse> Run([ActivityTrigger] ConverterRequest request, FunctionContext context)
    {
        var logger = context.GetLogger(nameof(Convertor));
        var credential = new DefaultAzureCredential();
        // Set Azure Resourse Management Client
        var armClient = new ArmClient(credential);
        // Get subscribtion Id if you have only one
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroupName = "md2pdfconverter";
        var containerId = Guid.NewGuid().ToString().Substring(0, 8);
        string containerGroupName = $"job-container-{containerId}";

        var envVariableNames = new string[]{
            "BLOB_STORAGE_URL",
            "BLOB_STORAGE_CONTAINER_NAME",
            "TOPIC_ENDPOINT",
        };
        var containerImage = Environment.GetEnvironmentVariable("CONTAINER_IMAGE");
        var uamiResourceId = Environment.GetEnvironmentVariable("CONTAINER_UAMI_RESOURCE_ID");

        var subscribtionId = subscription.Id.Name;

        var resourceGroup = armClient.GetResourceGroupResource(
            ResourceGroupResource.CreateResourceIdentifier(subscribtionId, resourceGroupName)
        );

        var resourceRequirements = new ContainerResourceRequirements(new ContainerResourceRequestsContent(cpu: 1, memoryInGB: 1));

        var container = new ContainerInstanceContainer(containerGroupName, containerImage, resourceRequirements);

        foreach (var envName in envVariableNames)
            container.EnvironmentVariables.Add(new ContainerEnvironmentVariable(envName) { Value = Environment.GetEnvironmentVariable(envName) });

        container.EnvironmentVariables.Add(new ContainerEnvironmentVariable("CONTAINER_ID") { Value = containerId });
        container.EnvironmentVariables.Add(new ContainerEnvironmentVariable("BLOB_INPUT_FOLDER_NAME") { Value = request.InputFolderPath });
        container.EnvironmentVariables.Add(new ContainerEnvironmentVariable("BLOB_OUTPUT_FOLDER_NAME") { Value = request.OutputFolderPath });
        container.EnvironmentVariables.Add(new ContainerEnvironmentVariable("BLOB_OUTPUT_FILE_NAME") { Value = request.OutputFileName });

        var uaManagedIdentity = new ManagedServiceIdentity(ManagedServiceIdentityType.UserAssigned)
        {
            UserAssignedIdentities =
            {
                { new ResourceIdentifier(uamiResourceId), new UserAssignedIdentity() }
            }
        };

        var containerGroupData = new ContainerGroupData(
            location: AzureLocation.GermanyWestCentral,
            containers: [container],
            osType: ContainerInstanceOperatingSystemType.Linux)
        {
            RestartPolicy = ContainerGroupRestartPolicy.Never,
            Identity = uaManagedIdentity
        };

        // Start container Long Runing Operation
        try
        {
            var containerGroupLro = await resourceGroup.GetContainerGroups().CreateOrUpdateAsync(
                Azure.WaitUntil.Completed, containerGroupName, containerGroupData);

            var result = containerGroupLro.Value;
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