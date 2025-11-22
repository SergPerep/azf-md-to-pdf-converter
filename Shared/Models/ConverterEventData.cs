namespace Shared.Models;

public class ConverterEventData
{
    public ConverterEventDataStatus Status { get; set; }
    public string OrchInstanceId { get; set; }
    public string ErrorMessage { get; set; }
}

public enum ConverterEventDataStatus
{
    Completed,
    Failed
}
