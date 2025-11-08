using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ABCRetailersFunction;

public class OrdersEnqueueFunctions
{
    private readonly ILogger<OrdersEnqueueFunctions> _logger;

    public OrdersEnqueueFunctions(ILogger<OrdersEnqueueFunctions> logger)
    {
        _logger = logger;
    }

    [Function("OrdersEnqueueFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders/enqueue")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request to enqueue order.");

        var body = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteStringAsync("Request body required.");
            return badRequestResponse;
        }

        var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var queueName = Environment.GetEnvironmentVariable("OrdersQueueName") ?? "input-queue";
        var queueClient = new QueueClient(conn, queueName);
        await queueClient.CreateIfNotExistsAsync();

        // Expect incoming body to be JSON of OrderMessage
        await queueClient.SendMessageAsync(body);

        _logger.LogInformation("Order message enqueued.");

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { result = "enqueued" });
        return response;
    }
}