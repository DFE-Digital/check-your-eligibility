// Ignore Spelling: Levenshtein

using AutoFixture;
using AutoMapper;
using Azure.Storage.Queues;
using CheckYourEligibility.Data.Enums;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CheckYourEligibility.ServiceUnitTests
{


    public class AdministrationServiceTests : TestBase.TestBase
    {
        private IEligibilityCheckContext _fakeInMemoryDb;
        private IMapper _mapper;
        private IConfiguration _configuration;
        private AdministrationService _sut;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EligibilityCheckContext>()
            .UseInMemoryDatabase(databaseName: "FakeInMemoryDb")
            .Options;

            _fakeInMemoryDb = new EligibilityCheckContext(options);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<EligibilityMappingProfile>());
            _mapper = config.CreateMapper();
            var configForSmsApi = new Dictionary<string, string>
            {
                {"QueueFsmCheckStandard", "notSet"},
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configForSmsApi)
                .Build();
            var webJobsConnection = "DefaultEndpointsProtocol=https;AccountName=none;AccountKey=none;EndpointSuffix=core.windows.net";


            _sut = new AdministrationService(new NullLoggerFactory(), _fakeInMemoryDb, _configuration);

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
            Action act = () => new AdministrationService(new NullLoggerFactory(), null, null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'dbContext')");
        }

        [Test]
        public void Given_CleanUpEligibilityChecks_Should_Return_Pass()
        {
            // Arrange

            // Act
            _sut.CleanUpEligibilityChecks();

            // Assert
            Assert.Pass();
        }
    }
}