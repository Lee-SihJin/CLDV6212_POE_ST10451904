using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ABCRetailersFunction.Models;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ABCRetailersFunction;

public class OrdersQueueProcessorFunction
{
    private readonly ILogger<OrdersQueueProcessorFunction> _logger;

    public OrdersQueueProcessorFunction(ILogger<OrdersQueueProcessorFunction> logger)
    {
        _logger = logger;
    }

    [Function("OrdersQueueProcessorFunction")]
    public async Task Run(
        [QueueTrigger("input-queue", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        _logger.LogInformation("Queue trigger fired. Processing message: {Message}", queueMessage);

        try
        {
            // Validate and deserialize message
            if (string.IsNullOrWhiteSpace(queueMessage))
            {
                _logger.LogError("Empty queue message received");
                return; 
            }

            _logger.LogInformation("Deserializing message...");
            var order = JsonSerializer.Deserialize<OrderMessage>(queueMessage, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (order == null)
            {
                _logger.LogError("Failed to deserialize message: {Message}", queueMessage);
                return; 
            }

            _logger.LogInformation("Successfully deserialized order: {OrderId}", order.OrderId);

            // Validate required fields
            if (string.IsNullOrEmpty(order.OrderId) || string.IsNullOrEmpty(order.CustomerId))
            {
                _logger.LogError("Missing required fields. OrderId: {OrderId}, CustomerId: {CustomerId}",
                    order.OrderId, order.CustomerId);
                return; 
            }

            // Setup table storage
            var tableConn = Environment.GetEnvironmentVariable("TableStorageConnectionString") ?? "UseDevelopmentStorage=true";
            _logger.LogInformation("Using table connection: {Connection}", tableConn);

            var tableClient = new TableClient(tableConn, "Orders");

            _logger.LogInformation("Ensuring table exists...");
            await tableClient.CreateIfNotExistsAsync();
            _logger.LogInformation("Table ready");

            var partitionKey = order.CustomerId;
            var rowKey = order.OrderId;

            _logger.LogInformation("Processing Order - Partition: {PartitionKey}, Row: {RowKey}", partitionKey, rowKey);

            // Process based on action
            var action = order.Action?.ToLowerInvariant();

            if (action == "create-order")
            {
                _logger.LogInformation("Creating new order...");

                var entity = new TableEntity(partitionKey, rowKey)
                {
                    ["OrderId"] = order.OrderId,
                    ["CustomerId"] = order.CustomerId,
                    ["ProductId"] = order.ProductId,
                    ["Status"] = order.Status,
                    ["TotalPrice"] = order.TotalPrice,
                    ["OrderDate"] = order.OrderDate,
                    ["Quantity"] = order.Quantity
                };

                await tableClient.AddEntityAsync(entity);
                _logger.LogInformation("Order {OrderId} successfully created for customer {CustomerId}",
                    order.OrderId, order.CustomerId);
            }
            else if (action == "status-update")
            {
                _logger.LogInformation("Updating order status...");

                try
                {
                    var response = await tableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey);
                    var entity = response.Value;
                    entity["Status"] = order.Status;

                    await tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
                    _logger.LogInformation("Order {OrderId} status updated to {Status}",
                        order.OrderId, order.Status);
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    _logger.LogWarning("Order {OrderId} not found, creating new record", order.OrderId);

                    var fallback = new TableEntity(partitionKey, rowKey)
                    {
                        ["OrderId"] = order.OrderId,
                        ["CustomerId"] = order.CustomerId,
                        ["ProductId"] = order.ProductId,
                        ["Status"] = order.Status,
                        ["TotalPrice"] = order.TotalPrice,
                        ["OrderDate"] = order.OrderDate,
                        ["Quantity"] = order.Quantity
                    };

                    await tableClient.AddEntityAsync(fallback);
                    _logger.LogInformation("Created missing order {OrderId} with status {Status}",
                        order.OrderId, order.Status);
                }
            }
            else
            {
                _logger.LogWarning("Unknown action type: {Action} for order {OrderId}",
                    order.Action, order.OrderId);
            }

            _logger.LogInformation("Order processing completed successfully");
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON deserialization failed for message: {Message}", queueMessage);
            return; 
        }
        catch (RequestFailedException storageEx)
        {
            _logger.LogError(storageEx, "Storage operation failed for message: {Message}", queueMessage);
            throw; 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing message: {Message}", queueMessage);
            throw; 
        }

    }
}