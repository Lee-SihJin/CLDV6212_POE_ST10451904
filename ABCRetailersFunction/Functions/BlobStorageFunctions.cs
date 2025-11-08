// Functions/BlobStorageFunctions.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;

namespace ABCRetailers.Functions
{
    public class BlobStorageFunctions
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<BlobStorageFunctions> _logger;

        public BlobStorageFunctions(BlobServiceClient blobServiceClient, ILogger<BlobStorageFunctions> logger)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        [Function("UploadBlob")]
        public async Task<IActionResult> UploadBlob(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blob/{containerName}")] HttpRequest req,
            string containerName)
        {
            try
            {
                if (!req.Form.Files.Any())
                    return new BadRequestObjectResult("No file uploaded");

                var file = req.Form.Files[0];
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var blobClient = containerClient.GetBlobClient(fileName);

                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, overwrite: true);

                return new OkObjectResult(new { Url = blobClient.Uri.ToString(), FileName = fileName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading blob to container {ContainerName}", containerName);
                return new StatusCodeResult(500);
            }
        }

        [Function("DeleteBlob")]
        public async Task<IActionResult> DeleteBlob(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "blob/{containerName}/{blobName}")] HttpRequest req,
            string containerName, string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync();

                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blob from container {ContainerName}", containerName);
                return new StatusCodeResult(500);
            }
        }
    }
}