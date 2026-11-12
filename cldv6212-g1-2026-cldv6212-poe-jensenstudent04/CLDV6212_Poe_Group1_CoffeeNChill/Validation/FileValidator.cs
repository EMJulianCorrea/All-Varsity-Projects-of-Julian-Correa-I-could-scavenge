namespace CoffeeNChill.Functions.Validation;

public static class FileValidator
{
    public const long MaxFileSize = 10 * 1024 * 1024;
    private const int MaxFileNameLength = 150;

    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

    private static readonly HashSet<string> AllowedExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf" };

    private static readonly HashSet<string> AllowedMimeTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/pdf" };

    /// <summary>
    /// Rejects path segments / unsafe characters so a client can't target other blobs or
    /// inject quotes into the Content-Disposition header. (Path.GetInvalidFileNameChars is
    /// almost empty on Linux, i.e. inside the Docker container, so we check explicitly.)
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A file name is required.");
        }

        string name = fileName.Trim();

        if (name.Length > MaxFileNameLength)
        {
            throw new ArgumentException($"The file name cannot be longer than {MaxFileNameLength} characters.");
        }

        bool unsafeName =
            name.Any(c => char.IsControl(c)
                || c == '/' || c == '\\' || c == '"' || c == ':'
                || c == '*' || c == '?' || c == '<' || c == '>' || c == '|')
            || name.Contains("..");

        if (unsafeName)
        {
            throw new ArgumentException("The file name contains invalid characters.");
        }

        return name;
    }

    /// <summary>Strips parameters, e.g. "application/pdf; charset=binary" -> "application/pdf".</summary>
    public static string NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return "application/octet-stream";
        }

        return contentType.Split(';')[0].Trim();
    }

    public static void Validate(string fileName, string contentType, long fileSize)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A file name is required.");
        }

        string extension = Path.GetExtension(fileName);

        if (!AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Invalid file type. Only PDF documents are allowed.");
        }

        if (!AllowedMimeTypes.Contains(contentType))
        {
            throw new ArgumentException("Invalid MIME type. Only application/pdf is allowed.");
        }

        if (fileSize <= 0)
        {
            throw new ArgumentException("The uploaded file cannot be empty.");
        }

        if (fileSize > MaxFileSize)
        {
            throw new ArgumentException("The uploaded file cannot be larger than 10 MB.");
        }
    }

    /// <summary>Checks the real content starts with the "%PDF-" magic bytes.</summary>
    public static void ValidatePdfSignature(byte[] header, int length)
    {
        if (length < PdfSignature.Length ||
            !header.AsSpan(0, PdfSignature.Length).SequenceEqual(PdfSignature))
        {
            throw new ArgumentException("The file content is not a valid PDF document.");
        }
    }
}
