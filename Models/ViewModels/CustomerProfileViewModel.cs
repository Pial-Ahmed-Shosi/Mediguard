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
    }
}
