using System.Text.RegularExpressions;
using Md2PDFConverter.Models;
using Md2PDFConverter.Services;
using Microsoft.Azure.Functions.Worker;

namespace Md2PDFConverter.Activities;

public class MapImagePaths(IStorageService storageService)
{
    private readonly Regex imageLineRegex = new Regex(@"!\[[^\]]*\]\(((?>[^()]+|\((?<open>)|\)(?<-open>))*(?(open)(?!)))\)", RegexOptions.Compiled);
    
    [Function(nameof(MapImagePaths))]
    public async Task<MapImagePathsResponse> Run([ActivityTrigger] MapImagePathsRequest request, FunctionContext context)
    {
        List<string> validationErrors = [];
        foreach (var md in request.Mds)
        {
            var content = await storageService.ReadMdAsTextAsync(md.Path);
            var imgSyntaxes = imageLineRegex.Matches(content);
            if (imgSyntaxes == null) continue;
            foreach (var imgSyntax in imgSyntaxes.Where(i => i != null))
            {
                var imgRelativePath = imgSyntax.Groups[1].Value;
                var imgAbsolutePath = CombineBlobPaths(Path.GetDirectoryName(md.Path), imgRelativePath);

                if (!request.ImagePaths.Contains(imgAbsolutePath))
                {
                    validationErrors.Add($"Image validation error. File \"{md.Path}\" has a link to non-existent image \"{imgAbsolutePath}\"");
                    continue;
                }

                md.ImagePaths[imgRelativePath] = imgAbsolutePath;
            }
        }

        return new MapImagePathsResponse
        {
            Mds = request.Mds,
            ValidationErrors = validationErrors
        };
    }

    private string CombineBlobPaths(string basePath, string relativePath)
    {
        var baseParts = basePath.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        var relativeParts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();

        foreach (var part in relativeParts)
        {
            if (part == ".") continue;
            else if (part == ".." && baseParts.Count > 0) baseParts.RemoveAt(baseParts.Count - 1);
            else baseParts.Add(part);
        }
        return string.Join("/", baseParts);
    }
}