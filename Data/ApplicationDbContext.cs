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

        // --- Operational & Dashboard Tables ---
        public DbSet<Sale> Sales { get; set; } = null!;
        public DbSet<InventoryBatch> Batches { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;

        // --- Inventory Classification Tables (Ticket 19 & 20) ---
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Manufacturer> Manufacturers { get; set; } = null!;
        public DbSet<Medicine> Medicines { get; set; } = null!;

        // --- Expiry & Alerts Tables (Ticket 24) ---
        public DbSet<Notification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // --- Ticket 8: Core User & Tenant Setup ---
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

            // --- Ticket 9: RBAC Configuration ---
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

            // --- Ticket 19: Categories & Manufacturers Configuration ---
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

            // =========================================================================
            // --- Ticket 20: Medicines & Inventory Batches Configuration ---
            // =========================================================================

            // 1. Medicines Table & Indexes
            builder.Entity<Medicine>(entity =>
            {
                entity.ToTable("medicines");

                // Barcode Lookup Index
                entity.HasIndex(m => new { m.PharmacyId, m.Barcode })
                      .HasDatabaseName("idx_medicines_barcode");

                // Cascade Rules
                entity.HasOne(m => m.Pharmacy)
                      .WithMany(p => p.Medicines)
                      .HasForeignKey(m => m.PharmacyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Category)
                      .WithMany(c => c.Medicines)
                      .HasForeignKey(m => m.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(m => m.Manufacturer)
                      .WithMany(mf => mf.Medicines)
                      .HasForeignKey(m => m.ManufacturerId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // 2. Inventory Batches Table, FEFO Composite Index & Check Constraint
            builder.Entity<InventoryBatch>(entity =>
            {
                entity.ToTable("inventory_batches");

                // FEFO Composite Index (PharmacyId, MedicineId, Status, ExpiryDate)
                entity.HasIndex(b => new { b.PharmacyId, b.MedicineId, b.Status, b.ExpiryDate })
                      .HasDatabaseName("idx_batches_fefo");

                // Database Engine Level Check Constraint (Quantity >= 0)
                entity.ToTable(t => t.HasCheckConstraint("CK_inventory_batches_quantity", "quantity >= 0"));

                // Cascade Rules
                entity.HasOne(b => b.Pharmacy)
                      .WithMany(p => p.Batches)
                      .HasForeignKey(b => b.PharmacyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(b => b.Medicine)
                      .WithMany(m => m.InventoryBatches)
                      .HasForeignKey(b => b.MedicineId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // --- Ticket 24: Notifications Configuration ---
            builder.Entity<Notification>(entity =>
            {
                entity.HasIndex(n => new { n.PharmacyId, n.BatchId, n.UrgencyLevel, n.IsRead })
                      .HasDatabaseName("idx_notifications_expiry_lookup");
            });

            // --- Seed Standard Roles 1-5 ---
            builder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Manager" },
                new Role { Id = 2, Name = "Pharmacist" },
                new Role { Id = 3, Name = "Cashier" },
                new Role { Id = 4, Name = "Deliveryman" },
                new Role { Id = 5, Name = "Customer" }
            );

            // --- Seed Default Permissions ---
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