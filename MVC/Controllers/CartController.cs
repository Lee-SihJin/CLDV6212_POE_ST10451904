// Controllers/CartController.cs
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly IShoppingCartService _cartService;
        private readonly IAuthService _authService;
        private readonly ILogger<CartController> _logger;

        public CartController(IShoppingCartService cartService, IAuthService authService, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await _cartService.GetCartAsync(userId);
                return View(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading shopping cart");
                TempData["Error"] = "Error loading shopping cart.";
                return View(new ShoppingCart());
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(string productId, string productName, decimal unitPrice, int quantity = 1)
        {
            try
            {
                if (quantity < 1)
                {
                    TempData["Error"] = "Quantity must be at least 1.";
                    return RedirectToAction("Index", "Product");
                }

                var userId = GetCurrentUserId();
                var item = new CartItem
                {
                    ProductId = productId,
                    ProductName = productName,
                    UnitPrice = unitPrice,
                    Quantity = quantity
                };

                await _cartService.AddToCartAsync(userId, item);

                TempData["Success"] = $"{productName} added to cart successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cart", productId);
                TempData["Error"] = "Error adding product to cart.";
            }

            return RedirectToAction("Index", "Store");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(Guid cartItemId, int quantity)
        {
            try
            {
                if (quantity < 0)
                {
                    TempData["Error"] = "Quantity cannot be negative.";
                    return RedirectToAction(nameof(Index));
                }

                await _cartService.UpdateCartItemAsync(cartItemId, quantity);
                TempData["Success"] = "Cart updated successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item {CartItemId}", cartItemId);
                TempData["Error"] = "Error updating cart item.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(Guid cartItemId)
        {
            try
            {
                await _cartService.RemoveFromCartAsync(cartItemId);
                TempData["Success"] = "Item removed from cart!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item {CartItemId}", cartItemId);
                TempData["Error"] = "Error removing item from cart.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                await _cartService.ClearCartAsync(userId);
                TempData["Success"] = "Cart cleared successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user");
                TempData["Error"] = "Error clearing cart.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await _cartService.GetCartAsync(userId);

                if (!cart.Items.Any())
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction(nameof(Index));
                }

                // Get user info for shipping address
                var user = await _authService.GetUserByIdAsync(userId);

                var checkoutModel = new CheckoutViewModel
                {
                    Cart = cart,
                    ShippingAddress = string.Empty,
                    Email = user?.Email ?? string.Empty
                };

                return View(checkoutModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout page");
                TempData["Error"] = "Error loading checkout page.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.ShippingAddress))
                {
                    ModelState.AddModelError("ShippingAddress", "Shipping address is required.");
                }

                if (ModelState.IsValid)
                {
                    var userId = GetCurrentUserId();
                    var order = await _cartService.CheckoutAsync(userId, model.ShippingAddress);

                    TempData["Success"] = $"Order placed successfully! Order ID: {order.OrderId}";
                    return RedirectToAction("OrderConfirmation", new { orderId = order.OrderId });
                }

                // If model state is invalid, reload the cart and return to checkout page
                var cart = await _cartService.GetCartAsync(GetCurrentUserId());
                model.Cart = cart;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout");
                TempData["Error"] = "Error during checkout process. Please try again.";
                return RedirectToAction(nameof(Checkout));
            }
        }

        //[HttpGet]
        //public IActionResult OrderConfirmation(Guid orderId)
        //{
        //    ViewBag.OrderId = orderId;
        //    return View();
        //}

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
            return Guid.Parse(userIdClaim);
        }
        [HttpGet]
        public async Task<JsonResult> GetCartItemCount()
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var userId = GetCurrentUserId();
                    var cart = await _cartService.GetCartAsync(userId);
                    return Json(cart.TotalItems);
                }
                return Json(0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item count");
                return Json(0);
            }
        }
        [HttpGet]
        public async Task<IActionResult> CompleteOrder()
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await _cartService.GetCartAsync(userId);

                if (!cart.Items.Any())
                {
                    TempData["Error"] = "Your cart is empty. Add some items before completing your order.";
                    return RedirectToAction(nameof(Index));
                }

                var user = await _authService.GetUserByIdAsync(userId);
                var model = new CompleteOrderViewModel
                {
                    Cart = cart
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading complete order page");
                TempData["Error"] = "Error loading order completion page.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(CompleteOrderViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Basic validation
                if (string.IsNullOrWhiteSpace(model.ShippingAddress))
                {
                    ModelState.AddModelError("ShippingAddress", "Shipping address is required.");
                }

                if (!ModelState.IsValid)
                {
                    // Reload cart data if validation fails
                    model.Cart = await _cartService.GetCartAsync(userId);
                    return View(model);
                }

                var order = await _cartService.CompleteOrderAsync(userId, model);

                TempData["Success"] = $"Order completed successfully! Your order ID is: {order.OrderId}";
                return RedirectToAction("OrderConfirmation", new { orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing order for user {UserId}", GetCurrentUserId());
                TempData["Error"] = $"Error completing your order: {ex.Message}";

                // Reload cart data
                var userId = GetCurrentUserId();
                model.Cart = await _cartService.GetCartAsync(userId);
                return View(model);
            }
        }


        [HttpGet]
        public async Task<IActionResult> OrderConfirmation(Guid orderId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var order = await _cartService.GetOrderByIdAsync(orderId);

                if (order == null || order.UserId != userId)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order confirmation for order {OrderId}", orderId);
                TempData["Error"] = "Error loading order confirmation.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> OrderHistory()
        {
            try
            {
                var userId = GetCurrentUserId();
                var orders = await _cartService.GetUserOrdersAsync(userId);
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order history");
                TempData["Error"] = "Error loading order history.";
                return View(new List<CustomerOrder>());
            }
        }
    }

    // ViewModel for checkout
    public class CheckoutViewModel
    {
        public ShoppingCart Cart { get; set; } = new ShoppingCart();

        [Required(ErrorMessage = "Shipping address is required.")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty;
    }
}