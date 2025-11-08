// Services/ShoppingCartService.cs
using System.Data.SqlClient;
using ABCRetailers.Models;
using Dapper;

namespace ABCRetailers.Services
{
    public interface IShoppingCartService
    {
        Task<ShoppingCart> GetCartAsync(Guid userId);
        Task AddToCartAsync(Guid userId, CartItem item);
        Task UpdateCartItemAsync(Guid cartItemId, int quantity);
        Task RemoveFromCartAsync(Guid cartItemId);
        Task ClearCartAsync(Guid userId);
        Task<CustomerOrder> CheckoutAsync(Guid userId, string shippingAddress);
    }
}