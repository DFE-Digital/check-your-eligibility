using AutoFixture;
using CheckYourEligibility.Domain;
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
using System.Collections.Generic;

namespace CheckYourEligibility.APIUnitTests
{
    public class LoginControllerTests : TestBase.TestBase
    {
        private IConfigurationRoot _configuration;
        private ILogger<LoginController> _mockLogger;
        private LoginController _sut;
        private SystemUser validUser = new SystemUser { Username = "Test", Password = "letmein" };

        [SetUp]
        public void Setup()
        {
                 var configUser = new Dictionary<string, string>
            {
                {$"Jwt:Users:{validUser.Username}", $"{validUser.Password}"},
                {"Jwt:key", "This_ismySecretKeyforEcsjwtLogin"},
                {"Jwt:Issuer", "ece.com"},
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configUser)
                .Build();
            _mockLogger = Mock.Of<ILogger<LoginController>>();
            _sut = new LoginController(_configuration, _mockLogger);
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
            Action act = () => new LoginController(_configuration, null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'logger')");
        }

        [Test]
        public void Given_valid_User_Status200OK()
        {
            // Arrange
            var request = validUser;
            // Act
            var response = _sut.Login(request);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            var responseData = (OkObjectResult)response;
            var jwtAuthResponse = (JwtAuthResponse)responseData.Value;
            jwtAuthResponse.Token.Should().NotBeEmpty();
        }

        [Test]
        public void Given_Invalid_User_UnauthorizedResult()
        {
            // Arrange
            var request = validUser;
            request.Username = "xxx";
            // Act
            var response = _sut.Login(request);

            // Assert
            response.Should().BeOfType<UnauthorizedResult>();
        }

    }
}