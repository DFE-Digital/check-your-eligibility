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
    public class SchoolsControllerTests : TestBase.TestBase
    {
        private Mock<ISchoolsSearch> _mockService;
        private ILogger<SchoolsController> _mockLogger;
        private SchoolsController _sut;
        private Mock<IAudit> _mockAuditService;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<ISchoolsSearch>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<SchoolsController>>();
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new SchoolsController(_mockLogger, _mockService.Object, _mockAuditService.Object);
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
            ISchoolsSearch service = null;
            IAudit auditService = null;

            // Act
            Action act = () => new SchoolsController(_mockLogger, service,auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'service')");
        }

        [Test]
        public void Given_Search_Should_Return_Status200OK()
        {
            // Arrange
            var query = _fixture.Create<string>();
            var result = _fixture.CreateMany<Domain.Responses.School>();
            _mockService.Setup(cs => cs.Search(query)).ReturnsAsync(result);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());

            var expectedResult = new ObjectResult(new SchoolSearchResponse { Data = result }) { StatusCode = StatusCodes.Status200OK };

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
            var result = Enumerable.Empty<Domain.Responses.School>(); 
            _mockService.Setup(cs => cs.Search(query)).ReturnsAsync(result);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());

            var expectedResult = new ObjectResult(new SchoolSearchResponse { Data = result }) { StatusCode = StatusCodes.Status404NotFound };

            // Act
            var response = _sut.Search(query);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }
    }
}