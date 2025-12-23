using Azure.ResourceManager.ContainerInstance;

namespace Md2PDFConverter.Services;

public interface IContainerService
{
    Task<(string, ContainerGroupData)> CreateContainerGroupDataAsync(
        Guid runId,
        string inputFolderPath,
        string outputFolderPath,
        string outputFileName,
        string orchInstanceId
    );
    Task<ContainerGroupResource> StartContainerAsync(string containerGroupName, ContainerGroupData containerGroupData);
    Task DeleteContainerAsync(string containerGroupName);
}