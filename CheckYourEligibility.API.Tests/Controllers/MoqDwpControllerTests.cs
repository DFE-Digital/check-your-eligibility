using AutoFixture;
using CheckYourEligibility.API.Domain.Constants;
using CheckYourEligibility.API.Boundary.Requests.DWP;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Boundary.Responses.DWP;
using CheckYourEligibility.API.Controllers;
using CheckYourEligibility.API.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace CheckYourEligibility.API.Tests
{
    public class MoqDwpControllerTests : TestBase.TestBase
    {
        private Mock<IMatchCitizenUseCase> _mockMatchCitizenUseCase;
        private Mock<IGetCitizenClaimsUseCase> _mockGetCitizenClaimsUseCase;
        private ILogger<MoqDWPController> _mockLogger;
        private MoqDWPController _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockMatchCitizenUseCase = new Mock<IMatchCitizenUseCase>(MockBehavior.Strict);
            _mockGetCitizenClaimsUseCase = new Mock<IGetCitizenClaimsUseCase>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<MoqDWPController>>();
            _sut = new MoqDWPController(_mockLogger, _mockMatchCitizenUseCase.Object, _mockGetCitizenClaimsUseCase.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockMatchCitizenUseCase.VerifyAll();
            _mockGetCitizenClaimsUseCase.VerifyAll();
        }

        [Test]
        public async Task Given_valid_Request_PostCitizenMatch_Should_Return_Status200OK()
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

            _mockMatchCitizenUseCase.Setup(m => m.Execute(request)).ReturnsAsync(expectedResponse);

            var expectedResult = new ObjectResult(expectedResponse)
            {
                StatusCode = StatusCodes.Status200OK
            };

            // Act
            var response = await _sut.Match(request);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_valid_Request_PostCitizenMatch_Should_Return_Status422UnprocessableEntity()
        {
            // Arrange
            var request = _fixture.Create<CitizenMatchRequest>();
            request.Data.Attributes.LastName = MogDWPValues.validCitizenSurnameDuplicatesFound;

            _mockMatchCitizenUseCase.Setup(m => m.Execute(request)).ThrowsAsync(new InvalidOperationException("Duplicates found"));

            // Act
            var response = await _sut.Match(request);

            // Assert
            response.Should().BeOfType<UnprocessableEntityResult>();
        }

        [Test]
        public async Task Given_InValidRequest_Match_Should_Return_Status404NotFoundResult()
        {
            // Arrange
            var request = new CitizenMatchRequest();

            _mockMatchCitizenUseCase.Setup(m => m.Execute(request)).ReturnsAsync((DwpMatchResponse)null);

            // Act
            var response = await _sut.Match(request);

            // Assert
            response.Should().BeOfType<NotFoundResult>();
        }

        [Test]
        public async Task Given_valid_Request_Claim_Should_Return_Status200OK()
        {
            // Arrange
            var guid = MogDWPValues.validCitizenEligibleGuid;
            var benefitType = DwpBenefitType.pensions_credit.ToString();
            var expectedResponse = _fixture.Create<DwpClaimsResponse>();

            _mockGetCitizenClaimsUseCase.Setup(m => m.Execute(guid, benefitType)).ReturnsAsync(expectedResponse);

            var expectedResult = new ObjectResult(expectedResponse)
            {
                StatusCode = StatusCodes.Status200OK
            };

            // Act
            var response = await _sut.Claim(guid, benefitType);

            // Assert
            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Given_InValidRequest_Claim_Should_Return_Status400BadRequest()
        {
            // Arrange
            var guid = MogDWPValues.inValidCitizenGuid;
            var benefitType = "invalid";

            _mockGetCitizenClaimsUseCase.Setup(m => m.Execute(guid, benefitType)).ThrowsAsync(new ArgumentException("Invalid GUID"));

            // Act
            var response = await _sut.Claim(guid, benefitType);

            // Assert
            response.Should().BeOfType<BadRequestResult>();
        }

        [Test]
        public async Task Given_Valid_Request_With_Non_Eligible_GUID_Should_Return_NotFoundResult()
        {
            // Arrange
            var guid = MogDWPValues.validCitizenNotEligibleGuid;
            var benefitType = DwpBenefitType.pensions_credit.ToString();

            _mockGetCitizenClaimsUseCase.Setup(m => m.Execute(guid, benefitType)).ReturnsAsync((DwpClaimsResponse)null);

            // Act
            var response = await _sut.Claim(guid, benefitType);

            // Assert
            response.Should().BeOfType<NotFoundResult>();
        }
    }
}