using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
var identityConn = builder.Configuration.GetConnectionString("IdentityConnection");

builder.Services.AddDbContext<VentCleanInventory.Web.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(defaultConn));

builder.Services.AddDbContext<VentCleanInventory.Web.Data.AppIdentityDbContext>(options =>
    options.UseSqlServer(identityConn));

builder.Services
    .AddIdentity<VentCleanInventory.Web.Data.ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<VentCleanInventory.Web.Data.AppIdentityDbContext>();

builder.Services.AddScoped<VentCleanInventory.Web.Services.StockService>();
builder.Services.AddScoped<VentCleanInventory.Web.Services.WriteOffActService>();
builder.Services.AddScoped<VentCleanInventory.Web.Services.BackupService>();
builder.Services.AddScoped<VentCleanInventory.Web.Services.NotificationService>();
builder.Services.AddHostedService<VentCleanInventory.Web.Services.BackgroundJobService>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 120;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.ContentType = "text/plain";
        await context.HttpContext.Response.WriteAsync("Слишком много запросов. Попробуйте позже.", ct);
    };
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new("ru-RU"),
    SupportedCultures = [new CultureInfo("ru-RU")],
    SupportedUICultures = [new CultureInfo("ru-RU")],
});

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=AdminHome}/{action=Index}/{id?}")
    .RequireRateLimiting("api");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure both databases exist
using (var seedScope = app.Services.CreateScope())
{
    await seedScope.ServiceProvider.GetRequiredService<VentCleanInventory.Web.Data.ApplicationDbContext>().Database.EnsureCreatedAsync();
    await seedScope.ServiceProvider.GetRequiredService<VentCleanInventory.Web.Data.AppIdentityDbContext>().Database.EnsureCreatedAsync();
}

await VentCleanInventory.Web.Data.DbSeeder.SeedAsync(app.Services, app.Environment);

app.Run();
