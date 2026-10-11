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
            string? category)
        {
            var query = _context.StorefrontProducts
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity >= 0);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();

                query = query.Where(p =>
                    p.BrandName.Contains(term) ||
                    p.GenericName.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(category))
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
                .Where(p =>
                    p.IsActive &&
                    p.Category != null)
                .Select(p => p.Category!.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();

            var model = new StorefrontViewModel
            {
                Products = products,
                Categories = categories,
                SearchTerm = search,
                SelectedCategory = category
            };

            return View(model);
        }
    }
}