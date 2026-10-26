using MediGuard.Data;
using MediGuard.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediGuard.Controllers
{
    public class StorefrontController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StorefrontController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? pharmacy,
            string? location,
            string? category)
        {
            var query = _context.StorefrontProducts
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Pharmacy)
                .Where(p => p.IsActive && p.StockQuantity >= 0);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(p =>
                    p.BrandName.Contains(term) ||
                    p.GenericName.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(pharmacy))
            {
                string pharmTerm = pharmacy.Trim();
                query = query.Where(p =>
                    p.Pharmacy != null &&
                    p.Pharmacy.Name.Contains(pharmTerm));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                string locTerm = location.Trim();
                query = query.Where(p =>
                    p.Pharmacy != null &&
                    (p.Pharmacy.City.Contains(locTerm) || p.Pharmacy.Address.Contains(locTerm)));
            }

            if (!string.IsNullOrWhiteSpace(category) && !string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p =>
                    p.Category != null &&
                    p.Category.Name == category);
            }

            var products = await query
                .OrderBy(p => p.BrandName)
                .ToListAsync();

            var categories = await _context.StorefrontProducts
                .AsNoTracking()
                .Where(p => p.IsActive && p.Category != null)
                .Select(p => p.Category!.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();

            var pharmacies = await _context.Pharmacies
                .AsNoTracking()
                .Select(p => p.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();

            var locations = await _context.Pharmacies
                .AsNoTracking()
                .Select(p => p.City)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var model = new StorefrontViewModel
            {
                Products = products,
                Categories = categories,
                Pharmacies = pharmacies,
                Locations = locations,
                SearchTerm = search,
                PharmacyTerm = pharmacy,
                LocationTerm = location,
                SelectedCategory = category
            };

            return View(model);
        }
    }
}