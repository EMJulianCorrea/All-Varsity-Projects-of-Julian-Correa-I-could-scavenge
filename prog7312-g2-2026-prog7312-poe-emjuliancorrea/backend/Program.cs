using System.IO;
using Microsoft.AspNetCore.Http.Features;
using Smart_X.backend.Models;
using Smart_X.backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = LogStorageService.MaxFileBytes + 1024 * 1024);

var app = builder.Build();
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "swagger";
});

app.MapGet("/", () => Results.Redirect("/swagger"));

var sensors = new Dictionary<string, SensorRecord>();
var stream = new CommandStreamService(sensors);
var logStore = new LogStorageService(Path.Combine(AppContext.BaseDirectory, "sensor-logs"));
var network = new NetworkTopologyService();

app.MapGet("/api/health", () => Results.Ok(new { status = "OK", time = DateTime.UtcNow }));

app.MapPost("/api/sensors", (SensorRecord incoming) =>
{
    if (string.IsNullOrWhiteSpace(incoming.Id))
        return Results.BadRequest(new { error = "Sensor ID required." });

    lock (sensors)
    {
        if (sensors.ContainsKey(incoming.Id))
            return Results.Conflict(new { error = $"Sensor ID '{incoming.Id}' already exists." });

        if (string.IsNullOrWhiteSpace(incoming.DeploymentLocation))
            return Results.BadRequest(new { error = "Deployment location required." });

        incoming.RegisteredAt = DateTime.UtcNow;
        incoming.LastStatus = "Registered";
        sensors[incoming.Id] = incoming;
    }

    return Results.Created($"/api/sensors/{incoming.Id}", incoming);
});

