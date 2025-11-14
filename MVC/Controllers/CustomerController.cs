// CustomerController.cs
using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class CustomerController : Controller
    {
        private readonly ISqlDataService _sqlDataService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(ISqlDataService sqlDataService, ILogger<CustomerController> logger)
        {
            _sqlDataService = sqlDataService;
            _logger = logger;
        }

        // List all customers with optional search
        public async Task<IActionResult> Index(string searchTerm = "", bool exactMatch = false)
        {
            try
            {
                // Get all users with Customer role
                var customers = await _sqlDataService.GetUsersByRoleAsync("Customer");

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

                return View(new List<User>());
            }
        }

        private List<User> SearchCustomers(List<User> customers, string searchTerm, bool exactMatch)
        {
            if (exactMatch)
            {
                return customers.Where(c =>
                    c.FirstName.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.LastName.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Username.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Equals(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }
            else
            {
                return customers.Where(c =>
                    c.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
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
        public async Task<IActionResult> Create(User user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Set default values for new customer
                    user.Role = "Customer";
                    user.IsActive = true;

                    // Check if username already exists
                    var existingUser = await _sqlDataService.GetUserByUsernameAsync(user.Username);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Username", "Username already exists. Please choose a different username.");
                        return View(user);
                    }

                    // Check if email already exists
                    var existingEmail = await _sqlDataService.GetUserByEmailAsync(user.Email);
                    if (existingEmail != null)
                    {
                        ModelState.AddModelError("Email", "Email address already exists. Please use a different email.");
                        return View(user);
                    }

                    // In a real application, you should hash the password properly
                    // For now, we'll store it as plain text (NOT RECOMMENDED FOR PRODUCTION)
                    user.PasswordHash = user.PasswordHash; // You should hash this: BCrypt.Net.BCrypt.HashPassword(user.PasswordHash)

                    var createdUser = await _sqlDataService.CreateUserAsync(user);
                    TempData["Success"] = "Customer created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating customer");
                    ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                }
            }

            return View(user);
        }

        // Show edit form
        public async Task<IActionResult> Edit(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = "Customer ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var customer = await _sqlDataService.GetUserByIdAsync(id);
                if (customer == null || customer.Role != "Customer")
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
        public async Task<IActionResult> Edit(User user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (user.UserId == Guid.Empty)
                    {
                        ModelState.AddModelError("", "Customer ID is required");
                        return View(user);
                    }

                    // Get existing user to preserve password if not changed
                    var existingUser = await _sqlDataService.GetUserByIdAsync(user.UserId);
                    if (existingUser == null)
                    {
                        ModelState.AddModelError("", "Customer not found");
                        return View(user);
                    }

                    // Check if username already exists (excluding current user)
                    var existingUsername = await _sqlDataService.GetUserByUsernameAsync(user.Username);
                    if (existingUsername != null && existingUsername.UserId != user.UserId)
                    {
                        ModelState.AddModelError("Username", "Username already exists. Please choose a different username.");
                        return View(user);
                    }

                    // Check if email already exists (excluding current user)
                    var existingEmail = await _sqlDataService.GetUserByEmailAsync(user.Email);
                    if (existingEmail != null && existingEmail.UserId != user.UserId)
                    {
                        ModelState.AddModelError("Email", "Email address already exists. Please use a different email.");
                        return View(user);
                    }

                    // Preserve password if not changed (in real app, you'd check if password field was modified)
                    if (string.IsNullOrEmpty(user.PasswordHash))
                    {
                        user.PasswordHash = existingUser.PasswordHash;
                    }

                    // Ensure role remains as Customer
                    user.Role = "Customer";

                    var updatedCustomer = await _sqlDataService.UpdateUserAsync(user);
                    TempData["Success"] = "Customer updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating customer ID: {CustomerId}", user.UserId);
                    ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                }
            }

            return View(user);
        }

        // Delete customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = "Customer ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _sqlDataService.DeleteUserAsync(id);
                if (result)
                {
                    TempData["Success"] = "Customer deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Customer not found or could not be deleted";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer ID: {CustomerId}", id);
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Customer details
        public async Task<IActionResult> Details(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = "Customer ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var customer = await _sqlDataService.GetUserByIdAsync(id);
                if (customer == null || customer.Role != "Customer")
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

        // Toggle customer active status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = "Customer ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var customer = await _sqlDataService.GetUserByIdAsync(id);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found";
                    return RedirectToAction(nameof(Index));
                }

                var newStatus = !customer.IsActive;
                var result = await _sqlDataService.ToggleUserActiveStatusAsync(id, newStatus);

                if (result)
                {
                    TempData["Success"] = $"Customer {(newStatus ? "activated" : "deactivated")} successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to update customer status";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling customer status. ID: {CustomerId}", id);
                TempData["Error"] = $"Error updating customer status: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Change customer role (if needed for promotions, etc.)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeRole(Guid id, string newRole)
        {
            if (id == Guid.Empty || string.IsNullOrEmpty(newRole))
            {
                TempData["Error"] = "Customer ID and role are required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _sqlDataService.ChangeUserRoleAsync(id, newRole);
                if (result)
                {
                    TempData["Success"] = $"Customer role changed to {newRole} successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to change customer role";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing customer role. ID: {CustomerId}, Role: {NewRole}", id, newRole);
                TempData["Error"] = $"Error changing customer role: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}