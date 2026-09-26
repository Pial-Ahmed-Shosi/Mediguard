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

        public async Task<(bool Success, string Message, string? TempPassword)> CreateStaffAsync(Guid pharmacyId, CreateStaffViewModel model)
        {
            if (!AllowedStaffRoles.Contains(model.Role, StringComparer.OrdinalIgnoreCase))
            {
                return (false, "Invalid role. Allowed roles: Pharmacist, Cashier, Deliveryman.", null);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return (false, "A user with this email address already exists.", null);
            }

            string tempPassword = GenerateTemporaryPassword();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PharmacyId = pharmacyId,
                Role = model.Role,
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, tempPassword);
            if (!result.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, $"Failed to create user: {errors}", null);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            return (true, "Staff account created successfully.", tempPassword);
        }

        public async Task<(bool Success, string Message)> UpdatePermissionsAsync(Guid pharmacyId, string userId, List<int> permissionIds)
        {
            // Strict tenant isolation check
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.PharmacyId == pharmacyId);
            if (targetUser == null)
            {
                return (false, "User not found or you do not have permission to modify this user.");
            }

            // Convert string userId to Guid for the UserPermission table
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return (false, "Invalid user ID format.");
            }

            // Fixed Line 124: Compare Guid to Guid
            var existingPermissions = _context.UserPermissions.Where(up => up.UserId == userGuid);
            _context.UserPermissions.RemoveRange(existingPermissions);

            if (permissionIds != null && permissionIds.Any())
            {
                // Fixed Line 131: Assign Guid value to UserId
                var newPermissions = permissionIds.Select(pid => new UserPermission
                {
                    UserId = userGuid,
                    PermissionId = pid
                });

                await _context.UserPermissions.AddRangeAsync(newPermissions);
            }

            await _context.SaveChangesAsync();
            return (true, "User permissions updated successfully.");
        }

        private static string GenerateTemporaryPassword()
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
            using var rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[10];
            rng.GetBytes(bytes);

            char[] chars = new char[10];
            for (int i = 0; i < 10; i++)
            {
                chars[i] = validChars[bytes[i] % validChars.Length];
            }
            return "Mg1!" + new string(chars);
        }
    }
}