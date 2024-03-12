using CheckYourEligibility.Services;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;

namespace CheckYourEligibility.WebApp
{
    public static class ProgramExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddDbContext<IEligibilityCheckContext, EligibilityCheckContext>(options =>
               options.UseSqlServer(
                   configuration.GetConnectionString("EligibilityCheck") ?? throw new InvalidOperationException("Connection string 'EligibilityCheck' not found."),
                   x=>x.MigrationsAssembly("CheckYourEligibility.Data.Migrations"))
               .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
               );
            services.AddDatabaseDeveloperPageExceptionFilter();
          
            return services;
        }

        public static IServiceCollection AddAzureClients(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAzureClients(builder =>
            {
                builder.AddQueueServiceClient(configuration.GetValue<string>("AzureWebJobsStorage"));

            });
            return services;
        }

        public static IServiceCollection AddServices(this IServiceCollection services)
        {

            services.AddTransient<IFsmCheckEligibility, FsmCheckEligibilityService>();
            services.AddTransient<IAdministration, AdministrationService>();
            return services;
        }

    }
}
