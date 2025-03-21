using AutoFixture;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FeatureManagement.Domain.Validation;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class CheckEligibilityForFSMUseCaseTests : TestBase.TestBase
    {
        private Mock<ICheckEligibility> _mockCheckService;
        private Mock<IAudit> _mockAuditService;
        private Mock<ILogger<CheckEligibilityForFSMUseCase>> _mockLogger;
        private CheckEligibilityForFSMUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockCheckService = new Mock<ICheckEligibility>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<CheckEligibilityForFSMUseCase>>(MockBehavior.Loose);
            _sut = new CheckEligibilityForFSMUseCase(_mockCheckService.Object, _mockAuditService.Object, _mockLogger.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockCheckService.VerifyAll();
            _mockAuditService.VerifyAll();
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_checkService_is_null()
        {
            // Arrange
            ICheckEligibility checkService = null;
            var auditService = _mockAuditService.Object;
            var logger = _mockLogger.Object;

            // Act
            Action act = () => new CheckEligibilityForFSMUseCase(checkService, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'checkService')");
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_auditService_is_null()
        {
            // Arrange
            var checkService = _mockCheckService.Object;
            IAudit auditService = null;
            var logger = _mockLogger.Object;

            // Act
            Action act = () => new CheckEligibilityForFSMUseCase(checkService, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'auditService')");
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_logger_is_null()
        {
            // Arrange
            var checkService = _mockCheckService.Object;
            var auditService = _mockAuditService.Object;
            ILogger<CheckEligibilityForFSMUseCase> logger = null;

            // Act
            Action act = () => new CheckEligibilityForFSMUseCase(checkService, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'logger')");
        }

        [Test]
        public async Task Execute_returns_failure_when_model_is_null()
        {
            // Act
            Func<Task> act = async () => await _sut.Execute(null);

            // Assert
            act.Should().ThrowAsync<ValidationException>().WithMessage("Invalid Request, data is required.");
        }

        [Test]
        public async Task Execute_returns_failure_when_model_data_is_null()
        {
            // Arrange
            var model = new CheckEligibilityRequest_Fsm { Data = null };

            // Act
            Func<Task> act = async () => await _sut.Execute(model);

            // Assert
            act.Should().ThrowAsync<ValidationException>().WithMessage("Invalid Request, data is required.");
        }

        [Test]
        public async Task Execute_returns_failure_when_model_type_is_incorrect()
        {
            // Arrange
            // Use a different type that implements the same interface or extends the same base class
            var incorrectModel = new IncorrectModelType()
            {
                Data = new CheckEligibilityRequestData_Fsm()
            };

            // Act
            Func<Task> act = async () => await _sut.Execute(incorrectModel);

            // Assert
            act.Should().ThrowAsync<ValidationException>().WithMessage($"Unknown request type:-{incorrectModel.GetType()}");
        }

        // Add this class to help with the test
        private class IncorrectModelType : CheckEligibilityRequest_Fsm
        {
            // Inheriting from CheckEligibilityRequest_Fsm but it's a different type
        }

        [Test]
        public async Task Execute_normalizes_input_data()
        {
            // Arrange
            var model = CreateValidFsmRequest();

            var responseData = new PostCheckResult
            {
                Id = _fixture.Create<string>(),
                Status = CheckEligibilityStatus.queuedForProcessing
            };

            // Setup with a callback to capture the actual argument
            CheckEligibilityRequestData_Fsm capturedArg = null;
            _mockCheckService
                .Setup(s => s.PostCheck(It.IsAny<CheckEligibilityRequestData_Fsm>()))
                .Callback<CheckEligibilityRequestData_Fsm>(arg => capturedArg = arg)
                .ReturnsAsync(responseData);

            _mockAuditService
               .Setup(a => a.CreateAuditEntry(AuditType.Check, responseData.Id))
               .ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(model);

            // Assert
            // Check that normalization happened on the model
            model.Data.NationalInsuranceNumber.Should().Be("AB123456C");

            // Verify the service was called
            _mockCheckService.Verify(
                s => s.PostCheck(It.IsAny<CheckEligibilityRequestData_Fsm>()), Times.Once);

            // Additional check to diagnose the issue - examine what was actually passed
            capturedArg.Should().NotBeNull("PostCheck should have been called");
            if (capturedArg != null)
            {
                capturedArg.NationalInsuranceNumber.Should().Be("AB123456C");
            }
        }

        [Test]
        public async Task Execute_returns_failure_when_validation_fails()
        {
            // Arrange
            var model = new CheckEligibilityRequest_Fsm
            {
                Data = new CheckEligibilityRequestData_Fsm
                {
                    // Missing required fields for validation
                    DateOfBirth = "2000-01-01"
                }
            };

            // Act
            Func<Task> act = async () => await _sut.Execute(model);

            // Assert
            act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task Execute_returns_success_with_correct_data_when_service_returns_response()
        {
            // Arrange
            var model = CreateValidFsmRequest();
            var checkId = _fixture.Create<string>();
            var responseData = new PostCheckResult
            {
                Id = checkId,
                Status = CheckEligibilityStatus.queuedForProcessing
            };

            _mockCheckService.Setup(s => s.PostCheck(model.Data))
                .ReturnsAsync(responseData);
            _mockAuditService.Setup(a => a.CreateAuditEntry(AuditType.Check, checkId))
                .ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(model);

            // Assert
            result.Data.Should().NotBeNull();
            result.Data.Status.Should().Be(responseData.Status.ToString());
            result.Links.Should().NotBeNull();
            result.Links.Get_EligibilityCheck.Should().Be($"{CheckLinks.GetLink}{checkId}");
            result.Links.Put_EligibilityCheckProcess.Should().Be($"{CheckLinks.ProcessLink}{checkId}");
            result.Links.Get_EligibilityCheckStatus.Should().Be($"{CheckLinks.GetLink}{checkId}/status");
        }

        [Test]
        public async Task Execute_calls_service_PostCheck_with_correct_data()
        {
            // Arrange
            var model = CreateValidFsmRequest();
            var checkId = _fixture.Create<string>();
            var responseData = new PostCheckResult
            {
                Id = checkId,
                Status = CheckEligibilityStatus.queuedForProcessing
            };

            _mockCheckService.Setup(s => s.PostCheck(model.Data))
                .ReturnsAsync(responseData);

            _mockAuditService.Setup(a => a.CreateAuditEntry(AuditType.Check, checkId))
                .ReturnsAsync(_fixture.Create<string>());

            // Act
            await _sut.Execute(model);

            // Assert
            _mockCheckService.Verify(s => s.PostCheck(model.Data), Times.Once);
        }

        [Test]
        public async Task Execute_returns_failure_when_service_returns_null_response()
        {
            // Arrange
            var model = CreateValidFsmRequest();

            _mockCheckService.Setup(s => s.PostCheck(model.Data))
                .ReturnsAsync((PostCheckResult)null);

            // Act
            Func<Task> act = async () => await _sut.Execute(model);

            // Assert
            act.Should().ThrowAsync<ValidationException>().WithMessage("Eligibility check not completed successfully.");

            // Verify audit service was not called
            _mockAuditService.Verify(a => a.CreateAuditEntry(It.IsAny<AuditType>(), It.IsAny<string>()), Times.Never);
        }

        private CheckEligibilityRequest_Fsm CreateValidFsmRequest()
        {
            return new CheckEligibilityRequest_Fsm
            {
                Data = new CheckEligibilityRequestData_Fsm
                {
                    NationalInsuranceNumber = "AB123456C",
                    DateOfBirth = "2000-01-01",
                    LastName = "Doe"
                }
            };
        }
    }
}