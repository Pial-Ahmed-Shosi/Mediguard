using MediGuard.Data;
using MediGuardApp.Data;
using MediGuardApp.Models;
using MediGuardApp.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediGuardApp.Services
{
    public class DeliveryService : IDeliveryService
    {
        private readonly ApplicationDbContext _context;

        public DeliveryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DeliveryDetailsDto>> GetDeliveriesForUserAsync(string deliverymanId, string statusFilter)
        {
            var normalizedStatus = statusFilter?.Trim().ToUpper() ?? "PENDING";

            // Enforce tenant user-id scoping and status filtering
            var query = _context.Set<Delivery>()
                .Include(d => d.Order)
                    .ThenInclude(o => o.Customer)
                .Include(d => d.Order)
                    .ThenInclude(o => o.Pharmacy)
                .Include(d => d.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Medicine)
                .Where(d => d.DeliverymanId == deliverymanId && d.Status == normalizedStatus);

            var deliveries = await query.ToListAsync();

            return deliveries.Select(d => new DeliveryDetailsDto
            {
                DeliveryId = d.Id,
                OrderId = d.OrderId,
                OrderType = d.Order.PharmacyId.HasValue ? "B2B" : "B2C",
                Status = d.Status,
                CreatedAt = d.CreatedAt,
                CompletedAt = d.CompletedAt,
                CancellationReason = d.CancellationReason,
                TotalAmount = d.Order.TotalAmount,

                // Recipient Metadata for both B2C and B2B
                RecipientName = d.Order.PharmacyId.HasValue
                    ? d.Order.Pharmacy?.ContactPerson ?? d.Order.Customer?.FullName ?? "N/A"
                    : d.Order.Customer?.FullName ?? "N/A",

                RecipientPhone = d.Order.PharmacyId.HasValue
                    ? d.Order.Pharmacy?.Phone ?? d.Order.Customer?.PhoneNumber ?? "N/A"
                    : d.Order.Customer?.PhoneNumber ?? "N/A",

                ShippingAddress = d.Order.PharmacyId.HasValue
                    ? d.Order.Pharmacy?.Address ?? d.Order.ShippingAddress
                    : d.Order.ShippingAddress,

                PharmacyName = d.Order.Pharmacy?.Name,

                // Order Items mapping
                Items = d.Order.OrderItems.Select(oi => new DeliveryOrderItemDto
                {
                    MedicineName = oi.Medicine?.Name ?? "Unknown Medicine",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            });
        }

        public async Task<(bool Success, string Message)> UpdateDeliveryStatusAsync(string deliverymanId, UpdateDeliveryStatusDto dto)
        {
            if (dto == null)
            {
                return (false, "Invalid payload.");
            }

            var newStatus = dto.NewStatus?.Trim().ToUpper();

            // Validate status rules
            if (newStatus != "DONE" && newStatus != "CANCELLED")
            {
                return (false, "Status must be either 'DONE' or 'CANCELLED'.");
            }

            // Require reason for cancellation
            if (newStatus == "CANCELLED" && string.IsNullOrWhiteSpace(dto.Reason))
            {
                return (false, "A reason is required when cancelling a delivery.");
            }

            // Retrieve delivery record belonging strictly to this deliveryman
            var delivery = await _context.Set<Delivery>()
                .Include(d => d.Order)
                .FirstOrDefaultAsync(d => d.Id == dto.DeliveryId && d.DeliverymanId == deliverymanId);

            if (delivery == null)
            {
                return (false, "Delivery record not found or access denied.");
            }

            var now = DateTime.UtcNow;

            if (newStatus == "DONE")
            {
                delivery.Status = "DONE";
                delivery.CompletedAt = now;
                delivery.CancellationReason = null;

                // Synchronize linked order statuses
                if (delivery.Order != null)
                {
                    delivery.Order.DeliveryStatus = "COMPLETED";
                    delivery.Order.OrderStatus = "COMPLETED";
                    delivery.Order.UpdatedAt = now;
                }
            }
            else if (newStatus == "CANCELLED")
            {
                delivery.Status = "CANCELLED";
                delivery.CompletedAt = now;
                delivery.CancellationReason = dto.Reason?.Trim();

                // Synchronize linked order status
                if (delivery.Order != null)
                {
                    delivery.Order.DeliveryStatus = "CANCELLED";
                    delivery.Order.OrderStatus = "CANCELLED";
                    delivery.Order.UpdatedAt = now;
                }
            }

            await _context.SaveChangesAsync();
            return (true, $"Delivery status updated to {newStatus} successfully.");
        }
    }
}