using AutoFixture;
using AutoMapper;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CheckYourEligibility.ServiceUnitTests
{


    public class FsmCheckEligibilityServiceTests : TestBase.TestBase
    {
        //private Mock<IEligibilityCheckContext> _mockDb;
        private IEligibilityCheckContext _fakeInMemoryDb;
        private IMapper _mapper;
        private FsmCheckEligibilityService _sut;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EligibilityCheckContext>()
            .UseInMemoryDatabase(databaseName: "FakeInMemoryDb")
            .Options;

            _fakeInMemoryDb = new EligibilityCheckContext(options);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<EligibilityMappingProfile>());
            _mapper = config.CreateMapper();
           _sut = new FsmCheckEligibilityService(new NullLoggerFactory(), _fakeInMemoryDb, _mapper);

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
            Action act = () => new FsmCheckEligibilityService(new NullLoggerFactory(), null,_mapper);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'dbContext')");
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
            var item = _fixture.Create<FsmCheckEligibility>();
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.GetStatus(item.FsmCheckEligibilityID);

            // Assert
            _ = response.Result.Data.Status.Should().BeEquivalentTo(item.Status.ToString());
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
            var item = _fixture.Create<FsmCheckEligibility>();
            item.NASSNumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();
            
            // Act
            var response = _sut.ProcessCheck(item.FsmCheckEligibilityID);

            // Assert
            response.Result.Data.Status.Should().BeEquivalentTo(FsmCheckEligibilityStatus.parentNotFound.ToString());
        }

        [Test]
        public void Given_validRequest_HMRC_InvalidNI_Process_Should_Return_updatedStatus_parentNotFound()
        {
            // Arrange
            var item = _fixture.Create<FsmCheckEligibility>();
            item.NASSNumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.ProcessCheck(item.FsmCheckEligibilityID);

            // Assert
            response.Result.Data.Status.Should().BeEquivalentTo(FsmCheckEligibilityStatus.parentNotFound.ToString());
        }

        [Test]
        public void Given_validRequest_HO_InvalidNASS_Process_Should_Return_updatedStatus_parentNotFound()
        {
            // Arrange
            var item = _fixture.Create<FsmCheckEligibility>();
            item.NINumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.ProcessCheck(item.FsmCheckEligibilityID);

            // Assert
            response.Result.Data.Status.Should().BeEquivalentTo(FsmCheckEligibilityStatus.parentNotFound.ToString());
        }

        [Test]
        public void Given_validRequest_HMRC_Process_Should_Return_updatedStatus_eligible()
        {
            // Arrange
            var item = _fixture.Create<FsmCheckEligibility>();
            item.NASSNumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.FreeSchoolMealsHMRC.Add(new FreeSchoolMealsHMRC { FreeSchoolMealsHMRCID= item.NINumber,Surname = item.LastName, DateOfBirth = item.DateOfBirth });
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.ProcessCheck(item.FsmCheckEligibilityID);

            // Assert
            response.Result.Data.Status.Should().BeEquivalentTo(FsmCheckEligibilityStatus.eligible.ToString());
        }

        [Test]
        public void Given_validRequest_HO_Process_Should_Return_updatedStatus_eligible()
        {
            // Arrange
            var item = _fixture.Create<FsmCheckEligibility>();
            item.NINumber = string.Empty;
            _fakeInMemoryDb.FsmCheckEligibilities.Add(item);
            _fakeInMemoryDb.FreeSchoolMealsHO.Add(new FreeSchoolMealsHO {FreeSchoolMealsHOID ="123", NASS  = item.NASSNumber, LastName = item.LastName, DateOfBirth = item.DateOfBirth });
            _fakeInMemoryDb.SaveChangesAsync();

            // Act
            var response = _sut.ProcessCheck(item.FsmCheckEligibilityID);

            // Assert
            response.Result.Data.Status.Should().BeEquivalentTo(FsmCheckEligibilityStatus.eligible.ToString());
        }
    }
}