using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MediGuard.Models;

namespace MediGuard.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Pharmacy> Pharmacies { get; set; } = null!;
        public DbSet<Role> CustomRoles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<UserPermission> UserPermissions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Previous Ticket 8 Configuration
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Pharmacy)
                .WithMany(p => p.Users)
                .HasForeignKey(u => u.PharmacyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Pharmacy>()
                .HasIndex(p => p.Domain)
                .IsUnique()
                .HasDatabaseName("idx_pharmacies_domain");

            builder.Entity<Pharmacy>()
                .HasIndex(p => p.LicenseNumber)
                .IsUnique();

            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.PharmacyId)
                .HasDatabaseName("idx_users_pharmacy_id");

            // --- Ticket 9 (G3M-113) RBAC Configuration ---

            // Composite Primary Key for RolePermissions
            builder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            builder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite Primary Key for UserPermissions
            builder.Entity<UserPermission>()
                .HasKey(up => new { up.UserId, up.PermissionId });

            builder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed Standard Roles 1-5
            builder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Manager" },
                new Role { Id = 2, Name = "Pharmacist" },
                new Role { Id = 3, Name = "Cashier" },
                new Role { Id = 4, Name = "Deliveryman" },
                new Role { Id = 5, Name = "Customer" }
            );

            // Seed Default Permissions
            builder.Entity<Permission>().HasData(
                new Permission { Id = 1, Code = "pos.sell", Category = "POS" },
                new Permission { Id = 2, Code = "pos.discount", Category = "POS" },
                new Permission { Id = 3, Code = "inventory.add", Category = "Inventory" },
                new Permission { Id = 4, Code = "inventory.edit", Category = "Inventory" },
                new Permission { Id = 5, Code = "delivery.view_assigned", Category = "Delivery" },
                new Permission { Id = 6, Code = "delivery.update_status", Category = "Delivery" },
                new Permission { Id = 7, Code = "b2b.mediplus.access", Category = "B2B" }
            );
        }
    }
}