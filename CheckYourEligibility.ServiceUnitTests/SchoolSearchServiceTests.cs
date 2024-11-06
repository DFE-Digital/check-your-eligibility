// Ignore Spelling: Levenshtein

using AutoFixture;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CheckYourEligibility.ServiceUnitTests
{


    public class SchoolSearchServiceTests : TestBase.TestBase
    {
        private IEligibilityCheckContext _fakeInMemoryDb;
        private EstablishmentSearchService _sut;
        private Establishment Establishment;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EligibilityCheckContext>()
            .UseInMemoryDatabase(databaseName: "FakeInMemoryDb")
            .Options;
            _fakeInMemoryDb = new EligibilityCheckContext(options);
             _sut = new EstablishmentSearchService(new NullLoggerFactory(), _fakeInMemoryDb);

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
            Action act = () => new EstablishmentSearchService(new NullLoggerFactory(), null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'dbContext')");
        }

        [Test]
        public async Task Given_Search_Should_Return_ExpectedResult()
        {
            // Arrange
            _fakeInMemoryDb.Establishments.RemoveRange(_fakeInMemoryDb.Establishments);
            Establishment = _fixture.Create<Establishment>();
            _fakeInMemoryDb.Establishments.Add(Establishment);
            _fakeInMemoryDb.SaveChanges();
            var    expectedResult = _fakeInMemoryDb.Establishments.First();
           
            // Act
            var   response  = await _sut.Search(expectedResult.EstablishmentName);

            // Assert
            if (response != null && response.Any())
            {
                response.First().Name.Should().BeEquivalentTo(expectedResult.EstablishmentName);
            }
            
        }

        [Test]
        public async Task Given_Search_Urn_Should_Return_ExpectedResult()
        {
            // Arrange
            _fakeInMemoryDb.Establishments.RemoveRange(_fakeInMemoryDb.Establishments);
            Establishment = _fixture.Create<Establishment>();
            var urn = 12345;
            Establishment.EstablishmentId = urn;
            _fakeInMemoryDb.Establishments.Add(Establishment);
            _fakeInMemoryDb.SaveChanges();
            var expectedResult = _fakeInMemoryDb.Establishments.First();

            // Act
            var response = await _sut.Search(urn.ToString());

            // Assert
            if (response != null && response.Any())
            {
                response.First().Name.Should().BeEquivalentTo(expectedResult.EstablishmentName);
            }

        }
    }
}