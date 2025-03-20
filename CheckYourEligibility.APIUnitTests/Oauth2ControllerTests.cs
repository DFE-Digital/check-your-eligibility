using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Exceptions;
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

            _mockAuthenticateUserUseCase = new Mock<IAuthenticateUserUseCase>(MockBehavior.Strict);
            _mockLogger = Mock.Of<ILogger<Oauth2Controller>>();
            _sut = new Oauth2Controller(_mockLogger, _mockAuthenticateUserUseCase.Object);

            // Setup authentication for user credentials
            _mockAuthenticateUserUseCase
                .Setup(cs => cs.AuthenticateUser(It.Is<SystemUser>(u =>
                    u.Username == validUser.Username && u.Password == validUser.Password)))
                .ReturnsAsync(new JwtAuthResponse { access_token = "validUserToken" });

            _mockAuthenticateUserUseCase
                .Setup(cs => cs.AuthenticateUser(It.Is<SystemUser>(u =>
                    u.Username == invalidUser.Username && u.Password == invalidUser.Password)))
                .ThrowsAsync(new AuthenticationException("invalid_grant", "Invalid username or password"));

            // Setup authentication for client credentials
            _mockAuthenticateUserUseCase
                .Setup(cs => cs.AuthenticateUser(It.Is<SystemUser>(u =>
                    u.client_id == validClient.client_id && u.client_secret == validClient.client_secret)))
                .ReturnsAsync(new JwtAuthResponse { access_token = "validClientToken" });

            _mockAuthenticateUserUseCase
                .Setup(cs => cs.AuthenticateUser(It.Is<SystemUser>(u =>
                    u.client_id == validClientWithScope.client_id &&
                    u.client_secret == validClientWithScope.client_secret &&
                    u.scope == validClientWithScope.scope)))
                .ReturnsAsync(new JwtAuthResponse { access_token = "validClientTokenWithScope" });

            _mockAuthenticateUserUseCase
                .Setup(cs => cs.AuthenticateUser(It.Is<SystemUser>(u =>
                    u.client_id == invalidClient.client_id && u.client_secret == invalidClient.client_secret)))
                .ThrowsAsync(new AuthenticationException("invalid_client", "Invalid client credentials"));

            // Setup for empty credentials
            _mockAuthenticateUserUseCase
                .Setup(cs => cs.AuthenticateUser(It.Is<SystemUser>(u => 
                    string.IsNullOrEmpty(u.Username) && string.IsNullOrEmpty(u.Password) && 
                    string.IsNullOrEmpty(u.client_id) && string.IsNullOrEmpty(u.client_secret))))
                .ThrowsAsync(new AuthenticationException("invalid_request", "Either client_id/client_secret pair or Username/Password pair must be provided"));
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
            Action act = () => new Oauth2Controller(null, null);

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
            _mockAuthenticateUserUseCase.Verify(cs => cs.AuthenticateUser(
                It.Is<SystemUser>(u => u.Username == validUser.Username && u.Password == validUser.Password)),
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
            _mockAuthenticateUserUseCase.Verify(cs => cs.AuthenticateUser(
                It.Is<SystemUser>(u => u.Username == invalidUser.Username && u.Password == invalidUser.Password)),
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
            _mockAuthenticateUserUseCase.Verify(cs => cs.AuthenticateUser(
                It.Is<SystemUser>(u => u.client_id == validClient.client_id && u.client_secret == validClient.client_secret)),
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
            _mockAuthenticateUserUseCase.Verify(cs => cs.AuthenticateUser(
                It.Is<SystemUser>(u =>
                    u.client_id == validClientWithScope.client_id &&
                    u.client_secret == validClientWithScope.client_secret &&
                    u.scope == validClientWithScope.scope)),
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
            _mockAuthenticateUserUseCase.Verify(cs => cs.AuthenticateUser(
                It.Is<SystemUser>(u => u.client_id == invalidClient.client_id && u.client_secret == invalidClient.client_secret)),
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
            response.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = (UnauthorizedObjectResult)response;
            ((ErrorResponse)unauthorizedResult.Value).Errors.First().Title.Should().Be("invalid_request");
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
            _mockAuthenticateUserUseCase.Verify(cs => cs.AuthenticateUser(
                It.Is<SystemUser>(u => u.Username == validUser.Username && u.Password == validUser.Password)),
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
            response.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = (UnauthorizedObjectResult)response;
            ((ErrorResponse)unauthorizedResult.Value).Errors.First().Title.Should().Be("invalid_request");
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

            // Setup specific mock for this test case
            _mockAuthenticateUserUseCase
                .Setup(cs => cs.AuthenticateUser(It.Is<SystemUser>(u =>
                    u.Username == validUser.Username &&
                    u.Password == validUser.Password &&
                    u.grant_type == "invalid_grant_type")))
                .ReturnsAsync(new JwtAuthResponse { access_token = "validUserToken" });

            // Act
            var response = await _sut.LoginJson(request);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            var responseData = (OkObjectResult)response;
            var jwtAuthResponse = (JwtAuthResponse)responseData.Value;
            jwtAuthResponse.access_token.Should().NotBeEmpty();

            // Verify
            _mockAuthenticateUserUseCase.Verify(cs => cs.AuthenticateUser(
                It.Is<SystemUser>(u =>
                    u.Username == validUser.Username &&
                    u.Password == validUser.Password &&
                    u.grant_type == "invalid_grant_type")),
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

            // Setup specific mock for this test case
            _mockAuthenticateUserUseCase
                .Setup(cs => cs.AuthenticateUser(It.Is<SystemUser>(u =>
                    u.Username == validUser.Username &&
                    u.Password == validUser.Password &&
                    u.grant_type == "invalid_grant_type")))
                .ReturnsAsync(new JwtAuthResponse { access_token = "validUserToken" });

            // Act
            var response = await _sut.LoginForm(request);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            var responseData = (OkObjectResult)response;
            var jwtAuthResponse = (JwtAuthResponse)responseData.Value;
            jwtAuthResponse.access_token.Should().NotBeEmpty();

            // Verify
            _mockAuthenticateUserUseCase.Verify(cs => cs.AuthenticateUser(
                It.Is<SystemUser>(u =>
                    u.Username == validUser.Username &&
                    u.Password == validUser.Password &&
                    u.grant_type == "invalid_grant_type")),
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

            // Setup specific mock for this test case
            _mockAuthenticateUserUseCase
                .Setup(cs => cs.AuthenticateUser(It.Is<SystemUser>(u =>
                    u.client_id == validClient.client_id &&
                    u.client_secret == validClient.client_secret &&
                    u.grant_type == "client_credentials")))
                .ReturnsAsync(new JwtAuthResponse { access_token = "validClientToken" });

            // Act
            var response = await _sut.LoginForm(request);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            var responseData = (OkObjectResult)response;
            var jwtAuthResponse = (JwtAuthResponse)responseData.Value;
            jwtAuthResponse.access_token.Should().NotBeEmpty();

            // Verify
            _mockAuthenticateUserUseCase.Verify(cs => cs.AuthenticateUser(
                It.Is<SystemUser>(u =>
                    u.client_id == validClient.client_id &&
                    u.client_secret == validClient.client_secret &&
                    u.grant_type == "client_credentials")),
                Times.Once);
        }

        [Test]
        public async Task Controller_Handles_Generic_Exception_From_UseCase()
        {
            // Arrange
            var request = new SystemUser { Username = "error_user", Password = "error_password" };

            // Setup specific mock for this test case
            _mockAuthenticateUserUseCase
                .Setup(cs => cs.AuthenticateUser(It.Is<SystemUser>(u =>
                    u.Username == "error_user" && u.Password == "error_password")))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var response = await _sut.LoginJson(request);

            // Assert
            response.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = (UnauthorizedObjectResult)response;
            ((ErrorResponse)unauthorizedResult.Value).Errors.First().Title.Should().Be("server_error");
        }
    }
}