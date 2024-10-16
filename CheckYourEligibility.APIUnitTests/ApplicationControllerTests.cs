using AutoFixture;
using Azure.Core;
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

namespace CheckYourEligibility.APIUnitTests
{
    public class ApplicationControllerTests : TestBase.TestBase
    {
      
        private Mock<IApplication> _mockApplicationService;
        private Mock<IAudit> _mockAuditService;
        private ILogger<EligibilityCheckController> _mockLogger;
        private ApplicationController _sut;

        [SetUp]
        public void Setup()
        {
            _mockApplicationService = new Mock<IApplication>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<EligibilityCheckController>>();
            _sut = new ApplicationController(_mockLogger, _mockApplicationService.Object, _mockAuditService.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _mockApplicationService.VerifyAll();
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_service_is_null()
        {
            // Arrange
            IApplication applicationService = null;
            IAudit auditService = null;

            // Act
            Action act = () => new ApplicationController(_mockLogger, applicationService, auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'applicationService')");
        }

        [Test]
        public void Given_valid_NInumber_ApplicationRequest_Post_Should_Return_Status201Created()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequest>();
            var applicationFsm = _fixture.Create<ApplicationResponse>();
            request.Data.ParentNationalInsuranceNumber = "ns738356d";
            request.Data.ParentDateOfBirth = "1970-02-01";
            request.Data.ChildDateOfBirth = "1970-02-01";
            request.Data.ParentNationalAsylumSeekerServiceNumber = string.Empty;
            _mockApplicationService.Setup(cs => cs.PostApplication(request.Data)).ReturnsAsync(applicationFsm);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());

            var expectedResult = new ObjectResult(new ApplicationSaveItemResponse
            {
                Data = applicationFsm,
                Links = new ApplicationResponseLinks
                {
                    get_Application = $"{Domain.Constants.ApplicationLinks.GetLinkApplication}{applicationFsm.Id}"
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
        public void Given_InValidRequest_Validation_Application_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequest>();
            request.Data.ParentLastName = string.Empty;

            // Act
            var response = _sut.Application(request);

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestObjectResult));
        }


        [Test]
        public void Given_InValidRequest_Type_Application_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequest>();
            request.Data.Type = CheckEligibilityType.None;

            // Act
            var response = _sut.Application(request);

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestObjectResult));
        }

        [Test]
        public async Task Given_Exception_Return_500()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequest>();
            request.Data.Type = CheckEligibilityType.FreeSchoolMeals;
            var applicationFsm = _fixture.Create<ApplicationResponse>();
            request.Data.ParentNationalInsuranceNumber = "ns738356d";
            request.Data.ParentDateOfBirth = "1970-02-01";
            request.Data.ChildDateOfBirth = "1970-02-01";
            request.Data.ParentNationalAsylumSeekerServiceNumber = string.Empty;

            var expectedResult = new StatusCodeResult(500);

            // Act
            var response = await _sut.Application(request);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);

        }

        // Act
      
        [Test]
        public void Given_InValid_guid_Application_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            _mockApplicationService.Setup(cs => cs.GetApplication(guid)).Returns(Task.FromResult<ApplicationResponse?>(null));
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
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());
            _mockApplicationService.Setup(cs => cs.GetApplication(guid)).ReturnsAsync(expectedResponse);
            var expectedResult = new ObjectResult(new ApplicationItemResponse
            {
                Data = expectedResponse,
                Links = new ApplicationResponseLinks
                {
                    get_Application = $"{Domain.Constants.ApplicationLinks.GetLinkApplication}{expectedResponse.Id}"
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
            var searchData = _fixture.Create<ApplicationRequestSearchData>();
            var model = new ApplicationRequestSearch
            {
                Data = searchData,
                PageNumber = 1,
                PageSize = 10
            };
            _mockApplicationService.Setup(cs => cs.GetApplications(It.IsAny<ApplicationRequestSearch>())).ReturnsAsync(new ApplicationSearchResponse { Data = new List<ApplicationResponse>() });
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
            var searchData = _fixture.Create<ApplicationRequestSearchData>();
            var model = new ApplicationRequestSearch
            {
                Data = searchData,
                PageNumber = 1,
                PageSize = 10
            };
            var expectedResponse = _fixture.CreateMany<ApplicationResponse>();
            _mockApplicationService.Setup(cs => cs.GetApplications(It.IsAny<ApplicationRequestSearch>())).ReturnsAsync(new ApplicationSearchResponse { Data = expectedResponse });
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());
            var expectedResult = new ObjectResult(new ApplicationSearchResponse { Data = expectedResponse })
            {
                StatusCode = StatusCodes.Status200OK
            };

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
            _mockApplicationService.Setup(cs => cs.UpdateApplicationStatus(guid, request.Data)).Returns(Task.FromResult<ApplicationStatusUpdateResponse?>(null));
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
            _mockApplicationService.Setup(cs => cs.UpdateApplicationStatus(guid,request.Data)).ReturnsAsync(expectedResponse);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());
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
        public void Given_Valid_guid_CheckEligibilityStatusUpdate_Should_Return_StatusOk()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var request = _fixture.Create<ApplicationStatusUpdateRequest>();
            var expectedResponse = _fixture.Create<ApplicationStatusUpdateResponse>();
            _mockApplicationService.Setup(cs => cs.UpdateApplicationStatus(guid, request.Data)).ReturnsAsync(expectedResponse);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());
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