using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CheckYourEligibility.SystemTests.Utilities.Models;
using CheckYourEligibility.SystemTests.Utilities;
using Microsoft.Playwright;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CheckYourEligibility.SystemTests.API
{
    public static class ApiHelper
    {
        public static Uri BaseUri => new Uri("http://ecs-dev-as.azurewebsites.net");

        public static async Task<HttpResponseMessage> PostRequest(string endpoint, object requestBody)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = BaseUri;

                var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                return await client.PostAsync(endpoint, jsonContent);
            }
        }

        public static async Task AssertStatusCode(HttpResponseMessage response, HttpStatusCode expectedStatusCode)
        {
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Request failed with status code {response.StatusCode}.");
                Console.WriteLine($"Response body: {await response.Content.ReadAsStringAsync()}");
            }

            Assert.That(response.StatusCode, Is.EqualTo(expectedStatusCode));
        }

        public static async Task<string?> ExecutePostRequestAndGetGuid(object requestBody)
        {
            var endpoint = "/FreeSchoolMeals";
            var response = await PostRequest(endpoint, requestBody);
            ProcessResponse(response);
            return await ExtractGuidFromResponse(response);
        }



        private static string ReadRequestBodyFromFile(string filePath)
        {
            return File.ReadAllText(filePath);
        }
        private static void ProcessResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Request failed with status code {response.StatusCode}.");
                Console.WriteLine($"Response body: {response.Content.ReadAsStringAsync().Result}");
                // Handle or log the error as needed
            }

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        }

        private static async Task<string> ExtractGuidFromResponse(HttpResponseMessage response)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var deserializedResponse = JsonConvert.DeserializeObject<FreeSchoolMealsResponseModel>(jsonResponse);

            Console.WriteLine($"Status: {deserializedResponse.Data.Status}");
            Console.WriteLine($"GetEligibilityCheck Link: {deserializedResponse.Links.Get_EligibilityCheck}");
         
            Assert.That(deserializedResponse?.Data?.Status, Is.EqualTo("queuedForProcessing"));

            return CommonMethods.ExtractStringAfterLastSlash(deserializedResponse?.Links?.Get_EligibilityCheck);
        }

        public static async Task<CheckEligibilityResponseModel?> PerformGetRequestAndStoreGUID(string guid)
        {
            var endpoint = $"/FreeSchoolMeals/{guid}";
            var response = await GetRequest(endpoint);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var deserializedResponse = JsonConvert.DeserializeObject<CheckEligibilityResponseModel>(jsonResponse);
            var checkEligibilityModel = deserializedResponse?.Data?.CheckEligibility;
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            return deserializedResponse;
        }

        public static async Task<HttpResponseMessage> GetRequest(string endpoint)
        {
            var uri = new Uri(BaseUri, endpoint);

            using (var client = new HttpClient())
            {
                return await client.GetAsync(uri);
            }
        }
    }
}