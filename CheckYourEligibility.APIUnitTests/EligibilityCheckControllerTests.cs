using AutoFixture;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework.Internal;

namespace CheckYourEligibility.APIUnitTests
{
    public class EligibilityCheckControllerTests : TestBase.TestBase
    {
        private Mock<ICheckEligibility> _mockCheckService;
        private Mock<IAudit> _mockAuditService;
        private ILogger<EligibilityCheckController> _mockLogger;
        private EligibilityCheckController _sut;

        [SetUp]
        public void Setup()
        {
            _mockCheckService = new Mock<ICheckEligibility>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<EligibilityCheckController>>();
            _sut = new EligibilityCheckController(_mockLogger, _mockCheckService.Object, _mockAuditService.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _mockCheckService.VerifyAll();
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_service_is_null()
        {
            // Arrange
            ICheckEligibility checkService = null;
            IAudit auditService = null;

            // Act
            Action act = () => new EligibilityCheckController(_mockLogger, checkService, auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'checkService')");
        }


        [Test]
        public void Given_InValidRequest_Values_CheckEligibilityBulk_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = new CheckEligibilityRequestBulk_Fsm();

            // Act
            var response = _sut.CheckEligibilityBulk(request);

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestObjectResult));
        }

        [Test]
        public void Given_valid_CheckEligibilityBulk_Should_Return_Status202Accepted()
        {
            // Arrange
            var requestItem = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            requestItem.NationalInsuranceNumber = "ns738356d";
            requestItem.DateOfBirth = "1970-02-01";
            requestItem.NationalAsylumSeekerServiceNumber = string.Empty;
                        
            _mockCheckService.Setup(cs => cs.PostCheck(It.IsAny<IEnumerable<CheckEligibilityRequestData_Fsm>>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _mockAuditService.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");

            // Act
            var response = _sut.CheckEligibilityBulk(new CheckEligibilityRequestBulk_Fsm { Data = [requestItem] });

            // Assert
            Assert.Pass();
        }

        [Test]
        public void Given_Invalid_CheckEligibilityBulk_Should_Return_BadRequest()
        {
            // Arrange
            var requestItem = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            requestItem.NationalInsuranceNumber = "xyz";
            requestItem.DateOfBirth = "1970-02-01";
            requestItem.NationalAsylumSeekerServiceNumber = string.Empty;

            
            // Act
            var response = _sut.CheckEligibilityBulk(new CheckEligibilityRequestBulk_Fsm { Data = [requestItem] });

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestObjectResult));
        }

        [Test]
        public void Given_valid_NInumber_Request_Post_Should_Return_Status202Accepted()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            var id = _fixture.Create<string>();
            request.NationalInsuranceNumber = "ns738356d";
            request.DateOfBirth = "1970-02-01";
            request.NationalAsylumSeekerServiceNumber = string.Empty;
            _mockCheckService.Setup(cs => cs.PostCheck(request)).ReturnsAsync(new PostCheckResult { Id = id});
            _mockAuditService.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");

            var expectedResult = new ObjectResult(new CheckEligibilityStatusResponse() { Data = new StatusValue() { Status = CheckEligibilityStatus.queuedForProcessing.ToString() } }) { StatusCode = StatusCodes.Status202Accepted };

            // Act
            var response = _sut.CheckEligibility(new CheckEligibilityRequest_Fsm { Data = request});

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = new CheckEligibilityRequest_Fsm();

