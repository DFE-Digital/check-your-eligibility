// Ignore Spelling: Levenshtein

using AutoFixture;
using AutoMapper;
using Azure.Core;
using Azure.Storage.Queues;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Requests.DWP;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services;
using CheckYourEligibility.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetTopologySuite.Index.HPRtree;
using Newtonsoft.Json;
using System;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CheckYourEligibility.ServiceUnitTests
{


    public class FsmCheckEligibilityServiceTests : TestBase.TestBase
    {
        private IEligibilityCheckContext _fakeInMemoryDb;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private CheckEligibilityService _sut;
        private Mock<IDwpService> _moqDwpService;
        private Mock<IAudit> _moqAudit;
        private HashService _hashService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EligibilityCheckContext>()
            .UseInMemoryDatabase(databaseName: "FakeInMemoryDb")
            .Options;

            _fakeInMemoryDb = new EligibilityCheckContext(options);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();
            var configForSmsApi = new Dictionary<string, string>
            {
                {"QueueFsmCheckStandard", "notSet"},
                {"QueueFsmCheckBulk", "notSet"},
                {"HashCheckDays", "7"},

            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configForSmsApi)
                .Build();
            var webJobsConnection = "DefaultEndpointsProtocol=https;AccountName=none;AccountKey=none;EndpointSuffix=core.windows.net";
           
            _moqDwpService = new Mock<IDwpService>(MockBehavior.Strict);
            _moqAudit = new Mock<IAudit>(MockBehavior.Strict);
            _hashService = new HashService(new NullLoggerFactory(), _fakeInMemoryDb, _configuration, _moqAudit.Object);


            _sut = new CheckEligibilityService(new NullLoggerFactory(), _fakeInMemoryDb, _mapper, new QueueServiceClient(webJobsConnection),
                _configuration, _moqDwpService.Object, _moqAudit.Object, _hashService);

        }

        [TearDown]
        public void Teardown()
        {
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_service_is_null()
        {
            // Arrange
            // Act
            Action act = () => new CheckEligibilityService(new NullLoggerFactory(), null, _mapper, null, null,null,null,null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'dbContext')");
        }

        [Test]
        public async Task Given_PostCheck_ExceptionRaised()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            request.DateOfBirth = "1970-02-01";
            request.NationalAsylumSeekerServiceNumber = null;

            var db = new Mock<IEligibilityCheckContext>(MockBehavior.Strict);
            
            var svc = new CheckEligibilityService(new NullLoggerFactory(), db.Object, _mapper, null, _configuration, _moqDwpService.Object, _moqAudit.Object, _hashService);
            db.Setup(x => x.CheckEligibilities.AddAsync(It.IsAny<EligibilityCheck>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());
            
            // Act
            Func<Task> act = async () => await svc.PostCheck(request);

            // Assert
            act.Should().ThrowExactlyAsync<DbUpdateException>();
        }

        [Test]
        public async Task Given_PostCheck_HashIsOldSoNewOne_generated()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            request.DateOfBirth = "1970-02-01";
            request.NationalAsylumSeekerServiceNumber = null;

            //Set UpValid hmrc check
            _fakeInMemoryDb.FreeSchoolMealsHMRC.Add(new FreeSchoolMealsHMRC
            {
                FreeSchoolMealsHMRCID = request.NationalInsuranceNumber,
                Surname = request.LastName,
                DateOfBirth = DateTime.Parse(request.DateOfBirth)
            });
            await _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(Guid.NewGuid().ToString());
            var result = new StatusCodeResult(StatusCodes.Status200OK);
            _moqDwpService.Setup(x => x.GetCitizenClaims(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(result);
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");
                        // Act/Assert
            var response = await _sut.PostCheck(request);
            
            
            var baseItem = _fakeInMemoryDb.CheckEligibilities.FirstOrDefault(x => x.EligibilityCheckID == response.Id);
            baseItem.EligibilityCheckHashID.Should().BeNull();
            await _sut.ProcessCheck(response.Id, _fixture.Create<AuditData>());
            baseItem = _fakeInMemoryDb.CheckEligibilities.Include(x => x.EligibilityCheckHash).FirstOrDefault(x => x.EligibilityCheckID == response.Id);
            baseItem.EligibilityCheckHash.Should().NotBeNull();
            var BaseHash = _fakeInMemoryDb.EligibilityCheckHashes.First(x=>x.EligibilityCheckHashID == baseItem.EligibilityCheckHashID);

            BaseHash.TimeStamp = BaseHash.TimeStamp.AddMonths(-12);
            await _fakeInMemoryDb.SaveChangesAsync();

            //post a second check so that New hash is used for outcome
            var responseNewPostCheck = await _sut.PostCheck(request);

            var newItem = _fakeInMemoryDb.CheckEligibilities.Include(x => x.EligibilityCheckHash).FirstOrDefault(x => x.EligibilityCheckID == responseNewPostCheck.Id);
            newItem.EligibilityCheckHash.Should().BeNull();
            newItem.Status.Should().Be(CheckEligibilityStatus.queuedForProcessing);
        }

        [Test]
        public async Task Given_PostCheck_Status_should_Come_From_Hash()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            request.DateOfBirth = "1970-02-01";
            request.NationalAsylumSeekerServiceNumber = null;

            //Set UpValid hmrc check
            _fakeInMemoryDb.FreeSchoolMealsHMRC.Add(new FreeSchoolMealsHMRC
            {
                FreeSchoolMealsHMRCID = request.NationalInsuranceNumber,
                Surname = request.LastName,
                DateOfBirth = DateTime.Parse(request.DateOfBirth)
            });
            await _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(Guid.NewGuid().ToString());
            var result = new StatusCodeResult(StatusCodes.Status200OK);
            _moqDwpService.Setup(x => x.GetCitizenClaims(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(result);
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");


            // Act/Assert
            var response = await _sut.PostCheck(request);
            var baseItem = _fakeInMemoryDb.CheckEligibilities.FirstOrDefault(x => x.EligibilityCheckID == response.Id);
            baseItem.EligibilityCheckHashID.Should().BeNull();
            await _sut.ProcessCheck(response.Id, _fixture.Create<AuditData>());

            baseItem = _fakeInMemoryDb.CheckEligibilities.Include(x=>x.EligibilityCheckHash).FirstOrDefault(x => x.EligibilityCheckID == response.Id);
            baseItem.EligibilityCheckHash.Should().NotBeNull();
            var BaseHash = baseItem.EligibilityCheckHash;

            //post a second check so that BaseHash is used for outcome
            var responseNewPostCheck = await _sut.PostCheck(request);

            var newItem = _fakeInMemoryDb.CheckEligibilities.Include(x => x.EligibilityCheckHash).FirstOrDefault(x => x.EligibilityCheckID == responseNewPostCheck.Id);
            newItem.EligibilityCheckHash.Should().NotBeNull();
            newItem.Status.Should().Be(BaseHash.Outcome);
        }

        [Test]
        public async Task Given_validRequest_PostFeature_Should_Return_id_HashShouldBeCreated()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            request.DateOfBirth = "1970-02-01";
            request.NationalAsylumSeekerServiceNumber = null;
            var key = string.IsNullOrEmpty(request.NationalInsuranceNumber) ? request.NationalAsylumSeekerServiceNumber : request.NationalInsuranceNumber;
            //Set UpValid hmrc check
            _fakeInMemoryDb.FreeSchoolMealsHMRC.Add(new FreeSchoolMealsHMRC { FreeSchoolMealsHMRCID = request.NationalInsuranceNumber,
                Surname = request.LastName,
                DateOfBirth = DateTime.Parse(request.DateOfBirth) });
            await _fakeInMemoryDb.SaveChangesAsync();

            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(Guid.NewGuid().ToString());
            var result = new StatusCodeResult(StatusCodes.Status200OK);
            _moqDwpService.Setup(x => x.GetCitizenClaims(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(result);
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");

            // Act
            var response = _sut.PostCheck(request);
            var process = _sut.ProcessCheck(response.Result.Id, _fixture.Create<AuditData>());
            var item = _fakeInMemoryDb.CheckEligibilities.FirstOrDefault(x=>x.EligibilityCheckID == response.Result.Id);
            var hash = CheckEligibilityService.GetHash(
                new CheckProcessData {DateOfBirth = request.DateOfBirth,
                LastName = request.LastName,
                NationalInsuranceNumber = request.NationalInsuranceNumber,
                NationalAsylumSeekerServiceNumber = request.NationalAsylumSeekerServiceNumber,
            Type = request.Type
            });
            // Assert
            _fakeInMemoryDb.EligibilityCheckHashes.First(x=>x.Hash == hash).Should().NotBeNull();
        }


        [Test]
        public async Task Given_PostBulk_Should_Complete()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            request.DateOfBirth = "1970-02-01";
            request.NationalAsylumSeekerServiceNumber = null;
            var key = string.IsNullOrEmpty(request.NationalInsuranceNumber) ? request.NationalAsylumSeekerServiceNumber : request.NationalInsuranceNumber;
            //Set UpValid hmrc check
            _fakeInMemoryDb.FreeSchoolMealsHMRC.Add(new FreeSchoolMealsHMRC
            {
                FreeSchoolMealsHMRCID = request.NationalInsuranceNumber,
                Surname = request.LastName,
                DateOfBirth = DateTime.Parse(request.DateOfBirth)
            });
            await _fakeInMemoryDb.SaveChangesAsync();

            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(Guid.NewGuid().ToString());
            var result = new StatusCodeResult(StatusCodes.Status200OK);
            _moqDwpService.Setup(x => x.GetCitizenClaims(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(result);
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");


            // Act
            var response = _sut.PostCheck(new List<CheckEligibilityRequestData_Fsm>(){request}, Guid.NewGuid().ToString());
            Assert.Pass();
        }


        [Test]
        public void Given_validRequest_PostFeature_Should_Return_id()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            request.DateOfBirth = "1970-02-01";

            // Act
            var response = _sut.PostCheck(request);

            // Assert
            response.Result.Id.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void Given_InValidRequest_GetStatus_Should_Return_null()
        {
            // Arrange
            var request = _fixture.Create<Guid>().ToString();

            // Act
            var response = _sut.GetStatus(request);

            // Assert
            response.Result.Should().BeNull();
        }

        [Test]
        public void Given_InValidRequest_GetStatus_Should_Return_status()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.GetStatus(item.EligibilityCheckID);

            // Assert
            _ = response.Result.ToString().Should().Be(item.Status.ToString());
        }

        [Test]
        public void Given_InValidRequest_GetBulkStatus_Should_Return_null()
        {
            // Arrange
            var request = _fixture.Create<Guid>().ToString();

            // Act
            var response = _sut.GetBulkStatus(request);

            // Assert
            response.Result.Should().BeNull();
        }

        [Test]
        public void Given_InValidRequest_GetBulkStatus_Should_Return_status()
        {
            // Arrange
            var items = _fixture.CreateMany<EligibilityCheck>();
            var guid = _fixture.Create<string>();
            foreach (var item in items)
            {
                item.Group = guid;
            }
            _fakeInMemoryDb.CheckEligibilities.AddRange(items);
            _fakeInMemoryDb.SaveChangesAsync();
            var results = _fakeInMemoryDb.CheckEligibilities
                .Where(x => x.Group == guid)
                .GroupBy(n => n.Status)
                .Select(n => new { Status = n.Key, ct = n.Count() });
            var total = results.Sum(s => s.ct);
            var completed = results.Where(a => a.Status != CheckEligibilityStatus.queuedForProcessing).Sum(s => s.ct);

            // Act
            var response = _sut.GetBulkStatus(guid);

            // Assert
            response.Result.Total.Should().Be(total);
            response.Result.Complete.Should().Be(completed);
        }

        [Test]
        public void Given_InValidRequest_Process_Should_Return_null()
        {
            // Arrange
            var request = _fixture.Create<Guid>().ToString();

            // Act
            var response = _sut.ProcessCheck(request, _fixture.Create<AuditData>());

            // Assert
            response.Result.Should().BeNull();
        }
        [Test]
        public void Given_validRequest_StatusNot_queuedForProcessing_Process_Should_throwProcessException()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.eligible;
            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            act.Should().ThrowExactlyAsync<ProcessCheckException>();
        }

        [Test]
        public async Task Given_validRequest_Process_Should_Return_updatedStatus_parentNotFound()
        {
            // Arrange
           
            var item = _fixture.Create<EligibilityCheck>();
            item.Type = CheckEligibilityType.FreeSchoolMeals;
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.CheckData = JsonConvert.SerializeObject(dataItem);

            _fakeInMemoryDb.CheckEligibilities.Add(item);

            await _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.UseEcsforChecks).Returns(false);
            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(CheckEligibilityStatus.parentNotFound.ToString());
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");

            // Act
            var response = await _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Should().Be(CheckEligibilityStatus.parentNotFound);
        }

        [Test]
        public void Given_validRequest_HMRC_InvalidNI_Process_Should_Return_updatedStatus_parentNotFound()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.CheckData = JsonConvert.SerializeObject(dataItem);

            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.UseEcsforChecks).Returns(false);
            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(CheckEligibilityStatus.parentNotFound.ToString());
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");

            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.parentNotFound);
        }
       

        [Test]
        public void Given_validRequest_DWP_Soap_Process_Should_Return_updatedStatus_Eligible()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.CheckData = JsonConvert.SerializeObject(dataItem);

            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.UseEcsforChecks).Returns(true);
            var ecsSoapCheckResponse = new SoapFsmCheckRespone { Status="1",ErrorCode ="0", Qualifier =""};
            _moqDwpService.Setup(x => x.EcsFsmCheck(It.IsAny<CheckProcessData>())).ReturnsAsync(ecsSoapCheckResponse);
            var result = new StatusCodeResult(StatusCodes.Status200OK);
            //_moqDwpService.Setup(x => x.GetCitizenClaims(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(result);
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");


            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.eligible);
        }
        [Test]
        public void Given_validRequest_DWP_Soap_Process_Should_Return_updatedStatus_notEligible()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.CheckData = JsonConvert.SerializeObject(dataItem);

            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.UseEcsforChecks).Returns(true);
            var ecsSoapCheckResponse = new SoapFsmCheckRespone { Status = "0", ErrorCode = "0", Qualifier = "" };
            _moqDwpService.Setup(x => x.EcsFsmCheck(It.IsAny<CheckProcessData>())).ReturnsAsync(ecsSoapCheckResponse);
            var result = new StatusCodeResult(StatusCodes.Status200OK);
            //_moqDwpService.Setup(x => x.GetCitizenClaims(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(result);
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");


            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.notEligible);
        }

        [Test]
        public void Given_validRequest_DWP_Soap_Process_Should_Return_updatedStatus_parentNotFound()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.CheckData = JsonConvert.SerializeObject(dataItem);

            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.UseEcsforChecks).Returns(true);
            var ecsSoapCheckResponse = new SoapFsmCheckRespone { Status = "0", ErrorCode = "0", Qualifier = "No trace" };
            _moqDwpService.Setup(x => x.EcsFsmCheck(It.IsAny<CheckProcessData>())).ReturnsAsync(ecsSoapCheckResponse);
            var result = new StatusCodeResult(StatusCodes.Status200OK);
            //_moqDwpService.Setup(x => x.GetCitizenClaims(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(result);
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");


            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.parentNotFound);
        }

        [Test]
        public void Given_validRequest_DWP_Soap_Process_Should_Return_Null_DwpError()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.CheckData = JsonConvert.SerializeObject(dataItem);
            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.UseEcsforChecks).Returns(true);
            
            _moqDwpService.Setup(x => x.EcsFsmCheck(It.IsAny<CheckProcessData>())).ReturnsAsync(value:null);
            var result = new StatusCodeResult(StatusCodes.Status200OK);
            //_moqDwpService.Setup(x => x.GetCitizenClaims(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(result);
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");


            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.queuedForProcessing);
        }

        [Test]
        public void Given_validRequest_DWP_Soap_Process_Should_Return_updatedStatus_DwpError()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.CheckData = JsonConvert.SerializeObject(dataItem);

            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.UseEcsforChecks).Returns(true);
            var ecsSoapCheckResponse = new SoapFsmCheckRespone { Status = "0", ErrorCode = "-1", Qualifier = "refer to admin" };
            _moqDwpService.Setup(x => x.EcsFsmCheck(It.IsAny<CheckProcessData>())).ReturnsAsync(ecsSoapCheckResponse);
            var result = new StatusCodeResult(StatusCodes.Status200OK);
            //_moqDwpService.Setup(x => x.GetCitizenClaims(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(result);
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");


            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.queuedForProcessing);
        }

        [Test]
        public void Given_validRequest_DWP_Process_Should_Return_updatedStatus_Eligible()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.CheckData = JsonConvert.SerializeObject(dataItem);
            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.UseEcsforChecks).Returns(false);
            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(Guid.NewGuid().ToString());
            var result = new StatusCodeResult(StatusCodes.Status200OK);
            _moqDwpService.Setup(x => x.GetCitizenClaims(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(result);
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");


            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.eligible);
        }

        [Test]
        public void Given_validRequest_DWP_Process_Should_Return_updatedStatus_notEligible()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.CheckData = JsonConvert.SerializeObject(dataItem);
            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.UseEcsforChecks).Returns(false);
            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(Guid.NewGuid().ToString());
            var result = new StatusCodeResult(StatusCodes.Status404NotFound);
            _moqDwpService.Setup(x => x.GetCitizenClaims(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(result);
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");


            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.notEligible);
        }

        [Test]
        public void Given_validRequest_DWP_Process_Should_Return_checkError()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.CheckData = JsonConvert.SerializeObject(dataItem);
            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.UseEcsforChecks).Returns(false);
            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(Guid.NewGuid().ToString());
            var result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            _moqDwpService.Setup(x => x.GetCitizenClaims(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(result);
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");


            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.queuedForProcessing);
        }

        [Test]
        public async Task Given_validRequest_DWP_Process_Should_Return_500_Failure_status_is_NotUpdated()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.CheckData = JsonConvert.SerializeObject(dataItem);
            _fakeInMemoryDb.CheckEligibilities.Add(item);
            await _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.UseEcsforChecks).Returns(false);
            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(CheckEligibilityStatus.DwpError.ToString());
            var result = new StatusCodeResult(StatusCodes.Status500InternalServerError);

            // Act
            var response = await _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Should().Be(CheckEligibilityStatus.queuedForProcessing);
        }

        [Test]
        public void Given_validRequest_HO_InvalidNASS_Process_Should_Return_updatedStatus_parentNotFound()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            fsm.NationalInsuranceNumber = null;
            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.CheckData = JsonConvert.SerializeObject(dataItem);
            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");

            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.parentNotFound);
        }

        [Test]
        public void Given_validRequest_HMRC_Process_Should_Return_updatedStatus_eligible()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.CheckData = JsonConvert.SerializeObject(dataItem);

            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.FreeSchoolMealsHMRC.Add(new FreeSchoolMealsHMRC { FreeSchoolMealsHMRCID = fsm.NationalInsuranceNumber, Surname = fsm.LastName, DateOfBirth = DateTime.ParseExact(fsm.DateOfBirth, "yyyy-MM-dd", null, DateTimeStyles.None),
                 });
            _fakeInMemoryDb.SaveChangesAsync();
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");


            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.eligible);
        }

        [Test]
        public void Given_SurnameCharacterMatchFails_HMRC_Process_Should_Return_updatedStatus_parentNotFound()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.CheckData = JsonConvert.SerializeObject(dataItem);
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var surnamevalid = "simpson";
            var surnameInvalid = "x" + surnamevalid;

            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.FreeSchoolMealsHMRC.Add(new FreeSchoolMealsHMRC { FreeSchoolMealsHMRCID = fsm.NationalInsuranceNumber, Surname = surnameInvalid, DateOfBirth =
                DateTime.ParseExact(dataItem.DateOfBirth, "yyyy-MM-dd", null, DateTimeStyles.None),
            });
            _fakeInMemoryDb.SaveChangesAsync();

            _moqDwpService.Setup(x=> x.UseEcsforChecks).Returns(false);
            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(CheckEligibilityStatus.parentNotFound.ToString());
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");

            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.parentNotFound);
        }

        [Test]
        public async Task Given_SurnameCharacterMatchPasses_HMRC_Process_Should_Return_updatedStatus_eligible()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var surnamevalid = "simpson";
            fsm.LastName = surnamevalid;
            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.CheckData = JsonConvert.SerializeObject(dataItem);
            item.Status = CheckEligibilityStatus.queuedForProcessing;
           
            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.FreeSchoolMealsHMRC.Add(new FreeSchoolMealsHMRC { FreeSchoolMealsHMRCID = dataItem.NationalInsuranceNumber, Surname = surnamevalid, DateOfBirth = DateTime.ParseExact(dataItem.DateOfBirth, "yyyy-MM-dd", null, DateTimeStyles.None),
            });
            await _fakeInMemoryDb.SaveChangesAsync();
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");

            // Act
            var response = await _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Should().Be(CheckEligibilityStatus.eligible);
        }

        [Test]
        public async Task Given_validRequest_HO_Process_Should_Return_updatedStatus_eligible()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            fsm.NationalInsuranceNumber = string.Empty;

            var dataItem = GetCheckProcessData(fsm);
            item.Type = fsm.Type;
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            item.CheckData = JsonConvert.SerializeObject(dataItem);

            item.CheckData = JsonConvert.SerializeObject(dataItem);

            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.FreeSchoolMealsHO.Add(new FreeSchoolMealsHO { FreeSchoolMealsHOID = "123", NASS = dataItem.NationalAsylumSeekerServiceNumber, LastName = dataItem.LastName,
                DateOfBirth = DateTime.ParseExact(dataItem.DateOfBirth, "yyyy-MM-dd", null, DateTimeStyles.None)
            });
            _fakeInMemoryDb.SaveChangesAsync();
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");

            // Act
            var response = await _sut.ProcessCheck(item.EligibilityCheckID, _fixture.Create<AuditData>());

            // Assert
            response.Should().Be(CheckEligibilityStatus.eligible);
        }

        [Test]
        public void Given_InValidRequest_GetItem_Should_Return_null()
        {
            // Arrange
            var request = _fixture.Create<Guid>().ToString();

            // Act
            var response = _sut.GetItem(request);

            // Assert
            response.Result.Should().BeNull();
        }

        [Test]
        public void Given_ValidRequest_GetItem_Should_Return_Item()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.GetItem(item.EligibilityCheckID);

            // Assert
            response.Result.Should().BeOfType<CheckEligibilityItemFsm>();
        }

        [Test]
        public void Given_InValidRequest_GetBulkCheckResults_Should_Return_null()
        {
            // Arrange
            var request = _fixture.Create<Guid>().ToString();

            // Act
            var response = _sut.GetBulkCheckResults(request);

            // Assert
            response.Result.Should().BeNull();
        }

        [Test]
        public void Given_ValidRequest_GetBulkCheckResults_Should_Return_Items()
        {
            // Arrange
            var groupId = Guid.NewGuid().ToString();
            var item = _fixture.Create<EligibilityCheck>();
            item.Group = groupId;
            _fakeInMemoryDb.CheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.GetBulkCheckResults(groupId);

            // Assert
            response.Result.Should().BeOfType<List<CheckEligibilityItemFsm>>();
        }

        [Test]
        public void Given_InValidRequest_UpdateEligibilityCheckStatus_Should_Return_null()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var request = _fixture.Create<EligibilityStatusUpdateRequest>();

            // Act
            var response = _sut.UpdateEligibilityCheckStatus(guid, request.Data);

            // Assert
            response.Result.Should().BeNull();
        }

        [Test]
        public async Task Given_ValidRequest_UpdateEligibilityCheckStatus_Should_Return_UpdatedStatus()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            _fakeInMemoryDb.CheckEligibilities.Add(item);
            await _fakeInMemoryDb.SaveChangesAsync();

            var requestUpdateStatus = _fixture.Create<EligibilityCheckStatusData>();

            // Act
            var statusUpdate = await _sut.UpdateEligibilityCheckStatus(item.EligibilityCheckID, requestUpdateStatus);

            // Assert
            statusUpdate.Should().BeOfType<CheckEligibilityStatusResponse>();
            statusUpdate.Data.Status.Should().BeEquivalentTo(requestUpdateStatus.Status.ToString());
        }

        private CheckProcessData GetCheckProcessData(CheckEligibilityRequestData_Fsm request)
        {
            return  new CheckProcessData
            {
                DateOfBirth = request.DateOfBirth,
                LastName = request.LastName,
                NationalAsylumSeekerServiceNumber = request.NationalAsylumSeekerServiceNumber,
                NationalInsuranceNumber = request.NationalInsuranceNumber,
                Type = new CheckEligibilityRequestData_Fsm().Type
            };
        }
    }
}