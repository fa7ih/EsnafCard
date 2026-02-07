using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using SecureCardSystem.Authorization.Requirements;
using SecureCardSystem.Data;
using SecureCardSystem.Models;
using SecureCardSystem.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();


// =====================================
// DATABASE CONFIG (Local + Railway)
// =====================================
string? connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection") ??
    builder.Configuration["ConnectionStrings__DefaultConnection"] ??
    builder.Configuration["MYSQL_URL"];

if (!string.IsNullOrEmpty(connectionString))
{
    var csb = new MySqlConnectionStringBuilder(connectionString)
    {
        SslMode = MySqlSslMode.Required,
        AllowUserVariables = true
    };

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(
            csb.ConnectionString,
            ServerVersion.AutoDetect(csb.ConnectionString)
        )
    );

    // DataProtection keys DB'de tutulacak
    builder.Services.AddDataProtection()
        .PersistKeysToDbContext<ApplicationDbContext>();
}
else
{
    Console.WriteLine("WARNING: Database connection string not found.");
}


// =====================================
// IDENTITY
// =====================================
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


// =====================================
// COOKIE CONFIG
// =====================================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
});


// =====================================
// EMAIL + SERVICES
// =====================================
var emailConfig = builder.Configuration
    .GetSection("EmailConfiguration")
    .Get<EmailConfiguration>();

if (emailConfig != null)
    builder.Services.AddSingleton(emailConfig);

builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<CardService>();
builder.Services.AddScoped<OcrService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddHttpContextAccessor();


// =====================================
// AUTHORIZATION
// =====================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IpRestricted", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new IpRangeRequirement());
    });
});


var app = builder.Build();


// =====================================
// MIGRATION + SEED (SAFE)
// =====================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        context.Database.Migrate();

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        if (!await roleManager.RoleExistsAsync("User"))
            await roleManager.CreateAsync(new IdentityRole("User"));

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
                await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Migration/Seed error.");
    }
}


// =====================================
// PIPELINE
// =====================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(); // Railway için önemli

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
