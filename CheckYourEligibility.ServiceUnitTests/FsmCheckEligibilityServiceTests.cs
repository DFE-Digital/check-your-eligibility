using AutoFixture;
using AutoMapper;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CheckYourEligibility.ServiceUnitTests
{


    public class FsmCheckEligibilityServiceTests : TestBase.TestBase
    {
        private Mock<IEligibilityCheckContext> _mockDb;
        private IMapper _mapper;
        private FsmCheckEligibilityService _sut;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EligibilityCheckContext>()
            .UseInMemoryDatabase(databaseName: "MovieListDatabase")
            .Options;
            _mockDb = new Mock<IEligibilityCheckContext>(MockBehavior.Strict);
           
             var config = new MapperConfiguration(cfg => cfg.AddProfile<EligibilityMappingProfile>());
            _mapper = config.CreateMapper();
            _sut = new FsmCheckEligibilityService(new NullLoggerFactory(), _mockDb.Object, _mapper);
        }

        [TearDown]
        public void Teardown()
        {
            _mockDb.VerifyAll();
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
            var request = _fixture.Create<CheckEligibilityRequestData>();
            request.DateOfBirth = "01/02/1970";
            var data = _fixture.Create<FsmCheckEligibility>();
            var settings = new List<FsmCheckEligibility>() { data};
            var id = _fixture.Create<string>();
            
            _mockDb
    .Setup(_ => _.FsmCheckEligibilities.AddAsync(It.IsAny<FsmCheckEligibility>(), It.IsAny<CancellationToken>()))
    .Callback((FsmCheckEligibility model, CancellationToken token) => { settings.Add(model); })
    .Returns((FsmCheckEligibility model, CancellationToken token) => Task.FromResult(data));



            _mockDb.Setup(c => c.SaveChangesAsync());

            // Act
            var response = _sut.PostCheck(request);

            // Assert
            response.Result.Should().Be(id);
        }
    }
}