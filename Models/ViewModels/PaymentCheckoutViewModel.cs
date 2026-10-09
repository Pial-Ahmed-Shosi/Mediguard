
using System.ComponentModel.DataAnnotations;

namespace MediGuard.Models.ViewModels
{
    public class PaymentCheckoutViewModel
    {
        public Guid OrderId { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;

        public List<PaymentCheckoutItemViewModel> Items { get; set; } = new();

        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TotalAmount { get; set; }

        // Must be supplied by the server's payment configuration.
        public string StripePublishableKey { get; set; } = string.Empty;
    }

    public class PaymentCheckoutItemViewModel
    {
        public string MedicineName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
