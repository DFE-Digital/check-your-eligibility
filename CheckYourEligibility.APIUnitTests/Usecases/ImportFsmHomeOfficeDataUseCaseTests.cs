// filepath: /c:/Projects/check-your-eligibility/CheckYourEligibility.WebApp.Tests/UseCases/ImportFsmHomeOfficeDataUseCaseTest.cs
using AutoFixture;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using CheckYourEligibility.Domain.Requests;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class ImportFsmHomeOfficeDataUseCaseTests : TestBase.TestBase
    {
        private Mock<IAdministration> _mockService;
        private Mock<IAudit> _mockAuditService;
        private Mock<ILogger<ImportFsmHomeOfficeDataUseCase>> _mockLogger;
        private ImportFsmHomeOfficeDataUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<IAdministration>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<ImportFsmHomeOfficeDataUseCase>>(MockBehavior.Loose);
            _sut = new ImportFsmHomeOfficeDataUseCase(_mockService.Object, _mockAuditService.Object, _mockLogger.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockService.VerifyAll();
            _mockAuditService.VerifyAll();
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_service_is_null()
        {
            // Arrange
            IAdministration service = null;
            IAudit auditService = _mockAuditService.Object;
            ILogger<ImportFsmHomeOfficeDataUseCase> logger = _mockLogger.Object;

            // Act
            Action act = () => new ImportFsmHomeOfficeDataUseCase(service, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'service')");
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_auditService_is_null()
        {
            // Arrange
            IAdministration service = _mockService.Object;
            IAudit auditService = null;
            ILogger<ImportFsmHomeOfficeDataUseCase> logger = _mockLogger.Object;

            // Act
            Action act = () => new ImportFsmHomeOfficeDataUseCase(service, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'auditService')");
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_logger_is_null()
        {
            // Arrange
            IAdministration service = _mockService.Object;
            IAudit auditService = _mockAuditService.Object;
            ILogger<ImportFsmHomeOfficeDataUseCase> logger = null;

            // Act
            Action act = () => new ImportFsmHomeOfficeDataUseCase(service, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'logger')");
        }

        [Test]
        public void Execute_Should_Throw_InvalidDataException_When_File_Is_Null()
        {
            // Arrange
            IFormFile file = null;

            // Act
            Func<Task> act = async () => await _sut.Execute(file);

            // Assert
            act.Should().ThrowExactlyAsync<InvalidDataException>().WithMessage("CSV file required.");
        }

        [Test]
        public void Execute_Should_Throw_InvalidDataException_When_File_Is_Not_CSV()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.ContentType).Returns("text/plain");

            // Act
            Func<Task> act = async () => await _sut.Execute(fileMock.Object);

            // Assert
            act.Should().ThrowExactlyAsync<InvalidDataException>().WithMessage("CSV file required.");
        }

        [Test]
        public async Task Execute_Should_Process_CSV_File_And_Call_ImportHomeOfficeData()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.ContentType).Returns("text/csv");
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Properties.Resources.HO_Data_small)));

            _mockService.Setup(s => s.ImportHomeOfficeData(It.IsAny<List<FreeSchoolMealsHO>>())).Returns(Task.CompletedTask);
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Administration, string.Empty)).ReturnsAsync(_fixture.Create<string>());

            // Act
            await _sut.Execute(fileMock.Object);

            // Assert
            _mockService.Verify(s => s.ImportHomeOfficeData(It.IsAny<List<FreeSchoolMealsHO>>()), Times.Once);
        }

        [Test]
        public void Execute_Should_Throw_InvalidDataException_When_XML_File_Has_No_Content()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.ContentType).Returns("text/csv");
            fileMock.Setup(f => f.FileName).Returns("test.csv");

            // Create empty CSV file
            var emptyCsv = "InvalidContent";
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(emptyCsv)));

            // Act
            Func<Task> act = async () => await _sut.Execute(fileMock.Object);

            // Assert
            act.Should().ThrowExactlyAsync<InvalidDataException>()
                .WithMessage("Invalid file no content.");
        }
    }
}