using App4Function.Service;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace App4Function.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly AzureFileShareService _fileShareService;

        public FileUploadController(AzureFileShareService fileShareService)
        {
            _fileShareService = fileShareService;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                await _fileShareService.UploadFileAsync(file);
                ViewBag.Message = "File uploaded successfully!";
            }
            else
            {
                ViewBag.Message = "Please select a file to upload.";
            }

            return View();
        }
    }
}
