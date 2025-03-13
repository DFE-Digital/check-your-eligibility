using AutoFixture;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace CheckYourEligibility.APIUnitTests.UseCases
{
    [TestFixture]
    public class CreateOrUpdateUserUseCaseTests : TestBase.TestBase
    {
        private Mock<IUsers> _mockUserService;
        private Mock<IAudit> _mockAuditService;
        private CreateOrUpdateUserUseCase _sut;

        [SetUp]
        public void Setup()
        {
            _mockUserService = new Mock<IUsers>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new CreateOrUpdateUserUseCase(_mockUserService.Object, _mockAuditService.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _mockUserService.VerifyAll();
            _mockAuditService.VerifyAll();
        }

        [Test]
        public async Task Execute_Should_Return_UserSaveItemResponse_When_Successful()
        {
            // Arrange
            var request = _fixture.Create<UserCreateRequest>();
            var responseId = _fixture.Create<string>();
            
            _mockUserService.Setup(us => us.Create(request.Data)).ReturnsAsync(responseId);
            _mockAuditService.Setup(a => a.CreateAuditEntry(Domain.Enums.AuditType.User, responseId)).ReturnsAsync(_fixture.Create<string>());

            var expectedResponse = new UserSaveItemResponse { Data = responseId };

            // Act
            var result = await _sut.Execute(request);

            // Assert
            result.Should().BeEquivalentTo(expectedResponse);
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_userService_is_null()
        {
            // Arrange
            IUsers userService = null;
            IAudit auditService = _mockAuditService.Object;

            // Act
            Action act = () => new CreateOrUpdateUserUseCase(userService, auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'userService')");
        }

        [Test]
        public void Constructor_throws_argumentNullException_when_auditService_is_null()
        {
            // Arrange
            IUsers userService = _mockUserService.Object;
            IAudit auditService = null;

            // Act
            Action act = () => new CreateOrUpdateUserUseCase(userService, auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'auditService')");
        }
    }
}