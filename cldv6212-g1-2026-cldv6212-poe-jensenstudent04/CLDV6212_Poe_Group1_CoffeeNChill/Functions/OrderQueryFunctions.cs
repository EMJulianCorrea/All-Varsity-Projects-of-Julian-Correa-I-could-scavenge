using System.Net;
using CoffeeNChill.Functions.Helpers;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Repository;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions;

public class OrderQueryFunctions
{
    private readonly IOrderRepository _orderRepository;

    public OrderQueryFunctions(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    [Function("GetOrderStatus")]
    public async Task<HttpResponseData> GetOrderStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{orderId}")] HttpRequestData req,
        string orderId)
    {
        var entity = await _orderRepository.FindByOrderIdAsync(orderId);
        if (entity is null)
            return await HttpResponseHelper.ErrorAsync(req, HttpStatusCode.NotFound, $"Order '{orderId}' was not found.");

        return await HttpResponseHelper.OkAsync(req, Order.FromEntity(entity));

    }

}