namespace Models
{
    public class Md
{
    public string Path { get; set; }
    public Dictionary<string, string> ImagePaths { get; set; } = [];
    public int NestingLevel { get; set; }

    public Md(string mdPath, string rootFolderPath)
    {
        Path = mdPath;
        NestingLevel = GetNestingLevel(mdPath, rootFolderPath);
    }
    static private int GetNestingLevel(string mdPath, string rootFolderPath)
    {
        var partToRemove = rootFolderPath.Last() == '/' ? rootFolderPath : rootFolderPath + "/";
        var nestingLevel = mdPath.Replace(partToRemove, "").Split("/").Count() - 1;
        return nestingLevel;
    }
}
}