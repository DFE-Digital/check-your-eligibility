// Ignore Spelling: Levenshtein

using AutoMapper;
using Azure.Core;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;
using System.Resources;

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
            // Arrange
            var content = Properties.Resources.small_gis;
            var fileName = "test.pdf";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            //create FormFile with desired data
            IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);


            // Act
            _sut.ImportEstablishments(file);

            // Assert
            Assert.Pass();
        }

        [Test]
        public void Given_ImportEstablishments_BadContentShould_Return_Fail()
        {
            // Arrange
            var content = "BadContent";
            var fileName = "test.pdf";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            //create FormFile with desired data
            IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);

            // Act
            Func<Task> act = async () => await _sut.ImportEstablishments(file);

            // Assert
            act.Should().ThrowExactlyAsync<Exception>();
        }

        
        /// <summary>
        /// Calling multiple times will generate concurrency errors, which is a limitation of in memory db
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task Given_ImportEstablishments_DuplicatesShould_Return_Pass()
        {
            // Arrange
            var content = Properties.Resources.small_gis;
            var fileName = "test.xls";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            //create FormFile with desired data
            IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);

            // Act
            await _sut.ImportEstablishments(file);

            // Assert
            Assert.Pass();
        }
    }
}