using FlaUI.UIA3;
using FlaUIApplication = FlaUI.Core.Application;

namespace HPAICOmpanionTester.Drivers;

/// <summary>
/// Holds the single shared FlaUI app and automation instances for the entire test run.
/// Scenarios reuse this session instead of kill-and-relaunch each time, keeping
/// test execution fast. NeedsRelaunch is set to true when a scenario fails so that
/// the next scenario starts from a known-clean state.
/// </summary>
internal static class AppSession
{
    private static FlaUIApplication? _app;
    private static UIA3Automation? _automation;

    public static bool NeedsRelaunch { get; set; } = true;

    public static FlaUIApplication? App        => _app;
    public static UIA3Automation?  Automation  => _automation;

    public static void Set(FlaUIApplication app, UIA3Automation automation)
    {
        _app        = app;
        _automation = automation;
        NeedsRelaunch = false;
    }

    /// <summary>Disposes FlaUI objects and resets state, without touching the OS process.</summary>
    public static void Clear()
    {
        _app        = null;
        _automation = null;
        NeedsRelaunch = true;
    }

    /// <summary>Closes the app cleanly and resets the session. Called at end of test run.</summary>
    public static void Teardown()
    {
        try
        {
            _app?.Close();
        }
        catch { /* best effort — must not throw during cleanup */ }

        try
        {
            _app?.Dispose();
        }
        catch { /* disposal may fail if Close() corrupted state */ }

        try
        {
            _automation?.Dispose();
        }
        catch { /* must not throw during cleanup */ }

        Clear();
    }
}
