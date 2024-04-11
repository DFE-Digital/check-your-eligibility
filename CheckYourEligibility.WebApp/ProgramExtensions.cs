using CheckYourEligibility.Services;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.IdentityModel.Tokens;

namespace CheckYourEligibility.WebApp
{
    public static class ProgramExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("EligibilityCheck");
            if (!Environment.GetEnvironmentVariable("KEY_VAULT_NAME").IsNullOrEmpty())
            {
                var keyVault = GetAzureKeyVault();

                connectionString = keyVault.GetSecret("ConnectionString").Value.Value;
            }
            
            services.AddDbContext<IEligibilityCheckContext, EligibilityCheckContext>(options =>
               options.UseSqlServer(
                   connectionString,
                   x=>x.MigrationsAssembly("CheckYourEligibility.Data.Migrations"))
               // **** note adding this back in will have undesired effects on updates 
               //.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
               );
            services.AddDatabaseDeveloperPageExceptionFilter();
          
            return services;
        }

        public static IServiceCollection AddAzureClients(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("AzureWebJobsStorage");
            if (!Environment.GetEnvironmentVariable("KEY_VAULT_NAME").IsNullOrEmpty())
            {
                var keyVault = GetAzureKeyVault();
                connectionString = keyVault.GetSecret("QueueConnectionString").Value.Value;
            }
            
            services.AddAzureClients(builder =>
            {
                builder.AddQueueServiceClient(connectionString);

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

        public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
        {           
            services.AddHttpClient<IDwpService, DwpService>(client =>
            {
                client.BaseAddress = new Uri(configuration["DWPBaseUrl"]);              
            });
            return services;
        }

        private static SecretClient GetAzureKeyVault()
        {
            var keyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME");
            var kvUri = $"https://{keyVaultName}.vault.azure.net";

            return new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
        }
    }
}
