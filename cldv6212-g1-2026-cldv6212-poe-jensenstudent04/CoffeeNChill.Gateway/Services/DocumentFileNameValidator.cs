namespace CoffeeNChill.Gateway.Services
{
    /// <summary>Validates a single document filename before it is used in document routes.</summary>
    public static class DocumentFileNameValidator
    {
        public static bool IsValid(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            if (fileName == "." || fileName == "..")
                return false;

            if (fileName.Contains('/') || fileName.Contains('\\'))
                return false;

            if (fileName.Contains(".."))
                return false;

            if (fileName.IndexOfAny(new[] { '<', '>', ':', '"', '|', '?', '*', '\0' }) >= 0)
                return false;

            return true;
        }
    }
}
