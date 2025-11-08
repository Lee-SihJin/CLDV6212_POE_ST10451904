using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ABCRetailers.Models;
using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Services
{
    public class FunctionsApiClient : IFunctionsApi
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FunctionsApiClient> _logger;

        public FunctionsApiClient(HttpClient httpClient, ILogger<FunctionsApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // Table operations
        public async Task<List<T>> GetAllEntitiesAsync<T>(string tableName) where T : class, new()
        {
            try
            {
                var response = await _httpClient.GetAsync($"table/{tableName}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var entities = JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return entities ?? new List<T>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all entities from table {TableName}", tableName);
                throw;
            }
        }

        public async Task<T?> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, new()
        {
            try
            {
                var response = await _httpClient.GetAsync($"table/{tableName}/{partitionKey}/{rowKey}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entity from table {TableName}", tableName);
                throw;
            }
        }

        public async Task<T> AddEntityAsync<T>(string tableName, T entity) where T : class
        {
            try
            {
                var request = new StorageRequest
                {
                    EntityData = JsonSerializer.Serialize(entity)
                };

                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"table/{tableName}", content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Failed to deserialize response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding entity to table {TableName}", tableName);
                throw;
            }
        }

        public async Task<T> UpdateEntityAsync<T>(string tableName, T entity) where T : class
        {
            try
            {
                var request = new StorageRequest
                {
                    EntityData = JsonSerializer.Serialize(entity)
                };

                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"table/{tableName}", content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Failed to deserialize response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating entity in table {TableName}", tableName);
                throw;
            }
        }

        public async Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"table/{tableName}/{partitionKey}/{rowKey}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting entity from table {TableName}", tableName);
                throw;
            }
        }

        // Blob operations
        public async Task<string> UploadBlobAsync(IFormFile file, string containerName)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                using var streamContent = new StreamContent(fileStream);

                content.Add(streamContent, "file", file.FileName);

                var response = await _httpClient.PostAsync($"blob/{containerName}", content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<UploadResult>();
                return result?.Url ?? throw new InvalidOperationException("Failed to get upload URL");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading blob to container {ContainerName}", containerName);
                throw;
            }
        }

        public async Task DeleteBlobAsync(string containerName, string blobName)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"blob/{containerName}/{blobName}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blob from container {ContainerName}", containerName);
                throw;
            }
        }

        // Queue operations
        public async Task SendMessageAsync(string queueName, string message)
        {
            try
            {
                var request = new StorageRequest { Message = message };
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"queue/{queueName}", content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to queue {QueueName}", queueName);
                throw;
            }
        }

        public async Task<string?> ReceiveMessageAsync(string queueName)
        {
            try
            {
                var response = await _httpClient.GetAsync($"queue/{queueName}");

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    return null;

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<MessageResult>();
                return result?.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving message from queue {QueueName}", queueName);
                throw;
            }
        }
        // File Share operations
        public async Task<string> UploadToFileShareAsync(IFormFile file, string shareName, string directoryName = "")
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                using var streamContent = new StreamContent(fileStream);

                content.Add(streamContent, "file", file.FileName);

                var url = string.IsNullOrEmpty(directoryName)
                    ? $"fileshare/{shareName}"
                    : $"fileshare/{shareName}?directoryName={Uri.EscapeDataString(directoryName)}";

                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<FileShareUploadResult>();
                return result?.FileName ?? throw new InvalidOperationException("Failed to get file name");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading to file share {ShareName}", shareName);
                throw;
            }
        }

        public async Task<byte[]> DownloadFromFileShareAsync(string shareName, string fileName, string directoryName = "")
        {
            try
            {
                var url = string.IsNullOrEmpty(directoryName)
                    ? $"fileshare/{shareName}/{fileName}"
                    : $"fileshare/{shareName}/{fileName}?directoryName={Uri.EscapeDataString(directoryName)}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading from file share {ShareName}", shareName);
                throw;
            }
        }

        public async Task DeleteFromFileShareAsync(string shareName, string fileName, string directoryName = "")
        {
            try
            {
                var url = string.IsNullOrEmpty(directoryName)
                    ? $"fileshare/{shareName}/{fileName}"
                    : $"fileshare/{shareName}/{fileName}?directoryName={Uri.EscapeDataString(directoryName)}";

                var response = await _httpClient.DeleteAsync(url);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting from file share {ShareName}", shareName);
                throw;
            }
        }

        public async Task<List<FileShareItem>> ListFileShareAsync(string shareName, string directoryName = "")
        {
            try
            {
                var url = string.IsNullOrEmpty(directoryName)
                    ? $"fileshare/{shareName}"
                    : $"fileshare/{shareName}?directoryName={Uri.EscapeDataString(directoryName)}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<List<FileShareItem>>();
                return result ?? new List<FileShareItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing file share {ShareName}", shareName);
                throw;
            }
        }

        // Update existing records and add new ones
        private record UploadResult(string Url);
        private record MessageResult(string Message);
        private record FileShareUploadResult(string FileName);
    }

   
}