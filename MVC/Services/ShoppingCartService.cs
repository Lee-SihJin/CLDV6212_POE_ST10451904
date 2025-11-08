// Services/ShoppingCartService.cs
using System.Data.SqlClient;
using ABCRetailers.Models;
using Dapper;

namespace ABCRetailers.Services
{
    public class ShoppingCartService : IShoppingCartService
    {
        private readonly string _connectionString;
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<ShoppingCartService> _logger;

        public ShoppingCartService(IConfiguration configuration, IFunctionsApi functionsApi, ILogger<ShoppingCartService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _functionsApi = functionsApi;
            _logger = logger;
        }

        public async Task<ShoppingCart> GetCartAsync(Guid userId)
        {
            using var connection = new SqlConnection(_connectionString);

            var cart = await connection.QueryFirstOrDefaultAsync<ShoppingCart>(
                "SELECT * FROM ShoppingCart WHERE UserId = @UserId",
                new { UserId = userId });

            if (cart == null)
            {
                cart = new ShoppingCart
                {
                    CartId = Guid.NewGuid(),
                    UserId = userId,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                };

                await connection.ExecuteAsync(
                    "INSERT INTO ShoppingCart (CartId, UserId, CreatedDate, LastModifiedDate) VALUES (@CartId, @UserId, @CreatedDate, @LastModifiedDate)",
                    cart);
            }

            // Get cart items
            var items = await connection.QueryAsync<CartItem>(
                "SELECT * FROM CartItems WHERE CartId = @CartId ORDER BY AddedDate DESC",
                new { cart.CartId });

            cart.Items = items.ToList();
            return cart;
        }

        public async Task AddToCartAsync(Guid userId, CartItem item)
        {
            var cart = await GetCartAsync(userId);

            using var connection = new SqlConnection(_connectionString);

            // Check if item already exists in cart
            var existingItem = await connection.QueryFirstOrDefaultAsync<CartItem>(
                "SELECT * FROM CartItems WHERE CartId = @CartId AND ProductId = @ProductId",
                new { cart.CartId, item.ProductId });

            if (existingItem != null)
            {
                // Update quantity
                await connection.ExecuteAsync(
                    "UPDATE CartItems SET Quantity = Quantity + @Quantity, UnitPrice = @UnitPrice WHERE CartItemId = @CartItemId",
                    new { item.Quantity, item.UnitPrice, existingItem.CartItemId });

                _logger.LogInformation("Updated quantity for product {ProductId} in cart", item.ProductId);
            }
            else
            {
                // Add new item
                item.CartItemId = Guid.NewGuid();
                item.CartId = cart.CartId;
                item.AddedDate = DateTime.UtcNow;

                await connection.ExecuteAsync(
                    @"INSERT INTO CartItems (CartItemId, CartId, ProductId, ProductName, Quantity, UnitPrice, AddedDate) 
                     VALUES (@CartItemId, @CartId, @ProductId, @ProductName, @Quantity, @UnitPrice, @AddedDate)",
                    item);

                _logger.LogInformation("Added product {ProductId} to cart", item.ProductId);
            }

            // Update cart last modified date
            await connection.ExecuteAsync(
                "UPDATE ShoppingCart SET LastModifiedDate = @LastModifiedDate WHERE CartId = @CartId",
                new { LastModifiedDate = DateTime.UtcNow, cart.CartId });
        }

        public async Task UpdateCartItemAsync(Guid cartItemId, int quantity)
        {
            using var connection = new SqlConnection(_connectionString);

            if (quantity <= 0)
            {
                await RemoveFromCartAsync(cartItemId);
            }
            else
            {
                await connection.ExecuteAsync(
                    "UPDATE CartItems SET Quantity = @Quantity WHERE CartItemId = @CartItemId",
                    new { Quantity = quantity, CartItemId = cartItemId });

                // Update cart last modified date
                var cartId = await connection.ExecuteScalarAsync<Guid>(
                    "SELECT CartId FROM CartItems WHERE CartItemId = @CartItemId",
                    new { CartItemId = cartItemId });

                await connection.ExecuteAsync(
                    "UPDATE ShoppingCart SET LastModifiedDate = @LastModifiedDate WHERE CartId = @CartId",
                    new { LastModifiedDate = DateTime.UtcNow, CartId = cartId });

                _logger.LogInformation("Updated quantity for cart item {CartItemId} to {Quantity}", cartItemId, quantity);
            }
        }

