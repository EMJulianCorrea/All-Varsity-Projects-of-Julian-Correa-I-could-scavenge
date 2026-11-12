using System.Net;
using Azure;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Middleware;

/// <summary>
/// Custom global exception middleware. Anything thrown further down the pipeline is caught
/// here and returned as an RFC 7807 ProblemDetails JSON body, so callers never see a stack
/// trace or an HTML error page, and every failure is logged in one place.
/// </summary>
public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        var (status, detail) = Map(context, ex);

        if (status >= 500) logger.LogError(ex, "Unhandled {Status} for {Method} {Path}", status, context.Request.Method, context.Request.Path);
        else logger.LogWarning("Handled {Status} for {Method} {Path}: {Message}", status, context.Request.Method, context.Request.Path, ex.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = TitleFor(status),
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (ex is FunctionsApiException { Details.Length: > 0 } api)
            problem.Extensions["errors"] = api.Details;

        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }

    /// <summary>Decides the HTTP status and the message that is safe to show the caller.</summary>
    public static (int Status, string Detail) Map(HttpContext context, Exception ex) => ex switch
    {
        // The Functions app said no because of what the caller sent: pass its answer through.
        FunctionsApiException { StatusCode: HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.Conflict } api
            => ((int)api.StatusCode, api.Message),

        // Any other Functions failure is a gateway problem, not the caller's.
        FunctionsApiException => (StatusCodes.Status502BadGateway, "The back-end service returned an error."),

        BadHttpRequestException => (StatusCodes.Status400BadRequest, "The request could not be read."),

        // The caller hung up: nobody is listening, so the exact code does not matter.
        OperationCanceledException when context.RequestAborted.IsCancellationRequested
            => (499, "The client closed the request."),

        // A timeout talking to the Functions app.
        TaskCanceledException => (StatusCodes.Status504GatewayTimeout, "The back-end service took too long to respond."),

        HttpRequestException => (StatusCodes.Status502BadGateway, "The back-end service is unreachable."),

        RequestFailedException => (StatusCodes.Status503ServiceUnavailable, "Storage is temporarily unavailable."),

        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };

    private static string TitleFor(int status) => status switch
    {
        400 => "Bad Request",
        404 => "Not Found",
        409 => "Conflict",
        499 => "Client Closed Request",
        500 => "Internal Server Error",
        502 => "Bad Gateway",
        503 => "Service Unavailable",
        504 => "Gateway Timeout",
        _ => "Error"
    };
}
