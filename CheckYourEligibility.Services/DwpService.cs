using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests.DWP;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Domain.Responses.DWP;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CheckYourEligibility.Services
{

    public interface IDwpService
    {
        Task<StatusCodeResult> GetCitizenClaims(string guid, string effectiveFromDate, string effectiveToDate);
        Task<string?> GetCitizen(CitizenMatchRequest requestBody);
        Task<SoapFsmCheckRespone?> EcsFsmCheck(CheckProcessData eligibilityCheck);
        public bool UseEcsforChecks { get; }
    }

    [ExcludeFromCodeCoverage]
    public class DwpService : BaseService , IDwpService
    {
        public const string decision_entitled = "decision_entitled";
        public const string statusInPayment = "in_payment";
        public const string awardStatusLive = "live";

        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly string _controllerUrl;
        private readonly IConfiguration _configuration;

        private string _DWP_ApiInstigatingUserId;
        private string _DWP_ApiPolicyId;
        private string _DWP_ApiCorrelationId;
        private string _DWP_ApiContext;
        private string _DWP_AccessLevel;
        private double _DWP_UniversalCreditThreshhold_1;
        private double _DWP_UniversalCreditThreshhold_2;
        private double _DWP_UniversalCreditThreshhold_3;
        private bool _UseEcsforChecks;
        private string _DWP_EcsHost;
        private string _DWP_EcsServiceVersion;
        private string _DWP_EcsLAId;
        private string _DWP_EcsSystemId;
        private string _DWP_EcsPassword;

        bool IDwpService.UseEcsforChecks => _UseEcsforChecks;

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
            double.TryParse(_configuration["Dwp:UniversalCreditThreshhold-1"], out _DWP_UniversalCreditThreshhold_1);
            double.TryParse(_configuration["Dwp:UniversalCreditThreshhold-2"], out _DWP_UniversalCreditThreshhold_2);
            double.TryParse(_configuration["Dwp:UniversalCreditThreshhold-3"], out _DWP_UniversalCreditThreshhold_3);

            bool.TryParse(_configuration["Dwp:UseEcsforChecks"], out _UseEcsforChecks);
            _DWP_EcsHost = _configuration["Dwp:EcsHost"];
            _DWP_EcsServiceVersion = _configuration["Dwp:EcsServiceVersion"];
            _DWP_EcsLAId = _configuration["Dwp:EcsLAId"];
            _DWP_EcsSystemId = _configuration["Dwp:EcsSystemId"];
            _DWP_EcsPassword = _configuration["Dwp:EcsPassword"];
        }

        

        #region ECS API Soap
        public async Task<SoapFsmCheckRespone?> EcsFsmCheck(CheckProcessData eligibilityCheck)
        {
            try
            {
                var uri = _DWP_EcsHost;
                if (!uri.Contains("https"))
                {
                    uri = $"https://{_DWP_EcsHost}/fsm.lawebservice/20170701/OnlineQueryService.svc";
                }

                var soapMessage = Properties.Resources.EcsSoapFsm;
                soapMessage = soapMessage.Replace("{{SystemId}}", _DWP_EcsSystemId);
                soapMessage = soapMessage.Replace("{{Password}}", _DWP_EcsPassword);
                soapMessage = soapMessage.Replace("{{LAId}}", _DWP_EcsLAId);
                soapMessage = soapMessage.Replace("{{ServiceVersion}}", _DWP_EcsServiceVersion);
                soapMessage = soapMessage.Replace("<ns:Surname>WEB</ns:Surname>", $"<ns:Surname>{eligibilityCheck.LastName}</ns:Surname>");
                soapMessage = soapMessage.Replace("<ns:DateOfBirth>1967-03-07</ns:DateOfBirth>", $"<ns:DateOfBirth>{eligibilityCheck.DateOfBirth}</ns:DateOfBirth>");
                soapMessage = soapMessage.Replace("<ns:NiNo>NN668767B</ns:NiNo>", $"<ns:NiNo>{eligibilityCheck.NationalInsuranceNumber}</ns:NiNo>");

                var content = new StringContent(soapMessage, Encoding.UTF8, "text/xml");
                var soapResponse = new SoapFsmCheckRespone();
                try
                {
                    _httpClient.DefaultRequestHeaders.Add("SOAPAction", "http://www.dcsf.gov.uk/20090308/OnlineQueryService/SubmitSingleQuery");

                    var response = await _httpClient.PostAsync(uri, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var doc = XDocument.Parse(response.Content.ReadAsStringAsync().Result);
                        var namespacePrefix = doc.Root.GetNamespaceOfPrefix("s");
                        var elements = doc.Descendants(namespacePrefix + "Body").First().Descendants().Elements();
                        XElement xElement = elements.First(x => x.Name.LocalName == "EligibilityStatus");
                        soapResponse.Status = xElement.Value;
                        xElement = elements.First(x => x.Name.LocalName == "ErrorCode");
                        soapResponse.ErrorCode = xElement.Value;
                        xElement = elements.FirstOrDefault(x=>x.Name.LocalName == "Qualifier");
                        soapResponse.Qualifier = xElement.Value;
                        return soapResponse;
                    }
                    else
                    {

                        _logger.LogError($"ECS check failed. uri:-{_httpClient.BaseAddress}{uri} Response:- {response.StatusCode} content:-{JsonConvert.SerializeObject(eligibilityCheck)}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"ECS check failed. uri:-{_httpClient.BaseAddress}{uri} content:-{JsonConvert.SerializeObject(eligibilityCheck)}");
                
                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, $"ECS check failed.  content:-{JsonConvert.SerializeObject(eligibilityCheck)}");
            }
            return null;
        }


        #endregion

        #region Citizen Api Rest
        public async Task<StatusCodeResult> GetCitizenClaims(string guid, string effectiveFromDate, string effectiveToDate )
        {
            var uri = $"{_controllerUrl}/v2/citizens/{guid}/claims?effectiveFromDate={effectiveFromDate}&effectiveToDate={effectiveToDate}";

            try
            {
                _httpClient.DefaultRequestHeaders.Add("instigating-user-id", _DWP_ApiInstigatingUserId);
                _httpClient.DefaultRequestHeaders.Add("access-level", _DWP_AccessLevel);
                _httpClient.DefaultRequestHeaders.Add("correlation-id", _DWP_ApiCorrelationId);
                _httpClient.DefaultRequestHeaders.Add("context", _DWP_ApiContext);

                var response = await _httpClient.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var claims = JsonConvert.DeserializeObject<DwpClaimsResponse>(jsonString);
                    if (CheckBenefitEntitlement(guid, claims))
                    {
                        return new OkResult();
                    }
                    else
                    {
                        return new NotFoundResult();
                    }
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

        public bool CheckBenefitEntitlement(string citizenId, DwpClaimsResponse claims)
        {
            if (CheckStandardBenefitType(citizenId,claims, DwpBenefitType.employment_support_allowance_income_based))
                return true;
            else if (CheckStandardBenefitType(citizenId, claims, DwpBenefitType.income_support))
                return true;
            else if (CheckStandardBenefitType(citizenId, claims, DwpBenefitType.job_seekers_allowance_income_based))
                return true;
            else if (CheckStandardBenefitType(citizenId, claims, DwpBenefitType.pensions_credit))
                return true;
            else if (CheckUniversalCreditBenefitType(citizenId, claims))
                return true;
            else
                return false;
        }

        private bool CheckUniversalCreditBenefitType(string citizenId, DwpClaimsResponse claims)
        {
            var benefit = claims.data.FirstOrDefault(x => x.attributes.benefitType == DwpBenefitType.universal_credit.ToString() 
            && x.attributes.status == statusInPayment);
            if (benefit != null)
            {
                var entitled = false;
                var threshHoldUsed = 0;
                var liveAwards = benefit.attributes.awards.Where(x=> x.status == awardStatusLive);
                if (liveAwards != null && liveAwards.Count()>0)
                {
                    var takeHomePay = 0.00;
                    threshHoldUsed = liveAwards.Count();
                    if (threshHoldUsed == 1)
                    {
                       takeHomePay = liveAwards.Sum(x=>x.assessmentAttributes.takeHomePay);
                        if (takeHomePay <= _DWP_UniversalCreditThreshhold_1) {
                            entitled = true;
                        }
                    }
                    else if (threshHoldUsed == 2)
                    {
                        takeHomePay = liveAwards.Sum(x => x.assessmentAttributes.takeHomePay);
                        if (takeHomePay <= _DWP_UniversalCreditThreshhold_2)
                        {
                            entitled = true;
                        }
                    }
                    else if (threshHoldUsed == 3)
                    {
                        takeHomePay = liveAwards.Sum(x => x.assessmentAttributes.takeHomePay);
                        if (takeHomePay <= _DWP_UniversalCreditThreshhold_3)
                        {
                            entitled = true;
                        }
                    }
                    else
                    {
                        throw new Exception($"DWP CheckUniversal credit has {liveAwards.Count()} when there should only be 3.");
                    }


                    if (entitled)
                    {
                        _logger.LogInformation($"Dwp {DwpBenefitType.universal_credit} found for CitizenId:-{citizenId}");
                        TrackMetric($"Dwp {DwpBenefitType.universal_credit} entitled", 1);
                        return true;
                    }
                }                  
            }
            return false;
        }

        private bool CheckStandardBenefitType(string citizenId, DwpClaimsResponse claims, DwpBenefitType benefitType)
        {
            var benefit = claims.data.FirstOrDefault(x => x.attributes.benefitType == benefitType.ToString()
            && x.attributes.status == decision_entitled);
            if (benefit != null)
            {
                _logger.LogInformation($"Dwp {benefitType} found for CitizenId:-{citizenId}");
                TrackMetric($"Dwp {benefitType} entitled", 1);
                return true;
            }
            return false;
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
                    var responseData = JsonConvert.DeserializeObject<DwpMatchResponse>(response.Content.ReadAsStringAsync().Result);
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
        #endregion

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
