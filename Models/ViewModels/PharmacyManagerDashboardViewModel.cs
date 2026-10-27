using System;
using System.Collections.Generic;
using MediGuard.Models;

namespace MediGuard.Models.ViewModels
{
    public class PharmacyManagerDashboardViewModel
    {
        public ApplicationUser ManagerUser { get; set; } = null!;
        public Pharmacy Pharmacy { get; set; } = null!;
        public bool IsPharmacyOpen { get; set; } = true;

        // Financial & Register Metrics
        public decimal DailyDrawerTotal { get; set; }
        public decimal MonthlyRevenueTotal { get; set; }
        public decimal PrepaidOnlineTotal { get; set; }
        public decimal PendingCodTotal { get; set; }
        public int TotalOnlineOrdersMonth { get; set; }
        public int TotalPosSalesMonth { get; set; }

        // Inventory scoped to this pharmacy
        public List<InventoryItemDto> BranchInventory { get; set; } = new();
        public List<Batch> ExpiringBatches { get; set; } = new();
        public List<Batch> LowStockBatches { get; set; } = new();
        public List<MediGuard.Models.Medicine> BranchMedicines { get; set; } = new();

        // Online Orders Queue
        public List<Order> IncomingOrders { get; set; } = new();

        // Prescriptions Queue
        public List<Prescription> PendingPrescriptions { get; set; } = new();

        // Active Delivery Riders
        public List<ApplicationUser> ActiveDeliveryRiders { get; set; } = new();

        // Daily Cash Sales Register Log
        public List<Sale> TodayOfflineSales { get; set; } = new();
    }

    public class InventoryItemDto
    {
        public Guid BatchId { get; set; }
        public Guid MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int LowStockThreshold { get; set; } = 10;
        public decimal PurchasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public DateTime ManufacturingDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public bool IsExpiringSoon => ExpiryDate <= DateTime.UtcNow.AddDays(30) && ExpiryDate >= DateTime.UtcNow;
        public bool IsExpired => ExpiryDate < DateTime.UtcNow;
        public bool IsLowStock => Quantity <= LowStockThreshold;
    }

    public class RestockBatchRequest
    {
        public Guid? MedicineId { get; set; }
        public string? MedicineName { get; set; }
        public string? GenericName { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int QuantityAdded { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal PurchaseCost { get; set; }
        public decimal RetailUnitPrice { get; set; }
    }
}
