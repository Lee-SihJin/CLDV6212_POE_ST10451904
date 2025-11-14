// Services/OrderService.cs
using System.Data.SqlClient;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using Dapper;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Services
{
    public class OrderService : IOrderService
    {
        private readonly string _connectionString;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IConfiguration configuration, ILogger<OrderService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public async Task<List<CustomerOrder>> GetAllOrdersAsync()
        {
            using var connection = new SqlConnection(_connectionString);

            try
            {
                var orders = await connection.QueryAsync<CustomerOrder>(
                    @"SELECT * FROM Orders 
                      ORDER BY OrderDate DESC");

                foreach (var order in orders)
                {
                    var items = await connection.QueryAsync<OrderItem>(
                        "SELECT * FROM OrderItems WHERE OrderId = @OrderId",
                        new { order.OrderId });

                    order.Items = items.ToList();
                }

                return orders.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all orders from SQL database");
                throw;
            }
        }

        public async Task<CustomerOrder?> GetOrderByIdAsync(Guid orderId)
        {
            using var connection = new SqlConnection(_connectionString);

            try
            {
                var order = await connection.QueryFirstOrDefaultAsync<CustomerOrder>(
                    "SELECT * FROM Orders WHERE OrderId = @OrderId",
                    new { OrderId = orderId });

                if (order != null)
                {
                    var items = await connection.QueryAsync<OrderItem>(
                        "SELECT * FROM OrderItems WHERE OrderId = @OrderId",
                        new { order.OrderId });

                    order.Items = items.ToList();
                }

                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId} from SQL database", orderId);
                throw;
            }
        }

        public async Task<CustomerOrder> UpdateOrderAsync(Guid orderId, OrderEditViewModel model)
        {
            using var connection = new SqlConnection(_connectionString);

            try
            {
                // Update the order
                await connection.ExecuteAsync(
                    @"UPDATE Orders 
                      SET Status = @Status, OrderDate = @OrderDate
                      WHERE OrderId = @OrderId",
                    new
                    {
                        model.Status,
                        model.OrderDate,
                        OrderId = orderId
                    });

                // Return the updated order
                var updatedOrder = await GetOrderByIdAsync(orderId);
                if (updatedOrder == null)
                    throw new InvalidOperationException($"Order {orderId} not found after update");

                _logger.LogInformation("Order {OrderId} updated successfully", orderId);
                return updatedOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId} in SQL database", orderId);
                throw;
            }
        }

        public async Task<bool> DeleteOrderAsync(Guid orderId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Delete order items first (foreign key constraint)
                await connection.ExecuteAsync(
                    "DELETE FROM OrderItems WHERE OrderId = @OrderId",
                    new { OrderId = orderId }, transaction);

                // Delete the order
                var affectedRows = await connection.ExecuteAsync(
                    "DELETE FROM Orders WHERE OrderId = @OrderId",
                    new { OrderId = orderId }, transaction);

                await transaction.CommitAsync();

                var success = affectedRows > 0;
                if (success)
                    _logger.LogInformation("Order {OrderId} deleted successfully", orderId);
                else
                    _logger.LogWarning("Order {OrderId} not found for deletion", orderId);

                return success;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting order {OrderId} from SQL database", orderId);
                throw;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(Guid orderId, string status)
        {
            using var connection = new SqlConnection(_connectionString);

            try
            {
                var validStatuses = new[] { "Submitted", "Processing", "Completed", "Cancelled" };
                if (!validStatuses.Contains(status))
                    throw new ArgumentException($"Invalid status: {status}");

                var affectedRows = await connection.ExecuteAsync(
                    "UPDATE Orders SET Status = @Status WHERE OrderId = @OrderId",
                    new { Status = status, OrderId = orderId });

                var success = affectedRows > 0;
                if (success)
                    _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, status);
                else
                    _logger.LogWarning("Order {OrderId} not found for status update", orderId);

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId} status to {Status}", orderId, status);
                throw;
            }
        }

        public async Task<List<CustomerOrder>> GetOrdersByCustomerAsync(string customerId)
        {
            using var connection = new SqlConnection(_connectionString);

            try
            {
                var orders = await connection.QueryAsync<CustomerOrder>(
                    @"SELECT * FROM Orders 
                      WHERE UserId = @UserId 
                      ORDER BY OrderDate DESC",
                    new { UserId = Guid.Parse(customerId) });

                foreach (var order in orders)
                {
                    var items = await connection.QueryAsync<OrderItem>(
                        "SELECT * FROM OrderItems WHERE OrderId = @OrderId",
                        new { order.OrderId });

                    order.Items = items.ToList();
                }

                return orders.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for customer {CustomerId} from SQL database", customerId);
                throw;
            }
        }
    }
}