// Ignore Spelling: Levenshtein

using AutoFixture;
using AutoMapper;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services;
using EFCore.BulkExtensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using School = CheckYourEligibility.Data.Models.School;

namespace CheckYourEligibility.ServiceUnitTests
{

    [ExcludeFromCodeCoverage(Justification = "test")]
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
         
            _sut = new FsmApplicationService(new NullLoggerFactory(), _fakeInMemoryDb, _mapper, _configuration);

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
            Action act = () => new FsmApplicationService(new NullLoggerFactory(), null, _mapper, _configuration);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'dbContext')");
        }

        [Test]
        public void Given_DB_Add_Should_ThrowException()
        {
            // Arrange
            var db = new Mock<IEligibilityCheckContext>(MockBehavior.Strict);
            var svc = new FsmApplicationService(new NullLoggerFactory(), db.Object, _mapper, _configuration);
            db.Setup(x => x.Applications.AddAsync(It.IsAny<Application>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());
            var request = _fixture.Create<ApplicationRequestData>();

            // Act
            Func<Task> act = async () => await svc.PostApplication(request);

            // Assert
            act.Should().ThrowExactlyAsync<Exception>();
        }

        [Test]
        public void Given_validRequest_PostApplication_Should_Return_ApplicationSaveFsm()
        {
            // Arrange
            var request = _fixture.Create<ApplicationRequestData>();
            request.ParentDateOfBirth = "1970-02-01";
            request.ChildDateOfBirth = "2007-02-01";
            var la = _fixture.Create<LocalAuthority>();
            var school = _fixture.Create<School>();
            school.LocalAuthorityId = la.LocalAuthorityId;
            request.School = school.SchoolId;
            _fakeInMemoryDb.LocalAuthorities.Add(la);
            _fakeInMemoryDb.Schools.Add(school);
            var hash = FsmCheckEligibilityService.GetHash(
                new EligibilityCheck {LastName = request.ParentLastName,
                NINumber = request.ParentNationalInsuranceNumber,
                    DateOfBirth = DateTime.ParseExact(request.ParentDateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                Type = Domain.Enums.CheckEligibilityType.FreeSchoolMeals});
            var hashRecord = _fixture.Create<EligibilityCheckHash>();
            hashRecord.Hash = hash;
            _fakeInMemoryDb.EligibilityCheckHashes.Add(hashRecord);

            _fakeInMemoryDb.SaveChanges();



            // Act
            var response = _sut.PostApplication(request);

            // Assert
            response.Result.Should().BeOfType<ApplicationResponse>();
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
            request.ParentDateOfBirth = "1970-02-01";
            request.ChildDateOfBirth = "2007-02-01";
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
            _fakeInMemoryDb.Applications.RemoveRange(_fakeInMemoryDb.Applications);
            _fakeInMemoryDb.Schools.RemoveRange(_fakeInMemoryDb.Schools);
            _fakeInMemoryDb.LocalAuthorities.RemoveRange(_fakeInMemoryDb.LocalAuthorities);
            _fakeInMemoryDb.SaveChanges();
            var request = _fixture.Create<ApplicationRequestData>();
            request.ParentDateOfBirth = "1970-02-01";
            request.ChildDateOfBirth = "2007-02-01";
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
            request.ParentDateOfBirth = "1990-01-01";
            request.ChildDateOfBirth = "1990-01-01";

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
            request.ParentDateOfBirth = "1970-02-01";
            request.ChildDateOfBirth = "2007-02-01";
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
        public async Task Given_Application_WithUserReturnNewUser()
        {
            // Arrange
            _fakeInMemoryDb.Applications.RemoveRange(_fakeInMemoryDb.Applications);
            _fakeInMemoryDb.Schools.RemoveRange(_fakeInMemoryDb.Schools);
            _fakeInMemoryDb.LocalAuthorities.RemoveRange(_fakeInMemoryDb.LocalAuthorities);
            _fakeInMemoryDb.Users.RemoveRange(_fakeInMemoryDb.Users);
            _fakeInMemoryDb.SaveChanges();

            var request = _fixture.Create<ApplicationRequestData>();
            request.ParentDateOfBirth = "1970-02-01";
            request.ChildDateOfBirth = "2007-02-01";
            var la = _fixture.Create<LocalAuthority>();
            var school = _fixture.Create<School>();
            school.LocalAuthorityId = la.LocalAuthorityId;
            request.School = school.SchoolId;
            _fakeInMemoryDb.LocalAuthorities.Add(la);
            _fakeInMemoryDb.Schools.Add(school);
            var user = _fixture.Create<User>();
            _fakeInMemoryDb.Users.Add(user);
            request.UserId = user.UserID;
            await _fakeInMemoryDb.SaveChangesAsync();
            var appResponse = await _sut.PostApplication(request);

            // Act
            var response = await _sut.GetApplication(appResponse.Id);

            // Assert
            response.User.UserID.Should().BeEquivalentTo(user.UserID);
        }
    }
}