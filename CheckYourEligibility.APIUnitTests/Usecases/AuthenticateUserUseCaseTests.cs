using AutoFixture;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class AuthenticateUserUseCaseTests
    {
        private Mock<IAudit> _mockAuditService;
        private Mock<ILogger<AuthenticateUserUseCase>> _mockLogger;
        private JwtSettings _jwtSettings;
        private AuthenticateUserUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<AuthenticateUserUseCase>>();
            _jwtSettings = new JwtSettings
            {
                Key = "test_key_12345678901234567890123456789012",
                Issuer = "test_issuer",
                Clients = new Dictionary<string, ClientSettings>
                {
                    ["test_client"] = new ClientSettings
                    {
                        Secret = "correct_password",
                        Scope = "read write admin"
                    }
                }
            };
            _sut = new AuthenticateUserUseCase(_mockAuditService.Object, _mockLogger.Object, _jwtSettings);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockAuditService.VerifyAll();
        }

        [Test]
        public async Task AuthenticateUser_Should_Return_JwtAuthResponse_When_Successful()
        {
            // Arrange
            var login = new SystemUser
            {
                client_id = "test_client",
                client_secret = "correct_password"
            };

            _mockAuditService
                .Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Client, login.client_id))
                .ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login);

            // Assert
            result.Should().NotBeNull();
            result.access_token.Should().NotBeNullOrEmpty();
            result.expires_in.Should().BeGreaterThan(0);
            result.token_type.Should().Be("Bearer");
        }

        [Test]
        public void AuthenticateUser_Should_Throw_InvalidClientException_When_Authentication_Fails()
        {
            // Arrange
            var login = new SystemUser
            {
                client_id = "test_client",
                client_secret = "wrong_password"
            };

            // Act & Assert
            Func<Task> act = async () => await _sut.Execute(login);
            act.Should().ThrowAsync<InvalidClientException>()
                .WithMessage("Invalid client credentials");
        }

        [Test]
        public void AuthenticateUser_Should_Throw_InvalidClientException_When_User_Not_Found()
        {
            // Arrange
            var login = new SystemUser
            {
                client_id = "unknown_user",
                client_secret = "any_password"
            };

            // Act & Assert
            Func<Task> act = async () => await _sut.Execute(login);
            act.Should().ThrowAsync<InvalidClientException>()
                .WithMessage("The client authentication failed");
        }

        [Test]
        public void AuthenticateUser_Should_Throw_ServerErrorException_When_Key_Is_Empty()
        {
            // Arrange
            var login = new SystemUser
            {
                client_id = "test_client",
                client_secret = "correct_password"
            };

            _jwtSettings.Key = "";

            // Act & Assert
            Func<Task> act = async () => await _sut.Execute(login);
            act.Should().ThrowAsync<ServerErrorException>()
                .WithMessage("The authorization server is misconfigured. Key is required.");
        }

        [Test]
        public void AuthenticateUser_Should_Throw_ServerErrorException_When_Issuer_Is_Empty()
        {
            // Arrange
            var login = new SystemUser
            {
                client_id = "test_client",
                client_secret = "correct_password"
            };

            _jwtSettings.Issuer = "";

            // Act & Assert
            Func<Task> act = async () => await _sut.Execute(login);
            act.Should().ThrowAsync<ServerErrorException>()
                .WithMessage("The authorization server is misconfigured. Issuer is required.");
        }

        [Test]
        public async Task AuthenticateUser_Should_Audit_When_User_Is_Authenticated()
        {
            // Arrange
            var login = new SystemUser
            {
                client_id = "test_client",
                client_secret = "correct_password"
            };

            _mockAuditService
                .Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Client, login.client_id))
                .ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login);

            // Assert
            result.Should().NotBeNull();
            _mockAuditService.Verify(a => a.CreateAuditEntry(Domain.Enums.AuditType.Client, login.client_id), Times.Once);
        }

        [Test]
        public async Task AuthenticateUser_Should_Return_JwtAuthResponse_When_Successful_Using_ClientCredentials()
        {
            // Arrange
            var login = new SystemUser
            {
                client_id = "test_client",
                client_secret = "correct_password"
            };

            _mockAuditService
                .Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Client, login.client_id))
                .ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login);

            // Assert
            result.Should().NotBeNull();
            result.access_token.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void AuthenticateUser_Should_Throw_InvalidClientException_When_Authentication_Fails_Using_ClientCredentials()
        {
            // Arrange
            var login = new SystemUser
            {
                client_id = "test_client",
                client_secret = "wrong_password"
            };

            // Act & Assert
            Func<Task> act = async () => await _sut.Execute(login);
            act.Should().ThrowAsync<InvalidClientException>()
                .WithMessage("Invalid client credentials");
        }

        [Test]
        public void AuthenticateUser_Should_Throw_InvalidRequestException_When_No_Valid_Credentials()
        {
            // Arrange
            var login = new SystemUser(); // No credentials set

            // Act & Assert
            Func<Task> act = async () => await _sut.Execute(login);
            act.Should().ThrowAsync<InvalidRequestException>()
                .WithMessage("Either client_id/client_secret pair or Username/Password pair must be provided");
        }

        [Test]
        public async Task AuthenticateUser_Should_Include_scope_Claim_When_scope_Is_Provided()
        {
            // Arrange
            var login = new SystemUser
            {
                client_id = "test_client",
                client_secret = "correct_password",
                scope = "read write"
            };

            _mockAuditService
                .Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Client, login.client_id))
                .ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login);

            // Assert
            result.Should().NotBeNull();

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(result.access_token);

            token.Claims.FirstOrDefault(c => c.Type == "scope")?.Value.Should().Be("read write");
        }

        [Test]
        public void AuthenticateUser_Should_Throw_InvalidScopeException_When_Requested_scopes_Are_Not_Allowed()
        {
            // Arrange
            var login = new SystemUser
            {
                client_id = "test_client",
                client_secret = "correct_password",
                scope = "read delete" // delete is not allowed
            };

            // Act & Assert
            Func<Task> act = async () => await _sut.Execute(login);
            act.Should().ThrowAsync<InvalidScopeException>()
                .WithMessage("The requested scope is invalid, unknown, or exceeds the scope granted by the resource owner");
        }

        [Test]
        public async Task AuthenticateUser_Should_Generate_Token_With_Expected_Claims()
        {
            // Arrange
            var login = new SystemUser
            {
                client_id = "test_client",
                client_secret = "correct_password"
            };

            _mockAuditService
                .Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Client, login.client_id))
                .ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login);

            // Assert
            result.Should().NotBeNull();

            // Decode token to verify claims
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(result.access_token);

            // Check expected claims are present
            token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value.Should().Be("test_client");
            token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti).Should().NotBeNull();

            // Check token properties
            token.Issuer.Should().Be("test_issuer");
            token.Audiences.Should().Contain("test_issuer");

            // Verify expiration is set to 120 minutes from now
            var expectedExpiry = DateTime.UtcNow.AddMinutes(120);
            token.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
        }

        [Test]
        public async Task AuthenticateUser_Should_Log_Warning_But_Continue_When_GrantType_Is_Invalid()
        {
            // Arrange
            var login = new SystemUser
            {
                client_id = "test_client",
                client_secret = "correct_password",
                grant_type = "invalid_grant_type" // Using an invalid grant type
            };

            _mockAuditService
                .Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Client, login.client_id))
                .ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login);

            // Assert
            // Verify we still got a valid response despite invalid grant type
            result.Should().NotBeNull();
            result.access_token.Should().NotBeNullOrEmpty();

            // Verify the warning was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Unsupported grant_type: {login.grant_type}")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Test]
        public void AuthenticateUser_Should_Throw_InvalidScopeException_When_Client_Has_No_Allowed_Scopes_Configured()
        {
            // Arrange
            // Create a client with no scopes configured
            string clientIdWithoutScopes = "client_without_scopes";
            string clientSecret = "some_secret";

            _jwtSettings.Clients[clientIdWithoutScopes] = new ClientSettings
            {
                Secret = clientSecret,
                Scope = null // No scopes configured for this client
            };

            var login = new SystemUser
            {
                client_id = clientIdWithoutScopes,
                client_secret = clientSecret,
                scope = "read write" // Requesting scopes that aren't defined
            };

            // Act & Assert
            Func<Task> act = async () => await _sut.Execute(login);

            // Should throw InvalidScopeException with the specific error message
            act.Should().ThrowAsync<InvalidScopeException>()
                .WithMessage("Client is not authorized for any scopes");

            // Verify the error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Allowed scopes not found for client: {clientIdWithoutScopes}")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Test]
        public void AuthenticateUser_Should_Throw_ServerErrorException_When_Token_Generation_Fails()
        {
            // Arrange
            var login = new SystemUser
            {
                client_id = "test_client",
                client_secret = "correct_password"
            };

            // Set up a scenario that would cause token generation to fail
            // An invalid key length will cause the JWT token generation to fail
            _jwtSettings.Key = "too_short_key";

            // Act & Assert
            Func<Task> act = async () => await _sut.Execute(login);

            // Should throw ServerErrorException with the specific error message
            act.Should().ThrowAsync<ServerErrorException>()
                .WithMessage("The authorization server encountered an unexpected error");
        }

        [Test]
        public void AuthenticateUser_Should_Throw_InvalidClientException_When_Secret_Not_Found_For_Valid_Identifier()
        {
            // Arrange
            // Create a malformed client entry where the identifier exists but has a null secret
            string userWithNoSecret = "client_with_no_secret";

            // Add the user to the dictionary but with null secret
            _jwtSettings.Clients[userWithNoSecret] = new ClientSettings(){Secret = null};

            var login = new SystemUser
            {
                client_id = userWithNoSecret,
                client_secret = "any_password"
            };

            // Act & Assert
            Func<Task> act = async () => await _sut.Execute(login);

            // Should throw InvalidClientException with the correct message
            act.Should().ThrowAsync<InvalidClientException>()
                .WithMessage("The client authentication failed");

            // Verify the error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Authentication secret not found for identifier: {userWithNoSecret}")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Test]
        public void ValidateScopes_Should_Return_False_When_RequestedScopes_Provided_But_AllowedScopes_Empty()
        {
            // This test uses reflection to access the private ValidateScopes method
            // Arrange
            string requestedScopes = "read write";
            string allowedScopes = null;  // No allowed scopes

            // Act
            var method = typeof(AuthenticateUserUseCase).GetMethod("ValidateScopes",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var result = (bool)method.Invoke(null, new object[] { requestedScopes, allowedScopes });

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void ValidateScopes_Should_Return_True_When_RequestedScopes_Is_Empty()
        {
            // This test uses reflection to access the private ValidateScopes method
            // Arrange
            string requestedScopes = "";  // Empty requested scopes
            string allowedScopes = null;  // No allowed scopes

            // Act
            var method = typeof(AuthenticateUserUseCase).GetMethod("ValidateScopes",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var result = (bool)method.Invoke(null, new object[] { requestedScopes, allowedScopes });

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void ValidateScopes_Should_Return_True_When_RequestedScopes_Is_Default()
        {
            // This test uses reflection to access the private ValidateScopes method
            // Arrange
            string requestedScopes = "default";  // Default scope
            string allowedScopes = null;  // No allowed scopes

            // Act
            var method = typeof(AuthenticateUserUseCase).GetMethod("ValidateScopes",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var result = (bool)method.Invoke(null, new object[] { requestedScopes, allowedScopes });

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void ValidateScopes_Should_Return_False_When_RequestedScope_Not_In_AllowedScopes()
        {
            // This test uses reflection to access the private ValidateScopes method
            // Arrange
            string requestedScopes = "read write delete";  // Requesting scopes including 'delete'
            string allowedScopes = "read write";  // Only 'read' and 'write' are allowed

            // Act
            var method = typeof(AuthenticateUserUseCase).GetMethod("ValidateScopes",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var result = (bool)method.Invoke(null, new object[] { requestedScopes, allowedScopes });

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void ValidateScopes_Should_Return_True_When_All_RequestedScopes_In_AllowedScopes()
        {
            // This test uses reflection to access the private ValidateScopes method
            // Arrange
            string requestedScopes = "read write";  // Requesting 'read' and 'write'
            string allowedScopes = "read write admin";  // All requested scopes are allowed

            // Act
            var method = typeof(AuthenticateUserUseCase).GetMethod("ValidateScopes",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var result = (bool)method.Invoke(null, new object[] { requestedScopes, allowedScopes });

            // Assert
            result.Should().BeTrue();
        }
    }
}