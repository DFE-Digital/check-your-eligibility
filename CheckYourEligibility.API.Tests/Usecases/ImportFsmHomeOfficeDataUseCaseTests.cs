// filepath: /c:/Projects/check-your-eligibility/CheckYourEligibility.API.Tests/UseCases/ImportFsmHomeOfficeDataUseCaseTest.cs
using AutoFixture;
using CheckYourEligibility.API.Domain;
using CheckYourEligibility.API.Gateways.Interfaces;
using CheckYourEligibility.API.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using CheckYourEligibility.API.Boundary.Requests;

namespace CheckYourEligibility.API.Tests.UseCases
{
    [TestFixture]
    public class ImportFsmHomeOfficeDataUseCaseTests : TestBase.TestBase
    {
        private Mock<IAdministration> _mockGateway;
        private Mock<IAudit> _mockAuditGateway;
        private Mock<ILogger<ImportFsmHomeOfficeDataUseCase>> _mockLogger;
        private ImportFsmHomeOfficeDataUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockGateway = new Mock<IAdministration>(MockBehavior.Strict);
            _mockAuditGateway = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<ImportFsmHomeOfficeDataUseCase>>(MockBehavior.Loose);
            _sut = new ImportFsmHomeOfficeDataUseCase(_mockGateway.Object, _mockAuditGateway.Object, _mockLogger.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockGateway.VerifyAll();
            _mockAuditGateway.VerifyAll();
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

            _mockGateway.Setup(s => s.ImportHomeOfficeData(It.IsAny<List<FreeSchoolMealsHO>>())).Returns(Task.CompletedTask);
            _mockAuditGateway.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Administration, string.Empty)).ReturnsAsync(_fixture.Create<string>());

            // Act
            await _sut.Execute(fileMock.Object);

            // Assert
            _mockGateway.Verify(s => s.ImportHomeOfficeData(It.IsAny<List<FreeSchoolMealsHO>>()), Times.Once);
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