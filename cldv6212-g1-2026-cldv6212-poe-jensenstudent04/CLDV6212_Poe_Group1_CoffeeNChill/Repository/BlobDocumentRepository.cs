using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CoffeeNChill.Functions.Exceptions;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Repository;

/* Blob Storage implementation of IDocumentRepository. All storage access for staff
   documents lives here, so the functions never touch the Azure SDK directly
   (clean separation of storage concerns). */
public class BlobDocumentRepository : IDocumentRepository
{
    // One container client, created once and reused for every request (no new clients per call)
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobDocumentRepository> _logger;

    // BlobServiceClient is injected from Program.cs, where it is registered once as a singleton
    public BlobDocumentRepository(BlobServiceClient blobServiceClient, ILogger<BlobDocumentRepository> logger)
    {
        _logger = logger;

        // All staff documents are stored in the "staff-docs" container
        _containerClient = blobServiceClient.GetBlobContainerClient("staff-docs");

        // Creates the container the first time if it doesn't exist (safe to call repeatedly)
        _containerClient.CreateIfNotExists();
    }

    // STREAM-BASED UPLOAD: the request stream goes straight to the Blob SDK, which sends it
    // in blocks. The file is never loaded into a byte[] / MemoryStream.
    public async Task UploadAsync(string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = _containerClient.GetBlobClient(fileName);

        // Log the file name only - never the file contents
        _logger.LogInformation("Streaming staff document to Blob Storage: {FileName}", fileName);

        var options = new BlobUploadOptions
        {
            // Store the MIME type with the blob so it is returned correctly on download
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },

            // Create only - never silently overwrite an existing document.
            // IfNoneMatch = ETag.All means "only succeed if no blob with this name exists yet".
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
        };

        try
        {
            // The SDK reads the stream in chunks while sending it, so memory use stays small
            await blobClient.UploadAsync(fileStream, options, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 409 || ex.Status == 412)
        {
            // 409/412 means a blob with this name already exists. Translate the SDK error into
            // our own exception so the function can return a clean 409 Conflict to the client.
            _logger.LogWarning("Staff document already exists: {FileName}", fileName);
            throw new DocumentAlreadyExistsException(fileName);
        }
    }

    // Lists every document with its name, size and last-modified time
    public async Task<List<StaffDocumentResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var documents = new List<StaffDocumentResponse>();

        // await foreach pages through the blobs asynchronously instead of loading them all at once
        await foreach (BlobItem blob in _containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            documents.Add(new StaffDocumentResponse
            {
                FileName = blob.Name,

                // Size can be null, so default to 0 (metadata tracking)
                Size = blob.Properties.ContentLength ?? 0,

                // "O" gives an ISO 8601 timestamp that clients can parse reliably
                LastModified = blob.Properties.LastModified?.ToString("O") ?? string.Empty
            });
        }

        return documents;
    }

    // STREAM-BASED DOWNLOAD: returns the network stream from Blob Storage. The function
    // copies it to the HTTP response in chunks and disposes it.
    public async Task<(Stream Content, string ContentType)> DownloadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = _containerClient.GetBlobClient(fileName);

        try
        {
            // DownloadStreamingAsync gives us a stream we read as it arrives,
            // instead of downloading the whole file into memory first
            BlobDownloadStreamingResult result = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

            // Fall back to a generic type if none was stored with the blob
            string contentType = result.Details.ContentType ?? "application/octet-stream";

            return (result.Content, contentType);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // The blob doesn't exist: convert to our own exception so the function returns 404
            _logger.LogWarning("Staff document not found: {FileName}", fileName);
            throw new DocumentNotFoundException(fileName);
        }
    }
    // Delete the named blob without loading its contents into memory.
    public async Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = _containerClient.GetBlobClient(fileName);
        var result = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        if (!result.Value)
        {
            _logger.LogWarning("Staff document not found for deletion: {FileName}", fileName);
            throw new DocumentNotFoundException(fileName);
        }
        _logger.LogInformation("Deleted staff document: {FileName}", fileName);
    }
}