        public async Task RemoveFromCartAsync(Guid cartItemId)
        {
            using var connection = new SqlConnection(_connectionString);

            // Get cart ID before deletion for updating last modified date
            var cartId = await connection.ExecuteScalarAsync<Guid?>(
                "SELECT CartId FROM CartItems WHERE CartItemId = @CartItemId",
                new { CartItemId = cartItemId });

            await connection.ExecuteAsync(
                "DELETE FROM CartItems WHERE CartItemId = @CartItemId",
                new { CartItemId = cartItemId });

            if (cartId.HasValue)
            {
                await connection.ExecuteAsync(
                    "UPDATE ShoppingCart SET LastModifiedDate = @LastModifiedDate WHERE CartId = @CartId",
                    new { LastModifiedDate = DateTime.UtcNow, CartId = cartId.Value });
            }

            _logger.LogInformation("Removed cart item {CartItemId} from cart", cartItemId);
        }

        public async Task ClearCartAsync(Guid userId)
        {
            var cart = await GetCartAsync(userId);
            using var connection = new SqlConnection(_connectionString);

            await connection.ExecuteAsync(
                "DELETE FROM CartItems WHERE CartId = @CartId",
                new { cart.CartId });

            await connection.ExecuteAsync(
                "UPDATE ShoppingCart SET LastModifiedDate = @LastModifiedDate WHERE CartId = @CartId",
                new { LastModifiedDate = DateTime.UtcNow, cart.CartId });

            _logger.LogInformation("Cleared cart for user {UserId}", userId);
        }

        public async Task<CustomerOrder> CheckoutAsync(Guid userId, string shippingAddress)
        {
            var cart = await GetCartAsync(userId);
            if (!cart.Items.Any())
                throw new InvalidOperationException("Cart is empty");

            using var connection = new SqlConnection(_connectionString);
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Create order in SQL database
                var order = new CustomerOrder
                {
                    OrderId = Guid.NewGuid(),
                    UserId = userId,
                    TotalAmount = cart.Items.Sum(item => item.UnitPrice * item.Quantity),
                    Status = "Submitted",
                    ShippingAddress = shippingAddress,
                    OrderDate = DateTime.UtcNow,
                    PaymentStatus = "Pending"
                };

                await connection.ExecuteAsync(
                    @"INSERT INTO Orders (OrderId, UserId, TotalAmount, Status, ShippingAddress, OrderDate, PaymentStatus) 
                     VALUES (@OrderId, @UserId, @TotalAmount, @Status, @ShippingAddress, @OrderDate, @PaymentStatus)",
                    order, transaction);

                // Add order items
                foreach (var item in cart.Items)
                {
                    var orderItem = new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.UnitPrice * item.Quantity
                    };

                    await connection.ExecuteAsync(
                        @"INSERT INTO OrderItems (OrderItemId, OrderId, ProductId, ProductName, Quantity, UnitPrice, TotalPrice) 
                         VALUES (@OrderItemId, @OrderId, @ProductId, @ProductName, @Quantity, @UnitPrice, @TotalPrice)",
                        orderItem, transaction);
                }

                // Create order in Table Storage via Functions API
                var tableStorageOrder = new ABCRetailers.Models.Order // This is your existing Table Storage Order class
                {
                    CustomerId = userId.ToString(),
                    ProductId = "Multiple",
                    ProductName = "Multiple Products",
                    Quantity = cart.Items.Sum(item => item.Quantity),
                    UnitPrice = (double)order.TotalAmount,
                    TotalPrice = (double)order.TotalAmount,
                    Status = "Submitted",
                    OrderDate = DateTime.UtcNow
                };

                await _functionsApi.AddEntityAsync("Orders", tableStorageOrder);
                order.TableStorageOrderId = tableStorageOrder.RowKey;

                // Update order with TableStorage reference
                await connection.ExecuteAsync(
                    "UPDATE Orders SET TableStorageOrderId = @TableStorageOrderId WHERE OrderId = @OrderId",
                    new { order.TableStorageOrderId, order.OrderId }, transaction);

                // Clear cart
                await ClearCartAsync(userId);

                await transaction.CommitAsync();

                _logger.LogInformation("Order {OrderId} created successfully for user {UserId}", order.OrderId, userId);
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during checkout for user {UserId}", userId);
                throw;
            }
        }
    }
}