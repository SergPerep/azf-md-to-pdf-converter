namespace Md2PDFConverter.Services;

public interface IStorageService
{
    Task<List<string>> ListAllFilesAsync(string folderPath);
    Task<string> ReadMdAsTextAsync(string filePath);
    Task DeleteFolderAsync(string folderPath);
    Task<Stream> DownloadStreamAsync(string blobPath);
    Task UploadStreamAsync(string blobPath, Stream fileStream);
    Task UploadBlobFromTextAsync(string blobPath, string content);
    Task DeleteBlobIfExistsAsync(string blobPath);
    string GenerateDownloadBlobLink(string blobName);
}