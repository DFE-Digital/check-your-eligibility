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
            login.ClientId = null;
            login.ClientSecret = null;
            login.Username = "test_username";
            login.Password = "correct_password";
            login.InitializeCredentials();
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012"; // 32 chars for HMACSHA256

            var auditData = _fixture.Create<AuditData>();

            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.User, login.Username)).Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            result.Expires.Should().BeAfter(DateTime.UtcNow);
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
            login.ClientId = null;
            login.ClientSecret = null;
            login.Username = "test_username";
            login.Password = "correct_password";
            login.InitializeCredentials();
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012"; // 32 chars for HMACSHA256

            var auditData = _fixture.Create<AuditData>();

            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.User, login.Identifier)).Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            _mockAuditService.Verify(a => a.AuditAdd(auditData), Times.Once);
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
            _mockAuditService.Verify(a => a.AuditAdd(It.IsAny<AuditData>()), Times.Never);
        }

        [Test]
        public async Task Execute_Should_Return_JwtAuthResponse_When_Successful_Using_ClientCredentials()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.Username = null;
            login.Password = null;
            login.ClientId = "test_client";
            login.ClientSecret = "correct_password";
            login.InitializeCredentials(); // This should set Identifier to ClientId

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012"; // 32 chars for HMACSHA256

            var auditData = _fixture.Create<AuditData>();

            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.User, login.Identifier)).Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            result.Expires.Should().BeAfter(DateTime.UtcNow);
        }

        [Test]
        public async Task Execute_Should_Return_Null_When_Authentication_Fails_Using_ClientCredentials()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.Username = null;
            login.Password = null;
            login.ClientId = "test_client";
            login.ClientSecret = "wrong_password";
            login.InitializeCredentials(); // This should set Identifier to ClientId

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task Execute_Should_Prioritize_ClientId_Over_Username_When_Both_Are_Present()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.Username = "test_username";
            login.Password = "correct_password";
            login.ClientId = "test_client"; // This should take precedence
            login.ClientSecret = "correct_password";
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";

            var auditData = _fixture.Create<AuditData>();

            // Verify Identifier is set to ClientId, not Username
            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.User, "test_client")).Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            login.Identifier.Should().Be("test_client"); // Verify identifier was set to ClientId
        }

        [Test]
        public async Task Execute_Should_Fallback_To_Username_When_ClientId_Is_Not_Present()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.Username = "test_username";
            login.Password = "correct_password";
            login.ClientId = null;
            login.ClientSecret = null;
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";

            var auditData = _fixture.Create<AuditData>();

            // Verify Identifier is set to Username
            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.User, "test_username")).Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            login.Identifier.Should().Be("test_username"); // Verify identifier was set to Username
        }

        [Test]
        public async Task Execute_Should_Audit_When_User_Is_Authenticated_Using_ClientCredentials()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.Username = null;
            login.Password = null;
            login.ClientId = "test_client";
            login.ClientSecret = "correct_password";
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";

            var auditData = _fixture.Create<AuditData>();

            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.User, login.Identifier)).Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            _mockAuditService.Verify(a => a.AuditAdd(auditData), Times.Once);
        }

        [Test]
        public void SystemUser_IsValid_Should_Return_True_For_Valid_ClientCredentials()
        {
            // Arrange
            var user = new SystemUser
            {
                ClientId = "test_client",
                ClientSecret = "test_secret",
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
                ClientId = null,
                ClientSecret = null,
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
                ClientId = null,
                ClientSecret = "only_secret_no_id",
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
                ClientId = "test_client",
                ClientSecret = "test_secret",
                Username = "test_user",
                Password = "test_password"
            };

            var userWithUsernamePwd = new SystemUser
            {
                ClientId = null,
                ClientSecret = null,
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
        public async Task Execute_Should_Not_Audit_When_AuditData_Is_Null()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.ClientId = null;
            login.ClientSecret = null;
            login.Username = "test_username";
            login.Password = "correct_password";
            login.InitializeCredentials();
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012"; // 32 chars for HMACSHA256

            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.User, login.Identifier)).Returns((AuditData)null);

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            _mockAuditService.Verify(a => a.AuditAdd(It.IsAny<AuditData>()), Times.Never);
        }

        [Test]
        public async Task Execute_Should_Continue_When_AuditAdd_Throws_Exception()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.ClientId = null;
            login.ClientSecret = null;
            login.Username = "test_username";
            login.Password = "correct_password";
            login.InitializeCredentials();
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012"; // 32 chars for HMACSHA256

            var auditData = _fixture.Create<AuditData>();

            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.User, login.Identifier)).Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData)).ThrowsAsync(new Exception("Audit log error"));

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            _mockAuditService.Verify(a => a.AuditAdd(auditData), Times.Once);
        }

        [Test]
        public async Task Execute_Should_Initialize_Credentials_When_Identifier_And_Secret_Are_Empty()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            // Explicitly don't call InitializeCredentials() here
            login.Identifier = null; // This would normally be set by InitializeCredentials
            login.Secret = null;     // This would normally be set by InitializeCredentials
            login.ClientId = "test_client";
            login.ClientSecret = "correct_password";

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";

            var auditData = _fixture.Create<AuditData>();
            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.User, "test_client")).Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData)).ReturnsAsync(_fixture.Create<string>());

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
            login.ClientId = null;
            login.ClientSecret = null;
            login.Username = "test_username";
            login.Password = "correct_password";
            login.InitializeCredentials();

            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.ExpectedSecret = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012";
            jwtConfig.Issuer = "test_issuer";

            var auditData = _fixture.Create<AuditData>();
            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.User, login.Identifier)).Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();

            // Decode token to verify claims
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(result.Token);

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
    }
}