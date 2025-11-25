namespace Md2PDFConverter.Models;

public class MoveImagesRequest
{
    public string DestFolderPath { get; set; }
    public List<Md> Mds { get; set; }
}