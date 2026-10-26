namespace MediGuard.Models.ViewModels
{
    public class StorefrontViewModel
    {
        public List<StorefrontProduct> Products { get; set; } = new();

        public List<string> Categories { get; set; } = new();

        public List<string> Pharmacies { get; set; } = new();

        public List<string> Locations { get; set; } = new();

        public string? SearchTerm { get; set; }

        public string? PharmacyTerm { get; set; }

        public string? LocationTerm { get; set; }

        public string? SelectedCategory { get; set; }

        public int TotalProducts => Products.Count;
    }
}