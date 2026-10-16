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
        public DbSet<Payment> Payments { get; set; } = null!;

        // --- Public Storefront Catalog (Ticket 31) ---
        public DbSet<StorefrontProduct> StorefrontProducts { get; set; } = null!;

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

                entity.HasOne(m => m.Pharmacy)
                      .WithMany()
                      .HasForeignKey(m => m.PharmacyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Category)
                      .WithMany()
                      .HasForeignKey(m => m.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(m => m.Manufacturer)
                      .WithMany()
                      .HasForeignKey(m => m.ManufacturerId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(m => new { m.PharmacyId, m.Barcode }, "idx_medicines_barcode");

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

                entity.HasOne(b => b.Pharmacy)
                      .WithMany()
                      .HasForeignKey(b => b.PharmacyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(b => b.Medicine)
                      .WithMany(m => m.InventoryBatches)
                      .HasForeignKey(b => b.MedicineId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(b => new { b.PharmacyId, b.MedicineId, b.Status, b.ExpiryDate }, "idx_batches_fefo");
            });

            // -------------------------------------------------------------
            // Order Entity Configuration (Ticket 28 & Ticket 37)
            // -------------------------------------------------------------
            builder.Entity<Order>(entity =>
            {
                entity.ToTable("orders");

                entity.HasKey(o => o.Id);
                entity.Property(o => o.Id)
                      .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(o => o.OrderNumber)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.HasIndex(o => o.OrderNumber)
                      .IsUnique()
                      .HasDatabaseName("idx_orders_order_number_unique");

                entity.Property(o => o.OrderType)
                      .IsRequired()
                      .HasMaxLength(30)
                      .HasDefaultValue("POS");

                entity.Property(o => o.IsB2BOrder)
                      .IsRequired()
                      .HasDefaultValue(false);

                entity.Property(o => o.DeliveryStatus)
                      .IsRequired()
                      .HasMaxLength(30)
                      .HasDefaultValue("NOT_REQUIRED");

                entity.Property(o => o.Subtotal)
                      .HasColumnType("numeric(10,2)")
                      .IsRequired();

                entity.Property(o => o.Tax)
                      .HasColumnType("numeric(10,2)")
                      .HasDefaultValue(0.00m);

                entity.Property(o => o.Discount)
                      .HasColumnType("numeric(10,2)")
                      .HasDefaultValue(0.00m);

                entity.Property(o => o.TotalAmount)
                      .HasColumnType("numeric(10,2)")
                      .IsRequired();

                entity.Property(o => o.PaymentStatus)
                      .IsRequired()
                      .HasMaxLength(30)
                      .HasDefaultValue("UNPAID");

                entity.Property(o => o.PaymentMethod)
                      .IsRequired()
                      .HasMaxLength(30)
                      .HasDefaultValue("CASH");

                entity.Property(o => o.CreatedAt)
                      .HasColumnType("TIMESTAMPTZ")
                      .HasDefaultValueSql("NOW()");

                entity.HasIndex(o => new { o.PharmacyId, o.CreatedAt })
                      .HasDatabaseName("idx_orders_pharmacy_date");

                // Composite Index for Delivery Dashboard Queries (Ticket 37)
                entity.HasIndex(o => new { o.AssignedDeliverymanId, o.DeliveryStatus })
                      .HasDatabaseName("idx_orders_delivery");

                // Relationships & Delete Behaviors
                entity.HasOne(o => o.Pharmacy)
                      .WithMany()
                      .HasForeignKey(o => o.PharmacyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(o => o.User)
                      .WithMany()
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(o => o.Customer)
                      .WithMany()
                      .HasForeignKey(o => o.CustomerId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(o => o.Deliveryman)
                      .WithMany()
                      .HasForeignKey(o => o.DeliverymanId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(o => o.AssignedDeliveryman)
                      .WithMany()
                      .HasForeignKey(o => o.AssignedDeliverymanId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(o => o.SupplierPharmacy)
                      .WithMany()
                      .HasForeignKey(o => o.SupplierPharmacyId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(o => o.OrderItems)
                      .WithOne(oi => oi.Order)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // -------------------------------------------------------------
            // OrderItem Entity Configuration (Ticket 28)
            // -------------------------------------------------------------
            builder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("order_items");

                entity.HasKey(oi => oi.Id);
                entity.Property(oi => oi.Id)
                      .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(oi => oi.Quantity)
                      .IsRequired();

                entity.Property(oi => oi.UnitPrice)
                      .HasColumnType("numeric(10,2)")
                      .IsRequired();

                entity.Property(oi => oi.TotalPrice)
                      .HasColumnType("numeric(10,2)")
                      .IsRequired();

                entity.Property(oi => oi.LineTotal)
                      .HasColumnType("numeric(10,2)");

                entity.Property(oi => oi.CreatedAt)
                      .HasColumnType("TIMESTAMPTZ")
                      .HasDefaultValueSql("NOW()");

                entity.HasIndex(oi => new { oi.OrderId, oi.MedicineId })
                      .HasDatabaseName("idx_orderitems_order_medicine");

                entity.HasOne(oi => oi.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.Medicine)
                      .WithMany()
                      .HasForeignKey(oi => oi.MedicineId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(oi => oi.Batch)
                      .WithMany()
                      .HasForeignKey(oi => oi.BatchId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // -------------------------------------------------------------
            // Prescription Entity Configuration (Ticket 36)
            // -------------------------------------------------------------
            builder.Entity<Prescription>(entity =>
            {
                entity.ToTable("prescriptions");

                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id)
                      .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(p => p.FilePath)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(p => p.FileType)
                      .IsRequired()
                      .HasMaxLength(20)
                      .HasDefaultValue("jpg");

                entity.Property(p => p.Status)
                      .IsRequired()
                      .HasMaxLength(30)
                      .HasDefaultValue("PENDING");

                entity.Property(p => p.RejectionReason)
                      .HasMaxLength(500);

                entity.Property(p => p.CreatedAt)
                      .HasColumnType("TIMESTAMPTZ")
                      .HasDefaultValueSql("NOW()");

                entity.HasOne(p => p.Order)
                      .WithMany()
                      .HasForeignKey(p => p.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Customer)
                      .WithMany()
                      .HasForeignKey(p => p.CustomerId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(p => p.VerifiedBy)
                      .WithMany()
                      .HasForeignKey(p => p.VerifiedById)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(p => p.Pharmacy)
                      .WithMany()
                      .HasForeignKey(p => p.PharmacyId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // -------------------------------------------------------------
            // Delivery Entity Configuration (Ticket 36)
            // -------------------------------------------------------------
            builder.Entity<Delivery>(entity =>
            {
                entity.ToTable("deliveries");

                entity.HasKey(d => d.Id);
                entity.Property(d => d.Id)
                      .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(d => d.DeliverymanId)
                      .IsRequired();

                entity.Property(d => d.DeliveryType)
                      .IsRequired()
                      .HasMaxLength(20)
                      .HasDefaultValue("B2C");

                entity.Property(d => d.Status)
                      .IsRequired()
                      .HasMaxLength(30)
                      .HasDefaultValue("PENDING");

                entity.Property(d => d.CancellationReason)
                      .HasMaxLength(500);

                entity.Property(d => d.AssignedAt)
                      .HasColumnType("TIMESTAMPTZ")
                      .HasDefaultValueSql("NOW()");

                entity.Property(d => d.CompletedAt)
                      .HasColumnType("TIMESTAMPTZ");

                entity.HasIndex(d => new { d.OrderId, d.Status })
                      .HasDatabaseName("idx_deliveries_order_status");

                entity.HasOne(d => d.Order)
                      .WithMany()
                      .HasForeignKey(d => d.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Deliveryman)
                      .WithMany()
                      .HasForeignKey(d => d.DeliverymanId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // -------------------------------------------------------------
            // Payment Ledger Configuration (Ticket 44)
            // -------------------------------------------------------------
            builder.Entity<Payment>(entity =>
            {
                entity.ToTable("payments", t =>
                {
                    t.HasCheckConstraint("CK_payments_payment_method", "\"PaymentMethod\" IN ('CARD', 'WALLET', 'BANK')");
                    t.HasCheckConstraint("CK_payments_status", "\"Status\" IN ('PENDING', 'SUCCESS', 'FAILED')");
                });

                entity.HasKey(p => p.Id);

                entity.Property(p => p.Id)
                      .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(p => p.GatewayTransactionId)
                      .HasMaxLength(150);

                entity.HasIndex(p => p.GatewayTransactionId)
                      .IsUnique()
                      .HasDatabaseName("idx_payments_gateway_tx_unique");

                entity.Property(p => p.Amount)
                      .HasColumnType("decimal(10,2)")
                      .IsRequired();

                entity.Property(p => p.Currency)
                      .IsRequired()
                      .HasMaxLength(10)
                      .HasDefaultValue("USD");

                entity.Property(p => p.PaymentMethod)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(p => p.Status)
                      .IsRequired()
                      .HasMaxLength(30)
                      .HasDefaultValue("PENDING");

                entity.Property(p => p.GatewayResponseJson)
                      .HasColumnType("jsonb");

                entity.Property(p => p.CreatedAt)
                      .HasColumnType("TIMESTAMPTZ")
                      .HasDefaultValueSql("NOW()");

                entity.HasIndex(p => p.OrderId)
                      .HasDatabaseName("idx_payments_order");

                entity.HasOne(p => p.Order)
                      .WithMany()
                      .HasForeignKey(p => p.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Pharmacy)
                      .WithMany()
                      .HasForeignKey(p => p.PharmacyId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // -------------------------------------------------------------
            // Storefront Product Configuration (Ticket 31)
            // -------------------------------------------------------------
            builder.Entity<StorefrontProduct>(entity =>
            {
                entity.ToTable("storefront_products");

                entity.HasIndex(p => new { p.PharmacyId, p.IsActive });
                entity.HasIndex(p => p.CategoryId);

                entity.HasOne(p => p.Pharmacy)
                      .WithMany()
                      .HasForeignKey(p => p.PharmacyId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Category)
                      .WithMany()
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.Property(p => p.UnitPrice)
                      .HasPrecision(12, 2);
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
            builder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Manager" },
                new Role { Id = 2, Name = "Pharmacist" },
                new Role { Id = 3, Name = "Cashier" },
                new Role { Id = 4, Name = "Deliveryman" },
                new Role { Id = 5, Name = "Customer" }
            );

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