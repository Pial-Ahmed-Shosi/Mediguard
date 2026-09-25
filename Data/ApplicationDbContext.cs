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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure multi-tenant link (ApplicationUser -> Pharmacy) with CASCADE DELETE
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Pharmacy)
                .WithMany(p => p.Users)
                .HasForeignKey(u => u.PharmacyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Required Performance & Constraint Indexes from Ticket 8
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
        }
    }
}