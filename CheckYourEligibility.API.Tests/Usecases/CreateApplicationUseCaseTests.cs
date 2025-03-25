using AutoFixture;
using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Gateways.Interfaces;
using CheckYourEligibility.API.UseCases;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace CheckYourEligibility.API.Tests.UseCases;

[TestFixture]
public class CreateApplicationUseCaseTests
{
    [SetUp]
    public void Setup()
    {
        _mockApplicationGateway = new Mock<IApplication>(MockBehavior.Strict);
        _mockAuditGateway = new Mock<IAudit>(MockBehavior.Strict);
        _sut = new CreateApplicationUseCase(_mockApplicationGateway.Object, _mockAuditGateway.Object);
        _fixture = new Fixture();


        _validApplicationRequest = _fixture.Build<ApplicationRequest>()
            .With(x => x.Data, _fixture.Build<ApplicationRequestData>()
                .With(d => d.Type, CheckEligibilityType.FreeSchoolMeals)
                .With(d => d.ParentNationalInsuranceNumber, "ns738356d")
                .With(d => d.ParentDateOfBirth, "1970-02-01")
                .With(d => d.ChildDateOfBirth, "1970-02-01")
                .With(d => d.ParentNationalAsylumSeekerServiceNumber, string.Empty)
                .Create())
            .Create();
    }

    [TearDown]
    public void Teardown()
    {
        _mockApplicationGateway.VerifyAll();
        _mockAuditGateway.VerifyAll();
    }

    private Mock<IApplication> _mockApplicationGateway;
    private Mock<IAudit> _mockAuditGateway;
    private CreateApplicationUseCase _sut;

    private Fixture _fixture;

    // valid application request
    private ApplicationRequest _validApplicationRequest;

    [Test]
    public void Execute_Should_Throw_ValidationException_When_Model_Is_Null()
    {
        // Act
        Func<Task> act = async () => await _sut.Execute(null);

        // Assert
        act.Should().ThrowAsync<ValidationException>().WithMessage("Invalid request, data is required");
    }

    [Test]
    public void Execute_Should_Throw_ValidationException_When_ModelData_Is_Null()
    {
        // Arrange
        var model = new ApplicationRequest { Data = null };

        // Act
        Func<Task> act = async () => await _sut.Execute(model);

        // Assert
        act.Should().ThrowAsync<ValidationException>().WithMessage("Invalid request, data is required");
    }

    [Test]
    public void Execute_Should_Throw_ValidationException_When_ModelData_Type_Is_None()
    {
        // Arrange
        var model = _fixture.Build<ApplicationRequest>()
            .With(x => x.Data, _fixture.Build<ApplicationRequestData>()
                .With(d => d.Type, CheckEligibilityType.None)
                .Create())
            .Create();

        // Act
        Func<Task> act = async () => await _sut.Execute(model);

        // Assert
        act.Should().ThrowAsync<ValidationException>().WithMessage("Invalid request, Valid Type is required: None");
    }

    [Test]
    public void Execute_Should_Throw_ValidationException_When_ApplicationRequestValidator_Fails()
    {
        // Arrange
        // Create an application with invalid data that will trigger the ApplicationRequestValidator
        var model = _fixture.Build<ApplicationRequest>()
            .With(x => x.Data, _fixture.Build<ApplicationRequestData>()
                .With(d => d.Type, CheckEligibilityType.FreeSchoolMeals)
                .With(d => d.ParentNationalInsuranceNumber, "invalid-format") // Invalid NI number format
                .Create())
            .Create();

        // Act
        Func<Task> act = async () => await _sut.Execute(model);

        // Assert
        act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Execute_Should_Call_PostApplication_On_ApplicationGateway()
    {
        // Arrange
        var model = _validApplicationRequest;
        var response = _fixture.Create<ApplicationResponse>();

        _mockApplicationGateway.Setup(s => s.PostApplication(model.Data)).ReturnsAsync(response);
        _mockAuditGateway.Setup(a => a.CreateAuditEntry(AuditType.Application, response.Id))
            .ReturnsAsync(_fixture.Create<string>());

        // Act
        var result = await _sut.Execute(model);

        // Assert
        _mockApplicationGateway.Verify(s => s.PostApplication(model.Data), Times.Once);
        result.Data.Should().Be(response);
    }
}