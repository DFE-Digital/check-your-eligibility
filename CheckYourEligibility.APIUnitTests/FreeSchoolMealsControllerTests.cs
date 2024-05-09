using AutoFixture;
using Azure;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework.Internal;
using System.Diagnostics;

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
        public void Given_valid_NInumber_ApplicationRequest_Post_Should_Return_Status201Created()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequest>();
            var applicationFsm = _fixture.Create<ApplicationSave>();
            request.Data.ParentNationalInsuranceNumber = "ns738356d";
            request.Data.ParentDateOfBirth = "01/02/1970";
            request.Data.ChildDateOfBirth = "01/02/1970";
            request.Data.ParentNationalAsylumSeekerServiceNumber = string.Empty;
            _mockService.Setup(cs => cs.PostApplication(request.Data)).ReturnsAsync(applicationFsm);

            var expectedResult = new ObjectResult(new ApplicationSaveItemResponse
            {
                Data = applicationFsm,
                Links = new ApplicationResponseLinks
                {
                    get_Application = $"{Domain.Constants.FSMLinks.GetLinkApplication}{applicationFsm.Id}"
                }
            })
            { StatusCode = StatusCodes.Status201Created };

            // Act
            var response = _sut.Application(request);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_Values_Application_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = new ApplicationRequest();

            // Act
            var response = _sut.Application(request);

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestObjectResult));
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
            _mockService.Setup(cs => cs.PostCheck(request.Data)).ReturnsAsync(new PostCheckResult { Id = id});

            var expectedResult = new ObjectResult(new CheckEligibilityStatusResponse() { Data = new StatusValue() { Status = CheckEligibilityStatus.queuedForProcessing.ToString() } }) { StatusCode = StatusCodes.Status202Accepted };

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
            var expectedResult = new BadRequestObjectResult(new MessageResponse { Data = Domain.Constants.ErrorMessages.FSM.NI_and_NASS });

            // Act
            var response = _sut.CheckEligibility(request);
            
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
            var expectedResult = new BadRequestObjectResult(new MessageResponse { Data = Domain.Constants.ErrorMessages.FSM.NI_or_NASS });

            // Act
            var response = _sut.CheckEligibility(request);
           
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
            var expectedResult = new BadRequestObjectResult(new MessageResponse { Data = Domain.Constants.ErrorMessages.FSM.NI });

            // Act
            var response = _sut.CheckEligibility(request);
            
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
            var expectedResult = new BadRequestObjectResult(new MessageResponse { Data = Domain.Constants.ErrorMessages.FSM.DOB });

            // Act
            var response = _sut.CheckEligibility(request);
            
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
            var expectedResult = new BadRequestObjectResult(new MessageResponse { Data = Domain.Constants.ErrorMessages.FSM.LastName });
            
            // Act
            var response = _sut.CheckEligibility(request);
           
            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValid_guid_CheckEligibilityStatus_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            _mockService.Setup(cs => cs.GetStatus(guid)).Returns(Task.FromResult<CheckEligibilityStatus?>(null));
            var expectedResult = new ObjectResult(guid)
            { StatusCode = StatusCodes.Status404NotFound };

            // Act
            var response = _sut.CheckEligibilityStatus(guid);
            
            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_Not_queuedForProcessing_Process_Should_Return_BadRequest()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            _mockService.Setup(cs => cs.ProcessCheck(guid)).ThrowsAsync(new ProcessCheckException());
            var expectedResult = new ObjectResult(guid)
            { StatusCode = StatusCodes.Status400BadRequest };

            // Act
            var response = _sut.Process(guid);
            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }


        [Test]
        public void Given_InValid_guid_Process_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            _mockService.Setup(cs => cs.ProcessCheck(guid)).Returns(Task.FromResult<CheckEligibilityStatus?>(null));
            var expectedResult = new ObjectResult(guid)
            { StatusCode = StatusCodes.Status404NotFound };

            // Act
            var response = _sut.Process(guid);


            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_Process_Should_Return_StatusOk()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var expectedResponse = CheckEligibilityStatus.parentNotFound;
            _mockService.Setup(cs => cs.ProcessCheck(guid)).ReturnsAsync(expectedResponse);
            expectedResponse = CheckEligibilityStatus.parentNotFound;
            var expectedResult = new ObjectResult(new CheckEligibilityStatusResponse() { Data = new StatusValue() { Status = expectedResponse.ToString() } }) { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.Process(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_DWPError_Process_Should_Return_queuedForProcessing()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var expectedResponse = CheckEligibilityStatus.queuedForProcessing;
            _mockService.Setup(cs => cs.ProcessCheck(guid)).ReturnsAsync(expectedResponse);
            expectedResponse = CheckEligibilityStatus.queuedForProcessing;
            var expectedResult = new ObjectResult(new CheckEligibilityStatusResponse() { Data = new StatusValue() { Status = expectedResponse.ToString() } }) { StatusCode = StatusCodes.Status503ServiceUnavailable };

            // Act
            var response = _sut.Process(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValid_guid_GetEligibilityCheck_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            _mockService.Setup(cs => cs.GetItem(guid)).Returns(Task.FromResult<CheckEligibilityItemFsm?>(null));
            var expectedResult = new ObjectResult(guid)
            { StatusCode = StatusCodes.Status404NotFound };

            // Act
            var response = _sut.EligibilityCheck(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_GetEligibilityCheck_Should_Return_StatusOk()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var expectedResponse = _fixture.Create<CheckEligibilityItemFsm>();
            _mockService.Setup(cs => cs.GetItem(guid)).ReturnsAsync(expectedResponse);
            var expectedResult = new ObjectResult(new CheckEligibilityItemResponse()
            {
                Data = expectedResponse,
                Links = new CheckEligibilityResponseLinks
                {
                    Get_EligibilityCheck = $"{Domain.Constants.FSMLinks.GetLink}{guid}",
                    Put_EligibilityCheckProcess = $"{Domain.Constants.FSMLinks.ProcessLink}{guid}"
                }
            })
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.EligibilityCheck(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValid_guid_Application_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            _mockService.Setup(cs => cs.GetApplication(guid)).Returns(Task.FromResult<ApplicationResponse?>(null));
            var expectedResult = new ObjectResult(guid)
            { StatusCode = StatusCodes.Status404NotFound };

            // Act
            var response = _sut.Application(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_Application_Should_Return_StatusOk()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var expectedResponse = _fixture.Create<ApplicationResponse>();
            _mockService.Setup(cs => cs.GetApplication(guid)).ReturnsAsync(expectedResponse);
            var expectedResult = new ObjectResult(new ApplicationItemResponse
            {
                Data = expectedResponse,
                Links = new ApplicationResponseLinks
                {
                    get_Application = $"{Domain.Constants.FSMLinks.GetLinkApplication}{expectedResponse.Id}"
                }
            })
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.Application(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValid_ApplicationSearch_Should_Return_Status204NoContent()
        {
            // Arrange
            var model = _fixture.Create<ApplicationRequestSearch>();
            _mockService.Setup(cs => cs.GetApplications(model.Data)).ReturnsAsync(new List<ApplicationResponse>());
            var expectedResult = new NoContentResult();
            
            // Act
            var response = _sut.ApplicationSearch(model);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_ApplicationSearch_Should_Return_StatusOk()
        {
            // Arrange
            var model = _fixture.Create<ApplicationRequestSearch>();
            var expectedResponse = _fixture.CreateMany<ApplicationResponse>();
            _mockService.Setup(cs => cs.GetApplications(model.Data)).ReturnsAsync(expectedResponse);
            var expectedResult = new ObjectResult(new ApplicationSearchResponse { Data = expectedResponse }){ StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.ApplicationSearch(model);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValid_guid_ApplicationStatusUpdate_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var request = _fixture.Create<ApplicationStatusUpdateRequest>();
            _mockService.Setup(cs => cs.UpdateApplicationStatus(guid, request.Data)).Returns(Task.FromResult<ApplicationStatusUpdateResponse?>(null));
            var expectedResult = new NotFoundResult();

            // Act
            var response = _sut.ApplicationStatusUpdate(guid,request);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_ApplicationStatusUpdate_Should_Return_StatusOk()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var request = _fixture.Create<ApplicationStatusUpdateRequest>();
            var expectedResponse = _fixture.Create<ApplicationStatusUpdateResponse>();
            _mockService.Setup(cs => cs.UpdateApplicationStatus(guid,request.Data)).ReturnsAsync(expectedResponse);
            var expectedResult = new ObjectResult(new ApplicationStatusUpdateResponse
            {
                Data = expectedResponse.Data
            })
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.ApplicationStatusUpdate(guid, request);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValid_guid_CheckEligibilityStatusUpdate_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var request = _fixture.Create<EligibilityStatusUpdateRequest>();
            _mockService.Setup(cs => cs.UpdateEligibilityCheckStatus(guid, request.Data)).Returns(Task.FromResult<CheckEligibilityStatusResponse?>(null));
            var expectedResult = new NotFoundResult();

            // Act
            var response = _sut.EligibilityCheckStatusUpdate(guid, request);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_CheckEligibilityStatusUpdate_Should_Return_StatusOk()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var request = _fixture.Create<ApplicationStatusUpdateRequest>();
            var expectedResponse = _fixture.Create<ApplicationStatusUpdateResponse>();
            _mockService.Setup(cs => cs.UpdateApplicationStatus(guid, request.Data)).ReturnsAsync(expectedResponse);
            var expectedResult = new ObjectResult(new ApplicationStatusUpdateResponse
            {
                Data = expectedResponse.Data
            })
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.ApplicationStatusUpdate(guid, request);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }
    }
}