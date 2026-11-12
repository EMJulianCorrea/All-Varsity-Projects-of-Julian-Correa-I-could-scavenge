using System.Net;
using System.Text.Json;
using CoffeeNChill.Functions.Helpers;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Repository;
using CoffeeNChill.Functions.Validation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions;

public class OrderFunctions
{
    /* The same lifecycle the StatusSimulationTimer walks through automatically.
       Staff (via the gateway) may move an order forward through it manually. */
    private static readonly string[] StatusSequence = { "Received", "Preparing", "Ready", "Collected" };

    private readonly IQueueRepository _queueRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly OrderValidator _validator;
    private readonly ILogger<OrderFunctions> _logger;

    public OrderFunctions(
        IQueueRepository queueRepository,
        IOrderRepository orderRepository,
        OrderValidator validator,
        ILogger<OrderFunctions> logger)
    {
        _queueRepository = queueRepository;
        _orderRepository = orderRepository;
        _validator = validator;
        _logger = logger;
    }

    [Function("PlaceOrder")]
    public async Task<HttpResponseData> PlaceOrder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/queue")] HttpRequestData req)
    {
        var request = await req.ReadFromJsonAsync<OrderRequest>();
        if (request is null)
            return await HttpResponseHelper.ErrorAsync(req, HttpStatusCode.BadRequest, "Request body could not be read.");

        var errors = _validator.ValidateOrderRequest(request, requireIdAndName: true);
        if (errors.Count > 0)
            return await HttpResponseHelper.ErrorAsync(req, HttpStatusCode.BadRequest, "Validation failed.", errors);

        var entity = request.ToOrder(request.OrderId!, request.CustomerName!);
        try
        {
            var sent = await _queueRepository.SendMessageAsync(entity);
            _logger.LogInformation("SendOrder succeeded for {CustomerName}", entity.CustomerName);
            return await HttpResponseHelper.OkAsync(req, sent, HttpStatusCode.Created);
        }
        catch (InvalidOperationException ex)
        {
            return await HttpResponseHelper.ErrorAsync(req, HttpStatusCode.Conflict, ex.Message);
        }
    }

    /* Part 3: staff move an order to a new status. Reached only through the gateway,
       which enforces the Admin/Staff role before forwarding here. */
    [Function("UpdateOrderStatus")]
    public async Task<HttpResponseData> UpdateOrderStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "orders/status")] HttpRequestData req)
    {
        UpdateOrderStatusRequest? body;
        try
        {
            body = await req.ReadFromJsonAsync<UpdateOrderStatusRequest>();
        }
        catch (JsonException)
        {
            return await HttpResponseHelper.ErrorAsync(req, HttpStatusCode.BadRequest, "Request body is not valid JSON.");
        }

        if (body is null || string.IsNullOrWhiteSpace(body.OrderId) || string.IsNullOrWhiteSpace(body.Status))
            return await HttpResponseHelper.ErrorAsync(req, HttpStatusCode.BadRequest, "OrderId and Status are required.");

        var newStatus = StatusSequence.FirstOrDefault(s => s.Equals(body.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (newStatus is null)
        {
            return await HttpResponseHelper.ErrorAsync(req, HttpStatusCode.BadRequest,
                $"'{body.Status}' is not a valid status. Valid values: {string.Join(", ", StatusSequence)}.");
        }

        var order = await _orderRepository.FindByOrderIdAsync(body.OrderId.Trim());
        if (order is null)
            return await HttpResponseHelper.ErrorAsync(req, HttpStatusCode.NotFound, $"Order '{body.OrderId}' was not found.");

        // Orders only ever move forward: Received -> Preparing -> Ready -> Collected.
        var currentIndex = Array.IndexOf(StatusSequence, order.Status);
        var newIndex = Array.IndexOf(StatusSequence, newStatus);
        if (newIndex <= currentIndex)
        {
            _logger.LogWarning("Rejected transition {Current} -> {New} for order {OrderId}.", order.Status, newStatus, order.RowKey);
            return await HttpResponseHelper.ErrorAsync(req, HttpStatusCode.Conflict,
                $"Cannot change order status from {order.Status} to {newStatus}.");
        }

        await _orderRepository.UpdateStatusAsync(order.PartitionKey, order.RowKey, newStatus);
        _logger.LogInformation("Order {OrderId} status changed {Current} -> {New}.", order.RowKey, order.Status, newStatus);

        return await HttpResponseHelper.OkAsync(req, new UpdateOrderStatusResponse { OrderId = order.RowKey, Status = newStatus });
    }
}
