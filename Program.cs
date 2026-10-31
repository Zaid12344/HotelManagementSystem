using HotelManagement.Models.Data;
using HotelManagement.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// 1. Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Identity + roles (cookie auth)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// 3. Cookie paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// 4. MVC
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<HotelManagement.Services.ReservationService>();
builder.Services.AddScoped<HotelManagement.Services.CheckInOutService>();
builder.Services.Configure<HotelManagement.Models.Settings.HotelSettings>(
    builder.Configuration.GetSection("HotelSettings"));
builder.Services.AddScoped<HotelManagement.Services.BillingService>();
builder.Services.AddScoped<HotelManagement.Services.InvoicePdfService>();
builder.Services.AddScoped<HotelManagement.Services.HousekeepingService>();
builder.Services.AddScoped<HotelManagement.Services.MaintenanceService>();
builder.Services.AddScoped<HotelManagement.Services.ServiceRequestService>();
builder.Services.AddScoped<HotelManagement.Services.NotificationService>();
var app = builder.Build();

// 5. Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/500");
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
    app.UseHsts();
}
else
{
    // Even in development, show friendly 404 instead of blank page
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();   // ⚠️ MUST come before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// 6. Seed roles + admin on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        await DbSeeder.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Seeding failed");
    }
}

app.Run();