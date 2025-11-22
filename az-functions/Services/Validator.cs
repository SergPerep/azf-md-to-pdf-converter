using System.Text.RegularExpressions;
using Md2PDFConverter.Models;

namespace Md2PDFConverter.Services;

public class Validator
{
    public static void ValidateMdImagePaths(List<Md> mds, List<string> imgFilePaths, List<string> validationErrors)
    {
        // Validate image paths in .md and extract them as relative paths
        Regex imageLineRegex = new Regex(@"!\[[^\]]*\]\(((?>[^()]+|\((?<open>)|\)(?<-open>))*(?(open)(?!)))\)", RegexOptions.Compiled);
        foreach (var md in mds)
        {
            var content = File.ReadAllText(md.Path);
            var imgSyntaxes = imageLineRegex.Matches(content);
            if (imgSyntaxes == null) continue;
            foreach (var imgSyntax in imgSyntaxes.Where(i => i != null))
            {
                var imgRelativePath = imgSyntax.Groups[1].Value;
                var imgAbsolutePath = Path.GetFullPath(imgRelativePath, Path.GetDirectoryName(md.Path));

                if (!imgFilePaths.Contains(imgAbsolutePath))
                {
                    validationErrors.Add($"Image validation error. File \"{md.Path}\" has a link to non-existent image \"{imgAbsolutePath}\"");
                    continue;
                }

                md.ImagePaths[imgRelativePath] = imgAbsolutePath;
            }
        }
    }
    public static void ValidateMdHeader(string mdFilePath, List<string> validationErrors)
    {
        var headerRegex = new Regex(@"^# .{1,}", RegexOptions.Compiled);
        foreach (var line in File.ReadAllLines(mdFilePath))
        {
            if (headerRegex.IsMatch(line.Trim()))
            {
                return;
            }
        }
        validationErrors.Add($"{mdFilePath} must contain h1");
    }

    public static void ValidateFolderName(string dirName, List<string> validationErrors)
    {
        if (dirName!.Length >= 2)
        {
            validationErrors.Add($"Folder name must be longer than 2 charachters. Istead got \"{dirName}\"");
            return;
        }
        var countString = dirName.Substring(0, 2);

        if (!Int32.TryParse(countString, out int _))
        {
            validationErrors.Add($"Folder name must start with two digits followed by _. Instead got \"{dirName}\"");
            return;
        }

        var separator = dirName!.Length > 2 ? dirName.Substring(2, 1) : "";
        if (string.IsNullOrWhiteSpace(separator))
        {
            validationErrors.Add($"Folder name must start with two digits followed by _. Instead got \"{dirName}\"");
        }
    }

    public static void ValidateMdFileName(string filePath, List<string> validationErrors)
    {
        Regex mdFileNameRegex = new Regex(@"(^\d{2}_.*\.md$)|(^index\.md$)", RegexOptions.Compiled);
        if (!mdFileNameRegex.IsMatch(Path.GetFileName(filePath)))
        {
            validationErrors.Add($"Wrong naming convention: {filePath}");
        }
    }

    // Validate that each md containing folder has at least one index.md
    public static void ValidateFolderContent(string folderPath, List<string> validationErrors)
    {
        var mdFilePaths = Directory.GetFiles(folderPath).Where(filePath => Path.GetExtension(filePath) == ".md").ToArray();
        if (!mdFilePaths.Any()) return;
        if (!mdFilePaths.Any(filePath => Path.GetFileName(filePath) == "index.md"))
        {
            validationErrors.Add($"The folder {folderPath} must not contain index.md");
        }
    }
}

