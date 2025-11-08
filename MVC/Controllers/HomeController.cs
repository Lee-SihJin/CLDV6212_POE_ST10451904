using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IFunctionsApi functionsApi, ILogger<HomeController> logger)
        {
            _functionsApi = functionsApi;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _functionsApi.GetAllEntitiesAsync<Product>("Products");
                var customers = await _functionsApi.GetAllEntitiesAsync<Customer>("Customers");
                var orders = await _functionsApi.GetAllEntitiesAsync<Order>("Orders");
                var viewModel = new HomeViewModel
                {
                    FeaturedProducts = products.Take(5).ToList(),
                    ProductCount = products.Count,
                    CustomerCount = customers.Count,
                    OrderCount = orders.Count
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page data");

                var viewModel = new HomeViewModel
                {
                    FeaturedProducts = new List<Product>(),
                    ProductCount = 0,
                    CustomerCount = 0,
                    OrderCount = 0
                };

                TempData["Error"] = "Unable to load dashboard data. Please check if Azure Functions are running.";
                return View(viewModel);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> InitializeStorage()
        {
            try
            {
                // Test connectivity to all services
                var products = await _functionsApi.GetAllEntitiesAsync<Product>("Products");
                var customers = await _functionsApi.GetAllEntitiesAsync<Customer>("Customers");
                var orders = await _functionsApi.GetAllEntitiesAsync<Order>("Orders");

                TempData["Success"] = $"Azure Functions connected successfully! Loaded {products.Count} products, {customers.Count} customers, and {orders.Count} orders.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing storage connectivity");
                TempData["Error"] = $"Failed to connect to Azure Functions: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }


        public IActionResult Health()
        {
            try
            {
                return Json(new
                {
                    status = "Healthy",
                    timestamp = DateTime.UtcNow,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in health check");
                return Json(new
                {
                    status = "Unhealthy",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}