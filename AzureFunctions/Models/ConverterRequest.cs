namespace Md2PDFConverter.Models;

public class ConverterRequest
{
    public Guid RunId { get; set; }
    public string InputFolderPath { get; set; }
    public string OutputFolderPath { get; set; }
    public string OutputFileName { get; set; }
    public string OrchInstanceId { get; set; }
}