using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class UploadController : Controller
    {
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IFunctionsApi functionsApi, ILogger<UploadController> logger)
        {
            _functionsApi = functionsApi;
            _logger = logger;
        }

        // GET: Upload/Index
        public IActionResult Index()
        {
            return View(new FileUploadModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            if (ModelState.IsValid && model.ProofOfPayment != null)
            {
                try
                {
                    if (model.ProofOfPayment != null && model.ProofOfPayment.Length > 0)
                    {
                        // Upload to blob storage
                        var fileName = await _functionsApi.UploadBlobAsync(model.ProofOfPayment, "payment-proofs");

                        // upload to file share
                        await _functionsApi.UploadToFileShareAsync(model.ProofOfPayment, "contracts", "payments");

                        TempData["Success"] = $"File uploaded successfully! File name: {fileName}";

                        // Return fresh model for new upload
                        return View(new FileUploadModel());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file");
                    ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
                }
            }

            return View(model);
        }
    
    public class BlobUploadResult
        {
            public string BlobUrl { get; set; }
        }
    }
}
