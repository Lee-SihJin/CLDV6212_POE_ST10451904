// Models/ViewModels/OrderEditViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models.ViewModels
{
    public class OrderEditViewModel
    {
        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;
    }
}