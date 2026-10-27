using System;
using System.Collections.Generic;
using MediGuard.Models;

namespace MediGuard.Models.ViewModels
{
    public class CustomerProfileViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public List<Order> RecentOrders { get; set; } = new();
        public List<Prescription> RecentPrescriptions { get; set; } = new();
        public int ActiveOrdersCount { get; set; }
        public int TotalOrdersCount { get; set; }
        public int PendingPrescriptionsCount { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? EmergencyContact { get; set; }
        public List<SavedPaymentMethodDto> SavedPaymentMethods { get; set; } = new();
        public List<CustomerNotificationItemDto> Notifications { get; set; } = new();
    }

    public class CustomerNotificationItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "INFO"; // "ORDER", "PRESCRIPTION", "PROMO"
        public string TimeAgo { get; set; } = "Just now";
        public string IconClass { get; set; } = "bi-bell";
        public string BadgeClass { get; set; } = "bg-primary";
        public string? ActionUrl { get; set; }
    }
}
