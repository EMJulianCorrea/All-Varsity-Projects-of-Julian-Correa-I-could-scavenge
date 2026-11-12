using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.Dtos;

/// <summary>Body for <c>POST /api/orders/queue</c> (anonymous - customers do not need an account).</summary>
public class PlaceOrderRequest
{
    /// <summary>Optional. Leave empty and the gateway generates a GUID for you.</summary>
    [StringLength(64)]
    public string? OrderId { get; set; }

    /// <example>Jane</example>
    [Required, StringLength(100)]
    public string CustomerName { get; set; } = "";

    /// <summary>SKUs of the ordered menu items (at least one).</summary>
    /// <example>["COF-001","BAK-002"]</example>
    [Required, MinLength(1)]
    public string[] SelectedItemSKUs { get; set; } = [];

    /// <summary>Order total in rand; must be zero or greater.</summary>
    [Range(0, 100000)]
    public double TotalPrice { get; set; }

    /// <summary>Optional ISO-8601 timestamp; defaults to now.</summary>
    public string? OrderTimestamp { get; set; }
}

/// <summary>Returned when an order has been accepted onto the processing queue.</summary>
/// <param name="OrderId">Use this to track the order with <c>GET /api/orders/{orderId}</c>.</param>
/// <param name="MessageId">The queue message id.</param>
/// <param name="Note">Reminder that processing is asynchronous.</param>
public record OrderQueuedResponse(string OrderId, string MessageId, string Note);

/// <summary>An order as stored by the queue processor.</summary>
public class OrderDto
{
    public string OrderId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string[] SelectedItemSKUs { get; set; } = [];
    public double TotalPrice { get; set; }
    public string OrderTimestamp { get; set; } = "";
    /// <summary>Received, Preparing, Ready or Collected.</summary>
    public string Status { get; set; } = "";
}

/// <summary>Body for <c>PUT /api/orders/status</c> (Admin or Staff).</summary>
public class UpdateOrderStatusRequest
{
    /// <summary>The order's GUID.</summary>
    [Required, StringLength(64)]
    public string OrderId { get; set; } = "";

    /// <summary>New status: Preparing, Ready or Collected. Orders only move forward.</summary>
    /// <example>Ready</example>
    [Required, StringLength(20)]
    public string Status { get; set; } = "";
}

/// <summary>Confirmation of a status change.</summary>
public class UpdateOrderStatusResponse
{
    public string OrderId { get; set; } = "";
    public string Status { get; set; } = "";
}
