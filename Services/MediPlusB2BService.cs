using MediGuard.Data;
using MediGuard.Models;
using MediGuard.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MediGuard.Services
{
    public class MediPlusB2BService : IMediPlusB2BService
    {
        private readonly ApplicationDbContext _context;

        public MediPlusB2BService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, Guid? OrderId)> CreateB2BOrderAsync(B2BOrderViewModel model)
        {
            if (model == null || model.Items == null || !model.Items.Any())
            {
                return (false, "Order details or order items cannot be empty.", null);
            }

            if (model.PurchaserPharmacyId == model.SupplierPharmacyId)
            {
                return (false, "Purchaser and Supplier pharmacies cannot be the same.", null);
            }

            // 1. Fetch Purchaser & Supplier Pharmacies
            var purchaser = await _context.Pharmacies.FirstOrDefaultAsync(p => p.Id == model.PurchaserPharmacyId);
            var supplier = await _context.Pharmacies.FirstOrDefaultAsync(p => p.Id == model.SupplierPharmacyId);

            if (purchaser == null)
            {
                return (false, "Purchaser pharmacy not found.", null);
            }

            if (supplier == null)
            {
                return (false, "Supplier pharmacy not found.", null);
            }

            // 2. Acceptance Criteria Check: Both Purchaser and Supplier MUST have Active Medi+ status
            if (!purchaser.IsMediPlusActive)
            {
                return (false, "Purchaser pharmacy does not have an active Medi+ subscription.", null);
            }

            if (!supplier.IsMediPlusActive)
            {
                return (false, "Supplier pharmacy does not have an active Medi+ subscription.", null);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal totalAmount = model.Items.Sum(i => i.Quantity * i.UnitPrice);

                // 3. Create Inter-Pharmacy Order Record
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    PharmacyId = model.PurchaserPharmacyId,       // PurchaserId
                    SupplierPharmacyId = model.SupplierPharmacyId, // SupplierId
                    IsB2BOrder = true,                             // is_b2b_order = true
                    ShippingAddress = model.ShippingAddress,
                    TotalAmount = totalAmount,
                    OrderStatus = "PENDING",
                    DeliveryStatus = "PENDING",
                    Notes = model.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    OrderItems = model.Items.Select(item => new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        MedicineId = item.MedicineId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    }).ToList()
                };

                await _context.Orders.AddAsync(order);

                // 4. Create Matching Delivery Entry with delivery_type = "B2B"
                var delivery = new Delivery
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    DeliveryType = "B2B",  // delivery_type = "B2B"
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Deliveries.AddAsync(delivery);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Medi+ B2B order created successfully.", order.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error processing B2B order: {ex.Message}", null);
            }
        }
    }
}