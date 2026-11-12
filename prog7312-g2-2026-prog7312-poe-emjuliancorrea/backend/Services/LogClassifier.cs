using Smart_X.backend.Models;

namespace Smart_X.backend.Services;
public static class LogClassifier
{
    private static readonly string[] FirmwareExtensions = { ".bin", ".hex", ".fw" };
    private static readonly string[] ConfigExtensions = { ".cfg", ".conf", ".ini", ".json", ".xml", ".yaml", ".yml" };
    private static readonly string[] ErrorContentMarkers = { "FATAL", "ERROR", "PANIC", "Exception", "Traceback" };

    public static ClassificationResult Classify(string fileName, string? sample)
    {
        var name = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (FirmwareExtensions.Contains(ext))
            return new(LogClassification.Firmware, $"'{ext}' files are firmware images.");

        if (NameHas(name, "error", "fault", "crash", "exception"))
            return new(LogClassification.Error, "the file name mentions an error or fault.");

        if (NameHas(name, "calib"))
            return new(LogClassification.Calibration, "the file name mentions calibration.");

        if (NameHas(name, "firmware", "flash"))
            return new(LogClassification.Firmware, "the file name mentions firmware.");

        if (NameHas(name, "config", "settings") || ConfigExtensions.Contains(ext))
            return new(LogClassification.Configuration, "it looks like a configuration or settings file.");

        if (NameHas(name, "diag", "debug", "trace"))
            return new(LogClassification.Diagnostic, "the file name mentions diagnostics.");

        if (ext == ".csv")
            return new(LogClassification.RawData, "CSV files usually hold raw sensor readings.");

        if (ext is ".log" or ".txt" or ".dat")
        {
            if (!string.IsNullOrEmpty(sample))
            {
                if (ErrorContentMarkers.Any(m => sample.Contains(m, StringComparison.Ordinal)))
                    return new(LogClassification.Error, "the contents include error markers such as ERROR or FATAL.");

                if (sample.Contains("calibrat", StringComparison.OrdinalIgnoreCase))
                    return new(LogClassification.Calibration, "the contents mention calibration.");
            }

            return new(LogClassification.Diagnostic, "plain-text logs are treated as diagnostic logs.");
        }

        return new(LogClassification.Other, "no classification rule matched this file.");
    }

    private static bool NameHas(string name, params string[] terms) => terms.Any(name.Contains);
}
