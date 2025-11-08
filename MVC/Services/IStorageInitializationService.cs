namespace ABCRetailers.Services
{
    public interface IStorageInitializationService
    {
        Task InitializeStorageAsync();
    }

    public class StorageHealthStatus
    {
        public bool IsHealthy { get; set; }
        public Dictionary<string, bool> Tables { get; set; } = new();
        public Dictionary<string, bool> Blobs { get; set; } = new();
        public Dictionary<string, bool> Queues { get; set; } = new();
        public Dictionary<string, bool> FileShares { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }
}