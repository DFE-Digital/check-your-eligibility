// Ignore Spelling: Levenshtein

using AutoFixture;
using AutoMapper;
using Azure.Storage.Queues;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using School = CheckYourEligibility.Data.Models.School;

namespace CheckYourEligibility.ServiceUnitTests
{


    public class FsmCheckEligibilityServiceTests : TestBase.TestBase
    {
        private IEligibilityCheckContext _fakeInMemoryDb;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private FsmCheckEligibilityService _sut;

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
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configForSmsApi)
                .Build();
            var webJobsConnection = "DefaultEndpointsProtocol=https;AccountName=none;AccountKey=none;EndpointSuffix=core.windows.net";


            _sut = new FsmCheckEligibilityService(new NullLoggerFactory(), _fakeInMemoryDb, _mapper, new QueueServiceClient(webJobsConnection), _configuration);

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
            Action act = () => new FsmCheckEligibilityService(new NullLoggerFactory(), null, _mapper, null, null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'dbContext')");
        }

        [Test]
        public void Given_validRequest_PostApplication_Should_Return_ApplicationSaveFsm()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequestDataFsm>();
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
            response.Result.Should().BeOfType<ApplicationSaveFsm>();
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
            response.Result.Should().NotBeNullOrEmpty();
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
        public void Given_validRequest_Process_Should_Return_updatedStatus_parentNotFound()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
            item.NASSNumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();

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
            item.NASSNumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.ProcessCheck(item.EligibilityCheckID);

            // Assert
            response.Result.Should().Be(CheckEligibilityStatus.parentNotFound);
        }

        [Test]
        public void Given_validRequest_HO_InvalidNASS_Process_Should_Return_updatedStatus_parentNotFound()
        {
            // Arrange
            var item = _fixture.Create<EligibilityCheck>();
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
            var surnamevalid = "simpson";
            item.LastName = surnamevalid;
            var surnameInvalid = "x" + surnamevalid;
            item.NASSNumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.FreeSchoolMealsHMRC.Add(new FreeSchoolMealsHMRC { FreeSchoolMealsHMRCID = item.NINumber, Surname = surnameInvalid, DateOfBirth = item.DateOfBirth });
            _fakeInMemoryDb.SaveChangesAsync();

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
            var request = _fixture.Create<ApplicationRequestDataFsm>();
            request.ParentDateOfBirth = "01/02/1970";
            request.ChildDateOfBirth = "01/02/2007";
            var la = _fixture.Create<LocalAuthority>();
            var school = _fixture.Create<School>();
            school.LocalAuthorityId = la.LocalAuthorityId;
            request.School = school.SchoolId;
            _fakeInMemoryDb.LocalAuthorities.Add(la);
            _fakeInMemoryDb.Schools.Add(school);
            await _fakeInMemoryDb.SaveChangesAsync();
            var response = await _sut.PostApplication(request);

            // Act
            var responseApplication = _sut.GetApplication(response.Id);

            // Assert
            responseApplication.Result.Should().BeOfType<ApplicationFsm>();
        }

    }
}