using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Idioms;
using Moq;

namespace CheckYourEligibility.TestBase;

[ExcludeFromCodeCoverage]
public abstract class TestBase
{
    protected readonly Fixture _fixture = new();
    protected readonly MockRepository MockRepository = new(MockBehavior.Strict);
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

        static void lineBreak()
        {
            Trace.WriteLine(new string('*', 50));
        }

        lineBreak();
        Trace.WriteLine(string.Format("* Elapsed time for test (milliseconds): {0}",
            _stopwatch.Elapsed.TotalMilliseconds));
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