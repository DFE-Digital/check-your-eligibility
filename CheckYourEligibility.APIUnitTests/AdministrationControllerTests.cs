using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.CsvImport;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework.Internal;

namespace CheckYourEligibility.APIUnitTests
{
    public class AdministrationControllerTests : TestBase.TestBase
    {
        private Mock<IAdministration> _mockService;
        private ILogger<AdministrationController> _mockLogger;
        private AdministrationController _sut;
        private Mock<IAudit> _mockAuditService;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<IAdministration>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<AdministrationController>>();
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new AdministrationController(_mockLogger, _mockService.Object, _mockAuditService.Object);
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
            IAdministration service = null;
            IAudit auditService = null;

            // Act
            Action act = () => new AdministrationController(_mockLogger, service,auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'service')");
        }

        [Test]
        public void Given_CleanUpEligibilityChecks_Should_Return_Status200OK()
        {
            // Arrange
            _mockService.Setup(cs => cs.CleanUpEligibilityChecks()).Returns(Task.CompletedTask);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());

            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{Admin.EligibilityChecksCleanse}" }) { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.CleanUpEligibilityChecks();

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_ImportEstablishments_Should_Return_Status200OK()
        {
            // Arrange
            _mockService.Setup(cs => cs.ImportEstablishments(It.IsAny<IEnumerable<EstablishmentRow>>())).Returns(Task.CompletedTask);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());

            var content = Properties.Resources.small_gis;
            var fileName = "test.csv";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            //create FormFile with desired data
            var file = new FormFile(stream, 0, stream.Length, fileName, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };
            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {Admin.EstablishmentFileProcessed}" }) { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.ImportEstablishments(file);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }


        [Test]
        public void Given_ImportEstablishments_Should_Return_Status400BadRequest()
        {
            // Arrange
            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{Admin.CsvfileRequired}" }) { StatusCode = StatusCodes.Status400BadRequest };

            // Act
            var response = _sut.ImportEstablishments(null);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_ImportEstablishments_Should_Return_Status400BadData()
        {
            // Arrange
            var content = "SomeContent";
            var fileName = "test.csv";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            //create FormFile with desired data
            var file = new FormFile(stream, 0, stream.Length, fileName, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };
            var ex = new InvalidDataException("Invalid file content.");
            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {JsonConvert.SerializeObject(new EstablishmentRow())} :- {ex.Message}," }) { StatusCode = StatusCodes.Status400BadRequest };

            // Act
            var response = _sut.ImportEstablishments(file);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_ImportEstablishments_Should_Return_Status400BadDataNoContent()
        {
            // Arrange
            var content = "";
            var fileName = "test.csv";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            //create FormFile with desired data
            var file = new FormFile(stream, 0, stream.Length, fileName, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };
            var ex = new InvalidDataException("Invalid file content.");
            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {JsonConvert.SerializeObject(new EstablishmentRow())} :- {ex.Message}," }) { StatusCode = StatusCodes.Status400BadRequest };

            // Act
            var response = _sut.ImportEstablishments(file);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_HomeOfficeData_Should_Return_Status200OK()
        {
            // Arrange
            _mockService.Setup(cs => cs.ImportHomeOfficeData(It.IsAny<IEnumerable<FreeSchoolMealsHO>>())).Returns(Task.CompletedTask);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());

            var content = Properties.Resources.HO_Data_small; ;
            var fileName = "test.csv";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            //create FormFile with desired data
            var file = new FormFile(stream, 0, stream.Length, fileName, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };
            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {Admin.HomeOfficeFileProcessed}" }) { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.ImportFsmHomeOfficeData(file);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_HomeOfficeData_Should_Return_Status400BadRequest()
        {
            // Arrange
            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{Admin.CsvfileRequired}" }) { StatusCode = StatusCodes.Status400BadRequest };

            // Act
            var response = _sut.ImportFsmHomeOfficeData(null);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_HomeOfficeData_Should_Return_Status400BadData()
        {
            // Arrange
            var content = "SomeContent";
            var fileName = "test.csv";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            //create FormFile with desired data
            var file = new FormFile(stream, 0, stream.Length, fileName, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };
            var ex = new InvalidDataException("String '' was not recognized as a valid DateTime.");
            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {JsonConvert.SerializeObject(new HomeOfficeRow())} :- {ex.Message}," }) { StatusCode = StatusCodes.Status400BadRequest };

            // Act
            var response =  _sut.ImportFsmHomeOfficeData(file);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_HomeOfficeData_Should_Return_Status400BadDataNoContent()
        {
            // Arrange
            var content = "";
            var fileName = "test.csv";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            //create FormFile with desired data
            var file = new FormFile(stream, 0, stream.Length, fileName, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };
            var ex = new InvalidDataException("Invalid file content.");
            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {JsonConvert.SerializeObject(new HomeOfficeRow())} :- {ex.Message}," }) { StatusCode = StatusCodes.Status400BadRequest };

            // Act
            var response = _sut.ImportFsmHomeOfficeData(file);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }


        [Test]
        public void Given_ImportFsmHMRCData_Should_Return_Status200OK()
        {
            // Arrange
            _mockService.Setup(cs => cs.ImportHMRCData(It.IsAny<IEnumerable<FreeSchoolMealsHMRC>>())).Returns(Task.CompletedTask);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());

            var content = Properties.Resources.exampleHMRC; ;
            var fileName = "test.xml";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            //create FormFile with desired data
            var file = new FormFile(stream, 0, stream.Length, fileName, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/xml"
            };
            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {Admin.HMRCFileProcessed}" }) { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.ImportFsmHMRCData(file);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_ImportFsmHMRCData_Should_Return_Status400BadRequest()
        {
            // Arrange
            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{Admin.XmlfileRequired}" }) { StatusCode = StatusCodes.Status400BadRequest };

            // Act
            var response = _sut.ImportFsmHMRCData(null);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_ImportFsmHMRCData_Should_Return_Status400BadData()
        {
            // Arrange
            var content = "SomeContent";
            var fileName = "test.xml";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            //create FormFile with desired data
            var file = new FormFile(stream, 0, stream.Length, fileName, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/xml"
            };
            var ex = new Exception("Data at the root level is invalid. Line 1, position 1.");
            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {JsonConvert.SerializeObject(new FreeSchoolMealsHMRC())} :- {ex.Message}," }) { StatusCode = StatusCodes.Status400BadRequest };

            // Act
            var response = _sut.ImportFsmHMRCData(file);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_ImportFsmHMRCData_Should_Return_Status400BadDataNoContent()
        {
            // Arrange
            var content = Properties.Resources.exampleHMRC_empty;
            var fileName = "test.xml";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            //create FormFile with desired data
            var file = new FormFile(stream, 0, stream.Length, fileName, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/xml"
            };
            var ex = new InvalidDataException("Invalid file no content.");
            var expectedResult = new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {JsonConvert.SerializeObject(new FreeSchoolMealsHMRC())} :- {ex.Message}," }) { StatusCode = StatusCodes.Status400BadRequest };

            // Act
            var response = _sut.ImportFsmHMRCData(file);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }
    }
}