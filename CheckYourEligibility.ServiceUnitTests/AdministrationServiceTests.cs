// Ignore Spelling: Levenshtein

using AutoFixture;
using AutoMapper;
using Azure.Core;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Services;
using CheckYourEligibility.Services.CsvImport;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Resources;

namespace CheckYourEligibility.ServiceUnitTests
{

    [ExcludeFromCodeCoverage]
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

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
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

        [Test]
        public void Given_ImportEstablishments_Should_Return_Pass()
        {
            var data = _fixture.CreateMany<EstablishmentRow>().ToList();
            //Make a duplicate la
            var existingData = data.First();
            var la = new LocalAuthority
            {
                LocalAuthorityId = existingData.LaCode,
                LaName = existingData.LaName
            };
            _fakeInMemoryDb.LocalAuthorities.Add(la);
            _fakeInMemoryDb.Establishments.Add(new Establishment { EstablishmentId =  existingData.Urn, EstablishmentName = existingData.EstablishmentName, LocalAuthority = la,
                County = existingData.County, Postcode = existingData.Postcode, Locality = existingData.Locality, Street = existingData.Street,Town = existingData.Town, StatusOpen = true,
                Type = existingData.Type });

            _fakeInMemoryDb.SaveChanges();

            // Act
            _sut.ImportEstablishments(data);

            // Assert
            Assert.Pass();
        }

        /// <summary>
        /// Calling multiple times will generate concurrency errors, which is a limitation of in memory db
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task Given_ImportEstablishments_DuplicatesShould_Return_Pass()
        {
            // Arrange
            var data = _fixture.CreateMany<EstablishmentRow>();

            // Act
            await _sut.ImportEstablishments(data);

            // Assert
            Assert.Pass();
        }

        [Test]
        public void Given_ImportHomeOfficeData_Should_Return_Pass()
        {
            // Arrange
            var data = _fixture.CreateMany<FreeSchoolMealsHO>();

            // Act
            _sut.ImportHomeOfficeData(data);

            // Assert
            Assert.Pass();
        }

        [Test]
        public void Given_ImportHMRCData_Should_Return_Pass()
        {
            // Arrange
            var data = _fixture.CreateMany<FreeSchoolMealsHMRC>();

            // Act
            _sut.ImportHMRCData(data);

            // Assert
            Assert.Pass();
        }

    }
}