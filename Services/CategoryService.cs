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
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllAsync(Guid pharmacyId)
        {
            return await _context.Categories
                .Where(c => c.PharmacyId == pharmacyId)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(Guid pharmacyId, Guid id)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.PharmacyId == pharmacyId && c.Id == id);
        }

        public async Task<(bool Success, string Message, Category? Category)> CreateAsync(Guid pharmacyId, CreateCategoryViewModel model)
        {
            string trimmedName = model.Name.Trim();

            // Duplicate check scoped to current tenant
            bool exists = await _context.Categories
                .AnyAsync(c => c.PharmacyId == pharmacyId && c.Name.ToLower() == trimmedName.ToLower());

            if (exists)
            {
                return (false, $"A category named '{trimmedName}' already exists for your pharmacy.", null);
            }

            var category = new Category
            {
                Id = Guid.NewGuid(),
                PharmacyId = pharmacyId,
                Name = trimmedName,
                Description = model.Description?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            return (true, "Category created successfully.", category);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(Guid pharmacyId, EditCategoryViewModel model)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.PharmacyId == pharmacyId && c.Id == model.Id);

            if (category == null)
            {
                return (false, "Category not found or access denied.");
            }

            string trimmedName = model.Name.Trim();

            // Duplicate check excluding current record
            bool exists = await _context.Categories
                .AnyAsync(c => c.PharmacyId == pharmacyId && c.Name.ToLower() == trimmedName.ToLower() && c.Id != model.Id);

            if (exists)
            {
                return (false, $"Another category named '{trimmedName}' already exists.");
            }

            category.Name = trimmedName;
            category.Description = model.Description?.Trim();

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return (true, "Category updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid pharmacyId, Guid id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.PharmacyId == pharmacyId && c.Id == id);

            if (category == null)
            {
                return (false, "Category not found or access denied.");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return (true, "Category deleted successfully.");
        }

        public async Task<(bool Success, string Message, Category? Category)> CreateQuickAsync(Guid pharmacyId, QuickCreateCategoryDto dto)
        {
            return await CreateAsync(pharmacyId, new CreateCategoryViewModel
            {
                Name = dto.Name,
                Description = dto.Description
            });
        }
    }
}