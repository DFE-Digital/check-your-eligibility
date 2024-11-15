using CheckYourEligibility.AcceptanceTests.Models;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CheckYourEligibility.AcceptanceTests
{
    public class CheckTest
    {
        private Api _api;
        static Random rd = new Random();

        [SetUp]
        public async Task Setup()
        {

            _api = new Api();
            await _api.Login();
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


        private void CleanUpCheckData(string checkId)
        {
            var checkitem = _api.Db.EligibilityCheck.First(h => h.EligibilityCheckId == checkId);
            if (checkitem.Status != CheckEligibilityStatus.queuedForProcessing.ToString())
            {
                if (checkitem.EligibilityCheckHashId != null)
                {
                    var hash = _api.Db.EligibilityCheckHashes.First(h => h.EligibilityCheckHashId == checkitem.EligibilityCheckHashId);
                    var application = _api.Db.Applications.FirstOrDefault(h => h.EligibilityCheckHashId == hash.EligibilityCheckHashId);
                    if (application != null)
                    {
                        _api.Db.Applications.Remove(application);
                    }
                    _api.Db.EligibilityCheckHashes.Remove(hash);
                }
            }

            _api.Db.EligibilityCheck.Remove(checkitem);
            _api.Db.SaveChanges();
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
            var responseCheck = await _api.ApiDataPostAsynch("/EligibilityCheck/FreeSchoolMeals", data, new CheckEligibilityResponse());
            responseCheck.Data.Status.Should().Be(CheckEligibilityStatus.queuedForProcessing.ToString());

            Enum.TryParse(responseCheck.Data.Status, out CheckEligibilityStatus status);
            if (_api.RunLocal)
            {
               await _api.ApiDataPutAsynch(responseCheck.Links.Put_EligibilityCheckProcess, new CheckEligibilityStatusResponse());
                if (expectedStatus == CheckEligibilityStatus.DwpError)
                {
                    await _api.ApiDataPatchAsynch(responseCheck.Links.Get_EligibilityCheckStatus, new EligibilityStatusUpdateRequest { Data = new EligibilityCheckStatusData { Status = CheckEligibilityStatus.DwpError } }, new CheckEligibilityStatusResponse());
                }
                status = CheckEligibilityStatus.DwpError;
            }
            else
            {
               await _api.ApiDataPostAsynch($"/EligibilityCheck/ProcessQueueMessages?queue={_api.StandardQueue}", data, new OkObjectResult(""));
            }

            var attempts = 0;
            while (status == CheckEligibilityStatus.queuedForProcessing)
            {
                var responseStatus = await _api.ApiDataGetAsynch($"{responseCheck.Links.Get_EligibilityCheckStatus}", new CheckEligibilityStatusResponse());
                Enum.TryParse(responseStatus.Data.Status, out CheckEligibilityStatus statusCheck);
                status = statusCheck;

                Thread.Sleep(1000); //wait 1 second
                attempts++;
                if (attempts >= 100)
                {
                    break;
                }
            }
            //clean up
            var checkId = responseCheck.Links.Get_EligibilityCheck.Replace("/EligibilityCheck/", "");
            CleanUpCheckData(checkId);

            //assert
            status.Should().Be(expectedStatus);
        }

        [Test]
        public async Task CheckEligibility_fsm_and_createApplication()
        {
            //arrange
            var data = new CheckEligibilityRequest_Fsm
            {
                Data = new CheckEligibilityRequestData_Fsm
                {
                    LastName = CreateString(30),
                    DateOfBirth = "1990-12-15",
                    NationalInsuranceNumber = "NN668767B",
                }
            };

            //act
            var responseCheck = await _api.ApiDataPostAsynch("/EligibilityCheck/FreeSchoolMeals", data, new CheckEligibilityResponse());
            responseCheck.Data.Should();
            responseCheck.Data.Status.Should().Be(CheckEligibilityStatus.queuedForProcessing.ToString());

            Enum.TryParse(responseCheck.Data.Status, out CheckEligibilityStatus status);
            if (_api.RunLocal)
            {
               await _api.ApiDataPutAsynch(responseCheck.Links.Put_EligibilityCheckProcess, new CheckEligibilityStatusResponse());
               
            }
            else
            {
                await _api.ApiDataPostAsynch($"/EligibilityCheck/ProcessQueueMessages?queue={_api.StandardQueue}", data, new OkObjectResult(""));
            }

            var attempts = 0;
            while (status == CheckEligibilityStatus.queuedForProcessing)
            {
                var responseStatus = await _api.ApiDataGetAsynch($"{responseCheck.Links.Get_EligibilityCheckStatus}", new CheckEligibilityStatusResponse());
                Enum.TryParse(responseStatus.Data.Status, out CheckEligibilityStatus statusCheck);
                status = statusCheck;

                Thread.Sleep(1000); //wait 1 second
                attempts++;
                if (attempts >= 100)
                {
                    break;
                }
            }
            
            var checkId = responseCheck.Links.Get_EligibilityCheck.Replace("/EligibilityCheck/", "");

            var establishmentId = _api.Db.Establishments.First().EstablishmentId;
            var userid = _api.Db.Users.First().UserId;

            var appData = new ApplicationRequest
            {
                Data = new ApplicationRequestData {
                Type = CheckEligibilityType.FreeSchoolMeals,
                ParentFirstName = CreateString(30),
                ParentLastName = data.Data.LastName,
                ParentNationalInsuranceNumber = data.Data.NationalInsuranceNumber,
                ParentDateOfBirth = data.Data.DateOfBirth,
                ParentEmail = CreateString(30) + "@test.com",
                Establishment = establishmentId,
                UserId = userid,
                ChildFirstName = CreateString(30),
                ChildLastName = CreateString(30),
                ChildDateOfBirth = data.Data.DateOfBirth,
                }
            };

            var responseApplication= await _api.ApiDataPostAsynch("/Application", appData, new ApplicationSaveItemResponse());

            //assert
            status.Should().Be(CheckEligibilityStatus.eligible);

            responseApplication.Data.Status.Should().Be(ApplicationStatus.Entitled.ToString());

            //clean up
            CleanUpCheckData(checkId);
        }

    }
}