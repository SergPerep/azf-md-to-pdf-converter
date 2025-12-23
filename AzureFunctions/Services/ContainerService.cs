using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.ContainerInstance.Models;
using Azure.ResourceManager.Models;
using Azure.ResourceManager.Resources;

namespace Md2PDFConverter.Services;

public class ContainerService : IContainerService
{
    readonly string resourceGroupName = "md2pdfconverter";
    readonly ResourceGroupResource resourceGroup;
    public ContainerService()
    {
        var credential = new DefaultAzureCredential();
        // Set Azure Resourse Management Client
        var armClient = new ArmClient(credential);
        // Get subscribtion Id if you have only one
        var subscription = armClient.GetDefaultSubscription();
        var subscribtionId = subscription.Id.Name;

        resourceGroup = armClient.GetResourceGroupResource(
            ResourceGroupResource.CreateResourceIdentifier(subscribtionId, resourceGroupName)
        );
    }

    public async Task<(string, ContainerGroupData)> CreateContainerGroupDataAsync(
        Guid runId,
        string inputFolderPath,
        string outputFolderPath,
        string outputFileName,
        string orchInstanceId
        )
    {
        string containerGroupName = $"job-container-{runId}";

        var envVariableNames = new string[]{
            "BLOB_STORAGE_URL",
            "BLOB_STORAGE_CONTAINER_NAME",
            "TOPIC_ENDPOINT",
        };
        var containerImage = Environment.GetEnvironmentVariable("CONTAINER_IMAGE");
        var uamiResourceId = Environment.GetEnvironmentVariable("CONTAINER_UAMI_RESOURCE_ID");
        var resourceRequirements = new ContainerResourceRequirements(new ContainerResourceRequestsContent(cpu: 1, memoryInGB: 1));

        var container = new ContainerInstanceContainer(containerGroupName, containerImage, resourceRequirements);

        foreach (var envName in envVariableNames)
            container.EnvironmentVariables.Add(new ContainerEnvironmentVariable(envName) { Value = Environment.GetEnvironmentVariable(envName) });

        container.EnvironmentVariables.Add(new ContainerEnvironmentVariable("CONTAINER_ID") { Value = runId.ToString() });
        container.EnvironmentVariables.Add(new ContainerEnvironmentVariable("BLOB_INPUT_FOLDER_NAME") { Value = inputFolderPath });
        container.EnvironmentVariables.Add(new ContainerEnvironmentVariable("BLOB_OUTPUT_FOLDER_NAME") { Value = outputFolderPath });
        container.EnvironmentVariables.Add(new ContainerEnvironmentVariable("BLOB_OUTPUT_FILE_NAME") { Value = outputFileName });
        container.EnvironmentVariables.Add(new ContainerEnvironmentVariable("ORCH_INSTANCE_ID") { Value = orchInstanceId });

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

        return (containerGroupName, containerGroupData);
    }

    public async Task<ContainerGroupResource> StartContainerAsync(string containerGroupName, ContainerGroupData containerGroupData)
    {
        var containerGroupLro = await resourceGroup.GetContainerGroups().CreateOrUpdateAsync(
            WaitUntil.Completed, containerGroupName, containerGroupData);
        return containerGroupLro.Value;
    }

    public async Task DeleteContainerAsync(string containerGroupName)
    {
        try
        {
            var containerGroup = await resourceGroup.GetContainerGroupAsync(containerGroupName);
            await containerGroup.Value.DeleteAsync(WaitUntil.Completed);
        }
        catch (Exception) { }
    }
}