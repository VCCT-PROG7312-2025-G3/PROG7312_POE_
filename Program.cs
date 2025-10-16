using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;   // 👈 add
using PROG7312_POE.Data;
using PROG7312_POE.Services;
using PROG7312_POE.Domain;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// EF Core (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// file upload limit
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 40 * 1024 * 1024);

// In-memory Event store
builder.Services.AddSingleton<IEventStore, EventStore>();

// 👇 SIMPLE COOKIE AUTH
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});

var app = builder.Build();

// Seed demo events (unchanged) …
using (var scope = app.Services.CreateScope())
{
    var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
    if (!store.All().Any())
    {
        store.Add(new MunicipalEvent { Title = "Water Outage - Observatory", Description = "Scheduled maintenance on mains.", Category = EventCategory.Water, Start = DateTime.Today.AddDays(1).AddHours(9), Location = "Observatory, Cape Town", Priority = 4 });
        store.Add(new MunicipalEvent { Title = "Community Clean-up", Description = "Join the neighbourhood clean-up.", Category = EventCategory.Community, Start = DateTime.Today.AddDays(2).AddHours(10), Location = "Rondebosch Common", Priority = 2 });
        store.Add(new MunicipalEvent { Title = "Road Closure - Main Rd (Sea Point)", Description = "Storm damage repairs.", Category = EventCategory.Traffic, Start = DateTime.Today.AddDays(1).AddHours(6), Location = "Main Rd, Sea Point", Priority = 5 });
        store.Add(new MunicipalEvent { Title = "Water Outage - City Bowl", Description = "Scheduled maintenance from 08:00 to 17:00.", Category = EventCategory.Water, Start = DateTime.UtcNow.AddHours(1), Location = "Cape Town City Bowl", Priority = 4 });
        store.Add(new MunicipalEvent { Title = "Road Closure - Main Rd (Rondebosch)", Description = "Traffic diverted due to construction.", Category = EventCategory.Traffic, Start = DateTime.UtcNow.AddDays(1), Location = "Main Road, Rondebosch", Priority = 3 });
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();   // 👈 must be before authorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
