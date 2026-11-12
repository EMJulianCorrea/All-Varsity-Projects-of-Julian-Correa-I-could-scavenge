using CoffeeNChill.Gateway.Dtos;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

/// <summary>Placing and tracking orders. Customers order anonymously; only staff change statuses.</summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController(FunctionsClient functions) : ControllerBase
{
    /// <summary>Place an order onto the processing queue (public).</summary>
    /// <remarks>
    /// Processing is asynchronous: the order appears in <c>GET /api/orders/{orderId}</c> a few seconds later
    /// with status <c>Received</c>, then progresses automatically.
    /// </remarks>
    /// <response code="202">Accepted onto the queue.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">An order with this id was already queued.</response>
    [HttpPost("queue")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrderQueuedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderQueuedResponse>> Place(PlaceOrderRequest request, CancellationToken ct)
    {
        // The Functions app needs an id and a timestamp; fill them in for the caller when absent.
        request.OrderId = string.IsNullOrWhiteSpace(request.OrderId) ? Guid.NewGuid().ToString() : request.OrderId.Trim();
        request.OrderTimestamp = string.IsNullOrWhiteSpace(request.OrderTimestamp)
            ? DateTimeOffset.UtcNow.ToString("O")
            : request.OrderTimestamp;

        var messageId = await functions.PlaceOrderAsync(request, ct);
        return Accepted(new OrderQueuedResponse(request.OrderId, messageId, "Order queued. It will be processed in a few seconds."));
    }

    /// <summary>Look up an order and its current status (public).</summary>
    /// <param name="orderId">The id returned when the order was placed.</param>
    /// <response code="200">The order.</response>
    /// <response code="404">Not found - it may still be waiting in the queue.</response>
    [HttpGet("{orderId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> Get(string orderId, CancellationToken ct)
        => Ok(await functions.GetOrderAsync(orderId, ct));

    /// <summary>Move an order to a new status (Admin or Staff).</summary>
    /// <response code="200">Status updated.</response>
    /// <response code="400">Unknown status value.</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="403">The caller is a Customer.</response>
    /// <response code="404">No such order.</response>
    /// <response code="409">Orders only move forward (Received, Preparing, Ready, Collected).</response>
    [HttpPut("status")]
    [Authorize(Roles = Roles.AdminOrStaff)]
    [ProducesResponseType(typeof(UpdateOrderStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateOrderStatusResponse>> UpdateStatus(UpdateOrderStatusRequest request, CancellationToken ct)
        => Ok(await functions.UpdateOrderStatusAsync(request, ct));
}
