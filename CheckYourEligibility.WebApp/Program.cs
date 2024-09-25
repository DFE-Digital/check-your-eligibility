using Azure.Identity;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.WebApp;
using CheckYourEligibility.WebApp.Middleware;
using CheckYourEligibility.WebApp.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Azure;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ------------------------
// 1. Configure Services
// ------------------------

// Application Insights Telemetry
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    // Disable adaptive sampling for development to capture all telemetry
    options.EnableAdaptiveSampling = false;
});

// Register IHttpContextAccessor to access HttpContext in Telemetry Initializer
builder.Services.AddHttpContextAccessor();

// Register the TelemetryInitializer to attach user information to telemetry data
builder.Services.AddSingleton<ITelemetryInitializer, UserTelemetryInitializer>();

// Register TelemetryClient for manual tracking of telemetry data
builder.Services.AddSingleton<TelemetryClient>();

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
            Title = "ECE API - V1",
            Version = "v1",
            Description = "DFE Eligibility Checking Engine: API to perform Checks determining eligibility for entitlements via integration with OGDs",
          
            Contact = new OpenApiContact
            {
                Email = "Ian.HOWARD@education.gov.uk",
                Name = "Further Information",

            },
            License = new OpenApiLicense
            {
                Name = "Api Documentation",
                Url = new Uri("https://github.com/DFE-Digital/check-your-eligibility-documentation/blob/main/Runbook/System/API/Readme.md")
            }
           
        }
     );
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
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

// IMPORTANT:
// Register ExceptionLoggingMiddleware before any exception handling middleware.
// This ensures that all exceptions are logged, even those handled by DeveloperExceptionPage or ExceptionHandler.

app.UseMiddleware<ExceptionLoggingMiddleware>();
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
// 2.4. Inline Middleware for Logging Request and Response Bodies
// ------------------------

// NOTE:
// This inline middleware can be redundant if you have separate RequestBodyLoggingMiddleware and ResponseBodyLoggingMiddleware.
// Consider removing it if those middlewares sufficiently handle request and response logging.

app.Use(async (httpContext, next) =>
{
    try
    {
        // Log Request Body
        httpContext.Request.EnableBuffering();
        string requestBody = await new StreamReader(httpContext.Request.Body, Encoding.UTF8).ReadToEndAsync();
        httpContext.Request.Body.Position = 0;
        app.Logger.LogInformation($"Request body: {requestBody}");
    }
    catch (Exception ex)
    {
        app.Logger.LogError($"Exception reading request: {ex.Message}");
    }

    // Capture the original response body stream
    Stream originalBody = httpContext.Response.Body;

    try
    {
        using var memStream = new MemoryStream();
        httpContext.Response.Body = memStream;

        // Call the next middleware in the pipeline
        await next(httpContext);

        // Read the response body from the memory stream
        memStream.Position = 0;
        string responseBody = await new StreamReader(memStream).ReadToEndAsync();

        memStream.Position = 0;
        await memStream.CopyToAsync(originalBody); // Copy the response back to the original stream

        app.Logger.LogInformation($"Response body: {responseBody}");
    }
    finally
    {
        httpContext.Response.Body = originalBody; // Restore the original response body stream
    }
});

// ------------------------
// 2.5. HTTPS Redirection
// ------------------------

app.UseHttpsRedirection();

// ------------------------
// 2.6. Authentication & Authorization
// ------------------------

app.UseAuthentication();
app.UseAuthorization();

// ------------------------
// 2.7. Map Controllers
// ------------------------

app.MapControllers();

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program { };
