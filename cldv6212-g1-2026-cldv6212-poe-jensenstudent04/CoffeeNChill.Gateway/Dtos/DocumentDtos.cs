namespace CoffeeNChill.Gateway.Dtos;

/// <summary>A staff document stored in Blob Storage.</summary>
public class DocumentDto
{
    public string FileName { get; set; } = "";
    /// <summary>Size in bytes.</summary>
    public long Size { get; set; }
    public string LastModified { get; set; } = "";
}

/// <summary>Returned after a successful upload.</summary>
/// <param name="FileName">The sanitised name the document was stored under.</param>
public record DocumentUploadResponse(string FileName);
