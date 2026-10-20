namespace MediGuard.Models
{
    public class Storefront
    {
        public int Id { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int StockQuantity { get; set; }
        public bool RequiresPrescription { get; set; }
        public string? ImageUrl { get; set; }
        public Category? Category { get; set; }

        // --- Missing properties causing CS1061 ---
        public string? DosageForm { get; set; }
        public string? Strength { get; set; }
        public string? Manufacturer { get; set; }
        public string? PackagingType { get; set; }
        public string? PharmacyName { get; set; }
        public string? PharmacyArea { get; set; }
        public string? UnitLabel { get; set; }
        public double? Rating { get; set; }
    }
}