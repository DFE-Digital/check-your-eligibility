// Ignore Spelling: Levenshtein

using AutoFixture;
using AutoMapper;
using Azure.Core;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services;
using CheckYourEligibility.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using static System.Runtime.InteropServices.JavaScript.JSType;
using School = CheckYourEligibility.Data.Models.School;

namespace CheckYourEligibility.ServiceUnitTests
{

    [ExcludeFromCodeCoverage(Justification = "test")]
    public class ApplicationServiceTests : TestBase.TestBase
    {
        private IEligibilityCheckContext _fakeInMemoryDb;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private ApplicationService _sut;
        private IHash _HashService;
        private Mock<IAudit> _moqAudit;

        Data.Models.User User;
        School School;

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
                {"HashCheckDays", "7"},

            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configForSmsApi)
                .Build();
            var webJobsConnection = "DefaultEndpointsProtocol=https;AccountName=none;AccountKey=none;EndpointSuffix=core.windows.net";

            _moqAudit = new Mock<IAudit>(MockBehavior.Strict);
            _HashService = new HashService(new NullLoggerFactory(), _fakeInMemoryDb, _configuration, _moqAudit.Object);
            _sut = new ApplicationService(new NullLoggerFactory(), _fakeInMemoryDb, _mapper, _configuration);

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
            Action act = () => new ApplicationService(new NullLoggerFactory(), null, _mapper, _configuration);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'dbContext')");
        }

        [Test]
        public void Given_DB_Add_Should_ThrowException()
        {
            // Arrange
            var db = new Mock<IEligibilityCheckContext>(MockBehavior.Strict);
            var svc = new ApplicationService(new NullLoggerFactory(), db.Object, _mapper, _configuration);
            db.Setup(x => x.Applications.AddAsync(It.IsAny<Application>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());
            var request = _fixture.Create<ApplicationRequestData>();

            // Act
            Func<Task> act = async () => await svc.PostApplication(request);

            // Assert
            act.Should().ThrowExactlyAsync<Exception>();
        }


        [Test]
        public async Task Given_HashNotFound_PostApplication_Should_throwException_withMessageNoCheckfound()
        {
            // Arrange
            await ClearDownData();
            await CreateUserSchoolAndLa();
            var request = await CreateApplication(CheckEligibilityType.FreeSchoolMeals, CheckEligibilityStatus.eligible);

            request.ParentLastName = "";
          
            _fakeInMemoryDb.SaveChanges();

            // Act
            var message = $"No Check found. Type:- {request.Type} {JsonConvert.SerializeObject(request)}";
            Func<Task> act = async () => await _sut.PostApplication(request);
            
            // Assert
            act.Should().ThrowExactlyAsync<Exception>().Result.WithMessage(message);
        }

        [Test]
        public async Task Given_validRequest_PostApplication_Should_Return_ApplicationSaveFsm()
        {
            // Arrange
            await ClearDownData();
            await CreateUserSchoolAndLa();
            var request = await CreateApplication(CheckEligibilityType.FreeSchoolMeals, CheckEligibilityStatus.eligible);

            // Act
            var response = _sut.PostApplication(request);

            // Assert
            response.Result.Should().BeOfType<ApplicationResponse>();
        }
        [Test]
        public async Task Given_PostApplication_InvalidSchool_Should_Return_Exception()
        {
            //
            await ClearDownData();
            await CreateUserSchoolAndLa();

            var request = await CreateApplication(CheckEligibilityType.FreeSchoolMeals, CheckEligibilityStatus.notEligible);
            request.School = -1;
            try
            {
                await _sut.PostApplication(request);
            }
            catch (Exception ex)
            {

                ex.Message.Should().BeEquivalentTo("Unable to find school:- -1, Sequence contains no elements");
            }
        }

        [Test]
        public async Task Given_PostApplication_StatusEvidenseNeeded_Should_Return_ApplicationSaveFsm()
        {
            //
            await ClearDownData();
            await CreateUserSchoolAndLa();

            var request = await CreateApplication(CheckEligibilityType.FreeSchoolMeals, CheckEligibilityStatus.notEligible);

            // Act
            var response = _sut.PostApplication(request);

            // Assert
            response.Result.Should().BeOfType<ApplicationResponse>();
            response.Result.Status.Should().BeEquivalentTo(Domain.Enums.ApplicationStatus.EvidenceNeeded.ToString());
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
            await ClearDownData();
            await CreateUserSchoolAndLa();

            var request = await CreateApplication(CheckEligibilityType.FreeSchoolMeals, CheckEligibilityStatus.notEligible);
            var response = _sut.PostApplication(request);

            var requestUpdateStatus = _fixture.Create<ApplicationStatusUpdateRequest>();

            // Act
           

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
            await ClearDownData();
            await CreateUserSchoolAndLa();

            var request = await CreateApplication(CheckEligibilityType.FreeSchoolMeals, CheckEligibilityStatus.notEligible);

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
            var requestSearch = new ApplicationRequestSearch
            {
                Data = new ApplicationRequestSearchData
                {
                    ParentDateOfBirth = "1990-01-01",
                    ChildDateOfBirth = "1990-01-01"
                }
            };

            // Act
            var response = _sut.GetApplications(requestSearch);

            // Assert
            response.Result.Data.Should().BeEmpty();
        }

        [Test]
        public async Task Given_ValidRequest_GetApplications_MultipleStatuses_Should_Return_results()
        {
            await ClearDownData();
            await CreateUserSchoolAndLa();

            var request = await CreateApplication(CheckEligibilityType.FreeSchoolMeals, CheckEligibilityStatus.notEligible);

            var postApplicationResponse = await _sut.PostApplication(request);

            Enum.TryParse(postApplicationResponse.Status, out Domain.Enums.ApplicationStatus statusItem);

            var requestSearch = new ApplicationRequestSearch
            {
                Data = new ApplicationRequestSearchData
                {
                    Statuses = [statusItem],
                    School = School.SchoolId
                }
            };

            // Act
            var response = await _sut.GetApplications(requestSearch);

            // Assert
            response.Data.Should().NotBeEmpty();
        }

        [Test]
        public async Task Given_ValidRequest_GetApplications_AllSearchCritieria_Should_Return_results()
        {
            // Arrange
            await ClearDownData();
            await CreateUserSchoolAndLa();

            var request = await CreateApplication(CheckEligibilityType.FreeSchoolMeals, CheckEligibilityStatus.notEligible);

            var postApplicationResponse = await _sut.PostApplication(request);

            Enum.TryParse(postApplicationResponse.Status, out Domain.Enums.ApplicationStatus statusItem);

            var requestSearch = new ApplicationRequestSearch
            {
                Data = new ApplicationRequestSearchData
                {
                    Statuses = [statusItem],
                    School = School.SchoolId,
                    LocalAuthority = School.LocalAuthorityId,
                    ParentDateOfBirth = postApplicationResponse.ParentDateOfBirth,
                    ParentLastName = postApplicationResponse.ParentLastName,
                    ParentNationalAsylumSeekerServiceNumber = postApplicationResponse.ParentNationalAsylumSeekerServiceNumber,
                    ParentNationalInsuranceNumber = postApplicationResponse.ParentNationalInsuranceNumber,
                    Reference = postApplicationResponse.Reference,
                    ChildDateOfBirth = postApplicationResponse.ChildDateOfBirth,
                    ChildLastName = postApplicationResponse.ChildLastName 
                }
            };

            // Act
            var response = await _sut.GetApplications(requestSearch);

            // Assert
            response.Data.Should().NotBeEmpty();
        }
        [Test]
        public async Task Given_ValidRequest_GetApplicationsWithNas_AllSearchCritieria_Should_Return_results()
        {
            // Arrange
            await ClearDownData();
            await CreateUserSchoolAndLa();

            var request = await CreateApplicationWithNas(CheckEligibilityType.FreeSchoolMeals, CheckEligibilityStatus.notEligible);

            var postApplicationResponse = await _sut.PostApplication(request);

            Enum.TryParse(postApplicationResponse.Status, out Domain.Enums.ApplicationStatus statusItem);

            var requestSearch = new ApplicationRequestSearch
            {
                Data = new ApplicationRequestSearchData
                {
                    Statuses = [statusItem],
                    School = School.SchoolId,
                    LocalAuthority = School.LocalAuthorityId,
                    ParentDateOfBirth = postApplicationResponse.ParentDateOfBirth,
                    ParentLastName = postApplicationResponse.ParentLastName,
                    ParentNationalAsylumSeekerServiceNumber = postApplicationResponse.ParentNationalAsylumSeekerServiceNumber,
                    ParentNationalInsuranceNumber = postApplicationResponse.ParentNationalInsuranceNumber,
                    Reference = postApplicationResponse.Reference,
                    ChildDateOfBirth = postApplicationResponse.ChildDateOfBirth,
                    ChildLastName = postApplicationResponse.ChildLastName
                }
            };

            // Act
            var response = await _sut.GetApplications(requestSearch);

            // Assert
            response.Data.Should().NotBeEmpty();
        }

        [Test]
        public async Task Given_ValidRequest_GetApplications_Should_Return_results()
        {
            // Arrange
            await ClearDownData();
            await CreateUserSchoolAndLa();

            var request = await CreateApplication(CheckEligibilityType.FreeSchoolMeals, CheckEligibilityStatus.notEligible);

            await _sut.PostApplication(request);

            var requestSearch = new ApplicationRequestSearch
            {
                Data = new ApplicationRequestSearchData
                {
                    School = School.SchoolId
                }
            };

            // Act
            var response = await _sut.GetApplications(requestSearch);

            // Assert
            response.Data.Should().NotBeEmpty();
        }

        [Test]
        public async Task Given_Application_WithUserReturnNewUser()
        {
            // Arrange
            await ClearDownData();
            await CreateUserSchoolAndLa();

            var request = await CreateApplication(CheckEligibilityType.FreeSchoolMeals, CheckEligibilityStatus.notEligible);

            var appResponse = await _sut.PostApplication(request);

            // Act
            var response = await _sut.GetApplication(appResponse.Id);

            // Assert
            response.User.UserID.Should().BeEquivalentTo(User.UserID);
        }

        [Test]
        public async Task Given_ZeroOrNegativePageNumber_GetApplications_Should_DefaultToFirstPage()
        {
            // Arrange
            await ClearDownData();
            await CreateUserSchoolAndLa();
            var request = await CreateApplication(CheckEligibilityType.FreeSchoolMeals, CheckEligibilityStatus.notEligible);
            await _sut.PostApplication(request);

            var requestSearch = new ApplicationRequestSearch
            {
                PageNumber = 0, // Edge case: Zero page number
                PageSize = 10,  // Valid page size
                Data = new ApplicationRequestSearchData()
            };

            // Act
            var response = await _sut.GetApplications(requestSearch);

            // Assert
            response.Data.Should().NotBeEmpty(); // Ensures data is returned
            response.Data.Count().Should().BeLessOrEqualTo(10); // Ensures correct page size is respected
        }

        [Test]
        public async Task Given_NegativePageNumber_GetApplications_Should_DefaultToFirstPage()
        {
            // Arrange
            await ClearDownData();
            await CreateUserSchoolAndLa();

            var request = await CreateApplication(CheckEligibilityType.FreeSchoolMeals, CheckEligibilityStatus.notEligible);

            var postApplicationResponse = await _sut.PostApplication(request);
            var requestSearch = new ApplicationRequestSearch
            {
                PageNumber = -1, // Edge case: Negative page number
                PageSize = 10,   // Valid page size
                Data = new ApplicationRequestSearchData()
            };

            // Act
            var response = await _sut.GetApplications(requestSearch);

            // Assert
            response.Data.Should().NotBeEmpty(); // Ensures data is returned
            response.Data.Count().Should().BeLessOrEqualTo(10); // Ensures correct page size is respected
        }

        private async Task AddHash(CheckProcessData request, CheckEligibilityStatus status = CheckEligibilityStatus.eligible)
        {
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");
          
            var processItem = new CheckProcessData
            {
                DateOfBirth = request.DateOfBirth,
                LastName = request.LastName,
                NationalAsylumSeekerServiceNumber = request.NationalAsylumSeekerServiceNumber,
                NationalInsuranceNumber = request.NationalInsuranceNumber,
                Type = new CheckEligibilityRequestData_Fsm().Type
            };
           var hashId =  await _HashService.Create(processItem, status, ProcessEligibilityCheckSource.HO,
            new AuditData { Type = AuditType.Check });
        }

        private async Task CreateUserSchoolAndLa()
        {
            var la = _fixture.Create<LocalAuthority>();
            School = _fixture.Create<School>();
            School.LocalAuthorityId = la.LocalAuthorityId;
         
            _fakeInMemoryDb.LocalAuthorities.Add(la);
            _fakeInMemoryDb.Schools.Add(School);
            User = _fixture.Create<Data.Models.User>();
            _fakeInMemoryDb.Users.Add(User);
            await _fakeInMemoryDb.SaveChangesAsync();

        }

        private async Task<ApplicationRequestData> CreateApplication(CheckEligibilityType type, CheckEligibilityStatus checkEligibilityStatus)
        {

            var request = _fixture.Create<ApplicationRequestData>();
            request.Type = type;
            request.ParentDateOfBirth = "1970-02-01";
            request.ChildDateOfBirth = "2007-02-01";
            request.ParentNationalAsylumSeekerServiceNumber = null;
            request.UserId = User.UserID;
            request.School = School.SchoolId;

            await AddHash(new CheckProcessData
            {
                DateOfBirth = request.ParentDateOfBirth,
                LastName = request.ParentLastName,
                Type = request.Type,
                NationalAsylumSeekerServiceNumber = request.ParentNationalAsylumSeekerServiceNumber,
                NationalInsuranceNumber = request.ParentNationalInsuranceNumber,

            }, checkEligibilityStatus);
            await _fakeInMemoryDb.SaveChangesAsync();
            return request;
        }

        private async Task<ApplicationRequestData> CreateApplicationWithNas(CheckEligibilityType type, CheckEligibilityStatus checkEligibilityStatus)
        {

            var request = _fixture.Create<ApplicationRequestData>();
            request.Type = type;
            request.ParentDateOfBirth = "1970-02-01";
            request.ChildDateOfBirth = "2007-02-01";
            request.ParentNationalInsuranceNumber = null;
            request.UserId = User.UserID;
            request.School = School.SchoolId;

            await AddHash(new CheckProcessData
            {
                DateOfBirth = request.ParentDateOfBirth,
                LastName = request.ParentLastName,
                Type = request.Type,
                NationalAsylumSeekerServiceNumber = request.ParentNationalAsylumSeekerServiceNumber,
                NationalInsuranceNumber = request.ParentNationalInsuranceNumber,

            }, checkEligibilityStatus);
            await _fakeInMemoryDb.SaveChangesAsync();
            return request;
        }



        private async Task ClearDownData()
        {
            _fakeInMemoryDb.Applications.RemoveRange(_fakeInMemoryDb.Applications);
            _fakeInMemoryDb.Schools.RemoveRange(_fakeInMemoryDb.Schools);
            _fakeInMemoryDb.LocalAuthorities.RemoveRange(_fakeInMemoryDb.LocalAuthorities);
            _fakeInMemoryDb.Users.RemoveRange(_fakeInMemoryDb.Users);
            _fakeInMemoryDb.SaveChanges();

            await _fakeInMemoryDb.SaveChangesAsync();
        }

    }
}