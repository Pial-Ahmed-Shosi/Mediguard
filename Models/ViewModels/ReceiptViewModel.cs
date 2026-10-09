using System;
using System.Collections.Generic;

namespace MediGuard.Models.ViewModels
{
    public class ReceiptViewModel
    {
        // Pharmacy Header
        public string PharmacyName { get; set; } = "MediGuard Central Pharmacy";
        public string PharmacyAddress { get; set; } = "123 Healthcare Ave, Medical District";
        public string PharmacyPhone { get; set; } = "+1 (800) 555-0199";
        public string TaxId { get; set; } = "TX-89472019";
        public string LicenseNumber { get; set; } = "PHARM-LIC-2026-88";

        // Transaction Meta
        public string ReceiptNumber { get; set; } = $"REC-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public string CashierName { get; set; } = "Staff Cashier";
        public string OrderType { get; set; } = "POS";

        // Customer Information
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        // Order Information
        public Guid? OrderId { get; set; }
        public bool IsB2BOrder { get; set; } = false;
        public string? ShippingAddress { get; set; }

        // Line Items Table
        public List<ReceiptItemViewModel> Items { get; set; } = new();

        // Totals & Calculations
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TaxRate { get; set; } = 5.0m; // Default 5%
        public decimal DiscountAmount { get; set; }
        public decimal GrandTotal { get; set; }

        // Payment Breakdown
        public string PaymentMethod { get; set; } = "CASH";
        public decimal PaidAmount { get; set; }
        public decimal ChangeAmount { get; set; }

        // Footer Message
        public string FooterMessage { get; set; } = "Thank you for visiting! Keep receipt for returns.";
    }

    public class ReceiptItemViewModel
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string? BatchNumber { get; set; }
    }
}
