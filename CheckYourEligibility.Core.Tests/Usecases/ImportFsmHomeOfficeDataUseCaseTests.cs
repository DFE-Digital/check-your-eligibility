using System.Text;
using CheckYourEligibility.Core.Domain;
using CheckYourEligibility.Core.Gateways.Interfaces;
using CheckYourEligibility.Core.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace CheckYourEligibility.Core.Tests.UseCases;

[TestFixture]
public class ImportFsmHomeOfficeDataUseCaseTests : TestBase
{
    [SetUp]
    public void Setup()
    {
        _mockGateway = new Mock<IAdministration>(MockBehavior.Strict);
        _mockAuditGateway = new Mock<IAudit>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<ImportFsmHomeOfficeDataUseCase>>(MockBehavior.Loose);
        _sut = new ImportFsmHomeOfficeDataUseCase(_mockGateway.Object, _mockAuditGateway.Object, _mockLogger.Object);
    }

    [TearDown]
    public void Teardown()
    {
        _mockGateway.VerifyAll();
        _mockAuditGateway.VerifyAll();
    }

    private Mock<IAdministration> _mockGateway;
    private Mock<IAudit> _mockAuditGateway;
    private Mock<ILogger<ImportFsmHomeOfficeDataUseCase>> _mockLogger;
    private ImportFsmHomeOfficeDataUseCase _sut;

    [Test]
    public async Task Execute_Should_Throw_InvalidDataException_When_File_Is_Null()
    {
        // Arrange
        IFormFile file = null;

        // Act
        var act = async () => await _sut.Execute(file);

        // Assert
        await act.Should().ThrowExactlyAsync<InvalidDataException>().WithMessage("Csv data file is required.");
    }

    [Test]
    public async Task Execute_Should_Throw_InvalidDataException_When_File_Is_Not_CSV()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.ContentType).Returns("text/plain");

        // Act
        var act = async () => await _sut.Execute(fileMock.Object);

        // Assert
        await act.Should().ThrowExactlyAsync<InvalidDataException>().WithMessage("Csv data file is required.");
    }

    [Test]
    public async Task Execute_Should_Process_CSV_File_And_Call_ImportHomeOfficeData()
    {
        // Arrange
        const string csv =
            "Type,AS Ref,Port Ref,Given Name,Family Name,DOB,Group Size,No of 4-17 year old in Group\r\n" +
            "Asylum,12/34/56789,MST/9011357,Alice,Smith,19900131,4,2";
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.ContentType).Returns("text/csv");
        fileMock.Setup(f => f.OpenReadStream())
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(csv)));

        List<FreeSchoolMealsHO> importedData = null!;
        _mockGateway.Setup(s => s.ImportHomeOfficeData(It.IsAny<List<FreeSchoolMealsHO>>()))
            .Callback<IEnumerable<FreeSchoolMealsHO>>(data => importedData = data.ToList())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Execute(fileMock.Object);

        // Assert
        importedData.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                FirstName = "Alice",
                LastName = "Smith",
                DateOfBirth = new DateTime(1990, 1, 31),
                NASS = "123456789",
                PortReferenceNumber = "MST/9011357"
            });
    }

    [Test]
    public async Task Execute_Should_Throw_InvalidDataException_When_CSV_File_Has_No_Content()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.ContentType).Returns("text/csv");
        fileMock.Setup(f => f.FileName).Returns("test.csv");

        // Create empty CSV file
        var emptyCsv =
            "Type,AS Ref,Port Ref,Given Name,Family Name,DOB,Group Size,No of 4-17 year old in Group";
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes(emptyCsv)));

        // Act
        var act = async () => await _sut.Execute(fileMock.Object);

        // Assert
        await act.Should().ThrowExactlyAsync<InvalidDataException>();
    }

    [TestCase("")]
    [TestCase("12345678")]
    [TestCase("12/34/5678")]
    public async Task Execute_Should_Reject_Invalid_AsylumSeekerReferenceNumber(string asylumSeekerReferenceNumber)
    {
        // Arrange
        var csv =
            "Type,AS Ref,Port Ref,Given Name,Family Name,DOB,Group Size,No of 4-17 year old in Group\r\n" +
            $"Asylum,{asylumSeekerReferenceNumber},TN4/8689288,Alice,Smith,19900131,4,2";
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.ContentType).Returns("text/csv");
        fileMock.Setup(f => f.FileName).Returns("test.csv");
        fileMock.Setup(f => f.OpenReadStream())
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(csv)));

        // Act
        var act = async () => await _sut.Execute(fileMock.Object);

        // Assert
        await act.Should().ThrowExactlyAsync<InvalidDataException>()
            .WithMessage("*Asylum seeker reference number must contain nine digits*");
    }

    [Test]
    public async Task Execute_Should_Reject_CSV_Without_A_Required_Header()
    {
        // Arrange
        const string csv =
            "Type,AS Ref,Port Ref,Given Name,DOB,Group Size,No of 4-17 year old in Group\r\n" +
            "Asylum,12/34/56789,MST/9011357,Alice,19900131,4,2";
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.ContentType).Returns("text/csv");
        fileMock.Setup(f => f.FileName).Returns("test.csv");
        fileMock.Setup(f => f.OpenReadStream())
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(csv)));

        // Act
        var act = async () => await _sut.Execute(fileMock.Object);

        // Assert
        await act.Should().ThrowExactlyAsync<InvalidDataException>()
            .WithMessage("*Family Name*");
    }
}