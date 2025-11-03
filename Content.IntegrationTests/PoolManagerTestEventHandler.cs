namespace Content.IntegrationTests;

[SetUpFixture]
public sealed class PoolManagerTestEventHandler
{
    // This value is completely arbitrary.
    private static TimeSpan MaximumTotalTestingTimeLimit => TimeSpan.FromMinutes(60); // ADT-Tweak - увеличили с 20 до 60 минут
    private static TimeSpan HardStopTimeLimit => MaximumTotalTestingTimeLimit.Add(TimeSpan.FromMinutes(5)); // ADT-Tweak - увеличили с 1 до 5 минут
    private DateTime _startTime;

    [OneTimeSetUp]
    public void Setup()
    {
        Console.WriteLine("Test Setup Started");
        _startTime = DateTime.Now;
        PoolManager.Startup();
        LogElapsedTime("Startup");

        _ = Task.Delay(MaximumTotalTestingTimeLimit).ContinueWith(_ =>
        {
            var elapsed = DateTime.Now - _startTime;
            Console.WriteLine($"Maximum time exceeded: {elapsed.TotalSeconds}s");
            TestContext.Error.WriteLine($"\n\n{nameof(PoolManagerTestEventHandler)}: ERROR: Tests are taking too long. Shutting down all tests.\n\n");
            PoolManager.Shutdown();
        });

        // If ending it nicely doesn't work within a minute, we do something a bit meaner.
        _ = Task.Delay(HardStopTimeLimit).ContinueWith(_ =>
        {
            var elapsed = DateTime.Now - _startTime;
            Console.WriteLine($"Hard stop triggered after: {elapsed.TotalSeconds}s");
            var deathReport = PoolManager.DeathReport();
            Environment.FailFast($"Tests took way too long;\nDeath Report:\n{deathReport}");
        });

        Console.WriteLine("Test Setup Ended");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        Console.WriteLine("Test TearDown Started");
        PoolManager.Shutdown();
        Console.WriteLine("Test TearDown Ended");
    }

    private void LogElapsedTime(string phase)
    {
        var elapsed = DateTime.Now - _startTime;
        Console.WriteLine($"[{phase}] Elapsed: {elapsed.TotalSeconds}s");
    }
}
