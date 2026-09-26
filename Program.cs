using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediGuard.Data;
using MediGuard.Models;
using MediGuard.Services;
using MediGuard.Middlewares; // 1. ADD THIS USING STATEMENT

var builder = WebApplication.CreateBuilder(args);

// 1. Add MVC View Engine Services
builder.Services.AddControllersWithViews();

// 2. Fetch Connection String from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json.");

// 3. Register ApplicationDbContext with Supabase PostgreSQL (Npgsql)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 4. Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 5. Register Application Services (Dependency Injection)
builder.Services.AddScoped<IUserService, UserService>();

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

// 2. ADD THIS LINE RIGHT AFTER UseAuthorization()
app.UseMiddleware<TenantRbacMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();