using HomemadeCookie.Api.Infrastructure;

using HomemadeCookie.Api.Patterns.Facade;

using HomemadeCookie.Api.Repositories;

using Microsoft.AspNetCore.Authentication.Cookies;

using Microsoft.Extensions.FileProviders;



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



builder.Services.AddSingleton<InventorySystem>();

builder.Services.AddSingleton<PaymentGateway>();

builder.Services.AddSingleton<EmailService>();

builder.Services.AddSingleton<OrderManagementFacade>();



builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)

    .AddCookie(options =>

    {

        options.Cookie.Name = "HomemadeCookie.Auth";

        options.Cookie.HttpOnly = true;

        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Events.OnRedirectToLogin = context =>

        {

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            return Task.CompletedTask;

        };

        options.Events.OnRedirectToAccessDenied = context =>

        {

            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            return Task.CompletedTask;

        };

    });



builder.Services.AddAuthorization();



builder.Services.AddCors(options =>

{

    options.AddDefaultPolicy(policy =>

        policy.SetIsOriginAllowed(_ => true)

            .AllowAnyHeader()

            .AllowAnyMethod()

            .AllowCredentials());

});



var app = builder.Build();

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

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();



app.Run();


