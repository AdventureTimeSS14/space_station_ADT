#nullable enable
using NUnit.Framework.Interfaces;

namespace Content.IntegrationTests.Fixtures;

public abstract partial class GameTest
{
    private string? _failureMessage;
    private string? _failureStackTrace;

    private void RememberFailure()
    {
        var result = TestContext.CurrentContext.Result;

        _failureMessage = result.Message;
        _failureStackTrace = result.StackTrace;
    }

    private void RestoreLostFailure()
    {
        var message = _failureMessage;
        var stackTrace = _failureStackTrace;

        _failureMessage = null;
        _failureStackTrace = null;

        if (string.IsNullOrWhiteSpace(message))
            return;

        foreach (var assertion in TestContext.CurrentContext.Result.Assertions)
        {
            if (assertion.Status is AssertionStatus.Failed or AssertionStatus.Error)
                return;
        }

        Assert.Fail($"{message}\n{stackTrace}");
    }
}
