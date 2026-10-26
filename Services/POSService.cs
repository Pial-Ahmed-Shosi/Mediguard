using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediGuard.Data;
using MediGuard.Exceptions;
using MediGuard.Models;
using MediGuard.Models.DTOs;
using MediGuard.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MediGuard.Services
{
    /// <summary>
    /// POS checkout service implementation (Ticket 29A).
    /// Wraps the entire checkout pipeline inside a single IDbContextTransaction
    /// so that stock validation, FEFO deductions, order creation, and line-item
    /// persistence either all succeed or completely roll back.
    /// </summary>
    public class POSService : IPOSService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryDeductionService _inventoryDeductionService;

        public POSService(
            ApplicationDbContext context,
            IInventoryDeductionService inventoryDeductionService)
        {
            _context = context;
            _inventoryDeductionService = inventoryDeductionService;
        }

        public async Task<POSCheckoutResult> ProcessCheckoutAsync(
            POSCheckoutViewModel model,
            Guid pharmacyId,
            string userId)
        {
            // Begin atomic database transaction
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ──────────────────────────────────────────────
                // 1. Server-Side Stock Validation
                // ──────────────────────────────────────────────
                foreach (var item in model.Items)
                {
                    // Verify the medicine exists and belongs to this pharmacy
                    var medicine = await _context.Medicines
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == item.MedicineId && m.PharmacyId == pharmacyId);

                    if (medicine == null)
                    {
                        await transaction.RollbackAsync();
                        return new POSCheckoutResult
                        {
                            Success = false,
                            Message = $"Medicine with ID '{item.MedicineId}' not found in this pharmacy."
                        };
                    }

                    // Confirm available inventory across active batches
                    int totalAvailable = await _context.Batches
                        .Where(b => b.PharmacyId == pharmacyId
                                    && b.MedicineId == item.MedicineId
                                    && b.Status == "ACTIVE"
                                    && b.Quantity > 0
                                    && b.ExpiryDate.Date > DateTime.UtcNow.Date)
                        .SumAsync(b => b.Quantity);

                    if (totalAvailable < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return new POSCheckoutResult
                        {
                            Success = false,
                            Message = $"Insufficient stock for '{medicine.BrandName}'. Requested: {item.Quantity}, Available: {totalAvailable}."
                        };
                    }
                }

                // ──────────────────────────────────────────────
                // 2. Generate Unique Order Number (POS-YYYYMMDD-XXXX)
                // ──────────────────────────────────────────────
                string orderNumber = GenerateOrderNumber();

                // ──────────────────────────────────────────────
                // 3. Calculate Financials
                // ──────────────────────────────────────────────
                decimal subtotal = model.Subtotal;
                decimal taxAmount = Math.Round(subtotal * (model.TaxRate / 100m), 2);
                decimal discount = model.OverallDiscount;
                decimal totalAmount = model.GrandTotal > 0
                    ? model.GrandTotal
                    : Math.Max(0, subtotal + taxAmount - discount);

                // ──────────────────────────────────────────────
                // 4. Create Master Order Record
                // ──────────────────────────────────────────────
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    PharmacyId = pharmacyId,
                    UserId = userId,
                    OrderNumber = orderNumber,
                    OrderType = "POS",
                    Subtotal = subtotal,
                    Tax = taxAmount,
                    Discount = discount,
                    TotalAmount = totalAmount,
                    PaymentStatus = "PAID",
                    PaymentMethod = model.PaymentMethod,
                    Status = "COMPLETED",
                    OrderStatus = "COMPLETED",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // ──────────────────────────────────────────────
                // 5. FEFO Stock Deduction & OrderItem Creation
                // ──────────────────────────────────────────────
                var orderItems = new List<OrderItem>();

                foreach (var item in model.Items)
                {
                    // Deduct stock using FEFO rules (IInventoryDeductionService)
                    List<BatchDeductionResult> deductions;
                    try
                    {
                        deductions = await _inventoryDeductionService.DeductStockFEFOAsync(
                            pharmacyId, item.MedicineId, item.Quantity);
                    }
                    catch (InsufficientStockException ex)
                    {
                        await transaction.RollbackAsync();
                        return new POSCheckoutResult
                        {
                            Success = false,
                            Message = ex.Message
                        };
                    }

                    // Create OrderItem records for each batch deduction
                    foreach (var deduction in deductions)
                    {
                        decimal lineTotal = deduction.DeductedQuantity * item.UnitPrice;

                        // Apply per-item discount if present
                        if (item.DiscountPercent > 0 && item.DiscountPercent <= 100)
                        {
                            lineTotal -= Math.Round(lineTotal * (item.DiscountPercent / 100m), 2);
                        }

                        var orderItem = new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            OrderId = order.Id,
                            MedicineId = item.MedicineId,
                            BatchId = deduction.BatchId,
                            Quantity = deduction.DeductedQuantity,
                            UnitPrice = item.UnitPrice,
                            TotalPrice = lineTotal,
                            LineTotal = lineTotal,
                            CreatedAt = DateTime.UtcNow
                        };

                        orderItems.Add(orderItem);
                    }
                }

                _context.OrderItems.AddRange(orderItems);
                await _context.SaveChangesAsync();

                // ──────────────────────────────────────────────
                // 6. Commit Transaction
                // ──────────────────────────────────────────────
                await transaction.CommitAsync();

                return new POSCheckoutResult
                {
                    Success = true,
                    Message = "Checkout completed successfully.",
                    OrderId = order.Id,
                    OrderNumber = orderNumber
                };
            }
            catch (Exception ex)
            {
                // Ensure full rollback on any unexpected error
                await transaction.RollbackAsync();

                return new POSCheckoutResult
                {
                    Success = false,
                    Message = $"An unexpected error occurred during checkout: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Generates a unique order number following the format: POS-YYYYMMDD-XXXX
        /// where XXXX is a random alphanumeric string.
        /// </summary>
        private static string GenerateOrderNumber()
        {
            string datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            string randomPart = GenerateAlphanumericCode(4);
            return $"POS-{datePart}-{randomPart}";
        }

        /// <summary>
        /// Generates a random alphanumeric string of the specified length.
        /// </summary>
        private static string GenerateAlphanumericCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[Random.Shared.Next(chars.Length)];
            }
            return new string(result);
        }
    }
}
