namespace MediGuard.Models.ViewModels
{
    public class DashboardSummaryViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string PharmacyName { get; set; } = string.Empty;

        // Sales & Stock metrics
        public decimal TodaySalesTotal { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiringBatchesCount { get; set; }
        public int OutOfStockCount { get; set; }

        // Order metrics
        public int PendingOrders { get; set; }
        public int PendingDeliveries { get; set; }
        public int CompletedToday { get; set; }
        public int CancelledDeliveries { get; set; }

        // Deliveryman metrics
        public int ActiveDeliverymen { get; set; }

        // Prescription metrics
        public int PendingPrescriptions { get; set; }

        // Dashboard stats for counting
        public decimal TotalSalesToday { get; set; }
        public int LowStockBatchesCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public int ActiveDeliverymenCount { get; set; }
        public int AssignedPendingOrdersCount { get; set; }
        public int CompletedOrdersTodayCount { get; set; }
        public int CancelledOrdersCount { get; set; }
        public int PendingPrescriptionsCount { get; set; }

        // Existing properties
        public decimal DailyDrawerTotal { get; set; }
        public int ActiveCartItems { get; set; }
        public int PendingPickups { get; set; }
        public int ActiveOrdersInTransit { get; set; }
        public string UploadedPrescriptionStatus { get; set; } = "No Active Submissions";
    }
}