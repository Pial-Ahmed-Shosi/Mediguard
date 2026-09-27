namespace MediGuard.Models.ViewModels
{
    public class DashboardSummaryViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string PharmacyName { get; set; } = string.Empty;


        public int ExpiringBatchesCount { get; set; }
        public int OutOfStockCount { get; set; }

        public decimal DailyDrawerTotal { get; set; }
        public int ActiveCartItems { get; set; }
        public int PendingPickups { get; set; }

        public int ActiveOrdersInTransit { get; set; }
        public string UploadedPrescriptionStatus { get; set; } = "No Active Submissions";
    }
}