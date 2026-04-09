using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using HPAICOmpanionTester.Configuration;
using FlaUIApplication = FlaUI.Core.Application;

namespace HPAICOmpanionTester.Drivers;

/// <summary>
/// Manages the HP AI Companion process and FlaUI session lifetime.
///
/// One instance is created per scenario via Reqnroll context injection, but the
/// underlying FlaUI application lives in the static AppSession and is shared
/// across all scenarios. This avoids a kill-and-relaunch cycle between every
/// test, which saves ~15 seconds per scenario.
///
/// A fresh launch is only triggered when:
///   1. No app instance exists yet (first scenario of the run), OR
///   2. AppSession.NeedsRelaunch is true (set by AppHooks when a scenario fails).
/// </summary>
public sealed class AppDriver
{
    private readonly TestSettings _settings;

    public AppDriver()
    {
        _settings = TestSettings.Load();
    }

    /// <summary>
    /// Ensures the app is running and the session is healthy.
    /// Reuses the existing instance when possible; kills and relaunches only when
    /// the session is dirty or the process has unexpectedly died.
    /// </summary>
    public void EnsureLaunched()
    {
        if (!AppSession.NeedsRelaunch && IsProcessRunning())
            return;

        Kill();
        Launch();
    }

    /// <summary>
    /// Unconditionally kills any running instance.
    /// Used by the AppLaunch feature's explicit "Given the application is not running" step.
    /// </summary>
    public void EnsureNotRunning() => Kill();

    /// <summary>Launches a fresh instance and registers it in AppSession.</summary>
    public void Launch()
    {
        var automation = new UIA3Automation();
        var app        = FlaUIApplication.LaunchStoreApp(_settings.Aumid);
        AppSession.Set(app, automation);
    }

    /// <summary>Marks the session dirty so the next scenario performs a fresh launch.</summary>
    public void MarkForRelaunch() => AppSession.NeedsRelaunch = true;

    /// <summary>
    /// Retrieves the application's main window, waiting up to LaunchTimeoutSeconds.
    /// </summary>
    public Window GetMainWindow() =>
        AppSession.App?.GetMainWindow(
            AppSession.Automation
                ?? throw new InvalidOperationException("Automation not initialised. Call EnsureLaunched() first."),
            TimeSpan.FromSeconds(_settings.LaunchTimeoutSeconds))
        ?? throw new InvalidOperationException(
            "The application has not been launched. Call EnsureLaunched() before getting the window.");

    private bool IsProcessRunning() =>
        Process.GetProcessesByName(_settings.ProcessName).Any();

    private void Kill()
    {
        // Release FlaUI handles before killing the OS process
        AppSession.Teardown();

        if (!IsProcessRunning())
            return;

        using var killer = Process.Start(new ProcessStartInfo
        {
            FileName             = "taskkill",
            Arguments            = $"/F /IM {_settings.ProcessName}.exe",
            UseShellExecute      = false,
            CreateNoWindow       = true,
            RedirectStandardOutput = true
        });
        killer?.WaitForExit(5000);

        // Poll until the process is actually gone — MSIX packages can linger
        // for several seconds after taskkill, and launching before they exit
        // causes LaunchStoreApp to fail or attach to the dying instance.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            if (!IsProcessRunning()) break;
            Thread.Sleep(500);
        }

        Thread.Sleep(1500); // extra settle for MSIX background services
    }
}
