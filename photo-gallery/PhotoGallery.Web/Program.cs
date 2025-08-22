// Spins up the app and wires the essentials: database (SQLite by default),
// Identity with confirmed emails, Razor Pages + MVC, file storage, email
// sender, upload size limits, and the HTTP pipeline. Basically the plumbing.
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;          
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using PhotoGallery.Web.Data;
using PhotoGallery.Web.Models;                             
using PhotoGallery.Web.Services;                            

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(conn))                       
    conn = "Data Source=app.db";                            

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(conn));                             

// Email confirmation links last for 10minutes only
builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
{
    o.TokenLifespan = TimeSpan.FromMinutes(10);             
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();


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


builder.Services.AddSingleton<IFileStorage, AzureBlobFileStorage>();   


if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEmailSender, DevEmailSender>();   
}
else
{
    builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();    
}


builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
