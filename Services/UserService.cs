using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models;
using MediGuard.Models.ViewModels;

namespace MediGuard.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private static readonly string[] AllowedStaffRoles = { "Pharmacist", "Cashier", "Deliveryman" };

        public UserService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<StaffPagedListViewModel> GetStaffListAsync(Guid pharmacyId, string search, string roleFilter, int page, int pageSize = 10)
        {
            // Strict tenant isolation: filter by PharmacyId (Guid)
            var query = _context.Users
                .Where(u => u.PharmacyId == pharmacyId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchLower = search.ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(searchLower) || (u.Email != null && u.Email.ToLower().Contains(searchLower)));
            }

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                query = query.Where(u => u.Role == roleFilter);
            }

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var users = await query
                .OrderBy(u => u.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new StaffUserListItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? string.Empty,
                    Role = u.Role ?? string.Empty,
                    PharmacyId = u.PharmacyId ?? Guid.Empty,
                    IsActive = u.IsActive,
                    AssignedPermissionIds = _context.UserPermissions
                        .Where(up => up.UserId.ToString() == u.Id) // Fixed Line 60: Convert Guid up.UserId to string
                        .Select(up => up.PermissionId)
                        .ToList()
                })
                .ToListAsync();

            return new StaffPagedListViewModel
            {
                Users = users,
                CurrentPage = page,
                TotalPages = totalPages,
                Search = search,
                RoleFilter = roleFilter
            };
        }
