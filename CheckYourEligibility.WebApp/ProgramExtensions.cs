using CheckYourEligibility.Services;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CheckYourEligibility.WebApp
{
    public static class ProgramExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddDbContext<IEligibilityCheckContext, EligibilityCheckContext>(options =>
               options.UseSqlServer(configuration.GetConnectionString("EligibilityCheck") ?? throw new InvalidOperationException("Connection string 'EligibilityCheck' not found.")));
            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddTransient<IServiceTest, ServiceTest>();
            return services;
        }

    }
}
