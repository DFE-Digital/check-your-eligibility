using AutoFixture;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class CheckEligibilityBulkUseCaseTests : TestBase.TestBase
    {
        private Mock<ICheckEligibility> _mockCheckService;
        private Mock<IAudit> _mockAuditService;
        private Mock<ILogger<CheckEligibilityBulkUseCase>> _mockLogger;
        private CheckEligibilityBulkUseCase _sut;
        private Fixture _fixture;
        private int _recordCountLimit;

        [SetUp]
        public void Setup()
        {
            _mockCheckService = new Mock<ICheckEligibility>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<CheckEligibilityBulkUseCase>>(MockBehavior.Loose);
            _sut = new CheckEligibilityBulkUseCase(_mockCheckService.Object, _mockAuditService.Object, _mockLogger.Object);
            _fixture = new Fixture();
            _recordCountLimit = 100;
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
            Action act = () => new CheckEligibilityBulkUseCase(checkService, auditService, logger);

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
            Action act = () => new CheckEligibilityBulkUseCase(checkService, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'auditService')");
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_logger_is_null()
        {
            // Arrange
            var checkService = _mockCheckService.Object;
            var auditService = _mockAuditService.Object;
            ILogger<CheckEligibilityBulkUseCase> logger = null;

            // Act
            Action act = () => new CheckEligibilityBulkUseCase(checkService, auditService, logger);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'logger')");
        }

        [Test]
        public async Task Execute_returns_failure_when_model_is_null()
        {
            // Arrange
            CheckEligibilityRequestBulk_Fsm model = null;

            // Act
            var result = await _sut.Execute(model, _recordCountLimit);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ValidationErrors.Should().Be("Invalid Request, data is required.");
        }

        [Test]
        public async Task Execute_returns_failure_when_model_data_is_null()
        {
            // Arrange
            var model = new CheckEligibilityRequestBulk_Fsm { Data = null };

            // Act
            var result = await _sut.Execute(model, _recordCountLimit);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ValidationErrors.Should().Be("Invalid Request, data is required.");
        }

        [Test]
        public async Task Execute_returns_failure_when_record_count_exceeds_limit()
        {
            // Arrange
            var limit = 5;
            var data = _fixture.CreateMany<CheckEligibilityRequestData_Fsm>(limit + 1).ToList();
            var model = new CheckEligibilityRequestBulk_Fsm { Data = data };

            // Act
            var result = await _sut.Execute(model, limit);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ValidationErrors.Should().Be($"Invalid Request, data limit of {limit} exceeded, {data.Count} records.");
        }

        [Test]
        public async Task Execute_returns_failure_when_validation_errors_exist()
        {
            // Arrange
            // Create a request with invalid data that will fail validation
            var data = new List<CheckEligibilityRequestData_Fsm>
            {
                new CheckEligibilityRequestData_Fsm() // Empty properties will fail validation
                {
                    DateOfBirth = "1990-01-01"
                }
            };
            var model = new CheckEligibilityRequestBulk_Fsm { Data = data };

            // Act
            var result = await _sut.Execute(model, _recordCountLimit);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ValidationErrors.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task Execute_calls_services_with_correct_parameters_when_valid()
        {
            // Arrange
            var data = new List<CheckEligibilityRequestData_Fsm>
            {
                new CheckEligibilityRequestData_Fsm
                {
                    LastName = "Smith",
                    DateOfBirth = "1990-01-01",
                    NationalInsuranceNumber = "AB123456C"
                }
            };
            var model = new CheckEligibilityRequestBulk_Fsm { Data = data };
            var auditData = _fixture.Create<AuditData>();

            _mockCheckService.Setup(s => s.PostCheck(It.IsAny<IEnumerable<CheckEligibilityRequestData_Fsm>>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockAuditService.Setup(a => a.AuditDataGet(AuditType.BulkCheck, It.IsAny<string>()))
                .Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData))
                .ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(model, _recordCountLimit);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Response.Should().NotBeNull();
            result.Response.Data.Status.Should().Be(Messages.Processing);
            result.Response.Links.Should().NotBeNull();
            result.Response.Links.Get_Progress_Check.Should().Contain(CheckLinks.BulkCheckProgress);
            result.Response.Links.Get_BulkCheck_Results.Should().Contain(CheckLinks.BulkCheckResults);
            
            _mockCheckService.Verify(s => s.PostCheck(It.IsAny<IEnumerable<CheckEligibilityRequestData_Fsm>>(), It.IsAny<string>()), Times.Once);
            _mockAuditService.Verify(a => a.AuditAdd(auditData), Times.Once);
        }

        [Test]
        public async Task Execute_should_not_call_AuditAdd_when_AuditData_is_null()
        {
            // Arrange
            var data = new List<CheckEligibilityRequestData_Fsm>
            {
                new CheckEligibilityRequestData_Fsm
                {
                    LastName = "Smith",
                    DateOfBirth = "1990-01-01",
                    NationalInsuranceNumber = "AB123456C"
                }
            };
            var model = new CheckEligibilityRequestBulk_Fsm { Data = data };

            _mockCheckService.Setup(s => s.PostCheck(It.IsAny<IEnumerable<CheckEligibilityRequestData_Fsm>>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockAuditService.Setup(a => a.AuditDataGet(AuditType.BulkCheck, It.IsAny<string>()))
                .Returns((AuditData)null);

            // Act
            var result = await _sut.Execute(model, _recordCountLimit);

            // Assert
            result.IsValid.Should().BeTrue();
            
            _mockAuditService.Verify(a => a.AuditAdd(It.IsAny<AuditData>()), Times.Never);
        }

        [Test]
        public async Task Execute_converts_national_insurance_number_to_uppercase()
        {
            // Arrange
            var nino = "ab123456c";
            var data = new List<CheckEligibilityRequestData_Fsm>
            {
                new CheckEligibilityRequestData_Fsm
                {
                    LastName = "Smith",
                    DateOfBirth = "1990-01-01",
                    NationalInsuranceNumber = nino
                }
            };
            var model = new CheckEligibilityRequestBulk_Fsm { Data = data };

            _mockCheckService.Setup(s => s.PostCheck(It.Is<IEnumerable<CheckEligibilityRequestData_Fsm>>(
                d => d.First().NationalInsuranceNumber == nino.ToUpper()), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockAuditService.Setup(a => a.AuditDataGet(AuditType.BulkCheck, It.IsAny<string>()))
                .Returns((AuditData)null);

            // Act
            await _sut.Execute(model, _recordCountLimit);

            // Assert
            _mockCheckService.Verify(s => s.PostCheck(It.Is<IEnumerable<CheckEligibilityRequestData_Fsm>>(
                d => d.First().NationalInsuranceNumber == "AB123456C"), It.IsAny<string>()), Times.Once);
        }
    }
}