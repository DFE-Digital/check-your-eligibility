using AutoFixture;
using Azure;
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
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CheckYourEligibility.APIUnitTests
{
    public class FreeSchoolMealsControllerTests : TestBase.TestBase
    {
        private Mock<IFsmCheckEligibility> _mockCheckService;
        private Mock<IFsmApplication> _mockApplicationService;
        private Mock<IAudit> _mockAuditService;
        private ILogger<FreeSchoolMealsController> _mockLogger;
        private FreeSchoolMealsController _sut;

        [SetUp]
        public void Setup()
        {
            _mockCheckService = new Mock<IFsmCheckEligibility>(MockBehavior.Strict);
            _mockApplicationService = new Mock<IFsmApplication>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<FreeSchoolMealsController>>();
            _sut = new FreeSchoolMealsController(_mockLogger, _mockCheckService.Object, _mockApplicationService.Object, _mockAuditService.Object);
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
            IFsmCheckEligibility checkService = null;
            IFsmApplication applicationService = null;
            IAudit auditService = null;

            // Act
            Action act = () => new FreeSchoolMealsController(_mockLogger, checkService, applicationService, auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'checkService')");
        }

        [Test]
        public void Given_valid_NInumber_ApplicationRequest_Post_Should_Return_Status201Created()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequest>();
            var applicationFsm = _fixture.Create<ApplicationSave>();
            request.Data.ParentNationalInsuranceNumber = "ns738356d";
            request.Data.ParentDateOfBirth = "1970-02-01";
            request.Data.ChildDateOfBirth = "1970-02-01";
            request.Data.ParentNationalAsylumSeekerServiceNumber = string.Empty;
            _mockApplicationService.Setup(cs => cs.PostApplication(request.Data)).ReturnsAsync(applicationFsm);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());

            var expectedResult = new ObjectResult(new ApplicationSaveItemResponse
            {
                Data = applicationFsm,
                Links = new ApplicationResponseLinks
                {
                    get_Application = $"{Domain.Constants.FSMLinks.GetLinkApplication}{applicationFsm.Id}"
                }
            })
            { StatusCode = StatusCodes.Status201Created };

            // Act
            var response = _sut.Application(request);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_Values_Application_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = new ApplicationRequest();

            // Act
            var response = _sut.Application(request);

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestObjectResult));
        }

        [Test]
        public void Given_InValidRequest_Validation_Application_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequest>();
            request.Data.ParentLastName = string.Empty;

            // Act
            var response = _sut.Application(request);

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestObjectResult));
        }

        [Test]
        public void Given_InValidRequest_Values_CheckEligibilityBulk_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = new CheckEligibilityRequestBulk();

            // Act
            var response = _sut.CheckEligibilityBulk(request);

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestObjectResult));
        }

        [Test]
        public void Given_valid_CheckEligibilityBulk_Should_Return_Status202Accepted()
        {
            // Arrange
            var requestItem = _fixture.Create<CheckEligibilityRequestDataFsm>();
            requestItem.NationalInsuranceNumber = "ns738356d";
            requestItem.DateOfBirth = "1970-02-01";
            requestItem.NationalAsylumSeekerServiceNumber = string.Empty;
                        
            _mockCheckService.Setup(cs => cs.PostCheck(It.IsAny<IEnumerable<CheckEligibilityRequestDataFsm>>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _mockAuditService.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");

            // Act
            var response = _sut.CheckEligibilityBulk(new CheckEligibilityRequestBulk { Data = [requestItem] });

            // Assert
            Assert.Pass();
        }

        [Test]
        public void Given_Invalid_CheckEligibilityBulk_Should_Return_BadRequest()
        {
            // Arrange
            var requestItem = _fixture.Create<CheckEligibilityRequestDataFsm>();
            requestItem.NationalInsuranceNumber = "xyz";
            requestItem.DateOfBirth = "1970-02-01";
            requestItem.NationalAsylumSeekerServiceNumber = string.Empty;

            
            // Act
            var response = _sut.CheckEligibilityBulk(new CheckEligibilityRequestBulk { Data = [requestItem] });

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestObjectResult));
        }

        [Test]
        public void Given_valid_NInumber_Request_Post_Should_Return_Status202Accepted()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequestDataFsm>();
            var id = _fixture.Create<string>();
            request.NationalInsuranceNumber = "ns738356d";
            request.DateOfBirth = "1970-02-01";
            request.NationalAsylumSeekerServiceNumber = string.Empty;
            _mockCheckService.Setup(cs => cs.PostCheck(request,null)).ReturnsAsync(new PostCheckResult { Id = id});
            _mockAuditService.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");

            var expectedResult = new ObjectResult(new CheckEligibilityStatusResponse() { Data = new StatusValue() { Status = CheckEligibilityStatus.queuedForProcessing.ToString() } }) { StatusCode = StatusCodes.Status202Accepted };

            // Act
            var response = _sut.CheckEligibility(new CheckEligibilityRequest { Data = request});

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = new CheckEligibilityRequest();

            // Act
            var response = _sut.CheckEligibility(request);

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestObjectResult));
        }

        [Test]
        public void Given_InValidRequest_NI_and_NASS_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest>();
            request.Data.NationalInsuranceNumber = "ns738356d";
            request.Data.DateOfBirth = "1970-02-01";
            request.Data.NationalAsylumSeekerServiceNumber = "789";
            var expectedResult = new BadRequestObjectResult(new MessageResponse { Data = Domain.Constants.ErrorMessages.FSM.NI_and_NASS });

            // Act
            var response = _sut.CheckEligibility(request);
            
            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_NI_or_NASS_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest>();
            request.Data.NationalInsuranceNumber = string.Empty;
            request.Data.DateOfBirth = "1970-02-01";
            request.Data.NationalAsylumSeekerServiceNumber = string.Empty;
            var expectedResult = new BadRequestObjectResult(new MessageResponse { Data = Domain.Constants.ErrorMessages.FSM.NI_or_NASS });

            // Act
            var response = _sut.CheckEligibility(request);
           
            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_NI_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest>();
            request.Data.NationalInsuranceNumber = "123";
            request.Data.DateOfBirth = "1970-02-01";
            request.Data.NationalAsylumSeekerServiceNumber = "";
            var expectedResult = new BadRequestObjectResult(new MessageResponse { Data = Domain.Constants.ErrorMessages.FSM.NI });

            // Act
            var response = _sut.CheckEligibility(request);
            
            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_DOB_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest>();
            request.Data.NationalInsuranceNumber = "ns738356d";
            request.Data.DateOfBirth = "01/02/1970";
            request.Data.NationalAsylumSeekerServiceNumber = "";
            var expectedResult = new BadRequestObjectResult(new MessageResponse { Data = Domain.Constants.ErrorMessages.FSM.DOB });

            // Act
            var response = _sut.CheckEligibility(request);
            
            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }


        [Test]
        public void Given_InValidRequest_LastName_Values_PostFeature_Should_Return_Status400BadRequest()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequest>();
            request.Data.NationalInsuranceNumber = "ns738356d";
            request.Data.DateOfBirth = "1970-02-01";
            request.Data.LastName = string.Empty;
            request.Data.NationalAsylumSeekerServiceNumber = string.Empty;
            var expectedResult = new BadRequestObjectResult(new MessageResponse { Data = Domain.Constants.ErrorMessages.FSM.LastName });
            
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
                { Get_BulkCheck_Results = $"{Domain.Constants.FSMLinks.BulkCheckLink}{guid}{Domain.Constants.FSMLinks.BulkCheckResults}" }
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
                    Get_EligibilityCheck = $"{Domain.Constants.FSMLinks.GetLink}{guid}",
                    Put_EligibilityCheckProcess = $"{Domain.Constants.FSMLinks.ProcessLink}{guid}"
                }
            })
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.EligibilityCheck(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValid_guid_Application_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            _mockApplicationService.Setup(cs => cs.GetApplication(guid)).Returns(Task.FromResult<ApplicationResponse?>(null));
            var expectedResult = new ObjectResult(guid)
            { StatusCode = StatusCodes.Status404NotFound };

            // Act
            var response = _sut.Application(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_Application_Should_Return_StatusOk()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var expectedResponse = _fixture.Create<ApplicationResponse>();
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());
            _mockApplicationService.Setup(cs => cs.GetApplication(guid)).ReturnsAsync(expectedResponse);
            var expectedResult = new ObjectResult(new ApplicationItemResponse
            {
                Data = expectedResponse,
                Links = new ApplicationResponseLinks
                {
                    get_Application = $"{Domain.Constants.FSMLinks.GetLinkApplication}{expectedResponse.Id}"
                }
            })
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.Application(guid);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValid_ApplicationSearch_Should_Return_Status204NoContent()
        {
            // Arrange
            var model = _fixture.Create<ApplicationRequestSearch>();
            _mockApplicationService.Setup(cs => cs.GetApplications(model.Data)).ReturnsAsync(new List<ApplicationResponse>());
            var expectedResult = new NoContentResult();
            
            // Act
            var response = _sut.ApplicationSearch(model);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_ApplicationSearch_Should_Return_StatusOk()
        {
            // Arrange
            var model = _fixture.Create<ApplicationRequestSearch>();
            var expectedResponse = _fixture.CreateMany<ApplicationResponse>();
            _mockApplicationService.Setup(cs => cs.GetApplications(model.Data)).ReturnsAsync(expectedResponse);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());
            var expectedResult = new ObjectResult(new ApplicationSearchResponse { Data = expectedResponse }){ StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.ApplicationSearch(model);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValid_guid_ApplicationStatusUpdate_Should_Return_StatusNotFound()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var request = _fixture.Create<ApplicationStatusUpdateRequest>();
            _mockApplicationService.Setup(cs => cs.UpdateApplicationStatus(guid, request.Data)).Returns(Task.FromResult<ApplicationStatusUpdateResponse?>(null));
            var expectedResult = new NotFoundResult();

            // Act
            var response = _sut.ApplicationStatusUpdate(guid,request);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_Valid_guid_ApplicationStatusUpdate_Should_Return_StatusOk()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var request = _fixture.Create<ApplicationStatusUpdateRequest>();
            var expectedResponse = _fixture.Create<ApplicationStatusUpdateResponse>();
            _mockApplicationService.Setup(cs => cs.UpdateApplicationStatus(guid,request.Data)).ReturnsAsync(expectedResponse);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());
            var expectedResult = new ObjectResult(new ApplicationStatusUpdateResponse
            {
                Data = expectedResponse.Data
            })
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.ApplicationStatusUpdate(guid, request);

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

        [Test]
        public void Given_Valid_guid_CheckEligibilityStatusUpdate_Should_Return_StatusOk()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var request = _fixture.Create<ApplicationStatusUpdateRequest>();
            var expectedResponse = _fixture.Create<ApplicationStatusUpdateResponse>();
            _mockApplicationService.Setup(cs => cs.UpdateApplicationStatus(guid, request.Data)).ReturnsAsync(expectedResponse);
            _mockAuditService.Setup(cs => cs.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync(Guid.NewGuid().ToString());
            var expectedResult = new ObjectResult(new ApplicationStatusUpdateResponse
            {
                Data = expectedResponse.Data
            })
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.ApplicationStatusUpdate(guid, request);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }
    }
}