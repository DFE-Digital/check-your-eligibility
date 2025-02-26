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
using System.Threading.Tasks;

namespace CheckYourEligibility.WebApp.Tests.UseCases
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
            login.Password = "correct_password";
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.UserPassword = "correct_password";
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
            var login = _fixture.Create<SystemUser>();
            login.Password = "wrong_password";
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.UserPassword = "correct_password";

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task Execute_Should_Return_Null_When_Token_Generation_Fails()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.Password = "correct_password";
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.UserPassword = "correct_password";
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
            login.Password = "correct_password";
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.UserPassword = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012"; // 32 chars for HMACSHA256

            var auditData = _fixture.Create<AuditData>();

            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.User, login.Username)).Returns(auditData);
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
            var login = _fixture.Create<SystemUser>();
            login.Password = "wrong_password";
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.UserPassword = "correct_password";

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().BeNull();
            _mockAuditService.Verify(a => a.AuditAdd(It.IsAny<AuditData>()), Times.Never);
        }

        [Test]
        public async Task Execute_Should_Not_Audit_When_AuditData_Is_Null()
        {
            // Arrange
            var login = _fixture.Create<SystemUser>();
            login.Password = "correct_password";
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.UserPassword = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012"; // 32 chars for HMACSHA256

            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.User, login.Username)).Returns((AuditData)null);

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
            login.Password = "correct_password";
            var jwtConfig = _fixture.Create<JwtConfig>();
            jwtConfig.UserPassword = "correct_password";
            jwtConfig.Key = "test_key_12345678901234567890123456789012"; // 32 chars for HMACSHA256

            var auditData = _fixture.Create<AuditData>();

            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.User, login.Username)).Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData)).ThrowsAsync(new Exception("Audit log error"));

            // Act
            var result = await _sut.Execute(login, jwtConfig);

            // Assert
            result.Should().NotBeNull();
            _mockAuditService.Verify(a => a.AuditAdd(auditData), Times.Once);
        }
    }
}