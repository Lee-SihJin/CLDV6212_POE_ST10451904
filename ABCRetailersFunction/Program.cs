using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        services.AddSingleton(provider =>
        {
            return new TableServiceClient(connectionString);
        });

        services.AddSingleton(provider =>
        {
            return new BlobServiceClient(connectionString);
        });

        services.AddSingleton(provider =>
        {
            return new QueueServiceClient(connectionString);
        });

        services.AddSingleton(provider =>
        {
            return new ShareServiceClient(connectionString);
        });
    })
    .Build();

host.Run();