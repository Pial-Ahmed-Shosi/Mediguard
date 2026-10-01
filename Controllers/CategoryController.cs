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
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // 1. Index View Route
        [HttpGet]
        [HasPermission("inventory.add")]
        public async Task<IActionResult> Index()
        {
            Guid pharmacyId = GetCurrentTenantId();
            var categories = await _categoryService.GetAllAsync(pharmacyId);
            return View(categories);
        }

        // 2. Create View Routes
        [HttpGet]
        [HasPermission("inventory.add")]
        public IActionResult Create()
        {
            return View(new CreateCategoryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("inventory.add")]
        public async Task<IActionResult> Create(CreateCategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Guid pharmacyId = GetCurrentTenantId();
            var (success, message, _) = await _categoryService.CreateAsync(pharmacyId, model);

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
            var category = await _categoryService.GetByIdAsync(pharmacyId, id);

            if (category == null)
            {
                return NotFound();
            }

            var editModel = new EditCategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };

            return View(editModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("inventory.edit")]
        public async Task<IActionResult> Edit(EditCategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Guid pharmacyId = GetCurrentTenantId();
            var (success, message) = await _categoryService.UpdateAsync(pharmacyId, model);

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
        public async Task<IActionResult> CreateQuick([FromBody] QuickCreateCategoryDto dto)
        {
            if (!ModelState.IsValid || dto == null || string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { success = false, message = "Category name is required." });
            }

            Guid pharmacyId = GetCurrentTenantId();
            var (success, message, category) = await _categoryService.CreateQuickAsync(pharmacyId, dto);

            if (!success || category == null)
            {
                return BadRequest(new { success = false, message });
            }

            // Exactly satisfies acceptance criteria: returns HTTP 200 with { id, name }
            return Ok(new
            {
                id = category.Id,
                name = category.Name
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