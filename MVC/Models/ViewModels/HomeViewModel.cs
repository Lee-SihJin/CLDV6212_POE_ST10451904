namespace ABCRetailers.Models.ViewModels
{
    public class HomeViewModel
    {
        // Featured products to display on the homepage
        public List<Product> FeaturedProducts { get; set; } = new();

        // Dashboard stats
        public int CustomerCount { get; set; }
        public int ProductCount { get; set; }
        public int OrderCount { get; set; }
    }
}
