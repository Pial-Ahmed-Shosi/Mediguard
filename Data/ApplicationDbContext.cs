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

        // --- Core & Auth Tables ---
        public DbSet<Pharmacy> Pharmacies { get; set; } = null!;
        public DbSet<Role> CustomRoles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<UserPermission> UserPermissions { get; set; } = null!;

        // --- Operational & Inventory Tables ---
        public DbSet<Medicine> Medicines { get; set; } = null!;
        public DbSet<Batch> Batches { get; set; } = null!;
        public DbSet<Sale> Sales { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;

        // --- Inventory Classification Tables (Ticket 19) ---
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Manufacturer> Manufacturers { get; set; } = null!;

        // --- Expiry & Alerts Tables (Ticket 24) ---
        public DbSet<Notification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ApplicationUser Configuration
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Pharmacy)
                .WithMany(p => p.Users)
                .HasForeignKey(u => u.PharmacyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.PharmacyId)
                .HasDatabaseName("idx_users_pharmacy_id");

            // Pharmacy Configuration
            builder.Entity<Pharmacy>()
                .HasIndex(p => p.Domain)
                .IsUnique()
                .HasDatabaseName("idx_pharmacies_domain");

            builder.Entity<Pharmacy>()
                .HasIndex(p => p.LicenseNumber)
                .IsUnique();

            // --- RBAC Configuration ---
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

            builder.Entity<UserPermission>()
                .HasKey(up => new { up.UserId, up.PermissionId });

            builder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Categories & Manufacturers Configuration ---
            builder.Entity<Category>(entity =>
            {
                entity.HasIndex(c => new { c.PharmacyId, c.Name })
                      .IsUnique()
                      .HasDatabaseName("idx_categories_pharmacy_id_name");

                entity.HasOne(c => c.Pharmacy)
                      .WithMany(p => p.Categories)
                      .HasForeignKey(c => c.PharmacyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Manufacturer>(entity =>
            {
                entity.HasIndex(m => new { m.PharmacyId, m.Name })
                      .IsUnique()
                      .HasDatabaseName("idx_manufacturers_pharmacy_id_name");

                entity.HasOne(m => m.Pharmacy)
                      .WithMany(p => p.Manufacturers)
                      .HasForeignKey(m => m.PharmacyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // --- Medicine & Batch Inventory Configurations ---
            builder.Entity<Medicine>(entity =>
            {
                entity.HasIndex(m => new { m.PharmacyId, m.Name })
                      .HasDatabaseName("idx_medicines_pharmacy_id_name");

                entity.HasOne(m => m.Pharmacy)
                      .WithMany()
                      .HasForeignKey(m => m.PharmacyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Batch>(entity =>
            {
                entity.HasIndex(b => new { b.PharmacyId, b.MedicineId, b.Status, b.ExpiryDate })
                      .HasDatabaseName("idx_batches_fefo_lookup");

                entity.HasOne(b => b.Pharmacy)
                      .WithMany()
                      .HasForeignKey(b => b.PharmacyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(b => b.Medicine)
                      .WithMany(m => m.Batches)
                      .HasForeignKey(b => b.MedicineId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // --- Notification Configuration ---
            builder.Entity<Notification>(entity =>
            {
                entity.HasIndex(n => new { n.PharmacyId, n.BatchId, n.UrgencyLevel, n.IsRead })
                      .HasDatabaseName("idx_notifications_expiry_lookup");
            });

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