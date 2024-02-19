using CheckYourEligibility.Services;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CheckYourEligibility.WebApp
{
    public static class ProgramExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddDbContext<IEligibilityCheckContext, EligibilityCheckContext>(options =>
               options.UseSqlServer(
                   configuration.GetConnectionString("EligibilityCheck") ?? throw new InvalidOperationException("Connection string 'EligibilityCheck' not found."),
                   x=>x.MigrationsAssembly("CheckYourEligibility.Data.Migrations")));
            services.AddDatabaseDeveloperPageExceptionFilter();
                        
            services.AddTransient<IFsmCheckEligibility, FsmCheckEligibility>();
            return services;
        }

    }
}
