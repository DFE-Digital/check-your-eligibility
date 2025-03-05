using AutoFixture;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FluentAssertions;
using Moq;

namespace CheckYourEligibility.WebApp.Tests.UseCases
{
    [TestFixture]
    public class SearchEstablishmentsUseCaseTests : TestBase.TestBase
    {
        private Mock<IEstablishmentSearch> _mockEstablishmentSearchService;
        private Mock<IAudit> _mockAuditService;
        private SearchEstablishmentsUseCase _sut;

        [SetUp]
        public void Setup()
        {
            _mockEstablishmentSearchService = new Mock<IEstablishmentSearch>(MockBehavior.Strict);
            _mockAuditService = new Mock<IAudit>(MockBehavior.Strict);
            _sut = new SearchEstablishmentsUseCase(_mockEstablishmentSearchService.Object, _mockAuditService.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _mockEstablishmentSearchService.VerifyAll();
            _mockAuditService.VerifyAll();
        }

        [Test]
        public async Task Execute_Should_Return_Results_When_Successful()
        {
            // Arrange
            var query = "test";
            var establishments = _fixture.CreateMany<Establishment>().ToList();
            var auditData = _fixture.Create<AuditData>();

            _mockEstablishmentSearchService.Setup(es => es.Search(query)).ReturnsAsync(establishments);
            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.Establishment, string.Empty)).Returns(auditData);
            _mockAuditService.Setup(a => a.AuditAdd(auditData)).ReturnsAsync(_fixture.Create<string>());

            // Act
            var result = await _sut.Execute(query);

            // Assert
            result.Should().BeEquivalentTo(establishments);
        }

        [Test]
        public async Task Execute_Should_Return_Empty_When_No_Results()
        {
            // Arrange
            var query = "test";
            var establishments = new List<Establishment>();

            _mockEstablishmentSearchService.Setup(es => es.Search(query)).ReturnsAsync(establishments);
            _mockAuditService.Setup(a => a.AuditDataGet(Domain.Enums.AuditType.Establishment, string.Empty)).Returns((AuditData)null);


            // Act
            var result = await _sut.Execute(query);

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public void Execute_Should_Throw_Exception_When_Query_Is_NullOrWhiteSpace()
        {
            // Arrange
            string query = null;

            // Act
            Func<Task> act = async () => await _sut.Execute(query);

            // Assert
            act.Should().ThrowAsync<ArgumentException>().WithMessage("Required input query was empty. (Parameter 'query')");
        }

        [Test]
        public void Execute_Should_Throw_Exception_When_Query_Length_Is_Less_Than_Three()
        {
            // Arrange
            string query = "ab";

            // Act
            Func<Task> act = async () => await _sut.Execute(query);

            // Assert
            act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("query.Length must be between 3 and 2147483647 (Parameter 'query.Length')");
        }

        [Test]
        public void Constructor_Throws_ArgumentNullException_When_Service_Is_Null()
        {
            // Arrange
            IEstablishmentSearch service = null;
            IAudit auditService = _mockAuditService.Object;

            // Act
            Action act = () => new SearchEstablishmentsUseCase(service, auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'service')");
        }

        [Test]
        public void Constructor_Throws_ArgumentNullException_When_AuditService_Is_Null()
        {
            // Arrange
            IEstablishmentSearch service = _mockEstablishmentSearchService.Object;
            IAudit auditService = null;

            // Act
            Action act = () => new SearchEstablishmentsUseCase(service, auditService);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.Message.Should().Contain("Value cannot be null. (Parameter 'auditService')");
        }
    }
}