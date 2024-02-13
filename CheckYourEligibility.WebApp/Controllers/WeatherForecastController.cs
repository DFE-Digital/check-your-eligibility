using CheckYourEligibility.Domain;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CheckYourEligibility.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IServiceTest _serviceTest;




        public WeatherForecastController(ILogger<WeatherForecastController> logger, IServiceTest serviceTest)
        {
            _logger = logger;
            _serviceTest = serviceTest;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            _logger.LogInformation("Test log");
            _logger.LogWarning("Test log wrning");
          var res =  await  _serviceTest.OnGetAsync();

            _logger.LogInformation(MyLogEvents.GetItem, "Getting item {Id}", 12345);
            _logger.LogInformation(MyLogEvents.InsertItem, "InsertItem item {Id}", 12345);
            _logger.LogError("test log error");
            

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
