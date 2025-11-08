using System.Text.Json;
using ABCRetailers.Models;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Functions
{
    public class TableStorageFunctions
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly ILogger<TableStorageFunctions> _logger;

        public TableStorageFunctions(TableServiceClient tableServiceClient, ILogger<TableStorageFunctions> logger)
        {
            _tableServiceClient = tableServiceClient;
            _logger = logger;
        }

        [Function("GetAllEntities")]
        public async Task<IActionResult> GetAllEntities(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "table/{tableName}")] HttpRequest req,
            string tableName)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                var entities = new List<Dictionary<string, object>>();

                await foreach (var entity in tableClient.QueryAsync<TableEntity>())
                {
                    entities.Add(entity.ToDictionary());
                }

                return new OkObjectResult(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entities from table {TableName}", tableName);
                return new StatusCodeResult(500);
            }
        }

        [Function("GetEntity")]
        public async Task<IActionResult> GetEntity(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "table/{tableName}/{partitionKey}/{rowKey}")] HttpRequest req,
            string tableName, string partitionKey, string rowKey)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                var response = await tableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey);
                return new OkObjectResult(response.Value.ToDictionary());
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                return new NotFoundResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entity from table {TableName}", tableName);
                return new StatusCodeResult(500);
            }
        }

        [Function("AddEntity")]
        public async Task<IActionResult> AddEntity(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "table/{tableName}")] HttpRequest req,
    string tableName)
        {
            try
            {
                var request = await JsonSerializer.DeserializeAsync<StorageRequest>(req.Body);
                if (request?.EntityData == null)
                    return new BadRequestObjectResult("Entity data is required");

                // Deserialize as JsonElement first to handle the data properly
                using JsonDocument doc = JsonDocument.Parse(request.EntityData);
                JsonElement root = doc.RootElement;

                // Create a new TableEntity and manually add properties
                var entity = new TableEntity();

                foreach (var property in root.EnumerateObject())
                {
                    // Handle different JSON value types appropriately
                    switch (property.Value.ValueKind)
                    {
                        case JsonValueKind.String:
                            entity[property.Name] = property.Value.GetString();
                            break;
                        case JsonValueKind.Number:
                            if (property.Value.TryGetInt32(out int intValue))
                                entity[property.Name] = intValue;
                            else if (property.Value.TryGetInt64(out long longValue))
                                entity[property.Name] = longValue;
                            else if (property.Value.TryGetDouble(out double doubleValue))
                                entity[property.Name] = doubleValue;
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            entity[property.Name] = property.Value.GetBoolean();
                            break;
                        case JsonValueKind.Null:
                            entity[property.Name] = null;
                            break;
                        default:
                            // For complex types, store as string
                            entity[property.Name] = property.Value.ToString();
                            break;
                    }
                }

                // Ensure PartitionKey and RowKey are present and are strings
                if (!entity.ContainsKey("PartitionKey") || string.IsNullOrEmpty(entity["PartitionKey"]?.ToString()))
                    return new BadRequestObjectResult("PartitionKey is required");

                if (!entity.ContainsKey("RowKey") || string.IsNullOrEmpty(entity["RowKey"]?.ToString()))
                    return new BadRequestObjectResult("RowKey is required");

                var tableClient = _tableServiceClient.GetTableClient(tableName);
                await tableClient.AddEntityAsync(entity);

                return new OkObjectResult(entity.ToDictionary());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding entity to table {TableName}", tableName);
                return new StatusCodeResult(500);
            }
        }
        [Function("UpdateEntity")]
        public async Task<IActionResult> UpdateEntity(
      [HttpTrigger(AuthorizationLevel.Function, "put", Route = "table/{tableName}")] HttpRequest req,
      string tableName)
        {
            try
            {
                var request = await JsonSerializer.DeserializeAsync<StorageRequest>(req.Body);
                if (request?.EntityData == null)
                    return new BadRequestObjectResult("Entity data is required");

                // Deserialize as JsonElement first to handle the data properly
                using JsonDocument doc = JsonDocument.Parse(request.EntityData);
                JsonElement root = doc.RootElement;

                // Get PartitionKey and RowKey first
                if (!root.TryGetProperty("PartitionKey", out JsonElement partitionKeyElement) ||
                    !root.TryGetProperty("RowKey", out JsonElement rowKeyElement))
                {
                    return new BadRequestObjectResult("PartitionKey and RowKey are required");
                }

                string partitionKey = partitionKeyElement.GetString();
                string rowKey = rowKeyElement.GetString();

                if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                    return new BadRequestObjectResult("PartitionKey and RowKey are required");

                var tableClient = _tableServiceClient.GetTableClient(tableName);

                // Get the existing entity from the database
                var existingEntity = await tableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey);
                if (existingEntity == null)
                {
                    return new NotFoundObjectResult("Entity not found");
                }

                // Update only the properties that are provided in the request
                foreach (var property in root.EnumerateObject())
                {
                    // Skip PartitionKey and RowKey as they cannot be updated
                    if (property.Name == "PartitionKey" || property.Name == "RowKey")
                        continue;

                    // Handle different JSON value types appropriately
                    switch (property.Value.ValueKind)
                    {
                        case JsonValueKind.String:
                            existingEntity.Value[property.Name] = property.Value.GetString();
                            break;
                        case JsonValueKind.Number:
                            if (property.Value.TryGetInt32(out int intValue))
                                existingEntity.Value[property.Name] = intValue;
                            else if (property.Value.TryGetInt64(out long longValue))
                                existingEntity.Value[property.Name] = longValue;
                            else if (property.Value.TryGetDouble(out double doubleValue))
                                existingEntity.Value[property.Name] = doubleValue;
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            existingEntity.Value[property.Name] = property.Value.GetBoolean();
                            break;
                        case JsonValueKind.Null:
                            existingEntity.Value[property.Name] = null;
                            break;
                        default:
                            // For complex types, store as string
                            existingEntity.Value[property.Name] = property.Value.ToString();
                            break;
                    }
                }

                // Update the entity with merge (only updates provided properties)
                await tableClient.UpdateEntityAsync(existingEntity.Value, existingEntity.Value.ETag, TableUpdateMode.Merge);

                return new OkObjectResult(existingEntity.Value.ToDictionary());
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                return new NotFoundObjectResult("Entity not found");
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 412)
            {
                return new ConflictObjectResult("ETag mismatch - entity was modified by another process");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating entity in table {TableName}", tableName);
                return new StatusCodeResult(500);
            }
        }

        [Function("DeleteEntity")]
        public async Task<IActionResult> DeleteEntity(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "table/{tableName}/{partitionKey}/{rowKey}")] HttpRequest req,
            string tableName, string partitionKey, string rowKey)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                await tableClient.DeleteEntityAsync(partitionKey, rowKey);
                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting entity from table {TableName}", tableName);
                return new StatusCodeResult(500);
            }
        }
    }
}