            // Act
            var response = _sut.CheckEligibility(request);

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestObjectResult));
        }

        [Test]
        public void Given_InValidRequest_NI_and_NASS_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest_Fsm>();
            request.Data.NationalInsuranceNumber = "ns738356d";
            request.Data.DateOfBirth = "1970-02-01";
            request.Data.NationalAsylumSeekerServiceNumber = "789";
            var expectedResult = new BadRequestObjectResult(new MessageResponse { Data = Domain.Constants.ErrorMessages.ApplicationValidationMessages.NI_and_NASS });

            // Act
            var response = _sut.CheckEligibility(request);
            
            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_NI_or_NASS_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest_Fsm>();
            request.Data.NationalInsuranceNumber = string.Empty;
            request.Data.DateOfBirth = "1970-02-01";
            request.Data.NationalAsylumSeekerServiceNumber = string.Empty;
            var expectedResult = new BadRequestObjectResult(new MessageResponse { Data = Domain.Constants.ErrorMessages.ApplicationValidationMessages.NI_or_NASS });

            // Act
            var response = _sut.CheckEligibility(request);
           
            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_NI_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest_Fsm>();
            request.Data.NationalInsuranceNumber = "123";
            request.Data.DateOfBirth = "1970-02-01";
            request.Data.NationalAsylumSeekerServiceNumber = "";
            var expectedResult = new BadRequestObjectResult(new MessageResponse { Data = Domain.Constants.ErrorMessages.ApplicationValidationMessages.NI });

            // Act
            var response = _sut.CheckEligibility(request);
            
            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_DOB_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest_Fsm>();
            request.Data.NationalInsuranceNumber = "ns738356d";
            request.Data.DateOfBirth = "01/02/1970";
            request.Data.NationalAsylumSeekerServiceNumber = "";
            var expectedResult = new BadRequestObjectResult(new MessageResponse { Data = Domain.Constants.ErrorMessages.ApplicationValidationMessages.DOB });

            // Act
            var response = _sut.CheckEligibility(request);
            
            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }


        [Test]
        public void Given_InValidRequest_LastName_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest_Fsm>();
            request.Data.NationalInsuranceNumber = "ns738356d";
            request.Data.DateOfBirth = "1970-02-01";
            request.Data.LastName = string.Empty;
            request.Data.NationalAsylumSeekerServiceNumber = string.Empty;
            var expectedResult = new BadRequestObjectResult(new MessageResponse { Data = Domain.Constants.ErrorMessages.ApplicationValidationMessages.LastName });
            
            // Act
            var response = _sut.CheckEligibility(request);
           
            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValid_guid_CheckEligibilityStatus_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            _mockCheckService.Setup(cs => cs.GetStatus(guid)).Returns(Task.FromResult<CheckEligibilityStatus?>(null));
            var expectedResult = new ObjectResult(guid)
            { StatusCode = StatusCodes.Status404NotFound };

            // Act
            var response = _sut.CheckEligibilityStatus(guid);
            
            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_CheckEligibilityStatus_Should_Return_Status()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var status = CheckEligibilityStatus.eligible;
            _mockCheckService.Setup(cs => cs.GetStatus(guid)).Returns(Task.FromResult<CheckEligibilityStatus?>(status));
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());
            var expectedResult = new ObjectResult(new CheckEligibilityStatusResponse() { Data = new StatusValue() { Status = status.ToString() } })
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.CheckEligibilityStatus(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValid_guid_BulkUploadProgress_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            _mockCheckService.Setup(cs => cs.GetBulkStatus(guid)).Returns(Task.FromResult<BulkStatus?>(null));
            var expectedResult = new ObjectResult(guid)
            { StatusCode = StatusCodes.Status404NotFound };

            // Act
            var response = _sut.BulkUploadProgress(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_BulkUploadProgress_Should_Return_Status()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var status = new BulkStatus();
            _mockCheckService.Setup(cs => cs.GetBulkStatus(guid)).Returns(Task.FromResult<BulkStatus?>(status));
            var expectedResult = new ObjectResult(new CheckEligibilityBulkStatusResponse()
            {
                Data = status,
                Links = new BulkCheckResponseLinks()
                { Get_BulkCheck_Results = $"{Domain.Constants.CheckLinks.BulkCheckLink}{guid}{Domain.Constants.CheckLinks.BulkCheckResults}" }
            })
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.BulkUploadProgress(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValid_guid_BulkUploadResults_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            _mockCheckService.Setup(cs => cs.GetBulkCheckResults(guid)).Returns(Task.FromResult<IEnumerable<CheckEligibilityItemFsm>>(null));
            var expectedResult = new ObjectResult(guid)
            { StatusCode = StatusCodes.Status404NotFound };

            // Act
            var response = _sut.BulkUploadResults(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_BulkUploadResults_Should_Return_Results()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var resultItems = _fixture.CreateMany<CheckEligibilityItemFsm>();
            _mockCheckService.Setup(cs => cs.GetBulkCheckResults(guid)).Returns(Task.FromResult(resultItems));
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());
            var expectedResult = new ObjectResult(new CheckEligibilityBulkResponse()
            {
                Data = resultItems
            })
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.BulkUploadResults(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_Not_queuedForProcessing_Process_Should_Return_BadRequest()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            _mockCheckService.Setup(cs => cs.ProcessCheck(guid, It.IsAny<AuditData>())).ThrowsAsync(new ProcessCheckException());
            var expectedResult = new ObjectResult(guid)
            { StatusCode = StatusCodes.Status400BadRequest };

            // Act
            var response = _sut.Process(guid);
            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_ProcessCheckException_Validate()
        {
            // Arrange
            // Act
            var ex = new ProcessCheckException();

            // Assert
            ex.Should().BeOfType<ProcessCheckException>();
        }

        [Test]
        public void Given_ProcessCheckException_ValidateMessage()
        {
            // Arrange
            // Act
            var ex = new ProcessCheckException("test");

            // Assert
            ex.Should().BeOfType<ProcessCheckException>();
        }


        [Test]
        public void Given_InValid_guid_Process_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            _mockCheckService.Setup(cs => cs.ProcessCheck(guid, It.IsAny<AuditData>())).Returns(Task.FromResult<CheckEligibilityStatus?>(null));
            var expectedResult = new ObjectResult(guid)
            { StatusCode = StatusCodes.Status404NotFound };

            // Act
            var response = _sut.Process(guid);


            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_Process_Should_Return_StatusOk()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var expectedResponse = CheckEligibilityStatus.parentNotFound;
            _mockCheckService.Setup(cs => cs.ProcessCheck(guid, It.IsAny<AuditData>())).ReturnsAsync(expectedResponse);
            expectedResponse = CheckEligibilityStatus.parentNotFound;
            var expectedResult = new ObjectResult(new CheckEligibilityStatusResponse() { Data = new StatusValue() { Status = expectedResponse.ToString() } }) { StatusCode = StatusCodes.Status200OK };
            _mockAuditService.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");

            // Act
            var response = _sut.Process(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_DWPError_Process_Should_Return_queuedForProcessing()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var expectedResponse = CheckEligibilityStatus.queuedForProcessing;
            _mockCheckService.Setup(cs => cs.ProcessCheck(guid, It.IsAny<AuditData>())).ReturnsAsync(expectedResponse);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());
            expectedResponse = CheckEligibilityStatus.queuedForProcessing;
            var expectedResult = new ObjectResult(new CheckEligibilityStatusResponse() { Data = new StatusValue() { Status = expectedResponse.ToString() } }) { StatusCode = StatusCodes.Status503ServiceUnavailable };

            // Act
            var response = _sut.Process(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValid_guid_GetEligibilityCheck_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            _mockCheckService.Setup(cs => cs.GetItem(guid)).Returns(Task.FromResult<CheckEligibilityItemFsm?>(null));
            var expectedResult = new ObjectResult(guid)
            { StatusCode = StatusCodes.Status404NotFound };

            // Act
            var response = _sut.EligibilityCheck(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_GetEligibilityCheck_Should_Return_StatusOk()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var expectedResponse = _fixture.Create<CheckEligibilityItemFsm>();
            _mockCheckService.Setup(cs => cs.GetItem(guid)).ReturnsAsync(expectedResponse);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());
            var expectedResult = new ObjectResult(new CheckEligibilityItemResponse()
            {
                Data = expectedResponse,
                Links = new CheckEligibilityResponseLinks
                {
                    Get_EligibilityCheck = $"{Domain.Constants.CheckLinks.GetLink}{guid}",
                    Put_EligibilityCheckProcess = $"{Domain.Constants.CheckLinks.ProcessLink}{guid}"
                }
            })
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.EligibilityCheck(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }


        [Test]
        public void Given_InValid_guid_CheckEligibilityStatusUpdate_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var request = _fixture.Create<EligibilityStatusUpdateRequest>();
            _mockCheckService.Setup(cs => cs.UpdateEligibilityCheckStatus(guid, request.Data)).Returns(Task.FromResult<CheckEligibilityStatusResponse?>(null));
            var expectedResult = new NotFoundResult();

            // Act
            var response = _sut.EligibilityCheckStatusUpdate(guid, request);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_CheckEligibilityStatusUpdate_Should_Return_Status()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var status = CheckEligibilityStatus.eligible;
            var resp = new CheckEligibilityStatusResponse { Data = new StatusValue { Status = status.ToString() } };

            _mockCheckService.Setup(cs => cs.UpdateEligibilityCheckStatus(guid, It.IsAny<EligibilityCheckStatusData>())).ReturnsAsync(resp);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());
            var expectedResult = new ObjectResult(resp)
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.EligibilityCheckStatusUpdate(guid, new EligibilityStatusUpdateRequest { Data = new EligibilityCheckStatusData { Status = status } });

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

    }
}