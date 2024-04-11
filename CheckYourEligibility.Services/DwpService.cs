using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Requests.DWP;
using CheckYourEligibility.Domain.Responses.DWP;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Text;

namespace CheckYourEligibility.Services
{

    public interface IDwpService
    {
        Task<StatusCodeResult> CheckForBenefit(string guid);
        Task<string?> GetCitizen(CitizenMatchRequest requestBody);
    }

    public class DwpService : IDwpService
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly string _moqControllerUrl;

        public DwpService(ILoggerFactory logger,HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger.CreateLogger("ServiceFsmCheckEligibility");
            _httpClient = httpClient;
            _moqControllerUrl = configuration["DWPMoqControllerUrl"];
        }

        public async Task<StatusCodeResult> CheckForBenefit(string guid)
        {
            try
            {
                var uri = $"{_moqControllerUrl}/v2/citizens/{guid}/claims?benefitType={MogDWPValues.validUniversalBenefitType}";

                _httpClient.DefaultRequestHeaders.Add("instigating-user-id", "abcdef1234577890abcdeffghi");
                _httpClient.DefaultRequestHeaders.Add("access-level", "1");
                _httpClient.DefaultRequestHeaders.Add("correlation-id", "4c6a63f1-1924-4911-b45c-95dbad8b6c37");
                _httpClient.DefaultRequestHeaders.Add("context", "abc-1-ab-x128881");

                var response = await _httpClient.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    return new OkResult();
                }
                return new BadRequestResult();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<string?> GetCitizen(CitizenMatchRequest requestBody)
        {
            try
            {
                var uri = $"{_moqControllerUrl}/v2/citizens";
                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                content.Headers.Add("instigating-user-id", "abcdef1234577890abcdeffghi");
                content.Headers.Add("policy-id", "fsm");
                content.Headers.Add("correlation-id", "4c6a63f1-1924-4911-b45c-95dbad8b6c37");
                content.Headers.Add("context", "abc-1-ab-x128881");

                var response = await _httpClient.PostAsync(uri, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonConvert.DeserializeObject<DwpResponse>(response.Content.ReadAsStringAsync().Result);
                    return responseData.Data.Id;
                }
                else
                {
                    _logger.LogInformation($"Get Citizen failed. uri:-{_httpClient.BaseAddress}{uri} Response:- {response.StatusCode} content:-{JsonConvert.SerializeObject(requestBody)}");
                }
                return null;
            }
            catch (Exception)
            {
                throw;
            }
            
        }

    }

}
