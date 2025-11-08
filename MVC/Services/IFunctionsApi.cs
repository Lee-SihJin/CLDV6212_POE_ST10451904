
using ABCRetailers.Models;

namespace ABCRetailers.Services
{
    public interface IFunctionsApi
    {
        // Table operations
        Task<List<T>> GetAllEntitiesAsync<T>(string tableName) where T : class, new();
        Task<T?> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, new();
        Task<T> AddEntityAsync<T>(string tableName, T entity) where T : class;
        Task<T> UpdateEntityAsync<T>(string tableName, T entity) where T : class;
        Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey);

        // Blob operations
        Task<string> UploadBlobAsync(IFormFile file, string containerName);
        Task DeleteBlobAsync(string containerName, string blobName);

        // Queue operations
        Task SendMessageAsync(string queueName, string message);
        Task<string?> ReceiveMessageAsync(string queueName);

        // File Share operations
        Task<string> UploadToFileShareAsync(IFormFile file, string shareName, string directoryName = "");
        Task<byte[]> DownloadFromFileShareAsync(string shareName, string fileName, string directoryName = "");
    }
}