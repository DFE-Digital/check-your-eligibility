using AutoFixture;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.Controllers;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using FluentValidation;
using System.Threading.Tasks;

namespace CheckYourEligibility.APIUnitTests
{
    public class ApplicationControllerTests : TestBase.TestBase
    {
        private Mock<ICreateApplicationUseCase> _mockCreateApplicationUseCase;
        private Mock<IGetApplicationUseCase> _mockGetApplicationUseCase;
        private Mock<ISearchApplicationsUseCase> _mockSearchApplicationsUseCase;
        private Mock<IUpdateApplicationStatusUseCase> _mockUpdateApplicationStatusUseCase;
        private Mock<IAudit> _mockAuditService;
        private ILogger<EligibilityCheckController> _mockLogger;
        private ApplicationController _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockCreateApplicationUseCase = new Mock<ICreateApplicationUseCase>(MockBehavior.Strict);
            _mockGetApplicationUseCase = new Mock<IGetApplicationUseCase>(MockBehavior.Strict);
            _mockSearchApplicationsUseCase = new Mock<ISearchApplicationsUseCase>(MockBehavior.Strict);
            _mockUpdateApplicationStatusUseCase = new Mock<IUpdateApplicationStatusUseCase>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<EligibilityCheckController>>();
            _sut = new ApplicationController(
                _mockLogger,
                _mockCreateApplicationUseCase.Object,
                _mockGetApplicationUseCase.Object,
                _mockSearchApplicationsUseCase.Object,
                _mockUpdateApplicationStatusUseCase.Object,
                _mockAuditService.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockCreateApplicationUseCase.VerifyAll();
            _mockGetApplicationUseCase.VerifyAll();
            _mockSearchApplicationsUseCase.VerifyAll();
            _mockUpdateApplicationStatusUseCase.VerifyAll();
            _mockAuditService.VerifyAll();
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_service_is_null()
        {
            // Arrange
            ICreateApplicationUseCase createApplicationUseCase = null;
            IGetApplicationUseCase getApplicationUseCase = null;
            ISearchApplicationsUseCase searchApplicationsUseCase = null;
            IUpdateApplicationStatusUseCase updateApplicationStatusUseCase = null;
            IAudit auditService = null;

            // Act
            Action act = () => new ApplicationController(
                _mockLogger,
                createApplicationUseCase,
                getApplicationUseCase,
                searchApplicationsUseCase,
                updateApplicationStatusUseCase,
                auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'createApplicationUseCase')");
        }

        [Test]
        public async Task Given_valid_NInumber_ApplicationRequest_Post_Should_Return_Status201Created()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequest>();
            var applicationFsm = _fixture.Create<ApplicationSaveItemResponse>();
            _mockCreateApplicationUseCase.Setup(cs => cs.Execute(request)).ReturnsAsync(applicationFsm);

            var expectedResult = new ObjectResult(applicationFsm)
            { StatusCode = StatusCodes.Status201Created };

            // Act
            var response = await _sut.Application(request);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_InValidRequest_Values_Application_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = new ApplicationRequest();
            _mockCreateApplicationUseCase.Setup(cs => cs.Execute(request))
                .ThrowsAsync(new ValidationException("Invalid request, data is required"));

            // Act
            var response = await _sut.Application(request);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = response as BadRequestObjectResult;
            badRequestResult.Value.Should().BeOfType<MessageResponse>();
        }

        [Test]
        public async Task Given_InValidRequest_Validation_Application_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequest>();
            request.Data.ParentLastName = string.Empty;

            // Setup mock to throw ValidationException when called with this request
            _mockCreateApplicationUseCase.Setup(cs => cs.Execute(request))
                .ThrowsAsync(new ValidationException("Parent last name cannot be empty"));

            // Act
            var response = await _sut.Application(request);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public async Task Given_InValidRequest_Type_Application_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequest>();
            request.Data.Type = CheckEligibilityType.None;

            // Setup mock to throw ValidationException when called with this request
            _mockCreateApplicationUseCase.Setup(cs => cs.Execute(request))
                .ThrowsAsync(new ValidationException("Invalid request, Valid Type is required: None"));

            // Act
            var response = await _sut.Application(request);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public async Task Given_InValid_guid_Application_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            _mockGetApplicationUseCase.Setup(cs => cs.Execute(guid)).ReturnsAsync((ApplicationItemResponse)null);
            var expectedResult = new NotFoundObjectResult(guid);

            // Act
            var response = await _sut.Application(guid);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_Valid_guid_Application_Should_Return_StatusOk()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var expectedResponse = _fixture.Create<ApplicationItemResponse>();
            _mockGetApplicationUseCase.Setup(cs => cs.Execute(guid)).ReturnsAsync(expectedResponse);
            var expectedResult = new ObjectResult(expectedResponse)
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = await _sut.Application(guid);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_InValid_ApplicationSearch_Should_Return_Status204NoContent()
        {
            // Arrange
            var model = _fixture.Create<ApplicationRequestSearch>();
            _mockSearchApplicationsUseCase.Setup(cs => cs.Execute(model)).ReturnsAsync((ApplicationSearchResponse)null);
            var expectedResult = new NoContentResult();

            // Act
            var response = await _sut.ApplicationSearch(model);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_Valid_ApplicationSearch_Should_Return_StatusOk()
        {
            // Arrange
            var model = _fixture.Create<ApplicationRequestSearch>();
            var expectedResponse = _fixture.Create<ApplicationSearchResponse>();
            _mockSearchApplicationsUseCase.Setup(cs => cs.Execute(model)).ReturnsAsync(expectedResponse);
            var expectedResult = new ObjectResult(expectedResponse)
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = await _sut.ApplicationSearch(model);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_InValid_guid_ApplicationStatusUpdate_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var request = _fixture.Create<ApplicationStatusUpdateRequest>();
            _mockUpdateApplicationStatusUseCase.Setup(cs => cs.Execute(guid, request)).ReturnsAsync((ApplicationStatusUpdateResponse)null);
            var expectedResult = new NotFoundResult();

            // Act
            var response = await _sut.ApplicationStatusUpdate(guid, request);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_Valid_guid_ApplicationStatusUpdate_Should_Return_StatusOk()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var request = _fixture.Create<ApplicationStatusUpdateRequest>();
            var expectedResponse = _fixture.Create<ApplicationStatusUpdateResponse>();
            _mockUpdateApplicationStatusUseCase.Setup(cs => cs.Execute(guid, request)).ReturnsAsync(expectedResponse);
            var expectedResult = new ObjectResult(expectedResponse)
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = await _sut.ApplicationStatusUpdate(guid, request);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }
    }
}