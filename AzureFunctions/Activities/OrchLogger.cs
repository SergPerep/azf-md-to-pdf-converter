using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Md2PDFConverter.Activities;

public class OrchLogger()
{
    [Function(nameof(OrchLogger))]
    public void Run([ActivityTrigger] string message, FunctionContext context)
    {
        var logger = context.GetLogger(nameof(OrchLogger));
        logger.LogInformation(message);
    }
}