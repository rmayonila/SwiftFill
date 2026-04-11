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
        public DbSet<HubAccessCode> HubAccessCodes { get; set; }
        public DbSet<ShippingRate> ShippingRates { get; set; }
        public DbSet<ManualRider> ManualRiders { get; set; }

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

            // One active code per hub — enforce at DB level
            builder.Entity<HubAccessCode>()
                .HasIndex(h => new { h.HubName, h.IsActive })
                .IsUnique(false); // allow history; uniqueness enforced in controller
        }
    }
}
