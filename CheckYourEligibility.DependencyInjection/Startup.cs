using CheckYourEligibility.WebApp.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CheckYourEligibility.DependencyInjection
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {

            services
                .AddControllers()
                .AddNewtonsoftJson()
                .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
                .AddApplicationPart(typeof(WeatherForecastController).GetTypeInfo().Assembly);

            services.AddSwaggerExtensions(new SwaggerConfig("FeatureManagement", "v1"), addApplicationContextParameters: true);
            services.AddHealthChecks();
            services.AddHttpContextAccessor();
            services.AddFluentValidationAutoValidation();
        }
    }
}
