// Ignore Spelling: Levenshtein

using AutoFixture;
using AutoMapper;
using CheckYourEligibility.Data.Mappings;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services;
using CheckYourEligibility.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using String = System.String;

namespace CheckYourEligibility.ServiceUnitTests
{


    public class DwpServiceTests : TestBase.TestBase
    {
        // private IEligibilityCheckContext _fakeInMemoryDb;
        private IConfiguration _configuration;
        private DwpService _sut;
        private HttpClient httpClient;

        [SetUp]
        public void Setup()
        {
            httpClient = new HttpClient();
            var options = new DbContextOptionsBuilder<EligibilityCheckContext>()
            .UseInMemoryDatabase(databaseName: "FakeInMemoryDb")
            .Options;

            var config = new MapperConfiguration(cfg => cfg.AddProfile<FsmMappingProfile>());
            var configForSmsApi = new Dictionary<string, string>
            {
                {"Dwp:UniversalCreditThreshhold-1","616.66" },
                {"Dwp:UniversalCreditThreshhold-2","1233.33" },
                {"Dwp:UniversalCreditThreshhold-3","1849.99" }

            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configForSmsApi)
                .Build();
            var webJobsConnection = "DefaultEndpointsProtocol=https;AccountName=none;AccountKey=none;EndpointSuffix=core.windows.net";

            _sut = new DwpService(new NullLoggerFactory(), httpClient, _configuration);
        }

        [TearDown]
        public void Teardown()
        {
        }

        [Test]
        public void Given_Claims_have_pensions_credit_CheckBenefitEntitlement_Should_Return_true()
        {
            // Arrange
            var citizenGuid = Guid.NewGuid().ToString();
            var request = _fixture.Create<DwpClaimsResponse>();
            request.data[0].attributes.benefitType = DwpBenefitType.pensions_credit.ToString();
            request.data[0].attributes.status = DwpService.decision_entitled;
            // Act
            var response = _sut.CheckBenefitEntitlement(citizenGuid, request);

            // Assert
            response.Should().Be(true);
        }

        [Test]
        public void Given_Claims_have_pensions_credit_CheckBenefitEntitlement_Should_Return_false()
        {
            // Arrange
            var citizenGuid = Guid.NewGuid().ToString();
            var request = _fixture.Create<DwpClaimsResponse>();
            request.data[0].attributes.benefitType = DwpBenefitType.pensions_credit.ToString();
            request.data[0].attributes.status = "not entitled";
            // Act
            var response = _sut.CheckBenefitEntitlement(citizenGuid, request);

            // Assert
            response.Should().Be(false);
        }

        [Test]
        public void Given_Claims_have_job_seekers_allowance_income_based_CheckBenefitEntitlement_Should_Return_true()
        {
            // Arrange
            var citizenGuid = Guid.NewGuid().ToString();
            var request = _fixture.Create<DwpClaimsResponse>();
            request.data[0].attributes.benefitType = DwpBenefitType.job_seekers_allowance_income_based.ToString();
            request.data[0].attributes.status = DwpService.decision_entitled;
            // Act
            var response = _sut.CheckBenefitEntitlement(citizenGuid, request);

            // Assert
            response.Should().Be(true);
        }

        [Test]
        public void Given_Claims_have_income_support_CheckBenefitEntitlement_Should_Return_true()
        {
            // Arrange
            var citizenGuid = Guid.NewGuid().ToString();
            var request = _fixture.Create<DwpClaimsResponse>();
            request.data[0].attributes.benefitType = DwpBenefitType.income_support.ToString();
            request.data[0].attributes.status = DwpService.decision_entitled;
            // Act
            var response = _sut.CheckBenefitEntitlement(citizenGuid, request);

            // Assert
            response.Should().Be(true);
        }

        [Test]
        public void Given_Claims_have_employment_support_allowance_income_based_CheckBenefitEntitlement_Should_Return_true()
        {
            // Arrange
            var citizenGuid = Guid.NewGuid().ToString();
            var request = _fixture.Create<DwpClaimsResponse>();
            request.data[0].attributes.benefitType = DwpBenefitType.employment_support_allowance_income_based.ToString();
            request.data[0].attributes.status = DwpService.decision_entitled;
            // Act
            var response = _sut.CheckBenefitEntitlement(citizenGuid, request);

            // Assert
            response.Should().Be(true);
        }

        /// <summary>
        /// UC1 616.66 One instance of an award with status live above threshold
        /// </summary>
        [Test]
        public void Given_Claims_have_universal_credit_CheckBenefitEntitlement_1_Should_Return_false()
        {
            // Arrange
            var citizenGuid = Guid.NewGuid().ToString();
            var request = _fixture.Create<DwpClaimsResponse>();
            request.data[0].attributes.benefitType = DwpBenefitType.universal_credit.ToString();
            request.data[0].attributes.status = DwpService.statusInPayment;
            request.data[0].attributes.awards = new List<Award> {
                new Award(){endDate= "2022-05-29", startDate = "2022-04-30",
            status = DwpService.awardStatusLive, assessmentAttributes = new AssessmentAttributes{takeHomePay = 10000 }
            }
            };


            // Act
            var response = _sut.CheckBenefitEntitlement(citizenGuid, request);

            // Assert
            response.Should().Be(false);
        }

