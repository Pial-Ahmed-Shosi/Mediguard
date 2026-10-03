using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MediGuard.Services;
using MediGuard.Models.ViewModels.Medicine;

namespace MediGuard.Controllers
{
    [Authorize]
    public class MedicineController : Controller
    {
        private readonly IMedicineService _medicineService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;

        public MedicineController(
            IMedicineService medicineService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService)
        {
            _medicineService = medicineService;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
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
        public async Task<IActionResult> Index(
            string? term,
            Guid? categoryId,
            bool? rxOnly,
            int page = 1,
            int pageSize = 10)
        {
            var pharmacyId = GetCurrentPharmacyId();
            if (pharmacyId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Pharmacy context is missing.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _medicineService.SearchMedicinesAsync(
                pharmacyId,
                term,
                categoryId,
                rxOnly,
                page,
                pageSize);

            var categories = await _categoryService.GetCategoriesByPharmacyAsync(pharmacyId);
            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);

            return View(result);
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