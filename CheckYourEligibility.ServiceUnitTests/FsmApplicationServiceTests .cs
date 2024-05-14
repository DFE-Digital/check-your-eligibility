// Ignore Spelling: Levenshtein

using AutoFixture;
using AutoMapper;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.Data.Models;
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


    public class FsmApplicationServiceTests : TestBase.TestBase
    {
        private IEligibilityCheckContext _fakeInMemoryDb;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private FsmApplicationService _sut;

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
         
            _sut = new FsmApplicationService(new NullLoggerFactory(), _fakeInMemoryDb, _mapper);

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
            Action act = () => new FsmApplicationService(new NullLoggerFactory(), null, _mapper);

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

        [Test]
        public void Given_Application_WithUserReturnNewUser()
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
            var user = _fixture.Create<User>();
            _fakeInMemoryDb.Users.Add(user);
            request.UserId = user.UserID;
            _fakeInMemoryDb.SaveChangesAsync();
            var appResponse = _sut.PostApplication(request);

            // Act
            var response = _sut.GetApplication(appResponse.Result.Id);

            // Assert
            response.Result.User.UserID.Should().BeEquivalentTo(user.UserID);
        }
    }
}