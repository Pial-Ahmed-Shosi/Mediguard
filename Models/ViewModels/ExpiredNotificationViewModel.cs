using System;
using System.Collections.Generic;

namespace MediGuard.Models.ViewModels
{
    public class ExpiredNotificationViewModel
    {
        // Metric Summary Cards
        public int TotalExpiredBatches { get; set; }
        public int ExpiringWithin30Days { get; set; }
        public int ExpiringWithin60Days { get; set; }
        public decimal FinancialValueAtRisk { get; set; }

        // Data Table Items
        public List<ExpiredBatchItemViewModel> BatchItems { get; set; } = new List<ExpiredBatchItemViewModel>();
    }

    public class ExpiredBatchItemViewModel
    {
        public string BatchId { get; set; } = string.Empty;
        public string MedicineName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public int AvailableQty { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysRemaining { get; set; }

        // EXPIRED, CRITICAL_30_DAYS, WARNING_60_DAYS
        public string ExpiryStatus { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
    }
}