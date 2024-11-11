using CheckYourEligibility.Services;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.WebApp
{
    [ExcludeFromCodeCoverage(Justification = "extension of program")]
    public static class ProgramExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("ConnectionString");

            services.AddDbContext<IEligibilityCheckContext, EligibilityCheckContext>(options =>
               options.UseSqlServer(
                   connectionString,
                   x => x.MigrationsAssembly("CheckYourEligibility.Data.Migrations"))
               // **** note adding this back in will have undesired effects on updates 
               //.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
               );
            services.AddDatabaseDeveloperPageExceptionFilter();

            return services;
        }

        public static IServiceCollection AddAzureClients(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("QueueConnectionString");
            services.AddAzureClients(builder =>
            {
                builder.AddQueueServiceClient(connectionString);

            });
            return services;
        }

        public static IServiceCollection AddServices(this IServiceCollection services)
        {

            services.AddTransient<ICheckEligibility, CheckEligibilityService>();
            services.AddTransient<IApplication, ApplicationService>();
            services.AddTransient<IAdministration, AdministrationService>();
            services.AddTransient<IEstablishmentSearch, EstablishmentSearchService>();
            services.AddTransient<IUsers, UsersService>();
            services.AddTransient<IAudit, AuditService>();
            services.AddTransient<IHash, HashService>();
            return services;
        }

        public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<IDwpService, DwpService>(client =>
            {
                client.BaseAddress = new Uri(configuration["Dwp:BaseUrl"]);
            });
            return services;
        }

        public static IServiceCollection AddAuthorization(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = configuration["Jwt:Issuer"],
                            ValidAudience = configuration["Jwt:Issuer"],
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
                        };
                    });
            return services;
        }
    }
}
