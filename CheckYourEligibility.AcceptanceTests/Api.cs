using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using CheckYourEligibility.AcceptanceTests.Models;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Responses;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;

namespace CheckYourEligibility.AcceptanceTests
{
    [ExcludeFromCodeCoverage]
    public class Api
    {
        private readonly string _serviceUrl;
        private readonly HttpClient _httpClient;
        private readonly string _userName;
        private readonly string _password;
        private readonly string _dbConnection;

        public readonly bool RunLocal;
        public readonly string StandardQueue;
        public readonly EligibilityCheckContext Db;

        public Api()
        {

            if (Environment.GetEnvironmentVariable("KEY_VAULT_NAME") != null)
            {
                var keyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME");
                var kvUri = $"https://{keyVaultName}.vault.azure.net";
                var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());

                _dbConnection = client.GetSecret("ConnectionString").Value.Value;
                _userName = client.GetSecret("Cypress").Value.Value; 
                _password = client.GetSecret("Jwt--Users--Cypress").Value.Value;
                RunLocal = false;
                StandardQueue = "process-eligibility-queue";
                _serviceUrl = client.GetSecret("serviceUrl").Value.Value;
            }
            else
            {
                _dbConnection = TestContext.Parameters["dbConnection"];
                _userName = TestContext.Parameters["apiUserName"];
                _password = TestContext.Parameters["apiPassword"];
                bool.TryParse(TestContext.Parameters["runLocal"], out RunLocal);
                if (!RunLocal)
                {
                    StandardQueue = TestContext.Parameters["standardQueue"];
                }
                _serviceUrl = TestContext.Parameters["serviceUrl"];
            }
            


            TestContext.Out.WriteLine($"Service url: {_serviceUrl}");
            _httpClient = new HttpClient() { BaseAddress = new Uri(_serviceUrl) }; 

            Db = new EligibilityCheckContext();
            Db.ConnectionString = _dbConnection;
            
        }

        public async Task Login()
        {
            var data = new SystemUser
            {
                Username = _userName,
                Password =_password,
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

        public async Task<T1> ApiDataPutAsynch<T1>(string address, T1 result)
        {
            string uri = address;
            LogRequest(uri, HttpMethod.Put,"");

            var task = await _httpClient.PutAsync(uri, null);
            if (task.IsSuccessStatusCode)
            {
                var jsonString = await task.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<T1>(jsonString);
            }
            else
            {
                if (task.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException();
                }
                if (task.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    var jsonString = await task.Content.ReadAsStringAsync();
                   return JsonConvert.DeserializeObject<T1>(jsonString);
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
            string    json = JsonConvert.SerializeObject(data);
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
                if (task.StatusCode == HttpStatusCode.BadRequest)
                {
                    var r = new BadRequestObjectResult(new MessageResponse { Data = responseMessage });
                    return (T2)(object)r;
                }
                throw new Exception($"Api error {task.StatusCode}, {uri},{responseMessage}");
            }

            return result;
        }

        private void LogRequest<t>(string query, HttpMethod method, t? data)
        {
            TestContext.Out.WriteLine($"Method: {method}");
            TestContext.Out.WriteLine($"RequestUri: {query}");
            if (data != null)
            {
                TestContext.Out.WriteLine($"Data: {JsonConvert.SerializeObject(data)}");
            }
        }
    }
}
