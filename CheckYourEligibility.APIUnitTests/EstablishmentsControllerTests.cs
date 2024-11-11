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
    public class EstablishmentsControllerTests : TestBase.TestBase
    {
        private Mock<IEstablishmentSearch> _mockService;
        private ILogger<EstablishmentsController> _mockLogger;
        private EstablishmentsController _sut;
        private Mock<IAudit> _mockAuditService;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<IEstablishmentSearch>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<EstablishmentsController>>();
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new EstablishmentsController(_mockLogger, _mockService.Object, _mockAuditService.Object);
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
            IEstablishmentSearch service = null;
            IAudit auditService = null;

            // Act
            Action act = () => new EstablishmentsController(_mockLogger, service,auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'service')");
        }

        [Test]
        public void Given_Search_Should_Return_Status200OK()
        {
            // Arrange
            var query = _fixture.Create<string>();
            var result = _fixture.CreateMany<Domain.Responses.Establishment>();
            _mockService.Setup(cs => cs.Search(query)).ReturnsAsync(result);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());

            var expectedResult = new ObjectResult(new EstablishmentSearchResponse { Data = result }) { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.Search(query);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Search_Should_Return_Status400BadRequest()
        {
            // Arrange
            var query = "A";

            // Act
            var response = _sut.Search(query);

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestObjectResult));
        }

        [Test]
        public void Given_Search_Should_Return_Status404NotFound()
        {
            // Arrange
            var query = _fixture.Create<string>();
            var result = Enumerable.Empty<Domain.Responses.Establishment>(); 
            _mockService.Setup(cs => cs.Search(query)).ReturnsAsync(result);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());

            var expectedResult = new ObjectResult(new EstablishmentSearchResponse { Data = result }) { StatusCode = StatusCodes.Status404NotFound };

            // Act
            var response = _sut.Search(query);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }
    }
}