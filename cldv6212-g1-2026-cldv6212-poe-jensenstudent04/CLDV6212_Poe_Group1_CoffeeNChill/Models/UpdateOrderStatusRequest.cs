namespace CoffeeNChill.Functions.Models;

/* Body of PUT /api/orders/status (Part 3). OrderId is the same GUID string the customer
   received when the order was placed. */
public class UpdateOrderStatusRequest
{
    public string? OrderId { get; set; }
    public string? Status { get; set; }
}

public class UpdateOrderStatusResponse
{
    public string OrderId { get; set; } = default!;
    public string Status { get; set; } = default!;
}
