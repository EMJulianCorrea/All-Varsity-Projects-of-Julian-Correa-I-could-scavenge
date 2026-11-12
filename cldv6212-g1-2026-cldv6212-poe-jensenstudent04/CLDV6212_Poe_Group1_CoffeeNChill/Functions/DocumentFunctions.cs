using System.Net;
using CoffeeNChill.Functions.Exceptions;
using CoffeeNChill.Functions.Helpers;
using CoffeeNChill.Functions.Repository;
using CoffeeNChill.Functions.Validation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions;

public class DocumentFunctions
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<DocumentFunctions> _logger;

    public DocumentFunctions(IDocumentRepository documentRepository, ILogger<DocumentFunctions> logger)
    {
        _documentRepository = documentRepository;
        _logger = logger;
    }

    [Function("UploadStaffDocument")]
    public async Task<HttpResponseData> UploadStaffDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents/upload")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!req.Headers.TryGetValues("X-File-Name", out var fileNameValues))
            {
                return await HttpResponseHelper.BadRequestAsync(req, "X-File-Name header is required.");
            }

            string fileName = FileValidator.SanitizeFileName(fileNameValues.FirstOrDefault() ?? string.Empty);

            string contentType = FileValidator.NormalizeContentType(
                req.Headers.TryGetValues("Content-Type", out var contentTypeValues)
                    ? contentTypeValues.FirstOrDefault()
                    : null);

            if (!req.Headers.TryGetValues("Content-Length", out var contentLengthValues))
            {
                return await HttpResponseHelper.BadRequestAsync(req, "Content-Length header is required.");
            }

            if (!long.TryParse(contentLengthValues.FirstOrDefault(), out long fileSize))
            {
                return await HttpResponseHelper.BadRequestAsync(req, "Invalid Content-Length.");
            }

            // 1. Header-level checks (extension, MIME type, declared size)
            FileValidator.Validate(fileName, contentType, fileSize);

            // 2. Content-level checks, done WHILE streaming:
            //    - peek the first 5 bytes for the %PDF- signature
            //    - count the real bytes and enforce the 10 MB cap
            Stream validatedStream = await StreamHelper.ReadAndValidatePdfHeaderAsync(req.Body, cancellationToken);
            Stream limitedStream = new MaxLengthStream(validatedStream, FileValidator.MaxFileSize);

            // 3. Stream straight into Blob Storage (no full-file buffering)
            await _documentRepository.UploadAsync(fileName, limitedStream, contentType, cancellationToken);

            _logger.LogInformation("Staff document uploaded successfully: {FileName}", fileName);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new { success = true, message = "Staff document uploaded successfully.", fileName });
            response.StatusCode = HttpStatusCode.Created;
            return response;
        }
        catch (ArgumentException ex)
        {
            return await HttpResponseHelper.BadRequestAsync(req, ex.Message);
        }
        catch (DocumentAlreadyExistsException ex)
        {
            return await HttpResponseHelper.ConflictAsync(req, ex.Message);
        }
        catch (Exception ex) when (ex.GetBaseException() is ArgumentException inner)
        {
            // The SDK may wrap an exception thrown by our stream wrappers.
            return await HttpResponseHelper.BadRequestAsync(req, inner.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading staff document.");
            return await HttpResponseHelper.ServerErrorAsync(req, "An error occurred while uploading the document.");
        }
    }

    [Function("ListStaffDocuments")]
    public async Task<HttpResponseData> ListStaffDocuments(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        try
        {
            var documents = await _documentRepository.ListAsync(cancellationToken);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(documents);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing staff documents.");
            return await HttpResponseHelper.ServerErrorAsync(req, "An error occurred while retrieving documents.");
        }
    }

    [Function("DownloadStaffDocument")]
    public async Task<HttpResponseData> DownloadStaffDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents/download/{fileName}")] HttpRequestData req,
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            fileName = ModelValidator.ValidateRequired(fileName, "File name");
            fileName = FileValidator.SanitizeFileName(fileName);

            // A 404 is thrown here, BEFORE any response headers are written.
            var result = await _documentRepository.DownloadAsync(fileName, cancellationToken);

            await using Stream content = result.Content;

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", result.ContentType);
            response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");

            _logger.LogInformation("Streaming staff document to client: {FileName}", fileName);

            // STREAM-BASED DOWNLOAD: copy blob -> response in 80 KB chunks.
            await content.CopyToAsync(response.Body, 81920, cancellationToken);
            return response;
        }
        catch (DocumentNotFoundException ex)
        {
            return await HttpResponseHelper.NotFoundAsync(req, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return await HttpResponseHelper.BadRequestAsync(req, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading staff document.");
            return await HttpResponseHelper.ServerErrorAsync(req, "An error occurred while downloading the document.");
        }
    }
}
