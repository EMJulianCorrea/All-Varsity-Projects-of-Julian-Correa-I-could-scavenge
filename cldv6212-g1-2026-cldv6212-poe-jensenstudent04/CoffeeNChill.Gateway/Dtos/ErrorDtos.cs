namespace CoffeeNChill.Gateway.Dtos;

/// <summary>
/// The error shape the Azure Functions app returns. Menu and order endpoints use
/// <c>message</c>/<c>details</c>; document endpoints use <c>success</c>/<c>message</c>.
/// </summary>
public class FunctionsErrorBody
{
    public string? Message { get; set; }
    public string[]? Details { get; set; }
}
