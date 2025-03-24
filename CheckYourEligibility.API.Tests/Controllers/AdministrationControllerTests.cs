using CheckYourEligibility.API.Domain.Constants;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Gateways.Interfaces;
using CheckYourEligibility.API.Controllers;
using CheckYourEligibility.API.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework.Internal;

namespace CheckYourEligibility.API.Tests
{
    public class AdministrationControllerTests : TestBase.TestBase
    {
        private Mock<ICleanUpEligibilityChecksUseCase> _mockCleanUpEligibilityChecksUseCase;
        private Mock<IImportEstablishmentsUseCase> _mockImportEstablishmentsUseCase;
        private Mock<IImportFsmHomeOfficeDataUseCase> _mockImportFsmHomeOfficeDataUseCase;
        private Mock<IImportFsmHMRCDataUseCase> _mockImportFsmHMRCDataUseCase;
        private ILogger<AdministrationController> _mockLogger;
        private AdministrationController _sut;
        private Mock<IAudit> _mockAuditGateway;

        [SetUp]
        public void Setup()
        {
            _mockCleanUpEligibilityChecksUseCase = new Mock<ICleanUpEligibilityChecksUseCase>(MockBehavior.Strict);
            _mockImportEstablishmentsUseCase = new Mock<IImportEstablishmentsUseCase>(MockBehavior.Strict);
            _mockImportFsmHomeOfficeDataUseCase = new Mock<IImportFsmHomeOfficeDataUseCase>(MockBehavior.Strict);
            _mockImportFsmHMRCDataUseCase = new Mock<IImportFsmHMRCDataUseCase>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<AdministrationController>>();
            _mockAuditGateway = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new AdministrationController(
                _mockCleanUpEligibilityChecksUseCase.Object,
                _mockImportEstablishmentsUseCase.Object,
                _mockImportFsmHomeOfficeDataUseCase.Object,
                _mockImportFsmHMRCDataUseCase.Object,
                _mockAuditGateway.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _mockCleanUpEligibilityChecksUseCase.VerifyAll();
            _mockImportEstablishmentsUseCase.VerifyAll();
            _mockImportFsmHomeOfficeDataUseCase.VerifyAll();
            _mockImportFsmHMRCDataUseCase.VerifyAll();
        }

        [Test]
        public async Task Given_CleanUpEligibilityChecks_Should_Return_Status200OK()
        {
            // Arrange
            _mockCleanUpEligibilityChecksUseCase.Setup(cs => cs.Execute()).Returns(Task.CompletedTask);

            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{Admin.EligibilityChecksCleanse}" }) { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = await _sut.CleanUpEligibilityChecks();

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_ImportEstablishments_Should_Return_Status200OK()
        {
            // Arrange
            _mockImportEstablishmentsUseCase.Setup(cs => cs.Execute(It.IsAny<IFormFile>())).Returns(Task.CompletedTask);

            var content = Properties.Resources.small_gis;
            var fileName = "test.csv";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            var file = new FormFile(stream, 0, stream.Length, fileName, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };
            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {Admin.EstablishmentFileProcessed}" }) { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = await _sut.ImportEstablishments(file);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_ImportEstablishments_Should_Return_Status400BadRequest()
        {
            // Arrange
            var expectedResult = new ObjectResult(new ErrorResponse { Errors = [new Error() { Title=$"{Admin.CsvfileRequired}" }]}) { StatusCode = StatusCodes.Status400BadRequest };

            // Setup mock to throw InvalidDataException
            _mockImportEstablishmentsUseCase
                .Setup(u => u.Execute(It.IsAny<IFormFile>()))
                .Throws(new InvalidDataException($"{Admin.CsvfileRequired}"));

            // Act
            var response = await _sut.ImportEstablishments(null);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_ImportFsmHomeOfficeData_Should_Return_Status200OK()
        {
            // Arrange
            _mockImportFsmHomeOfficeDataUseCase.Setup(cs => cs.Execute(It.IsAny<IFormFile>())).Returns(Task.CompletedTask);

            var content = Properties.Resources.HO_Data_small;
            var fileName = "test.csv";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            var file = new FormFile(stream, 0, stream.Length, fileName, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };
            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {Admin.HomeOfficeFileProcessed}" }) { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = await _sut.ImportFsmHomeOfficeData(file);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_ImportFsmHomeOfficeData_Should_Return_Status400BadRequest()
        {
            // Arrange
            var expectedResult = new ObjectResult(new ErrorResponse { Errors = [new Error() {Title = $"{Admin.CsvfileRequired}" }]}){ StatusCode = StatusCodes.Status400BadRequest };

            // Setup mock to throw InvalidDataException
            _mockImportFsmHomeOfficeDataUseCase
                .Setup(u => u.Execute(It.IsAny<IFormFile>()))
                .Throws(new InvalidDataException($"{Admin.CsvfileRequired}"));

            // Act
            var response = await _sut.ImportFsmHomeOfficeData(null);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_ImportFsmHMRCData_Should_Return_Status200OK()
        {
            // Arrange
            _mockImportFsmHMRCDataUseCase.Setup(cs => cs.Execute(It.IsAny<IFormFile>())).Returns(Task.CompletedTask);

            var content = Properties.Resources.exampleHMRC;
            var fileName = "test.xml";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            var file = new FormFile(stream, 0, stream.Length, fileName, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/xml"
            };
            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {Admin.HMRCFileProcessed}" }) { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = await _sut.ImportFsmHMRCData(file);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_ImportFsmHMRCData_Should_Return_Status400BadRequest()
        {
            // Arrange
            var expectedResult = new ObjectResult(new ErrorResponse { Errors = [new Error{Title = $"{Admin.XmlfileRequired}" }]}) { StatusCode = StatusCodes.Status400BadRequest };

            // Setup mock to throw InvalidDataException
            _mockImportFsmHMRCDataUseCase
                .Setup(u => u.Execute(It.IsAny<IFormFile>()))
                .Throws(new InvalidDataException($"{Admin.XmlfileRequired}"));

            // Act
            var response = await _sut.ImportFsmHMRCData(null);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }
    }
}