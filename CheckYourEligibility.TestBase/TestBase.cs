using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Idioms;
using Moq;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.TestBase
{
    [ExcludeFromCodeCoverage]
    public abstract class TestBase
    {
        protected readonly MockRepository MockRepository = new MockRepository(MockBehavior.Strict);
        protected readonly Fixture _fixture = new Fixture();
        private Stopwatch _stopwatch;

        [SetUp]
        public void TestInitialize()
        {
            _stopwatch = Stopwatch.StartNew();
        }

        [TearDown]
        public void TestCleanup()
        {
            MockRepository.VerifyAll();
            _stopwatch.Stop();

            static void lineBreak() => Trace.WriteLine(new string('*', 50));

            lineBreak();
            Trace.WriteLine(string.Format("* Elapsed time for test (milliseconds): {0}", _stopwatch.Elapsed.TotalMilliseconds));
            lineBreak();
        }

        protected void RunGuardClauseConstructorTest<T>()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            // Act & Assert
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(T).GetConstructors());
        }
    }
}