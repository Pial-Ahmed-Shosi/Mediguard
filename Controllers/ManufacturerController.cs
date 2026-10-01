using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediGuard.Filters;
using MediGuard.Models.ViewModels;
using MediGuard.Services;


namespace MediGuard.Controllers
{
    [Authorize]
    public class ManufacturerController : Controller
    {
        private readonly IManufacturerService _manufacturerService;

        public ManufacturerController(IManufacturerService manufacturerService)
        {
            _manufacturerService = manufacturerService;
        }

        // 1. Index View Route
        [HttpGet]
        [HasPermission("inventory.add")]
        public async Task<IActionResult> Index()
        {
            Guid pharmacyId = GetCurrentTenantId();
            var manufacturers = await _manufacturerService.GetAllAsync(pharmacyId);
            return View(manufacturers);
        }

        // 2. Create View Routes
        [HttpGet]
        [HasPermission("inventory.add")]
        public IActionResult Create()
        {
            return View(new CreateManufacturerViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("inventory.add")]
        public async Task<IActionResult> Create(CreateManufacturerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Guid pharmacyId = GetCurrentTenantId();
            var (success, message, _) = await _manufacturerService.CreateAsync(pharmacyId, model);

            if (!success)
            {
                ModelState.AddModelError("Name", message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        // 3. Edit View Routes
        [HttpGet]
        [HasPermission("inventory.edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            Guid pharmacyId = GetCurrentTenantId();
            var manufacturer = await _manufacturerService.GetByIdAsync(pharmacyId, id);

            if (manufacturer == null)
            {
                return NotFound();
            }

            var editModel = new EditManufacturerViewModel
            {
                Id = manufacturer.Id,
                Name = manufacturer.Name,
                ContactEmail = manufacturer.ContactEmail,
                Phone = manufacturer.Phone,
                Address = manufacturer.Address
            };

            return View(editModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("inventory.edit")]
        public async Task<IActionResult> Edit(EditManufacturerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Guid pharmacyId = GetCurrentTenantId();
            var (success, message) = await _manufacturerService.UpdateAsync(pharmacyId, model);

            if (!success)
            {
                ModelState.AddModelError("Name", message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        // 4. CreateQuick AJAX Modal Endpoint (Acceptance Criteria)
        [HttpPost]
        [HasPermission("inventory.add")]
        public async Task<IActionResult> CreateQuick([FromBody] QuickCreateManufacturerDto dto)
        {
            if (!ModelState.IsValid || dto == null || string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { success = false, message = "Manufacturer name is required." });
            }

            Guid pharmacyId = GetCurrentTenantId();
            var (success, message, manufacturer) = await _manufacturerService.CreateQuickAsync(pharmacyId, dto);

            if (!success || manufacturer == null)
            {
                return BadRequest(new { success = false, message });
            }

            // Exactly satisfies acceptance criteria: returns HTTP 200 with { id, name }
            return Ok(new
            {
                id = manufacturer.Id,
                name = manufacturer.Name
            });
        }

        // Helper: Extract Tenant ID from Middleware Context
        private Guid GetCurrentTenantId()
        {
            if (HttpContext.Items["PharmacyId"] is Guid pharmacyId)
            {
                return pharmacyId;
            }
            throw new UnauthorizedAccessException("Current user tenant identity (PharmacyId) is missing or invalid in context.");
        }
    }
}
