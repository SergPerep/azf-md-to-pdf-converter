namespace Md2PDFConverter.Models;

public class ConverterRequest
{
    public string InputFolderPath { get; set; }
    public string OutputFolderPath { get; set; }
    public string OutputFileName { get; set; }
}