using LeaveManager.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
// Public demo credentials must never be enabled on a production deployment.
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("This learning milestone runs in Development only. See README.md.");

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<DemoUsers>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<LeaveBook>();
builder.Services.AddDataProtection().PersistKeysToFileSystem(
    new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".local-keys")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Denied";
        options.Cookie.Name = "LeaveManager.Demo";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = false;
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Leave}/{action=Index}/{id?}");
app.Run();
