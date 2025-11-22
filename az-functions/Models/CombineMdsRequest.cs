namespace Md2PDFConverter.Models;

public class CombineMdsRequest
{
    public List<Md> Mds { get; set; }
    public string DestFilePath { get; set; }
}