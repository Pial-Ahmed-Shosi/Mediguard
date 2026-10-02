using System;

namespace MediGuard.Models.ViewModels
{
    public class DashboardSummaryViewModel
    {
        // --- User & Organization Context ---
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string PharmacyName { get; set; } = string.Empty;

        // --- Sales & Register Metrics ---
        public decimal DailyDrawerTotal { get; set; }
        public decimal TotalSalesToday { get; set; }
        public decimal TodaySalesTotal
        {
            get => TotalSalesToday;
            set => TotalSalesToday = value;
        }

        // --- Inventory & Stock Metrics ---
        public int TotalMedicines { get; set; }
        public int OutOfStockCount { get; set; }
        public int ExpiredCount { get; set; }

        public int ExpiringBatchesCount { get; set; }
        public int ExpiringSoonCount
        {
            get => ExpiringBatchesCount;
            set => ExpiringBatchesCount = value;
        }

        public int LowStockBatchesCount { get; set; }
        public int LowStockCount
        {
            get => LowStockBatchesCount;
            set => LowStockBatchesCount = value;
        }

        // --- Cart, Order & Transit Metrics ---
        public int ActiveCartItems { get; set; }
        public int PendingPickups { get; set; }
        public int ActiveOrdersInTransit { get; set; }

        public int PendingOrdersCount { get; set; }
        public int PendingOrders
        {
            get => PendingOrdersCount;
            set => PendingOrdersCount = value;
        }

        public int CompletedOrdersTodayCount { get; set; }
        public int CompletedToday
        {
            get => CompletedOrdersTodayCount;
            set => CompletedOrdersTodayCount = value;
        }

        public int CancelledOrdersCount { get; set; }
        public int CancelledDeliveries
        {
            get => CancelledOrdersCount;
            set => CancelledOrdersCount = value;
        }

        // --- Delivery & Personnel Metrics ---
        public int ActiveDeliverymenCount { get; set; }
        public int ActiveDeliverymen
        {
            get => ActiveDeliverymenCount;
            set => ActiveDeliverymenCount = value;
        }

        public int AssignedPendingOrdersCount { get; set; }
        public int PendingDeliveries
        {
            get => AssignedPendingOrdersCount;
            set => AssignedPendingOrdersCount = value;
        }

        // --- Prescription Metrics ---
        public string UploadedPrescriptionStatus { get; set; } = "No Active Submissions";
        public int PendingPrescriptionsCount { get; set; }
        public int PendingPrescriptions
        {
            get => PendingPrescriptionsCount;
            set => PendingPrescriptionsCount = value;
        }
    }
}