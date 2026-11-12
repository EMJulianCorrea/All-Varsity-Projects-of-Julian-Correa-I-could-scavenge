using System.Net;
using CoffeeNChill.Functions.Exceptions;
using CoffeeNChill.Functions.Helpers;
using CoffeeNChill.Functions.Repository;
using CoffeeNChill.Functions.Validation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions;

/// <summary>Internal Azure Function; the gateway must enforce Admin/Staff authorization.</summary>
public class DeleteDocumentFunction
{
    private readonly IDocumentRepository _documents;
    private readonly ILogger<DeleteDocumentFunction> _logger;

    public DeleteDocumentFunction(IDocumentRepository documents, ILogger<DeleteDocumentFunction> logger)
    {
        _documents = documents;
        _logger = logger;
    }

    [Function("DeleteStaffDocument")]
    public async Task<HttpResponseData> DeleteStaffDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "documents/{fileName}")] HttpRequestData req,
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            fileName = FileValidator.SanitizeFileName(fileName);
            await _documents.DeleteAsync(fileName, cancellationToken);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (ArgumentException ex)
        {
            return await HttpResponseHelper.BadRequestAsync(req, ex.Message);
        }
        catch (DocumentNotFoundException ex)
        {
            return await HttpResponseHelper.NotFoundAsync(req, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting staff document.");
            return await HttpResponseHelper.ServerErrorAsync(req, "An error occurred while deleting the document.");
        }
    }
}
