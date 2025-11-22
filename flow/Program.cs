using System.Text;
using System.Text.RegularExpressions;
using Models;

string inputFolderPath = "/home/sergei/Documents/Repos/azf-md-to-pdf-converter/flow/input";

var (mds, imgFilePaths) = GetMdsAndImagePathsFromDirectory(inputFolderPath);

Validator.ValidateMdImagePaths(mds, imgFilePaths);

mds = SortMds(mds);

MoveImages("/home/sergei/Documents/Repos/azf-md-to-pdf-converter/flow/_temp/img", mds);
CombineMds(mds, "/home/sergei/Documents/Repos/azf-md-to-pdf-converter/flow/_temp/index.md");

List<Md> SortMds(List<Md> mds)
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

    Console.WriteLine("Sorted mds: ");
    mds.ForEach(md => Console.WriteLine(md.Path));
    return mds;
}

static void MoveImages(string destImageFolderPath, List<Md> mds)
{
    if (Directory.Exists(destImageFolderPath))
    {
        Directory.Delete(destImageFolderPath, true);
    }
    Directory.CreateDirectory(destImageFolderPath);

    foreach (var md in mds)
    {
        foreach (var imagePathPair in md.ImagePaths)
        {
            var destImageFilePath = destImageFolderPath + "/" + Guid.NewGuid() + Path.GetExtension(imagePathPair.Value);
            File.Copy(imagePathPair.Value, destImageFilePath);
            md.ImagePaths[imagePathPair.Key] = destImageFilePath;
        }
    }
}

static void CombineMds(List<Md> mds, string destFilePath)
{
    if (Path.Exists(destFilePath))
    {
        File.Delete(destFilePath);
    }

    File.Copy(mds[0].Path, destFilePath);

    using var writer = new StreamWriter(destFilePath, append: true);

    foreach (var md in mds.TakeLast(mds.Count - 1))
    {
        Console.WriteLine($"Import {md.Path}");
        var lines = File.ReadAllLines(md.Path);

        // Downgrade headers
        var headerRegex = new Regex(@"^#{1,6} .{1,}", RegexOptions.Compiled);
        // Console.WriteLine("  - Downgrade headers");
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!headerRegex.IsMatch(line)) continue;
            var headerLevel = line.Count(ch => ch == '#');
            var headerText = line.Replace("#", "").Trim();
            var downgradeLevel = Path.GetFileName(md.Path) == "index.md" ? md.NestingLevel : md.NestingLevel + 1;
            var targetHeaderLevel = headerLevel + downgradeLevel;
            targetHeaderLevel = targetHeaderLevel > 6 ? 6 : targetHeaderLevel;
            var updatedLine = $"{new string('#', targetHeaderLevel)} {headerText}";
            Console.WriteLine($"Downgrade header lvl. {downgradeLevel}: \"{line}\" -> \"{updatedLine}\"");
            lines[i] = $"{new string('#', targetHeaderLevel)} {headerText}";
        }

        // Resolve image paths
        // Console.WriteLine("  - Resolve image paths");
        var content = string.Join(Environment.NewLine, lines);
        foreach (var imagePathPair in md.ImagePaths)
        {
            var relativeImagePath = Path.GetRelativePath(Path.GetDirectoryName(destFilePath), imagePathPair.Value);
            content = content.Replace(imagePathPair.Key, relativeImagePath);
            // Console.WriteLine($"    - \"{imagePathPair.Key}\" -> \"{relativeImagePath}\"");
        }

        writer.Write(Environment.NewLine + Environment.NewLine + content);
    }
}

static (List<Md>, List<string>) GetMdsAndImagePathsFromDirectory(string folderPath)
{
    string[] filePaths = Directory.GetFiles(folderPath, "", SearchOption.AllDirectories);
    List<Md> mds = [];
    List<string> imgFilePaths = [];
    string[] imgExtensions = [".png", ".img", ".gif", ".jpg", ".jpeg"];
    foreach (var filePath in filePaths)
    {
        string ext = Path.GetExtension(filePath).ToLower();
        if (ext == ".md")
        {
            mds.Add(new Md(filePath, folderPath));
            continue;
        }
        if (imgExtensions.Any(e => e == ext))
        {
            imgFilePaths.Add(filePath);
            // Console.WriteLine("Image: " + filePath);
        }
    }
    return (mds, imgFilePaths);
}