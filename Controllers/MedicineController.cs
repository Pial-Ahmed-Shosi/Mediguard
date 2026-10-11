using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Services;
using MediGuard.Models.ViewModels;
using MediGuard.Models.ViewModels.Medicine;

namespace MediGuard.Controllers
{
    [Authorize]
    public class MedicineController : Controller
    {
        private readonly IMedicineService _medicineService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ApplicationDbContext _context;

        public MedicineController(
            IMedicineService medicineService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            ApplicationDbContext context)
        {
            _medicineService = medicineService;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _context = context;
        }

        private Guid GetCurrentPharmacyId()
        {
            var claim = User.FindFirst("PharmacyId") ?? User.FindFirst(ClaimTypes.GroupSid);
            if (claim != null && Guid.TryParse(claim.Value, out var pharmacyId))
            {
                return pharmacyId;
            }
            return Guid.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pharmacyId = GetCurrentPharmacyId();
            if (pharmacyId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Pharmacy context is missing.";
                return RedirectToAction("Index", "Home");
            }

            // Populate initial dropdowns dynamically from tenant-scoped data
            var categories = await _categoryService.GetCategoriesByPharmacyAsync(pharmacyId);
            var manufacturers = await _manufacturerService.GetManufacturersByPharmacyAsync(pharmacyId);

            var vm = new MedicineCatalogViewModel
            {
                Categories = categories.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList(),
                Manufacturers = manufacturers.Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> LoadMedicineTable(string? search, Guid? categoryId, Guid? manufacturerId, bool rxOnly)
        {
            var pharmacyId = GetCurrentPharmacyId();
            if (pharmacyId == Guid.Empty)
            {
                return BadRequest("Invalid pharmacy tenant context.");
            }

            var query = _context.Medicines
                .Include(m => m.Category)
                .Include(m => m.Manufacturer)
                .Where(m => m.PharmacyId == pharmacyId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(m => m.BrandName.ToLower().Contains(lowerSearch)
                                      || m.GenericName.ToLower().Contains(lowerSearch)
                                      || (m.Barcode != null && m.Barcode.ToLower().Contains(lowerSearch)));
            }

            if (categoryId.HasValue && categoryId.Value != Guid.Empty)
            {
                query = query.Where(m => m.CategoryId == categoryId);
            }

            if (manufacturerId.HasValue && manufacturerId.Value != Guid.Empty)
            {
                query = query.Where(m => m.ManufacturerId == manufacturerId);
            }

            if (rxOnly)
            {
                query = query.Where(m => m.IsRxOnly);
            }

            var medicines = await query.Select(m => new MedicineGridItemViewModel
            {
                Id = m.Id,
                BrandName = m.BrandName,
                GenericName = m.GenericName,
                Barcode = m.Barcode,
                CategoryName = m.Category != null ? m.Category.Name : "N/A",
                ManufacturerName = m.Manufacturer != null ? m.Manufacturer.Name : "N/A",
                Unit = m.Unit,
                Price = m.Price,
                IsRxOnly = m.IsRxOnly,
                // Ticket 20 Schema Compliance: Using InventoryBatches instead of Batches
                TotalAvailableStock = _context.InventoryBatches
                    .Where(b => b.MedicineId == m.Id && b.PharmacyId == pharmacyId && b.Status == "ACTIVE")
                    .Sum(b => (int?)b.Quantity) ?? 0
            }).ToListAsync();

            return PartialView("_MedicineTable", medicines);
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            string? term,
            Guid? categoryId,
            bool? rxOnly,
            int page = 1,
            int pageSize = 10)
        {
            var pharmacyId = GetCurrentPharmacyId();
            if (pharmacyId == Guid.Empty)
            {
                return BadRequest(new { success = false, message = "Invalid pharmacy tenant context." });
            }

            var result = await _medicineService.SearchMedicinesAsync(
                pharmacyId,
                term,
                categoryId,
                rxOnly,
                page,
                pageSize);

            return Json(new
            {
                success = true,
                data = result.Medicines,
                currentPage = result.CurrentPage,
                totalPages = result.TotalPages,
                totalCount = result.TotalCount,
                pageSize = result.PageSize
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var pharmacyId = GetCurrentPharmacyId();
            var medicine = await _medicineService.GetMedicineByIdAsync(id, pharmacyId);
            if (medicine == null)
            {
                return NotFound();
            }

            return View(medicine);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var pharmacyId = GetCurrentPharmacyId();
            await PopulateDropdownsAsync(pharmacyId);
            return View(new CreateMedicineViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMedicineViewModel model)
        {
            var pharmacyId = GetCurrentPharmacyId();

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(pharmacyId);
                return View(model);
            }

            var result = await _medicineService.CreateMedicineAsync(model, pharmacyId);
            if (!result.Success)
            {
                ModelState.AddModelError("Barcode", result.Message);
                await PopulateDropdownsAsync(pharmacyId);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var pharmacyId = GetCurrentPharmacyId();
            var medicine = await _medicineService.GetMedicineByIdAsync(id, pharmacyId);
            if (medicine == null)
            {
                return NotFound();
            }

            var editModel = new EditMedicineViewModel
            {
                Id = medicine.Id,
                PharmacyId = medicine.PharmacyId,
                BrandName = medicine.BrandName,
                GenericName = medicine.GenericName,
                Barcode = medicine.Barcode,
                CategoryId = medicine.CategoryId ?? Guid.Empty,
                ManufacturerId = medicine.ManufacturerId ?? Guid.Empty,
                IsRxOnly = medicine.IsRxOnly,
                Unit = medicine.Unit,
                Price = medicine.Price,
                IsActive = medicine.IsActive
            };

            await PopulateDropdownsAsync(pharmacyId, editModel.CategoryId, editModel.ManufacturerId);
            return View(editModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditMedicineViewModel model)
        {
            var pharmacyId = GetCurrentPharmacyId();

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(pharmacyId, model.CategoryId, model.ManufacturerId);
                return View(model);
            }

            var result = await _medicineService.UpdateMedicineAsync(model, pharmacyId);
            if (!result.Success)
            {
                ModelState.AddModelError("Barcode", result.Message);
                await PopulateDropdownsAsync(pharmacyId, model.CategoryId, model.ManufacturerId);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CheckBarcode(string barcode, Guid? excludeId)
        {
            var pharmacyId = GetCurrentPharmacyId();
            bool isUnique = await _medicineService.IsBarcodeUniqueAsync(barcode, pharmacyId, excludeId);
            return Json(new { isUnique });
        }

        private async Task PopulateDropdownsAsync(Guid pharmacyId, Guid? selectedCategory = null, Guid? selectedManufacturer = null)
        {
            var categories = await _categoryService.GetCategoriesByPharmacyAsync(pharmacyId);
            var manufacturers = await _manufacturerService.GetManufacturersByPharmacyAsync(pharmacyId);

            ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedCategory);
            ViewBag.Manufacturers = new SelectList(manufacturers, "Id", "Name", selectedManufacturer);
        }
    }
}