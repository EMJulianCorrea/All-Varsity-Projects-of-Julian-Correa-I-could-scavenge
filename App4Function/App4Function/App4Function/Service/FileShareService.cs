using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System.IO;
using System.Threading.Tasks;
namespace App4Function.Service
{
    public class AzureFileShareService
    {
        private readonly string _connectionString;
        private readonly string _fileShareName;

        public AzureFileShareService(IConfiguration configuration)
        {
            _connectionString = configuration["AzureStorage:ConnectionString"];
            _fileShareName = configuration["AzureStorage:FileShareName"];
        }

        public async Task UploadFileAsync(IFormFile file)
        {
            var shareClient = new ShareClient(_connectionString, _fileShareName);
            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(file.FileName);

            using (var stream = file.OpenReadStream())
            {
                await fileClient.CreateAsync(stream.Length);
                await fileClient.UploadAsync(stream);
            }
        }
    }
}
