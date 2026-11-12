using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using CoffeeNChill.Functions.Repository;
using CoffeeNChill.Functions.Validation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(config =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        /* Practice #2: this is a named app setting, kept deliberately separate from
           AzureWebJobsStorage (the Functions runtime's own storage account for
           triggers/locks/logs) - even though both point at Azurite locally. */
        string menuStorageConnection = configuration["MenuStorageConnection"]
            ?? throw new InvalidOperationException("MenuStorageConnection is not configured. Check local.settings.json.");

        /* Practice #5: one TableServiceClient for the whole app's lifetime - repositories
           ask it for a TableClient, they never construct their own connection. */
        services.AddSingleton(new TableServiceClient(menuStorageConnection));

        services.AddSingleton<IMenuRepository, TableMenuRepository>();
        services.AddSingleton<IOrderRepository, TableOrderRepository>();
        services.AddSingleton<ModelValidator>();
        services.AddSingleton<OrderValidator>();

        /* Blob Storage for staff documents (Maheshen) - same connection string setting,
           same singleton-client pattern as Table Storage above. */
        services.AddSingleton(new BlobServiceClient(menuStorageConnection));
        services.AddSingleton<IDocumentRepository, BlobDocumentRepository>();

        /* Queue Storage for orders. Uses its own named setting (QueueStorageConnection) that points
           at the Azurite queue endpoint (port 10001). One QueueClient for the app's lifetime -
           same singleton pattern as the Table and Blob clients above. Base64 encoding is what
           the Azure Functions queue trigger expects by default. */
        string queueStorageConnection = configuration["QueueStorageConnection"]
            ?? throw new InvalidOperationException("QueueStorageConnection is not configured. Check local.settings.json.");

        services.AddSingleton(new QueueClient(
            queueStorageConnection,
            "order-processing-queue",   // was "order-queue"
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));
        services.AddSingleton<IQueueRepository, QueueRepository>();
        /*  Note: FileValidator is a static class (Maheshen's design), so it needs no DI
            registration - it's called directly as FileValidator.Validate(...) */
    })
    .Build();

host.Run();