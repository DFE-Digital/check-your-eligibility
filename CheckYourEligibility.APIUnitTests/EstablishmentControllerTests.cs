using AutoFixture;
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
    public class EstablishmentControllerTests : TestBase.TestBase
    {
        private Mock<ISearchEstablishmentsUseCase> _mockSearchUseCase;
        private ILogger<EstablishmentController> _mockLogger;
        private EstablishmentController _sut;
        private Mock<IAudit> _mockAuditService;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockSearchUseCase = new Mock<ISearchEstablishmentsUseCase>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<EstablishmentController>>();
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new EstablishmentController(_mockLogger, _mockSearchUseCase.Object, _mockAuditService.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockSearchUseCase.VerifyAll();
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_service_is_null()
        {
            // Arrange
            ISearchEstablishmentsUseCase searchUseCase = null;
            IAudit auditService = null;

            // Act
            Action act = () => new EstablishmentController(_mockLogger, searchUseCase, auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'searchUseCase')");
        }

        [Test]
        public async Task Given_Search_Should_Return_Status200OK()
        {
            // Arrange
            var query = _fixture.Create<string>();
            var result = _fixture.CreateMany<Establishment>().ToList();
            _mockSearchUseCase.Setup(cs => cs.Execute(query)).ReturnsAsync(result);

            var expectedResult = new ObjectResult(new EstablishmentSearchResponse { Data = result }) { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = await _sut.Search(query);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_Search_Should_Return_Status400BadRequest()
        {
            // Arrange
            var query = "A";

            // Act
            var response = await _sut.Search(query);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public async Task Given_Search_Should_Return_Status200NotFound()
        {
            // Arrange
            var query = _fixture.Create<string>();
            var result = Enumerable.Empty<Establishment>();
            _mockSearchUseCase.Setup(cs => cs.Execute(query)).ReturnsAsync(result);

            var expectedResult = new ObjectResult(new EstablishmentSearchResponse { Data = result }) { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = await _sut.Search(query);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }
    }
}