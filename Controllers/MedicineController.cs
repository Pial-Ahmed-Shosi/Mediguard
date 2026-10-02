using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediGuard.Models.DTOs;
using MediGuard.Services;

namespace MediGuard.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MedicineController : ControllerBase
    {
        private readonly IMedicineService _medicineService;

        public MedicineController(IMedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        private Guid GetUserPharmacyId()
        {
            var pharmacyIdClaim = User.FindFirst("PharmacyId")?.Value ?? User.FindFirst(ClaimTypes.GroupSid)?.Value;
            if (Guid.TryParse(pharmacyIdClaim, out var pharmacyId))
            {
                return pharmacyId;
            }
            throw new UnauthorizedAccessException("Pharmacy context is missing from security context.");
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? term,
            [FromQuery] Guid? categoryId,
            [FromQuery] bool? rxOnly,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var pharmacyId = GetUserPharmacyId();
            var result = await _medicineService.SearchMedicinesAsync(pharmacyId, term, categoryId, rxOnly, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var pharmacyId = GetUserPharmacyId();
            var medicine = await _medicineService.GetMedicineByIdAsync(pharmacyId, id);

            if (medicine == null)
            {
                return NotFound(new { message = "Medicine not found." });
            }

            return Ok(medicine);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMedicineDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var pharmacyId = GetUserPharmacyId();
            var (success, message, medicineId) = await _medicineService.CreateMedicineAsync(pharmacyId, dto);

            if (!success)
            {
                return BadRequest(new { message });
            }

            return CreatedAtAction(nameof(GetById), new { id = medicineId }, new { message, id = medicineId });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMedicineDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            dto.Id = id;
            var pharmacyId = GetUserPharmacyId();
            var (success, message) = await _medicineService.UpdateMedicineAsync(pharmacyId, dto);

            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new { message });
        }
    }
}