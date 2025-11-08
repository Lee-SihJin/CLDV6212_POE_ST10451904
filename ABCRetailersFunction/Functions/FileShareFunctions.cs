using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Azure.Storage.Files.Shares;
using ABCRetailers.Models;

namespace ABCRetailers.Functions
{
    public class FileShareFunctions
    {
        private readonly ShareServiceClient _shareServiceClient;
        private readonly ILogger<FileShareFunctions> _logger;

        public FileShareFunctions(ShareServiceClient shareServiceClient, ILogger<FileShareFunctions> logger)
        {
            _shareServiceClient = shareServiceClient;
            _logger = logger;
        }

        [Function("UploadToFileShare")]
        public async Task<IActionResult> UploadToFileShare(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "fileshare/{shareName}")] HttpRequest req,
            string shareName)
        {
            try
            {
                var directoryName = req.Query["directoryName"].ToString();

                if (!req.Form.Files.Any())
                    return new BadRequestObjectResult("No file uploaded");

                var file = req.Form.Files[0];
                var shareClient = _shareServiceClient.GetShareClient(shareName);
                var directoryClient = string.IsNullOrEmpty(directoryName)
                    ? shareClient.GetRootDirectoryClient()
                    : shareClient.GetDirectoryClient(directoryName);

                await directoryClient.CreateIfNotExistsAsync();

                var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{file.FileName}";
                var fileClient = directoryClient.GetFileClient(fileName);

                using var stream = file.OpenReadStream();
                await fileClient.CreateAsync(stream.Length);
                await fileClient.UploadAsync(stream);

                return new OkObjectResult(new { FileName = fileName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading to file share {ShareName}", shareName);
                return new StatusCodeResult(500);
            }
        }

        [Function("DownloadFromFileShare")]
        public async Task<IActionResult> DownloadFromFileShare(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "fileshare/{shareName}/{fileName}")] HttpRequest req,
            string shareName, string fileName)
        {
            try
            {
                var directoryName = req.Query["directoryName"].ToString();

                var shareClient = _shareServiceClient.GetShareClient(shareName);
                var directoryClient = string.IsNullOrEmpty(directoryName)
                    ? shareClient.GetRootDirectoryClient()
                    : shareClient.GetDirectoryClient(directoryName);

                var fileClient = directoryClient.GetFileClient(fileName);

                if (!await fileClient.ExistsAsync())
                    return new NotFoundResult();

                var response = await fileClient.DownloadAsync();
                var memoryStream = new MemoryStream();
                await response.Value.Content.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                return new FileContentResult(fileBytes, "application/octet-stream")
                {
                    FileDownloadName = fileName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading from file share {ShareName}", shareName);
                return new StatusCodeResult(500);
            }
        }

        [Function("DeleteFromFileShare")]
        public async Task<IActionResult> DeleteFromFileShare(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "fileshare/{shareName}/{fileName}")] HttpRequest req,
            string shareName, string fileName)
        {
            try
            {
                var directoryName = req.Query["directoryName"].ToString();

                var shareClient = _shareServiceClient.GetShareClient(shareName);
                var directoryClient = string.IsNullOrEmpty(directoryName)
                    ? shareClient.GetRootDirectoryClient()
                    : shareClient.GetDirectoryClient(directoryName);

                var fileClient = directoryClient.GetFileClient(fileName);
                await fileClient.DeleteIfExistsAsync();

                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting from file share {ShareName}", shareName);
                return new StatusCodeResult(500);
            }
        }

        [Function("ListFileShare")]
        public async Task<IActionResult> ListFileShare(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "fileshare/{shareName}")] HttpRequest req,
            string shareName)
        {
            try
            {
                var directoryName = req.Query["directoryName"].ToString();

                var shareClient = _shareServiceClient.GetShareClient(shareName);
                var directoryClient = string.IsNullOrEmpty(directoryName)
                    ? shareClient.GetRootDirectoryClient()
                    : shareClient.GetDirectoryClient(directoryName);

                var files = new List<FileShareItem>();
                await foreach (var fileItem in directoryClient.GetFilesAndDirectoriesAsync())
                {
                    files.Add(new FileShareItem
                    {
                        Name = fileItem.Name,
                        IsDirectory = fileItem.IsDirectory,
                        FileSize = fileItem.FileSize
                    });
                }

                return new OkObjectResult(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing file share {ShareName}", shareName);
                return new StatusCodeResult(500);
            }
        }
    }
}