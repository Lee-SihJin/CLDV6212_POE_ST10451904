// Services/IOrderService.cs
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;

namespace ABCRetailers.Services
{
    public interface IOrderService
    {
        Task<List<CustomerOrder>> GetAllOrdersAsync();
        Task<CustomerOrder?> GetOrderByIdAsync(Guid orderId);
        Task<CustomerOrder> UpdateOrderAsync(Guid orderId, OrderEditViewModel model);
        Task<bool> DeleteOrderAsync(Guid orderId);
        Task<bool> UpdateOrderStatusAsync(Guid orderId, string status);
        Task<List<CustomerOrder>> GetOrdersByCustomerAsync(string customerId);
    }
}