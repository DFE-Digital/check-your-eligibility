using AutoFixture;
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

namespace CheckYourEligibility.APIUnitTests
{
    public class UsersControllerTests : TestBase.TestBase
    {
        private Mock<ICreateOrUpdateUserUseCase> _mockCreateOrUpdateUserUseCase;
        private ILogger<UsersController> _mockLogger;
        private UsersController _sut;
        private Mock<IAudit> _mockAuditService;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockCreateOrUpdateUserUseCase = new Mock<ICreateOrUpdateUserUseCase>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<UsersController>>();
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new UsersController(_mockLogger, _mockCreateOrUpdateUserUseCase.Object, _mockAuditService.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockCreateOrUpdateUserUseCase.VerifyAll();
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_service_is_null()
        {
            // Arrange
            ICreateOrUpdateUserUseCase createOrUpdateUserUseCase = null;
            IAudit auditService = null;

            // Act
            Action act = () => new UsersController(_mockLogger, createOrUpdateUserUseCase, auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'createOrUpdateUserUseCase')");
        }

        [Test]
        public async Task Given_valid_Request_Post_Should_Return_Status201Created()
        {
            // Arrange
            var request = _fixture.Create<UserCreateRequest>();
            var response = _fixture.Create<UserSaveItemResponse>();
            _mockCreateOrUpdateUserUseCase.Setup(cs => cs.Execute(request)).ReturnsAsync(response);

            var expectedResult = new ObjectResult(response)
            { StatusCode = StatusCodes.Status201Created };

            // Act
            var result = await _sut.User(request);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_InValidRequest_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = new UserCreateRequest();

            // Act
            var result = await _sut.User(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}