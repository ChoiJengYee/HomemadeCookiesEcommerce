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

builder.Services.AddSingleton<CookieRepository>();
builder.Services.AddSingleton<CartRepository>();
builder.Services.AddSingleton<OrderRepository>();
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<WishlistRepository>();
builder.Services.AddSingleton<CategoryRepository>();

builder.Services.AddSingleton<InventorySystem>();
builder.Services.AddSingleton<PaymentGateway>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<OrderManagementFacade>();

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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
