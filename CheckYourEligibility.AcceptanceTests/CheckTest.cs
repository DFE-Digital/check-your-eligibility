using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CheckYourEligibility.AcceptanceTests
{
    public class CheckTest
    {
        private ApiEndPoint _endpoint;
        static Random rd = new Random();

        [SetUp]
        public async Task Setup()
        {

            _endpoint = new ApiEndPoint();
            await _endpoint.Login();
        }
        
        internal static string CreateString(int stringLength)
        {
            const string allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
            char[] chars = new char[stringLength];

            for (int i = 0; i < stringLength; i++)
            {
                chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
            }

            return new string(chars);
        }


        [Test]
        [TestCase(CheckEligibilityStatus.eligible, "NN668767B")]
        [TestCase(CheckEligibilityStatus.notEligible, "PN668767B")]
        [TestCase(CheckEligibilityStatus.parentNotFound, "PE668767B")]
        [TestCase(CheckEligibilityStatus.DwpError, "RH668767A")]
        public async Task CheckEligibility_fsm_status_should_be_expectedStatus(CheckEligibilityStatus expectedStatus, string magicNi)
        {
            
            //arrange
            var data = new CheckEligibilityRequest_Fsm
            {
                Data = new CheckEligibilityRequestData_Fsm
                {
                    LastName = CreateString(30),
                    DateOfBirth = "1990-12-15",
                    NationalInsuranceNumber = magicNi,
                }
            };

            //act
            var responseCheck = await _endpoint.ApiDataPostAsynch("/EligibilityCheck/FreeSchoolMeals", data, new CheckEligibilityResponse());
            responseCheck.Data.Should();
            responseCheck.Data.Status.Should().Be(CheckEligibilityStatus.queuedForProcessing.ToString());

            Enum.TryParse(responseCheck.Data.Status, out CheckEligibilityStatus status);
            var attempts = 0;
            while (status == CheckEligibilityStatus.queuedForProcessing)
            {
                var responseStatus = await _endpoint.ApiDataGetAsynch($"{responseCheck.Links.Get_EligibilityCheckStatus}", new CheckEligibilityStatusResponse());
                Enum.TryParse(responseStatus.Data.Status, out CheckEligibilityStatus statusCheck);
                status = statusCheck;

                Thread.Sleep(1000); //wait 1 second
                attempts++;
                if (attempts >= 60)
                {
                    break;
                    //throw new Exception("Failed to get processed check result within 30 seconds");
                }
            }

            //assert
            status.Should().Be(expectedStatus);
        }
    }
}