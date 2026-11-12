using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Smart_X.frontend.Models;
using System.Net.Http.Json;

namespace Smart_X.frontend.Services;

public class Smart_XApiClient
{
    private readonly HttpClient _http;

    public Smart_XApiClient(string baseUrl = "http://localhost:8080")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }


    public async Task<List<TelemetryReading>> GetTelemetryRangeAsync(string sensorId, DateTime from, DateTime to, int limit = 5000)
    {
        try
        {
            var url = $"/api/telemetry/{Uri.EscapeDataString(sensorId)}/range?from={Uri.EscapeDataString(from.ToUniversalTime().ToString("O"))}&to={Uri.EscapeDataString(to.ToUniversalTime().ToString("O"))}&limit={limit}";
            return await _http.GetFromJsonAsync<List<TelemetryReading>>(url) ?? new();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException) { return new(); }
    }

    public async Task<System.Text.Json.JsonElement?> GetNetworkAnalysisAsync(string gatewayId)
    {
        try { return await _http.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/network/analyze?gatewayId={Uri.EscapeDataString(gatewayId)}"); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException) { return null; }
    }

    public async Task<System.Text.Json.JsonElement?> GetNetworkTopologyAsync()
    {
        try { return await _http.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/network/topology"); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException) { return null; }
    }

    public async Task<(bool Success, string? Error, SensorRecord? Sensor)> RegisterSensorAsync(SensorRecord sensor)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/sensors", sensor);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<SensorRecord>();
                return (true, null, created);
            }

            var problem = await response.Content.ReadFromJsonAsync<ApiError>();
            return (false, problem?.Error ?? $"Request failed with status {response.StatusCode}.", null);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Could not reach the API: {ex.Message}", null);
        }
    }

    public async Task<List<SensorRecord>> GetSensorsAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<SensorRecord>>("/api/sensors");
            return result ?? new List<SensorRecord>();
        }
        catch (HttpRequestException)
        {
            return new List<SensorRecord>();
        }
    }

    public async Task<(bool Success, string? Error, SensorRecord? Sensor)> UpdateSensorAsync(string id, SensorRecord sensor)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/sensors/{id}", sensor);
            if (response.IsSuccessStatusCode)
            {
                var updated = await response.Content.ReadFromJsonAsync<SensorRecord>();
                return (true, null, updated);
            }

            var problem = await response.Content.ReadFromJsonAsync<ApiError>();
            return (false, problem?.Error ?? $"Request failed with status {response.StatusCode}.", null);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Could not reach the API: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string? Error)> DeleteSensorAsync(string id)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/sensors/{id}");
            if (response.IsSuccessStatusCode) return (true, null);

            var problem = await response.Content.ReadFromJsonAsync<ApiError>();
            return (false, problem?.Error ?? $"Delete failed with status {response.StatusCode}.");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Could not reach the API: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> SendTelemetryAsync(TelemetryReading reading)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/telemetry", reading);
            if (response.IsSuccessStatusCode) return (true, null);

            var problem = await response.Content.ReadFromJsonAsync<ApiError>();
            return (false, problem?.Error ?? $"Request failed with status {response.StatusCode}.");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Could not reach the API: {ex.Message}");
        }
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response)
    {
        try { return (await response.Content.ReadFromJsonAsync<ApiError>())?.Error; }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or NotSupportedException) { return null; }
    }

    public async Task<(bool Success, string? Error, ClassificationResult? Result)> ClassifyLogAsync(string fileName, string sample)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/logs/classify",
                new ClassifyRequest { FileName = fileName, Sample = sample });
            if (response.IsSuccessStatusCode)
                return (true, null, await response.Content.ReadFromJsonAsync<ClassificationResult>());

            return (false, await ReadErrorAsync(response) ?? $"Request failed with status {response.StatusCode}.", null);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Could not reach the API: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string? Error, SensorLog? Log)> UploadLogAsync(
        string sensorId, string filePath, LogClassification? classification = null)
    {
        try
        {
            await using var fileStream = File.OpenRead(filePath);
            using var content = new MultipartFormDataContent();

            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", Path.GetFileName(filePath));

            if (classification is not null)
                content.Add(new StringContent(classification.Value.ToString()), "classification");

            var response = await _http.PostAsync($"/api/sensors/{Uri.EscapeDataString(sensorId)}/logs", content);
            if (response.IsSuccessStatusCode)
                return (true, null, await response.Content.ReadFromJsonAsync<SensorLog>());

            return (false, await ReadErrorAsync(response) ?? $"Upload failed with status {response.StatusCode}.", null);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Could not reach the API: {ex.Message}", null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return (false, $"Could not read the file: {ex.Message}", null);
        }
    }

    public async Task<List<SensorLog>?> GetLogsAsync(string sensorId, LogClassification? classification = null)
    {
        try
        {
            var url = $"/api/sensors/{Uri.EscapeDataString(sensorId)}/logs";
            if (classification is not null) url += $"?classification={classification.Value}";
            return await _http.GetFromJsonAsync<List<SensorLog>>(url);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    public async Task<(bool Success, string? Error)> DownloadLogAsync(string sensorId, Guid logId, string destinationPath)
    {
        try
        {
            var url = $"/api/sensors/{Uri.EscapeDataString(sensorId)}/logs/{logId}/download";
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                return (false, await ReadErrorAsync(response) ?? $"Download failed with status {response.StatusCode}.");

            await using var source = await response.Content.ReadAsStreamAsync();
            var temporaryPath = destinationPath + ".download";
            try
            {
                await using (var target = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    await source.CopyToAsync(target);
                    await target.FlushAsync();
                }
                File.Move(temporaryPath, destinationPath, overwrite: true);
                return (true, null);
            }
            catch
            {
                try { File.Delete(temporaryPath); } catch (IOException) { }
                throw;
            }
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Could not reach the API: {ex.Message}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return (false, $"Could not save the file: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> DeleteLogAsync(string sensorId, Guid logId)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/sensors/{Uri.EscapeDataString(sensorId)}/logs/{logId}");
            if (response.IsSuccessStatusCode) return (true, null);

            return (false, await ReadErrorAsync(response) ?? $"Delete failed with status {response.StatusCode}.");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Could not reach the API: {ex.Message}");
        }
    }

    public async Task<StreamSnapshot?> GetSnapshotAsync(string? category)
    {
        try
        {
            var url = "/api/stream/snapshot";
            if (!string.IsNullOrWhiteSpace(category)) url += $"?category={Uri.EscapeDataString(category)}";
            return await _http.GetFromJsonAsync<StreamSnapshot>(url);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    public async Task<(bool Success, string? Error, BatchResult? Result)> SendBatchAsync(IEnumerable<TelemetryReading> readings)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/telemetry/batch", readings);
            if (response.IsSuccessStatusCode)
                return (true, null, await response.Content.ReadFromJsonAsync<BatchResult>());

            var problem = await response.Content.ReadFromJsonAsync<ApiError>();
            return (false, problem?.Error ?? $"Request failed with status {response.StatusCode}.", null);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Could not reach the API: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string? Error, OverrideCommand? Command)> OverrideAsync(string sensorId, string command)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/commands/override",
                new OverrideRequest { SensorId = sensorId, Command = command });
            if (response.IsSuccessStatusCode)
                return (true, null, await response.Content.ReadFromJsonAsync<OverrideCommand>());

            var problem = await response.Content.ReadFromJsonAsync<ApiError>();
            return (false, problem?.Error ?? $"Request failed with status {response.StatusCode}.", null);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Could not reach the API: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string? Error, OverrideCommand? Undone)> UndoOverrideAsync()
    {
        try
        {
            var response = await _http.PostAsync("/api/commands/undo", null);
            if (response.IsSuccessStatusCode)
                return (true, null, await response.Content.ReadFromJsonAsync<OverrideCommand>());

            var problem = await response.Content.ReadFromJsonAsync<ApiError>();
            return (false, problem?.Error ?? $"Request failed with status {response.StatusCode}.", null);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Could not reach the API: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string? Error, AlertEntry? Alert)> AcknowledgeAlertAsync()
    {
        try
        {
            var response = await _http.PostAsync("/api/alerts/acknowledge", null);
            if (response.IsSuccessStatusCode)
                return (true, null, await response.Content.ReadFromJsonAsync<AlertEntry>());

            var problem = await response.Content.ReadFromJsonAsync<ApiError>();
            return (false, problem?.Error ?? $"Request failed with status {response.StatusCode}.", null);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Could not reach the API: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string? Error)> ClearAlertsAsync()
    {
        try
        {
            var response = await _http.PostAsync("/api/alerts/clear", null);
            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, $"Request failed with status {response.StatusCode}.");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Could not reach the API: {ex.Message}");
        }
    }
}
