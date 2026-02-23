using CarRentalApi.Data;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    /*options.AddPolicy("AllowSpecificOrigin", builder =>
    {
        builder
            .SetIsOriginAllowed(origin =>
                origin == "https://cbtcarfront.blueberry-travel.com" ||
                origin == "https://cbtcarrentalapi.blueberry-travel.com" ||
                origin == "http://localhost:3000" ||
                origin == "http://localhost:3001" ||
                origin == "https://carrental.blueberry-travel.com" ||
                origin == "http://carrental.blueberry-travel.com" ||
                origin == "https://cbt-carlivefront.blueberry-travel.com" ||
                origin == "http://cbt-carlivefront.blueberry-travel.com" ||
                origin == "https://cbt-carrentalapilive.blueberry-travel.com" ||
                origin == "https://carrental.blueberry-travel.com" ||
                origin == "http://carrental.blueberry-travel.com" ||
                 origin == "http://localhost:7222" ||
                origin == "*")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });*/
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Add services to the container.

builder.Services.AddControllers();
var key = Encoding.ASCII.GetBytes("Harshit6h777b76r65dbw4@@@@567567"); // Replace with your actual secret key
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "harshitissue",
        ValidAudience = "HarshitAudience",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Harshit6h777b76r65dbw4@@@@567567Harshit6h777b76r65dbw4@@@@567567"))
    };
});
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DBConnectionString")
    ));


/*builder.Services.AddScoped<IImageUploadService, ImageUploadService>();*/
builder.Services.AddScoped<IImageUploadService>(provider =>
    new ImageUploadService(""));
builder.Services.AddHostedService<DocumentExpiryNotificationService>();
builder.Services.AddScoped<VehicleService>();

builder.Services.AddHttpClient<ICurrencyConversionService, CurrencyConversionService>(client =>
{
	client.BaseAddress = new Uri("https://maintravellerappapi.blueberry-travel.com/");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
	// Disable SSL certificate validation (for development only)
	ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
});

builder.Services.AddMemoryCache();

builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Car Rental API",
        Version = "v1"
    });

    // Add the Bearer Token Authorization to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer",
        Description = "Enter your Bearer token (JWT) in the format: Bearer {your_token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ILocationSyncService, LocationSyncService>();
builder.Services.AddHostedService<LocationSyncHostedService>();











var app = builder.Build();


/*app.UseCors("AllowSpecificOrigin");*/
app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "uploads")),
    RequestPath = "/uploads"
});
app.UseSession();


app.MapControllers();

app.Run();