app.MapGet("/api/sensors", (string? category) =>
{
    lock (sensors)
    {
        var result = sensors.Values
            .Where(s => string.IsNullOrWhiteSpace(category) ||
                        s.Category.ToString().Contains(category.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.RegisteredAt)
            .ToList();
        return Results.Ok(result);
    }
});

app.MapGet("/api/sensors/{id}", (string id) =>
{
    lock (sensors)
        return sensors.TryGetValue(id, out var sensor) ? Results.Ok(sensor) : Results.NotFound();
});

app.MapDelete("/api/sensors/{id}", (string id) =>
{
    lock (sensors)
    {
        if (!sensors.Remove(id)) return Results.NotFound();
        stream.OnSensorRemoved(id);
        logStore.DeleteAllForSensor(id);
        return Results.NoContent();
    }
});

app.MapPut("/api/sensors/{id}", (string id, SensorRecord updated) =>
{
    lock (sensors)
    {
        if (!sensors.TryGetValue(id, out var existing))
            return Results.NotFound(new { error = $"Sensor '{id}' is not registered." });

        if (string.IsNullOrWhiteSpace(updated.DeploymentLocation))
            return Results.BadRequest(new { error = "Deployment location required." });

        existing.DeploymentLocation = updated.DeploymentLocation;
        existing.Category = updated.Category;
        existing.IsSimulated = updated.IsSimulated;

        return Results.Ok(existing);
    }
});

app.MapPost("/api/telemetry", (TelemetryReading reading) =>
{
    var (outcome, error) = stream.Ingest(reading);
    if (outcome == IngestOutcome.Accepted) network.Record(reading);
    return outcome switch
    {
        IngestOutcome.Accepted => Results.Created($"/api/telemetry/{reading.SensorId}", reading),
        IngestOutcome.SensorDisabled => Results.Conflict(new { error }),
        _ => Results.BadRequest(new { error })
    };
});

app.MapPost("/api/telemetry/batch", (List<TelemetryReading> readings) =>
{
    int accepted = 0, rejected = 0;
    var errors = new List<string>();
    foreach (var reading in readings)
    {
        var (outcome, error) = stream.Ingest(reading);
        if (outcome == IngestOutcome.Accepted) { network.Record(reading); accepted++; }
        else { rejected++; if (error is not null && errors.Count < 10 && !errors.Contains(error)) errors.Add(error); }
    }
    return Results.Ok(new BatchResult(accepted, rejected, errors));
});

app.MapGet("/api/telemetry/{sensorId}/range", (string sensorId, DateTime from, DateTime to, int limit = 5000) =>
{
    if (from > to) return Results.BadRequest(new { error = "'from' must be earlier than or equal to 'to'." });
    if (limit < 1 || limit > 10000) return Results.BadRequest(new { error = "'limit' must be between 1 and 10,000." });
    return Results.Ok(network.Query(sensorId, from, to, limit));
});

app.MapGet("/api/network/topology", () => { lock (sensors) return Results.Ok(network.GetTopology(sensors.Values)); });

app.MapPut("/api/network/topology", (MeshTopology topology) =>
{
    try { network.Configure(topology); lock (sensors) return Results.Ok(network.GetTopology(sensors.Values)); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapGet("/api/network/analyze", (string gatewayId) =>
{
    try { lock (sensors) return Results.Ok(network.Analyze(sensors.Values, gatewayId)); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapGet("/api/telemetry/{sensorId}", (string sensorId, int take = 50) =>
    Results.Ok(stream.GetRecent(take, sensorId)));

app.MapGet("/api/telemetry", (int take = 100) =>
    Results.Ok(stream.GetRecent(take)));

app.MapGet("/api/stream/snapshot", (string? category) =>
    Results.Ok(stream.GetSnapshot(category)));

app.MapPost("/api/commands/override", (OverrideRequest request) =>
{
    var (command, error, sensorMissing) = stream.ApplyOverride(request.SensorId, request.Command);
    if (command is not null) return Results.Ok(command);
    return sensorMissing ? Results.NotFound(new { error }) : Results.BadRequest(new { error });
});

app.MapPost("/api/commands/undo", () =>
{
    var undone = stream.UndoLastOverride();
    return undone is null
        ? Results.NotFound(new { error = "There are no overrides to undo." })
        : Results.Ok(undone);
});

app.MapGet("/api/commands/history", (int take = 20) =>
    Results.Ok(stream.GetOverrideHistory(take)));

app.MapPost("/api/alerts/acknowledge", () =>
{
    var alert = stream.AcknowledgeNextAlert();
    return alert is null
        ? Results.NotFound(new { error = "There are no outstanding alerts." })
        : Results.Ok(alert);
});

app.MapPost("/api/alerts/clear", () => Results.Ok(new { cleared = stream.ClearAlerts() }));

app.MapPost("/api/logs/classify", (ClassifyRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.FileName))
        return Results.BadRequest(new { error = "A file name is required." });
    if (request.FileName.Length > 255)
        return Results.BadRequest(new { error = "The file name is too long." });
     var sample = request.Sample is null ? string.Empty : request.Sample[..Math.Min(request.Sample.Length, 4096)];
    return Results.Ok(LogClassifier.Classify(Path.GetFileName(request.FileName), sample));
});

app.MapPost("/api/sensors/{id}/logs", async (string id, HttpRequest request) =>
{
    bool sensorExists;
    lock (sensors) sensorExists = sensors.ContainsKey(id);
    if (!sensorExists)
        return Results.NotFound(new { error = $"Sensor '{id}' is not registered." });

    if (!request.HasFormContentType)
        return Results.BadRequest(new { error = "Expected multipart/form-data with a file field named 'file'." });

    IFormCollection form;
    try
    {
        form = await request.ReadFormAsync();
    }
    catch (InvalidDataException ex) when (ex.Message.Contains("limit", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Json(new { error = $"The upload is over the {LogStorageService.MaxFileBytes / 1024 / 1024} MB limit." }, statusCode: 413);
    }
    catch (InvalidDataException)
    {
        return Results.BadRequest(new { error = "The upload could not be read." });
    }

    var file = form.Files["file"];
    if (file is null)
        return Results.BadRequest(new { error = "No file was provided." });

    var outcome = await logStore.SaveAsync(id, file, form["classification"].ToString());
    if (outcome.Log is null)
        return Results.Json(new { error = outcome.Error }, statusCode: outcome.StatusCode);

    return Results.Created($"/api/sensors/{id}/logs/{outcome.Log.LogId}", outcome.Log);
});

app.MapGet("/api/sensors/{id}/logs", (string id, string? classification) =>
{
    LogClassification? filter = null;
    if (!string.IsNullOrWhiteSpace(classification))
    {
        if (!LogStorageService.TryParseClassification(classification, out var parsed))
            return Results.BadRequest(new { error = $"Unknown classification '{classification}'. Valid values: {string.Join(", ", Enum.GetNames<LogClassification>())}." });
        filter = parsed;
    }

    return Results.Ok(logStore.GetForSensor(id, filter));
});

app.MapGet("/api/sensors/{id}/logs/{logId:guid}/download", (string id, Guid logId) =>
{
    var log = logStore.Find(id, logId);
    if (log is null)
        return Results.NotFound(new { error = "That log was not found for this sensor." });

    var path = logStore.GetPath(log);
    if (!File.Exists(path))
        return Results.NotFound(new { error = "The stored file is missing from the server." });

    return Results.File(path, "application/octet-stream", fileDownloadName: log.FileName);
});

app.MapDelete("/api/sensors/{id}/logs/{logId:guid}", (string id, Guid logId) =>
    logStore.Delete(id, logId)
        ? Results.NoContent()
        : Results.NotFound(new { error = "That log was not found for this sensor." }));

app.MapGet("/api/demo/telemetry-packet", () =>
{
    var temperature = TelemetryPacket.For("TEMP-01", 21.4f, "°C");
    var powerDraw = TelemetryPacket.For("PWR-01", 1450, "W");
    var switchState = TelemetryPacket.For("SWITCH-01", true, "on/off");

    return Results.Ok(new
    {
        temperature = temperature.ToString(),
        powerDraw = powerDraw.ToString(),
        switchState = switchState.ToString()
    });
});

app.MapPost("/api/sensors/aggregate-load", (AggregateLoadRequest request) =>
{
    var a = request.A;
    var b = request.B;
    var aggregate = a + b;
    var difference = a - b;
    var aIsHigher = a > b; 

    return Results.Ok(new
    {
        aggregate = aggregate.ToString(),
        difference = difference.ToString(),
        aIsHigherThanB = aIsHigher
    });
});

var batchStore = new TelemetryBatchStore();
app.MapPost("/api/telemetry/batches/demo", () =>
{
    var readings = new double[,]
    {
        { 21.0, 21.3, 21.1, 21.6 },
        { 44.0, 45.2, 44.8, 46.1 }
    };
    batchStore.AddBatch(new[] { "TEMP-01", "HUM-01" }, readings);

    return Results.Ok(new
    {
        totalBatches = batchStore.BatchCount,
        latestBatch = batchStore.GetBatch(batchStore.BatchCount - 1)
            .Select(s => new { s.SensorId, s.Readings }),
        temp01AverageInLatestBatch = batchStore.GetSensorAverage(batchStore.BatchCount - 1, sensorRow: 0)
    });
});

app.MapPost("/api/deployment/validate", (DeploymentNode root) =>
{
    var isValid = DeploymentTreeValidator.Validate(root, out var errors);
    var totalSensors = DeploymentTreeValidator.CountSensors(root);

    return Results.Ok(new { isValid, errors, totalSensors });
});

app.MapFallback(() => Results.Json(new { error = "Route not found" }, statusCode: 404));

app.Run();
