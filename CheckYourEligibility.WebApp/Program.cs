using Azure.Identity;
using CheckYourEligibility.Data;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.WebApp;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.OpenApi.Models;
using System.Diagnostics.CodeAnalysis;
using CheckYourEligibility.WebApp.Middleware;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using CheckYourEligibility.WebApp.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// ------------------------
// 1. Configure Services
// ------------------------

// Application Insights Telemetry
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    // Disable adaptive sampling to capture all telemetry
    options.EnableAdaptiveSampling = false;
});

// Register IHttpContextAccessor to access HttpContext in Telemetry Initializer
builder.Services.AddHttpContextAccessor();

// Register the TelemetryInitializer to attach user information to telemetry data
builder.Services.AddSingleton<ITelemetryInitializer, UserTelemetryInitializer>();

// Remove explicit TelemetryClient registration (it's automatically added by Application Insights)
// builder.Services.AddSingleton<TelemetryClient>(); // Removed

// Add Controllers with JSON options
builder.Services.AddControllers()
    .AddNewtonsoftJson()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Title = "ECS API - V1",
            Version = "v1"
        }
     );
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.\r\n\r\n 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
      {
        {
          new OpenApiSecurityScheme
          {
            Reference = new OpenApiReference
              {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
              },
              Scheme = "oauth2",
              Name = "Bearer",
              In = ParameterLocation.Header,

            },
            new List<string>()
          }
        });

    var filePath = Path.Combine(System.AppContext.BaseDirectory, "CheckYourEligibility.WebApp.xml");
    c.IncludeXmlComments(filePath);
});

// Configure Azure Key Vault if environment variable is set
if (Environment.GetEnvironmentVariable("KEY_VAULT_NAME") != null)
{
    var keyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME");
    var kvUri = $"https://{keyVaultName}.vault.azure.net";

    builder.Configuration.AddAzureKeyVault(new Uri(kvUri), new DefaultAzureCredential());
}

// Register Database and other services
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAzureClients(builder.Configuration);
builder.Services.AddServices();
builder.Services.AddExternalServices(builder.Configuration);

// Configure IIS and Kestrel server options
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = int.MaxValue; // Default is 30 MB
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(FsmMappingProfile));

// Add Authorization
builder.Services.AddAuthorization(builder.Configuration);

var app = builder.Build();

// ------------------------
// 2. Configure Middleware Pipeline
// ------------------------

// ------------------------
// 2.1. Custom Middlewares
// ------------------------

// Remove ExceptionLoggingMiddleware to allow Application Insights to handle exceptions automatically
// app.UseMiddleware<ExceptionLoggingMiddleware>(); // Removed

app.UseMiddleware<RequestBodyLoggingMiddleware>();
app.UseMiddleware<ResponseBodyLoggingMiddleware>();

// ------------------------
// 2.2. Exception Handling
// ------------------------

if (app.Environment.IsDevelopment())
{
    // DeveloperExceptionPage provides detailed exception information in Development
    app.UseDeveloperExceptionPage();
}
else
{
    // ExceptionHandler handles exceptions in Production and redirects to a generic error page
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// ------------------------
// 2.3. Swagger Middleware
// ------------------------

app.UseSwagger();
app.UseSwaggerUI();

// ------------------------
// 2.4. HTTPS Redirection
// ------------------------

app.UseHttpsRedirection();

// ------------------------
// 2.5. Authentication & Authorization
// ------------------------

app.UseAuthentication();
app.UseAuthorization();

// ------------------------
// 2.6. Map Controllers
// ------------------------

app.MapControllers();

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program { }
