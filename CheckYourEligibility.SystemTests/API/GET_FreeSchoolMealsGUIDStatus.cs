using System.Net;
namespace CheckYourEligibility.SystemTests.API
{
    public class GET_FreeSchoolMealsGUIDStatus : PlaywrightTest
    {
        private dynamic _requestBody;

        [Test]
        [Ignore("Targeting live environment")]
        public async Task GetRequestWithGUIDToCheckStatus()
        {
            _requestBody = new
            {
                data = new
                {
                    nationalInsuranceNumber = "SB123456C",
                    lastName = "Test",
                    dateOfBirth = "01/12/1990",
                    nationalAsylumSeekerServiceNumber = ""
                }
            };

            // Step 1: Execute a POST request to create a GUID
            var createdGuid = await ApiHelper.ExecutePostRequestAndGetGuid(_requestBody);

            // Log the created GUID 
            Console.WriteLine($"Created GUID: {createdGuid}");

            // Step 2: Execute a GET request using the created GUID
            var jsonResponse = await ApiHelper.PerformGetRequestAndStoreGUID(createdGuid);

            // Access the CheckEligibilityModel from the deserialized response
            var checkEligibilityModel = jsonResponse?.Data?.CheckEligibility;

            // Assertions to check if the response contains the same data as the request body
            Assert.That(checkEligibilityModel.nationalInsuranceNumber, Is.EqualTo(_requestBody.data.nationalInsuranceNumber));
            Assert.That(checkEligibilityModel.lastName, Is.EqualTo(_requestBody.data.lastName));
            Assert.That(checkEligibilityModel.dateOfBirth, Is.EqualTo(_requestBody.data.dateOfBirth));
            Assert.That(checkEligibilityModel.nationalAsylumSeekerServiceNumber, Is.EqualTo(_requestBody.data.nationalAsylumSeekerServiceNumber));
        }

        [Test]
        [Ignore("Targeting live environment")]
        public async Task GetRequest_WithInvalidGUID()
        {
            var jsonResponse = await ApiHelper.GetRequest("/FreeSchoolMeals/7df4a175-c153-43ac-87e9-4a48e4517");
            Assert.That(jsonResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
     
    }
}