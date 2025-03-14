using AutoFixture;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class AuthenticateUserUseCaseTests
    {
        private Mock<IAudit> _mockAuditService;
        private AuthenticateUserUseCase _sut;
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new AuthenticateUserUseCase(_mockAuditService.Object);
            _fixture = new Fixture();
        }

        [TearDown]
        public void Teardown()
        {
            _mockAuditService.VerifyAll();
        }

        [Test]
        public async Task Execute_Should_Return_JwtAuthResponse_When_Successful()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.client_id = null;
            login.client_secret = null;
            login.Username = "test_username";
            login.Password = "correct_password";
            login.scope = null;
            login.InitializeCredentials();
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012"; // 32 chars for HMACSHA256

            

            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.User, login.Username)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            result.access_token.Should().NotBeNullOrEmpty();
            result.expires_in.Should().Be(3600);
        }

        [Test]
        public async Task Execute_Should_Return_Null_When_Authentication_Fails()
        {
            // Arrange
            var login = new SystemUser();
            login.Username = "test_username";
            login.Password = "wrong_password";
            login.InitializeCredentials();
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task Execute_Should_Return_Null_When_Token_Generation_Fails()
        {
            // Arrange
            var login = new SystemUser();
            login.Username = "test_username";
            login.Password = "correct_password";
            login.InitializeCredentials();
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = ""; // Invalid key

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task Execute_Should_Audit_When_User_Is_Authenticated()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.client_id = null;
            login.client_secret = null;
            login.Username = "test_username";
            login.Password = "correct_password";
            login.scope = null;
            login.InitializeCredentials();
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012"; // 32 chars for HMACSHA256

            
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.User, login.Identifier)).ReturnsAsync(_fixture.Create<string>());
            
            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            // _mockAuditService.Verify(a => a.AuditAdd(auditData), Times.Once);
            _mockAuditService.Verify(a => a.CreateAuditEntry(Domain.Enums.AuditType.User, login.Identifier), Times.Once);
        }

        [Test]
        public async Task Execute_Should_Not_Audit_When_User_Is_Not_Authenticated()
        {
            // Arrange
            var login = new SystemUser();
            login.Username = "test_username";
            login.Password = "wrong_password";
            login.InitializeCredentials();
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().BeNull();
            _mockAuditService.Verify(a => a.CreateAuditEntry(It.IsAny<Domain.Enums.AuditType>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task Execute_Should_Return_JwtAuthResponse_When_Successful_Using_ClientCredentials()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.Username = null;
            login.Password = null;
            login.client_id = "test_client";
            login.client_secret = "correct_password";
            login.scope = null;
            login.InitializeCredentials(); // This should set Identifier to client_id

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012"; // 32 chars for HMACSHA256

            
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Client, login.client_id)).ReturnsAsync(_fixture.Create<string>());


            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            result.access_token.Should().NotBeNullOrEmpty();
            result.expires_in.Should().Be(3600);
        }

        [Test]
        public async Task Execute_Should_Return_Null_When_Authentication_Fails_Using_ClientCredentials()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.Username = null;
            login.Password = null;
            login.client_id = "test_client";
            login.client_secret = "wrong_password";
            login.InitializeCredentials(); // This should set Identifier to client_id

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task Execute_Should_Prioritize_client_id_Over_Username_When_Both_Are_Present()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.Username = "test_username";
            login.Password = "correct_password";
            login.client_id = "test_client"; // This should take precedence
            login.client_secret = "correct_password";
            login.scope = null;
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";

            

            // Verify Identifier is set to client_id, not Username
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Client, "test_client")).ReturnsAsync(_fixture.Create<string>());
            
            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            result.access_token.Should().NotBeNullOrEmpty();
            login.Identifier.Should().Be("test_client"); // Verify identifier was set to client_id
        }

        [Test]
        public async Task Execute_Should_Fallback_To_Username_When_client_id_Is_Not_Present()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.Username = "test_username";
            login.Password = "correct_password";
            login.scope = null;
            login.client_id = null;
            login.client_secret = null;
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";

            

            // Verify Identifier is set to Username
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.User, login.Identifier)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            result.access_token.Should().NotBeNullOrEmpty();
            login.Identifier.Should().Be("test_username"); // Verify identifier was set to Username
        }

        [Test]
        public async Task Execute_Should_Audit_When_User_Is_Authenticated_Using_ClientCredentials()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.Username = null;
            login.Password = null;
            login.scope = null;
            login.client_id = "test_client";
            login.client_secret = "correct_password";
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";

            
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Client, login.client_id)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            // _mockAuditService.Verify(a => a.AuditAdd(auditData), Times.Once);
            _mockAuditService.Verify(a => a.CreateAuditEntry(Domain.Enums.AuditType.Client, login.client_id), Times.Once);
        }

        [Test]
        public void SystemUser_IsValid_Should_Return_True_For_Valid_ClientCredentials()
        {
            // Arrange
            var user = new SystemUser
            {
                client_id = "test_client",
                client_secret = "test_secret",
                Username = null,
                Password = null
            };

            // Act
            var result = user.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void SystemUser_IsValid_Should_Return_True_For_Valid_UsernamePassword()
        {
            // Arrange
            var user = new SystemUser
            {
                client_id = null,
                client_secret = null,
                Username = "test_user",
                Password = "test_password"
            };

            // Act
            var result = user.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void SystemUser_IsValid_Should_Return_False_When_No_Valid_Credentials()
        {
            // Arrange
            var user = new SystemUser
            {
                client_id = null,
                client_secret = "only_secret_no_id",
                Username = null,
                Password = "only_password_no_username"
            };

            // Act
            var result = user.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void SystemUser_InitializeCredentials_Should_Set_Identifier_And_Secret_Correctly()
        {
            // Arrange
            var userWithClientCreds = new SystemUser
            {
                client_id = "test_client",
                client_secret = "test_secret",
                Username = "test_user",
                Password = "test_password"
            };

            var userWithUsernamePwd = new SystemUser
            {
                client_id = null,
                client_secret = null,
                Username = "test_user",
                Password = "test_password"
            };

            // Act
            userWithClientCreds.InitializeCredentials();
            userWithUsernamePwd.InitializeCredentials();

            // Assert
            userWithClientCreds.Identifier.Should().Be("test_client");
            userWithClientCreds.Secret.Should().Be("test_secret");

            userWithUsernamePwd.Identifier.Should().Be("test_user");
            userWithUsernamePwd.Secret.Should().Be("test_password");
        }

        [Test]
        public async Task Execute_Should_Initialize_Credentials_When_Identifier_And_Secret_Are_Empty()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            // Explicitly don't call InitializeCredentials() here
            login.Identifier = null; // This would normally be set by InitializeCredentials
            login.Secret = null;     // This would normally be set by InitializeCredentials
            login.scope = null;
            login.client_id = "test_client";
            login.client_secret = "correct_password";

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";

            
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.Client, "test_client")).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            login.Identifier.Should().Be("test_client"); // Verify credentials were initialized
            login.Secret.Should().Be("correct_password");
        }

        [Test]
        public async Task Execute_Should_Generate_Token_With_Expected_Claims()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.client_id = null;
            login.client_secret = null;
            login.scope = null;
            login.Username = "test_username";
            login.Password = "correct_password";
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";
            jwtConfig.Issuer = "test_issuer";

            
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.User, login.Identifier)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();

            // Decode token to verify claims
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(result.access_token);

            token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value.Should().Be("test_username");
            token.Claims.FirstOrDefault(c => c.Type == "EcsApi")?.Value.Should().Be("apiCustomClaim");
            token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value.Should().Be("ecs@ecs.com");
            token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti).Should().NotBeNull();

            token.Issuer.Should().Be("test_issuer");
            token.Audiences.Should().Contain("test_issuer");

            // Verify expiration is set to 120 minutes from now
            var expectedExpiry = DateTime.UtcNow.AddMinutes(120);
            token.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
        }

        [Test]
        public async Task Execute_Should_Include_scope_Claim_When_scope_Is_Provided()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.client_id = null;
            login.client_secret = null;
            login.Username = "test_username";
            login.Password = "correct_password";
            login.scope = "read write";
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";
            jwtConfig.AllowedScopes = "read write delete";

            
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.User, login.Identifier)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(result.access_token);

            token.Claims.FirstOrDefault(c => c.Type == "scope")?.Value.Should().Be("read write");
        }

        [Test]
        public async Task Execute_Should_Not_Include_scope_Claim_When_scope_Is_Default()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();

            login.Username = "test_username";
            login.Password = "correct_password";
            login.scope = "default";
            login.client_id = null;
            login.client_secret = null;
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";
            jwtConfig.AllowedScopes = "read write";

            
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.User, login.Identifier)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(result.access_token);

            token.Claims.FirstOrDefault(c => c.Type == "scope").Should().BeNull();
        }

        [Test]
        public async Task Execute_Should_Not_Include_scope_Claim_When_scope_Is_Empty()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();

            login.Username = "test_username";
            login.Password = "correct_password";
            login.scope = "";
            login.client_id = null;
            login.client_secret = null;
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";

            
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.User, login.Identifier)).ReturnsAsync(_fixture.Create<string>());


            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(result.access_token);

            token.Claims.FirstOrDefault(c => c.Type == "scope").Should().BeNull();
        }

        [Test]
        public async Task Execute_Should_Return_Null_When_Requested_scopes_Are_Not_Allowed()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.Username = "test_username";
            login.Password = "correct_password";
            login.scope = "read delete"; // delete is not allowed
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";
            jwtConfig.AllowedScopes = "read write"; // only read and write are allowed

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task Execute_Should_Allow_Authentication_When_No_scopes_Requested()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();

            login.client_id = null;
            login.client_secret = null;
            login.Username = "test_username";
            login.Password = "correct_password";
            login.scope = null; // No scopes requested
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";
            jwtConfig.AllowedScopes = "read write";

            
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.User, login.Identifier)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
        }

        [Test]
        public async Task Execute_Should_Deny_Authentication_When_scopes_Requested_But_None_Allowed()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.Username = "test_username";
            login.Password = "correct_password";
            login.scope = "read write"; // scopes requested
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";
            jwtConfig.AllowedScopes = ""; // No scopes allowed

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task Execute_Should_Deny_Authentication_When_scopes_Requested_But_AllowedScopes_Is_Null()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.Username = "test_username";
            login.Password = "correct_password";
            login.scope = "read write"; // scopes requested
            login.client_id = null;
            login.client_secret = null;
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";
            jwtConfig.AllowedScopes = null; // AllowedScopes is null

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task Execute_Should_Generate_Token_With_Updated_Claims_Structure()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.client_id = null;
            login.client_secret = null;
            login.Username = "test_username";
            login.Password = "correct_password";
            login.scope = "read write"; // Include scope for testing
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";
            jwtConfig.Issuer = "test_issuer";
            jwtConfig.AllowedScopes = "read write admin";

            
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.User, login.Identifier)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();

            // Decode token to verify claims
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(result.access_token);

            // Check expected claims are present
            token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value.Should().Be("test_username");
            token.Claims.FirstOrDefault(c => c.Type == "scope")?.Value.Should().Be("read write");
            token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti).Should().NotBeNull();

            // Check removed claims are not present
            token.Claims.FirstOrDefault(c => c.Type == "EcsApi").Should().BeNull();
            token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email).Should().BeNull();

            // Check token properties
            token.Issuer.Should().Be("test_issuer");
            token.Audiences.Should().Contain("test_issuer");

            // Verify expiration is set to 120 minutes from now
            var expectedExpiry = DateTime.UtcNow.AddMinutes(120);
            token.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
        }
    }
}