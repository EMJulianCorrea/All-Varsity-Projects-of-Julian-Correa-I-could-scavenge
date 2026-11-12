using System.Net;

namespace CoffeeNChill.Gateway.Services;

/// <summary>
/// Thrown by <see cref="FunctionsClient"/> when the internal Azure Functions app answers with an
/// error. The global exception middleware turns it into a clean ProblemDetails response.
/// </summary>
public class FunctionsApiException(HttpStatusCode statusCode, string message, string[]? details = null)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string[]? Details { get; } = details;
}
