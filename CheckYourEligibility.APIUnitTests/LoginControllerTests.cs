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
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CheckYourEligibility.APIUnitTests
{
    public class LoginControllerTests : TestBase.TestBase
    {
        private Mock<IAuthenticateUserUseCase> _mockAuthenticateUserUseCase;
        private IConfigurationRoot _configuration;
        private ILogger<LoginController> _mockLogger;
        private LoginController _sut;
        private SystemUser validUser = new SystemUser { Username = "Test", Password = "letmein" };
        private SystemUser invalidUser = new SystemUser { Username = "invalidUser", Password = "wrongPassword" };

        [SetUp]
        public void Setup()
        {
            var configUser = new Dictionary<string, string>
            {
                {$"Jwt:Users:{validUser.Username}", $"{validUser.Password}"},
                {$"Jwt:Users:{invalidUser.Username}", $"{invalidUser.Password}"},
                {"Jwt:key", "This_ismySecretKeyforEcsjwtLogin"},
                {"Jwt:Issuer", "ece.com"},
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configUser)
                .Build();
            _mockAuthenticateUserUseCase = new Mock<IAuthenticateUserUseCase>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<LoginController>>();
            _sut = new LoginController(_configuration, _mockLogger, _mockAuthenticateUserUseCase.Object);

            _mockAuthenticateUserUseCase
            .Setup(cs => cs.Execute(validUser, It.IsAny<JwtConfig>()))
                .ReturnsAsync(new JwtAuthResponse { Token = "validToken" });

            _mockAuthenticateUserUseCase
            .Setup(cs => cs.Execute(invalidUser, It.IsAny<JwtConfig>()))
                .ReturnsAsync((JwtAuthResponse)null);
        }

        [TearDown]
        public void Teardown()
        {
            // _mockAuthenticateUserUseCase.VerifyAll();
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_service_is_null()
        {
            // Arrange
            // Act
            Action act = () => new LoginController(_configuration, null, null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'logger')");
        }

        [Test]
        public async Task Given_valid_User_Status200OK()
        {
            // Arrange
            var request = validUser;

            // Act
            var response = await _sut.Login(request);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            var responseData = (OkObjectResult)response;
            var jwtAuthResponse = (JwtAuthResponse)responseData.Value;
            jwtAuthResponse.Token.Should().NotBeEmpty();

            // Verify
            _mockAuthenticateUserUseCase.Verify(cs => cs.Execute(validUser, It.IsAny<JwtConfig>()), Times.Once);
        }

        [Test]
        public async Task Given_Invalid_User_UnauthorizedResult()
        {
            // Arrange
            var request = invalidUser;

            // Act
            var response = await _sut.Login(request);

            // Assert
            response.Should().BeOfType<UnauthorizedResult>();

            // Verify
            _mockAuthenticateUserUseCase.Verify(cs => cs.Execute(invalidUser, It.IsAny<JwtConfig>()), Times.Once);
        }

        [Test]
        public void Login_throws_ArgumentNullException_when_JwtKey_is_missing()
        {
            // Arrange
            var configUser = new Dictionary<string, string>
            {
                {$"Jwt:Users:{validUser.Username}", $"{validUser.Password}"},
                {"Jwt:Issuer", "ece.com"},
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configUser)
                .Build();
            _sut = new LoginController(_configuration, _mockLogger, _mockAuthenticateUserUseCase.Object);

            // Act
            Func<Task> act = async () => await _sut.Login(validUser);

            // Assert
            act.Should().ThrowExactlyAsync<ArgumentNullException>().WithMessage("Jwt:Key is required");
        }

        [Test]
        public void Login_throws_ArgumentNullException_when_JwtIssuer_is_missing()
        {
            // Arrange
            var configUser = new Dictionary<string, string>
            {
                {$"Jwt:Users:{validUser.Username}", $"{validUser.Password}"},
                {"Jwt:key", "This_ismySecretKeyforEcsjwtLogin"},
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configUser)
                .Build();
            _sut = new LoginController(_configuration, _mockLogger, _mockAuthenticateUserUseCase.Object);

            // Act
            Func<Task> act = async () => await _sut.Login(validUser);

            // Assert
            act.Should().ThrowExactlyAsync<ArgumentNullException>().WithMessage("Jwt:Issuer is required");
        }

        [Test]
        public void Login_throws_ArgumentNullException_when_UserPassword_is_missing()
        {
            // Arrange
            var configUser = new Dictionary<string, string>
            {
                {"Jwt:key", "This_ismySecretKeyforEcsjwtLogin"},
                {"Jwt:Issuer", "ece.com"},
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configUser)
                .Build();
            _sut = new LoginController(_configuration, _mockLogger, _mockAuthenticateUserUseCase.Object);

            // Act
            Func<Task> act = async () => await _sut.Login(validUser);

            // Assert
            act.Should().ThrowExactlyAsync<ArgumentNullException>().WithMessage("UserName:Password is required");
        }
    }
}