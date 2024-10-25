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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework.Internal;

namespace CheckYourEligibility.APIUnitTests
{
    public class MoqDwpControllerTests : TestBase.TestBase
    {
        private IConfigurationRoot _configuration;
        private Mock<ICheckEligibility> _mockService;
        private ILogger<EligibilityCheckController> _mockLogger;
        private MoqDWPController _sut;

        [SetUp]
        public void Setup()
        {
            var configForBulkUpload = new Dictionary<string, string>
            {
                {"BulkEligibilityCheckLimit", "5"},
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configForBulkUpload)
                .Build();
            _mockService = new Mock<ICheckEligibility>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<EligibilityCheckController>>();
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
            ICheckEligibility checkService = null;
            IApplication applicationService = null;
            IAudit auditService = null;
          

            // Act
            Action act = () => new EligibilityCheckController(_mockLogger, checkService,  auditService,_configuration);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'checkService')");
        }

        [Test]
        public void Given_valid_Request_PostCitizenMatch_Should_Return_Status200OK()
        {
            // Arrange
            var request = _fixture.Create<CitizenMatchRequest>();
            request.Data.Attributes.DateOfBirth = MogDWPValues.validCitizenDob;
            request.Data.Attributes.LastName = MogDWPValues.validCitizenSurnameEligible;
            request.Data.Attributes.NinoFragment = MogDWPValues.validCitizenNino;

            var expectedResult = new ObjectResult(new DwpMatchResponse()
            {
                Data = new DwpMatchResponse.DwpResponse_Data
                {
                    Id = MogDWPValues.validCitizenEligibleGuid,
                    Type = "MatchResult",
                    Attributes = new DwpMatchResponse.DwpResponse_Attributes { MatchingScenario = "FSM" }
                }
                    ,
                Jsonapi = new DwpMatchResponse.DwpResponse_Jsonapi { Version = "2.0" }
            })
            { StatusCode = StatusCodes.Status200OK };

            // Act
            var response = _sut.Match(request);

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }


        [Test]
        public void Given_valid_Request_PostCitizenMatch_Should_Return_Status422UnprocessableEntity()
        {
            // Arrange
            var request = _fixture.Create<CitizenMatchRequest>();
            request.Data.Attributes.DateOfBirth = MogDWPValues.validCitizenDob;
            request.Data.Attributes.LastName = MogDWPValues.validCitizenSurnameDuplicatesFound;
            request.Data.Attributes.NinoFragment = MogDWPValues.validCitizenNino;

            var expectedResult = new ObjectResult(new DwpMatchResponse()
            {
                Data = new DwpMatchResponse.DwpResponse_Data
                {
                    Id = MogDWPValues.validCitizenEligibleGuid,
                    Type = "MatchResult",
                    Attributes = new DwpMatchResponse.DwpResponse_Attributes { MatchingScenario = "FSM" }
                }
                    ,
                Jsonapi = new DwpMatchResponse.DwpResponse_Jsonapi { Version = "2.0" }
            })
            { StatusCode = StatusCodes.Status422UnprocessableEntity };

            // Act
            var response = _sut.Match(request);

            // Assert
            response.Result.Should().BeOfType(typeof(Microsoft.AspNetCore.Mvc.UnprocessableEntityResult));
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
            var response = _sut.Claim(MogDWPValues.validCitizenEligibleGuid, DwpBenefitType.pensions_credit.ToString());

            // Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Given_InValidRequest_Claim_Should_Return_Status400BadRequest()
        {
            // Arrange
            // Act
            var response = _sut.Claim(MogDWPValues.inValidCitizenGuid, "invalid");

            // Assert
            response.Result.Should().BeOfType(typeof(BadRequestResult));
        }

        [Test]
        public void Given_Valid_Request_With_Non_Eligible_GUID_Should_Return_NotFoundResult()
        {
            //Arrange
            var expectedResult = new NotFoundResult();
            //Act
            var response = _sut.Claim(MogDWPValues.validCitizenNotEligibleGuid, DwpBenefitType.pensions_credit.ToString());

            //Assert
            response.Result.Should().BeEquivalentTo(expectedResult);
        }
    }
}