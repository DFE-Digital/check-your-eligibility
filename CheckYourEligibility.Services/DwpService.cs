using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests.DWP;
using CheckYourEligibility.Domain.Responses.DWP;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

namespace CheckYourEligibility.Services
{

    public interface IDwpService
    {
        Task<StatusCodeResult> CheckForBenefit(string guid);
        Task<string?> GetCitizen(CitizenMatchRequest requestBody);
    }

    [ExcludeFromCodeCoverage]
    public class DwpService : BaseService , IDwpService
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly string _controllerUrl;
        private readonly IConfiguration _configuration;

        private string _DWP_ApiInstigatingUserId;
        private string _DWP_ApiPolicyId;
        private string _DWP_ApiCorrelationId;
        private string _DWP_ApiContext;
        private string _DWP_AccessLevel;

        public DwpService(ILoggerFactory logger,HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger.CreateLogger("ServiceFsmCheckEligibility");
            _httpClient = httpClient;
            _configuration = configuration;
            _controllerUrl = _configuration["Dwp:ApiControllerUrl"];
            _DWP_ApiInstigatingUserId = _configuration["Dwp:ApiInstigatingUserId"];
            _DWP_ApiPolicyId = _configuration["Dwp:ApiPolicyId"];
            _DWP_ApiCorrelationId = _configuration["Dwp:ApiCorrelationId"];
            _DWP_ApiContext = _configuration["Dwp:ApiContext"];
            _DWP_AccessLevel = _configuration["Dwp:AccessLevel"];
        }

        public async Task<StatusCodeResult> CheckForBenefit(string guid)
        {
            var uri = $"{_controllerUrl}/v2/citizens/{guid}/claims?benefitType={MogDWPValues.validUniversalBenefitType}";

            try
            {
                _httpClient.DefaultRequestHeaders.Add("instigating-user-id", _DWP_ApiInstigatingUserId);
                _httpClient.DefaultRequestHeaders.Add("access-level", _DWP_AccessLevel);
                _httpClient.DefaultRequestHeaders.Add("correlation-id", _DWP_ApiCorrelationId);
                _httpClient.DefaultRequestHeaders.Add("context", _DWP_ApiContext);

                var response = await _httpClient.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    return new OkResult();
                }
                else
                {

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return new NotFoundResult();
                    }
                    else
                    {
                        _logger.LogInformation($"Get CheckForBenefit failed. uri:-{_httpClient.BaseAddress}{uri} Response:- {response.StatusCode}");
                        return new InternalServerErrorResult();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CheckForBenefit failed. uri:-{_httpClient.BaseAddress}{uri}");
                return new InternalServerErrorResult();
            }
        }

        public async Task<string?> GetCitizen(CitizenMatchRequest requestBody)
        {
            var uri = $"{_controllerUrl}/v2/citizens";
            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            try
            {
                content.Headers.Add("instigating-user-id", _DWP_ApiInstigatingUserId);
                content.Headers.Add("policy-id", _DWP_ApiPolicyId);
                content.Headers.Add("correlation-id", _DWP_ApiCorrelationId);
                content.Headers.Add("context", _DWP_ApiContext);

                var response = await _httpClient.PostAsync(uri, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonConvert.DeserializeObject<DwpResponse>(response.Content.ReadAsStringAsync().Result);
                    return responseData.Data.Id;
                }
                else
                {
                   
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return CheckEligibilityStatus.parentNotFound.ToString();
                    }
                    else if(response.StatusCode == HttpStatusCode.UnprocessableEntity)
                    {
                        _logger.LogInformation($"DWP Duplicate matches found for {JsonConvert.SerializeObject(requestBody)}");
                        TrackMetric($"DWP Duplicate Matches Found", 1);
                        return CheckEligibilityStatus.DwpError.ToString();
                    }
                    else
                    {
                        _logger.LogInformation($"Get Citizen failed. uri:-{_httpClient.BaseAddress}{uri} Response:- {response.StatusCode} content:-{JsonConvert.SerializeObject(requestBody)}");
                        return CheckEligibilityStatus.DwpError.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,$"Get Citizen failed. uri:-{_httpClient.BaseAddress}{uri} content:-{JsonConvert.SerializeObject(requestBody)}");
                return CheckEligibilityStatus.DwpError.ToString();
            }
            
        }

    }

    [ExcludeFromCodeCoverage]
    public class InternalServerErrorResult : StatusCodeResult
    {
        private const int DefaultStatusCode = StatusCodes.Status500InternalServerError;

        /// <summary>
        /// Creates a new <see cref="BadRequestResult"/> instance.
        /// </summary>
        public InternalServerErrorResult()
            : base(DefaultStatusCode)
        {
        }
    }
}
