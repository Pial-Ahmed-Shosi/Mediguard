namespace MediGuard.Models.ViewModels
{
    public class DashboardSummaryViewModel
    {
        // Common header info
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string PharmacyName { get; set; } = string.Empty;

        // ========== Manager ==========
        public decimal TotalSalesToday { get; set; }
        public int LowStockBatchesCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public int ActiveDeliverymenCount { get; set; }

        // ========== Deliveryman ==========
        public int AssignedPendingOrdersCount { get; set; }
        public int CompletedOrdersTodayCount { get; set; }
        public int CancelledOrdersCount { get; set; }

        // ========== Pharmacist ==========
        public int PendingPrescriptionsCount { get; set; }
        public int ExpiringBatchesCount { get; set; }
        public int OutOfStockCount { get; set; }

        // ========== Cashier (bonus) ==========
        public decimal DailyDrawerTotal { get; set; }
        public int PendingPickups { get; set; }

        // ========== Customer (bonus) ==========
        public int ActiveOrdersInTransit { get; set; }
    }
}