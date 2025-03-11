using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.WebApp.Controllers;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace CheckYourEligibility.APIUnitTests
{
    public class Oauth2ControllerTests : TestBase.TestBase
    {
        private Mock<IAuthenticateUserUseCase> _mockAuthenticateUserUseCase;
        private IConfigurationRoot _configuration;
        private ILogger<Oauth2Controller> _mockLogger;
        private Oauth2Controller _sut;

        // User credentials
        private SystemUser validUser;
        private SystemUser invalidUser;

        // Client credentials
        private SystemUser validClient;
        private SystemUser invalidClient;
        private SystemUser validClientWithScope;

        [SetUp]
        public void Setup()
        {
            // Initialize test users and clients
            validUser = new SystemUser { Username = "Test", Password = "letmein" };
            invalidUser = new SystemUser { Username = "invalidUser", Password = "wrongPassword" };
            validClient = new SystemUser { client_id = "client1", client_secret = "secret1" };
            validClientWithScope = new SystemUser { client_id = "client1", client_secret = "secret1", scope = "read write" };
            invalidClient = new SystemUser { client_id = "invalidClient", client_secret = "wrongSecret" };

            var configData = new Dictionary<string, string>
            {
                // User credentials config
                {$"Jwt:Users:{validUser.Username}", $"{validUser.Password}"},
                {$"Jwt:Users:{invalidUser.Username}", $"{invalidUser.Password}"},
                
                // Client credentials config
                {$"Jwt:Clients:{validClient.client_id}:Secret", $"{validClient.client_secret}"},
                {$"Jwt:Clients:{validClientWithScope.client_id}:Scope", "read write"},
                {$"Jwt:Clients:{invalidClient.client_id}:Secret", $"{invalidClient.client_secret}"},
                
                // JWT config
                {"Jwt:Key", "This_ismySecretKeyforEcsjwtLogin"},
                {"Jwt:Issuer", "ece.com"},
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            _mockAuthenticateUserUseCase = new Mock<IAuthenticateUserUseCase>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<Oauth2Controller>>();
            _sut = new Oauth2Controller(_configuration, _mockLogger, _mockAuthenticateUserUseCase.Object);

            // Setup authentication for user credentials
            _mockAuthenticateUserUseCase
                .Setup(cs => cs.Execute(It.Is<SystemUser>(u =>
                    u.Username == validUser.Username && u.Password == validUser.Password),
                    It.IsAny<JwtConfig>()))
                .ReturnsAsync(new JwtAuthResponse { access_token = "validUserToken" });

            _mockAuthenticateUserUseCase
                .Setup(cs => cs.Execute(It.Is<SystemUser>(u =>
                    u.Username == invalidUser.Username && u.Password == invalidUser.Password),
                    It.IsAny<JwtConfig>()))
                .ReturnsAsync((JwtAuthResponse)null);

            // Setup authentication for client credentials
            _mockAuthenticateUserUseCase
                .Setup(cs => cs.Execute(It.Is<SystemUser>(u =>
                    u.client_id == validClient.client_id && u.client_secret == validClient.client_secret),
                    It.IsAny<JwtConfig>()))
                .ReturnsAsync(new JwtAuthResponse { access_token = "validClientToken" });

            _mockAuthenticateUserUseCase
                .Setup(cs => cs.Execute(It.Is<SystemUser>(u =>
                    u.client_id == validClientWithScope.client_id &&
                    u.client_secret == validClientWithScope.client_secret &&
                    u.scope == validClientWithScope.scope),
                    It.IsAny<JwtConfig>()))
                .ReturnsAsync(new JwtAuthResponse { access_token = "validClientTokenWithScope" });

            _mockAuthenticateUserUseCase
                .Setup(cs => cs.Execute(It.Is<SystemUser>(u =>
                    u.client_id == invalidClient.client_id && u.client_secret == invalidClient.client_secret),
                    It.IsAny<JwtConfig>()))
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
            Action act = () => new Oauth2Controller(_configuration, null, null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().EndWithEquivalentOf("Value cannot be null. (Parameter 'logger')");
        }

        [Test]
        public async Task Given_valid_User_Status200OK()
        {
            // Arrange
            var request = validUser;

            // Act
            var response = await _sut.LoginJson(request);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            var responseData = (OkObjectResult)response;
            var jwtAuthResponse = (JwtAuthResponse)responseData.Value;
            jwtAuthResponse.access_token.Should().NotBeEmpty();

            // Verify
            _mockAuthenticateUserUseCase.Verify(cs => cs.Execute(
                It.Is<SystemUser>(u => u.Username == validUser.Username && u.Password == validUser.Password),
                It.IsAny<JwtConfig>()),
                Times.Once);
        }

        [Test]
        public async Task Given_Invalid_User_UnauthorizedResult()
        {
            // Arrange
            var request = invalidUser;

            // Act
            var response = await _sut.LoginJson(request);

            // Assert
            response.Should().BeOfType<UnauthorizedObjectResult>();

            // Verify
            _mockAuthenticateUserUseCase.Verify(cs => cs.Execute(
                It.Is<SystemUser>(u => u.Username == invalidUser.Username && u.Password == invalidUser.Password),
                It.IsAny<JwtConfig>()),
                Times.Once);
        }

        [Test]
        public async Task Given_valid_Client_Status200OK()
        {
            // Arrange
            var request = validClient;

            // Act
            var response = await _sut.LoginJson(request);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            var responseData = (OkObjectResult)response;
            var jwtAuthResponse = (JwtAuthResponse)responseData.Value;
            jwtAuthResponse.access_token.Should().NotBeEmpty();

            // Verify
            _mockAuthenticateUserUseCase.Verify(cs => cs.Execute(
                It.Is<SystemUser>(u => u.client_id == validClient.client_id && u.client_secret == validClient.client_secret),
                It.IsAny<JwtConfig>()),
                Times.Once);
        }

        [Test]
        public async Task Given_valid_Client_With_Scope_Status200OK()
        {
            // Arrange
            var request = validClientWithScope;

            // Act
            var response = await _sut.LoginJson(request);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            var responseData = (OkObjectResult)response;
            var jwtAuthResponse = (JwtAuthResponse)responseData.Value;
            jwtAuthResponse.access_token.Should().NotBeEmpty();

            // Verify
            _mockAuthenticateUserUseCase.Verify(cs => cs.Execute(
                It.Is<SystemUser>(u =>
                    u.client_id == validClientWithScope.client_id &&
                    u.client_secret == validClientWithScope.client_secret &&
                    u.scope == validClientWithScope.scope),
                It.Is<JwtConfig>(c => c.AllowedScopes == "read write")),
                Times.Once);
        }

        [Test]
        public async Task Given_Invalid_Client_UnauthorizedResult()
        {
            // Arrange
            var request = invalidClient;

            // Act
            var response = await _sut.LoginJson(request);

            // Assert
            response.Should().BeOfType<UnauthorizedObjectResult>();

            // Verify
            _mockAuthenticateUserUseCase.Verify(cs => cs.Execute(
                It.Is<SystemUser>(u => u.client_id == invalidClient.client_id && u.client_secret == invalidClient.client_secret),
                It.IsAny<JwtConfig>()),
                Times.Once);
        }

        [Test]
        public async Task Given_No_Credentials_BadRequest()
        {
            // Arrange
            var request = new SystemUser(); // No credentials provided

            // Act
            var response = await _sut.LoginJson(request);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)response;
            ((ErrorResponse)badRequestResult.Value).Errors.First().Title.Should().Be("Either client_id/client_secret pair or Username/Password pair must be provided");
        }

        [Test]
        public async Task Login_throws_Exception_when_JwtKey_is_missing()
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
            _sut = new Oauth2Controller(_configuration, _mockLogger, _mockAuthenticateUserUseCase.Object);

            // Act
            var response = await _sut.LoginJson(validUser);

            // Assert
            response.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Test]
        public async Task Login_throws_Exception_when_JwtIssuer_is_missing()
        {
            // Arrange
            var configUser = new Dictionary<string, string>
            {
                {$"Jwt:Users:{validUser.Username}", $"{validUser.Password}"},
                {"Jwt:Key", "This_ismySecretKeyforEcsjwtLogin"},
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configUser)
                .Build();
            _sut = new Oauth2Controller(_configuration, _mockLogger, _mockAuthenticateUserUseCase.Object);

            // Act
            var response = await _sut.LoginJson(validUser);

            // Assert
            response.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Test]
        public async Task Login_returns_Unauthorized_when_UserPassword_is_missing()
        {
            // Arrange
            var configUser = new Dictionary<string, string>
            {
                {"Jwt:Key", "This_ismySecretKeyforEcsjwtLogin"},
                {"Jwt:Issuer", "ece.com"},
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configUser)
                .Build();
            _sut = new Oauth2Controller(_configuration, _mockLogger, _mockAuthenticateUserUseCase.Object);

            // Act
            var response = await _sut.LoginJson(validUser);

            // Assert
            response.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Test]
        public async Task Login_returns_Unauthorized_when_client_secret_is_missing()
        {
            // Arrange
            var configUser = new Dictionary<string, string>
            {
                {"Jwt:Key", "This_ismySecretKeyforEcsjwtLogin"},
                {"Jwt:Issuer", "ece.com"},
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configUser)
                .Build();
            _sut = new Oauth2Controller(_configuration, _mockLogger, _mockAuthenticateUserUseCase.Object);

            // Act
            var response = await _sut.LoginJson(validClient);

            // Assert
            response.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Test]
        public async Task LoginForm_Given_valid_User_Status200OK()
        {
            // Arrange
            var request = validUser;

            // Act
            var response = await _sut.LoginForm(request);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            var responseData = (OkObjectResult)response;
            var jwtAuthResponse = (JwtAuthResponse)responseData.Value;
            jwtAuthResponse.access_token.Should().NotBeEmpty();

            // Verify
            _mockAuthenticateUserUseCase.Verify(cs => cs.Execute(
                It.Is<SystemUser>(u => u.Username == validUser.Username && u.Password == validUser.Password),
                It.IsAny<JwtConfig>()),
                Times.Once);
        }

        [Test]
        public async Task LoginForm_Given_Invalid_User_UnauthorizedResult()
        {
            // Arrange
            var request = invalidUser;

            // Act
            var response = await _sut.LoginForm(request);

            // Assert
            response.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Test]
        public async Task LoginForm_Given_No_Credentials_BadRequest()
        {
            // Arrange
            var request = new SystemUser(); // No credentials provided

            // Act
            var response = await _sut.LoginForm(request);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)response;
            ((ErrorResponse)badRequestResult.Value).Errors.First().Title.Should().Be("Either client_id/client_secret pair or Username/Password pair must be provided");
        }

        [Test]
        public async Task Client_With_Missing_Scope_Configuration_Returns_Unauthorized()
        {
            // Arrange
            var clientWithInvalidScope = new SystemUser
            {
                client_id = "client1",
                client_secret = "secret1",
                scope = "admin" // Scope that doesn't match configuration
            };

            var configData = new Dictionary<string, string>
            {
                {$"Jwt:Clients:{clientWithInvalidScope.client_id}:Secret", $"{clientWithInvalidScope.client_secret}"},
                // No scope configuration for this client
                {"Jwt:Key", "This_ismySecretKeyforEcsjwtLogin"},
                {"Jwt:Issuer", "ece.com"},
            };

            var configWithoutScope = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            _sut = new Oauth2Controller(configWithoutScope, _mockLogger, _mockAuthenticateUserUseCase.Object);

            // Act
            var response = await _sut.LoginJson(clientWithInvalidScope);

            // Assert
            response.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Test]
        public async Task LoginJson_Given_Invalid_GrantType_StillProcessesRequest()
        {
            // Arrange
            var request = new SystemUser
            {
                Username = validUser.Username,
                Password = validUser.Password,
                grant_type = "invalid_grant_type" // Invalid grant type
            };

            // Act
            var response = await _sut.LoginJson(request);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            var responseData = (OkObjectResult)response;
            var jwtAuthResponse = (JwtAuthResponse)responseData.Value;
            jwtAuthResponse.access_token.Should().NotBeEmpty();

            // Verify
            _mockAuthenticateUserUseCase.Verify(cs => cs.Execute(
                It.Is<SystemUser>(u =>
                    u.Username == validUser.Username &&
                    u.Password == validUser.Password &&
                    u.grant_type == "invalid_grant_type"),
                It.IsAny<JwtConfig>()),
                Times.Once);
        }

        [Test]
        public async Task LoginForm_Given_Invalid_GrantType_StillProcessesRequest()
        {
            // Arrange
            var request = new SystemUser
            {
                Username = validUser.Username,
                Password = validUser.Password,
                grant_type = "invalid_grant_type" // Invalid grant type
            };

            // Act
            var response = await _sut.LoginForm(request);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            var responseData = (OkObjectResult)response;
            var jwtAuthResponse = (JwtAuthResponse)responseData.Value;
            jwtAuthResponse.access_token.Should().NotBeEmpty();

            // Verify
            _mockAuthenticateUserUseCase.Verify(cs => cs.Execute(
                It.Is<SystemUser>(u =>
                    u.Username == validUser.Username &&
                    u.Password == validUser.Password &&
                    u.grant_type == "invalid_grant_type"),
                It.IsAny<JwtConfig>()),
                Times.Once);
        }

        [Test]
        public async Task LoginForm_Given_Valid_ClientCredentialsGrantType_ReturnsToken()
        {
            // Arrange
            var request = new SystemUser
            {
                client_id = validClient.client_id,
                client_secret = validClient.client_secret,
                grant_type = "client_credentials" // Valid grant type
            };

            // Act
            var response = await _sut.LoginForm(request);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            var responseData = (OkObjectResult)response;
            var jwtAuthResponse = (JwtAuthResponse)responseData.Value;
            jwtAuthResponse.access_token.Should().NotBeEmpty();

            // Verify
            _mockAuthenticateUserUseCase.Verify(cs => cs.Execute(
                It.Is<SystemUser>(u =>
                    u.client_id == validClient.client_id &&
                    u.client_secret == validClient.client_secret &&
                    u.grant_type == "client_credentials"),
                It.IsAny<JwtConfig>()),
                Times.Once);
        }
    }
}