using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MediGuard.Data;
using MediGuard.Models;
using MediGuard.Models.DTOs;
using MediGuard.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MediGuard.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public CheckoutController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Payment(Guid? orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var customer = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            Order? order = null;

            if (orderId.HasValue && orderId.Value != Guid.Empty)
            {
                order = await _context.Orders
                    .Include(o => o.Pharmacy)
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == orderId.Value && (o.CustomerId == userId || o.UserId == userId));
            }

            // Fallback: load latest pending unpaid order for this customer
            if (order == null)
            {
                order = await _context.Orders
                    .Include(o => o.Pharmacy)
                    .Include(o => o.OrderItems)
                    .Where(o => (o.CustomerId == userId || o.UserId == userId) && o.PaymentStatus == "UNPAID")
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();
            }

            // If no order exists at all, initialize a pending order with storefront products
            if (order == null)
            {
                var sampleProduct = await _context.StorefrontProducts
                    .Include(p => p.Pharmacy)
                    .FirstOrDefaultAsync(p => p.IsActive);

                var pharmacy = sampleProduct?.Pharmacy ?? await _context.Pharmacies.FirstOrDefaultAsync();
                var pharmacyId = pharmacy?.Id ?? Guid.NewGuid();

                order = new Order
                {
                    Id = Guid.NewGuid(),
                    PharmacyId = pharmacyId,
                    CustomerId = userId,
                    UserId = userId,
                    OrderNumber = "ORD-" + DateTime.UtcNow.ToString("yyMMdd") + "-" + Guid.NewGuid().ToString("N")[..4].ToUpper(),
                    OrderType = "B2C_ONLINE",
                    PaymentStatus = "UNPAID",
                    PaymentMethod = "ONLINE",
                    Status = "PENDING",
                    DeliveryStatus = "PENDING",
                    Subtotal = sampleProduct?.UnitPrice ?? 120.00m,
                    Tax = 0.00m,
                    Discount = 0.00m,
                    TotalAmount = (sampleProduct?.UnitPrice ?? 120.00m) + 60.00m,
                    ShippingAddress = customer?.PersonalInfoNote ?? "123 Green Road, Dhaka",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);

                if (sampleProduct != null)
                {
                    _context.OrderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        MedicineId = sampleProduct.Id,
                        BatchId = Guid.Empty,
                        Quantity = 1,
                        UnitPrice = sampleProduct.UnitPrice,
                        TotalPrice = sampleProduct.UnitPrice,
                        LineTotal = sampleProduct.UnitPrice
                    });
                }

                await _context.SaveChangesAsync();
            }

            var items = new List<PaymentCheckoutItemViewModel>();

            if (order.OrderItems != null && order.OrderItems.Any())
            {
                foreach (var oi in order.OrderItems)
                {
                    var prod = await _context.StorefrontProducts.FirstOrDefaultAsync(p => p.Id == oi.MedicineId);
                    items.Add(new PaymentCheckoutItemViewModel
                    {
                        MedicineName = prod?.BrandName ?? "Prescribed Medicine",
                        DosageForm = "Capsule",
                        Strength = "Standard",
                        Manufacturer = "MediGuard Certified",
                        UnitPrice = oi.UnitPrice,
                        Quantity = oi.Quantity,
                        LineTotal = oi.LineTotal > 0 ? oi.LineTotal : oi.TotalPrice
                    });
                }
            }

            if (!items.Any())
            {
                items.Add(new PaymentCheckoutItemViewModel
                {
                    MedicineName = "Standard Medicine Package",
                    DosageForm = "Pack",
                    Strength = "General",
                    Manufacturer = "MediGuard Partner",
                    UnitPrice = order.Subtotal > 0 ? order.Subtotal : 120.00m,
                    Quantity = 1,
                    LineTotal = order.Subtotal > 0 ? order.Subtotal : 120.00m
                });
            }

            var model = new PaymentCheckoutViewModel
            {
                OrderId = order.Id,
                CustomerName = customer?.FullName ?? User.Identity?.Name ?? "Valued Customer",
                CustomerEmail = customer?.Email ?? "",
                CustomerPhone = customer?.PhoneNumber ?? "",
                ShippingAddress = customer?.PersonalInfoNote ?? "Default Delivery Address",
                PharmacyName = order.Pharmacy?.Name ?? "MediGuard Central Pharmacy",
                PharmacyAddress = order.Pharmacy?.Address ?? "Dhaka, Bangladesh",
                PharmacyVerified = true,
                Items = items,
                Subtotal = order.Subtotal > 0 ? order.Subtotal : items.Sum(i => i.LineTotal),
                DeliveryFee = 60.00m,
                TotalAmount = order.TotalAmount > 0 ? order.TotalAmount : (items.Sum(i => i.LineTotal) + 60.00m),
                CurrencySymbol = "৳",
                StripePublishableKey = _configuration["Stripe:PublishableKey"] ?? "pk_test_sampleKey12345"
            };

            return View("~/Views/Checkout/Payment.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateB2COrder([FromBody] CreateB2COrderDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "You must be signed in to checkout." });
            }

            if (dto == null || dto.Items == null || !dto.Items.Any())
            {
                return BadRequest(new { message = "Cart is empty." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            // Fetch products from database
            var productIds = dto.Items.Select(i => i.ProductId).ToList();
            var products = await _context.StorefrontProducts
                .Include(p => p.Pharmacy)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            if (!products.Any())
            {
                return BadRequest(new { message = "No valid products found for checkout." });
            }

            Guid pharmacyId = products.First().PharmacyId;
            decimal subtotal = 0m;
            var orderItems = new List<OrderItem>();
            var newOrderId = Guid.NewGuid();

            foreach (var item in dto.Items)
            {
                var prod = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (prod != null)
                {
                    int qty = item.Quantity > 0 ? item.Quantity : 1;
                    decimal lineTotal = prod.UnitPrice * qty;
                    subtotal += lineTotal;

                    orderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = newOrderId,
                        MedicineId = prod.Id,
                        BatchId = Guid.Empty,
                        Quantity = qty,
                        UnitPrice = prod.UnitPrice,
                        TotalPrice = lineTotal,
                        LineTotal = lineTotal
                    });
                }
            }

            decimal deliveryFee = 60.00m;
            var order = new Order
            {
                Id = newOrderId,
                PharmacyId = pharmacyId,
                CustomerId = userId,
                UserId = userId,
                OrderNumber = "ORD-" + DateTime.UtcNow.ToString("yyMMdd") + "-" + Guid.NewGuid().ToString("N")[..4].ToUpper(),
                OrderType = "B2C_ONLINE",
                PaymentStatus = "UNPAID",
                PaymentMethod = "ONLINE",
                Status = "PENDING",
                DeliveryStatus = "PENDING",
                Subtotal = subtotal,
                Tax = 0.00m,
                Discount = 0.00m,
                TotalAmount = subtotal + deliveryFee,
                ShippingAddress = !string.IsNullOrWhiteSpace(dto.ShippingAddress) ? dto.ShippingAddress : (user?.PersonalInfoNote ?? "Default Delivery Address"),
                CreatedAt = DateTime.UtcNow,
                OrderItems = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                orderId = order.Id,
                redirectUrl = $"/Checkout/Payment?orderId={order.Id}"
            });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(
            [FromQuery] Guid orderId,
            [FromQuery] string? status = null,
            [FromQuery] string? txId = null,
            [FromQuery] string? paymentMethod = null)
        {
            if (orderId == Guid.Empty)
            {
                return BadRequest("A valid order ID is required.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // Fetch logged-in Customer details for name & initials
            var customer = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            // Verify order ownership
            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);

            if (order == null)
            {
                return NotFound();
            }

            // Read latest payment attempt
            var payment = await _context.Payments
                .AsNoTracking()
                .Where(p => p.OrderId == order.Id)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            // Derive status from route parameters first; fallback to database
            var effectiveStatus = !string.IsNullOrWhiteSpace(status)
                ? status.Trim().ToUpperInvariant()
                : payment?.Status?.Trim().ToUpperInvariant() ?? "PENDING";

            var isSuccessful =
                effectiveStatus == "SUCCEEDED" ||
                effectiveStatus == "SUCCESS" ||
                effectiveStatus == "PAID" ||
                effectiveStatus == "COMPLETED";

            var isFailed =
                effectiveStatus == "FAILED" ||
                effectiveStatus == "CANCELED" ||
                effectiveStatus == "CANCELLED" ||
                effectiveStatus == "DECLINED";

            // Derive Gateway Reference ID
            var effectiveTxId = !string.IsNullOrWhiteSpace(txId)
                ? txId
                : (!string.IsNullOrWhiteSpace(payment?.GatewayTransactionId)
                    ? payment.GatewayTransactionId
                    : "N/A");

            // Derive Payment Method
            var effectivePaymentMethod = !string.IsNullOrWhiteSpace(paymentMethod)
                ? paymentMethod
                : (!string.IsNullOrWhiteSpace(payment?.PaymentMethod)
                    ? payment.PaymentMethod
                    : "Cash / Online Payment");

            // Derive Customer Name
            var displayName = !string.IsNullOrWhiteSpace(customer?.FullName)
                ? customer.FullName
                : (!string.IsNullOrWhiteSpace(User.Identity?.Name) ? User.Identity.Name : "Valued Customer");

            // Calculate Dynamic Initials from First Name, Middle Name, Last Name
            var nameParts = displayName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var computedInitials = string.Concat(nameParts.Select(part => char.ToUpper(part[0])));

            if (string.IsNullOrWhiteSpace(computedInitials))
            {
                computedInitials = "CU";
            }

            var model = new OrderConfirmationViewModel
            {
                CustomerName = displayName,
                CustomerInitials = computedInitials,

                OrderNumber = order.Id.ToString().Substring(0, 8).ToUpper(),
                PaymentStatus = effectiveStatus,
                IsPaymentSuccessful = isSuccessful,
                IsPaymentFailed = isFailed,
                TransactionReferenceId = effectiveTxId,
                TotalPaidAmount = isSuccessful && payment != null ? payment.Amount : order.TotalAmount,
                OrderTotalAmount = order.TotalAmount,
                PaymentMethod = effectivePaymentMethod,
                EstimatedDeliveryTimeframe = "Within 1-2 hours",
                PaymentStatusMessage = isSuccessful
                    ? "Your order has been placed successfully and is being prepared by the pharmacy."
                    : isFailed
                        ? "Your transaction could not be completed. Please check your details or try another payment method."
                        : "Your payment is currently awaiting confirmation from the payment gateway."
            };

            return View("~/Views/Checkout/Confirmation.cshtml", model);
        }

        // POST: /Checkout/ProcessDirectPayment
        [HttpPost]
        public async Task<IActionResult> ProcessDirectPayment([FromBody] DirectPaymentRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Authentication required." });
            }

            var order = await _context.Orders
                .Include(o => o.Pharmacy)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && (o.CustomerId == userId || o.UserId == userId));

            if (order == null)
            {
                return NotFound(new { message = "Order not found." });
            }

            string method = (request.Method ?? "COD").ToUpperInvariant();
            string txId = "TXN-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
            string paymentStatus = method == "COD" ? "UNPAID" : "PAID";

            order.PaymentMethod = method;
            order.PaymentStatus = paymentStatus;
            order.Status = "PENDING_APPROVAL";
            order.DeliveryStatus = "PENDING";
            if (!string.IsNullOrWhiteSpace(request.Address)) order.ShippingAddress = request.Address.Trim();
            if (!string.IsNullOrWhiteSpace(request.Instructions)) order.Notes = request.Instructions.Trim();

            // Create Payment record
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                PharmacyId = order.PharmacyId,
                OrderId = order.Id,
                Amount = order.TotalAmount,
                PaymentMethod = method,
                Status = paymentStatus == "PAID" ? "SUCCEEDED" : "PENDING",
                GatewayTransactionId = txId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Payments.Add(payment);

            // If user checked "save payment method"
            if (request.SaveMethod && !string.IsNullOrWhiteSpace(request.AccountNumber))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    var saved = user.SavedPaymentMethods;
                    string mfsType = method.Contains("BKASH") ? "BKASH" : (method.Contains("NAGAD") ? "NAGAD" : (method.Contains("ROCKET") ? "ROCKET" : "CARD"));
                    saved.Add(new SavedPaymentMethodDto
                    {
                        Type = mfsType,
                        Title = $"{mfsType} ({request.AccountNumber.Trim()})",
                        AccountNumber = request.AccountNumber.Trim(),
                        Expiry = request.CardExpiry
                    });
                    user.SavedPaymentMethods = saved;
                    await _context.SaveChangesAsync();
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                redirectUrl = $"/Checkout/Confirmation?orderId={order.Id}&status={(paymentStatus == "PAID" ? "SUCCEEDED" : "PENDING")}&txId={txId}&paymentMethod={method}"
            });
        }

        // POST: /Checkout/ReOrder
        [HttpPost]
        public async Task<IActionResult> ReOrder([FromForm] Guid orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var pastOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId && (o.CustomerId == userId || o.UserId == userId));

            if (pastOrder == null) return RedirectToAction("UserProfile", "Account");

            var newOrderId = Guid.NewGuid();
            var newOrder = new Order
            {
                Id = newOrderId,
                PharmacyId = pastOrder.PharmacyId,
                CustomerId = userId,
                UserId = userId,
                OrderNumber = "ORD-" + DateTime.UtcNow.ToString("yyMMdd") + "-" + Guid.NewGuid().ToString("N")[..4].ToUpper(),
                OrderType = "B2C_ONLINE",
                PaymentStatus = "UNPAID",
                PaymentMethod = "ONLINE",
                Status = "PENDING_APPROVAL",
                DeliveryStatus = "PENDING",
                Subtotal = pastOrder.Subtotal,
                Tax = pastOrder.Tax,
                Discount = pastOrder.Discount,
                TotalAmount = pastOrder.TotalAmount,
                ShippingAddress = pastOrder.ShippingAddress,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var item in pastOrder.OrderItems)
            {
                newOrder.OrderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = newOrderId,
                    MedicineId = item.MedicineId,
                    BatchId = Guid.Empty,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    LineTotal = item.LineTotal
                });
            }

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order #{pastOrder.OrderNumber} re-ordered! Review your items and complete payment.";
            return RedirectToAction("Payment", new { orderId = newOrderId });
        }

        // GET: /Checkout/Invoice/{orderId}
        [HttpGet]
        public async Task<IActionResult> Invoice(Guid orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var order = await _context.Orders
                .Include(o => o.Pharmacy)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Medicine)
                .Include(o => o.Customer)
                .Include(o => o.AssignedDeliveryman)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            var payment = await _context.Payments
                .Where(p => p.OrderId == order.Id)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            ViewBag.Payment = payment;
            return View("~/Views/Checkout/Invoice.cshtml", order);
        }
    }

    public class DirectPaymentRequest
    {
        public Guid OrderId { get; set; }
        public string Method { get; set; } = "COD"; // "COD", "BKASH", "NAGAD", "ROCKET", "CARD"
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Instructions { get; set; }
        public string? AccountNumber { get; set; }
        public string? CardExpiry { get; set; }
        public bool SaveMethod { get; set; }
    }
}