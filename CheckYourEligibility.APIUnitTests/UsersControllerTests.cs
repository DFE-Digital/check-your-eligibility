using AutoFixture;
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
    public class UsersControllerTests : TestBase.TestBase
    {
        private Mock<IUsers> _mockService;
        private ILogger<UsersController> _mockLogger;
        private UsersController _sut;
        private Mock<IAudit> _mockAuditService;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<IUsers>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<UsersController>>();
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new UsersController(_mockLogger, _mockService.Object, _mockAuditService.Object);
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
            IUsers service = null;
            IAudit auditService = null;

            // Act
            Action act = () => new UsersController(_mockLogger, service, auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'service')");
        }

        [Test]
        public async Task Given_ApplicationSearch_Exception_Return_500()
        {
            // Arrange
            var request = _fixture.Create<UserCreateRequest>();
            _mockService.Setup(x => x.Create(It.IsAny<UserData>())).Throws<Exception>();

            var expectedResult = new StatusCodeResult(500);

            // Act
            var response = await _sut.User(request);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_valid_Request_Post_Should_Return_Status201Created()
        {
            // Arrange
            var request = _fixture.Create<UserCreateRequest>();
            var id = _fixture.Create<Guid>().ToString();
            _mockService.Setup(cs => cs.Create(request.Data)).ReturnsAsync(id);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());

            var expectedResult = new ObjectResult(new UserSaveItemResponse
            {
                Data = id
            })
            { StatusCode = StatusCodes.Status201Created };

            // Act
            var response = _sut.User(request);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = new UserCreateRequest();
            
            // Act
            var response = _sut.User(request);

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestObjectResult));
        }
    }
}