        /// <summary>
        /// UC1 616.66 One instance of an award with status live within threshold
        /// </summary>
        [Test]
        public void Given_Claims_have_universal_credit_CheckBenefitEntitlement_1_Should_Return_true()
        {
            // Arrange
            var citizenGuid = Guid.NewGuid().ToString();
            var request = _fixture.Create<DwpClaimsResponse>();
            request.data[0].attributes.benefitType = DwpBenefitType.universal_credit.ToString();
            request.data[0].attributes.status = DwpService.statusInPayment;
            request.data[0].attributes.awards = new List<Award> { new Award(){
                endDate= "2022-04-29", startDate = "2022-03-30",
            status = DwpService.awardStatusLive,
                assessmentAttributes = new AssessmentAttributes{takeHomePay = 500 }
            }
            };


            // Act
            var response = _sut.CheckBenefitEntitlement(citizenGuid, request);

            // Assert
            response.Should().Be(true);
        }


        /// <summary>
        /// UC2 1233.33 two instance of an award with status live above threshold
        /// </summary>
        [Test]
        public void Given_Claims_have_universal_credit_CheckBenefitEntitlement_2_Should_Return_false()
        {
            // Arrange
            var citizenGuid = Guid.NewGuid().ToString();
            var request = _fixture.Create<DwpClaimsResponse>();
            request.data[0].attributes.benefitType = DwpBenefitType.universal_credit.ToString();
            request.data[0].attributes.status = DwpService.statusInPayment;
            request.data[0].attributes.awards = new List<Award> { new Award(){
                endDate= "2022-04-29", startDate = "2022-03-30",
            status = DwpService.awardStatusLive,
                assessmentAttributes = new AssessmentAttributes{takeHomePay = 5000 }
            }, new Award(){endDate= "2022-05-29", startDate = "2022-04-30",
            status = DwpService.awardStatusLive,
                assessmentAttributes = new AssessmentAttributes{takeHomePay = 5000 }
            }
            };


            // Act
            var response = _sut.CheckBenefitEntitlement(citizenGuid, request);

            // Assert
            response.Should().Be(false);
        }

        /// <summary>
        /// UC1 1233.33 two instance of an award with status live within threshold
        /// </summary>
        [Test]
        public void Given_Claims_have_universal_credit_CheckBenefitEntitlement_2_Should_Return_true()
        {
            // Arrange
            var citizenGuid = Guid.NewGuid().ToString();
            var request = _fixture.Create<DwpClaimsResponse>();
            request.data[0].attributes.benefitType = DwpBenefitType.universal_credit.ToString();
            request.data[0].attributes.status = DwpService.statusInPayment;
            request.data[0].attributes.awards = new List<Award> { new Award(){
                endDate= "2022-04-29", startDate = "2022-03-30",
            status = DwpService.awardStatusLive,
                assessmentAttributes = new AssessmentAttributes{takeHomePay = 100 }
            }, new Award(){endDate= "2022-05-29", startDate = "2022-04-30",
            status = DwpService.awardStatusLive,
                assessmentAttributes = new AssessmentAttributes{takeHomePay = 500 }
            }
            };


            // Act
            var response = _sut.CheckBenefitEntitlement(citizenGuid, request);

            // Assert
            response.Should().Be(true);
        }

        /// <summary>
        /// UC2 1849.99 two instance of an award with status live above threshold
        /// </summary>
        [Test]
        public void Given_Claims_have_universal_credit_CheckBenefitEntitlement_3_Should_Return_false()
        {
            // Arrange
            var citizenGuid = Guid.NewGuid().ToString();
            var request = _fixture.Create<DwpClaimsResponse>();
            request.data[0].attributes.benefitType = DwpBenefitType.universal_credit.ToString();
            request.data[0].attributes.status = DwpService.statusInPayment;
            request.data[0].attributes.awards = new List<Award> { new Award(){
                endDate= "2022-04-29", startDate = "2022-03-30",
            status = DwpService.awardStatusLive,
                assessmentAttributes = new AssessmentAttributes{takeHomePay = 5000 }
            }, new Award(){endDate= "2022-05-29", startDate = "2022-04-30",
            status = DwpService.awardStatusLive,
                assessmentAttributes = new AssessmentAttributes{takeHomePay = 5000 }
            },
            new Award(){endDate= "2022-05-29", startDate = "2022-04-30",
            status = DwpService.awardStatusLive,
                assessmentAttributes = new AssessmentAttributes{takeHomePay = 5000 }
            }
            };


            // Act
            var response = _sut.CheckBenefitEntitlement(citizenGuid, request);

            // Assert
            response.Should().Be(false);
        }

        /// <summary>
        /// UC1 1849.99 two instance of an award with status live within threshold
        /// </summary>
        [Test]
        public void Given_Claims_have_universal_credit_CheckBenefitEntitlement_3_Should_Return_true()
        {
            // Arrange
            var citizenGuid = Guid.NewGuid().ToString();
            var request = _fixture.Create<DwpClaimsResponse>();
            request.data[0].attributes.benefitType = DwpBenefitType.universal_credit.ToString();
            request.data[0].attributes.status = DwpService.statusInPayment;
            request.data[0].attributes.awards = new List<Award> { new Award(){
                endDate= "2022-04-29", startDate = "2022-03-30",
            status = DwpService.awardStatusLive,
                assessmentAttributes = new AssessmentAttributes{takeHomePay = 100 }
            }, new Award(){endDate= "2022-05-29", startDate = "2022-04-30",
            status = DwpService.awardStatusLive,
                assessmentAttributes = new AssessmentAttributes{takeHomePay = 500 }
            },
            new Award(){endDate= "2022-05-29", startDate = "2022-04-30",
            status = DwpService.awardStatusLive,
                assessmentAttributes = new AssessmentAttributes{takeHomePay = 100 }
            }
            };


            // Act
            var response = _sut.CheckBenefitEntitlement(citizenGuid, request);

            // Assert
            response.Should().Be(true);
        }
    }
}