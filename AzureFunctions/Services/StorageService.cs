using System.Reflection.Metadata;
using System.Text;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace Md2PDFConverter.Services;

public class StorageService : IStorageService
{
    private BlobServiceClient _blobServiceClient;
    private BlobContainerClient _blobContainerClient;
    private UserDelegationKey _userDelegationKey;
    public StorageService()
    {

        var blobStorageUrl = Environment.GetEnvironmentVariable("BLOB_STORAGE_URL");
        var blobStorageContainerName = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONTAINER_NAME");
        _blobServiceClient = new BlobServiceClient(
            new Uri(blobStorageUrl),
            new DefaultAzureCredential()
        );
        _blobContainerClient = _blobServiceClient.GetBlobContainerClient(blobStorageContainerName);
    }

    public async Task<List<string>> ListAllFilesAsync(string folderPath)
    {
        var filePaths = new List<string>();
        if (!folderPath.EndsWith("/")) folderPath += "/";

        await foreach (var blobItem in _blobContainerClient.GetBlobsAsync(prefix: folderPath))
        {
            filePaths.Add(blobItem.Name);
        }
        return filePaths;
    }

    public async Task<string> ReadMdAsTextAsync(string filePath)
    {
        if (Path.GetExtension(filePath) != ".md")
        {
            throw new Exception($"The file must be .md. Instead got {filePath}");
        }

        var blobClient = _blobContainerClient.GetBlobClient(filePath);
        var response = await blobClient.DownloadContentAsync();
        return response.Value.Content.ToString();
    }

    public async Task DeleteFolderAsync(string folderPath)
    {
        await foreach (var blobItem in _blobContainerClient.GetBlobsAsync(prefix: folderPath))
        {
            await _blobContainerClient.DeleteBlobAsync(blobItem.Name);
        }
    }

    public async Task<Stream> DownloadStreamAsync(string blobPath)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobPath);
        var response = await blobClient.DownloadStreamingAsync();
        return response.Value.Content;
    }

    public async Task UploadStreamAsync(string blobPath, Stream fileStream)
    {
        await _blobContainerClient.UploadBlobAsync(blobPath, fileStream);
    }

    public async Task UploadBlobFromTextAsync(string blobPath, string content)
    {
        byte[] byteArray = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(byteArray);
        await _blobContainerClient.UploadBlobAsync(blobPath, stream);
    }

    public async Task DeleteBlobIfExistsAsync(string blobPath)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobPath);
        await blobClient.DeleteIfExistsAsync();
    }

    public string GenerateDownloadBlobLink(string blobName)
    {

        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        if (!blobClient.Exists())
        {
            throw new InvalidOperationException($"Cannot find blob: {blobName}");
        }

        if (_userDelegationKey == null || _userDelegationKey.SignedExpiresOn <= DateTime.UtcNow.AddMinutes(-1))
        {
            _userDelegationKey = _blobServiceClient.GetUserDelegationKey(
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1)
            );
        }

        var sasUri = blobClient.GenerateUserDelegationSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.AddMinutes(15),
            _userDelegationKey);
        
        return sasUri.ToString();
    }
}