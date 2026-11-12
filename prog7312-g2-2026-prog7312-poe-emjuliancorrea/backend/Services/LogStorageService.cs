using System.Text;
using System.Text.Json;
using Smart_X.backend.Models;

namespace Smart_X.backend.Services;

public record UploadOutcome(SensorLog? Log, string? Error, int StatusCode);

public class LogStorageService
{
    public const long MaxFileBytes = 10 * 1024 * 1024;

    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".log", ".txt", ".csv", ".json", ".xml", ".cfg", ".conf", ".ini",
        ".yaml", ".yml", ".bin", ".hex", ".fw", ".dat", ".zip"
    };

    private static readonly JsonSerializerOptions IndexJson = new() { WriteIndented = true };

    private readonly string _root;
    private readonly string _indexPath;
    private readonly Dictionary<Guid, SensorLog> _logs = new();
    private readonly object _gate = new();

    public LogStorageService(string root)
    {
        _root = root;
        Directory.CreateDirectory(root);
        _indexPath = Path.Combine(root, "index.json");
        LoadIndex();
    }

    public string GetPath(SensorLog log) => Path.Combine(_root, $"{log.LogId:N}.dat");

    public async Task<UploadOutcome> SaveAsync(string sensorId, IFormFile file, string? requestedClassification)
    {
        if (file is null) return new(null, "No file was supplied.", 400);
        var originalName = SanitizeFileName(file.FileName);
        var ext = Path.GetExtension(originalName);
        if (!AllowedExtensions.Contains(ext))
        {
            var what = ext.Length == 0 ? "Files without an extension" : $"'{ext}' files";
            return new(null, $"{what} are not accepted. Allowed types: {string.Join(", ", AllowedExtensions.Order())}.", 400);
        }
        if (file.Length == 0) return new(null, "The file is empty.", 400);
        if (file.Length > MaxFileBytes)
            return new(null, $"The file is over the {MaxFileBytes / 1024 / 1024} MB limit.", 413);

        string sample;
        await using (var peek = file.OpenReadStream())
        {
            var buffer = new byte[4096];
            var read = await peek.ReadAsync(buffer.AsMemory());
            sample = Encoding.UTF8.GetString(buffer, 0, read);
        }

        LogClassification classification;
        if (string.IsNullOrWhiteSpace(requestedClassification) || requestedClassification.Equals("auto", StringComparison.OrdinalIgnoreCase))
            classification = LogClassifier.Classify(originalName, sample).Classification;
        else if (!TryParseClassification(requestedClassification, out classification))
            return new(null, $"Unknown classification '{requestedClassification}'. Valid values: {string.Join(", ", Enum.GetNames<LogClassification>())}.", 400);

        var log = new SensorLog { SensorId = sensorId, FileName = originalName, SizeInBytes = file.Length, Classification = classification };
        var finalPath = GetPath(log);
        var temporaryPath = finalPath + ".uploading";
        try
        {
            await using (var target = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await file.CopyToAsync(target);
                await target.FlushAsync();
            }
            var actualLength = new FileInfo(temporaryPath).Length;
            if (actualLength == 0 || actualLength > MaxFileBytes)
                throw new InvalidDataException("The uploaded file size changed while it was being read.");
            File.Move(temporaryPath, finalPath);

            lock (_gate)
            {
                _logs[log.LogId] = log;
                try { SaveIndex(); }
                catch
                {
                    _logs.Remove(log.LogId);
                    TryDeleteFile(log);
                    throw;
                }
            }
            return new(log, null, 201);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            try { File.Delete(temporaryPath); } catch (IOException) { }
            return new(null, $"The file could not be stored: {ex.Message}", 500);
        }
    }

    public List<SensorLog> GetForSensor(string sensorId, LogClassification? classification = null)
    {
        lock (_gate)
        {
            return _logs.Values
                .Where(l => string.Equals(l.SensorId, sensorId, StringComparison.OrdinalIgnoreCase) && (classification is null || l.Classification == classification))
                .OrderByDescending(l => l.UploadedAt)
                .ToList();
        }
    }

    public SensorLog? Find(string sensorId, Guid logId)
    {
        lock (_gate)
            return _logs.TryGetValue(logId, out var log) && string.Equals(log.SensorId, sensorId, StringComparison.OrdinalIgnoreCase) ? log : null;
    }

    public bool Delete(string sensorId, Guid logId)
    {
        lock (_gate)
        {
            if (!_logs.TryGetValue(logId, out var log) || !string.Equals(log.SensorId, sensorId, StringComparison.OrdinalIgnoreCase)) return false;

            _logs.Remove(logId);
            TryDeleteFile(log);
            SaveIndex();
            return true;
        }
    }

    public int DeleteAllForSensor(string sensorId)
    {
        lock (_gate)
        {
            var doomed = _logs.Values.Where(l => string.Equals(l.SensorId, sensorId, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var log in doomed)
            {
                _logs.Remove(log.LogId);
                TryDeleteFile(log);
            }

            if (doomed.Count > 0) SaveIndex();
            return doomed.Count;
        }
    }

    public static bool TryParseClassification(string value, out LogClassification result) =>
        Enum.TryParse(value, ignoreCase: true, out result) && Enum.IsDefined(result);

    private static string SanitizeFileName(string? raw)
    {
        var name = Path.GetFileName((raw ?? string.Empty).Replace('\\', '/'));
        name = string.Concat(name.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Trim();
        if (name.Length > 120) name = name[^120..];
        return name.Length == 0 ? "upload" : name;
    }

    private void TryDeleteFile(SensorLog log)
    {
        try { File.Delete(GetPath(log)); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException){}
    }

    private void LoadIndex()
    {
        if (!File.Exists(_indexPath)) return;

        try
        {
            var saved = JsonSerializer.Deserialize<List<SensorLog>>(File.ReadAllText(_indexPath));
            foreach (var log in saved ?? new List<SensorLog>())
                if (File.Exists(GetPath(log))) _logs[log.LogId] = log;
        }
        catch (JsonException)
        {
        }
    }

    private void SaveIndex()
    {
        var temp = _indexPath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(_logs.Values.ToList(), IndexJson));
        File.Move(temp, _indexPath, overwrite: true);
    }
}
