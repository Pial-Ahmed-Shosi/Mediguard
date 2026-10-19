namespace MediGuard.Models.ViewModels
{
    public class OrderConfirmationViewModel
    {
       
        public string CustomerName { get; set; } = string.Empty;

      
        public string CustomerInitials { get; set; } = string.Empty;

       
        public string OrderNumber { get; set; } = string.Empty;

       
        public bool IsPaymentSuccessful { get; set; }

      
        public bool IsPaymentFailed { get; set; }

        
        public string PaymentStatus { get; set; } = "PENDING";

     
        public string TransactionReferenceId { get; set; } = "N/A";

        
        public decimal TotalPaidAmount { get; set; }

        public decimal OrderTotalAmount { get; set; }

        public string PaymentMethod { get; set; } = "Not available";

     
        public string EstimatedDeliveryTimeframe { get; set; } = "Within 1-2 hours";

        public string PaymentStatusMessage { get; set; } = string.Empty;
    }
}