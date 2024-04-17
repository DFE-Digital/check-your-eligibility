using AutoFixture;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Requests.DWP;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Domain.Responses.DWP;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework.Internal;

namespace CheckYourEligibility.APIUnitTests
{
    public class MoqDwpControllerTests : TestBase.TestBase
    {
        private Mock<IFsmCheckEligibility> _mockService;
        private ILogger<FreeSchoolMealsController> _mockLogger;
        private MoqDWPController _sut;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<IFsmCheckEligibility>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<FreeSchoolMealsController>>();
            _sut = new MoqDWPController(_mockLogger, _mockService.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _mockService.VerifyAll();
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_service_is_null()
        {
            // Arrange
            IFsmCheckEligibility service = null;

            // Act
            Action act = () => new FreeSchoolMealsController(_mockLogger, service);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'service')");
        }

        [Test]
        public void Given_valid_Request_PostCitizenMatch_Should_Return_Status200OK()
        {
            // Arrange
            var request = _fixture.Create<CitizenMatchRequest>();
            request.Data.Attributes.DateOfBirth = MogDWPValues.validCitizenDob;
            request.Data.Attributes.LastName = MogDWPValues.validCitizenSurnameEligible;
            request.Data.Attributes.NinoFragment = MogDWPValues.validCitizenNino;

            var expectedResult = new ObjectResult(new DwpResponse()
            {
                Data = new DwpResponse.DwpResponse_Data
                {
                    Id = MogDWPValues.validCitizenEligibleGuid,
                    Type = "MatchResult",
                    Attributes = new DwpResponse.DwpResponse_Attributes { MatchingScenario = "FSM" }
                }
                    ,
                Jsonapi = new DwpResponse.DwpResponse_Jsonapi { Version = "2.0" }
            })
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.Match(request);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_Match_Should_Return_Status404NotFoundResult()
        {
            // Arrange
            var request = new CitizenMatchRequest();

            // Act
            var response = _sut.Match(request);

            // Assert
            response.Result.Should().BeOfType(typeof(NotFoundResult));
        }

        [Test]
        public void Given_valid_Request_Claim_Should_Return_Status200OK()
        {
            // Arrange
            var expectedResult = new OkResult();

            // Act
            var response = _sut.Claim(MogDWPValues.validCitizenEligibleGuid, MogDWPValues.validUniversalBenefitType);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_Claim_Should_Return_Status400BadRequest()
        {
            // Arrange
            // Act
            var response = _sut.Claim(MogDWPValues.validCitizenEligibleGuid, "invalid");

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestResult));
        }
    }
}