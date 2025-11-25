namespace Md2PDFConverter.Models;

public class ScanFilesResponse
{
    public List<Md> Mds { get; set; }
    public List<string> ImageFilePaths { get; set; }
}