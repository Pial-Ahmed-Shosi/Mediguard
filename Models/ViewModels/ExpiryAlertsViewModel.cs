// File: Models/ViewModels/ExpiryAlertsViewModel.cs
using System;
using System.Collections.Generic;

namespace MediGuard.Models.ViewModels
{
    public class ExpiryAlertsViewModel
    {
        public List<ExpiryAlertItemDto> CriticalExpiries { get; set; } = new();
        public List<StockShortageItemDto> StockShortages { get; set; } = new();
    }

    public class ExpiryAlertItemDto
    {
        public Guid BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysLeft { get; set; }
        public int Quantity { get; set; }
    }

    public class StockShortageItemDto
    {
        public Guid BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}