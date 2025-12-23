using System.Text;
using System.Text.RegularExpressions;
using Md2PDFConverter.Models;
using Md2PDFConverter.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Md2PDFConverter.Activities;

public class CombineMds(IStorageService storageService)
{
    private readonly Regex headerRegex = new Regex(@"^#{1,6} .{1,}", RegexOptions.Compiled);
    [Function(nameof(CombineMds))]
    public async Task<CombineMdsResponse> Run([ActivityTrigger] CombineMdsRequest request, FunctionContext context)
    {
        var logger = context.GetLogger(nameof(CombineMds));
        await storageService.DeleteBlobIfExistsAsync(request.DestFilePath);
        var finalContent = await storageService.ReadMdAsTextAsync(request.Mds[0].Path);
        var builder = new StringBuilder(finalContent);

        foreach (var md in request.Mds.TakeLast(request.Mds.Count - 1))
        {
            // Console.WriteLine($"Import {md.Path}");
            var content = await storageService.ReadMdAsTextAsync(md.Path);
            var lines = content.Split(["\r\n", "\n"], StringSplitOptions.None);

            // Downgrade headers
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                logger.LogTrace($"Line {i}: {line}");
                if (!headerRegex.IsMatch(line)) continue;
                var headerLevel = line.Count(ch => ch == '#');
                var headerText = line.Replace("#", "").Trim();
                var downgradeLevel = Path.GetFileName(md.Path) == "index.md" ? md.NestingLevel : md.NestingLevel + 1;
                var targetHeaderLevel = headerLevel + downgradeLevel;
                targetHeaderLevel = targetHeaderLevel > 6 ? 6 : targetHeaderLevel;
                var updatedLine = $"{new string('#', targetHeaderLevel)} {headerText}";
                logger.LogInformation($"Downgrade header lvl. {downgradeLevel}: \"{line}\" -> \"{updatedLine}\"");
                lines[i] = $"{new string('#', targetHeaderLevel)} {headerText}";
            }

            // Resolve image paths
            content = string.Join(Environment.NewLine, lines);
            foreach (var imagePathPair in md.ImagePaths)
            {
                var relativeImagePath = GetRelativePath(Path.GetDirectoryName(request.DestFilePath), imagePathPair.Value);
                content = content.Replace(imagePathPair.Key, relativeImagePath);
            }

            builder.Append(Environment.NewLine + Environment.NewLine + content);
        }

        await storageService.UploadBlobFromTextAsync(request.DestFilePath, builder.ToString());

        return new CombineMdsResponse
        {
            MdFilePath = request.DestFilePath
        };
    }

    private string GetRelativePath(string relativeTo, string path)
    {
        var relativeToParts = relativeTo.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        foreach (var part in relativeToParts)
        {
            if (part == pathParts[0])
            {
                pathParts.RemoveAt(0);
            }
        }
        return "./" + string.Join("/", pathParts);
    }
}