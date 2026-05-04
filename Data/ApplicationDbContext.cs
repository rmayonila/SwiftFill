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
        public DbSet<ItemCategory> ItemCategories { get; set; }

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

            // Seed Data
            builder.Entity<PaymentMethod>().HasData(
                new PaymentMethod { Id = 1, Name = "COD", Description = "Collect on Delivery", IconClass = "bi-cash-stack", IsActive = true },
                new PaymentMethod { Id = 2, Name = "GCash", Description = "Digital Wallet (GCash)", IconClass = "bi-phone-fill", IsActive = true, IsOnline = true },
                new PaymentMethod { Id = 3, Name = "Bank Transfer", Description = "Direct Bank Deposit", IconClass = "bi-bank", IsActive = true, IsOnline = true }
            );

            builder.Entity<ItemCategory>().HasData(
                new ItemCategory { Id = 1, Name = "General Merchandise", Description = "Common household and commercial goods." },
                new ItemCategory { Id = 2, Name = "Electronics", Description = "Devices, gadgets, and computer parts." },
                new ItemCategory { Id = 3, Name = "Apparel", Description = "Clothing, shoes, and textiles." },
                new ItemCategory { Id = 4, Name = "Documents", Description = "Paper-based items and files." }
            );

            // Seed Roles
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "SuperAdmin", NormalizedName = "SUPERADMIN" },
                new IdentityRole { Id = "2", Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = "3", Name = "WarehouseStaff", NormalizedName = "WAREHOUSESTAFF" },
                new IdentityRole { Id = "4", Name = "DeliveryRider", NormalizedName = "DELIVERYRIDER" },
                new IdentityRole { Id = "5", Name = "Customer", NormalizedName = "CUSTOMER" }
            );

            // Seed SuperAdmin User
            var hasher = new PasswordHasher<ApplicationUser>();
            builder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                    Id = "a18be9c0-aa65-4af8-bd17-00bd9344e575",
                    UserName = "superadmin",
                    NormalizedUserName = "SUPERADMIN",
                    Email = "superadmin@swiftfill.com",
                    NormalizedEmail = "SUPERADMIN@SWIFTFILL.COM",
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null!, "SuperAdmin123!"),
                    SecurityStamp = string.Empty,
                    PhoneNumber = "800-555-0199"
                }
            );

            // Link User to Role
            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string> { UserId = "a18be9c0-aa65-4af8-bd17-00bd9344e575", RoleId = "1" },
                new IdentityUserRole<string> { UserId = "b18be9c0-aa65-4af8-bd17-00bd9344e576", RoleId = "2" },
                new IdentityUserRole<string> { UserId = "c18be9c0-aa65-4af8-bd17-00bd9344e577", RoleId = "3" },
                new IdentityUserRole<string> { UserId = "d18be9c0-aa65-4af8-bd17-00bd9344e578", RoleId = "4" },
                new IdentityUserRole<string> { UserId = "e18be9c0-aa65-4af8-bd17-00bd9344e579", RoleId = "5" }
            );

            builder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                    Id = "b18be9c0-aa65-4af8-bd17-00bd9344e576",
                    UserName = "admin",
                    NormalizedUserName = "ADMIN",
                    Email = "admin@swiftfill.com",
                    NormalizedEmail = "ADMIN@SWIFTFILL.COM",
                    FirstName = "Warehouse",
                    LastName = "Manager",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null!, "Admin123!"),
                    SecurityStamp = string.Empty
                },
                new ApplicationUser
                {
                    Id = "c18be9c0-aa65-4af8-bd17-00bd9344e577",
                    UserName = "staff",
                    NormalizedUserName = "STAFF",
                    Email = "staff@swiftfill.com",
                    NormalizedEmail = "STAFF@SWIFTFILL.COM",
                    FirstName = "Warehouse",
                    LastName = "Staff",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null!, "Staff123!"),
                    SecurityStamp = string.Empty
                },
                new ApplicationUser
                {
                    Id = "d18be9c0-aa65-4af8-bd17-00bd9344e578",
                    UserName = "rider",
                    NormalizedUserName = "RIDER",
                    Email = "rider@swiftfill.com",
                    NormalizedEmail = "RIDER@SWIFTFILL.COM",
                    FirstName = "Delivery",
                    LastName = "Rider",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null!, "Rider123!"),
                    SecurityStamp = string.Empty
                },
                new ApplicationUser
                {
                    Id = "e18be9c0-aa65-4af8-bd17-00bd9344e579",
                    UserName = "customer",
                    NormalizedUserName = "CUSTOMER",
                    Email = "customer@swiftfill.com",
                    NormalizedEmail = "CUSTOMER@SWIFTFILL.COM",
                    FirstName = "Alex",
                    LastName = "Doe",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null!, "Customer123!"),
                    SecurityStamp = string.Empty
                }
            );
        }
    }
}
