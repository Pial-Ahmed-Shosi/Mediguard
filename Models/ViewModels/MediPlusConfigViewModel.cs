using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models.ViewModels
{
    public class MediPlusConfigViewModel
    {
        // Pharmacy Identification
        public Guid PharmacyId { get; set; }
        public string PharmacyName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;

        // Subscription Status Banner
        public bool IsActive { get; set; } = false;
        public string CurrentTier { get; set; } = "Free Tier"; // "Free Tier" or "Medi+ B2B Tier"
        public string Status { get; set; } = "Inactive";      // "Active", "Inactive", "Expired"
        public DateTime? NextBillingDate { get; set; }
        public decimal MonthlyFee { get; set; } = 49.00m;

        // B2B Network Connection Settings
        [Display(Name = "Enable Wholesale Supplier Connection")]
        public bool IsSupplierConnectionEnabled { get; set; } = false;

        [Display(Name = "Minimum Wholesale Order Value ($)")]
        [Range(0, 1000000, ErrorMessage = "Minimum order value must be between $0 and $1,000,000.")]
        public decimal MinimumWholesaleOrderValue { get; set; } = 250.00m;

        [Display(Name = "Auto-Accept B2B Orders")]
        public bool AutoAcceptB2BOrders { get; set; } = false;

        // B2B API Access Keys
        [Display(Name = "B2B API Key")]
        public string ApiKey { get; set; } = string.Empty;

        [Display(Name = "B2B API Secret")]
        public string ApiSecretMasked { get; set; } = string.Empty;

        public DateTime? ApiKeyGeneratedAt { get; set; }

        // Reachable B2B Partner Nodes for search bar
        public List<B2BPartnerPharmacyViewModel> PartnerPharmacies { get; set; } = new();
    }

    public class B2BPartnerPharmacyViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public bool IsMediPlusActive { get; set; }
        public bool IsReachable { get; set; }
        public decimal MinimumOrderValue { get; set; }
    }

    public class SaveMediPlusConfigDto
    {
        public bool IsSupplierConnectionEnabled { get; set; }

        [Range(0, 1000000, ErrorMessage = "Order value must be positive.")]
        public decimal MinimumWholesaleOrderValue { get; set; }

        public bool AutoAcceptB2BOrders { get; set; }
    }
}
