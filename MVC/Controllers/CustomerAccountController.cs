// Controllers/CustomerAccountController.cs
using System.Security.Claims;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerAccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ILogger<CustomerAccountController> _logger;

        public CustomerAccountController(
            IAuthService authService,
            IShoppingCartService shoppingCartService,
            ILogger<CustomerAccountController> logger)
        {
            _authService = authService;
            _shoppingCartService = shoppingCartService;
            _logger = logger;
        }

        // GET: /CustomerAccount/MyAccount
        public async Task<IActionResult> MyAccount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetUserByIdAsync(userId);
                var profile = await _authService.GetUserProfileAsync(userId);
                var orders = await _shoppingCartService.GetUserOrdersAsync(userId);

                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("Index", "Home");
                }

                var viewModel = new CustomerInfoViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = profile?.PhoneNumber,
                    ShippingAddress = profile?.ShippingAddress,
                    DateOfBirth = profile?.DateOfBirth,
                    CreatedDate = user.CreatedDate,
                    LastLoginDate = user.LastLoginDate,
                    TotalOrders = orders.Count,
                    TotalSpent = orders.Sum(o => o.TotalAmount),
                    PendingOrders = orders.Count(o => o.Status == "Submitted" || o.Status == "Processing")
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer account for user {UserId}", GetCurrentUserId());
                TempData["Error"] = "Error loading account information.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /CustomerAccount/EditProfile
        public async Task<IActionResult> EditProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetUserByIdAsync(userId);
                var profile = await _authService.GetUserProfileAsync(userId);

                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("MyAccount");
                }

                var viewModel = new UpdateCustomerProfileModel
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = profile?.PhoneNumber,
                    ShippingAddress = profile?.ShippingAddress ?? string.Empty,
                    DateOfBirth = profile?.DateOfBirth
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit profile page for user {UserId}", GetCurrentUserId());
                TempData["Error"] = "Error loading profile editor.";
                return RedirectToAction("MyAccount");
            }
        }

        // POST: /CustomerAccount/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(UpdateCustomerProfileModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = GetCurrentUserId();
                var success = await _authService.UpdateUserProfileAsync(userId, model);

                if (success)
                {
                    TempData["Success"] = "Profile updated successfully!";
                    return RedirectToAction("MyAccount");
                }
                else
                {
                    TempData["Error"] = "Failed to update profile. Please try again.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", GetCurrentUserId());
                ModelState.AddModelError("", "An error occurred while updating your profile. Please try again.");
                return View(model);
            }
        }

        // GET: /CustomerAccount/OrderHistory
        public async Task<IActionResult> OrderHistory()
        {
            try
            {
                var userId = GetCurrentUserId();
                var orders = await _shoppingCartService.GetUserOrdersAsync(userId);
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order history for user {UserId}", GetCurrentUserId());
                TempData["Error"] = "Error loading order history.";
                return View(new List<CustomerOrder>());
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
            return Guid.Parse(userIdClaim);
        }
    }
}