using mk8.identity.Application;
using mk8.identity.Infrastructure;
using mk8.identity.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

var isDevelopment = builder.Environment.IsDevelopment();

// Add Infrastructure (DbContexts) with environment-based configuration
builder.Services.AddInfrastructureServices(isDevelopment);

// Add Application services
builder.Services.AddApplicationServices();

// Add authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Member", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("ActiveMember", policy => policy.RequireClaim("IsActiveMember", "true"));
    options.AddPolicy("Staff", policy => policy.RequireRole("Administrator", "Assessor", "Moderator", "Support"));
    options.AddPolicy("Assessor", policy => policy.RequireRole("Administrator", "Assessor"));
    options.AddPolicy("Admin", policy => policy.RequireRole("Administrator"));
});

builder.Services.AddRazorPages();

var app = builder.Build();

// Seed database on startup
await app.Services.SeedDatabaseAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
