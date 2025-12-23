using Md2PDFConverter.Models;
using Md2PDFConverter.Services;
using Microsoft.Azure.Functions.Worker;

namespace Md2PDFConverter.Activities;

public class MoveImages (IStorageService storageService)
{
    [Function(nameof(MoveImages))]
    public async Task<MoveImagesResponse> Run([ActivityTrigger] MoveImagesRequest request)
    {
        await storageService.DeleteFolderAsync(request.DestFolderPath);

        foreach (var md in request.Mds)
        {
            foreach (var imagePathPair in md.ImagePaths)
            {
                var destImageFilePath = request.DestFolderPath + "/" + Guid.NewGuid() + Path.GetExtension(imagePathPair.Value);
                var fileStream = await storageService.DownloadStreamAsync(imagePathPair.Value);
                await storageService.UploadStreamAsync(destImageFilePath, fileStream);
                md.ImagePaths[imagePathPair.Key] = destImageFilePath;
            }
        }
        return new MoveImagesResponse
        {
            Mds = request.Mds
        };
    }
}