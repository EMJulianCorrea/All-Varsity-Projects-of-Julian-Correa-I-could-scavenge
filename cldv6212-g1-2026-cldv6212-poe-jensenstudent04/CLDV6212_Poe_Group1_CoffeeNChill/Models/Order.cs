namespace CoffeeNChill.Functions.Models;

public class Order
{
    public string OrderId { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public string[] SelectedItemSKUs { get; set; } = default!;
    public double TotalPrice { get; set; }
    public string OrderTimestamp { get; set; } = default!;
    public string Status { get; set; } = "Received";

    public static Order FromEntity(OrderEntity entity) => new()
    {
        OrderId = entity.RowKey,
        CustomerName = entity.CustomerName,
        SelectedItemSKUs = entity.SelectedItemSKUs.Split(',', StringSplitOptions.RemoveEmptyEntries),
        TotalPrice = entity.TotalPrice,
        OrderTimestamp = entity.OrderTimestamp,
        Status = entity.Status
    };

    public OrderEntity ToEntity(string orderDate, string status = "Received") => new()
    {
        PartitionKey = orderDate,
        RowKey = OrderId,
        CustomerName = CustomerName,
        SelectedItemSKUs = string.Join(',', SelectedItemSKUs),
        TotalPrice = TotalPrice,
        OrderTimestamp = OrderTimestamp,
        Status = status
    };
}