using System;

namespace MediGuard.Models.DTOs
{
    /// <summary>
    /// Result DTO returned by IPOSService.ProcessCheckoutAsync (Ticket 29A).
    /// </summary>
    public class POSCheckoutResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
    }
}
