using System.Text.Json;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Repository;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions;

public class OrderQueueFunctions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderQueueFunctions> _logger;

    public OrderQueueFunctions(IOrderRepository orderRepository, ILogger<OrderQueueFunctions> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    [Function("ProcessOrderQueue")]
    public async Task Run([QueueTrigger("order-processing-queue")] string message)
    {
        // The queue client sends with Base64 MessageEncoding, so the isolated worker
        // runtime already decodes it before the string arrives here.
        var order = JsonSerializer.Deserialize<Order>(message, JsonOptions);
        if (order is null)
        {
            _logger.LogWarning("ProcessOrderQueue received a message that could not be deserialized: {Message}", message);
            return;
        }

        // PartitionKey = OrderDate, per the brief - derived from the order's own timestamp.
        var orderDate = DateTimeOffset.TryParse(order.OrderTimestamp, out var parsed)
            ? parsed.ToString("yyyy-MM-dd")
            : DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");

        var entity = order.ToEntity(orderDate, status: "Received");
        await _orderRepository.CreateAsync(entity);

        _logger.LogInformation("Order {OrderId} received and written to Orders table with status 'Received'.", order.OrderId);
    }
}
