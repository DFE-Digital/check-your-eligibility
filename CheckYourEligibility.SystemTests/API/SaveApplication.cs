using CheckYourEligibility.SystemTests.Utilities.Models.Responses;
using Newtonsoft.Json;
using System;
using System.Net;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.SystemTests.Utilities;
using CheckYourEligibility.Domain.Responses;
using NUnit.Framework.Internal;

namespace CheckYourEligibility.SystemTests.API
{

    public class SaveApplication

    {

        [Test]
        public async Task PostRequest_SaveApplication()
        {
            var endpoint = "/FreeSchoolMeals/Application";
            ApplicationRequest myApplicationRequest = CommonMethods.CreateAndProcessApplicationRequest();
            var response = await ApiHelper.PostRequest(endpoint, myApplicationRequest);
            await ApiHelper.AssertStatusCode(response, HttpStatusCode.Created);
        }


        [Test]
        public async Task PostRequest_CheckApplicationResponse()
        {
            var endpoint = "/FreeSchoolMeals/Application";
            ApplicationRequest myApplicationRequest = CommonMethods.CreateAndProcessApplicationRequest();

            var response = await ApiHelper.PostRequest(endpoint, myApplicationRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            var deserializedResponse = JsonConvert.DeserializeObject<ApplicationFreeSchoolMeals>(responseContent);
            var result = deserializedResponse.data;

            Assert.That(result.school, Is.EqualTo(139856));
            Assert.That(result.childFirstName, Is.EqualTo("Jane"));
            Assert.That(result.childLastName, Is.EqualTo("Smith"));
            Assert.That(result.parentNationalInsuranceNumber, Is.EqualTo("AB123456C"));
            Assert.That(result.parentNationalAsylumSeekerServiceNumber, Is.Null);
            Assert.That(result.parentDateOfBirth, Is.EqualTo("01/01/1980"));
            Assert.That(result.parentFirstName, Is.EqualTo("John"));
            Assert.That(result.parentLastName, Is.EqualTo("Smith"));
        }
    }
}
