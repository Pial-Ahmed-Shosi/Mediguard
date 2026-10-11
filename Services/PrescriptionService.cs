using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediGuard.Data;
using Microsoft.EntityFrameworkCore;

namespace MediGuard.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly ApplicationDbContext _context;

        public PrescriptionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ApprovePrescriptionAsync(Guid prescriptionId, string currentUserId)
        {
            var prescription = await _context.Prescriptions
                .FirstOrDefaultAsync(p => p.Id == prescriptionId);

            if (prescription == null)
            {
                throw new KeyNotFoundException($"Prescription with ID '{prescriptionId}' was not found.");
            }

            // 1. Update prescription status and auditor
            prescription.Status = "APPROVED";
            prescription.VerifiedById = currentUserId;

            // 2. Update linked order to advance into delivery workflow
            if (prescription.OrderId.HasValue)
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == prescription.OrderId.Value);
                if (order != null)
                {
                    order.DeliveryStatus = "PENDING";
                    order.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task RejectPrescriptionAsync(Guid prescriptionId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("A non-empty rejection reason is required.", nameof(reason));
            }

            var prescription = await _context.Prescriptions
                .FirstOrDefaultAsync(p => p.Id == prescriptionId);

            if (prescription == null)
            {
                throw new KeyNotFoundException($"Prescription with ID '{prescriptionId}' was not found.");
            }

            // 1. Update prescription status and reason
            prescription.Status = "REJECTED";
            prescription.RejectionReason = reason.Trim();

            // 2. Cancel linked order
            if (prescription.OrderId.HasValue)
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == prescription.OrderId.Value);
                if (order != null)
                {
                    order.Status = "CANCELLED";
                    order.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}