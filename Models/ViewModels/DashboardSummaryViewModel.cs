namespace MediGuard.Models.ViewModels
{
    public class DashboardSummaryViewModel
    {
        public string Role { get; set; } = string.Empty;

        // --- Manager Metrics ---
        public decimal TotalSalesToday { get; set; }
        public int LowStockBatchesCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public int ActiveDeliverymenCount { get; set; }

        // --- Deliveryman Metrics ---
        public int AssignedPendingOrdersCount { get; set; }
        public int CompletedOrdersTodayCount { get; set; }
        public int CancelledOrdersCount { get; set; }

        // --- Pharmacist Metrics ---
        public int PendingPrescriptionsCount { get; set; }
        public int ExpiringBatchesCount { get; set; }
    }
}