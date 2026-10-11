namespace MediGuard.Models.ViewModels
{
    public class StorefrontViewModel
    {
        public List<StorefrontProduct> Products { get; set; } = new();

        public List<string> Categories { get; set; } = new();

        public string? SearchTerm { get; set; }

        public string? SelectedCategory { get; set; }

        public int TotalProducts => Products.Count;
    }
}