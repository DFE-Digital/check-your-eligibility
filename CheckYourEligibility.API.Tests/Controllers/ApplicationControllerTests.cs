using AutoFixture;
using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Gateways.Interfaces;
using CheckYourEligibility.API.Controllers;
using CheckYourEligibility.API.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using CheckYourEligibility.API.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using ValidationException = FluentValidation.ValidationException;

namespace CheckYourEligibility.API.Tests
{
    public class ApplicationControllerTests : TestBase.TestBase
    {
        private Mock<ICreateApplicationUseCase> _mockCreateApplicationUseCase;
        private Mock<IGetApplicationUseCase> _mockGetApplicationUseCase;
        private Mock<ISearchApplicationsUseCase> _mockSearchApplicationsUseCase;
        private Mock<IUpdateApplicationStatusUseCase> _mockUpdateApplicationStatusUseCase;
        private Mock<IAudit> _mockAuditGateway;
        private ILogger<ApplicationController> _mockLogger;
        private IConfigurationRoot _configuration;
        private ApplicationController _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockCreateApplicationUseCase = new Mock<ICreateApplicationUseCase>(MockBehavior.Strict);
            _mockGetApplicationUseCase = new Mock<IGetApplicationUseCase>(MockBehavior.Strict);
            _mockSearchApplicationsUseCase = new Mock<ISearchApplicationsUseCase>(MockBehavior.Strict);
            _mockUpdateApplicationStatusUseCase = new Mock<IUpdateApplicationStatusUseCase>(MockBehavior.Strict);
            _mockAuditGateway = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<ApplicationController>>();

            // config data for Jwt:Scopes:local_authority
            var configData = new Dictionary<string, string>
            {
                {"Jwt:Scopes:local_authority", "local_authority"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            _sut = new ApplicationController(
                _mockLogger,
                _configuration,
                _mockCreateApplicationUseCase.Object,
                _mockGetApplicationUseCase.Object,
                _mockSearchApplicationsUseCase.Object,
                _mockUpdateApplicationStatusUseCase.Object,
                _mockAuditGateway.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockCreateApplicationUseCase.VerifyAll();
            _mockGetApplicationUseCase.VerifyAll();
            _mockSearchApplicationsUseCase.VerifyAll();
            _mockUpdateApplicationStatusUseCase.VerifyAll();
            _mockAuditGateway.VerifyAll();
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
            badRequestResult.Value.Should().BeOfType<ErrorResponse>();
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
                .ThrowsAsync(new FluentValidation.ValidationException("Invalid request, Valid Type is required: None"));

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
            
            _mockGetApplicationUseCase.Setup(cs => cs.Execute(guid))
                .ThrowsAsync(new NotFoundException());
            var expectedResult = new NotFoundObjectResult(new ErrorResponse() {Errors = [new Error() { Title = guid}]});

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
        public async Task Given_Valid_ApplicationSearch_Should_Return_StatusOk()
        {
            // Arrange
            var model = _fixture.Create<ApplicationRequestSearch>();
            var expectedResponse = _fixture.Create<ApplicationSearchResponse>();
            _mockSearchApplicationsUseCase.Setup(cs => cs.Execute(model, null)).ReturnsAsync(expectedResponse);
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