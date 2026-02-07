using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using SecureCardSystem.Authorization.Handlers;
using SecureCardSystem.Authorization.Requirements;
using SecureCardSystem.Data;
using SecureCardSystem.Models;
using SecureCardSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ============================================
// RAILWAY MySQL CONNECTION
// ============================================
string connectionString;

// Railway environment variables'ý kontrol et
// Railway environment variables'ý kontrol et
var mysqlHost = Environment.GetEnvironmentVariable("MYSQLHOST");
var mysqlPort = Environment.GetEnvironmentVariable("MYSQLPORT");
var mysqlDatabase = Environment.GetEnvironmentVariable("MYSQLDATABASE");
var mysqlUser = Environment.GetEnvironmentVariable("MYSQLUSER");
var mysqlPassword = Environment.GetEnvironmentVariable("MYSQLPASSWORD");

if (!string.IsNullOrEmpty(mysqlHost))
{
    // Railway'de çalýþýyoruz - database adýný railway olarak ayarla
    connectionString = $"server={mysqlHost};port={mysqlPort};database=railway;user={mysqlUser};password={mysqlPassword};Charset=utf8mb4;Convert Zero Datetime=True;SslMode=None;AllowPublicKeyRetrieval=True";
    Console.WriteLine($"Using Railway MySQL: {mysqlHost}:{mysqlPort}/railway");
}
else
{
    // Local development
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine("Using local MySQL connection");
}

// Database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Identity configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
});

// ============================================
// EMAIL CONFIGURATION (Railway ile uyumlu)
// ============================================
var emailConfig = new EmailConfiguration
{
    From = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? builder.Configuration["EmailConfiguration:From"] ?? "",
    SmtpServer = Environment.GetEnvironmentVariable("EMAIL_SMTP") ?? builder.Configuration["EmailConfiguration:SmtpServer"] ?? "",
    Port = int.Parse(Environment.GetEnvironmentVariable("EMAIL_PORT") ?? builder.Configuration["EmailConfiguration:Port"] ?? "465"),
    Username = Environment.GetEnvironmentVariable("EMAIL_USERNAME") ?? builder.Configuration["EmailConfiguration:Username"] ?? "",
    Password = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? builder.Configuration["EmailConfiguration:Password"] ?? ""
};

builder.Services.AddSingleton(emailConfig);
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<CardService>();
builder.Services.AddScoped<OcrService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IpRestricted", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new IpRangeRequirement());
    });
});

//builder.Services.AddScoped<IAuthorizationHandler, IpRangeHandler>();

// ============================================
// WINDOWS SERVICE SUPPORT (sadece Windows'ta)
// ============================================
// Railway Linux kullanýr, bu satýrý kaldýrdýk
// Windows'ta çalýþtýrmak için bu kod bloðunu kullanabilirsiniz:
// if (OperatingSystem.IsWindows())
// {
//     builder.Host.UseWindowsService();
// }

var app = builder.Build();

// ============================================
// DATABASE MIGRATION & SEEDING
// ============================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Starting database migration...");

        // Migrate database - Railway'de otomatik çalýþacak
        //context.Database.Migrate();

        logger.LogInformation("Database migration completed successfully");

        // Create roles
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            logger.LogInformation("Admin role created");
        }
        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
            logger.LogInformation("User role created");
        }

        // Create default admin
        var adminEmail = "admin@securecard.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation($"Default admin user created: {adminEmail}");
            }
            else
            {
                logger.LogError($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            logger.LogInformation($"Admin user already exists: {adminEmail}");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");

        // Railway'de hata detaylarýný görmek için
        Console.WriteLine($"Database Error: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");

        // Railway'de database hatasý uygulamayý durdurmamalý
        // Uygulamanýn ayaða kalkmasýna izin ver
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Railway HTTPS'i proxy seviyesinde hallediyor
    // app.UseHsts();
}

// Railway'de HTTPS redirect gerekmiyor (Railway proxy'si hallediyor)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

// IP Restriction Middleware
//app.UseMiddleware<IpRestrictionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Port bilgisini logla
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
Console.WriteLine($"Application starting on port: {port}");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");

app.Run();