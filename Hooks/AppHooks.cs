using System.Diagnostics;
using System.Runtime.InteropServices;
using HPAICOmpanionTester.Configuration;
using HPAICOmpanionTester.Drivers;
using HPAICOmpanionTester.Support;
using Reqnroll;
using Reqnroll.BoDi;

namespace HPAICOmpanionTester.Hooks;

/// <summary>
/// Scenario and test-run lifecycle hooks.
///
/// After each scenario: if the scenario failed, a screenshot is captured,
/// attached to the Reqnroll HTML report, and the app is flagged for relaunch.
/// Screenshots on success are opt-in via testSettings.json ("screenshotOnSuccess": true).
///
/// After the entire test run: the shared app session is closed cleanly.
/// The AfterTestRun hook must be static — Reqnroll does not support instance DI
/// at test-run scope.
/// </summary>
[Binding]
public sealed class AppHooks
{
    private readonly AppDriver _driver;
    private readonly ScenarioContext _scenarioContext;
    private readonly IReqnrollOutputHelper _outputHelper;
    private readonly TestSettings _settings;

    public AppHooks(AppDriver driver, ScenarioContext scenarioContext, IReqnrollOutputHelper outputHelper, TestSettings settings)
    {
        _driver = driver;
        _scenarioContext = scenarioContext;
        _outputHelper = outputHelper;
        _settings = settings;
    }

    [BeforeTestRun]
    public static void RegisterDependencies(IObjectContainer container)
    {
        container.RegisterInstanceAs(TestSettings.Load());
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        Console.WriteLine($"\n▶ {_scenarioContext.ScenarioInfo.Title}");
    }

    [AfterScenario]
    public void AfterScenario()
    {
        var status = _scenarioContext.ScenarioExecutionStatus;
        Console.WriteLine($"[{_scenarioContext.ScenarioInfo.Title}] {status}");

        if (status != ScenarioExecutionStatus.OK)
        {
            CaptureAndAttach();
            _driver.MarkForRelaunch();
        }
        else if (_settings.ScreenshotOnSuccess)
        {
            CaptureAndAttach();
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    private void CaptureAndAttach()
    {
        try
        {
            var proc = Process.GetProcessesByName(_settings.ProcessName).FirstOrDefault();
            if (proc?.MainWindowHandle is not 0)
            {
                ShowWindow(proc!.MainWindowHandle, SW_RESTORE);
                SetForegroundWindow(proc.MainWindowHandle);
                Thread.Sleep(500);
            }
        }
        catch { /* best effort — app may have crashed */ }

        var screenshotPath = ScreenshotHelper.CaptureScreen();
        if (string.IsNullOrEmpty(screenshotPath))
            return;

        var filename = Path.GetFileName(screenshotPath);
        Console.WriteLine($"  Screenshot: {filename}");
        Console.WriteLine($"  Full path:  {screenshotPath}");
        _outputHelper.AddAttachment(screenshotPath);
    }

    [AfterTestRun]
    public static void TeardownSession()
    {
        AppSession.Teardown();
    }
}

