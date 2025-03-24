using AutoFixture;
using CheckYourEligibility.API.Domain;
using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Gateways.Interfaces;
using CheckYourEligibility.API.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;

namespace CheckYourEligibility.API.Tests.UseCases
{
    [TestFixture]
    public class ImportFsmHMRCDataUseCaseTests : TestBase.TestBase
    {
        private Mock<IAdministration> _mockGateway;
        private Mock<IAudit> _mockAuditGateway;
        private Mock<ILogger<ImportFsmHMRCDataUseCase>> _mockLogger;
        private ImportFsmHMRCDataUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockGateway = new Mock<IAdministration>(MockBehavior.Strict);
            _mockAuditGateway = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<ImportFsmHMRCDataUseCase>>(MockBehavior.Loose);
            _sut = new ImportFsmHMRCDataUseCase(_mockGateway.Object, _mockAuditGateway.Object, _mockLogger.Object);
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
            act.Should().ThrowExactlyAsync<InvalidDataException>().WithMessage("XML file required.");
        }

        [Test]
        public void Execute_Should_Throw_InvalidDataException_When_File_Is_Not_XML()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.ContentType).Returns("text/plain");

            // Act
            Func<Task> act = async () => await _sut.Execute(fileMock.Object);

            // Assert
            act.Should().ThrowExactlyAsync<InvalidDataException>().WithMessage("XML file required.");
        }

        [Test]
        public async Task Execute_Should_Process_XML_File_And_Call_ImportHMRCData()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.ContentType).Returns("text/xml");
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Properties.Resources.exampleHMRC)));

            _mockGateway.Setup(s => s.ImportHMRCData(It.IsAny<List<FreeSchoolMealsHMRC>>())).Returns(Task.CompletedTask);
            _mockAuditGateway.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Administration, string.Empty)).ReturnsAsync(_fixture.Create<string>());

            // Act
            await _sut.Execute(fileMock.Object);

            // Assert
            _mockGateway.Verify(s => s.ImportHMRCData(It.Is<List<FreeSchoolMealsHMRC>>(list => list.Count == 2 && list[0].FreeSchoolMealsHMRCID == "AB123456C" && list[1].FreeSchoolMealsHMRCID == "AC123456D")), Times.Once);
        }

        [Test]
        public void Execute_Should_Throw_InvalidDataException_When_XML_File_Has_No_Content()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.ContentType).Returns("text/xml");
            fileMock.Setup(f => f.FileName).Returns("test.xml");

            // Create XML without EligiblePersons nodes
            var emptyXml = "<Root><SomeOtherElement>data</SomeOtherElement></Root>";
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(emptyXml)));

            // Act
            Func<Task> act = async () => await _sut.Execute(fileMock.Object);

            // Assert
            act.Should().ThrowExactlyAsync<InvalidDataException>()
                .WithMessage("Invalid file no content.");
        }
    }
}