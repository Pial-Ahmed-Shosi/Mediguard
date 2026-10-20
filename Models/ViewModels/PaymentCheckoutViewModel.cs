using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace MediGuard.Models.ViewModels
{
    /// <summary>
    /// Presentation data only. Contains NO payment-provider lifecycle state
    /// (client secret, intent status, etc.) - that stays in payment-checkout.js.
    /// </summary>
    public class PaymentCheckoutViewModel
    {
        public Guid OrderId { get; set; }

        // ---- Customer (sidebar + delivery prefill) ----
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        [Display(Name = "Full address")]
        public string ShippingAddress { get; set; } = string.Empty;

        public string CustomerInitials =>
            string.Concat(
                (CustomerName ?? string.Empty)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(2)
                    .Select(p => char.ToUpperInvariant(p[0])));

        // ---- Pharmacy ----
        public string PharmacyName { get; set; } = string.Empty;
        public string PharmacyAddress { get; set; } = string.Empty;
        public bool PharmacyVerified { get; set; }

        // ---- Items ----
        public List<PaymentCheckoutItemViewModel> Items { get; set; } = new();

        // ---- Amounts (display only; server recomputes the real payable amount) ----
        public string CurrencySymbol { get; set; } = "৳";
        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TotalAmount { get; set; }

        public bool StorePickupAvailable { get; set; } = true;
        public string EstimatedDelivery { get; set; } = "within 1-2 hours";

        // Publishable keys are safe to expose to the browser.
        public string StripePublishableKey { get; set; } = string.Empty;

        public string FormatMoney(decimal amount) =>
            $"{CurrencySymbol} {amount.ToString("N2", CultureInfo.InvariantCulture)}";
    }

    public class PaymentCheckoutItemViewModel
    {
        public string MedicineName { get; set; } = string.Empty;
        public string DosageForm { get; set; } = string.Empty;   // e.g. "Capsule"
        public string Strength { get; set; } = string.Empty;     // e.g. "300 mg"
        public string Manufacturer { get; set; } = string.Empty;
        public string PackagingNote { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }

        public string BadgeText =>
            string.Join(" | ", new[] { DosageForm, Strength }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
