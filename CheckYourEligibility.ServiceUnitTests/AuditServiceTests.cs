// Ignore Spelling: Levenshtein

using AutoFixture;
using AutoMapper;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services;
using CheckYourEligibility.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CheckYourEligibility.ServiceUnitTests
{


    public class AuditServiceTests : TestBase.TestBase
    {
        private IEligibilityCheckContext _fakeInMemoryDb;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private AuditService _sut;

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
         
            _sut = new AuditService(new NullLoggerFactory(), _fakeInMemoryDb, _mapper);

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
            Action act = () => new FsmApplicationService(new NullLoggerFactory(), null, _mapper,null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'dbContext')");
        }

        [Test]
        public void Given_validRequest_AuditAdd_Should_Return_New_Guid()
        {
            // Arrange
            var request = _fixture.Create<AuditData>();
           
            // Act
            var response = _sut.AuditAdd(request);

            // Assert
            response.Result.Should().BeOfType<String>();
        }

        [Test]
        public void Given_DB_Add_Should_ThrowException()
        {
            // Arrange
           var db= new Mock<IEligibilityCheckContext>(MockBehavior.Strict);
           var svc =  new AuditService(new NullLoggerFactory(), db.Object, _mapper);
            db.Setup(x => x.Audits.AddAsync(It.IsAny<Audit>(),  It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());
            var request = _fixture.Create<AuditData>();

            // Act
            Func<Task> act = async () => await svc.AuditAdd(request);

            // Assert
            act.Should().ThrowExactlyAsync<Exception>();
        }
    }
}