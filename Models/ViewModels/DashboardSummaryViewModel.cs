namespace MediGuard.Models.ViewModels
{
    public class DashboardSummaryViewModel
    {
        // User Profile Snapshot
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string PharmacyName { get; set; } = string.Empty;

        // Manager Metrics
        public decimal TodaySalesTotal { get; set; }
        public int LowStockCount { get; set; }
        public int PendingOrders { get; set; }
        public int ActiveDeliverymen { get; set; }

        // Pharmacist Metrics
        public int PendingPrescriptions { get; set; }
        public int ExpiringBatchesCount { get; set; }
        public int OutOfStockCount { get; set; }

        // Deliveryman Metrics
        public int PendingDeliveries { get; set; }
        public int CompletedToday { get; set; }
        public int CancelledDeliveries { get; set; }

        // Cashier Metrics
        public decimal DailyDrawerTotal { get; set; }
        public int ActiveCartItems { get; set; }
        public int PendingPickups { get; set; }

        // Customer Metrics
        public int ActiveOrdersInTransit { get; set; }
        public string UploadedPrescriptionStatus { get; set; } = "No Active Submissions";
    }
}