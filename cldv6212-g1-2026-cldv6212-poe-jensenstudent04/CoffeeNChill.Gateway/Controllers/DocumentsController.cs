using CoffeeNChill.Gateway.Dtos;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

/// <summary>Staff PDF documents (Blob Storage). Every endpoint needs the Admin or Staff role.</summary>
[ApiController]
[Route("api/documents")]
[Authorize(Roles = Roles.AdminOrStaff)]
public class DocumentsController(FunctionsClient functions) : ControllerBase
{
    /// <summary>Upload a PDF.</summary>
    /// <remarks>The file is streamed straight through to Blob Storage; the Functions app checks the extension, MIME type, %PDF signature and 10 MB limit.</remarks>
    /// <param name="file">The PDF to upload.</param>
    /// <response code="201">Uploaded.</response>
    /// <response code="400">No file, or the file failed validation.</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="403">The caller is a Customer.</response>
    /// <response code="409">A document with this name already exists.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Problem(detail: "Choose a PDF file to upload.", statusCode: StatusCodes.Status400BadRequest);

        await using var stream = file.OpenReadStream();
        var storedName = await functions.UploadDocumentAsync(
            Path.GetFileName(file.FileName), stream, file.Length, file.ContentType, ct);

        return StatusCode(StatusCodes.Status201Created, new DocumentUploadResponse(storedName));
    }

    /// <summary>List the stored documents.</summary>
    /// <response code="200">File name, size and last-modified time of each document.</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="403">The caller is a Customer.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(List<DocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<DocumentDto>>> List(CancellationToken ct)
        => Ok(await functions.ListDocumentsAsync(ct));

    /// <summary>Download a document.</summary>
    /// <param name="fileName">The stored file name, e.g. menu-policy.pdf.</param>
    /// <response code="200">The PDF, streamed.</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="403">The caller is a Customer.</response>
    /// <response code="404">No such document.</response>
    [HttpGet("{fileName}")]
    [Produces("application/pdf", "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(string fileName, CancellationToken ct)
    {
        var upstream = await functions.OpenDocumentAsync(fileName, ct);
        // Dispose the upstream response only once our response has been fully written.
        Response.RegisterForDispose(upstream);

        var stream = await upstream.Content.ReadAsStreamAsync(ct);
        var contentType = upstream.Content.Headers.ContentType?.ToString() ?? "application/pdf";
        return File(stream, contentType, fileName);
    }

    /// <summary>Delete a document.</summary>
    /// <param name="fileName">The stored file name.</param>
    /// <response code="204">Deleted.</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="403">The caller is a Customer.</response>
    /// <response code="404">No such document.</response>
    [HttpDelete("{fileName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string fileName, CancellationToken ct)
    {
        await functions.DeleteDocumentAsync(fileName, ct);
        return NoContent();
    }
}
