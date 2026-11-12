using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Exceptions;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Repository;

public class TableOrderRepository : IOrderRepository
{
    private const string TableName = "Orders";
    private readonly TableClient _tableClient;
    private readonly ILogger<TableOrderRepository> _logger;

    // Practice #5: the TableServiceClient is injected once (registered as a singleton in
    // Program.cs) - this repository just asks it for a TableClient, same pattern as
    // TableMenuRepository.
    public TableOrderRepository(TableServiceClient serviceClient, ILogger<TableOrderRepository> logger)
    {
        _logger = logger;
        _tableClient = serviceClient.GetTableClient(TableName);

        // Synchronous on purpose: runs once at startup (DI construction), not per request.
        _tableClient.CreateIfNotExists();
    }

    public async Task<OrderEntity> CreateAsync(OrderEntity entity, CancellationToken cancellationToken = default)
    {
        await _tableClient.AddEntityAsync(entity, cancellationToken);
        _logger.LogInformation("Order {OrderId} written for {OrderDate} with status {Status}",
            entity.RowKey, entity.PartitionKey, entity.Status);
        return entity;
    }

    public async Task<OrderEntity> GetAsync(string orderDate, string orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<OrderEntity>(orderDate, orderId, cancellationToken: cancellationToken);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Practice #9: plan for failures - surfaced as a typed exception, same as
            // MenuItemNotFoundException, so callers/HTTP functions can map it to a clean 404.
            throw new OrderNotFoundException(orderDate, orderId);
        }
    }

    public async Task UpdateStatusAsync(string orderDate, string orderId, string status, CancellationToken cancellationToken = default)
    {
        var entity = await GetAsync(orderDate, orderId, cancellationToken);
        entity.Status = status;
        await _tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, cancellationToken);
        _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, status);
    }

    public async Task<IReadOnlyList<OrderEntity>> GetActiveOrdersAsync(CancellationToken cancellationToken = default)
    {
        // Practice #8: filter server-side (not Collected) rather than pulling everything
        // back and filtering in C#.
        var results = new List<OrderEntity>();
        await foreach (var entity in _tableClient.QueryAsync<OrderEntity>(
            filter: "Status ne 'Collected'", cancellationToken: cancellationToken))
        {
            results.Add(entity);
        }
        return results;
    }

    public async Task<OrderEntity?> FindByOrderIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        // RowKey-only lookup since the caller (GetOrderStatus) only has the OrderId, not
        // the OrderDate partition - scans across partitions, acceptable at this scale.
        await foreach (var entity in _tableClient.QueryAsync<OrderEntity>(
            filter: $"RowKey eq '{orderId}'", cancellationToken: cancellationToken))
        {
            return entity;
        }
        return null;
    }
}