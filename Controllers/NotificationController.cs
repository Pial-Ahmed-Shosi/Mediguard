using Microsoft.AspNetCore.Mvc;
using MediGuard.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediGuard.Controllers
{
    public class NotificationController : Controller
    {
        // GET: /Notification/ExpiredMedicines
        public IActionResult ExpiredMedicines()
        {
            // Note: Ekhane database theke ashol data load hobe. 
            // Frontend test korar jonno nicher mock data use kora hoyeche.
            var viewModel = new ExpiredNotificationViewModel
            {
                TotalExpiredBatches = 2,
                ExpiringWithin30Days = 1,
                ExpiringWithin60Days = 1,
                FinancialValueAtRisk = 450.75m,
                BatchItems = new List<ExpiredBatchItemViewModel>
                {
                    new ExpiredBatchItemViewModel
                    {
                        BatchId = "BATCH-001",
                        MedicineName = "Napa Extra 500mg",
                        BatchNumber = "NX23-A",
                        AvailableQty = 150,
                        ExpiryDate = DateTime.Now.AddDays(-10),
                        DaysRemaining = -10,
                        ExpiryStatus = "EXPIRED",
                        UnitPrice = 1.50m
                    },
                    new ExpiredBatchItemViewModel
                    {
                        BatchId = "BATCH-002",
                        MedicineName = "Seclo 20mg",
                        BatchNumber = "SC-99",
                        AvailableQty = 40,
                        ExpiryDate = DateTime.Now.AddDays(15),
                        DaysRemaining = 15,
                        ExpiryStatus = "CRITICAL_30_DAYS",
                        UnitPrice = 5.0m
                    },
                     new ExpiredBatchItemViewModel
                    {
                        BatchId = "BATCH-003",
                        MedicineName = "Maxpro 20mg",
                        BatchNumber = "MP-45",
                        AvailableQty = 200,
                        ExpiryDate = DateTime.Now.AddDays(45),
                        DaysRemaining = 45,
                        ExpiryStatus = "WARNING_60_DAYS",
                        UnitPrice = 6.0m
                    }
                }
            };

            return View(viewModel);
        }

        // POST: /Notification/DisposeBatches
        [HttpPost]
        public IActionResult DisposeBatches([FromBody] DisposeBatchRequest request)
        {
            if (request == null || request.BatchIds == null || !request.BatchIds.Any())
            {
                return BadRequest("Invalid request or no batches selected.");
            }

            if (string.IsNullOrEmpty(request.Reason))
            {
                return BadRequest("Disposal reason is required.");
            }

            // TODO: Ekhane apnar InventoryService call kore database theke selected batch gulor qty 0/write-off korte hobe
            // Example: _inventoryService.WriteOffBatches(request.BatchIds, request.Reason);

            return Ok(new { success = true, message = "Batches disposed successfully." });
        }
    }

    // AJAX theke asha JSON data dhore rakhar jonno DTO class
    public class DisposeBatchRequest
    {
        public List<string> BatchIds { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
    }
}