using CoffeeNChill.Functions.Models;


namespace CoffeeNChill.Functions.Repository;

public interface IDocumentRepository
{
    Task UploadAsync(string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default);
    Task<List<StaffDocumentResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<(Stream Content, string ContentType)> DownloadAsync(string fileName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileName, CancellationToken cancellationToken = default);
}
