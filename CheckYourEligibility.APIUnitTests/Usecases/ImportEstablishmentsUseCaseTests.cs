using AutoFixture;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services.CsvImport;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class ImportEstablishmentsUseCaseTests: TestBase.TestBase
    {
        private Mock<IAdministration> _mockService;
        private Mock<IAudit> _mockAuditService;
        private Mock<ILogger<ImportEstablishmentsUseCase>> _mockLogger;
        private ImportEstablishmentsUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<IAdministration>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<ImportEstablishmentsUseCase>>(MockBehavior.Loose);
            _sut = new ImportEstablishmentsUseCase(_mockService.Object, _mockAuditService.Object, _mockLogger.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockService.VerifyAll();
            _mockAuditService.VerifyAll();
        }

        [Test]
        public async Task Execute_Should_ImportEstablishments_When_File_Is_Valid()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var content = Properties.Resources.small_gis;
            var fileName = "test.csv";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("text/csv");

            _mockService.Setup(s => s.ImportEstablishments(It.IsAny<List<EstablishmentRow>>())).Returns(Task.CompletedTask);
            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.Administration, string.Empty)).Returns((AuditData)null);

            // Act
            await _sut.Execute(fileMock.Object);

            // Assert
            _mockService.Verify(s => s.ImportEstablishments(It.IsAny<List<EstablishmentRow>>()), Times.Once);
        }

        [Test]
        public void Execute_Should_Throw_InvalidDataException_When_File_Is_Null()
        {
            // Act
            Func<Task> act = async () => await _sut.Execute(null);

            // Assert
            act.Should().ThrowAsync<InvalidDataException>().WithMessage("CSV file required.");
        }

        [Test]
        public void Execute_Should_Throw_InvalidDataException_When_File_Is_Not_CSV()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.ContentType).Returns("application/json");

            // Act
            Func<Task> act = async () => await _sut.Execute(fileMock.Object);

            // Assert
            act.Should().ThrowAsync<InvalidDataException>().WithMessage("CSV file required.");
        }

        [Test]
        public void Execute_Should_Throw_InvalidDataException_When_File_Content_Is_Invalid()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var content = "InvalidContent";
            var fileName = "test.csv";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("text/csv");

            // Act
            Func<Task> act = async () => await _sut.Execute(fileMock.Object);

            // Assert
            act.Should().ThrowAsync<InvalidDataException>().WithMessage("Invalid file content.");
        }

        [Test]
        public void Execute_Should_LogError_And_Throw_InvalidDataException_On_Exception()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var content = Properties.Resources.small_gis;
            var fileName = "test.csv";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("text/csv");

            _mockService.Setup(s => s.ImportEstablishments(It.IsAny<List<EstablishmentRow>>())).Throws(new Exception("Test exception"));

            // Act
            Func<Task> act = async () => await _sut.Execute(fileMock.Object);

            // Assert
            act.Should().ThrowAsync<InvalidDataException>().WithMessage($"{fileName} - {{}} :- Test exception, ");
        }

        [Test]
        public void Constructor_Should_Throw_ArgumentNullException_When_Service_Is_Null()
        {
            // Arrange
            IAdministration service = null;
            IAudit auditService = _mockAuditService.Object;
            ILogger<ImportEstablishmentsUseCase> logger = _mockLogger.Object;

            // Act
            Action act = () => new ImportEstablishmentsUseCase(service, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'service')");
        }

        [Test]
        public void Constructor_Should_Throw_ArgumentNullException_When_AuditService_Is_Null()
        {
            // Arrange
            IAdministration service = _mockService.Object;
            IAudit auditService = null;
            ILogger<ImportEstablishmentsUseCase> logger = _mockLogger.Object;

            // Act
            Action act = () => new ImportEstablishmentsUseCase(service, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'auditService')");
        }

        [Test]
        public void Constructor_Should_Throw_ArgumentNullException_When_Logger_Is_Null()
        {
            // Arrange
            IAdministration service = _mockService.Object;
            IAudit auditService = _mockAuditService.Object;
            ILogger<ImportEstablishmentsUseCase> logger = null;

            // Act
            Action act = () => new ImportEstablishmentsUseCase(service, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'logger')");
        }

        [Test]
        public async Task Execute_Should_Call_AuditAdd_When_AuditData_Is_Not_Null()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var content = Properties.Resources.small_gis;
            var fileName = "test.csv";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("text/csv");

            _mockService.Setup(s => s.ImportEstablishments(It.IsAny<List<EstablishmentRow>>())).Returns(Task.CompletedTask);
            var auditData = _fixture.Create<AuditData>();
            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.Administration, string.Empty)).Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData)).ReturnsAsync(_fixture.Create<string>());

            // Act
            await _sut.Execute(fileMock.Object);

            // Assert
            _mockAuditService.Verify(a => a.AuditAdd(auditData), Times.Once);
        }

        [Test]
        public async Task Execute_Should_Not_Call_AuditAdd_When_AuditData_Is_Null()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var content = Properties.Resources.small_gis;
            var fileName = "test.csv";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("text/csv");

            _mockService.Setup(s => s.ImportEstablishments(It.IsAny<List<EstablishmentRow>>())).Returns(Task.CompletedTask);
            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.Administration, string.Empty)).Returns((AuditData)null);

            // Act
            await _sut.Execute(fileMock.Object);

            // Assert
            _mockAuditService.Verify(a => a.AuditAdd(It.IsAny<AuditData>()), Times.Never);
        }
    }
}