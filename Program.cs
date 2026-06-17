using HomemadeCookie.Api.Infrastructure;
using HomemadeCookie.Api.Patterns.Facade;
using HomemadeCookie.Api.Repositories;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

DatabaseConnection.Instance.Configure(connectionString);

builder.Services.AddControllers();

// Repositories
builder.Services.AddSingleton<CartRepository>();
builder.Services.AddSingleton<CategoryRepository>();
builder.Services.AddSingleton<CookieRepository>();
builder.Services.AddSingleton<OrderRepository>();
builder.Services.AddSingleton<ReviewRepository>();
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<WishlistRepository>();

// Infrastructure Services
builder.Services.AddSingleton<InventorySystem>();
builder.Services.AddSingleton<PaymentGateway>();
builder.Services.AddSingleton<EmailService>();

// Facade Pattern - Register both the interface and implementation
builder.Services.AddSingleton<IOrderNotificationFacade, OrderNotificationFacade>(); // ADD THIS
builder.Services.AddSingleton<OrderManagementFacade>();
builder.Services.AddScoped<EmailService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddAuthorization();

// Simple cookie authentication for demo purposes
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login.html";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
        options.Events = new CookieAuthenticationEvents
        {
            OnSigningIn = ctx =>
            {
                // ensure role claim exists
                var identity = (ClaimsIdentity?)ctx.Principal?.Identity;
                if (identity != null && !identity.HasClaim(c => c.Type == ClaimTypes.Role))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, "Customer"));
                }
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

app.UseCors();

var frontendPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "frontend"));
if (Directory.Exists(frontendPath))
{
    var frontendProvider = new PhysicalFileProvider(frontendPath);
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = frontendProvider,
        RequestPath = ""
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = frontendProvider,
        RequestPath = ""
    });
}
// In Program.cs, before app.Run()
app.MapGet("/favicon.ico", () => Results.NotFound());

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseStaticFiles();

app.Run();
