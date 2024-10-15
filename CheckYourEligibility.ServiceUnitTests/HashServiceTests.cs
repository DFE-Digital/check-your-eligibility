// Ignore Spelling: Levenshtein

using AutoFixture;
using AutoMapper;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services;
using CheckYourEligibility.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetTopologySuite.Index.HPRtree;
using Newtonsoft.Json;
using System.Globalization;
using String = System.String;

namespace CheckYourEligibility.ServiceUnitTests
{


    public class HashServiceTests : TestBase.TestBase
    {
        private IEligibilityCheckContext _fakeInMemoryDb;
        private IConfiguration _configuration;
        private HashService _sut;
        private Mock<IAudit> _moqAudit;
        private int _hashCheckDays = 7;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EligibilityCheckContext>()
            .UseInMemoryDatabase(databaseName: "FakeInMemoryDb")
            .Options;

            _fakeInMemoryDb = new EligibilityCheckContext(options);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            var configForSmsApi = new Dictionary<string, string>
            {
                {"QueueFsmCheckStandard", "notSet"},
                {"HashCheckDays", _hashCheckDays.ToString()},

            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configForSmsApi)
                .Build();
            var webJobsConnection = "DefaultEndpointsProtocol=https;AccountName=none;AccountKey=none;EndpointSuffix=core.windows.net";
            _moqAudit = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new HashService(new NullLoggerFactory(), _fakeInMemoryDb, _configuration, _moqAudit.Object);
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
            Action act = () => new HashService(new NullLoggerFactory(), null, null, null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'dbContext')");
        }

        [Test]
        public async Task Given_validRequest_Create_Exists_Should_Return_Hash()
        {
            // Arrange
            var request = _fixture.Create<EligibilityCheck>();
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var dataItem = GetCheckProcessData(fsm);

            // Act
            var id = await _sut.Create(dataItem, Domain.Enums.CheckEligibilityStatus.parentNotFound, Domain.Enums.ProcessEligibilityCheckSource.HMRC, new AuditData());
            await _fakeInMemoryDb.SaveChangesAsync();

            var response = await _sut.Exists(dataItem);

            // Assert
            response.Should().BeOfType<EligibilityCheckHash>();
        }

        [Test]
        public async Task Given_HashIsOld_Exists_Should_Return_null()
        {
            // Arrange
            var request = _fixture.Create<EligibilityCheck>();
            _moqAudit.Setup(x => x.AuditAdd(It.IsAny<AuditData>())).ReturnsAsync("");
            var fsm = _fixture.Create<CheckEligibilityRequestData_Fsm>();
            fsm.DateOfBirth = "1990-01-01";
            var dataItem = GetCheckProcessData(fsm);


            var id = await _sut.Create(dataItem, Domain.Enums.CheckEligibilityStatus.parentNotFound, Domain.Enums.ProcessEligibilityCheckSource.HMRC, new AuditData());
            await _fakeInMemoryDb.SaveChangesAsync();
            var hashItem = _fakeInMemoryDb.EligibilityCheckHashes.First(x => x.EligibilityCheckHashID.Equals(id));
                        hashItem.TimeStamp = hashItem.TimeStamp.AddDays(-(_hashCheckDays + 1));
            await _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = await _sut.Exists(dataItem);

            // Assert
            response.Should().BeNull();
        }

        private CheckProcessData GetCheckProcessData(CheckEligibilityRequestData_Fsm request)
        {
            return new CheckProcessData
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