// Boots the web app: registers EF Core (SQLite), Identity auth,
// Razor Pages + MVC, upload limits, and the HTTP pipeline (HTTPS,
// static files, routing, auth). Basically the plumbing so galleries,
// photos, and the reel stuff can actually work.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;
using PhotoGallery.Web.Data;
using PhotoGallery.Web.Models;
using PhotoGallery.Web.Services;
using Microsoft.AspNetCore.Identity.UI.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
{
    o.TokenLifespan = TimeSpan.FromMinutes(10);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddTransient<IEmailSender, PhotoGallery.Web.Services.SmtpEmailSender>();


builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

var provider = builder.Configuration["Storage:Provider"] ?? "AzureBlob";

if (provider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IFileStorage, AzureBlobFileStorage>(); 
}
else
{
    builder.Services.AddSingleton<IFileStorage, AzureBlobFileStorage>();
}

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024;
});


builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseMigrationsEndPoint();
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();   
app.UseAuthorization();

app.MapControllerRoute(name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();        

app.Run();
