using AutoFixture;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using FluentValidation;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class CreateApplicationUseCaseTests
    {
        private Mock<IApplication> _mockApplicationService;
        private Mock<IAudit> _mockAuditService;
        private CreateApplicationUseCase _sut;
        private Fixture _fixture;
        // valid application request
        private ApplicationRequest _validApplicationRequest;

        [SetUp]
        public void Setup()
        {
            _mockApplicationService = new Mock<IApplication>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new CreateApplicationUseCase(_mockApplicationService.Object, _mockAuditService.Object);
            _fixture = new Fixture();


            _validApplicationRequest = _fixture.Build<ApplicationRequest>()
                .With(x => x.Data, _fixture.Build<ApplicationRequestData>()
                    .With(d => d.Type, Domain.Enums.CheckEligibilityType.FreeSchoolMeals)
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
            _mockApplicationService.VerifyAll();
            _mockAuditService.VerifyAll();
        }

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
                                                        .With(d => d.Type, Domain.Enums.CheckEligibilityType.None)
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
                                                        .With(d => d.Type, Domain.Enums.CheckEligibilityType.FreeSchoolMeals)
                                                        .With(d => d.ParentNationalInsuranceNumber, "invalid-format") // Invalid NI number format
                                                        .Create())
                                .Create();

            // Act
            Func<Task> act = async () => await _sut.Execute(model);

            // Assert
            act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task Execute_Should_Call_PostApplication_On_ApplicationService()
        {
            // Arrange
            var model = _validApplicationRequest;
            var response = _fixture.Create<ApplicationResponse>();

            _mockApplicationService.Setup(s => s.PostApplication(model.Data)).ReturnsAsync(response);
            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.Application, response.Id)).Returns((AuditData)null);

            // Act
            var result = await _sut.Execute(model);

            // Assert
            _mockApplicationService.Verify(s => s.PostApplication(model.Data), Times.Once);
            result.Data.Should().Be(response);
        }

        [Test]
        public async Task Execute_Should_Call_AuditAdd_When_AuditData_Is_Not_Null()
        {
            // Arrange
            var model = _validApplicationRequest;
            var response = _fixture.Create<ApplicationResponse>();
            var auditData = _fixture.Create<AuditData>();
            _mockApplicationService.Setup(s => s.PostApplication(model.Data)).ReturnsAsync(response);
            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.Application, response.Id)).Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData)).ReturnsAsync(_fixture.Create<string>());

            // Act
            await _sut.Execute(model);

            // Assert
            _mockAuditService.Verify(a => a.AuditAdd(auditData), Times.Once);
        }

        [Test]
        public async Task Execute_Should_Not_Call_AuditAdd_When_AuditData_Is_Null()
        {
            // Arrange
            var model = _validApplicationRequest;
            var response = _fixture.Create<ApplicationResponse>();
            _mockApplicationService.Setup(s => s.PostApplication(model.Data)).ReturnsAsync(response);
            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.Application, response.Id)).Returns((AuditData)null);

            // Act
            await _sut.Execute(model);

            // Assert
            _mockAuditService.Verify(a => a.AuditAdd(It.IsAny<AuditData>()), Times.Never);
        }
    }
}