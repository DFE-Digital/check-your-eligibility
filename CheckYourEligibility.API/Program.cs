using Azure.Identity;
using CheckYourEligibility.API.Data.Mappings;
using CheckYourEligibility.API;
using CheckYourEligibility.API.Telemetry;
using CheckYourEligibility.API.UseCases;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Azure;
using Microsoft.OpenApi.Models;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Swashbuckle.AspNetCore.SwaggerGen;

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-GB");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-GB");

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

// Add Controllers with JSON options
builder.Services.AddControllers()
    .AddNewtonsoftJson()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Configure Azure Key Vault if environment variable is set
if (Environment.GetEnvironmentVariable("API_KEY_VAULT_NAME") != null)
{
    var keyVaultName = Environment.GetEnvironmentVariable("API_KEY_VAULT_NAME");
    var kvUri = $"https://{keyVaultName}.vault.azure.net";

    builder.Configuration.AddAzureKeyVault(
        new Uri(kvUri), 
        new DefaultAzureCredential(),
        new AzureKeyVaultConfigurationOptions()
        {
            ReloadInterval = TimeSpan.FromSeconds(60*10)
        }
    );
}

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1-admin",
        new OpenApiInfo
        {
            Title = "ECE API - V1",
            Version = "v1-admin",
            Description = "DFE Eligibility Checking Engine: API to perform Checks determining eligibility for entitlements via integration with OGDs"
        }
     );
    c.SwaggerDoc("v1", 
        new OpenApiInfo
        {
            Title = "ECE Local Authority API - V1", 
            Version = "v1",
            Description = "DFE Eligibility Checking Engine: API to perform Checks determining eligibility for entitlements via integration with OGDs"
        });

    c.AddSecurityDefinition(
        "oauth2", 
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    TokenUrl = new Uri(builder.Configuration.GetValue<string>("Host")+"/oauth2/token"),
                    Scopes = builder.Configuration.GetSection("Jwt").GetSection("Scopes").Get<List<string>>().ToDictionary(x => x, x => x)
                }
            }
        });

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement 
        {
            {
                new OpenApiSecurityScheme{
                    Reference = new OpenApiReference{
                        Id = "oauth2", //The name of the previously defined security scheme.
                        Type = ReferenceType.SecurityScheme
                    }
                },
                new List<string>()
            }
        });

    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (!apiDesc.TryGetMethodInfo(out MethodInfo methodInfo)) return false;
        
        if(docName=="v1-admin") return true;
        if(apiDesc.RelativePath.StartsWith("check/")) return true;
        if(apiDesc.RelativePath.StartsWith("bulk-check/")) return true;
        
        return false;
    });
    
    var filePath = Path.Combine(System.AppContext.BaseDirectory, "CheckYourEligibility.API.xml");
    c.IncludeXmlComments(filePath);
});

// Register Database and other services
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAzureClients(builder.Configuration);
builder.Services.AddServices();
builder.Services.AddExternalServices(builder.Configuration);
builder.Services.AddJwtSettings(builder.Configuration);

// Use cases
builder.Services.AddScoped<ICreateOrUpdateUserUseCase, CreateOrUpdateUserUseCase>();
builder.Services.AddScoped<IAuthenticateUserUseCase, AuthenticateUserUseCase>();
builder.Services.AddScoped<IMatchCitizenUseCase, MatchCitizenUseCase>();
builder.Services.AddScoped<IGetCitizenClaimsUseCase, GetCitizenClaimsUseCase>();
builder.Services.AddScoped<ISearchEstablishmentsUseCase, SearchEstablishmentsUseCase>();
builder.Services.AddScoped<ICleanUpEligibilityChecksUseCase, CleanUpEligibilityChecksUseCase>();
builder.Services.AddScoped<IImportEstablishmentsUseCase, ImportEstablishmentsUseCase>();
builder.Services.AddScoped<IImportFsmHomeOfficeDataUseCase, ImportFsmHomeOfficeDataUseCase>();
builder.Services.AddScoped<IImportFsmHMRCDataUseCase, ImportFsmHMRCDataUseCase>();
builder.Services.AddScoped<ICreateApplicationUseCase, CreateApplicationUseCase>();
builder.Services.AddScoped<IGetApplicationUseCase, GetApplicationUseCase>();
builder.Services.AddScoped<ISearchApplicationsUseCase, SearchApplicationsUseCase>();
builder.Services.AddScoped<IUpdateApplicationStatusUseCase, UpdateApplicationStatusUseCase>();
builder.Services.AddScoped<IProcessQueueMessagesUseCase, ProcessQueueMessagesUseCase>();
builder.Services.AddScoped<ICheckEligibilityForFSMUseCase, CheckEligibilityForFSMUseCase>();
builder.Services.AddScoped<ICheckEligibilityBulkUseCase, CheckEligibilityBulkUseCase>();
builder.Services.AddScoped<IGetBulkUploadProgressUseCase, GetBulkUploadProgressUseCase>();
builder.Services.AddScoped<IGetBulkUploadResultsUseCase, GetBulkUploadResultsUseCase>();
builder.Services.AddScoped<IGetEligibilityCheckStatusUseCase, GetEligibilityCheckStatusUseCase>();
builder.Services.AddScoped<IUpdateEligibilityCheckStatusUseCase, UpdateEligibilityCheckStatusUseCase>();
builder.Services.AddScoped<IProcessEligibilityCheckUseCase, ProcessEligibilityCheckUseCase>();
builder.Services.AddScoped<IGetEligibilityCheckItemUseCase, GetEligibilityCheckItemUseCase>();

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
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add Authorization
builder.Services.AddAuthorization(builder.Configuration);

builder.Services.AddHealthChecks();

builder.Services.AddSwaggerGen(c =>
{
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

var app = builder.Build();

app.MapHealthChecks("/healthcheck");

// ------------------------
// 2. Configure Middleware Pipeline
// ------------------------

// 2.1. Exception Handling
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

// 2.2. HTTPS Redirection
app.UseHttpsRedirection();

// 2.3. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 2.5. Swagger Middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "ECE Local Authority API - V1");
    c.SwaggerEndpoint("v1-admin/swagger.json", "ECE API - V1");
});

// 2.6. Map Controllers
app.MapControllers();

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program { }
