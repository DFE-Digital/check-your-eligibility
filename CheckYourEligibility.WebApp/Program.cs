using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.WebApp;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
//builder.Logging.AddEventLog(eventLogSettings =>
//{
//    eventLogSettings.SourceName = "CheckYourEligibility";
//});

// Add services to the container.
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAutoMapper(typeof(EligibilityMappingProfile));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<EligibilityCheckContext>();
    //context.Database.EnsureCreated();
    //DbInitializer.Initialize(context);
    context.Database.Migrate(); //Runs all migrations that have not been processed. ensure there is a BaseMigration
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
