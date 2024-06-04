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
        private SchoolSearchService _sut;
        private School school;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EligibilityCheckContext>()
            .UseInMemoryDatabase(databaseName: "FakeInMemoryDb")
            .Options;

            _fakeInMemoryDb = new EligibilityCheckContext(options);
            if (!_fakeInMemoryDb.Schools.Any())
            {
                school = _fixture.Create<School>();
                _fakeInMemoryDb.Schools.Add(school);
                _fakeInMemoryDb.SaveChanges();
            }
           
            _sut = new SchoolSearchService(new NullLoggerFactory(), _fakeInMemoryDb);

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
            Action act = () => new SchoolSearchService(new NullLoggerFactory(), null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'dbContext')");
        }

        [Test]
        public async Task Given_Search_Should_Return_ExpectedResult()
        {
            // Arrange
            var expectedResult = school;
            // Act
            var response = await _sut.Search(expectedResult.EstablishmentName);

            // Assert

            response.FirstOrDefault().Name.Should().BeEquivalentTo(expectedResult.EstablishmentName);
        }
    }
}