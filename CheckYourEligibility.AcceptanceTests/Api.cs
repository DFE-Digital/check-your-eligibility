using CheckYourEligibility.AcceptanceTests.Models;
using CheckYourEligibility.Domain;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;

namespace CheckYourEligibility.AcceptanceTests
{

    public class Api
    {
        private readonly string _serviceUrl;
        private readonly HttpClient _httpClient;

        public readonly EligibilityCheckContext Db;

        public Api()
        {
            _serviceUrl = TestContext.Parameters["serviceUrl"];
            TestContext.Out.WriteLine($"Service url: {_serviceUrl}");
            _httpClient = new HttpClient() { BaseAddress = new Uri(_serviceUrl) }; 
            Db = new EligibilityCheckContext();
        }

        public async Task Login()
        {
            var data = new SystemUser
            {
                Username = TestContext.Parameters["apiUserName"],
                Password = TestContext.Parameters["apiPassword"]
            };

            var response = await ApiDataPostAsynch("/api/Login", data, new JwtAuthResponse());

            _httpClient.DefaultRequestHeaders
            .Add("Authorization", "Bearer " + response.Token);
        }

        public async Task<T> ApiDataDeleteAsynch<T>(string address, T result)
        {

            string uri = address;
            var task = await _httpClient.DeleteAsync(uri);
            LogRequest(uri, HttpMethod.Delete, -1);

            if (task.IsSuccessStatusCode)
            {
                var jsonString = await task.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<T>(jsonString);
            }
            else
            {
                if (task.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException();
                }
                string responseMessage = "";
                if (task.Content != null)
                {
                    responseMessage = await task.Content.ReadAsStringAsync();
                }

                throw new Exception($"Api error {task.StatusCode}, {uri},{responseMessage}");
            }
            return result;
        }

        public async Task<T> ApiDataGetAsynch<T>(string address, T result)
        {
            string uri = address;
            LogRequest(uri, HttpMethod.Get,1);

            var task = await _httpClient.GetAsync(uri);

            if (task.IsSuccessStatusCode)
            {
                var jsonString = await task.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<T>(jsonString);
            }
            else
            {
                if (task.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException();
                }
                string responseMessage = "";
                if (task.Content != null)
                {
                    responseMessage = await task.Content.ReadAsStringAsync();
                }

                throw new Exception($"Api error {task.StatusCode}, {uri},{responseMessage}");


            }

            return result;
        }

        public async Task<T2> ApiDataPutAsynch<T1, T2>(string address, T1 data, T2 result)
        {
            string uri = address;
            string json = JsonConvert.SerializeObject(data);
            HttpContent content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            LogRequest(uri, HttpMethod.Put, data);

            var task = await _httpClient.PutAsync(uri, content);
            if (task.IsSuccessStatusCode)
            {
                var jsonString = await task.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<T2>(jsonString);
            }
            else
            {
                if (task.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException();
                }
                string responseMessage = "";
                if (task.Content != null)
                {
                    responseMessage = await task.Content.ReadAsStringAsync();
                }

                throw new Exception($"Api error {task.StatusCode}, {uri},{responseMessage}");
            }

            return result;
        }

        public async Task<T2> ApiDataPatchAsynch<T1, T2>(string address, T1 data, T2 result)
        {
            string uri = address;
            string json = JsonConvert.SerializeObject(data);
            HttpContent content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            LogRequest(uri, HttpMethod.Patch, data);

            var task = await _httpClient.PatchAsync(uri, content);
            if (task.IsSuccessStatusCode)
            {
                var jsonString = await task.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<T2>(jsonString);
            }
            else
            {
                if (task.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException();
                }
                string responseMessage = "";
                if (task.Content != null)
                {
                    responseMessage = await task.Content.ReadAsStringAsync();
                }

                throw new Exception($"Api error {task.StatusCode}, {uri},{responseMessage}");
            }

            return result;
        }



        public async Task<T2> ApiDataPostAsynch<T1, T2>(string address, T1 data, T2 result)
        {
            string uri = address;
            string json = JsonConvert.SerializeObject(data);
            HttpContent content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            LogRequest(uri, HttpMethod.Post, data);

            var task = await _httpClient.PostAsync(uri, content);
            if (task.IsSuccessStatusCode)
            {
                var jsonString = await task.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<T2>(jsonString);
            }
            else
            {
                if (task.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException();
                }
                string responseMessage = "";
                if (task.Content != null)
                {
                    responseMessage = await task.Content.ReadAsStringAsync();
                }
                   
                throw new Exception($"Api error {task.StatusCode}, {uri},{responseMessage}");
            }

            return result;
        }

        private void LogRequest<t>(string query, HttpMethod method, t data)
        {
            TestContext.Out.WriteLine($"Method: {method}");
            TestContext.Out.WriteLine($"RequestUri: {query}");
            TestContext.Out.WriteLine($"Data: {data.ToString}");
        }
    }
}
