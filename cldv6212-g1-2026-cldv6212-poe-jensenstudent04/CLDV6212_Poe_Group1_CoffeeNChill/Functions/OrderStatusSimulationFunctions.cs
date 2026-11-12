using CoffeeNChill.Functions.Repository;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions;

public class OrderStatusSimulationFunctions
{
    private static readonly string[] StatusSequence = { "Received", "Preparing", "Ready", "Collected" };

    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderStatusSimulationFunctions> _logger;

    public OrderStatusSimulationFunctions(IOrderRepository orderRepository, ILogger<OrderStatusSimulationFunctions> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    // Every 30 seconds - frequent enough to visibly progress orders in a short video demo.
    [Function("StatusSimulationTimer")]
    public async Task Run([TimerTrigger("*/30 * * * * *")] TimerInfo timer)
    {
        var activeOrders = await _orderRepository.GetActiveOrdersAsync();

        foreach (var order in activeOrders)
        {
            var currentIndex = Array.IndexOf(StatusSequence, order.Status);
            if (currentIndex < 0 || currentIndex >= StatusSequence.Length - 1)
                continue; // unknown status, or already Collected

            var nextStatus = StatusSequence[currentIndex + 1];
            await _orderRepository.UpdateStatusAsync(order.PartitionKey, order.RowKey, nextStatus);
            _logger.LogInformation("Order {OrderId} progressed {OldStatus} -> {NewStatus}", order.RowKey, order.Status, nextStatus);
        }
    }
}