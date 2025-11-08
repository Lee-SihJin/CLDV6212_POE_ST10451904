using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;

namespace ABCRetailers.Functions
{
    public class StorageInitializationFunction
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly QueueServiceClient _queueServiceClient;
        private readonly ShareServiceClient _shareServiceClient;
        private readonly ILogger<StorageInitializationFunction> _logger;

        public StorageInitializationFunction(
            TableServiceClient tableServiceClient,
            BlobServiceClient blobServiceClient,
            QueueServiceClient queueServiceClient,
            ShareServiceClient shareServiceClient,
            ILogger<StorageInitializationFunction> logger)
        {
            _tableServiceClient = tableServiceClient;
            _blobServiceClient = blobServiceClient;
            _queueServiceClient = queueServiceClient;
            _shareServiceClient = shareServiceClient;
            _logger = logger;
        }

        [Function("InitializeStorage")]
        public async Task<IActionResult> InitializeStorage(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "storage/initialize")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("Starting Azure storage initialization...");

                // Create Tables
                await _tableServiceClient.CreateTableIfNotExistsAsync("Customers");
                await _tableServiceClient.CreateTableIfNotExistsAsync("Products");
                await _tableServiceClient.CreateTableIfNotExistsAsync("Orders");
                _logger.LogInformation("Tables created successfully");

                // Create Blob Containers
                var productImagesContainer = _blobServiceClient.GetBlobContainerClient("product-images");
                await productImagesContainer.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

                var paymentProofsContainer = _blobServiceClient.GetBlobContainerClient("payment-proofs");
                await paymentProofsContainer.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.None);

                _logger.LogInformation("Blob containers created successfully");

                // Create Queues
                var orderQueue = _queueServiceClient.GetQueueClient("order-notifications");
                await orderQueue.CreateIfNotExistsAsync();

                var stockQueue = _queueServiceClient.GetQueueClient("stock-updates");
                await stockQueue.CreateIfNotExistsAsync();

                _logger.LogInformation("Queues created successfully");

                // Create File Share
                var contractsShare = _shareServiceClient.GetShareClient("contracts");
                await contractsShare.CreateIfNotExistsAsync();

                // Create directory in file share
                var contractsDirectory = contractsShare.GetDirectoryClient("payments");
                await contractsDirectory.CreateIfNotExistsAsync();

                _logger.LogInformation("File shares created successfully");
                _logger.LogInformation("Azure storage initialization completed successfully");

                return new OkObjectResult(new { Message = "Storage initialized successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure storage: {Message}", ex.Message);
                return new StatusCodeResult(500);
            }
        }

        [Function("CheckStorageHealth")]
        public async Task<IActionResult> CheckStorageHealth(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "storage/health")] HttpRequest req)
        {
            try
            {
                var healthStatus = new
                {
                    Tables = await CheckTablesHealthAsync(),
                    Blobs = await CheckBlobsHealthAsync(),
                    Queues = await CheckQueuesHealthAsync(),
                    FileShares = await CheckFileSharesHealthAsync(),
                    Timestamp = DateTime.UtcNow
                };

                return new OkObjectResult(healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking storage health");
                return new StatusCodeResult(500);
            }
        }

        private async Task<Dictionary<string, bool>> CheckTablesHealthAsync()
        {
            var tables = new[] { "Customers", "Products", "Orders" };
            var results = new Dictionary<string, bool>();

            foreach (var tableName in tables)
            {
                try
                {
                    var tableClient = _tableServiceClient.GetTableClient(tableName);
                    await tableClient.CreateIfNotExistsAsync();
                    results[tableName] = true;
                }
                catch
                {
                    results[tableName] = false;
                }
            }

            return results;
        }

        private async Task<Dictionary<string, bool>> CheckBlobsHealthAsync()
        {
            var containers = new[] { "product-images", "payment-proofs" };
            var results = new Dictionary<string, bool>();

            foreach (var containerName in containers)
            {
                try
                {
                    var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                    await containerClient.CreateIfNotExistsAsync();
                    results[containerName] = true;
                }
                catch
                {
                    results[containerName] = false;
                }
            }

            return results;
        }

        private async Task<Dictionary<string, bool>> CheckQueuesHealthAsync()
        {
            var queues = new[] { "order-notifications", "stock-updates" };
            var results = new Dictionary<string, bool>();

            foreach (var queueName in queues)
            {
                try
                {
                    var queueClient = _queueServiceClient.GetQueueClient(queueName);
                    await queueClient.CreateIfNotExistsAsync();
                    results[queueName] = true;
                }
                catch
                {
                    results[queueName] = false;
                }
            }

            return results;
        }

        private async Task<Dictionary<string, bool>> CheckFileSharesHealthAsync()
        {
            var shares = new[] { "contracts" };
            var results = new Dictionary<string, bool>();

            foreach (var shareName in shares)
            {
                try
                {
                    var shareClient = _shareServiceClient.GetShareClient(shareName);
                    await shareClient.CreateIfNotExistsAsync();
                    results[shareName] = true;
                }
                catch
                {
                    results[shareName] = false;
                }
            }

            return results;
        }
    }
}