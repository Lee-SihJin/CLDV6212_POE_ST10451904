using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

public static class BlobFunction
{
    [Function("BlobUploadFunction")]
    public static async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blob/upload")] HttpRequestData req,
        FunctionContext context)
    {
        var log = context.GetLogger("BlobUploadFunction");
        var response = req.CreateResponse();

        try
        {
            log.LogInformation("Blob upload function triggered");

            if (!req.Headers.TryGetValues("Content-Type", out var contentTypes) ||
                !contentTypes.ToString().Contains("multipart/form-data"))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Expected multipart/form-data content type");
                return response;
            }

            var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
            var containerName = Environment.GetEnvironmentVariable("BlobContainerName") ?? "uploads";

            var container = new BlobContainerClient(conn, containerName);
            await container.CreateIfNotExistsAsync();

            var body = await new StreamReader(req.Body).ReadToEndAsync();

            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteStringAsync("Multipart form data parsing not implemented for isolated worker. Consider using Base64 encoding instead.");
            return response;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error in blob upload function");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }
}