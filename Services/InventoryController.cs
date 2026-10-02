using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models.ViewModels;
using MediGuard.Services;

namespace MediGuard.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly IBatchService _batchService;
        private readonly ApplicationDbContext _context;

        public InventoryController(IBatchService batchService, ApplicationDbContext context)
        {
            _batchService = batchService;
            _context = context;
        }

        // GET: /Inventory/CreateBatch
        [HttpGet]
        public async Task<IActionResult> CreateBatch()
        {
            var pharmacyId = GetCurrentPharmacyId();
            var model = new BatchEntryViewModel
            {
                MedicineList = await GetMedicineSelectListAsync(pharmacyId)
            };

            return View(model);
        }

        // POST: /Inventory/CreateBatch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBatch(BatchEntryViewModel model)
        {
            var pharmacyId = GetCurrentPharmacyId();

            if (!ModelState.IsValid)
            {
                model.MedicineList = await GetMedicineSelectListAsync(pharmacyId);
                return View(model);
            }

            var (success, message, createdBatch) = await _batchService.CreateBatchAsync(model, pharmacyId);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                model.MedicineList = await GetMedicineSelectListAsync(pharmacyId);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Batch '{model.BatchNumber}' added successfully.";
            return RedirectToAction(nameof(CreateBatch));
        }

        // GET: /Inventory/GetBatchesByMedicine?medicineId=...
        [HttpGet]
        public async Task<IActionResult> GetBatchesByMedicine(Guid medicineId)
        {
            if (medicineId == Guid.Empty)
            {
                return BadRequest("Invalid Medicine ID.");
            }

            var pharmacyId = GetCurrentPharmacyId();
            var batches = await _batchService.GetActiveBatchesFEFOAsync(medicineId, pharmacyId);

            return Json(batches);
        }

        #region Helper Methods

        private Guid GetCurrentPharmacyId()
        {
            var claimValue = User.FindFirstValue("PharmacyId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claimValue, out var pharmacyId) ? pharmacyId : Guid.Empty;
        }

        private async Task<IEnumerable<SelectListItem>> GetMedicineSelectListAsync(Guid pharmacyId)
        {
            return await _context.Medicines
                .AsNoTracking()
                .Where(m => m.PharmacyId == pharmacyId && m.IsActive)
                .OrderBy(m => m.Name)
                .Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = m.Name
                })
                .ToListAsync();
        }

        #endregion
    }
}