using MediGuard.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<InventoryBatch> InventoryBatches { get; set; } = null!;
        public DbSet<Sale> Sales { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Delivery> Deliveries { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;

        // --- Inventory Classification Tables (Ticket 19) ---
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Manufacturer> Manufacturers { get; set; } = null!;

        // --- Expiry & Alerts Tables (Ticket 24) ---
        public DbSet<Notification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // -------------------------------------------------------------
            // ApplicationUser Configuration
            // -------------------------------------------------------------
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Pharmacy)
                .WithMany(p => p.Users)
                .HasForeignKey(u => u.PharmacyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.PharmacyId)
                .HasDatabaseName("idx_users_pharmacy_id");

            // -------------------------------------------------------------
            // Pharmacy Configuration
            // -------------------------------------------------------------
            builder.Entity<Pharmacy>()
                .HasIndex(p => p.Domain)
                .IsUnique()
                .HasDatabaseName("idx_pharmacies_domain");

            builder.Entity<Pharmacy>()
                .HasIndex(p => p.LicenseNumber)
                .IsUnique();

            // -------------------------------------------------------------
            // RBAC Configuration
            // -------------------------------------------------------------
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

            // -------------------------------------------------------------
            // Categories & Manufacturers Configuration (Ticket 19)
            // -------------------------------------------------------------
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

            // -------------------------------------------------------------
            // Medicine Entity Configuration (Ticket 20)
            // -------------------------------------------------------------
            builder.Entity<Medicine>(entity =>
            {
                entity.ToTable("medicines");

                entity.HasKey(m => m.Id);
                entity.Property(m => m.Id)
                      .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(m => m.BrandName)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(m => m.GenericName)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(m => m.RequiresPrescription)
                      .HasDefaultValue(false);

                entity.Property(m => m.Unit)
                      .HasMaxLength(30)
                      .HasDefaultValue("Tablet");

                entity.Property(m => m.Barcode)
                      .HasMaxLength(100);

                entity.Property(m => m.CreatedAt)
                      .HasColumnType("TIMESTAMPTZ")
                      .HasDefaultValueSql("NOW()");

                // Cascade delete when Pharmacy is deleted
                entity.HasOne(m => m.Pharmacy)
                      .WithMany()
                      .HasForeignKey(m => m.PharmacyId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Set NULL when Category is deleted
                entity.HasOne(m => m.Category)
                      .WithMany()
                      .HasForeignKey(m => m.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Set NULL when Manufacturer is deleted
                entity.HasOne(m => m.Manufacturer)
                      .WithMany()
                      .HasForeignKey(m => m.ManufacturerId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Barcode Index
                entity.HasIndex(m => new { m.PharmacyId, m.Barcode }, "idx_medicines_barcode");

                // Lookup Name Index
                entity.HasIndex(m => new { m.PharmacyId, m.BrandName })
                      .HasDatabaseName("idx_medicines_pharmacy_id_name");
            });

            // -------------------------------------------------------------
            // Legacy/Existing Batch Entity Configuration
            // -------------------------------------------------------------
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

            // -------------------------------------------------------------
            // InventoryBatch Entity Configuration (Ticket 20)
            // -------------------------------------------------------------
            builder.Entity<InventoryBatch>(entity =>
            {
                entity.ToTable("inventory_batches", t =>
                {
                    // Check Constraint: Quantity >= 0
                    t.HasCheckConstraint("CK_inventory_batches_quantity_non_negative", "\"Quantity\" >= 0");
                });

                entity.HasKey(b => b.Id);
                entity.Property(b => b.Id)
                      .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(b => b.BatchNumber)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(b => b.Quantity)
                      .IsRequired();

                entity.Property(b => b.PurchasePrice)
                      .HasColumnType("numeric(10,2)");

                entity.Property(b => b.SellingPrice)
                      .HasColumnType("numeric(10,2)");

                entity.Property(b => b.MfgDate)
                      .HasColumnType("DATE");

                entity.Property(b => b.ExpiryDate)
                      .HasColumnType("DATE");

                entity.Property(b => b.Status)
                      .HasMaxLength(30)
                      .HasDefaultValue("ACTIVE");

                entity.Property(b => b.CreatedAt)
                      .HasColumnType("TIMESTAMPTZ")
                      .HasDefaultValueSql("NOW()");

                // Cascade delete when Pharmacy is deleted
                entity.HasOne(b => b.Pharmacy)
                      .WithMany()
                      .HasForeignKey(b => b.PharmacyId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Cascade delete when Medicine is deleted
                entity.HasOne(b => b.Medicine)
                      .WithMany(m => m.InventoryBatches)
                      .HasForeignKey(b => b.MedicineId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Composite FEFO Index: idx_batches_fefo on (PharmacyId, MedicineId, Status, ExpiryDate ASC)
                entity.HasIndex(b => new { b.PharmacyId, b.MedicineId, b.Status, b.ExpiryDate }, "idx_batches_fefo");
            });

            // -------------------------------------------------------------
            // Order Entity Configuration
            // -------------------------------------------------------------
            builder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.Property(o => o.Id)
                      .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(o => o.Status)
                      .IsRequired()
                      .HasMaxLength(50)
                      .HasDefaultValue("PENDING");

                entity.Property(o => o.OrderStatus)
                      .IsRequired()
                      .HasMaxLength(50)
                      .HasDefaultValue("PENDING");

                entity.Property(o => o.TotalAmount)
                      .HasColumnType("decimal(10, 2)")
                      .HasDefaultValue(0m);

                entity.Property(o => o.CreatedAt)
                      .HasDefaultValueSql("NOW()");

                entity.HasIndex(o => new { o.PharmacyId, o.CreatedAt })
                      .HasDatabaseName("idx_orders_pharmacy_date");

                // Relationships
                entity.HasOne(o => o.Pharmacy)
                      .WithMany()
                      .HasForeignKey(o => o.PharmacyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(o => o.Customer)
                      .WithMany()
                      .HasForeignKey(o => o.CustomerId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(o => o.Deliveryman)
                      .WithMany()
                      .HasForeignKey(o => o.DeliverymanId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(o => o.SupplierPharmacy)
                      .WithMany()
                      .HasForeignKey(o => o.SupplierPharmacyId)
                      .OnDelete(DeleteBehavior.SetNull);

                // One-to-many with OrderItems
                entity.HasMany(o => o.OrderItems)
                      .WithOne(oi => oi.Order)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // -------------------------------------------------------------
            // OrderItem Entity Configuration
            // -------------------------------------------------------------
            builder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(oi => oi.Id);

                entity.Property(oi => oi.Id)
                      .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(oi => oi.Quantity)
                      .IsRequired();

                entity.Property(oi => oi.UnitPrice)
                      .HasColumnType("decimal(10, 2)")
                      .IsRequired();

                entity.Property(oi => oi.LineTotal)
                      .HasColumnType("decimal(10, 2)");

                entity.Property(oi => oi.CreatedAt)
                      .HasDefaultValueSql("NOW()");

                entity.HasIndex(oi => new { oi.OrderId, oi.MedicineId })
                      .HasDatabaseName("idx_orderitems_order_medicine");

                // Relationships
                entity.HasOne(oi => oi.Medicine)
                      .WithMany()
                      .HasForeignKey(oi => oi.MedicineId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // -------------------------------------------------------------
            // Delivery Entity Configuration
            // -------------------------------------------------------------
            builder.Entity<Delivery>(entity =>
            {
                entity.ToTable("deliveries");

                entity.HasKey(d => d.Id);

                entity.Property(d => d.Id)
                      .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(d => d.Status)
                      .IsRequired()
                      .HasMaxLength(20)
                      .HasDefaultValue("PENDING");

                entity.Property(d => d.DeliveryType)
                      .IsRequired()
                      .HasMaxLength(50)
                      .HasDefaultValue("STANDARD");

                entity.Property(d => d.CreatedAt)
                      .HasDefaultValueSql("NOW()");

                entity.HasIndex(d => new { d.OrderId, d.Status })
                      .HasDatabaseName("idx_deliveries_order_status");

                // Relationships
                entity.HasOne(d => d.Order)
                      .WithMany()
                      .HasForeignKey(d => d.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Deliveryman)
                      .WithMany()
                      .HasForeignKey(d => d.DeliverymanId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // -------------------------------------------------------------
            // Notification Configuration (Ticket 24)
            // -------------------------------------------------------------
            builder.Entity<Notification>(entity =>
            {
                entity.HasIndex(n => new { n.PharmacyId, n.BatchId, n.UrgencyLevel, n.IsRead })
                      .HasDatabaseName("idx_notifications_expiry_lookup");
            });

            // -------------------------------------------------------------
            // Seed Data Configuration
            // -------------------------------------------------------------
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