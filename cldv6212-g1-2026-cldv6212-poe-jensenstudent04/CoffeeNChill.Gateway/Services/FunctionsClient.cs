using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CoffeeNChill.Gateway.Dtos;

namespace CoffeeNChill.Gateway.Services;

/// <summary>
/// The only class that talks to the Azure Functions app. The Functions are never exposed
/// publicly: inside Docker they are reached at http://functions:80 and only the gateway is published.
/// </summary>
public class FunctionsClient(HttpClient http, ILogger<FunctionsClient> logger)
{
    // Web defaults: camelCase output, case-insensitive input.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ---- Menu ---------------------------------------------------------------------------

    public async Task<List<MenuItemDto>> GetMenuAsync(CancellationToken ct)
    {
        using var response = await http.GetAsync("api/menu", ct);
        return await ReadAsync<List<MenuItemDto>>(response, ct);
    }

    public async Task<List<MenuItemDto>> GetMenuByCategoryAsync(string category, CancellationToken ct)
    {
        using var response = await http.GetAsync($"api/menu/category/{Uri.EscapeDataString(category)}", ct);
        return await ReadAsync<List<MenuItemDto>>(response, ct);
    }

    public async Task<MenuItemDto> CreateMenuItemAsync(CreateMenuItemRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("api/menu", request, Json, ct);
        return await ReadAsync<MenuItemDto>(response, ct);
    }

    public async Task<MenuItemDto> UpdateMenuItemAsync(string category, string sku, UpdateMenuItemRequest request, CancellationToken ct)
    {
        var url = $"api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(sku)}";
        using var response = await http.PutAsJsonAsync(url, request, Json, ct);
        return await ReadAsync<MenuItemDto>(response, ct);
    }

    public async Task DeleteMenuItemAsync(string category, string sku, CancellationToken ct)
    {
        var url = $"api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(sku)}";
        using var response = await http.DeleteAsync(url, ct);
        await EnsureSuccessAsync(response, ct);
    }

    // ---- Orders -------------------------------------------------------------------------

    /// <summary>Queues the order and returns the queue message id.</summary>
    public async Task<string> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("api/orders/queue", request, Json, ct);
        return await ReadAsync<string>(response, ct);
    }

    public async Task<OrderDto> GetOrderAsync(string orderId, CancellationToken ct)
    {
        using var response = await http.GetAsync($"api/orders/{Uri.EscapeDataString(orderId)}", ct);
        return await ReadAsync<OrderDto>(response, ct);
    }

    public async Task<UpdateOrderStatusResponse> UpdateOrderStatusAsync(UpdateOrderStatusRequest request, CancellationToken ct)
    {
        using var response = await http.PutAsJsonAsync("api/orders/status", request, Json, ct);
        return await ReadAsync<UpdateOrderStatusResponse>(response, ct);
    }

    // ---- Documents ----------------------------------------------------------------------

    /// <summary>Streams the PDF to the Functions app without buffering it in memory.</summary>
    public async Task<string> UploadDocumentAsync(string fileName, Stream content, long length, string contentType, CancellationToken ct)
    {
        using var body = new StreamContent(content);
        body.Headers.ContentLength = length;
        body.Headers.ContentType = MediaTypeHeaderValue.TryParse(contentType, out var parsed)
            ? parsed
            : new MediaTypeHeaderValue("application/pdf");

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/documents/upload") { Content = body };
        request.Headers.Add("X-File-Name", fileName);

        using var response = await http.SendAsync(request, ct);
        var result = await ReadAsync<UploadResult>(response, ct);
        return result.FileName ?? fileName;
    }

    public async Task<List<DocumentDto>> ListDocumentsAsync(CancellationToken ct)
    {
        using var response = await http.GetAsync("api/documents", ct);
        return await ReadAsync<List<DocumentDto>>(response, ct);
    }

    /// <summary>
    /// Opens a download. The caller owns the returned response and must dispose it
    /// (the controller registers it for disposal when the HTTP response finishes).
    /// </summary>
    public async Task<HttpResponseMessage> OpenDocumentAsync(string fileName, CancellationToken ct)
    {
        var response = await http.GetAsync($"api/documents/download/{Uri.EscapeDataString(fileName)}",
            HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            try { await EnsureSuccessAsync(response, ct); }
            finally { response.Dispose(); }
        }
        return response;
    }

    public async Task DeleteDocumentAsync(string fileName, CancellationToken ct)
    {
        using var response = await http.DeleteAsync($"api/documents/{Uri.EscapeDataString(fileName)}", ct);
        await EnsureSuccessAsync(response, ct);
    }

    // ---- Helpers ------------------------------------------------------------------------

    private sealed record UploadResult(string? FileName);

    private async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        await EnsureSuccessAsync(response, ct);
        var value = await response.Content.ReadFromJsonAsync<T>(Json, ct);
        return value ?? throw new FunctionsApiException(HttpStatusCode.BadGateway, "The Functions app returned an empty response.");
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var (message, details) = await ReadErrorAsync(response, ct);
        logger.LogWarning("Functions app returned {StatusCode}: {Message}", (int)response.StatusCode, message);
        throw new FunctionsApiException(response.StatusCode, message, details);
    }

    /// <summary>Menu/order errors use { message, details }, document errors { success, message }; handle both.</summary>
    private static async Task<(string Message, string[]? Details)> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<FunctionsErrorBody>(Json, ct);
            if (!string.IsNullOrWhiteSpace(body?.Message))
                return (body.Message, body.Details);
        }
        catch (JsonException) { /* body was not JSON */ }
        catch (NotSupportedException) { /* no JSON content type */ }

        return (response.ReasonPhrase ?? $"The Functions app returned {(int)response.StatusCode}.", null);
    }
}
