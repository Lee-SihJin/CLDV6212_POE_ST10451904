using System.Text.Json;
using ABCRetailers.Models;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Functions
{
    public class QueueStorageFunctions
    {
        private readonly QueueServiceClient _queueServiceClient;
        private readonly ILogger<QueueStorageFunctions> _logger;

        public QueueStorageFunctions(QueueServiceClient queueServiceClient, ILogger<QueueStorageFunctions> logger)
        {
            _queueServiceClient = queueServiceClient;
            _logger = logger;
        }

        [Function("SendMessage")]
        public async Task<IActionResult> SendMessage(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "queue/{queueName}")] HttpRequest req,
            string queueName)
        {
            try
            {
                var request = await JsonSerializer.DeserializeAsync<StorageRequest>(req.Body);
                if (string.IsNullOrEmpty(request?.Message))
                    return new BadRequestObjectResult("Message is required");

                var queueClient = _queueServiceClient.GetQueueClient(queueName);
                await queueClient.CreateIfNotExistsAsync();
                await queueClient.SendMessageAsync(request.Message);

                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to queue {QueueName}", queueName);
                return new StatusCodeResult(500);
            }
        }

        [Function("ReceiveMessage")]
        public async Task<IActionResult> ReceiveMessage(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "queue/{queueName}")] HttpRequest req,
            string queueName)
        {
            try
            {
                var queueClient = _queueServiceClient.GetQueueClient(queueName);
                await queueClient.CreateIfNotExistsAsync();

                var response = await queueClient.ReceiveMessageAsync();
                if (response.Value != null)
                {
                    await queueClient.DeleteMessageAsync(response.Value.MessageId, response.Value.PopReceipt);
                    return new OkObjectResult(new { Message = response.Value.MessageText });
                }

                return new NoContentResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving message from queue {QueueName}", queueName);
                return new StatusCodeResult(500);
            }
        }
    }
}