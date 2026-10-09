using MediGuard.Data;
using MediGuard.Middlewares;
using MediGuard.Models;
using MediGuard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add MVC View Engine Services
builder.Services.AddControllersWithViews();

// Add Memory Cache Service (Required for UserDashboard ViewComponent)
builder.Services.AddMemoryCache();

// Fetch Connection String from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json.");

// Register ApplicationDbContext with Supabase PostgreSQL (Npgsql)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ASP.NET Core Hosted Background Service Registration
builder.Services.AddHostedService<ExpiryScannerHostedService>();

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// --- Register Application Services (Dependency Injection) ---

// Core Services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Catalog & Category Management Services (Ticket 22)
builder.Services.AddScoped<IMedicineService, MedicineService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IManufacturerService, ManufacturerService>();

// Inventory & Batch Management Service (Ticket 23)
builder.Services.AddScoped<IBatchService, BatchService>();

// Inventory Deduction Service (Ticket 30)
builder.Services.AddScoped<IInventoryDeductionService, InventoryDeductionService>();

// File Storage Service (Supabase Storage) Registration (Ticket 38)
builder.Services.AddHttpClient<IFileStorageService, SupabaseStorageService>();

// Prescription Management Service (Ticket 39)
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();

// Deliveryman & Order Fulfillment Service (Ticket 40)
builder.Services.AddScoped<IDeliveryService, DeliveryService>();

// Medi+ B2B Order Processing Service (Ticket 41)
builder.Services.AddScoped<IMediPlusB2BService, MediPlusB2BService>();

// --- Authentication & Security Configuration ---

// Configure Cookie Authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "MediGuard.AuthCookie";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Enable Custom Security / Tenant & RBAC Middleware
app.UseMiddleware<TenantRbacMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();