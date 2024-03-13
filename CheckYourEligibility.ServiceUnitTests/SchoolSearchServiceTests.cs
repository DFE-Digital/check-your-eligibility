// Ignore Spelling: Levenshtein

using AutoFixture;
using AutoMapper;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CheckYourEligibility.ServiceUnitTests
{


    public class SchoolSearchServiceTests : TestBase.TestBase
    {
        private IEligibilityCheckContext _fakeInMemoryDb;
        private SchoolSearchService _sut;
        private Data.Models.School school;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EligibilityCheckContext>()
            .UseInMemoryDatabase(databaseName: "FakeInMemoryDb")
            .Options;

            _fakeInMemoryDb = new EligibilityCheckContext(options);

             school = _fixture.Create<Data.Models.School>();
            _fakeInMemoryDb.Schools.Add(school);
            _fakeInMemoryDb.SaveChangesAsync();

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
        public void Given_Search_Should_Return_ExpectedResult()
        {
            // Arrange
            var expectedResult = _fakeInMemoryDb.Schools.Select(x => new Domain.Responses.School()
            {
                Id = x.Urn,
                Name = x.EstablishmentName,
                Postcode = x.Postcode,
                Locality = x.Locality,
                County = x.County,
                Street = x.Street,
                Town = x.Town,
                La = x.LocalAuthority.LaName
            });

            // Act
            var response = _sut.Search(school.EstablishmentName);

            // Assert

            response.Result.Should().BeEquivalentTo(expectedResult);
        }
    }
}