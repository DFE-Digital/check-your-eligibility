using AutoFixture;
using CheckYourEligibility.API.Domain.Constants;
using CheckYourEligibility.API.Boundary.Requests.DWP;
using CheckYourEligibility.API.Boundary.Responses.DWP;
using CheckYourEligibility.API.Gateways.Interfaces;
using CheckYourEligibility.API.UseCases;
using FluentAssertions;
using Moq;

namespace CheckYourEligibility.API.Tests.UseCases
{
    [TestFixture]
    public class MatchCitizenUseCaseTests : TestBase.TestBase
    {
        private Mock<ICheckEligibility> _mockGateway;
        private MatchCitizenUseCase _sut;

        [SetUp]
        public void Setup()
        {
            _mockGateway = new Mock<ICheckEligibility>(MockBehavior.Strict);
            _sut = new MatchCitizenUseCase(_mockGateway.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _mockGateway.VerifyAll();
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