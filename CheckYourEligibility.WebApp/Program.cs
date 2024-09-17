using Azure.Identity;
using CheckYourEligibility.Data;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.WebApp;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.OpenApi.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddControllers()
    .AddNewtonsoftJson()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

if (Environment.GetEnvironmentVariable("KEY_VAULT_NAME")!=null)
{
    var keyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME");
    var kvUri = $"https://{keyVaultName}.vault.azure.net";

    builder.Configuration.AddAzureKeyVault(new Uri(kvUri), new DefaultAzureCredential());
}

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAzureClients(builder.Configuration);
builder.Services.AddServices();
builder.Services.AddExternalServices(builder.Configuration);
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = int.MaxValue; // if don't set default value is: 30 MB
});

builder.Services.AddAutoMapper(typeof(FsmMappingProfile));
builder.Services.AddAuthorization(builder.Configuration);

var app = builder.Build();


// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
    
//}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<EligibilityCheckContext>();
    if (app.Environment.IsDevelopment())
    {
        //context.Database.EnsureCreated();
        context.Database.Migrate(); //Runs all migrations that have not been processed. ensure there is a BaseMigration
        DbInitializer.Initialize(context);
    }
}

app.UseHttpsRedirection();
app.Use(async (httpContext, next) =>
{
    try
    {
        httpContext.Request.EnableBuffering();
        string requestBody = await new StreamReader(httpContext.Request.Body, Encoding.UTF8).ReadToEndAsync();
        httpContext.Request.Body.Position = 0;
        app.Logger.LogInformation($"Request body: {requestBody}");
    }
    catch (Exception ex)
    {
        app.Logger.LogInformation($"Exception reading request: {ex.Message}");
    }

    Stream originalBody = httpContext.Response.Body;
    try
    {
        using var memStream = new MemoryStream();
        httpContext.Response.Body = memStream;

        // call to the following middleware 
        // response should be produced by one of the following middlewares
        await next(httpContext);

        memStream.Position = 0;
        string responseBody = new StreamReader(memStream).ReadToEnd();

        memStream.Position = 0;
        await memStream.CopyToAsync(originalBody);
        app.Logger.LogInformation($"Response body: {responseBody}");
    }
    finally
    {
        httpContext.Response.Body = originalBody;
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program { }
