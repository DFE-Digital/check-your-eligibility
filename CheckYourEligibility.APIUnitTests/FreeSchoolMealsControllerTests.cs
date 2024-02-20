using AutoFixture;
using AutoMapper.Execution;
using Azure.Core;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Constants.ErrorMessages;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework.Internal;

namespace CheckYourEligibility.APIUnitTests
{
    public class FreeSchoolMealsControllerTests : TestBase.TestBase
    {
        private Mock<IFsmCheckEligibility> _mockService;
        private ILogger<FreeSchoolMealsController> _mockLogger;
        private FreeSchoolMealsController _sut;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<IFsmCheckEligibility>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<FreeSchoolMealsController>>();
            _sut = new FreeSchoolMealsController(_mockLogger, _mockService.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _mockService.VerifyAll();
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_service_is_null()
        {
            // Arrange
            IFsmCheckEligibility service = null;

            // Act
            Action act = () => new FreeSchoolMealsController(_mockLogger, service);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'service')");
        }

        [Test]
        public void Given_valid_NInumber_Request_Post_Should_Return_Status202Accepted()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest>();
            var id = _fixture.Create<string>();
            request.Data.NationalInsuranceNumber = "ns738356d";
            request.Data.DateOfBirth = "01/02/1970";
            request.Data.NationalAsylumSeekerServiceNumber = string.Empty;
            _mockService.Setup(cs => cs.PostCheck(request.Data)).ReturnsAsync(id);
            var expectedResult = new ObjectResult(new CheckEligibilityResponse() { Data = $"status : {FsmCheckEligibilityStatus.queuedForProcessing}", Links = $"eligibilityCheck: /eligibilityCheck/{id}" })
            { StatusCode = StatusCodes.Status202Accepted };

            // Act
            var response = _sut.CheckEligibility(request);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = new CheckEligibilityRequest();

            // Act
            var response = _sut.CheckEligibility(request);

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestObjectResult));
        }

        [Test]
        public void Given_InValidRequest_NI_and_NASS_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest>();
            request.Data.NationalInsuranceNumber = "ns738356d";
            request.Data.DateOfBirth = "01/02/1970";
            request.Data.NationalAsylumSeekerServiceNumber = "789";

            // Act
            var response = _sut.CheckEligibility(request);
            var expectedResult = new ObjectResult(new CheckEligibilityResponse() { Data = FSM.NI_and_NASS })
            { StatusCode = StatusCodes.Status400BadRequest };

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_NI_or_NASS_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest>();
            request.Data.NationalInsuranceNumber = string.Empty;
            request.Data.DateOfBirth = "01/02/1970";
            request.Data.NationalAsylumSeekerServiceNumber = string.Empty;

            // Act
            var response = _sut.CheckEligibility(request);
            var expectedResult = new ObjectResult(new CheckEligibilityResponse() { Data = FSM.NI_or_NASS })
            { StatusCode = StatusCodes.Status400BadRequest };

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_NI_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest>();
            request.Data.NationalInsuranceNumber = "123";
            request.Data.DateOfBirth = "01/02/1970";
            request.Data.NationalAsylumSeekerServiceNumber = "";

            // Act
            var response = _sut.CheckEligibility(request);
            var expectedResult = new ObjectResult(new CheckEligibilityResponse() { Data = FSM.NI })
            { StatusCode = StatusCodes.Status400BadRequest };

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_DOB_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest>();
            request.Data.NationalInsuranceNumber = "ns738356d";
            request.Data.DateOfBirth = "01/02/70";
            request.Data.NationalAsylumSeekerServiceNumber = "";

            // Act
            var response = _sut.CheckEligibility(request);
            var expectedResult = new ObjectResult(new CheckEligibilityResponse() { Data = FSM.DOB })
            { StatusCode = StatusCodes.Status400BadRequest };

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }


        [Test]
        public void Given_InValidRequest_LastName_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest>();
            request.Data.NationalInsuranceNumber = "ns738356d";
            request.Data.DateOfBirth = "01/02/1970";
            request.Data.LastName = string.Empty;
            request.Data.NationalAsylumSeekerServiceNumber = string.Empty;

            // Act
            var response = _sut.CheckEligibility(request);
            var expectedResult = new ObjectResult(new CheckEligibilityResponse() { Data = FSM.LastName })
            { StatusCode = StatusCodes.Status400BadRequest };

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValid_guid_CheckEligibilityStatus_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            _mockService.Setup(cs => cs.GetStatus(guid)).Returns(Task.FromResult<CheckEligibilityStatusResponse>(null));
            // Act
            var response = _sut.CheckEligibilityStatus(guid);
            var expectedResult = new ObjectResult(guid)
            { StatusCode = StatusCodes.Status404NotFound };

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_CheckEligibilityStatus_Should_Return_StatusOk()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var expectedResponse = _fixture.Create<CheckEligibilityStatusResponse>();
            _mockService.Setup(cs => cs.GetStatus(guid)).ReturnsAsync(expectedResponse);
            // Act
            var response = _sut.CheckEligibilityStatus(guid);
            var expectedResult = new ObjectResult(expectedResponse)
            { StatusCode = StatusCodes.Status200OK };

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }
    }
}