using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Models;

namespace SwiftFill.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<Manifest> Manifests { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ReturnRequest> ReturnRequests { get; set; }

        // ── New tables ──
        public DbSet<ShippingRate> ShippingRates { get; set; }
        public DbSet<ManualRider> ManualRiders { get; set; }
        public DbSet<SwiftFill.Services.AuditLogEntry> AuditLogs { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<BrandingSettings> BrandingSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Rename the default ASP.NET Identity tables
            builder.Entity<ApplicationUser>(entity => { entity.ToTable("users"); });
            builder.Entity<IdentityRole>(entity => { entity.ToTable("roles"); });
            builder.Entity<IdentityUserRole<string>>(entity => { entity.ToTable("user_roles"); });
            builder.Entity<IdentityUserClaim<string>>(entity => { entity.ToTable("user_claims"); });
            builder.Entity<IdentityUserLogin<string>>(entity => { entity.ToTable("user_logins"); });
            builder.Entity<IdentityRoleClaim<string>>(entity => { entity.ToTable("role_claims"); });
            builder.Entity<IdentityUserToken<string>>(entity => { entity.ToTable("user_tokens"); });

            // Configure Relationships
            builder.Entity<Order>()
                .HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.TrackingId)
                .HasPrincipalKey<Order>(o => o.TrackingId);

            builder.Entity<Order>()
                .HasOne(o => o.ReturnRequest)
                .WithOne(r => r.Order)
                .HasForeignKey<ReturnRequest>(r => r.TrackingId)
                .HasPrincipalKey<Order>(o => o.TrackingId);

            builder.Entity<Order>()
                .HasOne(o => o.Manifest)
                .WithMany(m => m.Orders)
                .HasForeignKey(o => o.ManifestId);

            builder.Entity<Order>()
                .HasOne(o => o.AssignedRider)
                .WithMany()
                .HasForeignKey(o => o.AssignedRiderId);

            // Warehouse Relationships
            builder.Entity<ManualRider>()
                .HasOne(m => m.Warehouse)
                .WithMany(w => w.ManualRiders)
                .HasForeignKey(m => m.WarehouseId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Order>()
                .HasOne(o => o.CurrentWarehouse)
                .WithMany(w => w.Orders)
                .HasForeignKey(o => o.CurrentWarehouseId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Order>()
                .HasOne(o => o.ShippingRate)
                .WithMany()
                .HasForeignKey(o => o.ShippingRateId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Order>()
                .HasOne(o => o.ManualRider)
                .WithMany()
                .HasForeignKey(o => o.ManualRiderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Payment Relationships
            builder.Entity<Payment>()
                .HasOne(p => p.PaymentMethod)
                .WithMany(pm => pm.Payments)
                .HasForeignKey(p => p.PaymentMethodId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<SwiftFill.Services.AuditLogEntry>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Decimal Precisions
            builder.Entity<Order>(entity => {
                entity.Property(e => e.PackingFee).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ShippingFee).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DeclaredValue).HasColumnType("decimal(18,2)");
            });

            builder.Entity<ShippingRate>(entity => {
                entity.Property(e => e.BaseRate).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PricePerKg).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ZoneSurcharge).HasColumnType("decimal(18,2)");
            });
        }
    }
}
