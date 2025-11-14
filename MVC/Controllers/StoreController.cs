// Controllers/StoreController.cs
using System.Collections.Concurrent;
using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    [AllowAnonymous]
    public class StoreController : Controller
    {
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<StoreController> _logger;

        public StoreController(IFunctionsApi functionsApi, ILogger<StoreController> logger)
        {
            _functionsApi = functionsApi;
            _logger = logger;
        }

        // GET: /Store
        public async Task<IActionResult> Index(string searchTerm = "", string category = "")
        {
            try
            {
                var products = await _functionsApi.GetAllEntitiesAsync<Product>("Products");

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    products = products.Where(p =>
                        p.ProductName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                ViewBag.SearchTerm = searchTerm;
                ViewBag.Category = category;

                return View(products); // This passes IEnumerable<Product> to the view
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product catalog");
                TempData["Error"] = "Error loading products. Please try again.";
                return View(new List<Product>());
            }
        }

        // GET: /Store/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var product = await _functionsApi.GetEntityAsync<Product>("Products", "Product", id);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details for ID: {ProductId}", id);
                TempData["Error"] = "Error loading product details.";
                return RedirectToAction(nameof(Index));
            }
        }

    }
}