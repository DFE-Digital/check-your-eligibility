using CheckYourEligibility.Data;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.WebApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddControllers()
    .AddNewtonsoftJson()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAzureClients(builder.Configuration);
builder.Services.AddServices();

builder.Services.AddAutoMapper(typeof(EligibilityMappingProfile));

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

app.UseAuthorization();

app.MapControllers();

app.Run();
