
using Microsoft.AspNetCore.Http;

namespace ABCRetailers.Models
{
    public class StorageRequest
    {
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public string? EntityData { get; set; } 
        public string? ContainerName { get; set; }
        public string? QueueName { get; set; }
        public string? Message { get; set; }
        public string? ShareName { get; set; }
        public string? DirectoryName { get; set; }
        public string? BlobName { get; set; }
        public string? FileName { get; set; }
    }

    
}