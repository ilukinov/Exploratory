using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;

namespace HPAICOmpanionTester.Support;

/// <summary>
/// Thin wrapper around FlaUI's Retry utilities.
/// Centralises polling defaults so page objects don't hardcode intervals.
/// </summary>
public static class WaitHelper
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Polls <paramref name="finder"/> until it returns a non-null element or the timeout expires.
    /// Returns null on timeout — callers decide whether that is an error.
    /// </summary>
    public static AutomationElement? ForElement(
        Func<AutomationElement?> finder,
        TimeSpan timeout,
        TimeSpan? pollInterval = null)
    {
        return Retry.WhileNull(
            finder,
            timeout,
            pollInterval ?? DefaultInterval).Result;
    }

    /// <summary>
    /// Polls <paramref name="condition"/> until it returns true or the timeout expires.
    /// </summary>
    public static bool Until(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null)
    {
        return Retry.WhileFalse(
            condition,
            timeout,
            pollInterval ?? DefaultInterval).Success;
    }
}
