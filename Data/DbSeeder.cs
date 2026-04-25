using Microsoft.AspNetCore.Identity;
using SwiftFill.Models;

namespace SwiftFill.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndSuperAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Define the roles required for the application
            string[] roles = new[] { "SuperAdmin", "Admin", "WarehouseStaff", "DeliveryRider", "Customer" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ... (Super Admin and Admin seeding can stay similar or be adjusted)
            // Create the default Super Admin user if it doesn't already exist
            var superAdminUser = await userManager.FindByEmailAsync("superadmin@swiftfill.com");
            if (superAdminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = "superadmin@swiftfill.com",
                    Email = "superadmin@swiftfill.com",
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    PhoneNumber = "800-555-0199"
                };

                var result = await userManager.CreateAsync(newAdmin, "SuperAdmin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "SuperAdmin");
                    
                    // Assign all granular permissions to SuperAdmin by default
                    string[] permissions = { "Shipments", "Inventory", "Reports", "Billing" };
                    foreach (var perm in permissions)
                    {
                        await userManager.AddClaimAsync(newAdmin, new System.Security.Claims.Claim("Permission", perm));
                    }
                }
            }
            
            // Auto-promote Rhealyn's Google Account to SuperAdmin
            var rhealyn = await userManager.FindByEmailAsync("r.mayonila.547819@umindanao.edu.ph");
            if (rhealyn != null && !await userManager.IsInRoleAsync(rhealyn, "SuperAdmin"))
            {
                await userManager.AddToRoleAsync(rhealyn, "SuperAdmin");
            }

            // Create default Admin user
            var adminUser = await userManager.FindByEmailAsync("admin@swiftfill.com");
            if (adminUser == null)
            {
                var newStandardAdmin = new ApplicationUser
                {
                    UserName = "admin@swiftfill.com",
                    Email = "admin@swiftfill.com",
                    FirstName = "Warehouse",
                    LastName = "Manager",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newStandardAdmin, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newStandardAdmin, "Admin");
                }
            }

            // Create default Warehouse Staff user
            var staffUser = await userManager.FindByEmailAsync("staff@swiftfill.com");
            if (staffUser == null)
            {
                var newStaff = new ApplicationUser
                {
                    UserName = "staff@swiftfill.com",
                    Email = "staff@swiftfill.com",
                    FirstName = "Warehouse",
                    LastName = "Staff",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newStaff, "Staff123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newStaff, "WarehouseStaff");
                }
            }

            // Create default Delivery Rider user
            var riderUser = await userManager.FindByEmailAsync("rider@swiftfill.com");
            if (riderUser == null)
            {
                var newRider = new ApplicationUser
                {
                    UserName = "rider@swiftfill.com",
                    Email = "rider@swiftfill.com",
                    FirstName = "Delivery",
                    LastName = "Rider",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newRider, "Rider123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newRider, "DeliveryRider");
                }
            }

            // Create default Customer user
            var customerUser = await userManager.FindByEmailAsync("customer@swiftfill.com");
            if (customerUser == null)
            {
                var newCustomer = new ApplicationUser
                {
                    UserName = "customer@swiftfill.com",
                    Email = "customer@swiftfill.com",
                    FirstName = "Alex",
                    LastName = "Doe",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newCustomer, "Customer123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newCustomer, "Customer");
                }
            }
        }
    }
}
