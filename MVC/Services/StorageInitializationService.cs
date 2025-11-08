using System.Text.Json;

namespace ABCRetailers.Services
{
    public class StorageInitializationService : IStorageInitializationService, IHostedService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StorageInitializationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly bool _autoInitialize;

        public StorageInitializationService(
            HttpClient httpClient,
            ILogger<StorageInitializationService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _autoInitialize = _configuration.GetValue<bool>("AzureStorage:AutoInitialize", true);
        }

        public async Task InitializeStorageAsync()
        {
            try
            {
                _logger.LogInformation("Initializing Azure storage through Functions...");

                var response = await _httpClient.PostAsync("storage/initialize", null);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Azure storage initialized successfully");
                }
                else
                {
                    _logger.LogWarning("Azure storage initialization returned status: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure storage through Functions");
                throw;
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_autoInitialize)
            {
                try
                {
                    _logger.LogInformation("Auto-initializing storage...");
                    await InitializeStorageAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Auto-initialization failed");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private record HealthResponse(
            Dictionary<string, bool> Tables,
            Dictionary<string, bool> Blobs,
            Dictionary<string, bool> Queues,
            Dictionary<string, bool> FileShares,
            DateTime Timestamp
        );
    }
}