// Models/ShoppingCartModels.cs
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class ShoppingCart
    {
        public Guid CartId { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public decimal TotalPrice => Items.Sum(item => item.TotalPrice);
        public int TotalItems => Items.Sum(item => item.Quantity);
    }

    public class CartItem
    {
        public Guid CartItemId { get; set; }
        public Guid CartId { get; set; }

        [Required]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.UtcNow;

        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class CustomerOrder
    {
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = "Not Required";
        public DateTime OrderDate { get; set; }
        public string? TableStorageOrderId { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    public class OrderItem
    {
        public Guid OrderItemId { get; set; }
        public Guid OrderId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class CompleteOrderViewModel
    {
        [Required(ErrorMessage = "Shipping address is required.")]
        [StringLength(500, ErrorMessage = "Shipping address cannot exceed 500 characters.")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Payment method is required.")]
        public string PaymentMethod { get; set; } = "Credit Card";

        public ShoppingCart Cart { get; set; } = new ShoppingCart();
    }
}