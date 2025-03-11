using AutoFixture;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.Controllers;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace CheckYourEligibility.APIUnitTests
{
    public class EligibilityCheckControllerTests : TestBase.TestBase
    {
        private Mock<IProcessQueueMessagesUseCase> _mockProcessQueueMessagesUseCase;
        private Mock<ICheckEligibilityForFSMUseCase> _mockCheckEligibilityForFsmUseCase;
        private Mock<ICheckEligibilityBulkUseCase> _mockCheckEligibilityBulkUseCase;
        private Mock<IGetBulkUploadProgressUseCase> _mockGetBulkUploadProgressUseCase;
        private Mock<IGetBulkUploadResultsUseCase> _mockGetBulkUploadResultsUseCase;
        private Mock<IGetEligibilityCheckStatusUseCase> _mockGetEligibilityCheckStatusUseCase;
        private Mock<IUpdateEligibilityCheckStatusUseCase> _mockUpdateEligibilityCheckStatusUseCase;
        private Mock<IProcessEligibilityCheckUseCase> _mockProcessEligibilityCheckUseCase;
        private Mock<IGetEligibilityCheckItemUseCase> _mockGetEligibilityCheckItemUseCase;
        
        private Mock<IAudit> _mockAuditService;
        private ILogger<EligibilityCheckController> _mockLogger;
        private IConfigurationRoot _configuration;
        private EligibilityCheckController _sut;

        [SetUp]
        public void Setup()
        {
            _mockProcessQueueMessagesUseCase = new Mock<IProcessQueueMessagesUseCase>(MockBehavior.Strict);
            _mockCheckEligibilityForFsmUseCase = new Mock<ICheckEligibilityForFSMUseCase>(MockBehavior.Strict);
            _mockCheckEligibilityBulkUseCase = new Mock<ICheckEligibilityBulkUseCase>(MockBehavior.Strict);
            _mockGetBulkUploadProgressUseCase = new Mock<IGetBulkUploadProgressUseCase>(MockBehavior.Strict);
            _mockGetBulkUploadResultsUseCase = new Mock<IGetBulkUploadResultsUseCase>(MockBehavior.Strict);
            _mockGetEligibilityCheckStatusUseCase = new Mock<IGetEligibilityCheckStatusUseCase>(MockBehavior.Strict);
            _mockUpdateEligibilityCheckStatusUseCase = new Mock<IUpdateEligibilityCheckStatusUseCase>(MockBehavior.Strict);
            _mockProcessEligibilityCheckUseCase = new Mock<IProcessEligibilityCheckUseCase>(MockBehavior.Strict);
            _mockGetEligibilityCheckItemUseCase = new Mock<IGetEligibilityCheckItemUseCase>(MockBehavior.Strict);
            
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<EligibilityCheckController>>();
            
            var configForBulkUpload = new Dictionary<string, string>
            {
                {"BulkEligibilityCheckLimit", "5"},
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configForBulkUpload)
                .Build();
                
            _sut = new EligibilityCheckController(
                _mockLogger,
                _mockAuditService.Object,
                _configuration,
                _mockProcessQueueMessagesUseCase.Object,
                _mockCheckEligibilityForFsmUseCase.Object,
                _mockCheckEligibilityBulkUseCase.Object,
                _mockGetBulkUploadProgressUseCase.Object,
                _mockGetBulkUploadResultsUseCase.Object,
                _mockGetEligibilityCheckStatusUseCase.Object,
                _mockUpdateEligibilityCheckStatusUseCase.Object,
                _mockProcessEligibilityCheckUseCase.Object,
                _mockGetEligibilityCheckItemUseCase.Object
            );
        }

        [TearDown]
        public void Teardown()
        {
            _mockProcessQueueMessagesUseCase.VerifyAll();
            _mockCheckEligibilityForFsmUseCase.VerifyAll();
            _mockCheckEligibilityBulkUseCase.VerifyAll();
            _mockGetBulkUploadProgressUseCase.VerifyAll();
            _mockGetBulkUploadResultsUseCase.VerifyAll();
            _mockGetEligibilityCheckStatusUseCase.VerifyAll();
            _mockUpdateEligibilityCheckStatusUseCase.VerifyAll();
            _mockProcessEligibilityCheckUseCase.VerifyAll();
            _mockGetEligibilityCheckItemUseCase.VerifyAll();
            _mockAuditService.VerifyAll();
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_processQueueMessagesUseCase_is_null()
        {
            // Arrange
            IProcessQueueMessagesUseCase processQueueMessagesUseCase = null;

            // Act
            Action act = () => new EligibilityCheckController(
                _mockLogger,
                _mockAuditService.Object,
                _configuration,
                processQueueMessagesUseCase,
                _mockCheckEligibilityForFsmUseCase.Object,
                _mockCheckEligibilityBulkUseCase.Object,
                _mockGetBulkUploadProgressUseCase.Object,
                _mockGetBulkUploadResultsUseCase.Object,
                _mockGetEligibilityCheckStatusUseCase.Object,
                _mockUpdateEligibilityCheckStatusUseCase.Object,
                _mockProcessEligibilityCheckUseCase.Object,
                _mockGetEligibilityCheckItemUseCase.Object
            );

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'processQueueMessagesUseCase')");
        }

        [Test]
        public async Task ProcessQueue_returns_bad_request_when_use_case_returns_invalid_result()
        {
            // Arrange
            var queue = _fixture.Create<string>();
            var executionResult = new MessageResponse() {
                Data = "Invalid Request."
            };
            
            _mockProcessQueueMessagesUseCase.Setup(u => u.Execute(queue)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.ProcessQueue(queue);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public async Task ProcessQueue_returns_ok_when_use_case_returns_valid_result()
        {
            // Arrange
            var queue = _fixture.Create<string>();
            var messageResponse = _fixture.Create<MessageResponse>();
            _mockProcessQueueMessagesUseCase.Setup(u => u.Execute(queue)).ReturnsAsync(messageResponse);

            // Act
            var response = await _sut.ProcessQueue(queue);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)response;
            okResult.Value.Should().Be(messageResponse);
        }

        [Test]
        public async Task CheckEligibility_returns_bad_request_when_use_case_returns_invalid_result()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest_Fsm>();
            var executionResult = new UseExecutionResult<CheckEligibilityResponse>();
            executionResult.SetFailure("Validation error");
            
            _mockCheckEligibilityForFsmUseCase.Setup(u => u.Execute(request)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.CheckEligibility(request);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)response;
            ((ErrorResponse)badRequestResult.Value).Errors.First().Title.Should().Be("Validation error");
        }

        [Test]
        public async Task CheckEligibility_returns_accepted_with_response_when_use_case_returns_valid_result()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest_Fsm>();
            var statusResponse = _fixture.Create<CheckEligibilityResponse>();
            var executionResult = new UseExecutionResult<CheckEligibilityResponse>();
            executionResult.SetSuccess(statusResponse);
            
            _mockCheckEligibilityForFsmUseCase.Setup(u => u.Execute(request)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.CheckEligibility(request);

            // Assert
            response.Should().BeOfType<ObjectResult>();
            var objectResult = (ObjectResult)response;
            objectResult.StatusCode.Should().Be(StatusCodes.Status202Accepted);
            objectResult.Value.Should().Be(statusResponse);
        }

        [Test]
        public async Task CheckEligibilityBulk_returns_bad_request_when_use_case_returns_invalid_result()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequestBulk_Fsm>();
            var executionResult = new UseExecutionResult<CheckEligibilityResponseBulk>();
            executionResult.SetFailure("Validation error");
            
            _mockCheckEligibilityBulkUseCase.Setup(u => u.Execute(request, _configuration.GetValue<int>("BulkEligibilityCheckLimit"))).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.CheckEligibilityBulk(request);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)response;
            ((ErrorResponse)badRequestResult.Value).Errors.First().Title.Should().Be("Validation error");
        }

        [Test]
        public async Task CheckEligibilityBulk_returns_accepted_with_response_when_use_case_returns_valid_result()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequestBulk_Fsm>();
            var bulkResponse = _fixture.Create<CheckEligibilityResponseBulk>();
            var executionResult = new UseExecutionResult<CheckEligibilityResponseBulk>();
            executionResult.SetSuccess(bulkResponse);
            
            _mockCheckEligibilityBulkUseCase.Setup(u => u.Execute(request, _configuration.GetValue<int>("BulkEligibilityCheckLimit"))).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.CheckEligibilityBulk(request);

            // Assert
            response.Should().BeOfType<ObjectResult>();
            var objectResult = (ObjectResult)response;
            objectResult.StatusCode.Should().Be(StatusCodes.Status202Accepted);
            objectResult.Value.Should().Be(bulkResponse);
        }

        [Test]
        public async Task BulkUploadProgress_returns_not_found_when_use_case_returns_not_found()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var executionResult = new UseExecutionResult<CheckEligibilityBulkStatusResponse>();
            executionResult.SetNotFound(guid);
            
            _mockGetBulkUploadProgressUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.BulkUploadProgress(guid);

            // Assert
            response.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = (NotFoundObjectResult)response;
            ((ErrorResponse)notFoundResult.Value).Errors.First().Title.Should().Be(guid);
        }

        [Test]
        public async Task BulkUploadProgress_returns_bad_request_when_use_case_returns_invalid_result()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var executionResult = new UseExecutionResult<CheckEligibilityBulkStatusResponse>();
            executionResult.SetFailure("Validation error");
            
            _mockGetBulkUploadProgressUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.BulkUploadProgress(guid);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)response;
            ((ErrorResponse)badRequestResult.Value).Errors.First().Title.Should().Be("Validation error");
        }

        [Test]
        public async Task BulkUploadProgress_returns_ok_with_response_when_use_case_returns_valid_result()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var statusResponse = _fixture.Create<CheckEligibilityBulkStatusResponse>();
            var executionResult = new UseExecutionResult<CheckEligibilityBulkStatusResponse>();
            executionResult.SetSuccess(statusResponse);
            
            _mockGetBulkUploadProgressUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.BulkUploadProgress(guid);

            // Assert
            response.Should().BeOfType<ObjectResult>();
            var objectResult = (ObjectResult)response;
            objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            objectResult.Value.Should().Be(statusResponse);
        }

        [Test]
        public async Task BulkUploadResults_returns_not_found_when_use_case_returns_not_found()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var executionResult = new UseExecutionResult<CheckEligibilityBulkResponse>();
            executionResult.SetNotFound(guid);
            
            _mockGetBulkUploadResultsUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.BulkUploadResults(guid);

            // Assert
            response.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = (NotFoundObjectResult)response;
            ((ErrorResponse)notFoundResult.Value).Errors.First().Title.Should().Be(guid);
        }

        [Test]
        public async Task BulkUploadResults_returns_bad_request_when_use_case_returns_invalid_result()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var executionResult = new UseExecutionResult<CheckEligibilityBulkResponse>();
            executionResult.SetFailure("Validation error");
            
            _mockGetBulkUploadResultsUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.BulkUploadResults(guid);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)response;
            ((ErrorResponse)badRequestResult.Value).Errors.First().Title.Should().Be("Validation error");
        }

        [Test]
        public async Task BulkUploadResults_returns_ok_with_response_when_use_case_returns_valid_result()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var bulkResponse = _fixture.Create<CheckEligibilityBulkResponse>();
            var executionResult = new UseExecutionResult<CheckEligibilityBulkResponse>();
            executionResult.SetSuccess(bulkResponse);
            
            _mockGetBulkUploadResultsUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.BulkUploadResults(guid);

            // Assert
            response.Should().BeOfType<ObjectResult>();
            var objectResult = (ObjectResult)response;
            objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            objectResult.Value.Should().Be(bulkResponse);
        }

        [Test]
        public async Task CheckEligibilityStatus_returns_not_found_when_use_case_returns_not_found()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var executionResult = new UseExecutionResult<CheckEligibilityStatusResponse>();
            executionResult.SetNotFound(guid);
            
            _mockGetEligibilityCheckStatusUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.CheckEligibilityStatus(guid);

            // Assert
            response.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = (NotFoundObjectResult)response;
            ((ErrorResponse)notFoundResult.Value).Errors.First().Title.Should().Be(guid);
        }

        [Test]
        public async Task CheckEligibilityStatus_returns_bad_request_when_use_case_returns_invalid_result()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var executionResult = new UseExecutionResult<CheckEligibilityStatusResponse>();
            executionResult.SetFailure("Validation error");
            
            _mockGetEligibilityCheckStatusUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.CheckEligibilityStatus(guid);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)response;
            ((ErrorResponse)badRequestResult.Value).Errors.First().Title.Should().Be("Validation error");
        }

        [Test]
        public async Task CheckEligibilityStatus_returns_ok_with_response_when_use_case_returns_valid_result()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var statusResponse = _fixture.Create<CheckEligibilityStatusResponse>();
            var executionResult = new UseExecutionResult<CheckEligibilityStatusResponse>();
            executionResult.SetSuccess(statusResponse);
            
            _mockGetEligibilityCheckStatusUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.CheckEligibilityStatus(guid);

            // Assert
            response.Should().BeOfType<ObjectResult>();
            var objectResult = (ObjectResult)response;
            objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            objectResult.Value.Should().Be(statusResponse);
        }

        [Test]
        public async Task EligibilityCheckStatusUpdate_returns_not_found_when_use_case_returns_not_found()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var request = _fixture.Create<EligibilityStatusUpdateRequest>();
            var executionResult = new UseExecutionResult<CheckEligibilityStatusResponse>();
            executionResult.SetNotFound(guid);
            
            _mockUpdateEligibilityCheckStatusUseCase.Setup(u => u.Execute(guid, request)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.EligibilityCheckStatusUpdate(guid, request);

            // Assert
            response.Should().BeOfType<NotFoundObjectResult>();
        }

        [Test]
        public async Task EligibilityCheckStatusUpdate_returns_bad_request_when_use_case_returns_invalid_result()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var request = _fixture.Create<EligibilityStatusUpdateRequest>();
            var executionResult = new UseExecutionResult<CheckEligibilityStatusResponse>();
            executionResult.SetFailure("Validation error");
            
            _mockUpdateEligibilityCheckStatusUseCase.Setup(u => u.Execute(guid, request)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.EligibilityCheckStatusUpdate(guid, request);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)response;
            ((ErrorResponse)badRequestResult.Value).Errors.First().Title.Should().Be("Validation error");
        }

        [Test]
        public async Task EligibilityCheckStatusUpdate_returns_ok_with_response_when_use_case_returns_valid_result()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var request = _fixture.Create<EligibilityStatusUpdateRequest>();
            var statusResponse = _fixture.Create<CheckEligibilityStatusResponse>();
            var executionResult = new UseExecutionResult<CheckEligibilityStatusResponse>();
            executionResult.SetSuccess(statusResponse);
            
            _mockUpdateEligibilityCheckStatusUseCase.Setup(u => u.Execute(guid, request)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.EligibilityCheckStatusUpdate(guid, request);

            // Assert
            response.Should().BeOfType<ObjectResult>();
            var objectResult = (ObjectResult)response;
            objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            objectResult.Value.Should().Be(statusResponse);
        }

        [Test]
        public async Task Process_returns_not_found_when_use_case_returns_not_found()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var executionResult = new UseExecutionResult<CheckEligibilityStatusResponse>();
            executionResult.SetNotFound(guid);
            
            _mockProcessEligibilityCheckUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.Process(guid);

            // Assert
            response.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = (NotFoundObjectResult)response;
            notFoundResult.Value.Equals(new ErrorResponse(){Errors = [new Error(){Title = guid}]});
        }

        [Test]
        public async Task Process_returns_bad_request_when_use_case_returns_invalid_result()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var executionResult = new UseExecutionResult<CheckEligibilityStatusResponse>();
            executionResult.SetFailure("Validation error");
            
            _mockProcessEligibilityCheckUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.Process(guid);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)response;
            ((ErrorResponse)badRequestResult.Value).Errors.First().Title.Should().Be("Validation error");
        }

        [Test]
        public async Task Process_returns_service_unavailable_when_use_case_returns_service_unavailable()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var statusResponse = _fixture.Create<CheckEligibilityStatusResponse>();
            var executionResult = new UseExecutionResult<CheckEligibilityStatusResponse>();
            executionResult.SetServiceUnavailable();
            executionResult.Response = statusResponse;
            
            _mockProcessEligibilityCheckUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.Process(guid);

            // Assert
            response.Should().BeOfType<ObjectResult>();
            var objectResult = (ObjectResult)response;
            objectResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
            objectResult.Value.Should().Be(statusResponse);
        }

        [Test]
        public async Task Process_returns_ok_with_response_when_use_case_returns_valid_result()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var statusResponse = _fixture.Create<CheckEligibilityStatusResponse>();
            var executionResult = new UseExecutionResult<CheckEligibilityStatusResponse>();
            executionResult.SetSuccess(statusResponse);
            
            _mockProcessEligibilityCheckUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.Process(guid);

            // Assert
            response.Should().BeOfType<ObjectResult>();
            var objectResult = (ObjectResult)response;
            objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            objectResult.Value.Should().Be(statusResponse);
        }

        [Test]
        public async Task Process_returns_bad_request_when_ProcessCheckException_is_thrown()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            
            _mockProcessEligibilityCheckUseCase.Setup(u => u.Execute(guid)).ThrowsAsync(new ProcessCheckException());

            // Act
            var response = await _sut.Process(guid);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)response;
            ((ErrorResponse)badRequestResult.Value).Errors.First().Title.Should().Be(guid);
        }

        [Test]
        public async Task EligibilityCheck_returns_not_found_when_use_case_returns_not_found()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var executionResult = new UseExecutionResult<CheckEligibilityItemResponse>();
            executionResult.SetNotFound(guid);
            
            _mockGetEligibilityCheckItemUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.EligibilityCheck(guid);

            // Assert
            response.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = (NotFoundObjectResult)response;
            ((ErrorResponse)notFoundResult.Value).Errors.First().Title.Should().Be(guid);
        }

        [Test]
        public async Task EligibilityCheck_returns_bad_request_when_use_case_returns_invalid_result()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var executionResult = new UseExecutionResult<CheckEligibilityItemResponse>();
            executionResult.SetFailure("Validation error");
            
            _mockGetEligibilityCheckItemUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.EligibilityCheck(guid);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)response;
            ((ErrorResponse)badRequestResult.Value).Errors.First().Title.Should().Be("Validation error");
        }

        [Test]
        public async Task EligibilityCheck_returns_ok_with_response_when_use_case_returns_valid_result()
        {
            // Arrange
            var guid = _fixture.Create<string>();
            var itemResponse = _fixture.Create<CheckEligibilityItemResponse>();
            var executionResult = new UseExecutionResult<CheckEligibilityItemResponse>();
            executionResult.SetSuccess(itemResponse);
            
            _mockGetEligibilityCheckItemUseCase.Setup(u => u.Execute(guid)).ReturnsAsync(executionResult);

            // Act
            var response = await _sut.EligibilityCheck(guid);

            // Assert
            response.Should().BeOfType<ObjectResult>();
            var objectResult = (ObjectResult)response;
            objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            objectResult.Value.Should().Be(itemResponse);
        }
    }
}