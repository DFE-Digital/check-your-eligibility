// Ignore Spelling: Levenshtein

using AutoFixture;
using AutoMapper;
using Azure.Storage.Queues;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Requests.DWP;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Globalization;
using School = CheckYourEligibility.Data.Models.School;

namespace CheckYourEligibility.ServiceUnitTests
{


    public class FsmCheckEligibilityServiceTests : TestBase.TestBase
    {
        private IEligibilityCheckContext _fakeInMemoryDb;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private FsmCheckEligibilityService _sut;
        private Mock<IDwpService> _moqDwpService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EligibilityCheckContext>()
            .UseInMemoryDatabase(databaseName: "FakeInMemoryDb")
            .Options;

            _fakeInMemoryDb = new EligibilityCheckContext(options);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<FsmMappingProfile>());
            _mapper = config.CreateMapper();
            var configForSmsApi = new Dictionary<string, string>
            {
                {"QueueFsmCheckStandard", "notSet"},
                {"HashCheckDays", "7"},

            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configForSmsApi)
                .Build();
            var webJobsConnection = "DefaultEndpointsProtocol=https;AccountName=none;AccountKey=none;EndpointSuffix=core.windows.net";
           
            _moqDwpService = new Mock<IDwpService>(MockBehavior.Strict);

            _sut = new FsmCheckEligibilityService(new NullLoggerFactory(), _fakeInMemoryDb, _mapper, new QueueServiceClient(webJobsConnection), _configuration, _moqDwpService.Object);

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
            Action act = () => new FsmCheckEligibilityService(new NullLoggerFactory(), null, _mapper, null, null,null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'dbContext')");
        }

        [Test]
        public void Given_validRequest_PostApplication_Should_Return_ApplicationSaveFsm()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequestData>();
            request.ParentDateOfBirth = "01/02/1970";
            request.ChildDateOfBirth = "01/02/2007";
            var la = _fixture.Create<LocalAuthority>();
            var school = _fixture.Create<School>();
            school.LocalAuthorityId = la.LocalAuthorityId;
            request.School = school.SchoolId;
            _fakeInMemoryDb.LocalAuthorities.Add(la);
            _fakeInMemoryDb.Schools.Add(school);
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.PostApplication(request);

            // Assert
            response.Result.Should().BeOfType<ApplicationSave>();
        }

        [Test]
        public async Task Given_PostCheck_HashIsOldSoNewOne_generated()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequestDataFsm>();
            request.DateOfBirth = "01/02/1970";
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
            _moqDwpService.Setup(x => x.CheckForBenefit(It.IsAny<string>())).ReturnsAsync(result);

            // Act/Assert
            var response = await _sut.PostCheck(request);
            var baseItem = _fakeInMemoryDb.FsmCheckEligibilities.FirstOrDefault(x => x.EligibilityCheckID == response.Id);
            baseItem.EligibilityCheckHashID.Should().BeNull();
            await _sut.ProcessCheck(response.Id);
            baseItem = _fakeInMemoryDb.FsmCheckEligibilities.Include(x => x.EligibilityCheckHash).FirstOrDefault(x => x.EligibilityCheckID == response.Id);
            baseItem.EligibilityCheckHash.Should().NotBeNull();
            var BaseHash = _fakeInMemoryDb.EligibilityCheckHashes.First(x=>x.EligibilityCheckHashID == baseItem.EligibilityCheckHashID);

            BaseHash.TimeStamp = BaseHash.TimeStamp.AddMonths(-12);
            await _fakeInMemoryDb.SaveChangesAsync();

            //post a second check so that New hash is used for outcome
            var responseNewPostCheck = await _sut.PostCheck(request);

            var newItem = _fakeInMemoryDb.FsmCheckEligibilities.Include(x => x.EligibilityCheckHash).FirstOrDefault(x => x.EligibilityCheckID == responseNewPostCheck.Id);
            newItem.EligibilityCheckHash.Should().BeNull();
            newItem.Status.Should().Be(CheckEligibilityStatus.queuedForProcessing);
        }

        [Test]
        public async Task Given_PostCheck_Status_should_Come_From_Hash()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequestDataFsm>();
            request.DateOfBirth = "01/02/1970";
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
            _moqDwpService.Setup(x => x.CheckForBenefit(It.IsAny<string>())).ReturnsAsync(result);

            // Act/Assert
            var response = await _sut.PostCheck(request);
            var baseItem = _fakeInMemoryDb.FsmCheckEligibilities.FirstOrDefault(x => x.EligibilityCheckID == response.Id);
            baseItem.EligibilityCheckHashID.Should().BeNull();
            await _sut.ProcessCheck(response.Id);
            baseItem = _fakeInMemoryDb.FsmCheckEligibilities.Include(x=>x.EligibilityCheckHash).FirstOrDefault(x => x.EligibilityCheckID == response.Id);
            baseItem.EligibilityCheckHash.Should().NotBeNull();
            var BaseHash = baseItem.EligibilityCheckHash;

            //post a second check so that BaseHash is used for outcome
            var responseNewPostCheck = await _sut.PostCheck(request);

            var newItem = _fakeInMemoryDb.FsmCheckEligibilities.Include(x => x.EligibilityCheckHash).FirstOrDefault(x => x.EligibilityCheckID == responseNewPostCheck.Id);
            newItem.EligibilityCheckHash.Should().NotBeNull();
            newItem.Status.Should().Be(BaseHash.Outcome);
        }

       
        [Test]
        public async Task Given_validRequest_PostFeature_Should_Return_id_HashShouldBeCreated()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequestDataFsm>();
            request.DateOfBirth = "01/02/1970";
            request.NationalAsylumSeekerServiceNumber = null;
            var key = string.IsNullOrEmpty(request.NationalInsuranceNumber) ? request.NationalAsylumSeekerServiceNumber : request.NationalInsuranceNumber;
            //Set UpValid hmrc check
            _fakeInMemoryDb.FreeSchoolMealsHMRC.Add(new FreeSchoolMealsHMRC { FreeSchoolMealsHMRCID = request.NationalInsuranceNumber,
                Surname = request.LastName,
                DateOfBirth = DateTime.Parse(request.DateOfBirth) });
            await _fakeInMemoryDb.SaveChangesAsync();

            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(Guid.NewGuid().ToString());
            var result = new StatusCodeResult(StatusCodes.Status200OK);
            _moqDwpService.Setup(x => x.CheckForBenefit(It.IsAny<string>())).ReturnsAsync(result);

            // Act
            var response = _sut.PostCheck(request);
            var process = _sut.ProcessCheck(response.Result.Id);
            var item = _fakeInMemoryDb.FsmCheckEligibilities.FirstOrDefault(x=>x.EligibilityCheckID == response.Result.Id);
            var hash = _sut.GetHash(item);
            // Assert
            _fakeInMemoryDb.EligibilityCheckHashes.First(x=>x.Hash == hash).Should().NotBeNull();
        }

        
        [Test]
        public void Given_validRequest_PostFeature_Should_Return_id()
        {
            // Arrange
            var request = _fixture.Create<CheckEligibilityRequestDataFsm>();
            request.DateOfBirth = "01/02/1970";

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
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.GetStatus(item.EligibilityCheckID);

            // Assert
            _ = response.Result.ToString().Should().Be(item.Status.ToString());
        }

        [Test]
        public void Given_InValidRequest_Process_Should_Return_null()
        {
            // Arrange
            var request = _fixture.Create<Guid>().ToString();

            // Act
            var response = _sut.ProcessCheck(request);

            // Assert
            response.Result.Should().BeNull();
        }
        [Test]
        public void Given_validRequest_StatusNot_queuedForProcessing_Process_Should_throwProcessException()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.eligible;
            item.NASSNumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await _sut.ProcessCheck(item.EligibilityCheckID);

            // Assert
            act.Should().ThrowExactlyAsync<ProcessCheckException>();
        }

        [Test]
        public void Given_validRequest_Process_Should_Return_updatedStatus_parentNotFound()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.NASSNumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(CheckEligibilityStatus.parentNotFound.ToString());

            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID);

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.parentNotFound);
        }

        [Test]
        public void Given_validRequest_HMRC_InvalidNI_Process_Should_Return_updatedStatus_parentNotFound()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            item.NASSNumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(CheckEligibilityStatus.parentNotFound.ToString());

            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID);

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.parentNotFound);
        }

        [Test]
        public void Given_validRequest_DWP_Process_Should_Return_updatedStatus_Eligible()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            item.NASSNumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(Guid.NewGuid().ToString());
            var result = new StatusCodeResult(StatusCodes.Status200OK);
            _moqDwpService.Setup(x => x.CheckForBenefit(It.IsAny<string>())).ReturnsAsync(result);

            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID);

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.eligible);
        }

        [Test]
        public void Given_validRequest_DWP_Process_Should_Return_updatedStatus_notEligible()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            item.NASSNumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();
            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(Guid.NewGuid().ToString());
            var result = new StatusCodeResult(StatusCodes.Status404NotFound);
            _moqDwpService.Setup(x => x.CheckForBenefit(It.IsAny<string>())).ReturnsAsync(result);

            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID);

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.notEligible);
        }

        [Test]
        public void Given_validRequest_HO_InvalidNASS_Process_Should_Return_updatedStatus_parentNotFound()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            item.NINumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID);

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.parentNotFound);
        }

        [Test]
        public void Given_validRequest_HMRC_Process_Should_Return_updatedStatus_eligible()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            item.NASSNumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.FreeSchoolMealsHMRC.Add(new FreeSchoolMealsHMRC { FreeSchoolMealsHMRCID = item.NINumber, Surname = item.LastName, DateOfBirth = item.DateOfBirth });
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID);

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.eligible);
        }

        [Test]
        public void Given_SurnameCharacterMatchFails_HMRC_Process_Should_Return_updatedStatus_parentNotFound()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var surnamevalid = "simpson";
            item.LastName = surnamevalid;
            var surnameInvalid = "x" + surnamevalid;

            item.NASSNumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.FreeSchoolMealsHMRC.Add(new FreeSchoolMealsHMRC { FreeSchoolMealsHMRCID = item.NINumber, Surname = surnameInvalid, DateOfBirth = item.DateOfBirth });
            _fakeInMemoryDb.SaveChangesAsync();

            _moqDwpService.Setup(x => x.GetCitizen(It.IsAny<CitizenMatchRequest>())).ReturnsAsync(CheckEligibilityStatus.parentNotFound.ToString());

            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID);

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.parentNotFound);
        }

        [Test]
        public void Given_SurnameCharacterMatchPasses_HMRC_Process_Should_Return_updatedStatus_eligible()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            var surnamevalid = "simpson";
            item.LastName = surnamevalid;
            var surnameInvalid = surnamevalid + "x";
            item.NASSNumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.FreeSchoolMealsHMRC.Add(new FreeSchoolMealsHMRC { FreeSchoolMealsHMRCID = item.NINumber, Surname = surnameInvalid, DateOfBirth = item.DateOfBirth });
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID);

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.eligible);
        }

        [Test]
        public void Given_validRequest_HO_Process_Should_Return_updatedStatus_eligible()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.Status = CheckEligibilityStatus.queuedForProcessing;
            item.NINumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.FreeSchoolMealsHO.Add(new FreeSchoolMealsHO { FreeSchoolMealsHOID = "123", NASS = item.NASSNumber, LastName = item.LastName, DateOfBirth = item.DateOfBirth });
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID);

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.eligible);
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
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.GetItem(item.EligibilityCheckID);

            // Assert
            response.Result.Should().BeOfType<CheckEligibilityItemFsm>();
        }

        [Test]
        public void Given_InValidRequest_UpdateApplicationStatus_Should_Return_null()
        {
            // Arrange
            var guid = _fixture.Create<Guid>().ToString();
            var request = _fixture.Create<ApplicationStatusUpdateRequest>();

            // Act
            var response = _sut.UpdateApplicationStatus(guid, request.Data);

            // Assert
            response.Result.Should().BeNull();
        }

        [Test]
        public async Task Given_ValidRequest_UpdateApplicationStatus_Should_Return_UpdatedStatus()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequestData>();
            request.ParentDateOfBirth = "01/02/1970";
            request.ChildDateOfBirth = "01/02/2007";
            var la = _fixture.Create<LocalAuthority>();
            var school = _fixture.Create<School>();
            school.LocalAuthorityId = la.LocalAuthorityId;
            request.School = school.SchoolId;
            _fakeInMemoryDb.LocalAuthorities.Add(la);
            _fakeInMemoryDb.Schools.Add(school);
            await _fakeInMemoryDb.SaveChangesAsync();

            var requestUpdateStatus = _fixture.Create<ApplicationStatusUpdateRequest>();

            // Act
            var response = _sut.PostApplication(request);

            // Act
            var applicationStatusUpdate = await _sut.UpdateApplicationStatus(response.Result.Id, requestUpdateStatus.Data);

            // Assert
            applicationStatusUpdate.Should().BeOfType<ApplicationStatusUpdateResponse>();
            applicationStatusUpdate.Data.Status.Should().BeEquivalentTo(requestUpdateStatus.Data.Status.ToString());
        }


        [Test]
        public void Given_InValidRequest_GetApplication_Should_Return_null()
        {
            // Arrange
            var request = _fixture.Create<Guid>().ToString();

            // Act
            var response = _sut.GetApplication(request);

            // Assert
            response.Result.Should().BeNull();
        }

        [Test]
        public async Task Given_ValidRequest_GetApplication_Should_Return_Item()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequestData>();
            request.ParentDateOfBirth = "01/02/1970";
            request.ChildDateOfBirth = "01/02/2007";
            var la = _fixture.Create<LocalAuthority>();
            var school = _fixture.Create<School>();
            school.LocalAuthorityId = la.LocalAuthorityId;
            request.School = school.SchoolId;
            _fakeInMemoryDb.LocalAuthorities.Add(la);
            _fakeInMemoryDb.Schools.Add(school);
            await _fakeInMemoryDb.SaveChangesAsync();
            
            var postApplicationResponse =await _sut.PostApplication(request);

            // Act
            var response = _sut.GetApplication(postApplicationResponse.Id);

            // Assert
            response.Result.Should().BeOfType<Domain.Responses.ApplicationResponse>();
        }

        [Test]
        public void Given_NoResults_GetApplications_Should_Return_null()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequestSearchData>();

            // Act
            var response = _sut.GetApplications(request);

            // Assert
            response.Result.Should().BeEmpty();
        }

        [Test]
        public async Task Given_ValidRequest_GetApplications_Should_Return_results()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequestData>();
            request.ParentDateOfBirth = "01/02/1970";
            request.ChildDateOfBirth = "01/02/2007";
            var la = _fixture.Create<LocalAuthority>();
            var school = _fixture.Create<School>();
            school.LocalAuthorityId = la.LocalAuthorityId;
            request.School = school.SchoolId;
            _fakeInMemoryDb.LocalAuthorities.Add(la);
            _fakeInMemoryDb.Schools.Add(school);
            await _fakeInMemoryDb.SaveChangesAsync();

            await _sut.PostApplication(request);

            var requestSearch = new ApplicationRequestSearchData() { School = school.SchoolId };

            // Act
            var response = _sut.GetApplications(requestSearch);

            // Assert
            response.Result.Should().NotBeEmpty();
        }
    }
}