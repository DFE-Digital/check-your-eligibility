using CheckYourEligibility.Services;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace CheckYourEligibility.WebApp
{
    public static class ProgramExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
        {
            var connectionString = configuration.GetConnectionString("EligibilityCheck");
            if (!isDevelopment)
            {
                var keyVault = GetAzureKeyVault();
                connectionString = keyVault.GetSecret("ConnectionString").Value.ToString();
            }

            services.AddDbContext<IEligibilityCheckContext, EligibilityCheckContext>(options =>
               options.UseSqlServer(
                   connectionString,
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
            services.AddTransient<ISchoolsSearch, SchoolSearchService>();
            return services;
        }

        private static Azure.Security.KeyVault.Secrets.SecretClient GetAzureKeyVault()
        {
            var keyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME");
            var kvUri = $"https://{keyVaultName}.vault.azure.net";

            return new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
        }
    }
}
