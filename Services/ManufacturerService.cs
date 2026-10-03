using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models;
using MediGuard.Models.ViewModels;

namespace MediGuard.Services
{
    public class ManufacturerService : IManufacturerService
    {
        private readonly ApplicationDbContext _context;

        public ManufacturerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Manufacturer>> GetManufacturersByPharmacyAsync(Guid pharmacyId)
        {
            return await GetAllAsync(pharmacyId);
        }

        public async Task<List<Manufacturer>> GetAllAsync(Guid pharmacyId)
        {
            return await _context.Manufacturers
                .AsNoTracking()
                .Where(m => m.PharmacyId == pharmacyId)
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<Manufacturer?> GetByIdAsync(Guid pharmacyId, Guid id)
        {
            return await _context.Manufacturers
                .FirstOrDefaultAsync(m => m.PharmacyId == pharmacyId && m.Id == id);
        }

        public async Task<(bool Success, string Message, Manufacturer? Manufacturer)> CreateAsync(Guid pharmacyId, CreateManufacturerViewModel model)
        {
            string trimmedName = model.Name.Trim();

            // Duplicate name check per tenant
            bool exists = await _context.Manufacturers
                .AnyAsync(m => m.PharmacyId == pharmacyId && m.Name.ToLower() == trimmedName.ToLower());

            if (exists)
            {
                return (false, $"A manufacturer named '{trimmedName}' already exists for your pharmacy.", null);
            }

            var manufacturer = new Manufacturer
            {
                Id = Guid.NewGuid(),
                PharmacyId = pharmacyId,
                Name = trimmedName,
                ContactEmail = model.ContactEmail?.Trim(),
                Phone = model.Phone?.Trim(),
                Address = model.Address?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Manufacturers.AddAsync(manufacturer);
            await _context.SaveChangesAsync();

            return (true, "Manufacturer created successfully.", manufacturer);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(Guid pharmacyId, EditManufacturerViewModel model)
        {
            var manufacturer = await _context.Manufacturers
                .FirstOrDefaultAsync(m => m.PharmacyId == pharmacyId && m.Id == model.Id);

            if (manufacturer == null)
            {
                return (false, "Manufacturer not found or access denied.");
            }

            string trimmedName = model.Name.Trim();

            bool exists = await _context.Manufacturers
                .AnyAsync(m => m.PharmacyId == pharmacyId && m.Name.ToLower() == trimmedName.ToLower() && m.Id != model.Id);

            if (exists)
            {
                return (false, $"Another manufacturer named '{trimmedName}' already exists.");
            }

            manufacturer.Name = trimmedName;
            manufacturer.ContactEmail = model.ContactEmail?.Trim();
            manufacturer.Phone = model.Phone?.Trim();
            manufacturer.Address = model.Address?.Trim();

            _context.Manufacturers.Update(manufacturer);
            await _context.SaveChangesAsync();

            return (true, "Manufacturer updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid pharmacyId, Guid id)
        {
            var manufacturer = await _context.Manufacturers
                .FirstOrDefaultAsync(m => m.PharmacyId == pharmacyId && m.Id == id);

            if (manufacturer == null)
            {
                return (false, "Manufacturer not found or access denied.");
            }

            _context.Manufacturers.Remove(manufacturer);
            await _context.SaveChangesAsync();

            return (true, "Manufacturer deleted successfully.");
        }

        public async Task<(bool Success, string Message, Manufacturer? Manufacturer)> CreateQuickAsync(Guid pharmacyId, QuickCreateManufacturerDto dto)
        {
            return await CreateAsync(pharmacyId, new CreateManufacturerViewModel
            {
                Name = dto.Name,
                ContactEmail = dto.ContactEmail,
                Phone = dto.Phone,
                Address = dto.Address
            });
        }
    }
}