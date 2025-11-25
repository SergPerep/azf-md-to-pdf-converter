using Azure;
using Md2PDFConverter.Models;
using Md2PDFConverter.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Md2PDFConverter.Activities;

public class ScanFiles(IStorageService storageService)
{
    [Function(nameof(ScanFiles))]
    public async Task<ScanFilesResponse> Run([ActivityTrigger] ScanFilesRequest request, FunctionContext context)
    {
        var logger = context.GetLogger(nameof(ScanFiles));
        var filePaths = await storageService.ListAllFilesAsync(request.FolderPath);
        List<Md> mds = [];
        List<string> imgFilePaths = [];
        string[] imgExtensions = [".png", ".img", ".gif", ".jpg", ".jpeg"];

        foreach (var filePath in filePaths)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".md")
            {
                mds.Add(new Md(filePath, request.FolderPath));
                continue;
            }
            if (imgExtensions.Any(e => e == ext))
            {
                imgFilePaths.Add(filePath);
                logger.LogInformation($"Image: {filePath}");
            }
        }

        mds = SortMds(mds);
        return new ScanFilesResponse()
        {
            Mds = mds,
            ImageFilePaths = imgFilePaths
        };
    }

    private List<Md> SortMds(List<Md> mds)
    {
        // Alphabetically sort
        mds = mds.OrderBy(md => md.Path).ToList();

        // Find root (the shortest path)
        var selectedMd = mds[0];
        var selectedMdNestingCount = selectedMd.Path.Count(ch => ch == '/');
        foreach (var md in mds)
        {
            var mdNestingCount = md.Path.Count(ch => ch == '/');
            if (selectedMdNestingCount > mdNestingCount)
            {
                selectedMd = md;
            }
        }

        var pathArray = selectedMd.Path.Split("/");

        var rootFolderPath = string.Join("/", pathArray.Take(pathArray.Length - 1));

        // Move root index.md on top

        var rootIndexMd = mds.First(md => md.Path == rootFolderPath + "/index.md");

        mds.Remove(rootIndexMd);
        mds.Insert(0, rootIndexMd);

        // Find other index.md

        List<Md> otherIndexMds = [];

        foreach (var md in mds)
        {
            if (md == rootIndexMd) continue;
            if (md.Path.EndsWith("index.md"))
            {
                otherIndexMds.Add(md);
            }
        }
        // Put index.md before other paths of the same directories

        foreach (var indexMd in otherIndexMds)
        {
            var index = mds.IndexOf(indexMd);
            Md? topMd = null;
            var topMdIndex = 0;
            for (int i = 0; i < index; i++)
            {
                if (mds[i].Path.StartsWith(Path.GetDirectoryName(indexMd.Path)))
                {
                    topMd = mds[i];
                    topMdIndex = i;
                    break;
                }
            }
            if (topMd == null) continue;

            mds.Remove(indexMd);
            mds.Insert(topMdIndex, indexMd);
        }

        return mds;
    }
}
