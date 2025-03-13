using AutoFixture;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Requests.DWP;
using CheckYourEligibility.Domain.Responses.DWP;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Moq;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class MatchCitizenUseCaseTests : TestBase.TestBase
    {
        private Mock<ICheckEligibility> _mockService;
        private MatchCitizenUseCase _sut;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<ICheckEligibility>(MockBehavior.Strict);
            _sut = new MatchCitizenUseCase(_mockService.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _mockService.VerifyAll();
        }

        [Test]
        public async Task Execute_Should_Return_DwpMatchResponse_When_Citizen_Is_Eligible()
        {
            // Arrange
            var request = _fixture.Create<CitizenMatchRequest>();
            request.Data.Attributes.LastName = MogDWPValues.validCitizenSurnameEligible;

            var expectedResponse = new DwpMatchResponse
            {
                Data = new DwpMatchResponse.DwpResponse_Data
                {
                    Id = MogDWPValues.validCitizenEligibleGuid,
                    Type = "MatchResult",
                    Attributes = new DwpMatchResponse.DwpResponse_Attributes { MatchingScenario = "FSM" }
                },
                Jsonapi = new DwpMatchResponse.DwpResponse_Jsonapi { Version = "2.0" }
            };

            // Act
            var result = await _sut.Execute(request);

            // Assert
            result.Should().BeEquivalentTo(expectedResponse);
        }

        [Test]
        public async Task Execute_Should_Return_DwpMatchResponse_When_Citizen_Is_Not_Eligible()
        {
            // Arrange
            var request = _fixture.Create<CitizenMatchRequest>();
            request.Data.Attributes.LastName = MogDWPValues.validCitizenSurnameNotEligible;

            var expectedResponse = new DwpMatchResponse
            {
                Data = new DwpMatchResponse.DwpResponse_Data
                {
                    Id = MogDWPValues.validCitizenNotEligibleGuid,
                    Type = "MatchResult",
                    Attributes = new DwpMatchResponse.DwpResponse_Attributes { MatchingScenario = "FSM" }
                },
                Jsonapi = new DwpMatchResponse.DwpResponse_Jsonapi { Version = "2.0" }
            };

            // Act
            var result = await _sut.Execute(request);

            // Assert
            result.Should().BeEquivalentTo(expectedResponse);
        }

        [Test]
        public void Execute_Should_Throw_InvalidOperationException_When_Duplicates_Found()
        {
            // Arrange
            var request = _fixture.Create<CitizenMatchRequest>();
            request.Data.Attributes.LastName = MogDWPValues.validCitizenSurnameDuplicatesFound;

            // Act
            Func<Task> act = async () => await _sut.Execute(request);

            // Assert
            act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Duplicates found");
        }

        [Test]
        public async Task Execute_Should_Return_Null_When_Citizen_Is_Not_Found()
        {
            // Arrange
            var request = _fixture.Create<CitizenMatchRequest>();
            request.Data.Attributes.LastName = "Unknown";

            // Act
            var result = await _sut.Execute(request);

            // Assert
            result.Should().BeNull();
        }
    }
}