using HomemadeCookie.Api.Infrastructure;
using HomemadeCookie.Api.Patterns.Facade;
using HomemadeCookie.Api.Repositories;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

DatabaseConnection.Instance.Configure(connectionString);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<CookieRepository>();
builder.Services.AddSingleton<CartRepository>();
builder.Services.AddSingleton<OrderRepository>();

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

var frontendPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "frontend"));
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
app.UseAuthorization();
app.MapControllers();

app.Run();
