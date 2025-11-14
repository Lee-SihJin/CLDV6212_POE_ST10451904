// Controllers/OrderController.cs
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ABCRetailers.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, IFunctionsApi functionsApi, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _functionsApi = functionsApi;
            _logger = logger;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders from SQL database");
                TempData["Error"] = "Error retrieving orders";
                return View(new List<CustomerOrder>());
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

                await PopulateDropdowns(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create order page");
                TempData["Error"] = "Error loading order form";
                return View(new OrderCreateViewModel());
            }
        }


        // GET: Order Details
        public async Task<IActionResult> Details(Guid id)
        {
            if (id == Guid.Empty)
                return NotFound();

            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
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
        public async Task<IActionResult> Edit(Guid id)
        {
            if (id == Guid.Empty)
                return NotFound();

            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
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
        public async Task<IActionResult> Edit(Guid id, CustomerOrder order)
        {
            if (id != order.OrderId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                return View(order);
            }

            try
            {
                // Create edit view model with only editable fields
                var editModel = new OrderEditViewModel
                {
                    OrderDate = order.OrderDate,
                    Status = order.Status
                };

                var updatedOrder = await _orderService.UpdateOrderAsync(id, editModel);

                TempData["Success"] = "Order updated successfully!";
                return RedirectToAction(nameof(Details), new { id = updatedOrder.OrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order: {OrderId}", order.OrderId);
                ModelState.AddModelError("", "An error occurred while updating the order.");
                return View(order);
            }
        }

        // POST: Delete Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = "Invalid order ID";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var success = await _orderService.DeleteOrderAsync(id);
                if (success)
                {
                    TempData["Success"] = "Order deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Order not found or could not be deleted";
                }
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
        public async Task<IActionResult> GetOrderDetails(Guid id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
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

        // POST: Update Order Status
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] OrderStatusUpdateModel model)
        {
            if (model == null || model.OrderId == Guid.Empty || string.IsNullOrEmpty(model.Status))
            {
                return Json(new { success = false, message = "Order ID and status are required" });
            }

            try
            {
                var success = await _orderService.UpdateOrderStatusAsync(model.OrderId, model.Status);
                if (success)
                {
                    return Json(new { success = true, message = $"Order status updated to {model.Status}" });
                }
                else
                {
                    return Json(new { success = false, message = "Order not found or could not be updated" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for ID: {OrderId} to {Status}", model.OrderId, model.Status);
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

                // Also populate ViewBag for dropdowns
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

    // Model for status updates
    public class OrderStatusUpdateModel
    {
        public Guid OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}