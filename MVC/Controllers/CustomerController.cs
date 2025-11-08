using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(IFunctionsApi functionsApi, ILogger<CustomerController> logger)
        {
            _functionsApi = functionsApi;
            _logger = logger;
        }

        // List all customers with optional search
        public async Task<IActionResult> Index(string searchTerm = "", bool exactMatch = false)
        {
            try
            {
                var customers = await _functionsApi.GetAllEntitiesAsync<Customer>("Customers");

                // Apply search filter if search term is provided
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    customers = SearchCustomers(customers, searchTerm, exactMatch);
                }

                // Pass search parameters to view
                ViewBag.SearchTerm = searchTerm;
                ViewBag.ExactMatch = exactMatch;

                return View(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                TempData["Error"] = "Error retrieving customers";

                // Ensure ViewBag values are set even on error
                ViewBag.SearchTerm = searchTerm;
                ViewBag.ExactMatch = exactMatch;

                return View(new List<Customer>());
            }
        }
        private List<Customer> SearchCustomers(List<Customer> customers, string searchTerm, bool exactMatch)
        {
            if (exactMatch)
            {
                return customers.Where(c =>
                    c.Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Surname.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Username.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.ShippingAddress.Equals(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }
            else
            {
                return customers.Where(c =>
                    c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Surname.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.ShippingAddress.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }
        }

        // Show create form
        public IActionResult Create()
        {
            return View();
        }

        // Handle create post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var createdCustomer = await _functionsApi.AddEntityAsync<Customer>("Customers",customer);
                    TempData["Success"] = "Customer created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating customer");
                    ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                }
            }

            return View(customer);
        }

        // Show edit form
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Customer ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var customer = await _functionsApi.GetEntityAsync<Customer>("Customers", "Customer", id);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found";
                    return RedirectToAction(nameof(Index));
                }

                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer for editing. ID: {CustomerId}", id);
                TempData["Error"] = "Error loading customer for editing";
                return RedirectToAction(nameof(Index));
            }
        }

        // Handle edit post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(customer.CustomerId))
                    {
                        ModelState.AddModelError("", "Customer ID is required");
                        return View(customer);
                    }

                    var updatedCustomer = await _functionsApi.UpdateEntityAsync("Customers", customer);
                    TempData["Success"] = "Customer updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating customer ID: {CustomerId}", customer.CustomerId);
                    ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                }
            }

            return View(customer);
        }

        // Delete customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Customer ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _functionsApi.DeleteEntityAsync("Customers", "Customer", id);
                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer ID: {CustomerId}", id);
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Customer details
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Customer ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var customer = await _functionsApi.GetEntityAsync<Customer>("Customers", "Customer", id);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found";
                    return RedirectToAction(nameof(Index));
                }

                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer details. ID: {CustomerId}", id);
                TempData["Error"] = "Error retrieving customer details";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}