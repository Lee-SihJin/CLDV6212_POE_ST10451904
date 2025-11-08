using System.Text;
using System.Text.Json;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IFunctionsApi functionsApi, ILogger<OrderController> logger)
        {
            _functionsApi = functionsApi;
            _logger = logger;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            try
            {
                var orders = await _functionsApi.GetAllEntitiesAsync<Order>("Orders");
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                TempData["Error"] = "Error retrieving orders";
                return View(new List<Order>());
            }
        }

        // GET: Create Order
        public async Task<IActionResult> Create()
        {
            try
            {
                var customers = await _functionsApi.GetAllEntitiesAsync<Customer>("Customers");
                var products = await _functionsApi.GetAllEntitiesAsync<Product>("Products");
                var viewModel = new OrderCreateViewModel
                {
                    Customers = customers,
                    Products = products
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create order page");
                TempData["Error"] = "Error loading order form";
                return View(new OrderCreateViewModel());
            }
        }

        // POST: Create Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get customer and product using Functions API
                    var customer = await _functionsApi.GetEntityAsync<Customer>("Customers", "Customer", model.CustomerId);
                    var product = await _functionsApi.GetEntityAsync<Product>("Products", "Product", model.ProductId);
                    if (customer == null || product == null)
                    {
                        ModelState.AddModelError("", "Invalid customer or product selected.");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Check stock
                    if (product.StockAvailable < model.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Create order
                    var order = new Order
                    {
                        CustomerId = model.CustomerId,
                        Username = customer.Username,
                        ProductId = model.ProductId,
                        ProductName = product.ProductName,
                        OrderDate = model.OrderDate,
                        Quantity = model.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * model.Quantity,
                        Status = "Submitted"
                    };

                    await _functionsApi.AddEntityAsync("Orders", order);

                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating order");
                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                }
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // GET: Order Details
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                var order = await _functionsApi.GetEntityAsync<Order>("Orders", "Order", id);
                if (order == null)
                    return NotFound();

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order details for ID: {OrderId}", id);
                TempData["Error"] = "Error retrieving order details";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Edit Order
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                var order = await _functionsApi.GetEntityAsync<Order>("Orders", "Order",id);
                if (order == null)
                    return NotFound();

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit order page for ID: {OrderId}", id);
                TempData["Error"] = "Error loading order for editing";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Edit Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (!ModelState.IsValid)
            {
                return View(order);
            }

            try
            {
                // Get the existing order to ensure 
                var existingOrder = await _functionsApi.GetEntityAsync<Order>("Orders", "Order", order.RowKey);
                if (existingOrder == null)
                {
                    return NotFound();
                }

                // Update only the editable fields
                existingOrder.OrderDate = order.OrderDate;
                existingOrder.Status = order.Status;
                // Preserve all other fields from existingOrder

                await _functionsApi.UpdateEntityAsync("Orders", existingOrder);

                TempData["SuccessMessage"] = "Order updated successfully!";
                return RedirectToAction(nameof(Details), new { id = order.RowKey });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order: {OrderId}", order.RowKey);
                ModelState.AddModelError("", "An error occurred while updating the order.");
                return View(order);
            }
        }

        // POST: Delete Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Invalid order ID";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _functionsApi.DeleteEntityAsync("Orders", "Order", id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order ID: {OrderId}", id);
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX: Get product price
        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _functionsApi.GetEntityAsync<Product>("Products", "Product", productId);
                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.StockAvailable,
                        productName = product.ProductName
                    });
                }
                return Json(new { success = false, message = "Product not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product price for ID: {ProductId}", productId);
                return Json(new { success = false, message = "Error retrieving product information" });
            }
        }

        // GET: Get Order Details for JSON
        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(string id)
        {
            try
            {
                var order = await _functionsApi.GetEntityAsync<Order>("Orders", "Order", id);
                if (order == null)
                {
                    return NotFound();
                }
                return Json(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order details for ID: {OrderId}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: Update Order Status with complete Order object
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] Order order)
        {
            if (order == null || string.IsNullOrEmpty(order.RowKey))
            {
                return Json(new { success = false, message = "Order data is required" });
            }

            try
            {
                // Validate the status
                var validStatuses = new[] { "Submitted", "Processing", "Completed", "Cancelled" };
                if (!validStatuses.Contains(order.Status))
                {
                    return Json(new { success = false, message = "Invalid status value" });
                }

                // Get the existing order to preserve other properties that might not be sent
                var existingOrder = await _functionsApi.GetEntityAsync<Order>("Orders", "Order", order.RowKey);
                if (existingOrder == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                // Update only the status and preserve other properties
                existingOrder.Status = order.Status;

                // Update the entity in table storage
                await _functionsApi.UpdateEntityAsync("Orders", existingOrder);

                return Json(new { success = true, message = $"Order status updated to {order.Status}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for ID: {OrderId} to {Status}", order.RowKey, order.Status);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Helper: populate dropdowns
        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            try
            {
                var customers = await _functionsApi.GetAllEntitiesAsync<Customer>("Customers");
                var products = await _functionsApi.GetAllEntitiesAsync<Product>("Products");

                model.Customers = customers;
                model.Products = products;

                // Also populate ViewBag for dropdowns if needed
                ViewBag.Customers = customers.Select(c => new SelectListItem
                {
                    Value = c.CustomerId,
                    Text = $"{c.Name} {c.Surname} ({c.Username})"
                }).ToList();

                ViewBag.Products = products.Select(p => new SelectListItem
                {
                    Value = p.ProductId,
                    Text = $"{p.ProductName} - ${p.Price} (Stock: {p.StockAvailable})"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating dropdowns");
                model.Customers = new List<Customer>();
                model.Products = new List<Product>();
                ViewBag.Customers = new List<SelectListItem>();
                ViewBag.Products = new List<SelectListItem>();
            }
        }
    }
}