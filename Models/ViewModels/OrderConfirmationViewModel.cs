
namespace MediGuard.Models.ViewModels
{
    public class OrderConfirmationViewModel
    {
        public string OrderNumber { get; set; } = string.Empty;

        public bool IsPaymentSuccessful { get; set; }
        public bool IsPaymentFailed { get; set; }

        public string PaymentStatus { get; set; } = "PENDING";

        public string TransactionReferenceId { get; set; }
            = "Not available";

        public decimal TotalPaidAmount { get; set; }

        public decimal OrderTotalAmount { get; set; }

        public string PaymentMethod { get; set; }
            = "Not available";

        public string EstimatedDeliveryTimeframe { get; set; }
            = "To be confirmed";

        public string PaymentStatusMessage { get; set; }
            = string.Empty;
    }
}
