using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MDsToPDFConverter.Function;

public class TestAPI
{
    private readonly ILogger<TestAPI> _logger;

    public TestAPI(ILogger<TestAPI> logger)
    {
        _logger = logger;
    }

    [Function("TestAPI")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}