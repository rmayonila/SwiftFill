using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SwiftFill.Data;
using SwiftFill.Models;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 12;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


// Add services to the container.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in SwiftFill.Models.Permissions.GetAll())
    {
        options.AddPolicy(permission, policy => policy.RequireClaim("Permission", permission));
    }
    
    // Legacy support or combined policies
    options.AddPolicy("CanManageShipments", policy => policy.RequireClaim("Permission", SwiftFill.Models.Permissions.Shipments.Edit));
    options.AddPolicy("CanManageInventory", policy => policy.RequireClaim("Permission", SwiftFill.Models.Permissions.Inventory.Edit));
    options.AddPolicy("CanViewReports", policy => policy.RequireClaim("Permission", SwiftFill.Models.Permissions.Finance.View));
    options.AddPolicy("CanManageBilling", policy => policy.RequireClaim("Permission", SwiftFill.Models.Permissions.Finance.Edit));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SwiftFill.Services.OrderService>();
builder.Services.AddSingleton<SwiftFill.Services.AuditLogService>();
builder.Services.AddHttpClient<SwiftFill.Services.JawgMapsService>();
builder.Services.AddScoped<SwiftFill.Services.JawgMapsService>();
builder.Services.AddScoped<SwiftFill.Services.CloudinaryService>();
builder.Services.AddHttpClient();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Global Status Code Handling (404, 500, etc)
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
