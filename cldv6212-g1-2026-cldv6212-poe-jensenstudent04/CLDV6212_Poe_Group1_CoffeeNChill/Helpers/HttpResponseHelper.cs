using System.Net;
using CoffeeNChill.Functions.Models;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Helpers;

/* The one place that knows how to shape an HTTP response - functions should call this
   instead of building HttpResponseData manually, so every endpoint answers consistently. */
public static class HttpResponseHelper
{
    public static async Task<HttpResponseData> OkAsync<T>(HttpRequestData req, T body, HttpStatusCode status = HttpStatusCode.OK)
    {
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(body);
        return response;
    }

    public static async Task<HttpResponseData> ErrorAsync(HttpRequestData req, HttpStatusCode status, string message, IEnumerable<string>? details = null)
    {
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(new APIError(message, details));
        return response;
    }

    /* Added for the Document endpoints (Maheshen) - kept alongside OkAsync/ErrorAsync
       above rather than replacing them, since MenuFunctions/MenuQueryFunctions already
       depend on those two existing. */
    public static async Task<HttpResponseData> BadRequestAsync(HttpRequestData request, string message)
    {
        var response = request.CreateResponse(HttpStatusCode.BadRequest);
        await response.WriteAsJsonAsync(new { success = false, message });
        return response;
    }

    public static async Task<HttpResponseData> NotFoundAsync(HttpRequestData request, string message)
    {
        var response = request.CreateResponse(HttpStatusCode.NotFound);
        await response.WriteAsJsonAsync(new { success = false, message });
        return response;
    }

    // Part 2: duplicate document name -> 409 Conflict.
    public static async Task<HttpResponseData> ConflictAsync(HttpRequestData request, string message)
    {
        var response = request.CreateResponse(HttpStatusCode.Conflict);
        await response.WriteAsJsonAsync(new { success = false, message });
        return response;
    }

    public static async Task<HttpResponseData> ServerErrorAsync(HttpRequestData request, string message)
    {
        var response = request.CreateResponse(HttpStatusCode.InternalServerError);
        await response.WriteAsJsonAsync(new { success = false, message });
        return response;
    }
}
