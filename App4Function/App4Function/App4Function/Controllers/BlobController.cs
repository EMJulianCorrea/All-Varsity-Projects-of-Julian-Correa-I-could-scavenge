using App4Function.Service;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class FileUploadController : Controller
{
    private readonly BlobService _blobbyService;

    public FileUploadController(BlobService blobbyService)
    {
        _blobbyService = blobbyService;
    }

    [HttpGet]
    public IActionResult Upload()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ViewBag.Message = "Please select a file.";
            return View();
        }

        var blobUrl = await _blobbyService.UploadFileAsync(file);
        ViewBag.Message = $"File uploaded successfully: {blobUrl}";
        return View();
    }
}
