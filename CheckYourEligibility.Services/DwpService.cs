using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Responses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CheckYourEligibility.Services
{

    public interface IDwpService
    {
        Task<StatusResponse?> GetStatus(string guid);
    }

    public class DwpService : IDwpService
    {
        private readonly HttpClient _httpClient;
        private readonly string _remoteServiceBaseUrl;

        public DwpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<StatusResponse?> GetStatus(string guid)
        {
            try
            {
                var uri = $"FreeSchoolMeals/{guid}/status";

                var response = await _httpClient.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<StatusResponse>(response.Content.ReadAsStringAsync().Result);
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
