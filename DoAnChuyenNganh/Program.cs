using DoAnChuyenNganh.Hubs;
using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.Providers;
using DoAnChuyenNganh.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using DoAnChuyenNganh.Services;

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine("🚀🚀🚀 ỨNG DỤNG ĐÃ KHỞI ĐỘNG 🚀🚀🚀");

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
    options.Cookie.SameSite = SameSiteMode.Lax;
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

// Add Chat Service
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddControllersWithViews();
// THÊM 3 DÒNG NÀY VÀO CÙNG NƠI VỚI CÁC SERVICE KHÁC
builder.Services.AddHttpClient();                                    // BẮT BUỘC cho MoMoService
builder.Services.AddScoped<IMomoService, MomoService>();            // ĐĂNG KÝ SERVICE
builder.Services.AddScoped<DoAnChuyenNganh.Services.MomoService>();
builder.Services.AddScoped<IPaypalService, PaypalService>();// (Tùy chọn, để an toàn)

builder.Services.AddScoped<IVnPayService, VnPayService>();


// Add News Service
builder.Services.AddScoped<INewsService, NewsService>();


// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Add Google Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.CallbackPath = "/signin-google";
        options.Scope.Add("email");
        options.Scope.Add("profile");
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"]!;
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"]!;
        options.CallbackPath = "/signin-facebook";
        options.Scope.Add("public_profile");
        options.Scope.Add("email");  
        options.Fields.Add("name");
        options.Fields.Add("email");
        options.Fields.Add("picture");      
    });

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
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Map SignalR Hubs
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<ChatHub>("/chatHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
