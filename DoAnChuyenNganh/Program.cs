using DoAnChuyenNganh.Hubs;
using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.Providers;
using DoAnChuyenNganh.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext
builder.Services.AddDbContext<DoAnChuyenNganhContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Add SignalR for real-time notifications with custom user ID provider
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, SessionUserIdProvider>();

// Add Authentication Service
builder.Services.AddScoped<IAuthService, AuthService>();

// Add Forum Service
builder.Services.AddScoped<IForumService, ForumService>();

// Add File Upload Service
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

// Add Notification Service
builder.Services.AddScoped<INotificationService, NotificationService>();

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

// Map SignalR Hub
app.MapHub<NotificationHub>("/notificationHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
