using System.Net;
using Microsoft.Playwright;

namespace CheckYourEligibility.SystemTests.API
{

    public class POST_FreeSchoolMeals : PlaywrightTest
    {
        private IAPIRequestContext Request;
        public static T AsNullable<T>(T value) where T : class => value;

        [SetUp]
        public async Task SetUpAPITesting()
        {
            Request = await Playwright.APIRequest.NewContextAsync(new()
            {
                BaseURL = ApiHelper.BaseUri.ToString()
            });
        }

        [Test, Category("POST Request")]
        [Description("POST Request with valid NI")]
        public async Task PostRequestWithValidNI()
        {
            var endpoint = "/FreeSchoolMeals";
            var requestBody = new
            {
                data = new
                {
                    nationalInsuranceNumber = "SB123456C",
                    lastName = "Test",
                    dateOfBirth = "01/12/1990",
                    nationalAsylumSeekerServiceNumber = ""
                }
            };

            var response = await ApiHelper.PostRequest(endpoint, requestBody);
            await ApiHelper.AssertStatusCode(response, HttpStatusCode.Accepted);
        }

        [Test]
        public async Task PostRequestWithValidNASS()
        {
            var endpoint = "/FreeSchoolMeals";
            var requestBody = new
            {
                data = new
                {
                    nationalInsuranceNumber = "",
                    lastName = "Joe",
                    dateOfBirth = "01/12/1990",
                    nationalAsylumSeekerServiceNumber = "313"
                }
            };

            var response = await ApiHelper.PostRequest(endpoint, requestBody);
            await ApiHelper.AssertStatusCode(response, HttpStatusCode.Accepted);
        }

        [Test, Description("POST Request with invalid NI")]
        public async Task PostRequestWithInvalidNI()
        {
            var endpoint = "/FreeSchoolMeals";
            var requestBody = new
            {
                data = new
                {
                    nationalInsuranceNumber = "AB1234561",
                    lastName = "Joe",
                    dateOfBirth = "01/12/1990",
                    nationalAsylumSeekerServiceNumber = "null"
                }
            };

            var response = await ApiHelper.PostRequest(endpoint, requestBody);
            await ApiHelper.AssertStatusCode(response, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task PostRequestWithInvalidDOB()
        {
            var endpoint = "/FreeSchoolMeals";
            var requestBody = new
            {
                data = new
                {
                    nationalInsuranceNumber = "",
                    lastName = "Joe",
                    dateOfBirth = "01/13/1990",
                    nationalAsylumSeekerServiceNumber = "313"
                }
            };

            var response = await ApiHelper.PostRequest(endpoint, requestBody);
            await ApiHelper.AssertStatusCode(response, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task PostRequestWithNoNIAndNoNASS()
        {
            var endpoint = "/FreeSchoolMeals";
            var requestBody = new
            {
                data = new
                {
                    nationalInsuranceNumber = "",
                    lastName = "Joe",
                    dateOfBirth = "01/13/1990",
                    nationalAsylumSeekerServiceNumber = ""
                }
            };

            var response = await ApiHelper.PostRequest(endpoint, requestBody);
            await ApiHelper.AssertStatusCode(response, HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task PostRequestWithEmptyDOB()
        {
            var endpoint = "/FreeSchoolMeals";
            var requestBody = new
            {
                data = new
                {
                    nationalInsuranceNumber = "AB123456C",
                    lastName = "Joe",
                    dateOfBirth = "",
                    nationalAsylumSeekerServiceNumber = ""
                }
            };

            var response = await ApiHelper.PostRequest(endpoint, requestBody);
            await ApiHelper.AssertStatusCode(response, HttpStatusCode.BadRequest);
        }

        [Test, Description("POST Request with null NI")]
        public async Task PostRequestWithNullNI()
        {

            var endpoint = "/FreeSchoolMeals";
            var requestBody = new
            {
                data = new
                {
                    nationalInsuranceNumber = AsNullable<string>(null),
                    lastName = "Joe",
                    dateOfBirth = "01/12/1990",
                    nationalAsylumSeekerServiceNumber = ""
                }
            };

            var response = await ApiHelper.PostRequest(endpoint, requestBody);
            await ApiHelper.AssertStatusCode(response, HttpStatusCode.BadRequest);
        }

        [Test, Description("POST Request with Null NASS number")]
        public async Task PostRequestWithNullNASS()
        {
            var endpoint = "/FreeSchoolMeals";
            var requestBody = new
            {
                data = new
                {
                    nationalInsuranceNumber = "",
                    lastName = "Joe",
                    dateOfBirth = "01/12/1990",
                    nationalAsylumSeekerServiceNumber = AsNullable<string>(null)
                }
            };

            var response = await ApiHelper.PostRequest(endpoint, requestBody);
            await ApiHelper.AssertStatusCode(response, HttpStatusCode.BadRequest);
        }

        [TearDown]
        public async Task TearDownAPITesting()
        {
            await Request.DisposeAsync();
        }
    }
}
